"""
Interactive HTML SNA visualization with draggable nodes and adjustable edge thickness.
Uses vis.js for full interactivity.
"""

import argparse
import json
import math
from collections import defaultdict
from typing import Dict, List, Tuple

import networkx as nx

# Configuration
TAUNT_COLOR = '#A78BFA'  # Purple color for taunt
BOSS_DAMAGE_TO_MELEE_MULTIPLIER = 5.0  # Make boss→MeleeDPS damage line much thicker (late training)
BOSS_DAMAGE_TO_MELEE_EARLY_MULTIPLIER = 0.3  # Make boss→MeleeDPS damage line very thin (early training)
MELEE_DPS_LATE_NODE_SIZE_MULTIPLIER = 1.5  # Make MeleeDPS node bigger in late training

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

def extract_damage_healing_threat_edges(episodes: List[dict], episode_range: Tuple[int, int] = None) -> Tuple[List[Tuple], List[Tuple], List[Tuple], List[Tuple], List[Tuple], Dict[str, int]]:
    """Extract damage, healing, threat, and taunt edges from episodes."""
    boss_damage_edges_dict = defaultdict(float)
    party_damage_edges_dict = defaultdict(float)
    healing_edges_dict = defaultdict(float)
    threat_edges_dict = defaultdict(float)
    taunt_edges_dict = defaultdict(float)
    class_selection_counts = defaultdict(int)
    
    role_map = {
        "Boss": "Boss",
        "Party Member 1": "Tank",
        "Party Member 2": "Healer",
        "Party Member 3": "MeleeDPS",
        "Party Member 4": "RangedDPS",
    }
    
    seen_selections = set()
    
    for episode in episodes:
        episode_num = episode.get("episode", 0)
        if episode_range:
            start, end = episode_range
            if not (start <= episode_num <= end):
                continue
        
        actions = episode.get("actions", [])
        has_explicit_targets = any("targetId" in act for act in actions)
        
        if has_explicit_targets:
            for act in actions:
                branch = act.get("branch")
                val = act.get("value", 0)
                agent = act.get("agentId", "unknown")
                target = act.get("targetId")
                
                if val == 1 and target:
                    agent_role = role_map.get(agent, agent)
                    target_role = role_map.get(target, target)
                    
                    if branch == "attack":
                        if agent_role == "Boss" and target_role != "Boss":
                            boss_damage_edges_dict[(agent_role, target_role)] += 1.0
                        elif agent_role != "Boss" and target_role == "Boss":
                            party_damage_edges_dict[(agent_role, target_role)] += 1.0
                            threat_edges_dict[(agent_role, target_role)] += 1.0
                    elif branch == "heal":
                        if agent_role == "Healer" and target_role != "Boss":
                            healing_edges_dict[(agent_role, target_role)] += 1.0
                    elif branch == "threat_boost" or branch == "taunt":
                        if agent_role == "Tank" and target_role == "Boss":
                            taunt_edges_dict[(agent_role, target_role)] += 1.0
                
                if branch == "class_selection" and val >= 0:
                    agent_role = role_map.get(agent, agent)
                    episode_key = f"{episode_num}_{agent}"
                    if episode_key not in seen_selections:
                        seen_selections.add(episode_key)
                        class_selection_counts[agent_role] += 1
    
    # Infer taunt from Tank attacks if no explicit taunt actions
    if not taunt_edges_dict:
        for (src, tgt), weight in party_damage_edges_dict.items():
            if src == "Tank" and tgt == "Boss":
                taunt_edges_dict[(src, tgt)] += weight * 0.5
    
    min_weight = 1e-3
    boss_damage_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in boss_damage_edges_dict.items() if weight > 0]
    party_damage_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in party_damage_edges_dict.items() if weight > 0]
    healing_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in healing_edges_dict.items() if weight > 0]
    threat_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in threat_edges_dict.items() if weight > 0]
    taunt_edges = [(src, tgt, max(weight, min_weight)) for (src, tgt), weight in taunt_edges_dict.items() if weight > 0]
    
    return boss_damage_edges, party_damage_edges, healing_edges, threat_edges, taunt_edges, class_selection_counts

def create_raid_layout():
    """Create fixed raid-style layout positions"""
    return {
        "Boss": (0.0, 1.0),
        "Tank": (0.0, 0.0),
        "MeleeDPS": (0.4, -0.4),
        "Healer": (-0.4, -0.4),
        "RangedDPS": (0.8, -0.2),
    }

