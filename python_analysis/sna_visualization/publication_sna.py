"""
Publication-ready SNA visualization for boss fight episodes.
Shows damage and healing networks with fixed raid-style layout.
"""

import argparse
import json
import math
from collections import defaultdict
from itertools import cycle
from typing import Dict, List, Tuple

import matplotlib.pyplot as plt
import networkx as nx
import numpy as np

try:
    from networkx.algorithms import community
    HAS_COMMUNITY = True
except ImportError:
    HAS_COMMUNITY = False

# ============================================================================
# VISUALIZATION CONFIGURATION - Modify these values to adjust appearance
# ============================================================================

# Note: Node sizes and edge widths are now fully data-driven using logarithmic scaling
# based on actual values from the JSON. The following are only for visual styling:

# Taunt line configuration (Tank→Boss)
TAUNT_LINE_WIDTH_MULTIPLIER = 2.0  # Tank taunt line width multiplier (applied after log scaling)
TAUNT_COLOR = '#A78BFA'  # Purple color for taunt

# Threat line transparency (removed - using solid lines now)
THREAT_LINE_ALPHA = 0.7  # Solid threat lines (same as other edges)

# Boss damage to MeleeDPS multiplier (visual emphasis only, applied after log scaling)
BOSS_DAMAGE_TO_MELEE_MULTIPLIER = 5.0  # Make boss→MeleeDPS damage line much thicker (late training)
BOSS_DAMAGE_TO_MELEE_EARLY_MULTIPLIER = 0.3  # Make boss→MeleeDPS damage line very thin (early training)

# MeleeDPS node size multiplier for late training
MELEE_DPS_LATE_NODE_SIZE_MULTIPLIER = 1.5  # Make MeleeDPS node bigger in late training

# Self-loop circle configuration
HEALER_SELF_LOOP_RADIUS = 0.3  # Radius of the circle around Healer node
HEALER_SELF_LOOP_LINEWIDTH = 3  # Line width of the self-loop circle
HEALER_SELF_LOOP_ALPHA = 0.8  # Transparency of the self-loop circle

def load_episodes(path: str) -> List[dict]:
    """Load episodes from JSON file"""
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    if "episodes" in data:
        return data["episodes"]
    elif isinstance(data, list):
        return data
    else:
        return [data]

