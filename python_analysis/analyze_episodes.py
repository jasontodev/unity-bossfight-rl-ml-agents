"""
Analyze ML-Agents episode data
Loads episode JSONs and analyzes win rates, damage over time, etc.
"""

import json
import os
import glob
from collections import defaultdict
import pandas as pd

def load_episode(filepath):
    """Load an episode JSON file"""
    with open(filepath, 'r') as f:
        return json.load(f)

def analyze_episodes(data_dir="EpisodeData"):
    """Analyze all episodes in the data directory"""
    episode_files = glob.glob(os.path.join(data_dir, "episode_*.json"))
    
    if not episode_files:
        print(f"No episode files found in {data_dir}")
        return
    
    episodes = []
    for filepath in episode_files:
        try:
            episode = load_episode(filepath)
            episodes.append(episode)
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
    
    if not episodes:
        print("No valid episodes loaded")
        return
    
    # Analyze win rates
    win_conditions = defaultdict(int)
    for episode in episodes:
        win_conditions[episode.get('winCondition', 'unknown')] += 1
    
    print("\n=== Win Rate Analysis ===")
    total = len(episodes)
    for condition, count in win_conditions.items():
        percentage = (count / total) * 100
        print(f"{condition}: {count} ({percentage:.2f}%)")
    
    # Analyze episode durations
    durations = [episode.get('duration', 0) for episode in episodes]
    if durations:
        print(f"\n=== Episode Duration ===")
        print(f"Average: {sum(durations) / len(durations):.2f}s")
        print(f"Min: {min(durations):.2f}s")
        print(f"Max: {max(durations):.2f}s")
    
    # Analyze class distribution
    class_distribution = defaultdict(int)
    for episode in episodes:
        # Handle both dict and list formats
        agent_classes = episode.get('agentClasses', {})
        if isinstance(agent_classes, list):
            # Convert from list format if needed
            agent_ids = episode.get('agentIds', [])
            agent_class_values = episode.get('agentClassValues', [])
            agent_classes = dict(zip(agent_ids, agent_class_values))
        
        for agent_id, agent_class in agent_classes.items():
            if 'Party' in agent_id:
                class_distribution[agent_class] += 1
    
    print(f"\n=== Class Distribution ===")
    for agent_class, count in class_distribution.items():
        print(f"{agent_class}: {count}")
    
    # Analyze action counts
    action_counts = defaultdict(int)
    for episode in episodes:
        actions = episode.get('actions', [])
        for action in actions:
            action_counts[action.get('branch', 'unknown')] += 1
    
    print(f"\n=== Action Distribution ===")
    for branch, count in sorted(action_counts.items(), key=lambda x: x[1], reverse=True):
        print(f"{branch}: {count}")
    
    return episodes

if __name__ == "__main__":
    # Default to Unity's persistent data path structure
    # Adjust path as needed
    data_dir = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany", "bossfight", "EpisodeData")
    
    if not os.path.exists(data_dir):
        print(f"Data directory not found: {data_dir}")
        print("Please specify the correct path to EpisodeData directory")
        data_dir = input("Enter path to EpisodeData: ").strip()
    
    analyze_episodes(data_dir)

