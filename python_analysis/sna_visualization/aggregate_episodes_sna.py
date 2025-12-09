"""
Aggregate multiple episodes into a single SNA visualization showing strategy evolution.
Shows frequency, linkages, and how coordination develops over time.
"""

import argparse
import json
import os
from collections import defaultdict
from typing import Dict, List, Tuple

import networkx as nx
import plotly.graph_objects as go
import numpy as np

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

def aggregate_actions_from_episodes(episodes: List[dict]) -> Tuple[List[Tuple[str, str, float, str, int]], Dict[int, float]]:
    """
    Aggregate actions from all episodes.
    Returns: (edge_list, episode_weights)
    edge_list: (source, target, weight, edge_type, first_seen_episode)
    episode_weights: {episode_num: learning_progress}
    """
    edges = defaultdict(lambda: defaultdict(lambda: {"weight": 0.0, "first_seen": None, "episodes": []}))
    episode_weights = {}
    
    agent_classes = {}
    if episodes and "agentIds" in episodes[0] and "agentClassValues" in episodes[0]:
        for a_id, a_cls in zip(episodes[0]["agentIds"], episodes[0]["agentClassValues"]):
            agent_classes[a_id] = a_cls
    
    boss_ids = {aid for aid, cls in agent_classes.items() if str(cls).lower() == "boss"}
    boss_id = next(iter(boss_ids), "Boss")
    party_members = [aid for aid in agent_classes.keys() if aid not in boss_ids]
    
    for episode in episodes:
        episode_num = episode.get("episode", 0)
        learning_progress = episode.get("learningProgress", 0.0)
        episode_weights[episode_num] = learning_progress
        
        actions = episode.get("actions", [])
        
        for act in actions:
            branch = act.get("branch")
            val = act.get("value", 0)
            agent = act.get("agentId", "unknown")
            
            if branch == "attack" and val == 1:
                if agent in boss_ids or agent.lower() == "boss":
                    # Boss attacks party members - create individual edges to each party member
                    # This shows which party members the boss targets most
                    for pm in party_members:
                        if edges[agent][pm]["first_seen"] is None:
                            edges[agent][pm]["first_seen"] = episode_num
                        # Distribute boss attacks, but weight by episode (later episodes = more coordinated)
                        weight = 1.0 / len(party_members) if party_members else 1.0
                        edges[agent][pm]["weight"] += weight
                        edges[agent][pm]["episodes"].append(episode_num)
                else:
                    # Party member attacks boss - individual edges show DPS contribution
                    tgt = boss_id
                    if edges[agent][tgt]["first_seen"] is None:
                        edges[agent][tgt]["first_seen"] = episode_num
                    edges[agent][tgt]["weight"] += 1.0
                    edges[agent][tgt]["episodes"].append(episode_num)
            
            elif branch == "threat_boost" and val == 1:
                tgt = "Threat"
                if edges[agent][tgt]["first_seen"] is None:
                    edges[agent][tgt]["first_seen"] = episode_num
                edges[agent][tgt]["weight"] += 1.0
                edges[agent][tgt]["episodes"].append(episode_num)
            
            elif branch == "heal" and val == 1:
                tgt = "Heals"
                if edges[agent][tgt]["first_seen"] is None:
                    edges[agent][tgt]["first_seen"] = episode_num
                edges[agent][tgt]["weight"] += 1.0
                edges[agent][tgt]["episodes"].append(episode_num)
    
    # Convert to edge list
    edge_list = []
    for src, tgts in edges.items():
        for tgt, data in tgts.items():
            edge_type = "heal" if tgt == "Heals" else "damage"
            if tgt == "Threat":
                edge_type = "threat"
            edge_list.append((
                src, tgt, data["weight"], edge_type, 
                data["first_seen"], data["episodes"]
            ))
    
    return edge_list, episode_weights

def build_aggregate_graph(edge_list: List[Tuple]) -> nx.DiGraph:
    """Build NetworkX graph from aggregated edges"""
    G = nx.DiGraph()
    
    for src, tgt, weight, ev_type, first_seen, episodes in edge_list:
        if G.has_edge(src, tgt):
            G[src][tgt]["weight"] += weight
            G[src][tgt]["episodes"].extend(episodes)
            if first_seen is not None and (G[src][tgt].get("first_seen") is None or first_seen < G[src][tgt]["first_seen"]):
                G[src][tgt]["first_seen"] = first_seen
        else:
            G.add_edge(src, tgt, weight=weight, ev_type=ev_type, 
                      first_seen=first_seen, episodes=episodes.copy())
    
    return G

