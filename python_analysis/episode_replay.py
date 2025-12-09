"""
Load and visualize episode replays
"""

import json
import os
import matplotlib.pyplot as plt
import matplotlib.animation as animation
from collections import defaultdict

def load_episode(filepath):
    """Load an episode JSON file"""
    with open(filepath, 'r') as f:
        return json.load(f)

def extract_positions_over_time(episode):
    """Extract agent positions over time from episode"""
    # This is a placeholder - would need actual position data in episodes
    # Position tracking would need to be added to episode recording
    
    positions = defaultdict(list)
    frames = []
    
    actions = episode.get('actions', [])
    agent_classes = episode.get('agentClasses', {})
    
    # Group actions by frame
    actions_by_frame = defaultdict(list)
    for action in actions:
        frame = action.get('frame', 0)
        actions_by_frame[frame].append(action)
    
    # Extract positions from episode data (requires position tracking in episode recording)
    for frame in sorted(actions_by_frame.keys()):
        frames.append(frame)
        for agent_id in agent_classes.keys():
            # Placeholder: would extract actual positions from episode data
            positions[agent_id].append((0, 0, 0))
    
    return positions, frames

def visualize_episode(episode, output_file="episode_replay.gif"):
    """Create a visualization of the episode"""
    positions, frames = extract_positions_over_time(episode)
    
    if not positions:
        print("No position data available")
        return
    
    fig, ax = plt.subplots(figsize=(10, 10))
    
    # Set up arena bounds
    ax.set_xlim(-5, 5)
    ax.set_ylim(-5, 5)
    ax.set_aspect('equal')
    ax.set_title(f"Episode {episode.get('episode', 0)} Replay")
    ax.grid(True)
    
    # Draw arena
    arena = plt.Rectangle((-5, -5), 10, 10, fill=False, edgecolor='green', linewidth=2)
    ax.add_patch(arena)
    
    # Color map for classes
    color_map = {
        'Tank': 'blue',
        'Healer': 'green',
        'MeleeDPS': 'red',
        'RangedDPS': 'purple',
        'Boss': 'orange'
    }
    
    # Handle both dict and list formats
    agent_classes = episode.get('agentClasses', {})
    if isinstance(agent_classes, list):
        agent_ids = episode.get('agentIds', [])
        agent_class_values = episode.get('agentClassValues', [])
        agent_classes = dict(zip(agent_ids, agent_class_values))
    
    def animate(frame_idx):
        ax.clear()
        ax.set_xlim(-5, 5)
        ax.set_ylim(-5, 5)
        ax.set_aspect('equal')
        ax.set_title(f"Episode {episode.get('episode', 0)} - Frame {frames[frame_idx] if frame_idx < len(frames) else 0}")
        ax.grid(True)
        
        # Draw arena
        arena = plt.Rectangle((-5, -5), 10, 10, fill=False, edgecolor='green', linewidth=2)
        ax.add_patch(arena)
        
        # Draw agents
        for agent_id, pos_list in positions.items():
            if frame_idx < len(pos_list):
                pos = pos_list[frame_idx]
                agent_class = agent_classes.get(agent_id, 'Unknown')
                color = color_map.get(agent_class, 'gray')
                
                ax.scatter(pos[0], pos[2], c=color, s=200, label=agent_id if frame_idx == 0 else "")
        
        if frame_idx == 0:
            ax.legend()
    
    if frames:
        anim = animation.FuncAnimation(fig, animate, frames=min(len(frames), 100), 
                                      interval=100, repeat=True)
        anim.save(output_file, writer='pillow', fps=10)
        print(f"Saved animation to {output_file}")
    else:
        print("No frames to animate")

if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1:
        episode_file = sys.argv[1]
    else:
        data_dir = os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany", "bossfight", "EpisodeData")
        episode_file = os.path.join(data_dir, "episode_0.json")
    
    if not os.path.exists(episode_file):
        episode_file = input("Enter path to episode JSON file: ").strip()
    
    if os.path.exists(episode_file):
        episode = load_episode(episode_file)
        print(f"Loaded episode {episode.get('episode', 0)}")
        print(f"Win condition: {episode.get('winCondition', 'unknown')}")
        print(f"Duration: {episode.get('duration', 0):.2f}s")
        print(f"Actions: {len(episode.get('actions', []))}")
        
        visualize_episode(episode)
    else:
        print(f"Episode file not found: {episode_file}")