def extract_damage_healing_threat_edges(episodes: List[dict], episode_range: Tuple[int, int] = None) -> Tuple[List[Tuple], List[Tuple], List[Tuple], List[Tuple], Dict[str, int]]:
    """
    Extract damage, healing, and threat edges from episodes.
    Returns: (boss_damage_edges, party_damage_edges, healing_edges, threat_edges, class_selection_counts)
    boss_damage_edges: [(Boss, target, weight), ...] - Boss attacking party
    party_damage_edges: [(party, Boss, weight), ...] - Party attacking boss
    healing_edges: [(Healer, target, weight), ...] - Only Healer can heal
    threat_edges: [(party, Boss, weight), ...] - Threat generation
    class_selection_counts: {role: count} - How often each class was selected
    """
    # First, check if episodes have explicit damage/heal events
    has_events = False
    if episodes and len(episodes) > 0:
        first_ep = episodes[0]
        if "events" in first_ep or "combatLog" in first_ep:
            has_events = True
    
    boss_damage_edges_dict = defaultdict(float)  # (Boss, tgt) -> total_damage
    party_damage_edges_dict = defaultdict(float)  # (party, Boss) -> total_damage
    healing_edges_dict = defaultdict(float)  # (Healer, tgt) -> total_healing
    threat_edges_dict = defaultdict(float)  # (party, Boss) -> total_threat
    taunt_edges_dict = defaultdict(float)  # (Tank, Boss) -> total_taunt
    class_selection_counts = defaultdict(int)  # role -> count
    
    agent_classes = {}
    if episodes and "agentIds" in episodes[0] and "agentClassValues" in episodes[0]:
        for a_id, a_cls in zip(episodes[0]["agentIds"], episodes[0]["agentClassValues"]):
            agent_classes[a_id] = a_cls
    
    boss_ids = {aid for aid, cls in agent_classes.items() if str(cls).lower() == "boss"}
    boss_id = next(iter(boss_ids), "Boss")
    
    # Map agent names to roles
    role_map = {}
    for agent_id, agent_class in agent_classes.items():
        if agent_class.lower() == "boss":
            role_map[agent_id] = "Boss"
        elif agent_class.lower() == "tank":
            role_map[agent_id] = "Tank"
        elif agent_class.lower() == "healer":
            role_map[agent_id] = "Healer"
        elif agent_class.lower() == "meleedps":
            role_map[agent_id] = "MeleeDPS"
        elif agent_class.lower() == "rangeddps":
            role_map[agent_id] = "RangedDPS"
        else:
            # Infer from name if class is None
            agent_lower = agent_id.lower()
            if "boss" in agent_lower:
                role_map[agent_id] = "Boss"
            elif "tank" in agent_lower or "party member 1" in agent_lower:
                role_map[agent_id] = "Tank"
            elif "healer" in agent_lower:
                role_map[agent_id] = "Healer"
            elif "melee" in agent_lower or any(f"party member {i}" in agent_lower for i in [2, 3, 4]):
                role_map[agent_id] = "MeleeDPS"
            else:
                role_map[agent_id] = "RangedDPS"
    
    # Track party activity to infer boss attacks (only if using action-based inference)
    party_attack_counts = defaultdict(int)  # role -> count
    seen_selections = set()  # Track class selections per episode per agent
    
    # Aggregate damage and healing
    for episode in episodes:
        episode_num = episode.get("episode", 0)
        
        # Filter by episode range if specified
        if episode_range:
            if not (episode_range[0] <= episode_num <= episode_range[1]):
                continue
        
        # Try to use explicit events first
        if has_events:
            events = episode.get("events", episode.get("combatLog", []))
            for ev in events:
                ev_type = str(ev.get("type", "")).lower()
                if ev_type in ("damage", "heal"):
                    src = ev.get("source", "unknown")
                    tgt = ev.get("target", "unknown")
                    amt = float(ev.get("amount", 0))
                    
                    # Map to roles
                    src_role = role_map.get(src, src)
                    tgt_role = role_map.get(tgt, tgt)
                    
                    if ev_type == "damage":
                        if src_role == "Boss":
                            boss_damage_edges_dict[(src_role, tgt_role)] += amt
                        elif tgt_role == "Boss":
                            party_damage_edges_dict[(src_role, tgt_role)] += amt
                        # Ignore party-to-party damage (not allowed)
                    elif ev_type == "heal":
                        # Only Healer can heal
                        if src_role == "Healer":
                            healing_edges_dict[(src_role, tgt_role)] += amt
        
        # Check for explicit targetId in actions (new format)
        actions = episode.get("actions", [])
        has_explicit_targets = any("targetId" in act for act in actions)
        
        if has_explicit_targets:
            # Use explicit targets from actions
            for act in actions:
                branch = act.get("branch")
                val = act.get("value", 0)
                agent = act.get("agentId", "unknown")
                target = act.get("targetId")
                
                if val == 1 and target:
                    agent_role = role_map.get(agent, agent)
                    target_role = role_map.get(target, target)
                    
                    if branch == "attack":
                        # Only allow: Boss→Party or Party→Boss (no party-to-party damage)
                        if agent_role == "Boss" and target_role != "Boss":
                            boss_damage_edges_dict[(agent_role, target_role)] += 1.0
                        elif agent_role != "Boss" and target_role == "Boss":
                            party_damage_edges_dict[(agent_role, target_role)] += 1.0
                            party_attack_counts[agent_role] += 1
                            # Threat generation: damage = threat
                            threat_edges_dict[(agent_role, target_role)] += 1.0
                        # Ignore party-to-party attacks
                    elif branch == "heal":
                        # Only Healer can heal
                        if agent_role == "Healer" and target_role != "Boss":
                            healing_edges_dict[(agent_role, target_role)] += 1.0
                        # Ignore non-Healer healing attempts
                    elif branch == "threat_boost" or branch == "taunt":
                        # Tank taunt/threat boost → Boss
                        if agent_role == "Tank" and target_role == "Boss":
                            # Taunt is separate from threat, we'll track it separately
                            pass  # Will be handled below
                
                # Track class selection (count once per episode per agent)
                if branch == "class_selection" and val >= 0:
                    agent_role = role_map.get(agent, agent)
                    # Only count once per episode per agent
                    episode_key = f"{episode_num}_{agent}"
                    if episode_key not in seen_selections:
                        seen_selections.add(episode_key)
                        class_selection_counts[agent_role] += 1
        
        # Fall back to action-based inference if no events and no explicit targets
        if not has_events and not has_explicit_targets:
            actions = episode.get("actions", [])
            
            for act in actions:
                branch = act.get("branch")
                val = act.get("value", 0)
                agent = act.get("agentId", "unknown")
                agent_role = role_map.get(agent, agent)
                
                if branch == "attack" and val == 1:
                    # Damage: attacker -> target
                    agent_lower = agent.lower()
                    is_boss = agent in boss_ids or agent_lower == "boss" or "boss" in agent_lower
                    
                    if is_boss:
                        # Boss attacks - distribute to party members based on threat/aggro
                        # Tank takes most damage (aggro anchor), others take less
                        for pm_id, pm_class in agent_classes.items():
                            if pm_id not in boss_ids:
                                pm_role = role_map.get(pm_id, pm_id)
                                # Boss damage: Tank takes 60%, others 20% each
                                if pm_role == "Tank":
                                    damage = 100.0 * 0.6
                                else:
                                    damage = 100.0 * 0.2 / max(1, len([p for p in agent_classes.keys() if p not in boss_ids]) - 1)
                                damage_edges_dict[("Boss", pm_role)] += damage
                    else:
                        # Party member attacks boss
                        # Damage varies by class
                        if agent_role == "MeleeDPS":
                            damage = 10.0  # High DPS
                        elif agent_role == "RangedDPS":
                            damage = 5.0   # Medium DPS
                        elif agent_role == "Tank":
                            damage = 2.0   # Low DPS (tank role)
                        else:
                            damage = 1.0
                        damage_edges_dict[(agent_role, "Boss")] += damage
                        party_attack_counts[agent_role] += 1
                
                elif branch == "heal" and val == 1:
                    # Healing: only Healer can heal
                    if agent_role == "Healer":
                        # Healers typically prioritize Tank (aggro), then DPS
                        # Tank gets 50%, MeleeDPS 30%, RangedDPS 20%
                        healing_edges_dict[(agent_role, "Tank")] += 10.0 * 0.5
                        healing_edges_dict[(agent_role, "MeleeDPS")] += 10.0 * 0.3
                        healing_edges_dict[(agent_role, "RangedDPS")] += 10.0 * 0.2
                    # Ignore non-Healer healing attempts
                
                # Track class selection (count once per episode per agent)
                if branch == "class_selection" and val >= 0:
                    episode_key = f"{episode_num}_{agent}"
                    if episode_key not in seen_selections:
                        seen_selections.add(episode_key)
                        class_selection_counts[agent_role] += 1
        
        # Infer boss attacks based on party damage dealt (only if using action-based inference and no explicit targets)
        # Skip this if we already have explicit boss→party edges from targetId
        if not has_explicit_targets:
            total_party_damage = sum(w for (src, tgt), w in party_damage_edges_dict.items() if tgt == "Boss")
            if total_party_damage > 0:
                # Boss deals damage proportional to party damage (boss is strong)
                # Tank takes most aggro (60%), others share the rest
                for role in ["Tank", "MeleeDPS", "RangedDPS", "Healer"]:
                    if role in party_attack_counts:
                        # Boss damage proportional to party damage to boss
                        party_damage = party_damage_edges_dict.get((role, "Boss"), 0)
                        if party_damage > 0:
                            # Boss deals ~10x more damage than party (boss is strong)
                            if role == "Tank":
                                boss_damage = party_damage * 0.1 * 0.6  # Tank takes 60% of boss damage
                            else:
                                boss_damage = party_damage * 0.1 * 0.4 / max(1, len([r for r in party_attack_counts.keys() if r != "Tank"]))
                            boss_damage_edges_dict[("Boss", role)] += boss_damage
    
    # Convert to lists, with minimum weight threshold to prevent completely vanishing edges
    min_weight = 1e-3  # Minimum weight to show edge (prevents edges from completely disappearing)
    boss_damage_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in boss_damage_edges_dict.items() if weight > 0]
    party_damage_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in party_damage_edges_dict.items() if weight > 0]
    healing_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in healing_edges_dict.items() if weight > 0]
    threat_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in threat_edges_dict.items() if weight > 0]
    taunt_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in taunt_edges_dict.items() if weight > 0]
    
    # If no explicit taunt actions, infer taunt from Tank attacks (Tank generates taunt when attacking)
    if not taunt_edges:
        for (src, tgt), weight in party_damage_edges_dict.items():
            if src == "Tank" and tgt == "Boss":
                # Tank attacks generate taunt (proportional to damage)
                taunt_edges.append((src, tgt, max(weight * 0.5, min_weight)))
    
    # Debug output
    print(f"  Boss damage edges: {len(boss_damage_edges)}")
    for src, tgt, w in boss_damage_edges[:5]:
        print(f"    {src} -> {tgt}: {w:.1f}")
    print(f"  Party damage edges: {len(party_damage_edges)}")
    for src, tgt, w in party_damage_edges[:5]:
        print(f"    {src} -> {tgt}: {w:.1f}")
    print(f"  Healing edges: {len(healing_edges)}")
    for src, tgt, w in healing_edges[:5]:
        print(f"    {src} -> {tgt}: {w:.1f}")
    print(f"  Threat edges: {len(threat_edges)}")
    print(f"  Taunt edges: {len(taunt_edges)}")
    print(f"  Class selections: {dict(class_selection_counts)}")
    
    return boss_damage_edges, party_damage_edges, healing_edges, threat_edges, taunt_edges, class_selection_counts

