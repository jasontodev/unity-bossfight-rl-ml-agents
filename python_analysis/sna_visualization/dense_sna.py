"""
Generate dense SNA visualization using role × training window nodes.
Creates nodes like Tank_early, Tank_mid, Tank_late, etc.
"""

import argparse
import json
from collections import defaultdict
from itertools import cycle

import matplotlib.pyplot as plt
import networkx as nx

try:
    from networkx.algorithms import community
    HAS_COMMUNITY = True
except ImportError:
    HAS_COMMUNITY = False

def load_episodes(path: str):
    """Load episodes from JSON file"""
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    if "episodes" in data:
        return data["episodes"]
    elif isinstance(data, list):
        return data
    else:
        return [data]

def create_dense_network(episodes, num_windows=5):
    """
    Create dense network with nodes like Role_Window.
    Returns: NetworkX graph with role×window nodes
    """
    G = nx.DiGraph()
    
    # Define training windows using indices (safer than episode numbers)
    total_episodes = len(episodes)
    window_size = max(1, total_episodes // num_windows)
    
    windows = []
    for i in range(num_windows):
        start_idx = i * window_size
        end_idx = total_episodes - 1 if i == num_windows - 1 else min(total_episodes, (i + 1) * window_size) - 1
        windows.append((start_idx, end_idx))
    
    window_names = ["early", "mid_early", "mid", "mid_late", "late"][:num_windows]
    
    # Get agent roles
    agent_classes = {}
    if episodes and "agentIds" in episodes[0] and "agentClassValues" in episodes[0]:
        for a_id, a_cls in zip(episodes[0]["agentIds"], episodes[0]["agentClassValues"]):
            agent_classes[a_id] = a_cls
    
    boss_ids = {aid for aid, cls in agent_classes.items() if str(cls).lower() == "boss"}
    
    # Map to roles
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
    
    # Aggregate interactions per window
    # Structure: (role1, window1, role2, window2) -> {"damage": float, "heal": float}
    window_interactions = defaultdict(lambda: {"damage": 0.0, "heal": 0.0})
    
    for window_idx, (start_idx, end_idx) in enumerate(windows):
        window_name = window_names[window_idx]
        
        for ep_idx, episode in enumerate(episodes):
            if not (start_idx <= ep_idx <= end_idx):
                continue
            
            actions = episode.get("actions", [])
            party_attack_counts = defaultdict(int)
            
            for act in actions:
                branch = act.get("branch")
                val = act.get("value", 0)
                agent = act.get("agentId", "unknown")
                target = act.get("targetId")  # Use explicit target if available
                agent_role = role_map.get(agent, agent)
                
                if branch == "attack" and val == 1:
                    if target:
                        # Use explicit target
                        target_role = role_map.get(target, target)
                        damage = 10.0 if agent_role == "MeleeDPS" else (5.0 if agent_role == "RangedDPS" else (100.0 if agent_role == "Boss" else 2.0))
                        window_interactions[(agent_role, window_name, target_role, window_name)]["damage"] += damage
                        if agent_role != "Boss":
                            party_attack_counts[agent_role] += 1
                    else:
                        # Fallback to inference if no explicit target
                        agent_lower = agent.lower()
                        is_boss = agent in boss_ids or agent_lower == "boss" or "boss" in agent_lower
                        
                        if not is_boss:
                            # Party member attacks boss
                            damage = 10.0 if agent_role == "MeleeDPS" else (5.0 if agent_role == "RangedDPS" else 2.0)
                            window_interactions[(agent_role, window_name, "Boss", window_name)]["damage"] += damage
                            party_attack_counts[agent_role] += 1
                
                elif branch == "heal" and val == 1:
                    if target:
                        # Use explicit target
                        target_role = role_map.get(target, target)
                        window_interactions[(agent_role, window_name, target_role, window_name)]["heal"] += 10.0
                    else:
                        # Fallback to inference
                        if agent_role == "Healer":
                            window_interactions[("Healer", window_name, "Tank", window_name)]["heal"] += 10.0 * 0.5
                            window_interactions[("Healer", window_name, "MeleeDPS", window_name)]["heal"] += 10.0 * 0.3
                            window_interactions[("Healer", window_name, "RangedDPS", window_name)]["heal"] += 10.0 * 0.2
        
        # Infer boss attacks based on party activity (only if we don't have explicit boss attack targets)
        # Skip if we already have explicit Boss→Party edges from targetId
        has_explicit_boss_attacks = any(
            (r1 == "Boss" and r2 != "Boss") 
            for (r1, w1, r2, w2), data in window_interactions.items() 
            if w1 == window_name and w2 == window_name and data.get("damage", 0) > 0
        )
        
        if not has_explicit_boss_attacks:
            total_party_attacks = sum(party_attack_counts.values())
            if total_party_attacks > 0:
                for role, count in party_attack_counts.items():
                    if role != "Boss":
                        if role == "Tank":
                            boss_damage = 100.0 * 0.6 * (count / total_party_attacks)
                        else:
                            boss_damage = 100.0 * 0.4 * (count / total_party_attacks) / max(1, len(party_attack_counts) - 1)
                        window_interactions[("Boss", window_name, role, window_name)]["damage"] += boss_damage
    
    # Create nodes and edges
    all_role_window_pairs = set()
    for (r1, w1, r2, w2), data in window_interactions.items():
        all_role_window_pairs.add((r1, w1))
        all_role_window_pairs.add((r2, w2))
    
    # Extract unique roles
    all_roles = set()
    for role, window in all_role_window_pairs:
        all_roles.add(role)
    
    # Add nodes for all role×window combinations (even if no interactions)
    for role in all_roles:
        for window_name in window_names:
            node_name = f"{role}_{window_name}"
            if node_name not in G.nodes():
                G.add_node(node_name, role=role, window=window_name)
    
    # Add edges from window interactions
    for (r1, w1, r2, w2), data in window_interactions.items():
        src = f"{r1}_{w1}"
        tgt = f"{r2}_{w2}"
        
        total_weight = data.get("damage", 0) + data.get("heal", 0)
        if total_weight > 0:
            if G.has_edge(src, tgt):
                G[src][tgt]["weight"] += total_weight
            else:
                G.add_edge(src, tgt, weight=total_weight,
                          kind="damage" if data.get("damage", 0) > data.get("heal", 0) else "heal")
    
    # Add cross-window edges to connect same role across windows (evolution/temporal continuity)
    # This connects all components and creates a more organic, hairball-like structure
    # all_roles is already defined above from window_interactions
    
    # Connect same role across consecutive windows
    for role in all_roles:
        for i in range(len(window_names) - 1):
            w1 = window_names[i]
            w2 = window_names[i + 1]
            src = f"{role}_{w1}"
            tgt = f"{role}_{w2}"
            
            # Ensure nodes exist
            if src not in G.nodes():
                G.add_node(src, role=role, window=w1)
            if tgt not in G.nodes():
                G.add_node(tgt, role=role, window=w2)
            
            # Add evolution edge (small weight, represents temporal continuity)
            if G.has_edge(src, tgt):
                G[src][tgt]["weight"] += 1.0
            else:
                G.add_edge(src, tgt, weight=1.0, kind="evolution")
    
    return G, window_names

def draw_dense_graph(G, window_names, output_path, title):
    """Draw dense network graph with community detection"""
    
    # Convert to undirected for community detection
    G_undir = G.to_undirected()
    
    # Community detection
    if HAS_COMMUNITY:
        try:
            communities = list(community.greedy_modularity_communities(G_undir))
            community_map = {}
            for i, comm in enumerate(communities):
                for node in comm:
                    community_map[node] = i
            
            color_cycle = cycle(["#FF6B6B", "#4ECDC4", "#556270", "#C7F464", "#C44D58", "#95A5A6"])
            comm_color = {i: c for i, c in zip(range(len(communities)), color_cycle)}
            node_colors = [comm_color.get(community_map.get(n, 0), "#95A5A6") for n in G.nodes()]
        except:
            # Fallback: color by role
            role_colors = {
                "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
                "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
            }
            node_colors = [role_colors.get(G.nodes[n].get("role", "Unknown"), "#95A5A6") for n in G.nodes()]
    else:
        role_colors = {
            "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
            "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
        }
        node_colors = [role_colors.get(G.nodes[n].get("role", "Unknown"), "#95A5A6") for n in G.nodes()]
    
    # Node sizes from degree centrality
    deg_cent = nx.degree_centrality(G_undir)
    node_sizes = [800 + 3000 * deg_cent.get(n, 0.1) for n in G.nodes()]
    
    # Edge widths from weights (create map for efficient lookup)
    edges_list = list(G.edges())
    weights = [G[u][v]["weight"] for u, v in edges_list]
    max_w = max(weights) if weights else 1.0
    edge_width_map = {
        e: 0.5 + 4.5 * (G[e[0]][e[1]]["weight"] / max_w)
        for e in edges_list
    }
    
    # Force-directed layout
    pos = nx.spring_layout(G, k=1.2, iterations=200, seed=42)
    
    # Create figure
    fig, ax = plt.subplots(figsize=(12, 10), facecolor='white')
    
    # Draw nodes
    nx.draw_networkx_nodes(G, pos, node_size=node_sizes, node_color=node_colors,
                          ax=ax, alpha=0.9, linewidths=1.5, edgecolors="#333333")
    
    # Draw edges (all edges use the same style for dense graph)
    edge_widths = [edge_width_map.get((u, v), 2.0) for u, v in edges_list]
    nx.draw_networkx_edges(G, pos, width=edge_widths, alpha=0.4,
                          arrows=True, arrowsize=10, ax=ax, arrowstyle="-|>",
                          edge_color='#666666')
    
    # Draw labels (abbreviated)
    labels = {n: n.replace("_", "\n") for n in G.nodes()}
    nx.draw_networkx_labels(G, pos, labels=labels, font_size=7,
                           font_weight="bold", ax=ax)
    
    ax.set_title(title, fontsize=16, fontweight="bold", pad=20)
    ax.axis("off")
    
    # Add legend
    from matplotlib.patches import Patch
    role_colors = {
        "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
        "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
    }
    legend_elements = [Patch(facecolor=color, edgecolor='#333333', label=role)
                      for role, color in role_colors.items()]
    legend_elements.append(plt.Line2D([0], [0], color='#666666', linewidth=2, label='Interaction'))
    
    ax.legend(handles=legend_elements, loc='upper right', framealpha=0.9,
             fontsize=9, title='Roles', title_fontsize=10)
    
    # Add annotation
    ax.text(0.02, 0.02, f'Nodes: {len(G.nodes())} | Edges: {len(G.edges())}\n'
                       f'Edge width ∝ interaction strength\n'
                       f'Node size ∝ degree centrality\n'
                       f'Windows: {", ".join(window_names)}',
           transform=ax.transAxes, fontsize=9, verticalalignment='bottom',
           bbox=dict(boxstyle='round', facecolor='wheat', alpha=0.5))
    
    plt.tight_layout()
    plt.savefig(output_path, dpi=300, bbox_inches='tight', facecolor='white')
    plt.close()
    
    print(f"Saved dense network graph: {output_path}")
    print(f"  Nodes: {len(G.nodes())}, Edges: {len(G.edges())}")

def main():
    parser = argparse.ArgumentParser(description="Generate dense SNA visualization")
    parser.add_argument("--input", "-i", required=True, help="Path to episodes JSON file")
    parser.add_argument("--output", "-o", default="dense_sna.png", help="Output PNG path")
    parser.add_argument("--title", "-t", default="Episode-level Damage Network Over Training", help="Title")
    parser.add_argument("--windows", "-w", type=int, default=5, help="Number of training windows")
    args = parser.parse_args()
    
    print(f"Loading episodes from {args.input}...")
    episodes = load_episodes(args.input)
    print(f"Loaded {len(episodes)} episodes")
    
    print(f"Creating dense network with {args.windows} windows...")
    G, window_names = create_dense_network(episodes, num_windows=args.windows)
    
    print(f"Generating visualization...")
    draw_dense_graph(G, window_names, args.output, args.title)

if __name__ == "__main__":
    main()