def draw_aggregate_graph_plotly(G: nx.DiGraph, episode_weights: Dict[int, float], 
                                output_path: str, title: str):
    """Create sophisticated visualization of aggregated network"""
    
    # Compute metrics
    out_strength = {n: sum(d["weight"] for _, _, d in G.out_edges(n, data=True)) for n in G.nodes()}
    in_strength = {n: sum(d["weight"] for _, _, d in G.in_edges(n, data=True)) for n in G.nodes()}
    total_strength = {n: out_strength.get(n, 0) + in_strength.get(n, 0) for n in G.nodes()}
    
    # Calculate when edges first appeared (strategy formation indicator)
    edge_formation = {}
    for u, v, data in G.edges(data=True):
        first_seen = data.get("first_seen", 0)
        edge_formation[(u, v)] = first_seen
    
    # Centrality metrics
    try:
        degree_centrality = nx.degree_centrality(G.to_undirected())
        betweenness = nx.betweenness_centrality(G.to_undirected())
    except:
        degree_centrality = {n: 0.0 for n in G.nodes()}
        betweenness = {n: 0.0 for n in G.nodes()}
    
    # Layout
    try:
        pos = nx.kamada_kawai_layout(G.to_undirected())
    except:
        pos = nx.spring_layout(G, k=3, iterations=100, seed=42)
    
    # Normalize positions
    if pos:
        xs = [pos[n][0] for n in G.nodes()]
        ys = [pos[n][1] for n in G.nodes()]
        x_range = max(xs) - min(xs) if max(xs) != min(xs) else 1
        y_range = max(ys) - min(ys) if max(ys) != min(ys) else 1
        pos = {n: ((pos[n][0] - min(xs)) / x_range * 2 - 1, 
                   (pos[n][1] - min(ys)) / y_range * 2 - 1) for n in G.nodes()}
    
    max_strength = max(total_strength.values()) if total_strength.values() else 1.0
    max_episode = max(episode_weights.keys()) if episode_weights else 3000
    
    # Node colors
    def get_node_color(node):
        node_lower = str(node).lower()
        if "boss" in node_lower:
            return "#FF6B6B"
        elif "tank" in node_lower or "party member 1" in node_lower:
            return "#4ECDC4"
        elif "threat" in node_lower:
            return "#95E1D3"
        elif "heal" in node_lower:
            return "#F38181"
        else:
            return "#FFD93D"
    
    fig = go.Figure()
    
    # Group edges by type and formation time
    edge_traces = {"damage": [], "heal": [], "threat": []}
    
    for u, v, data in G.edges(data=True):
        x0, y0 = pos[u]
        x1, y1 = pos[v]
        w = data.get("weight", 1.0)
        ev_type = data.get("ev_type", "damage")
        first_seen = data.get("first_seen", max_episode)
        
        # Normalize first_seen to 0-1 (0 = early, 1 = late)
        formation_time = first_seen / max_episode if max_episode > 0 else 0.5
        
        # Curved edge
        mid_x, mid_y = (x0 + x1) / 2, (y0 + y1) / 2
        dx, dy = x1 - x0, y1 - y0
        perp_x, perp_y = -dy * 0.3, dx * 0.3
        curve_x = mid_x + perp_x
        curve_y = mid_y + perp_y
        
        edge_traces[ev_type].append((x0, y0, curve_x, curve_y, x1, y1, w, formation_time))
    
    # Add edges with color intensity based on formation time
    for ev_type, edges in edge_traces.items():
        if not edges:
            continue
        
        x_edges = []
        y_edges = []
        edge_hover = []
        edge_colors = []
        
        for x0, y0, cx, cy, x1, y1, w, form_time in edges:
            t_values = np.linspace(0, 1, 20)
            for t in t_values:
                x = (1-t)**2 * x0 + 2*(1-t)*t * cx + t**2 * x1
                y = (1-t)**2 * y0 + 2*(1-t)*t * cy + t**2 * y1
                x_edges.append(x)
                y_edges.append(y)
            x_edges.append(None)
            y_edges.append(None)
            
            # Color based on formation: early = darker, late = lighter
            # Early edges (random phase) = purple/blue
            # Late edges (learned) = bright color
            if form_time < 0.2:  # Early episodes (random)
                base_color = "#9B59B6"  # Purple
            elif form_time < 0.6:  # Learning phase
                base_color = "#3498DB"  # Blue
            else:  # Learned phase
                base_color = "#51CF66" if ev_type == "heal" else ("#4DABF7" if ev_type == "threat" else "#FF6B6B")
            
            edge_hover.append(f"{ev_type.capitalize()}: {w:.1f}<br>First seen: Episode {int(form_time * max_episode)}")
            edge_colors.append(base_color)
        
        fig.add_trace(go.Scatter(
            x=x_edges, y=y_edges,
            mode='lines',
            line=dict(width=3, color=base_color),
            hoverinfo='skip',
            showlegend=True,
            name=f"{ev_type.capitalize()} ({len(edges)} edges)",
            legendgroup=ev_type,
            opacity=0.7
        ))
    
    # Add nodes
    node_x = [pos[node][0] for node in G.nodes()]
    node_y = [pos[node][1] for node in G.nodes()]
    node_text = [str(node).replace("Party Member", "PM") for node in G.nodes()]
    node_sizes = [max(20, min(80, 25 + 55 * (total_strength.get(node, 0) / max_strength))) for node in G.nodes()]
    node_colors = [get_node_color(node) for node in G.nodes()]
    
    # Rich hover info
    node_info = []
    for node in G.nodes():
        info = f"<b>{node}</b><br>"
        info += f"Outgoing: {out_strength.get(node, 0):.0f}<br>"
        info += f"Incoming: {in_strength.get(node, 0):.0f}<br>"
        info += f"Total: {total_strength.get(node, 0):.0f}<br>"
        info += f"Degree Centrality: {degree_centrality.get(node, 0):.3f}<br>"
        info += f"Betweenness: {betweenness.get(node, 0):.3f}"
        node_info.append(info)
    
    fig.add_trace(go.Scatter(
        x=node_x, y=node_y,
        mode='markers+text',
        marker=dict(
            size=node_sizes,
            color=node_colors,
            line=dict(width=4, color='#2C3E50'),
            opacity=0.95
        ),
        text=node_text,
        textposition="middle center",
        textfont=dict(size=12, color='#2C3E50', family='Arial Black'),
        hovertext=node_info,
        hoverinfo='text',
        showlegend=False,
        hovertemplate='%{hovertext}<extra></extra>'
    ))
    
    # Statistics
    total_edges = len(G.edges())
    early_edges = sum(1 for _, _, d in G.edges(data=True) if d.get("first_seen", 3000) < 500)
    late_edges = sum(1 for _, _, d in G.edges(data=True) if d.get("first_seen", 0) > 2000)
    
    annotations = [
        dict(
            text=f"<b>{title}</b><br>"
                 f"Episodes: 0-{max_episode} | Nodes: {len(G.nodes())} | Edges: {total_edges}<br>"
                 f"Early (0-500): {early_edges} edges | Learned (2000+): {late_edges} edges",
            showarrow=False,
            xref="paper", yref="paper",
            x=0.02, y=0.98,
            xanchor="left", yanchor="top",
            font=dict(size=13, color="#2C3E50"),
            bgcolor="rgba(255,255,255,0.9)",
            bordercolor="#2C3E50",
            borderwidth=2
        ),
        dict(
            text="<b>Strategy Evolution:</b><br>"
                 "Purple edges = Early (random phase)<br>"
                 "Blue edges = Learning phase<br>"
                 "Bright edges = Learned strategy",
            showarrow=False,
            xref="paper", yref="paper",
            x=0.98, y=0.02,
            xanchor="right", yanchor="bottom",
            font=dict(size=11, color="#2C3E50"),
            bgcolor="rgba(255,255,255,0.9)",
            bordercolor="#2C3E50",
            borderwidth=2
        )
    ]
    
    fig.update_layout(
        title="",
        showlegend=True,
        hovermode='closest',
        margin=dict(b=20, l=20, r=20, t=20),
        annotations=annotations,
        xaxis=dict(showgrid=False, zeroline=False, showticklabels=False, range=[-1.3, 1.3]),
        yaxis=dict(showgrid=False, zeroline=False, showticklabels=False, range=[-1.3, 1.3]),
        plot_bgcolor='#F8F9FA',
        paper_bgcolor='white',
        legend=dict(
            x=0.02, y=0.5,
            bgcolor="rgba(255,255,255,0.9)",
            bordercolor="#2C3E50",
            borderwidth=1
        ),
        width=1400,
        height=900
    )
    
    fig.write_html(output_path)

def main():
    parser = argparse.ArgumentParser(description="Aggregate episodes into SNA visualization")
    parser.add_argument("--input", "-i", required=True, help="Path to episodes JSON file")
    parser.add_argument("--output", "-o", default="aggregate_sna.html", help="Output HTML path")
    parser.add_argument("--title", "-t", default="Aggregate Episode SNA", help="Title")
    args = parser.parse_args()
    
    print(f"Loading episodes from {args.input}...")
    episodes = load_episodes(args.input)
    print(f"Loaded {len(episodes)} episodes")
    
    print("Aggregating actions...")
    edge_list, episode_weights = aggregate_actions_from_episodes(episodes)
    print(f"Found {len(edge_list)} unique edges")
    
    print("Building network graph...")
    G = build_aggregate_graph(edge_list)
    print(f"Graph: {len(G.nodes())} nodes, {len(G.edges())} edges")
    
    print(f"Generating visualization...")
    draw_aggregate_graph_plotly(G, episode_weights, args.output, args.title)
    
    print(f"✓ Generated {args.output}")
    print(f"  Nodes: {len(G.nodes())}")
    print(f"  Edges: {len(G.edges())}")
    print(f"  Episodes analyzed: {len(episodes)}")

if __name__ == "__main__":
    main()

