"""
Visualize damage over time by agent and class
"""

import json
import os
import glob
import matplotlib.pyplot as plt
import pandas as pd
from collections import defaultdict

def load_episode(filepath):
    """Load an episode JSON file"""
    with open(filepath, 'r') as f:
        return json.load(f)

def extract_damage_data(episodes):
    """Extract damage data from episodes"""
    # This would need to be enhanced to track actual damage events
    # For now, this is a placeholder structure
    damage_data = []
    
    for episode in episodes:
        episode_num = episode.get('episode', 0)
        actions = episode.get('actions', [])
        # Handle both dict and list formats
        agent_classes = episode.get('agentClasses', {})
        if isinstance(agent_classes, list):
            agent_ids = episode.get('agentIds', [])
            agent_class_values = episode.get('agentClassValues', [])
            agent_classes = dict(zip(agent_ids, agent_class_values))
        
        # Track attack actions as proxy for damage
        for action in actions:
            if action.get('branch') == 'attack' and action.get('value') == 1:
                agent_id = action.get('agentId', 'unknown')
                agent_class = agent_classes.get(agent_id, 'Unknown')
                frame = action.get('frame', 0)
                
                damage_data.append({
                    'episode': episode_num,
                    'frame': frame,
                    'agent': agent_id,
                    'class': agent_class,
                    'action': 'attack'
                })
    
    return pd.DataFrame(damage_data)

def plot_damage_over_time(df, output_file="damage_over_time.png"):
    """Plot damage over time"""
    if df.empty:
        print("No damage data to plot")
        return
    
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    
    # Plot 1: Damage by class over time
    if 'class' in df.columns:
        damage_by_class = df.groupby(['frame', 'class']).size().unstack(fill_value=0)
        damage_by_class.plot(ax=axes[0, 0], title="Attacks by Class Over Time")
        axes[0, 0].set_xlabel("Frame")
        axes[0, 0].set_ylabel("Attack Count")
        axes[0, 0].legend(title="Class")
    
    # Plot 2: Total attacks per episode
    attacks_per_episode = df.groupby('episode').size()
    attacks_per_episode.plot(ax=axes[0, 1], kind='bar', title="Total Attacks per Episode")
    axes[0, 1].set_xlabel("Episode")
    axes[0, 1].set_ylabel("Attack Count")
    
    # Plot 3: Attacks by agent
    if 'agent' in df.columns:
        attacks_by_agent = df.groupby('agent').size()
        attacks_by_agent.plot(ax=axes[1, 0], kind='bar', title="Total Attacks by Agent")
        axes[1, 0].set_xlabel("Agent")
        axes[1, 0].set_ylabel("Attack Count")
        axes[1, 0].tick_params(axis='x', rotation=45)
    
    # Plot 4: Class distribution
    if 'class' in df.columns:
        class_dist = df['class'].value_counts()
        class_dist.plot(ax=axes[1, 1], kind='pie', autopct='%1.1f%%', title="Class Distribution")
    
    plt.tight_layout()
    plt.savefig(output_file, dpi=300, bbox_inches='tight')
    print(f"Saved plot to {output_file}")
    plt.show()

if __name__ == "__main__":
    data_dir = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany", "bossfight", "EpisodeData")
    
    if not os.path.exists(data_dir):
        data_dir = input("Enter path to EpisodeData: ").strip()
    
    episode_files = glob.glob(os.path.join(data_dir, "episode_*.json"))
    episodes = []
    
    for filepath in episode_files[:10]:  # Limit to first 10 for testing
        try:
            episodes.append(load_episode(filepath))
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
    
    if episodes:
        df = extract_damage_data(episodes)
        plot_damage_over_time(df)
    else:
        print("No episodes loaded")