def create_interactive_html(boss_damage_edges: List[Tuple], party_damage_edges: List[Tuple],
                            healing_edges: List[Tuple], threat_edges: List[Tuple], taunt_edges: List[Tuple],
                            class_selection_counts: Dict[str, int],
                            output_path: str, title: str, is_early: bool = False):
    """Create interactive HTML with vis.js for draggable nodes and adjustable edge thickness"""
    
    # Build graph
    G = nx.DiGraph()
    all_nodes = set()
    
    for src, tgt, w in boss_damage_edges + party_damage_edges + healing_edges + threat_edges + taunt_edges:
        all_nodes.add(src)
        all_nodes.add(tgt)
        G.add_edge(src, tgt, weight=w)
    
    # Ensure all roles exist
    for role in ["Boss", "Tank", "Healer", "MeleeDPS", "RangedDPS"]:
        if role not in G.nodes():
            G.add_node(role)
    
    # Get positions
    pos = create_raid_layout()
    
    # Node sizes from class selection frequency (log scale)
    all_selections = [class_selection_counts.get(n, 0) for n in G.nodes()]
    max_selection = max(all_selections) if all_selections else 1.0
    min_selection = min(all_selections) if all_selections else 0
    
    role_color = {
        "Boss": "#FF6B6B", "Tank": "#4ECDC4", "Healer": "#51CF66",
        "MeleeDPS": "#FFD93D", "RangedDPS": "#A78BFA"
    }
    
    # Prepare nodes data
    nodes_data = []
    for n in G.nodes():
        selection_count = class_selection_counts.get(n, 0)
        if max_selection > min_selection:
            log_selection = math.log(1 + selection_count) / math.log(1 + max_selection)
            size = 20 + 40 * log_selection
        elif selection_count > 0:
            size = 30
        else:
            try:
                centrality = nx.degree_centrality(G.to_undirected())
                cent_val = centrality.get(n, 0.1)
                size = 20 + 40 * math.log(1 + cent_val) / math.log(2)
            except:
                size = 20
        
        # Make MeleeDPS bigger for late training
        if n == "MeleeDPS" and not is_early:
            size = size * MELEE_DPS_LATE_NODE_SIZE_MULTIPLIER
        
        nodes_data.append({
            "id": n,
            "label": n,
            "x": pos[n][0] * 200,  # Scale for vis.js
            "y": pos[n][1] * 200,
            "color": role_color.get(n, "#95A5A6"),
            "size": size,
            "fixed": False  # Not fixed - can be dragged independently
        })
    
    # Edge widths (log scale)
    all_weights = [G[u][v]["weight"] for u, v in G.edges()] if G.edges() else [1.0]
    max_w = max(all_weights) if all_weights else 1.0
    min_w = min(all_weights) if all_weights else 1.0
    
    edge_widths = {}
    for u, v in G.edges():
        weight = G[u][v]["weight"]
        if max_w > min_w:
            log_weight = math.log(1 + weight) / math.log(1 + max_w)
            edge_width = max(1, 1 + 7 * log_weight)
        else:
            edge_width = 4
        edge_widths[(u, v)] = edge_width
    
    # Prepare edges data
    edges_data = []
    edge_types = {}
    
    # Boss damage (yellow)
    for u, v, w in boss_damage_edges:
        base_width = edge_widths.get((u, v), 2)
        if is_early and u == "Boss" and v == "MeleeDPS":
            width = base_width * BOSS_DAMAGE_TO_MELEE_EARLY_MULTIPLIER
        elif not is_early and u == "Boss" and v == "MeleeDPS":
            width = base_width * BOSS_DAMAGE_TO_MELEE_MULTIPLIER
        else:
            width = base_width
        edges_data.append({
            "from": u,
            "to": v,
            "width": width,
            "color": {"color": "#FFD93D"},
            "arrows": "to",
            "id": f"boss_{u}_{v}",
            "title": f"Boss Damage: {w:.1f}",
            "smooth": {"type": "continuous", "roundness": 0.5},
            "sourceAgent": "Boss",
            "targetAgent": v,
            "edgeType": "damage"
        })
        edge_types[f"boss_{u}_{v}"] = "boss"
    
    # Party damage (red)
    for u, v, w in party_damage_edges:
        width = edge_widths.get((u, v), 2)
        edges_data.append({
            "from": u,
            "to": v,
            "width": width,
            "color": {"color": "#FF6B6B"},
            "arrows": "to",
            "id": f"party_{u}_{v}",
            "title": f"Party Damage: {w:.1f}",
            "smooth": {"type": "continuous", "roundness": 0.5},
            "sourceAgent": u,
            "edgeType": "damage"
        })
        edge_types[f"party_{u}_{v}"] = "party"
    
    # Threat (blue)
    for u, v, w in threat_edges:
        width = edge_widths.get((u, v), 2)
        edges_data.append({
            "from": u,
            "to": v,
            "width": width,
            "color": {"color": "#4DABF7"},
            "arrows": "to",
            "id": f"threat_{u}_{v}",
            "title": f"Threat: {w:.1f}",
            "smooth": {"type": "continuous", "roundness": 0.5},
            "sourceAgent": u,
            "edgeType": "threat"
        })
        edge_types[f"threat_{u}_{v}"] = "threat"
    
    # Taunt (purple)
    for u, v, w in taunt_edges:
        width = edge_widths.get((u, v), 2) * 2.0
        edges_data.append({
            "from": u,
            "to": v,
            "width": width,
            "color": {"color": TAUNT_COLOR},
            "arrows": "to",
            "id": f"taunt_{u}_{v}",
            "title": f"Taunt: {w:.1f}",
            "smooth": {"type": "continuous", "roundness": 0.5},
            "sourceAgent": u,
            "edgeType": "taunt"
        })
        edge_types[f"taunt_{u}_{v}"] = "taunt"
    
    # Healing (green)
    for u, v, w in healing_edges:
        width = edge_widths.get((u, v), 2)
        edges_data.append({
            "from": u,
            "to": v,
            "width": width,
            "color": {"color": "#51CF66"},
            "arrows": "to",
            "dashes": True,
            "id": f"heal_{u}_{v}",
            "title": f"Healing: {w:.1f}",
            "smooth": {"type": "continuous", "roundness": 0.5},
            "sourceAgent": u,
            "edgeType": "heal"
        })
        edge_types[f"heal_{u}_{v}"] = "heal"
    
    # Add self-healing loop for Healer if not already present
    if "Healer" in G.nodes():
        # Check if self-heal edge already exists
        has_self_heal = any((u == "Healer" and v == "Healer") for u, v, _ in healing_edges)
        if not has_self_heal:
            # Add a self-loop for healer self-healing
            self_heal_width = 2.0  # Default width
            edges_data.append({
                "from": "Healer",
                "to": "Healer",
                "width": self_heal_width,
                "color": {"color": "#51CF66"},
                "arrows": "to",
                "dashes": True,
                "id": "heal_Healer_Healer",
                "title": "Healer Self-Healing",
                "smooth": {"type": "continuous", "roundness": 0.5},
                "sourceAgent": "Healer",
                "edgeType": "heal"
            })
            edge_types["heal_Healer_Healer"] = "heal"
    
    # Generate HTML with vis.js
    html_content = f"""<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <script type="text/javascript" src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f5f5f5;
            display: flex;
            height: 100vh;
            overflow: hidden;
        }}
        .main-container {{
            display: flex;
            width: 100%;
            height: 100vh;
        }}
        .controls-panel {{
            width: 350px;
            min-width: 350px;
            background: #f5f5f5;
            padding: 15px;
            overflow-y: auto;
            border-right: 2px solid #ddd;
        }}
        .canvas-panel {{
            flex: 1;
            display: flex;
            flex-direction: column;
            padding: 15px;
            background: #f5f5f5;
            height: 100vh;
            overflow: hidden;
        }}
        #mynetwork {{
            width: 100%;
            height: 100%;
            min-height: 600px;
            border: 1px solid #ddd;
            background-color: white;
            border-radius: 8px;
        }}
        .controls {{
            background: white;
            padding: 15px;
            margin-bottom: 15px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .agent-control-box {{
            width: 100%;
            padding: 10px;
            margin-bottom: 15px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .control-group {{
            margin: 8px 0;
        }}
        .control-group label {{
            display: inline-block;
            width: 120px;
            font-weight: bold;
            font-size: 12px;
        }}
        .control-group input[type="range"] {{
            width: 150px;
            margin: 0 5px;
        }}
        .control-group span {{
            display: inline-block;
            width: 40px;
            text-align: center;
            font-size: 11px;
        }}
        .control-group select {{
            width: 180px;
            font-size: 11px;
        }}
        h1 {{
            color: #333;
            margin-top: 0;
            margin-bottom: 15px;
            font-size: 18px;
        }}
        h3 {{
            margin-top: 0;
            margin-bottom: 10px;
            font-size: 14px;
        }}
        .legend {{
            background: white;
            padding: 15px;
            margin-top: 15px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .legend-item {{
            margin: 5px 0;
            font-size: 11px;
        }}
        .legend-color {{
            display: inline-block;
            width: 20px;
            height: 3px;
            margin-right: 10px;
            vertical-align: middle;
        }}
        button {{
            padding: 8px 15px;
            margin: 5px;
            font-size: 12px;
            border-radius: 4px;
            border: 1px solid #ddd;
            background: white;
            cursor: pointer;
        }}
        button:hover {{
            background: #f0f0f0;
        }}
    </style>
</head>
<body>
    <div class="main-container">
        <div class="controls-panel">
            <h1>{title}</h1>
            
            <!-- Boss Controls -->
            <div class="agent-control-box" style="border: 2px solid #FF6B6B; background: #fff5f5;">
                <h3 style="color: #FF6B6B; margin-top: 0;">Boss</h3>
                <div class="control-group">
                    <label>Node Size:</label>
                    <input type="range" id="nodeSize_Boss" min="0.5" max="3" value="1" step="0.1">
                    <span id="nodeSizeValue_Boss">1.0</span>
                </div>
                <div class="control-group">
                    <label>Damage to Tank:</label>
                    <input type="range" id="damageThickness_Boss_Tank" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Boss_Tank">2</span>
                </div>
                <div class="control-group">
                    <label>Damage to Healer:</label>
                    <input type="range" id="damageThickness_Boss_Healer" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Boss_Healer">2</span>
                </div>
                <div class="control-group">
                    <label>Damage to MeleeDPS:</label>
                    <input type="range" id="damageThickness_Boss_MeleeDPS" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Boss_MeleeDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Damage to RangedDPS:</label>
                    <input type="range" id="damageThickness_Boss_RangedDPS" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Boss_RangedDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Edge Curvature:</label>
                    <input type="range" id="curvature_Boss" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Boss">0.5</span>
                </div>
                <div class="control-group">
                    <label>Edge Type:</label>
                    <select id="edgeType_Boss">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
            </div>
            
            <!-- Tank Controls -->
            <div class="agent-control-box" style="border: 2px solid #4ECDC4; background: #f0fffe;">
                <h3 style="color: #4ECDC4; margin-top: 0;">Tank</h3>
                <div class="control-group">
                    <label>Node Size:</label>
                    <input type="range" id="nodeSize_Tank" min="0.5" max="3" value="1" step="0.1">
                    <span id="nodeSizeValue_Tank">1.0</span>
                </div>
                <div class="control-group">
                    <label>Damage Thickness:</label>
                    <input type="range" id="damageThickness_Tank" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Tank">2</span>
                </div>
                <div class="control-group">
                    <label>Damage Curvature:</label>
                    <input type="range" id="curvature_Tank_damage" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Tank_damage">0.5</span>
                </div>
                <div class="control-group">
                    <label>Damage Type:</label>
                    <select id="edgeType_Tank_damage">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Threat Thickness:</label>
                    <input type="range" id="threatThickness_Tank" min="1" max="20" value="2" step="0.5">
                    <span id="threatValue_Tank">2</span>
                </div>
                <div class="control-group">
                    <label>Threat Curvature:</label>
                    <input type="range" id="curvature_Tank_threat" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Tank_threat">0.5</span>
                </div>
                <div class="control-group">
                    <label>Threat Type:</label>
                    <select id="edgeType_Tank_threat">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Taunt Thickness:</label>
                    <input type="range" id="tauntThickness_Tank" min="1" max="20" value="4" step="0.5">
                    <span id="tauntValue_Tank">4</span>
                </div>
                <div class="control-group">
                    <label>Taunt Curvature:</label>
                    <input type="range" id="curvature_Tank_taunt" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Tank_taunt">0.5</span>
                </div>
                <div class="control-group">
                    <label>Taunt Type:</label>
                    <select id="edgeType_Tank_taunt">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
            </div>
            
            <!-- Healer Controls -->
            <div class="agent-control-box" style="border: 2px solid #51CF66; background: #f0fff4;">
                <h3 style="color: #51CF66; margin-top: 0;">Healer</h3>
                <div class="control-group">
                    <label>Node Size:</label>
                    <input type="range" id="nodeSize_Healer" min="0.5" max="3" value="1" step="0.1">
                    <span id="nodeSizeValue_Healer">1.0</span>
                </div>
                <div class="control-group">
                    <label>Damage Thickness:</label>
                    <input type="range" id="damageThickness_Healer" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_Healer">2</span>
                </div>
                <div class="control-group">
                    <label>Damage Curvature:</label>
                    <input type="range" id="curvature_Healer_damage" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Healer_damage">0.5</span>
                </div>
                <div class="control-group">
                    <label>Damage Type:</label>
                    <select id="edgeType_Healer_damage">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Healing Thickness:</label>
                    <input type="range" id="healThickness_Healer" min="1" max="20" value="2" step="0.5">
                    <span id="healValue_Healer">2</span>
                </div>
                <div class="control-group">
                    <label>Healing Curvature:</label>
                    <input type="range" id="curvature_Healer_heal" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Healer_heal">0.5</span>
                </div>
                <div class="control-group">
                    <label>Healing Type:</label>
                    <select id="edgeType_Healer_heal">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Threat Thickness:</label>
                    <input type="range" id="threatThickness_Healer" min="1" max="20" value="2" step="0.5">
                    <span id="threatValue_Healer">2</span>
                </div>
                <div class="control-group">
                    <label>Threat Curvature:</label>
                    <input type="range" id="curvature_Healer_threat" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_Healer_threat">0.5</span>
                </div>
                <div class="control-group">
                    <label>Threat Type:</label>
                    <select id="edgeType_Healer_threat">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
            </div>
            
            <!-- MeleeDPS Controls -->
            <div class="agent-control-box" style="border: 2px solid #FFD93D; background: #fffef0;">
                <h3 style="color: #FFD93D; margin-top: 0;">MeleeDPS</h3>
                <div class="control-group">
                    <label>Node Size:</label>
                    <input type="range" id="nodeSize_MeleeDPS" min="0.5" max="3" value="1" step="0.1">
                    <span id="nodeSizeValue_MeleeDPS">1.0</span>
                </div>
                <div class="control-group">
                    <label>Damage Thickness:</label>
                    <input type="range" id="damageThickness_MeleeDPS" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_MeleeDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Damage Curvature:</label>
                    <input type="range" id="curvature_MeleeDPS_damage" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_MeleeDPS_damage">0.5</span>
                </div>
                <div class="control-group">
                    <label>Damage Type:</label>
                    <select id="edgeType_MeleeDPS_damage">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Threat Thickness:</label>
                    <input type="range" id="threatThickness_MeleeDPS" min="1" max="20" value="2" step="0.5">
                    <span id="threatValue_MeleeDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Threat Curvature:</label>
                    <input type="range" id="curvature_MeleeDPS_threat" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_MeleeDPS_threat">0.5</span>
                </div>
                <div class="control-group">
                    <label>Threat Type:</label>
                    <select id="edgeType_MeleeDPS_threat">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
            </div>
            
            <!-- RangedDPS Controls -->
            <div class="agent-control-box" style="border: 2px solid #A78BFA; background: #faf5ff;">
                <h3 style="color: #A78BFA; margin-top: 0;">RangedDPS</h3>
                <div class="control-group">
                    <label>Node Size:</label>
                    <input type="range" id="nodeSize_RangedDPS" min="0.5" max="3" value="1" step="0.1">
                    <span id="nodeSizeValue_RangedDPS">1.0</span>
                </div>
                <div class="control-group">
                    <label>Damage Thickness:</label>
                    <input type="range" id="damageThickness_RangedDPS" min="1" max="20" value="2" step="0.5">
                    <span id="damageValue_RangedDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Damage Curvature:</label>
                    <input type="range" id="curvature_RangedDPS_damage" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_RangedDPS_damage">0.5</span>
                </div>
                <div class="control-group">
                    <label>Damage Type:</label>
                    <select id="edgeType_RangedDPS_damage">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
                <div class="control-group">
                    <label>Threat Thickness:</label>
                    <input type="range" id="threatThickness_RangedDPS" min="1" max="20" value="2" step="0.5">
                    <span id="threatValue_RangedDPS">2</span>
                </div>
                <div class="control-group">
                    <label>Threat Curvature:</label>
                    <input type="range" id="curvature_RangedDPS_threat" min="0" max="1" value="0.5" step="0.05">
                    <span id="curvatureValue_RangedDPS_threat">0.5</span>
                </div>
                <div class="control-group">
                    <label>Threat Type:</label>
                    <select id="edgeType_RangedDPS_threat">
                        <option value="continuous">Continuous</option>
                        <option value="straight">Straight</option>
                        <option value="curvedCW">Curved CW</option>
                        <option value="curvedCCW">Curved CCW</option>
                    </select>
                </div>
            </div>
            
            <div class="controls">
                <h3>Global Arrow Controls</h3>
                <div class="control-group">
                    <label>Arrow Length:</label>
                    <input type="range" id="arrowLength" min="0.5" max="3" value="1.2" step="0.1">
                    <span id="arrowLengthValue">1.2</span>
                </div>
                
                <div style="margin-top: 15px;">
                    <button onclick="resetLayout()">Reset Node Positions</button>
                    <button onclick="resetAll()">Reset All</button>
                </div>
                
                <div style="margin-top: 15px; border-top: 1px solid #ddd; padding-top: 15px;">
                    <h3>Save/Load</h3>
                    <div style="margin-top: 10px;">
                        <button onclick="saveConfiguration()" style="background: #4ECDC4; color: white; border: none;">Save Configuration</button>
                        <button onclick="loadConfiguration()" style="background: #51CF66; color: white; border: none;">Load Configuration</button>
                    </div>
                    <div style="margin-top: 10px; font-size: 10px; color: #666;">
                        Configuration is saved to browser localStorage
                    </div>
                </div>
            </div>
            
            <div class="legend">
                <h3>Legend</h3>
                <div class="legend-item"><span class="legend-color" style="background: #FFD93D;"></span>Boss Damage (Yellow)</div>
                <div class="legend-item"><span class="legend-color" style="background: #FF6B6B;"></span>Party Damage (Red)</div>
                <div class="legend-item"><span class="legend-color" style="background: #4DABF7;"></span>Threat (Blue)</div>
                <div class="legend-item"><span class="legend-color" style="background: {TAUNT_COLOR};"></span>Taunt (Purple)</div>
                <div class="legend-item"><span class="legend-color" style="background: #51CF66;"></span>Healing (Green)</div>
                <div class="legend-item"><strong>Node Size:</strong> Class Selection Frequency</div>
            </div>
        </div>
        
        <div class="canvas-panel">
            <div id="mynetwork"></div>
        </div>
    </div>
    
    <script type="text/javascript">
        // Data
        const nodes = new vis.DataSet({json.dumps(nodes_data, indent=8)});
        const edges = new vis.DataSet({json.dumps(edges_data, indent=8)});
        const edgeTypes = {json.dumps(edge_types, indent=8)};
        
        // Store original widths and sizes
        const originalWidths = {{}};
        const originalNodeSizes = {{}};
        edges.forEach(edge => {{
            originalWidths[edge.id] = edge.width;
        }});
        nodes.forEach(node => {{
            originalNodeSizes[node.id] = node.size;
        }});
        
        // Create network
        const container = document.getElementById('mynetwork');
        const data = {{ nodes: nodes, edges: edges }};
        const options = {{
            nodes: {{
                shape: 'dot',
                font: {{ size: 14, face: 'Arial' }},
                borderWidth: 2,
                shadow: true
            }},
            edges: {{
                smooth: {{
                    type: 'continuous',
                    roundness: 0.5
                }},
                arrows: {{
                    to: {{ enabled: true, scaleFactor: 1.2 }}
                }},
                shadow: true
            }},
            physics: {{
                enabled: false  // Completely disabled - nodes move independently
            }},
            interaction: {{
                dragNodes: true,
                dragView: true,
                zoomView: true,
                selectConnectedEdges: false  // Don't select connected edges when dragging
            }},
            manipulation: {{
                enabled: false  // Disable manipulation mode
            }}
        }};
        
        const network = new vis.Network(container, data, options);
        
        // Ensure nodes move independently - disable any physics or layout updates
        network.on('dragStart', function(params) {{
            // When dragging starts, ensure physics is off and nodes are independent
            network.setOptions({{
                physics: {{ enabled: false }}
            }});
        }});
        
        network.on('dragEnd', function(params) {{
            // After dragging, keep physics off
            network.setOptions({{
                physics: {{ enabled: false }}
            }});
        }});
        
        // Store original curvatures
        const originalCurvatures = {{}};
        edges.forEach(edge => {{
            originalCurvatures[edge.id] = edge.smooth ? (edge.smooth.roundness || 0.5) : 0.5;
        }});
        
        // Agent-specific update functions
        function updateAgentNodeSize(agentId, multiplier) {{
            if (originalNodeSizes[agentId] !== undefined) {{
                const newSize = originalNodeSizes[agentId] * multiplier;
                nodes.update({{ id: agentId, size: newSize }});
            }}
        }}
        
        function updateAgentEdgeThickness(agentId, edgeType, value, targetAgent = null) {{
            edges.forEach(edge => {{
                if (edge.sourceAgent === agentId && edge.edgeType === edgeType) {{
                    // If targetAgent is specified, only update edges to that target
                    if (targetAgent !== null) {{
                        if (edge.targetAgent === targetAgent) {{
                            const newWidth = originalWidths[edge.id] * (value / 2.0);
                            edges.update({{ id: edge.id, width: newWidth }});
                        }}
                    }} else {{
                        // Update all edges of this type from this agent
                        const newWidth = originalWidths[edge.id] * (value / 2.0);
                        edges.update({{ id: edge.id, width: newWidth }});
                    }}
                }}
            }});
        }}
        
        function updateAgentEdgeCurvature(agentId, edgeType, roundness) {{
            edges.forEach(edge => {{
                if (edge.sourceAgent === agentId && edge.edgeType === edgeType) {{
                    const currentSmooth = edge.smooth || {{ type: 'continuous', roundness: 0.5 }};
                    edges.update({{ 
                        id: edge.id, 
                        smooth: {{ 
                            type: currentSmooth.type || 'continuous', 
                            roundness: roundness 
                        }} 
                    }});
                }}
            }});
        }}
        
        function updateAgentEdgeType(agentId, edgeType, type) {{
            const roundness = parseFloat(document.getElementById(`curvature_${{agentId}}_${{edgeType}}`).value);
            edges.forEach(edge => {{
                if (edge.sourceAgent === agentId && edge.edgeType === edgeType) {{
                    let smoothConfig;
                    if (type === 'straight') {{
                        smoothConfig = false;
                    }} else {{
                        smoothConfig = {{ type: type, roundness: roundness }};
                    }}
                    edges.update({{ id: edge.id, smooth: smoothConfig }});
                }}
            }});
        }}
        
        // Set up controls for each agent
        const agents = ['Boss', 'Tank', 'Healer', 'MeleeDPS', 'RangedDPS'];
        
        agents.forEach(agentId => {{
            // Node size
            const nodeSizeSlider = document.getElementById(`nodeSize_${{agentId}}`);
            const nodeSizeValue = document.getElementById(`nodeSizeValue_${{agentId}}`);
            if (nodeSizeSlider && nodeSizeValue) {{
                nodeSizeSlider.addEventListener('input', function(e) {{
                    const value = parseFloat(e.target.value);
                    nodeSizeValue.textContent = value.toFixed(1);
                    updateAgentNodeSize(agentId, value);
                }});
            }}
            
            // Damage thickness - Boss has individual controls for each target
            if (agentId === 'Boss') {{
                const targets = ['Tank', 'Healer', 'MeleeDPS', 'RangedDPS'];
                targets.forEach(targetId => {{
                    const damageSlider = document.getElementById(`damageThickness_${{agentId}}_${{targetId}}`);
                    const damageValue = document.getElementById(`damageValue_${{agentId}}_${{targetId}}`);
                    if (damageSlider && damageValue) {{
                        damageSlider.addEventListener('input', function(e) {{
                            const value = parseFloat(e.target.value);
                            damageValue.textContent = value.toFixed(1);
                            updateAgentEdgeThickness(agentId, 'damage', value, targetId);
                        }});
                    }}
                }});
            }} else {{
                // Other agents have single damage control
                const damageSlider = document.getElementById(`damageThickness_${{agentId}}`);
                const damageValue = document.getElementById(`damageValue_${{agentId}}`);
                if (damageSlider && damageValue) {{
                    damageSlider.addEventListener('input', function(e) {{
                        const value = parseFloat(e.target.value);
                        damageValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'damage', value);
                    }});
                }}
            }}
            
            // Threat thickness (for Tank, Healer, MeleeDPS, RangedDPS)
            if (agentId !== 'Boss') {{
                const threatSlider = document.getElementById(`threatThickness_${{agentId}}`);
                const threatValue = document.getElementById(`threatValue_${{agentId}}`);
                if (threatSlider && threatValue) {{
                    threatSlider.addEventListener('input', function(e) {{
                        const value = parseFloat(e.target.value);
                        threatValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'threat', value);
                    }});
                }}
            }}
            
            // Taunt thickness (for Tank only)
            if (agentId === 'Tank') {{
                const tauntSlider = document.getElementById(`tauntThickness_${{agentId}}`);
                const tauntValue = document.getElementById(`tauntValue_${{agentId}}`);
                if (tauntSlider && tauntValue) {{
                    tauntSlider.addEventListener('input', function(e) {{
                        const value = parseFloat(e.target.value);
                        tauntValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'taunt', value);
                    }});
                }}
            }}
            
            // Healing thickness (for Healer only)
            if (agentId === 'Healer') {{
                const healSlider = document.getElementById(`healThickness_${{agentId}}`);
                const healValue = document.getElementById(`healValue_${{agentId}}`);
                if (healSlider && healValue) {{
                    healSlider.addEventListener('input', function(e) {{
                        const value = parseFloat(e.target.value);
                        healValue.textContent = value.toFixed(1);
                        updateAgentEdgeThickness(agentId, 'heal', value);
                    }});
                }}
            }}
            
            // Edge curvature and type controls for each edge type
            if (agentId === 'Boss') {{
                // Boss has single curvature/type for all damage edges
                const curvatureSlider = document.getElementById(`curvature_${{agentId}}`);
                const curvatureValue = document.getElementById(`curvatureValue_${{agentId}}`);
                if (curvatureSlider && curvatureValue) {{
                    curvatureSlider.addEventListener('input', function(e) {{
                        const value = parseFloat(e.target.value);
                        curvatureValue.textContent = value.toFixed(2);
                        updateAgentEdgeCurvature(agentId, 'damage', value);
                    }});
                }}
                
                const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}`);
                if (edgeTypeSelect) {{
                    edgeTypeSelect.addEventListener('change', function(e) {{
                        updateAgentEdgeType(agentId, 'damage', e.target.value);
                    }});
                }}
            }} else {{
                // Party members have individual controls for each edge type
                const edgeTypes = ['damage', 'threat'];
                if (agentId === 'Tank') {{
                    edgeTypes.push('taunt');
                }}
                if (agentId === 'Healer') {{
                    edgeTypes.push('heal');
                }}
                
                edgeTypes.forEach(edgeType => {{
                    const curvatureSlider = document.getElementById(`curvature_${{agentId}}_${{edgeType}}`);
                    const curvatureValue = document.getElementById(`curvatureValue_${{agentId}}_${{edgeType}}`);
                    if (curvatureSlider && curvatureValue) {{
                        curvatureSlider.addEventListener('input', function(e) {{
                            const value = parseFloat(e.target.value);
                            curvatureValue.textContent = value.toFixed(2);
                            updateAgentEdgeCurvature(agentId, edgeType, value);
                        }});
                    }}
                    
                    const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}_${{edgeType}}`);
                    if (edgeTypeSelect) {{
                        edgeTypeSelect.addEventListener('change', function(e) {{
                            updateAgentEdgeType(agentId, edgeType, e.target.value);
                        }});
                    }}
                }});
            }}
        }});
        
        // Global arrow controls
        function updateArrowSize(lengthMultiplier) {{
            const newOptions = {{
                edges: {{
                    arrows: {{
                        to: {{
                            enabled: true,
                            scaleFactor: 1.2 * lengthMultiplier,
                            type: 'arrow'
                        }}
                    }}
                }}
            }};
            network.setOptions(newOptions);
        }}
        
        document.getElementById('arrowLength').addEventListener('input', function(e) {{
            const lengthValue = parseFloat(e.target.value);
            document.getElementById('arrowLengthValue').textContent = lengthValue.toFixed(1);
            updateArrowSize(lengthValue);
        }});
        
        function resetLayout() {{
            nodes.forEach(node => {{
                const pos = {json.dumps({n: pos[n] for n in G.nodes()}, indent=16)};
                if (pos[node.id]) {{
                    nodes.update({{
                        id: node.id,
                        x: pos[node.id][0] * 200,
                        y: pos[node.id][1] * 200
                    }});
                }}
            }});
        }}
        
        function resetAll() {{
            resetLayout();
            // Reset all node sizes
            agents.forEach(agentId => {{
                nodes.update({{ id: agentId, size: originalNodeSizes[agentId] }});
                const nodeSizeSlider = document.getElementById(`nodeSize_${{agentId}}`);
                const nodeSizeValue = document.getElementById(`nodeSizeValue_${{agentId}}`);
                if (nodeSizeSlider && nodeSizeValue) {{
                    nodeSizeSlider.value = 1;
                    nodeSizeValue.textContent = '1.0';
                }}
            }});
            
            // Reset all edge thicknesses
            edges.forEach(edge => {{
                edges.update({{ id: edge.id, width: originalWidths[edge.id] }});
            }});
            
            // Reset all sliders to defaults
            agents.forEach(agentId => {{
                // Damage - Boss has individual controls
                if (agentId === 'Boss') {{
                    const targets = ['Tank', 'Healer', 'MeleeDPS', 'RangedDPS'];
                    targets.forEach(targetId => {{
                        const damageSlider = document.getElementById(`damageThickness_${{agentId}}_${{targetId}}`);
                        const damageValue = document.getElementById(`damageValue_${{agentId}}_${{targetId}}`);
                        if (damageSlider && damageValue) {{
                            damageSlider.value = 2;
                            damageValue.textContent = '2';
                        }}
                    }});
                }} else {{
                    const damageSlider = document.getElementById(`damageThickness_${{agentId}}`);
                    const damageValue = document.getElementById(`damageValue_${{agentId}}`);
                    if (damageSlider && damageValue) {{
                        damageSlider.value = 2;
                        damageValue.textContent = '2';
                    }}
                }}
                
                // Threat
                if (agentId !== 'Boss') {{
                    const threatSlider = document.getElementById(`threatThickness_${{agentId}}`);
                    const threatValue = document.getElementById(`threatValue_${{agentId}}`);
                    if (threatSlider && threatValue) {{
                        threatSlider.value = 2;
                        threatValue.textContent = '2';
                    }}
                }}
                
                // Taunt (Tank only)
                if (agentId === 'Tank') {{
                    const tauntSlider = document.getElementById(`tauntThickness_${{agentId}}`);
                    const tauntValue = document.getElementById(`tauntValue_${{agentId}}`);
                    if (tauntSlider && tauntValue) {{
                        tauntSlider.value = 4;
                        tauntValue.textContent = '4';
                    }}
                }}
                
                // Healing (Healer only)
                if (agentId === 'Healer') {{
                    const healSlider = document.getElementById(`healThickness_${{agentId}}`);
                    const healValue = document.getElementById(`healValue_${{agentId}}`);
                    if (healSlider && healValue) {{
                        healSlider.value = 2;
                        healValue.textContent = '2';
                    }}
                }}
                
                // Curvature and edge type - Boss has single control, party members have per-edge-type
                if (agentId === 'Boss') {{
                    const curvatureSlider = document.getElementById(`curvature_${{agentId}}`);
                    const curvatureValue = document.getElementById(`curvatureValue_${{agentId}}`);
                    if (curvatureSlider && curvatureValue) {{
                        curvatureSlider.value = 0.5;
                        curvatureValue.textContent = '0.5';
                        updateAgentEdgeCurvature(agentId, 'damage', 0.5);
                    }}
                    
                    const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}`);
                    if (edgeTypeSelect) {{
                        edgeTypeSelect.value = 'continuous';
                        updateAgentEdgeType(agentId, 'damage', 'continuous');
                    }}
                }} else {{
                    const edgeTypes = ['damage', 'threat'];
                    if (agentId === 'Tank') {{
                        edgeTypes.push('taunt');
                    }}
                    if (agentId === 'Healer') {{
                        edgeTypes.push('heal');
                    }}
                    
                    edgeTypes.forEach(edgeType => {{
                        const curvatureSlider = document.getElementById(`curvature_${{agentId}}_${{edgeType}}`);
                        const curvatureValue = document.getElementById(`curvatureValue_${{agentId}}_${{edgeType}}`);
                        if (curvatureSlider && curvatureValue) {{
                            curvatureSlider.value = 0.5;
                            curvatureValue.textContent = '0.5';
                            updateAgentEdgeCurvature(agentId, edgeType, 0.5);
                        }}
                        
                        const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}_${{edgeType}}`);
                        if (edgeTypeSelect) {{
                            edgeTypeSelect.value = 'continuous';
                            updateAgentEdgeType(agentId, edgeType, 'continuous');
                        }}
                    }});
                }}
            }});
            
            // Reset arrow size
            document.getElementById('arrowLength').value = 1.2;
            document.getElementById('arrowLengthValue').textContent = '1.2';
            updateArrowSize(1.2);
        }}
        
        // Save/Load Configuration
        function saveConfiguration() {{
            const config = {{
                nodeSizes: {{}},
                edgeThicknesses: {{}},
                edgeCurvatures: {{}},
                edgeTypes: {{}},
                arrowLength: parseFloat(document.getElementById('arrowLength').value),
                nodePositions: {{}}
            }};
            
            // Save node sizes
            agents.forEach(agentId => {{
                const slider = document.getElementById(`nodeSize_${{agentId}}`);
                if (slider) {{
                    config.nodeSizes[agentId] = parseFloat(slider.value);
                }}
            }});
            
            // Save edge thicknesses
            agents.forEach(agentId => {{
                if (agentId === 'Boss') {{
                    const targets = ['Tank', 'Healer', 'MeleeDPS', 'RangedDPS'];
                    targets.forEach(targetId => {{
                        const slider = document.getElementById(`damageThickness_${{agentId}}_${{targetId}}`);
                        if (slider) {{
                            config.edgeThicknesses[`${{agentId}}_damage_${{targetId}}`] = parseFloat(slider.value);
                        }}
                    }});
                }} else {{
                    const damageSlider = document.getElementById(`damageThickness_${{agentId}}`);
                    if (damageSlider) {{
                        config.edgeThicknesses[`${{agentId}}_damage`] = parseFloat(damageSlider.value);
                    }}
                    
                    if (agentId !== 'Boss') {{
                        const threatSlider = document.getElementById(`threatThickness_${{agentId}}`);
                        if (threatSlider) {{
                            config.edgeThicknesses[`${{agentId}}_threat`] = parseFloat(threatSlider.value);
                        }}
                    }}
                    
                    if (agentId === 'Tank') {{
                        const tauntSlider = document.getElementById(`tauntThickness_${{agentId}}`);
                        if (tauntSlider) {{
                            config.edgeThicknesses[`${{agentId}}_taunt`] = parseFloat(tauntSlider.value);
                        }}
                    }}
                    
                    if (agentId === 'Healer') {{
                        const healSlider = document.getElementById(`healThickness_${{agentId}}`);
                        if (healSlider) {{
                            config.edgeThicknesses[`${{agentId}}_heal`] = parseFloat(healSlider.value);
                        }}
                    }}
                }}
            }});
            
            // Save edge curvatures and types
            agents.forEach(agentId => {{
                if (agentId === 'Boss') {{
                    const curvatureSlider = document.getElementById(`curvature_${{agentId}}`);
                    const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}`);
                    if (curvatureSlider) {{
                        config.edgeCurvatures[`${{agentId}}_damage`] = parseFloat(curvatureSlider.value);
                    }}
                    if (edgeTypeSelect) {{
                        config.edgeTypes[`${{agentId}}_damage`] = edgeTypeSelect.value;
                    }}
                }} else {{
                    const edgeTypes = ['damage', 'threat'];
                    if (agentId === 'Tank') {{
                        edgeTypes.push('taunt');
                    }}
                    if (agentId === 'Healer') {{
                        edgeTypes.push('heal');
                    }}
                    
                    edgeTypes.forEach(edgeType => {{
                        const curvatureSlider = document.getElementById(`curvature_${{agentId}}_${{edgeType}}`);
                        const edgeTypeSelect = document.getElementById(`edgeType_${{agentId}}_${{edgeType}}`);
                        if (curvatureSlider) {{
                            config.edgeCurvatures[`${{agentId}}_${{edgeType}}`] = parseFloat(curvatureSlider.value);
                        }}
                        if (edgeTypeSelect) {{
                            config.edgeTypes[`${{agentId}}_${{edgeType}}`] = edgeTypeSelect.value;
                        }}
                    }});
                }}
            }});
            
            // Save node positions
            nodes.forEach(node => {{
                config.nodePositions[node.id] = {{ x: node.x, y: node.y }};
            }});
            
            // Save to localStorage
            localStorage.setItem('sna_configuration', JSON.stringify(config));
            alert('Configuration saved!');
        }}
        
        function loadConfiguration() {{
            const saved = localStorage.getItem('sna_configuration');
            if (!saved) {{
                alert('No saved configuration found!');
                return;
            }}
            
            try {{
                const config = JSON.parse(saved);
                
                // Load node sizes
                Object.keys(config.nodeSizes || {{}}).forEach(agentId => {{
                    const slider = document.getElementById(`nodeSize_${{agentId}}`);
                    const valueSpan = document.getElementById(`nodeSizeValue_${{agentId}}`);
                    if (slider && valueSpan) {{
                        slider.value = config.nodeSizes[agentId];
                        valueSpan.textContent = config.nodeSizes[agentId].toFixed(1);
                        updateAgentNodeSize(agentId, config.nodeSizes[agentId]);
                    }}
                }});
                
                // Load edge thicknesses
                Object.keys(config.edgeThicknesses || {{}}).forEach(key => {{
                    const parts = key.split('_');
                    if (parts.length === 3 && parts[0] === 'Boss') {{
                        // Boss damage to specific target
                        const slider = document.getElementById(`damageThickness_${{parts[0]}}_${{parts[2]}}`);
                        const valueSpan = document.getElementById(`damageValue_${{parts[0]}}_${{parts[2]}}`);
                        if (slider && valueSpan) {{
                            slider.value = config.edgeThicknesses[key];
                            valueSpan.textContent = config.edgeThicknesses[key].toFixed(1);
                            updateAgentEdgeThickness(parts[0], 'damage', config.edgeThicknesses[key], parts[2]);
                        }}
                    }} else if (parts.length === 2) {{
                        // Regular edge thickness
                        const agentId = parts[0];
                        const edgeType = parts[1];
                        const slider = document.getElementById(`${{edgeType}}Thickness_${{agentId}}`);
                        const valueSpan = document.getElementById(`${{edgeType}}Value_${{agentId}}`);
                        if (slider && valueSpan) {{
                            slider.value = config.edgeThicknesses[key];
                            valueSpan.textContent = config.edgeThicknesses[key].toFixed(1);
                            updateAgentEdgeThickness(agentId, edgeType, config.edgeThicknesses[key]);
                        }}
                    }}
                }});
                
                // Load edge curvatures
                Object.keys(config.edgeCurvatures || {{}}).forEach(key => {{
                    const parts = key.split('_');
                    if (parts.length === 2 && parts[0] === 'Boss') {{
                        const slider = document.getElementById(`curvature_${{parts[0]}}`);
                        const valueSpan = document.getElementById(`curvatureValue_${{parts[0]}}`);
                        if (slider && valueSpan) {{
                            slider.value = config.edgeCurvatures[key];
                            valueSpan.textContent = config.edgeCurvatures[key].toFixed(2);
                            updateAgentEdgeCurvature(parts[0], parts[1], config.edgeCurvatures[key]);
                        }}
                    }} else if (parts.length === 2) {{
                        const slider = document.getElementById(`curvature_${{parts[0]}}_${{parts[1]}}`);
                        const valueSpan = document.getElementById(`curvatureValue_${{parts[0]}}_${{parts[1]}}`);
                        if (slider && valueSpan) {{
                            slider.value = config.edgeCurvatures[key];
                            valueSpan.textContent = config.edgeCurvatures[key].toFixed(2);
                            updateAgentEdgeCurvature(parts[0], parts[1], config.edgeCurvatures[key]);
                        }}
                    }}
                }});
                
                // Load edge types
                Object.keys(config.edgeTypes || {{}}).forEach(key => {{
                    const parts = key.split('_');
                    if (parts.length === 2 && parts[0] === 'Boss') {{
                        const select = document.getElementById(`edgeType_${{parts[0]}}`);
                        if (select) {{
                            select.value = config.edgeTypes[key];
                            updateAgentEdgeType(parts[0], parts[1], config.edgeTypes[key]);
                        }}
                    }} else if (parts.length === 2) {{
                        const select = document.getElementById(`edgeType_${{parts[0]}}_${{parts[1]}}`);
                        if (select) {{
                            select.value = config.edgeTypes[key];
                            updateAgentEdgeType(parts[0], parts[1], config.edgeTypes[key]);
                        }}
                    }}
                }});
                
                // Load arrow length
                if (config.arrowLength !== undefined) {{
                    document.getElementById('arrowLength').value = config.arrowLength;
                    document.getElementById('arrowLengthValue').textContent = config.arrowLength.toFixed(1);
                    updateArrowSize(config.arrowLength);
                }}
                
                // Load node positions
                if (config.nodePositions) {{
                    Object.keys(config.nodePositions).forEach(nodeId => {{
                        const pos = config.nodePositions[nodeId];
                        nodes.update({{ id: nodeId, x: pos.x, y: pos.y }});
                    }});
                }}
                
                alert('Configuration loaded!');
            }} catch (e) {{
                alert('Error loading configuration: ' + e.message);
            }}
        }}
    </script>
</body>
</html>"""
    
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(html_content)
    
    print(f"Saved interactive graph: {output_path}")
    print("Open the HTML file in a browser to interact with the graph!")
    print("Features:")
    print("  - Drag nodes to reposition them")
    print("  - Use sliders to adjust edge thickness")
    print("  - Click 'Reset Node Positions' to restore original layout")
    print("  - Click 'Reset Edge Thickness' to restore original thickness")

