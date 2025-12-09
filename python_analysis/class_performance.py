"""
Analyze class selection and performance
"""

import json
import os
import glob
from collections import defaultdict
import pandas as pd
import matplotlib.pyplot as plt

def load_episode(filepath):
    """Load an episode JSON file"""
    with open(filepath, 'r') as f:
        return json.load(f)

def analyze_class_performance(episodes):
    """Analyze performance by class"""
    class_stats = defaultdict(lambda: {
        'episodes': 0,
        'wins': 0,
        'attacks': 0,
        'heals': 0,
        'threat_boosts': 0
    })
    
    for episode in episodes:
        win_condition = episode.get('winCondition', '')
        # Handle both dict and list formats
        agent_classes = episode.get('agentClasses', {})
        if isinstance(agent_classes, list):
            agent_ids = episode.get('agentIds', [])
            agent_class_values = episode.get('agentClassValues', [])
            agent_classes = dict(zip(agent_ids, agent_class_values))
        actions = episode.get('actions', [])
        
        # Track which classes were in this episode
        episode_classes = set(agent_classes.values())
        
        for agent_class in episode_classes:
            if agent_class == 'Boss':
                continue
            
            class_stats[agent_class]['episodes'] += 1
            
            # Check if party won (this class was on winning team)
            if win_condition == 'party':
                class_stats[agent_class]['wins'] += 1
        
        # Count actions by class
        for action in actions:
            agent_id = action.get('agentId', '')
            agent_class = agent_classes.get(agent_id, 'Unknown')
            
            if agent_class == 'Boss':
                continue
            
            branch = action.get('branch', '')
            value = action.get('value', 0)
            
            if branch == 'attack' and value == 1:
                class_stats[agent_class]['attacks'] += 1
            elif branch == 'heal' and value == 1:
                class_stats[agent_class]['heals'] += 1
            elif branch == 'threat_boost' and value == 1:
                class_stats[agent_class]['threat_boosts'] += 1
    
    return class_stats

def plot_class_performance(class_stats, output_file="class_performance.png"):
    """Plot class performance metrics"""
    if not class_stats:
        print("No class data to plot")
        return
    
    classes = list(class_stats.keys())
    wins = [class_stats[c]['wins'] for c in classes]
    episodes = [class_stats[c]['episodes'] for c in classes]
    win_rates = [w / e * 100 if e > 0 else 0 for w, e in zip(wins, episodes)]
    attacks = [class_stats[c]['attacks'] for c in classes]
    heals = [class_stats[c]['heals'] for c in classes]
    
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    
    # Win rate
    axes[0, 0].bar(classes, win_rates, color=['blue', 'green', 'red', 'purple'][:len(classes)])
    axes[0, 0].set_title("Win Rate by Class")
    axes[0, 0].set_ylabel("Win Rate (%)")
    axes[0, 0].set_ylim(0, 100)
    
    # Total attacks
    axes[0, 1].bar(classes, attacks, color=['blue', 'green', 'red', 'purple'][:len(classes)])
    axes[0, 1].set_title("Total Attacks by Class")
    axes[0, 1].set_ylabel("Attack Count")
    
    # Heals (only for Healer)
    axes[1, 0].bar(classes, heals, color=['blue', 'green', 'red', 'purple'][:len(classes)])
    axes[1, 0].set_title("Total Heals by Class")
    axes[1, 0].set_ylabel("Heal Count")
    
    # Episodes participated
    axes[1, 1].bar(classes, episodes, color=['blue', 'green', 'red', 'purple'][:len(classes)])
    axes[1, 1].set_title("Episodes Participated")
    axes[1, 1].set_ylabel("Episode Count")
    
    plt.tight_layout()
    plt.savefig(output_file, dpi=300, bbox_inches='tight')
    print(f"Saved plot to {output_file}")
    plt.show()

def print_class_report(class_stats):
    """Print a text report of class performance"""
    print("\n=== Class Performance Report ===")
    
    for agent_class, stats in sorted(class_stats.items()):
        print(f"\n{agent_class}:")
        print(f"  Episodes: {stats['episodes']}")
        print(f"  Wins: {stats['wins']}")
        if stats['episodes'] > 0:
            win_rate = (stats['wins'] / stats['episodes']) * 100
            print(f"  Win Rate: {win_rate:.2f}%")
        print(f"  Total Attacks: {stats['attacks']}")
        print(f"  Total Heals: {stats['heals']}")
        print(f"  Total Threat Boosts: {stats['threat_boosts']}")

if __name__ == "__main__":
    data_dir = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany", "bossfight", "EpisodeData")
    
    if not os.path.exists(data_dir):
        data_dir = input("Enter path to EpisodeData: ").strip()
    
    episode_files = glob.glob(os.path.join(data_dir, "episode_*.json"))
    episodes = []
    
    for filepath in episode_files:
        try:
            episodes.append(load_episode(filepath))
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
    
    if episodes:
        class_stats = analyze_class_performance(episodes)
        print_class_report(class_stats)
        plot_class_performance(class_stats)
    else:
        print("No episodes loaded")