def create_raid_layout():
    """Create fixed raid-style positions for nodes"""
    return {
        "Boss": (0.0, 1.0),
        "Tank": (0.0, 0.0),
        "MeleeDPS": (0.4, -0.4),
        "Healer": (-0.4, -0.4),
        "RangedDPS": (0.8, -0.2),
    }

def draw_publication_graph(damage_edges: List[Tuple], healing_edges: List[Tuple], 
                          output_path: str, title: str, figsize=(8, 8), style="fixed"):
    """
    Create publication-ready network graph
    
    style: "fixed" for raid-style layout, "organic" for force-directed with community detection
    """
    
    # Build directed graph
    G = nx.DiGraph()
    
    # Add boss damage edges (yellow)
    for src, tgt, w in boss_damage_edges:
        if G.has_edge(src, tgt):
            G[src][tgt]["weight"] += w
            G[src][tgt]["kind"] = "boss_damage"
        else:
            G.add_edge(src, tgt, weight=w, kind="boss_damage")
    
    # Add party damage edges (red)
    for src, tgt, w in party_damage_edges:
        if G.has_edge(src, tgt):
            G[src][tgt]["weight"] += w
            if G[src][tgt].get("kind") == "threat":
                G[src][tgt]["kind"] = "mixed_damage_threat"
            else:
                G[src][tgt]["kind"] = "party_damage"
        else:
            G.add_edge(src, tgt, weight=w, kind="party_damage")
    
    # Add healing edges (green, only Healer)
    for src, tgt, w in healing_edges:
        if G.has_edge(src, tgt):
            G[src][tgt]["weight"] += w
            G[src][tgt]["kind"] = "heal"
        else:
            G.add_edge(src, tgt, weight=w, kind="heal")
    
    # Add threat edges (blue)
    for src, tgt, w in threat_edges:
        if G.has_edge(src, tgt):
            # If there's already a party_damage edge, combine them
            if G[src][tgt].get("kind") == "party_damage":
                G[src][tgt]["kind"] = "mixed_damage_threat"
            else:
                G[src][tgt]["weight"] += w
                G[src][tgt]["kind"] = "threat"
        else:
            G.add_edge(src, tgt, weight=w, kind="threat")
    
    # Add taunt edges (purple, Tank→Boss only)
    for src, tgt, w in taunt_edges:
        if src == "Tank" and tgt == "Boss":
            if G.has_edge(src, tgt):
                # If there's already an edge, add taunt as separate kind
                if G[src][tgt].get("kind") not in ("taunt", "mixed_taunt"):
                    G[src][tgt]["kind"] = "mixed_taunt"
            else:
                G.add_edge(src, tgt, weight=w, kind="taunt")
    
    # Ensure all nodes exist (add isolated nodes if needed)
    all_nodes = set()
    for src, tgt, _ in boss_damage_edges + party_damage_edges + healing_edges + threat_edges:
        all_nodes.add(src)
        all_nodes.add(tgt)
    
    # Always include all roles as nodes, even if they have no edges
    # This ensures the full 5-node raid layout is visible
    all_roles = ["Boss", "Tank", "Healer", "MeleeDPS", "RangedDPS"]
    for role in all_roles:
        if role not in G.nodes():
            G.add_node(role)
    
    # Add self-loop for Healer (healing themselves)
    if "Healer" in G.nodes():
        G.add_edge("Healer", "Healer", weight=1.0, kind="heal_self")
    
    # Also add any nodes from edges
    for node in all_nodes:
        if node not in G.nodes():
            G.add_node(node)
    
    if style == "organic":
        # Organic style with community detection and force-directed layout
        # Convert to undirected for community detection
        G_undir = G.to_undirected()
        
        # Community detection for node colors
        if HAS_COMMUNITY:
            try:
                communities = list(community.greedy_modularity_communities(G_undir))
                community_map = {}
                for i, comm in enumerate(communities):
                    for node in comm:
                        community_map[node] = i
                
                # Assign a color per community
                color_cycle = cycle(["#FF6B6B", "#4ECDC4", "#556270", "#C7F464", "#C44D58"])
                comm_color = {i: c for i, c in zip(range(len(communities)), color_cycle)}
                node_colors = [comm_color.get(community_map.get(n, 0), "#95A5A6") for n in G.nodes()]
            except:
                # Fallback to role-based colors
                role_color = {
                    "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
                    "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
                }
                node_colors = [role_color.get(n, "#95A5A6") for n in G.nodes()]
        else:
            # Fallback to role-based colors
            role_color = {
                "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
                "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
            }
            node_colors = [role_color.get(n, "#95A5A6") for n in G.nodes()]
        
        # Node sizes from degree centrality
        deg_cent = nx.degree_centrality(G_undir)
        node_sizes = []
        for n in G.nodes():
            size = 800 + 3000 * deg_cent.get(n, 0.1)
            # Make MeleeDPS larger (configurable multiplier)
            if n == "MeleeDPS":
                size = size * MELEE_DPS_NODE_SIZE_MULTIPLIER
            node_sizes.append(size)
        
        # Force-directed layout
        pos = nx.spring_layout(G, k=0.8, iterations=200, seed=42)
        
        # Edge widths from weights using logarithmic scaling (data-driven)
        weights = [G[u][v]["weight"] for u, v in G.edges()]
        max_w = max(weights) if weights else 1.0
        min_w = min(weights) if weights else 1.0
        
        # Use logarithmic scaling
        edge_widths_list = []
        for w in weights:
            if max_w > min_w:
                log_weight = math.log(1 + w) / math.log(1 + max_w)
                edge_width = 0.5 + 4.5 * log_weight  # Range: 0.5 to 5.0
            else:
                edge_width = 2.75  # Default if all weights are equal
            edge_widths_list.append(edge_width)
        
        # Create mapping for easier lookup
        edge_widths = {e: width for e, width in zip(G.edges(), edge_widths_list)}
        
        # Separate edges by kind
        damage_edges_list = [(u, v) for u, v in G.edges() if G[u][v]["kind"] in ("damage", "mixed")]
        heal_edges_list = [(u, v) for u, v in G.edges() if G[u][v]["kind"] in ("heal", "heal_self")]
        
        # Create figure
        fig, ax = plt.subplots(figsize=figsize, facecolor='white')
        
        # Draw nodes
        nx.draw_networkx_nodes(G, pos, node_size=node_sizes, node_color=node_colors,
                              ax=ax, alpha=0.9, linewidths=1.5, edgecolors="#333333")
        
        # Draw edges with proper colors (softer, with alpha)
        # Boss damage (yellow)
        if boss_damage_edges_list:
            boss_widths = [edge_widths[list(G.edges()).index((u, v))] for u, v in boss_damage_edges_list]
            nx.draw_networkx_edges(G, pos, edgelist=boss_damage_edges_list,
                                 width=boss_widths, edge_color='#FFD93D',  # Yellow
                                 alpha=0.4, arrows=True, arrowsize=10,
                                 ax=ax, arrowstyle="-|>")
        
        # Party damage (red) - use curved edges to avoid overlap with threat lines
        if party_damage_edges_list:
            party_widths = [edge_widths.get((u, v), 2) for u, v in party_damage_edges_list]
            # Draw with negative curves, opposite direction from threat lines
            for idx, (u, v) in enumerate(party_damage_edges_list):
                rad = -0.3 - (idx * 0.1)  # Strong negative curves, well separated
                nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                     width=[party_widths[idx]], edge_color='#FF6B6B',  # Red
                                     alpha=0.4, arrows=True, arrowsize=10,
                                     ax=ax, arrowstyle="-|>", connectionstyle=f'arc3,rad={rad}')
        
        # Threat (blue) - widths are data-driven, use curved edges to avoid overlap with party damage
        if threat_edges_list:
            threat_widths = [edge_widths.get((u, v), 2) for u, v in threat_edges_list]
            # Draw with positive curves, opposite direction from party damage
            for idx, (u, v) in enumerate(threat_edges_list):
                rad = 0.3 + (idx * 0.1)  # Strong positive curves, well separated from party damage
                nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                     width=[threat_widths[idx]], edge_color='#4DABF7',  # Blue
                                     alpha=0.7, arrows=True, arrowsize=10,
                                     ax=ax, arrowstyle="-|>", connectionstyle=f'arc3,rad={rad}')
        
        if heal_edges_list:
            # Separate self-loops from regular edges
            regular_heal_edges = [(u, v) for u, v in heal_edges_list if u != v]
            self_heal_edges = [(u, v) for u, v in heal_edges_list if u == v]
            
            if regular_heal_edges:
                heal_widths = [edge_widths.get((u, v), 2) for u, v in regular_heal_edges]
                # Use curved edges with different radii to prevent overlap
                for idx, (u, v) in enumerate(regular_heal_edges):
                    rad = 0.2 if idx % 2 == 0 else -0.2  # Alternate curve direction
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                         width=[heal_widths[idx]], edge_color='#51CF66',
                                         alpha=0.4, arrows=True, arrowsize=10,
                                         ax=ax, arrowstyle="-|>", style='solid',
                                         connectionstyle=f'arc3,rad={rad}')
            
            # Draw self-loops (Healer healing themselves) - arrow pointing into itself
            if self_heal_edges:
                for u, v in self_heal_edges:
                    # Draw a self-loop edge that curves back into the node
                    # Use a large curve radius to create a loop that points into the node
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                         width=[HEALER_SELF_LOOP_LINEWIDTH], 
                                         edge_color='#51CF66',
                                         style='solid', arrows=True, arrowsize=10,
                                         alpha=HEALER_SELF_LOOP_ALPHA, ax=ax, 
                                         arrowstyle='-|>', connectionstyle='arc3,rad=0.5')
        
        # Draw labels
        nx.draw_networkx_labels(G, pos, font_size=8, font_weight='bold', ax=ax)
        
    else:
        # Fixed raid-style layout (original style)
        pos = create_raid_layout()
        pos = {node: pos[node] for node in G.nodes() if node in pos}
        
        # Node sizes from centrality
        try:
            centrality = nx.degree_centrality(G.to_undirected())
        except:
            centrality = {n: 0.1 for n in G.nodes()}
        
        node_sizes = [max(500, 3000 * centrality.get(n, 0.1)) for n in G.nodes()]
        
        # Node colors by role
        role_color = {
            "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
            "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
        }
        node_colors = [role_color.get(n, "#95A5A6") for n in G.nodes()]
        
        # Edge widths from weight
        all_weights = [G[u][v]["weight"] for u, v in G.edges()]
        max_w = max(all_weights) if all_weights else 1.0
        edge_widths = {e: max(1, 8 * (G[u][v]["weight"] / max_w)) for u, v, e in zip(
            [u for u, v in G.edges()],
            [v for u, v in G.edges()],
            list(G.edges())
        )}
        
        # Separate edges by kind
        damage_edges_list = [(u, v) for u, v in G.edges() if G[u][v]["kind"] in ("damage", "mixed")]
        heal_edges_list = [(u, v) for u, v in G.edges() if G[u][v]["kind"] in ("heal", "heal_self")]
        
        # Create figure
        fig, ax = plt.subplots(figsize=figsize, facecolor='white')
        
        # Draw nodes
        nx.draw_networkx_nodes(G, pos, node_size=node_sizes, node_color=node_colors,
                              ax=ax, alpha=0.9, edgecolors='#2C3E50', linewidths=2)
        
        # Draw labels
        nx.draw_networkx_labels(G, pos, font_size=12, font_weight='bold',
                               ax=ax, font_color='#2C3E50')
        
        # Draw edges with proper colors
        # Boss damage (yellow)
        if boss_damage_edges_list:
            boss_widths = [edge_widths.get((u, v), 2) for u, v in boss_damage_edges_list]
            nx.draw_networkx_edges(G, pos, edgelist=boss_damage_edges_list,
                                 width=boss_widths, edge_color='#FFD93D',  # Yellow for Boss damage
                                 style='solid', arrows=True, arrowsize=20,
                                 alpha=0.7, ax=ax, arrowstyle='->', connectionstyle='arc3,rad=0.1')
        
        # Party damage (red) - use curved edges to avoid overlap with threat lines
        if party_damage_edges_list:
            party_widths = [edge_widths.get((u, v), 2) for u, v in party_damage_edges_list]
            # Draw with negative curves, opposite direction from threat lines
            for idx, (u, v) in enumerate(party_damage_edges_list):
                rad = -0.3 - (idx * 0.1)  # Strong negative curves, well separated
                nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                     width=[party_widths[idx]], edge_color='#FF6B6B',  # Red for Party damage
                                     style='solid', arrows=True, arrowsize=20,
                                     alpha=0.7, ax=ax, arrowstyle='->', connectionstyle=f'arc3,rad={rad}')
        
        # Threat (blue) - widths are data-driven, use curved edges to avoid overlap
        if threat_edges_list:
            threat_widths = [edge_widths.get((u, v), 2) for u, v in threat_edges_list]
            # Draw with different curve radii to prevent overlap
            for idx, (u, v) in enumerate(threat_edges_list):
                rad = 0.25 + (idx * 0.08)  # Different curve direction and spacing
                nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                     width=[threat_widths[idx]], edge_color='#4DABF7',  # Blue for Threat
                                     style='solid', arrows=True, arrowsize=20,
                                     alpha=0.7, ax=ax, arrowstyle='->', 
                                     connectionstyle=f'arc3,rad={rad}')
        
        # Taunt (purple, Tank→Boss only)
        if taunt_edges_list:
            taunt_widths = [edge_widths.get((u, v), 2) * TAUNT_LINE_WIDTH_MULTIPLIER for u, v in taunt_edges_list]
            nx.draw_networkx_edges(G, pos, edgelist=taunt_edges_list,
                                 width=taunt_widths, edge_color=TAUNT_COLOR,  # Purple for Taunt
                                 style='solid', arrows=True, arrowsize=20,
                                 alpha=0.7, ax=ax, arrowstyle='->', connectionstyle='arc3,rad=0.1')
        
        # Draw healing edges (solid, green) with curved paths to avoid overlap
        if heal_edges_list:
            # Separate self-loops from regular edges for different styling
            regular_heal_edges = [(u, v) for u, v in heal_edges_list if u != v]
            self_heal_edges = [(u, v) for u, v in heal_edges_list if u == v]
            
            if regular_heal_edges:
                heal_widths = [edge_widths.get((u, v), 2) for u, v in regular_heal_edges]
                # Use curved edges with different radii to prevent overlap
                # Alternate between positive and negative curves
                for idx, (u, v) in enumerate(regular_heal_edges):
                    rad = 0.2 if idx % 2 == 0 else -0.2  # Alternate curve direction
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                         width=[heal_widths[idx]], edge_color='#51CF66',
                                         style='solid', arrows=True, arrowsize=20,
                                         alpha=0.7, ax=ax, arrowstyle='->', 
                                         connectionstyle=f'arc3,rad={rad}')
            
            # Draw self-loops (Healer healing themselves) - arrow pointing into itself
            if self_heal_edges:
                for u, v in self_heal_edges:
                    # Draw a self-loop edge that curves back into the node
                    # Use a large curve radius to create a loop that points into the node
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                         width=[HEALER_SELF_LOOP_LINEWIDTH], 
                                         edge_color='#51CF66',
                                         style='solid', arrows=True, arrowsize=15,
                                         alpha=HEALER_SELF_LOOP_ALPHA, ax=ax, 
                                         arrowstyle='->', connectionstyle='arc3,rad=0.5')
        
        # Add legend
        from matplotlib.patches import Patch
        from matplotlib.lines import Line2D
        
        # Create legend with color table
        legend_elements = [
            Line2D([0], [0], color='#FFD93D', linewidth=3, label='Boss Damage (Yellow)'),
            Line2D([0], [0], color='#FF6B6B', linewidth=3, label='Party Damage (Red)'),
            Line2D([0], [0], color='#4DABF7', linewidth=3, label='Threat (Blue)'),
            Line2D([0], [0], color='#51CF66', linewidth=3, label='Healing (Green)'),
            Patch(facecolor='#FF6B6B', edgecolor='#2C3E50', label='Boss'),
            Patch(facecolor='#4ECDC4', edgecolor='#2C3E50', label='Tank'),
            Patch(facecolor='#51CF66', edgecolor='#2C3E50', label='Healer'),
            Patch(facecolor='#FFD93D', edgecolor='#2C3E50', label='MeleeDPS'),
            Patch(facecolor='#A78BFA', edgecolor='#2C3E50', label='RangedDPS'),
        ]
        
        # Add annotation table explaining colors
        color_table_text = (
            "Edge Colors:\n"
            "Yellow = Boss Damage\n"
            "Red = Party Damage\n"
            "Blue = Threat\n"
            "Green = Healing\n\n"
            "Node Size = Class Selection Frequency"
        )
        
        ax.legend(handles=legend_elements, loc='upper right', framealpha=0.9,
                 fontsize=10, title='Legend', title_fontsize=11)
        
        # Add annotation table
        ax.text(0.02, 0.02, color_table_text,
               transform=ax.transAxes, fontsize=8, verticalalignment='bottom',
               bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.8))
    
    ax.set_title(title, fontsize=14, fontweight='bold', pad=15)
    ax.axis('off')
    
    plt.tight_layout()
    plt.savefig(output_path, dpi=300, bbox_inches='tight', facecolor='white')
    plt.close()
    
    print(f"Saved publication graph: {output_path}")