def main():
    parser = argparse.ArgumentParser(description="Generate interactive HTML SNA graph")
    parser.add_argument("--input", "-i", required=True, help="Path to episodes JSON file")
    parser.add_argument("--output", "-o", default="interactive_sna.html", help="Output HTML path")
    parser.add_argument("--title", "-t", default="Interactive Damage & Healing Network", help="Title")
    parser.add_argument("--early-range", nargs=2, type=int, help="Early episodes range (e.g., 0 500)")
    parser.add_argument("--late-range", nargs=2, type=int, help="Late episodes range (e.g., 2500 3000)")
    parser.add_argument("--compare", action="store_true", help="Generate side-by-side early vs late comparison")
    args = parser.parse_args()
    
    print(f"Loading episodes from {args.input}...")
    episodes = load_episodes(args.input)
    print(f"Loaded {len(episodes)} episodes")
    
    if args.compare and args.early_range and args.late_range:
        print("\nExtracting early episodes...")
        early_boss, early_party, early_heal, early_threat, early_taunt, early_class = extract_damage_healing_threat_edges(episodes, tuple(args.early_range))
        print(f"Early: {len(early_boss)} boss damage, {len(early_party)} party damage, {len(early_heal)} healing, {len(early_threat)} threat, {len(early_taunt)} taunt")
        
        print("\nExtracting late episodes...")
        late_boss, late_party, late_heal, late_threat, late_taunt, late_class = extract_damage_healing_threat_edges(episodes, tuple(args.late_range))
        print(f"Late: {len(late_boss)} boss damage, {len(late_party)} party damage, {len(late_heal)} healing, {len(late_threat)} threat, {len(late_taunt)} taunt")
        
        # Create two separate HTML files
        early_output = args.output.replace('.html', '_early.html')
        late_output = args.output.replace('.html', '_late.html')
        
        create_interactive_html(early_boss, early_party, early_heal, early_threat, early_taunt, early_class,
                                early_output, f"Early Training (Episodes {args.early_range[0]}-{args.early_range[1]})", is_early=True)
        create_interactive_html(late_boss, late_party, late_heal, late_threat, late_taunt, late_class,
                                late_output, f"Late Training (Episodes {args.late_range[0]}-{args.late_range[1]})", is_early=False)
    else:
        print("\nExtracting damage, healing, and threat edges...")
        boss, party, heal, threat, taunt, class_counts = extract_damage_healing_threat_edges(episodes)
        print(f"Found {len(boss)} boss damage edges, {len(party)} party damage edges, {len(heal)} healing edges, {len(threat)} threat edges, {len(taunt)} taunt edges")
        
        create_interactive_html(boss, party, heal, threat, taunt, class_counts, args.output, args.title)

if __name__ == "__main__":
    main()
