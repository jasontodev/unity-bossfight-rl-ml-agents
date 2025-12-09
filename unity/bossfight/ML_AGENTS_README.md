# ML-Agents Implementation Guide

## Overview

This Unity project has been converted to use Unity ML-Agents 2.0+ for reinforcement learning. The game features 5 ML-Agents: 4 party members and 1 boss, all controlled by ML-Agents with custom observation encoding based on LIDAR and entity embeddings.

## Setup

### 1. ML-Agents Package
- ML-Agents 2.0.2 is already installed in the project
- Python dependencies: Install via `pip install -r python_analysis/requirements.txt`

### 2. Create Training Scene
- In Unity Editor, go to: **Boss Fight > Setup ML-Agents Training Scene**
- This will create:
  - Arena with lava and void
  - Boss in center, facing -Z
  - 4 party members in a group, facing +Z (toward boss)
  - 3 walls positioned around boss
  - EpisodeManager
  - EpisodeRecorder
  - All ML-Agent components

## Agent Configuration

### PartyMemberAgent
- **Action Space**: 6 discrete branches
  - Movement: 0=no move, 1=forward, 2=backward
  - Rotation: 0=no rotate, 1=left, 2=right
  - Attack: 0=no attack, 1=attack
  - Heal: 0=no heal, 1=heal (Healer only)
  - Threat Boost: 0=no boost, 1=boost (Tank only)
  - Class Selection: 0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS

### BossAgent
- **Action Space**: 5 discrete branches
  - Movement: 0=no move, 1=forward, 2=backward
  - Rotation: 0=no rotate, 1=left, 2=right
  - Attack: 0=no attack, 1=attack
  - Wall Pickup: 0=no pickup, 1=pickup
  - Wall Place: 0=no place, 1=place

## Observation System

Observations include:
- **Self observations**: Position, velocity, health, class, threat, cooldowns, status
- **LIDAR observations**: 30 rays with distances, hit types, entity types
- **Entity observations**: Other agents (position, health, class, threat), walls (position, size)
- **Masked attention**: Visibility masks based on frontal vision cone and line of sight

## Reward System

- **Sparse rewards only**: +1/-1 at episode end
- **Party wins**: +1 for all party members, -1 for boss
- **Boss wins**: +1 for boss, -1 for all party members
- **No intermediate rewards**: Pure reinforcement learning

## Episode Management

- **Episode ends when**:
  - Boss health <= 0 (party wins)
  - All party members dead (boss wins)
  - Maximum episode length reached (timeout)
- **Auto-reset**: Episodes automatically reset after completion
- **Time scale**: Adjustable for faster training

## Episode Recording & Replay

### Recording
- Episodes are automatically recorded to JSON and binary formats
- Saved to: `Application.persistentDataPath/EpisodeData/`
- Snapshots saved every 1000 episodes (configurable)

### Replay
- Use `EpisodeReplay.cs` component to load and replay episodes
- Supports frame-by-frame playback, pause, speed control

## Data Analysis

Python scripts in `python_analysis/`:
- `analyze_episodes.py`: Win rates, durations, class distribution
- `visualize_damage.py`: Damage over time by agent and class
- `network_analysis.py`: Social network analysis of interactions
- `class_performance.py`: Class selection and performance metrics
- `episode_replay.py`: Visualize episode replays

## Training

### Configuration
- Training config: `ml-agents.yaml`
- Uses PPO algorithm
- Hyperparameters optimized for multi-agent learning

### Training Command
```bash
mlagents-learn ml-agents.yaml --run-id=bossfight_training
```

## Key Components

### Scripts
- `PartyMemberAgent.cs`: ML-Agent for party members
- `BossAgent.cs`: ML-Agent for boss
- `EpisodeManager.cs`: Manages episode lifecycle
- `ObservationEncoder.cs`: Encodes observations
- `EpisodeRecorder.cs`: Records episodes
- `EpisodeReplay.cs`: Replays episodes

### Modified Scripts
- `HealthSystem.cs`: Added IsDead, Despawn(), Respawn()
- `LIDARSystem.cs`: Added observation data methods
- Controllers: Modified to work with ML-Agents (skip Input when Agent present)

## Notes

- Agents can choose their class (learned behavior)
- Dead agents despawn (invisible, non-interactive)
- Episode recording happens automatically
- All actions are recorded for analysis and replay

