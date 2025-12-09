"""
Social network analysis of agent interactions
Analyzes attack/heal relationships, threat networks, etc.
"""

import json
import os
import glob
import networkx as nx
import matplotlib.pyplot as plt
from collections import defaultdict

def load_episode(filepath):
    """Load an episode JSON file"""
    with open(filepath, 'r') as f:
        return json.load(f)

def build_interaction_network(episodes):
    """Build a network graph of agent interactions"""
    G = nx.DiGraph()
    
    for episode in episodes:
        actions = episode.get('actions', [])
        # Handle both dict and list formats
        agent_classes = episode.get('agentClasses', {})
        if isinstance(agent_classes, list):
            agent_ids = episode.get('agentIds', [])
            agent_class_values = episode.get('agentClassValues', [])
            agent_classes = dict(zip(agent_ids, agent_class_values))
        
        # Track attack relationships (who attacks whom)
        # This is simplified - would need actual target information
        for action in actions:
            if action.get('branch') == 'attack' and action.get('value') == 1:
                attacker = action.get('agentId', 'unknown')
                attacker_class = agent_classes.get(attacker, 'Unknown')
                
                # Add node for attacker
                if not G.has_node(attacker):
                    G.add_node(attacker, class=attacker_class)
                
                # In a full implementation, we'd track the target
                # For now, we'll create edges based on attack patterns
    
    return G

def analyze_threat_network(episodes):
    """Analyze threat generation network"""
    threat_edges = defaultdict(int)
    
    for episode in episodes:
        actions = episode.get('actions', [])
        # Handle both dict and list formats
        agent_classes = episode.get('agentClasses', {})
        if isinstance(agent_classes, list):
            agent_ids = episode.get('agentIds', [])
            agent_class_values = episode.get('agentClassValues', [])
            agent_classes = dict(zip(agent_ids, agent_class_values))
        
        # Track threat-generating actions
        for action in actions:
            agent_id = action.get('agentId', 'unknown')
            branch = action.get('branch', '')
            
            # Attacks generate threat
            if branch == 'attack' and action.get('value') == 1:
                threat_edges[agent_id] += 1
            
            # Healing generates threat
            if branch == 'heal' and action.get('value') == 1:
                threat_edges[agent_id] += 3  # 3x multiplier
    
    return threat_edges

def plot_network(G, output_file="agent_network.png"):
    """Plot the agent interaction network"""
    if len(G.nodes()) == 0:
        print("No network data to plot")
        return
    
    plt.figure(figsize=(12, 8))
    
    # Color nodes by class
    node_colors = []
    for node in G.nodes():
        class_name = G.nodes[node].get('class', 'Unknown')
        color_map = {
            'Tank': 'blue',
            'Healer': 'green',
            'MeleeDPS': 'red',
            'RangedDPS': 'purple',
            'Boss': 'orange',
            'Unknown': 'gray'
        }
        node_colors.append(color_map.get(class_name, 'gray'))
    
    pos = nx.spring_layout(G, k=1, iterations=50)
    nx.draw(G, pos, with_labels=True, node_color=node_colors, 
            node_size=2000, font_size=10, font_weight='bold',
            arrows=True, arrowsize=20, edge_color='gray')
    
    plt.title("Agent Interaction Network")
    plt.savefig(output_file, dpi=300, bbox_inches='tight')
    print(f"Saved network plot to {output_file}")
    plt.show()

def analyze_centrality(G):
    """Analyze network centrality metrics"""
    if len(G.nodes()) == 0:
        return
    
    print("\n=== Network Centrality Analysis ===")
    
    # Degree centrality
    degree_centrality = nx.degree_centrality(G)
    print("\nDegree Centrality (most connected):")
    for node, centrality in sorted(degree_centrality.items(), key=lambda x: x[1], reverse=True)[:5]:
        print(f"  {node}: {centrality:.3f}")
    
    # Betweenness centrality
    betweenness = nx.betweenness_centrality(G)
    print("\nBetweenness Centrality (most important bridges):")
    for node, centrality in sorted(betweenness.items(), key=lambda x: x[1], reverse=True)[:5]:
        print(f"  {node}: {centrality:.3f}")

if __name__ == "__main__":
    data_dir = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany", "bossfight", "EpisodeData")
    
    if not os.path.exists(data_dir):
        data_dir = input("Enter path to EpisodeData: ").strip()
    
    episode_files = glob.glob(os.path.join(data_dir, "episode_*.json"))
    episodes = []
    
    for filepath in episode_files[:10]:
        try:
            episodes.append(load_episode(filepath))
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
    
    if episodes:
        G = build_interaction_network(episodes)
        threat_network = analyze_threat_network(episodes)
        
        print(f"\n=== Threat Generation ===")
        for agent, threat in sorted(threat_network.items(), key=lambda x: x[1], reverse=True):
            print(f"{agent}: {threat}")
        
        analyze_centrality(G)
        plot_network(G)
    else:
        print("No episodes loaded")

