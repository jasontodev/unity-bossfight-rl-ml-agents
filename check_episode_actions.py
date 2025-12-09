#!/usr/bin/env python3
"""Check if episode has any non-zero actions recorded"""

import json
import sys

if len(sys.argv) < 2:
    print("Usage: python check_episode_actions.py <episode.json>")
    sys.exit(1)

episode_file = sys.argv[1]

with open(episode_file, 'r') as f:
    data = json.load(f)

total_actions = len(data['actions'])
non_zero_actions = [a for a in data['actions'] if a['value'] != 0]

# Filter out invalid class_selection values (-1 and 4 are "no selection")
real_actions = [a for a in non_zero_actions if not (a['branch'] == 'class_selection' and (a['value'] == -1 or a['value'] == 4))]

print(f"Episode: {data['episode']}")
print(f"Duration: {data['duration']:.2f}s")
print(f"Win Condition: {data['winCondition']}")
print(f"\nTotal actions: {total_actions}")
print(f"Non-zero actions: {len(non_zero_actions)}")
print(f"Real actions (excluding invalid class_selection): {len(real_actions)}")
print(f"Zero actions: {total_actions - len(non_zero_actions)}")
print(f"Percentage real actions: {len(real_actions) / total_actions * 100:.2f}%")

if len(real_actions) > 0:
    print(f"\nFirst 30 real actions:")
    for i, action in enumerate(real_actions[:30]):
        print(f"  Frame {action['frame']}: {action['agentId']} - {action['branch']} = {action['value']}")
    
    # Group by agent
    print(f"\nReal actions by agent:")
    by_agent = {}
    for action in real_actions:
        agent = action['agentId']
        if agent not in by_agent:
            by_agent[agent] = []
        by_agent[agent].append(action)
    
    for agent, actions in by_agent.items():
        print(f"  {agent}: {len(actions)} non-zero actions")
        # Group by branch
        by_branch = {}
        for action in actions:
            branch = action['branch']
            if branch not in by_branch:
                by_branch[branch] = 0
            by_branch[branch] += 1
        for branch, count in by_branch.items():
            print(f"    {branch}: {count} actions")
else:
    print("\n⚠️  WARNING: No real actions found! The episode only recorded zeros or invalid class selections.")
    print("This means either:")
    print("  1. Agents weren't in HeuristicOnly mode")
    print("  2. Agents weren't selected via ManualControlManager")
    print("  3. No input was provided during recording")
    print("  4. Actions were recorded but all were zero (no keys pressed)")

