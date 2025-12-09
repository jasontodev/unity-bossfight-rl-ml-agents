"""
Generate a Social Network Analysis (SNA) visualization for a recorded episode JSON.

The script prefers explicit combat/heal event data, but will fall back to
action counts if only the EpisodeRecorder JSON (actions list) is available.

Assumed JSON formats (first match wins):
1) {"events": [{"type": "damage"|"heal", "source": "agentA", "target": "agentB", "amount": 10}, ...]}
2) {"combatLog": [...] }  # same schema as above
3) EpisodeRecorder JSON:
   {
     "agentIds": [...],
     "agentClassValues": [...],
     "actions": [{"frame":0,"agentId":"party_0","branch":"attack","value":1}, ...]
   }
   - In this fallback we infer edges:
     * Party attack actions -> edge to "Boss" (or "boss" if present)
     * Boss attack actions -> edge to "Party" (aggregated target)
     * Heal actions -> edge to "Heals" placeholder (targets unknown)

Output: PNG image of the SNA graph.
"""

import argparse
import json
import os
from collections import defaultdict
from typing import Dict, List, Tuple

import matplotlib.pyplot as plt
import networkx as nx


def load_episode(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def pick_event_list(data: dict) -> List[dict]:
    """Return the first available event list using the supported keys."""
    if isinstance(data, dict):
        for key in ("events", "combatLog"):
            if key in data and isinstance(data[key], list):
                return data[key]
    return []


def infer_edges_from_events(events: List[dict]) -> Tuple[List[Tuple[str, str, float, str]], set]:
    edges = []
    nodes = set()
    for ev in events:
        ev_type = str(ev.get("type", "")).lower()
        if ev_type not in ("damage", "heal", "threat"):
            continue
        src = ev.get("source", "unknown")
        tgt = ev.get("target", "unknown")
        amt = float(ev.get("amount", 0))
        nodes.update([src, tgt])
        edges.append((src, tgt, amt, ev_type))
    return edges, nodes


def infer_edges_from_actions(data: dict) -> Tuple[List[Tuple[str, str, float, str]], set]:
    """Fallback when no per-target events exist. Creates more detailed network."""
    actions = data.get("actions", [])
    agent_classes = {}
    if "agentIds" in data and "agentClassValues" in data:
        for a_id, a_cls in zip(data["agentIds"], data["agentClassValues"]):
            agent_classes[a_id] = a_cls

    boss_ids = {aid for aid, cls in agent_classes.items() if str(cls).lower() == "boss"}
    boss_id = next(iter(boss_ids), "Boss")
    
    # Separate party members from boss
    party_members = [aid for aid in agent_classes.keys() if aid not in boss_ids]

    edges = defaultdict(lambda: defaultdict(float))  # src -> tgt -> weight
    nodes = set(agent_classes.keys())

    # Track action frequencies per agent for better visualization
    action_counts = defaultdict(lambda: defaultdict(int))
    
    for act in actions:
        branch = act.get("branch")
        val = act.get("value", 0)
        agent = act.get("agentId", "unknown")
        
        if branch == "attack" and val == 1:
            action_counts[agent]["attack"] += 1
            if agent in boss_ids or agent.lower() == "boss":
                # Boss attacks all party members (distribute threat)
                for pm in party_members:
                    edges[agent][pm] += 1.0 / len(party_members) if party_members else 1.0
                    nodes.update([agent, pm])
            else:
                # Party member attacks boss
                tgt = boss_id
                edges[agent][tgt] += 1.0
                nodes.update([agent, tgt])
        elif branch == "heal" and val == 1:
            action_counts[agent]["heal"] += 1
            # Heal could target any party member, create placeholder
            tgt = "Heals"
            edges[agent][tgt] += 1.0
            nodes.update([agent, tgt])
        elif branch == "threat_boost" and val == 1:
            action_counts[agent]["threat_boost"] += 1
            tgt = "Threat"
            edges[agent][tgt] += 1.0
            nodes.update([agent, tgt])
        elif branch == "movement" and val > 0:
            action_counts[agent]["movement"] += 1
        elif branch == "rotation" and val > 0:
            action_counts[agent]["rotation"] += 1

    # Create edges with weights based on action frequency
    edge_list: List[Tuple[str, str, float, str]] = []
    for src, tgts in edges.items():
        for tgt, w in tgts.items():
            edge_type = "heal" if tgt == "Heals" else "damage"
            if tgt == "Threat":
                edge_type = "threat"
            edge_list.append((src, tgt, w, edge_type))
    
    # Add self-loops or activity indicators for high-activity agents
    for agent, counts in action_counts.items():
        total_actions = sum(counts.values())
        if total_actions > 100:  # High activity threshold
            # Could add self-loop or special node, but skip for now
            pass
    
    return edge_list, nodes


def build_graph(edge_list: List[Tuple[str, str, float, str]]) -> nx.DiGraph:
    G = nx.DiGraph()
    for src, tgt, weight, ev_type in edge_list:
        if G.has_edge(src, tgt):
            G[src][tgt]["weight"] += weight
        else:
            G.add_edge(src, tgt, weight=weight, ev_type=ev_type)
    return G


def draw_graph_matplotlib(G: nx.DiGraph, output_path: str, title: str):
    plt.figure(figsize=(10, 8))
    pos = nx.spring_layout(G, seed=42)

    # Node sizes by total outgoing weight
    out_strength = {n: sum(d["weight"] for _, _, d in G.out_edges(n, data=True)) for n in G.nodes()}
    node_sizes = [200 + 200 * out_strength.get(n, 0) for n in G.nodes()]

    # Edge styles by type
    colors = []
    widths = []
    for u, v, data in G.edges(data=True):
        ev_type = data.get("ev_type", "damage")
        if ev_type == "heal":
            colors.append("green")
        elif ev_type == "threat":
            colors.append("blue")
        else:
            colors.append("red")
        widths.append(0.5 + 1.5 * data.get("weight", 1.0))

    nx.draw_networkx_nodes(G, pos, node_size=node_sizes, node_color="#f0f0f0", edgecolors="#333")
    nx.draw_networkx_labels(G, pos, font_size=9)
    nx.draw_networkx_edges(G, pos, edge_color=colors, width=widths, arrows=True, arrowstyle="->", arrowsize=12)

    plt.title(title)
    plt.axis("off")
    plt.tight_layout()
    plt.savefig(output_path, dpi=200)
    plt.close()


def draw_graph_plotly(G: nx.DiGraph, output_path: str, title: str, layout_type: str = "kamada_kawai", use_3d: bool = False):
    try:
        import plotly.graph_objects as go
        import numpy as np
    except ImportError:
        raise SystemExit("plotly is not installed. Install with: pip install plotly")

    # Compute network metrics for richer visualization
    out_strength = {n: sum(d["weight"] for _, _, d in G.out_edges(n, data=True)) for n in G.nodes()}
    in_strength = {n: sum(d["weight"] for _, _, d in G.in_edges(n, data=True)) for n in G.nodes()}
    total_strength = {n: out_strength.get(n, 0) + in_strength.get(n, 0) for n in G.nodes()}
    
    # Centrality metrics
    try:
        degree_centrality = nx.degree_centrality(G.to_undirected())
        betweenness = nx.betweenness_centrality(G.to_undirected())
        closeness = nx.closeness_centrality(G.to_undirected())
    except:
        degree_centrality = {n: 0.0 for n in G.nodes()}
        betweenness = {n: 0.0 for n in G.nodes()}
        closeness = {n: 0.0 for n in G.nodes()}
    
    # Choose layout algorithm
    if layout_type == "kamada_kawai":
        try:
            pos = nx.kamada_kawai_layout(G.to_undirected())
        except:
            pos = nx.spring_layout(G, k=3, iterations=100, seed=42)
    elif layout_type == "circular":
        pos = nx.circular_layout(G)
    elif layout_type == "shell":
        pos = nx.shell_layout(G)
    elif layout_type == "spectral":
        try:
            pos = nx.spectral_layout(G.to_undirected())
        except:
            pos = nx.spring_layout(G, k=3, iterations=100, seed=42)
    else:  # spring or default
        pos = nx.spring_layout(G, k=3, iterations=100, seed=42)
    
    # Normalize positions
    if pos:
        xs = [pos[n][0] for n in G.nodes()]
        ys = [pos[n][1] for n in G.nodes()]
        x_range = max(xs) - min(xs) if max(xs) != min(xs) else 1
        y_range = max(ys) - min(ys) if max(ys) != min(ys) else 1
        pos = {n: ((pos[n][0] - min(xs)) / x_range * 2 - 1, (pos[n][1] - min(ys)) / y_range * 2 - 1) for n in G.nodes()}
    
    max_strength = max(total_strength.values()) if total_strength.values() else 1.0
    max_centrality = max(degree_centrality.values()) if degree_centrality.values() else 1.0
    
    # Node colors based on role/type
    def get_node_color(node):
        node_lower = str(node).lower()
        if "boss" in node_lower:
            return "#FF6B6B"  # Red
        elif "tank" in node_lower or "party member 1" in node_lower:
            return "#4ECDC4"  # Teal
        elif "threat" in node_lower:
            return "#95E1D3"  # Light teal
        elif "heal" in node_lower:
            return "#F38181"  # Light red
        else:
            return "#FFD93D"  # Yellow
    
    # Prepare edge traces with curved paths
    edge_traces = {"damage": [], "heal": [], "threat": []}
    
    for u, v, data in G.edges(data=True):
        x0, y0 = pos[u]
        x1, y1 = pos[v]
        w = data.get("weight", 1.0)
        ev_type = data.get("ev_type", "damage")
        
        # Create curved edge path
        mid_x, mid_y = (x0 + x1) / 2, (y0 + y1) / 2
        # Perpendicular offset for curve
        dx, dy = x1 - x0, y1 - y0
        perp_x, perp_y = -dy * 0.3, dx * 0.3
        curve_x = mid_x + perp_x
        curve_y = mid_y + perp_y
        
        edge_traces[ev_type].append((x0, y0, curve_x, curve_y, x1, y1, w))
    
    fig = go.Figure()
    
    # Add edges with curved paths
    for ev_type, edges in edge_traces.items():
        if not edges:
            continue
        
        x_edges = []
        y_edges = []
        edge_hover = []
        edge_widths = []
        
        for x0, y0, cx, cy, x1, y1, w in edges:
            # Create smooth curve using quadratic bezier
            t_values = np.linspace(0, 1, 20)
            for t in t_values:
                x = (1-t)**2 * x0 + 2*(1-t)*t * cx + t**2 * x1
                y = (1-t)**2 * y0 + 2*(1-t)*t * cy + t**2 * y1
                x_edges.append(x)
                y_edges.append(y)
            x_edges.append(None)
            y_edges.append(None)
            edge_hover.append(f"{ev_type.capitalize()}: {w:.1f}")
            edge_widths.append(max(1, min(8, 1 + 3 * w)))
        
        color = "#FF6B6B" if ev_type == "damage" else ("#51CF66" if ev_type == "heal" else "#4DABF7")
        
        fig.add_trace(go.Scatter(
            x=x_edges, y=y_edges,
            mode='lines',
            line=dict(width=2, color=color),
            hoverinfo='skip',
            showlegend=True,
            name=f"{ev_type.capitalize()} ({len(edges)} edges)",
            legendgroup=ev_type,
            opacity=0.6
        ))
    
    # Add nodes with rich information
    node_x = [pos[node][0] for node in G.nodes()]
    node_y = [pos[node][1] for node in G.nodes()]
    node_text = [str(node).replace("Party Member", "PM") for node in G.nodes()]
    
    # Node sizes based on total strength
    node_sizes = [max(15, min(60, 20 + 40 * (total_strength.get(node, 0) / max_strength))) for node in G.nodes()]
    
    # Node colors
    node_colors = [get_node_color(node) for node in G.nodes()]
    
    # Rich hover information
    node_info = []
    for node in G.nodes():
        info = f"<b>{node}</b><br>"
        info += f"Outgoing: {out_strength.get(node, 0):.1f}<br>"
        info += f"Incoming: {in_strength.get(node, 0):.1f}<br>"
        info += f"Total: {total_strength.get(node, 0):.1f}<br>"
        info += f"Degree Centrality: {degree_centrality.get(node, 0):.3f}<br>"
        info += f"Betweenness: {betweenness.get(node, 0):.3f}<br>"
        info += f"Closeness: {closeness.get(node, 0):.3f}"
        node_info.append(info)
    
    fig.add_trace(go.Scatter(
        x=node_x, y=node_y,
        mode='markers+text',
        marker=dict(
            size=node_sizes,
            color=node_colors,
            line=dict(width=3, color='#2C3E50'),
            opacity=0.9
        ),
        text=node_text,
        textposition="middle center",
        textfont=dict(size=11, color='#2C3E50', family='Arial Black'),
        hovertext=node_info,
        hoverinfo='text',
        showlegend=False,
        customdata=[degree_centrality.get(node, 0) for node in G.nodes()],
        hovertemplate='%{hovertext}<extra></extra>'
    ))
    
    # Add annotations for key metrics
    annotations = [
        dict(
            text=f"<b>{title}</b><br>Nodes: {len(G.nodes())} | Edges: {len(G.edges())}",
            showarrow=False,
            xref="paper", yref="paper",
            x=0.02, y=0.98,
            xanchor="left", yanchor="top",
            font=dict(size=14, color="#2C3E50"),
            bgcolor="rgba(255,255,255,0.8)",
            bordercolor="#2C3E50",
            borderwidth=1
        ),
        dict(
            text="Hover nodes for metrics | Drag to pan | Scroll to zoom",
            showarrow=False,
            xref="paper", yref="paper",
            x=0.5, y=-0.02,
            xanchor="center", yanchor="top",
            font=dict(size=10, color="gray")
        )
    ]
    
    fig.update_layout(
        title="",
        showlegend=True,
        hovermode='closest',
        margin=dict(b=40, l=20, r=20, t=20),
        annotations=annotations,
        xaxis=dict(showgrid=False, zeroline=False, showticklabels=False, range=[-1.2, 1.2]),
        yaxis=dict(showgrid=False, zeroline=False, showticklabels=False, range=[-1.2, 1.2]),
        plot_bgcolor='#F8F9FA',
        paper_bgcolor='white',
        legend=dict(
            x=0.02, y=0.02,
            bgcolor="rgba(255,255,255,0.8)",
            bordercolor="#2C3E50",
            borderwidth=1
        ),
        width=1200,
        height=800
    )
    
    fig.write_html(output_path)


def main():
    parser = argparse.ArgumentParser(description="Generate SNA image from episode JSON.")
    parser.add_argument("--input", "-i", required=True, help="Path to episode JSON")
    parser.add_argument("--output", "-o", default="episode_sna.png", help="Output PNG/HTML path")
    parser.add_argument("--title", "-t", default="Episode SNA", help="Title for the plot")
    parser.add_argument("--plotly", action="store_true", help="Output interactive HTML using plotly instead of PNG")
    parser.add_argument("--layout", choices=["spring", "kamada_kawai", "circular", "shell", "spectral"], 
                       default="kamada_kawai", help="Layout algorithm for network graph")
    parser.add_argument("--3d", dest="use_3d", action="store_true", help="Use 3D visualization (experimental)")
    args = parser.parse_args()

    data = load_episode(args.input)

    events = pick_event_list(data)
    if events:
        edge_list, nodes = infer_edges_from_events(events)
        mode = "events"
    else:
        edge_list, nodes = infer_edges_from_actions(data)
        mode = "actions (fallback)"

    if not edge_list:
        raise SystemExit("No edges found. Provide events or attack/heal actions in the JSON.")

    G = build_graph(edge_list)
    if args.plotly:
        draw_graph_plotly(G, args.output, f"{args.title} [{mode}]", 
                         layout_type=args.layout, use_3d=args.use_3d)
    else:
        draw_graph_matplotlib(G, args.output, f"{args.title} [{mode}]")

    fmt = "HTML" if args.plotly else "PNG"
    print(f"Generated SNA ({mode}) as {fmt} with {len(G.nodes())} nodes / {len(G.edges())} edges -> {os.path.abspath(args.output)}")


if __name__ == "__main__":
    main()