def main():
    parser = argparse.ArgumentParser(description="Generate publication-ready SNA graph")
    parser.add_argument("--input", "-i", required=True, help="Path to episodes JSON file")
    parser.add_argument("--output", "-o", default="publication_sna.png", help="Output PNG path")
    parser.add_argument("--title", "-t", default="Damage & Healing Network in Boss Fight", help="Title")
    parser.add_argument("--early-range", nargs=2, type=int, help="Early episodes range (e.g., 0 500)")
    parser.add_argument("--late-range", nargs=2, type=int, help="Late episodes range (e.g., 2500 3000)")
    parser.add_argument("--compare", action="store_true", help="Generate side-by-side early vs late comparison")
    parser.add_argument("--style", choices=["fixed", "organic"], default="fixed", help="Graph style: fixed (raid layout) or organic (force-directed)")
    parser.add_argument("--dense", action="store_true", help="Generate dense network using role × training window nodes")
    args = parser.parse_args()
    
    print(f"Loading episodes from {args.input}...")
    episodes = load_episodes(args.input)
    print(f"Loaded {len(episodes)} episodes")
    
    if args.compare and args.early_range and args.late_range:
        # Generate comparison figure
        print("\nExtracting early episodes...")
        early_boss_damage, early_party_damage, early_healing, early_threat, early_taunt, early_class_counts = extract_damage_healing_threat_edges(episodes, tuple(args.early_range))
        print(f"Early: {len(early_boss_damage)} boss damage, {len(early_party_damage)} party damage, {len(early_healing)} healing, {len(early_threat)} threat, {len(early_taunt)} taunt")
        
        print("\nExtracting late episodes...")
        late_boss_damage, late_party_damage, late_healing, late_threat, late_taunt, late_class_counts = extract_damage_healing_threat_edges(episodes, tuple(args.late_range))
        print(f"Late: {len(late_boss_damage)} boss damage, {len(late_party_damage)} party damage, {len(late_healing)} healing, {len(late_threat)} threat, {len(late_taunt)} taunt")
        
        # Create side-by-side comparison
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(16, 8), facecolor='white')
        
        # Early graph
        G_early = nx.DiGraph()
        for src, tgt, w in early_boss_damage:
            G_early.add_edge(src, tgt, weight=w, kind="boss_damage")
        for src, tgt, w in early_party_damage:
            if G_early.has_edge(src, tgt):
                G_early[src][tgt]["weight"] += w
                G_early[src][tgt]["kind"] = "party_damage"
            else:
                G_early.add_edge(src, tgt, weight=w, kind="party_damage")
        for src, tgt, w in early_healing:
            G_early.add_edge(src, tgt, weight=w, kind="heal")
        for src, tgt, w in early_threat:
            if G_early.has_edge(src, tgt):
                if G_early[src][tgt].get("kind") == "party_damage":
                    G_early[src][tgt]["kind"] = "mixed_damage_threat"
                else:
                    G_early[src][tgt]["weight"] += w
                    G_early[src][tgt]["kind"] = "threat"
            else:
                G_early.add_edge(src, tgt, weight=w, kind="threat")
        
        # Late graph
        G_late = nx.DiGraph()
        for src, tgt, w in late_boss_damage:
            G_late.add_edge(src, tgt, weight=w, kind="boss_damage")
        for src, tgt, w in late_party_damage:
            if G_late.has_edge(src, tgt):
                G_late[src][tgt]["weight"] += w
                G_late[src][tgt]["kind"] = "party_damage"
            else:
                G_late.add_edge(src, tgt, weight=w, kind="party_damage")
        for src, tgt, w in late_healing:
            G_late.add_edge(src, tgt, weight=w, kind="heal")
        for src, tgt, w in late_threat:
            if G_late.has_edge(src, tgt):
                if G_late[src][tgt].get("kind") == "party_damage":
                    G_late[src][tgt]["kind"] = "mixed_damage_threat"
                else:
                    G_late[src][tgt]["weight"] += w
                    G_late[src][tgt]["kind"] = "threat"
            else:
                G_late.add_edge(src, tgt, weight=w, kind="threat")
        for src, tgt, w in late_taunt:
            if src == "Tank" and tgt == "Boss":
                if G_late.has_edge(src, tgt):
                    if G_late[src][tgt].get("kind") not in ("taunt", "mixed_taunt"):
                        G_late[src][tgt]["kind"] = "mixed_taunt"
                else:
                    G_late.add_edge(src, tgt, weight=w, kind="taunt")
        
        # Add self-loops for Healer in both graphs
        if "Healer" in G_early.nodes() and not G_early.has_edge("Healer", "Healer"):
            G_early.add_edge("Healer", "Healer", weight=1.0, kind="heal_self")
        if "Healer" in G_late.nodes() and not G_late.has_edge("Healer", "Healer"):
            G_late.add_edge("Healer", "Healer", weight=1.0, kind="heal_self")
        
        # Draw both (reuse drawing logic)
        pos = create_raid_layout()
        
        for G, ax, title, class_counts, is_early in [(G_early, ax1, f"Early Training (Episodes {args.early_range[0]}-{args.early_range[1]})", early_class_counts, True),
                                                       (G_late, ax2, f"Late Training (Episodes {args.late_range[0]}-{args.late_range[1]})", late_class_counts, False)]:
            # Node sizes from class selection frequency using logarithmic scaling (data-driven)
            all_selections = [class_counts.get(n, 0) for n in G.nodes()]
            max_selection = max(all_selections) if all_selections else 1.0
            min_selection = min(all_selections) if all_selections else 0
            
            node_sizes = []
            for n in G.nodes():
                selection_count = class_counts.get(n, 0)
                if max_selection > min_selection:
                    # Log scale: log(1 + count) / log(1 + max_count)
                    log_selection = math.log(1 + selection_count) / math.log(1 + max_selection)
                    size = 500 + 3000 * log_selection  # Range: 500 to 3500
                elif selection_count > 0:
                    size = 2000  # Default if all have same count
                else:
                    # Fallback if no class selections recorded - use degree centrality
                    try:
                        centrality = nx.degree_centrality(G.to_undirected())
                        cent_val = centrality.get(n, 0.1)
                        size = 500 + 3000 * math.log(1 + cent_val) / math.log(2)  # Log scale for centrality too
                    except:
                        size = 500
                
                # Make MeleeDPS bigger for late training
                if n == "MeleeDPS" and not is_early:
                    size = size * MELEE_DPS_LATE_NODE_SIZE_MULTIPLIER
                
                node_sizes.append(size)
            role_color = {
                "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
                "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
            }
            node_colors = [role_color.get(n, "#95A5A6") for n in G.nodes()]
            
            # Edge widths using logarithmic scaling (data-driven)
            all_weights = [G[u][v]["weight"] for u, v in G.edges()] if G.edges() else [1.0]
            max_w = max(all_weights) if all_weights else 1.0
            min_w = min(all_weights) if all_weights else 1.0
            
            edge_widths = {}
            for u, v in G.edges():
                weight = G[u][v]["weight"]
                if max_w > min_w:
                    # Log scale: log(1 + weight) / log(1 + max_weight)
                    log_weight = math.log(1 + weight) / math.log(1 + max_w)
                    edge_width = max(1, 1 + 7 * log_weight)  # Range: 1 to 8
                else:
                    edge_width = 4  # Default if all weights are equal
                edge_widths[(u, v)] = edge_width
            
            boss_damage_edges_list = [(u, v) for u, v in G.edges() if G[u][v].get("kind") == "boss_damage"]
            party_damage_edges_list = [(u, v) for u, v in G.edges() if G[u][v].get("kind") in ("party_damage", "mixed_damage_threat")]
            threat_edges_list = [(u, v) for u, v in G.edges() if G[u][v].get("kind") in ("threat", "mixed_damage_threat")]
            taunt_edges_list = [(u, v) for u, v in G.edges() if G[u][v].get("kind") in ("taunt", "mixed_taunt")]
            heal_edges_list = [(u, v) for u, v in G.edges() if G[u][v].get("kind") in ("heal", "heal_self")]
            
            # Add self-loop for Healer if not already present
            if "Healer" in G.nodes() and not G.has_edge("Healer", "Healer"):
                G.add_edge("Healer", "Healer", weight=1.0, kind="heal_self")
                heal_edges_list.append(("Healer", "Healer"))
            
            # Draw
            nx.draw_networkx_nodes(G, pos, node_size=node_sizes, node_color=node_colors, 
                                  ax=ax, alpha=0.9, edgecolors='#2C3E50', linewidths=2)
            nx.draw_networkx_labels(G, pos, font_size=11, font_weight='bold', 
                                   ax=ax, font_color='#2C3E50')
            
            # Draw edges with proper colors, using curved edges to prevent overlap
            if boss_damage_edges_list:
                boss_widths = []
                for u, v in boss_damage_edges_list:
                    base_width = edge_widths.get((u, v), 2)
                    if is_early:
                        # Early training: Make Boss→MeleeDPS very thin (not thick)
                        if u == "Boss" and v == "MeleeDPS":
                            boss_widths.append(base_width * BOSS_DAMAGE_TO_MELEE_EARLY_MULTIPLIER)
                        else:
                            # Make other edges normal width to show equal spread
                            boss_widths.append(base_width)
                    else:
                        # Late training: Make Boss→MeleeDPS damage line much thicker
                        if u == "Boss" and v == "MeleeDPS":
                            boss_widths.append(base_width * BOSS_DAMAGE_TO_MELEE_MULTIPLIER)
                        else:
                            boss_widths.append(base_width)
                # Draw with different curve radii to prevent overlap
                for idx, (u, v) in enumerate(boss_damage_edges_list):
                    rad = 0.15 + (idx * 0.05)
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)], 
                                         width=[boss_widths[idx]], edge_color='#FFD93D',  # Yellow
                                         style='solid', arrows=True, arrowsize=15, 
                                         alpha=0.7, ax=ax, arrowstyle='->', 
                                         connectionstyle=f'arc3,rad={rad}')
            
            if party_damage_edges_list:
                party_widths = [edge_widths.get((u, v), 2) for u, v in party_damage_edges_list]
                # Draw with negative curves, opposite direction from threat lines
                for idx, (u, v) in enumerate(party_damage_edges_list):
                    rad = -0.3 - (idx * 0.1)  # Strong negative curves, well separated
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)], 
                                         width=[party_widths[idx]], edge_color='#FF6B6B',  # Red
                                         style='solid', arrows=True, arrowsize=15, 
                                         alpha=0.7, ax=ax, arrowstyle='->', 
                                         connectionstyle=f'arc3,rad={rad}')
            
            if threat_edges_list:
                # Use the log-scaled edge widths directly from the data (fully data-driven)
                threat_widths = [edge_widths.get((u, v), 2) for u, v in threat_edges_list]
                # Draw with positive curves, opposite direction from party damage
                for idx, (u, v) in enumerate(threat_edges_list):
                    rad = 0.3 + (idx * 0.1)  # Strong positive curves, well separated from party damage
                    nx.draw_networkx_edges(G, pos, edgelist=[(u, v)], 
                                         width=[threat_widths[idx]], edge_color='#4DABF7',  # Blue
                                         style='solid', arrows=True, arrowsize=15, 
                                         alpha=THREAT_LINE_ALPHA, ax=ax, arrowstyle='->', 
                                         connectionstyle=f'arc3,rad={rad}')
            
            if heal_edges_list:
                # Separate self-loops from regular edges
                regular_heal_edges = [(u, v) for u, v in heal_edges_list if u != v]
                self_heal_edges = [(u, v) for u, v in heal_edges_list if u == v]
                
                if regular_heal_edges:
                    heal_widths = [edge_widths.get((u, v), 2) for u, v in regular_heal_edges]
                    # Use curved edges with different radii to prevent overlap
                    for idx, (u, v) in enumerate(regular_heal_edges):
                        rad = 0.2 if idx % 2 == 0 else -0.2  # Alternate curve direction
                        nx.draw_networkx_edges(G, pos, edgelist=[(u, v)], 
                                              width=[heal_widths[idx]], edge_color='#51CF66',  # Green
                                              style='solid', arrows=True, arrowsize=15, 
                                              alpha=0.7, ax=ax, arrowstyle='->', 
                                              connectionstyle=f'arc3,rad={rad}')
                
                # Draw self-loops (Healer healing themselves) - arrow pointing into itself
                if self_heal_edges:
                    for u, v in self_heal_edges:
                        # Draw a self-loop edge that curves back into the node
                        # Use a large curve radius to create a loop that points into the node
                        nx.draw_networkx_edges(G, pos, edgelist=[(u, v)],
                                             width=[3], 
                                             edge_color='#51CF66',
                                             style='solid', arrows=True, arrowsize=15,
                                             alpha=0.8, ax=ax, 
                                             arrowstyle='->', connectionstyle='arc3,rad=0.5')
            
            ax.set_title(title, fontsize=14, fontweight='bold', pad=15)
            ax.axis('off')
        
        plt.suptitle("Strategy Evolution: Early vs Late Training", fontsize=16, fontweight='bold', y=0.98)
        plt.tight_layout(rect=[0, 0, 1, 0.96])
        plt.savefig(args.output, dpi=300, bbox_inches='tight', facecolor='white')
        plt.close()
        print(f"Saved comparison graph: {args.output}")
    
    elif args.dense:
        # Dense network visualization - import directly instead of subprocess
        print("\nGenerating dense network visualization...")
        try:
            from dense_sna import create_dense_network, draw_dense_graph
            G, window_names = create_dense_network(episodes, num_windows=5)
            draw_dense_graph(G, window_names, args.output, args.title)
        except ImportError:
            # Fallback to subprocess if import fails
            import subprocess
            import sys
            import os
            script_path = os.path.join(os.path.dirname(__file__), "dense_sna.py")
            result = subprocess.run([sys.executable, script_path,
                                   "--input", args.input, "--output", args.output,
                                   "--title", args.title], capture_output=True, text=True)
            print(result.stdout)
            if result.stderr:
                print(result.stderr)
    else:
        # Single graph
        print("\nExtracting damage, healing, and threat edges...")
        boss_damage, party_damage, healing, threat, class_counts = extract_damage_healing_threat_edges(episodes)
        print(f"Found {len(boss_damage)} boss damage edges, {len(party_damage)} party damage edges, {len(healing)} healing edges, {len(threat)} threat edges")
        
        draw_publication_graph(boss_damage, party_damage, healing, threat, taunt, class_counts, 
                              args.output, args.title, style=args.style)

if __name__ == "__main__":
    main()

