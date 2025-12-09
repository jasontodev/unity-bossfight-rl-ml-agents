# ML-Agents Training Guide

## Overview

The boss fight game has been converted from heuristic-only mode to full ML-Agents training mode. Agents can now be trained using reinforcement learning with the Python trainer.

## Changes Made

### 1. Removed Heuristic-Only Forcing
- **PartyMemberAgent.cs** and **BossAgent.cs**: Removed code that forced `BehaviorType.HeuristicOnly`
- Agents now use `BehaviorType.Default`, which will:
  - Use the remote Python trainer if available (training mode)
  - Fall back to inference with a trained model if no trainer is connected
  - Fall back to heuristic if neither trainer nor model is available

### 2. Re-enabled ML-Driven Class Selection
- **PartyMemberAgent.cs**: ML-Agents can now select classes (Tank, Healer, RangedDPS, MeleeDPS) via action branch 5
- Class selection only works if the agent hasn't selected a class yet and isn't locked

### 3. Manual Control Manager Updates
- **ManualControlManager.cs**: Only active when agents are in heuristic mode
- Automatically detects if any agent is in heuristic mode
- Shows "ML-Agents Training Mode Active" when training is running
- Manual control (1-5 keys) only works in heuristic mode

### 4. Behavior Names
- Updated scene setup to use "PartyMemberAgent" and "BossAgent" to match the YAML configuration

## Training Configuration

The training configuration is in `ml-agents.yaml` at the project root. It includes:

- **PartyMemberAgent**: PPO trainer with 164 observations, 6 discrete action branches
- **BossAgent**: PPO trainer with 164 observations, 5 discrete action branches

## How to Train

### 1. Setup the Scene
1. In Unity, go to **ML-Agents > Setup Training Scene**
2. This creates the arena with boss, 4 party members, and 3 walls
3. All agents are configured with ML-Agents components

### 2. Start Training
1. Open a terminal/command prompt
2. Navigate to the project root directory
3. Run the ML-Agents training command:
   ```bash
   mlagents-learn ml-agents.yaml --run-id=boss-fight-training
   ```
4. Press Play in Unity when prompted

### 3. Monitor Training
- Training statistics will appear in the terminal
- Episode data is saved to `Application.persistentDataPath/EpisodeData/`
- Models are saved periodically during training

### 4. Use Trained Models
1. After training, copy the `.onnx` model files from the `results/` directory
2. In Unity, select each agent (Boss and Party Members)
3. In the **Behavior Parameters** component:
   - Set **Behavior Type** to **Inference Only**
   - Assign the trained model to the **Model** field

## Switching Between Modes

### Heuristic Mode (Manual Testing)
1. Select each agent in the Unity Inspector
2. In **Behavior Parameters**, set **Behavior Type** to **Heuristic Only**
3. Press Play - you can manually control agents with 1-5 keys

### Training Mode (Default)
1. Leave **Behavior Type** as **Default** (or don't set it)
2. Start the Python trainer
3. Press Play - agents will be controlled by the trainer

### Inference Mode (Using Trained Models)
1. Select each agent
2. Set **Behavior Type** to **Inference Only**
3. Assign the trained model to the **Model** field
4. Press Play - agents will use the trained model

## Action Spaces

### Party Member Actions (6 branches)
- **0**: Movement (0=no move, 1=forward, 2=backward)
- **1**: Rotation (0=no rotate, 1=left, 2=right)
- **2**: Attack (0=no attack, 1=attack)
- **3**: Heal (0=no heal, 1=heal) - Healer only
- **4**: Threat Boost (0=no boost, 1=boost) - Tank only
- **5**: Class Selection (0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS)

### Boss Actions (5 branches)
- **0**: Movement (0=no move, 1=forward, 2=backward)
- **1**: Rotation (0=no rotate, 1=left, 2=right)
- **2**: Attack (0=no attack, 1=attack)
- **3**: Wall Pickup (0=no pickup, 1=pickup)
- **4**: Wall Place (0=no place, 1=place)

## Observations

Each agent observes:
- **Self state**: Position, velocity, health, class, threat, cooldowns, status (lava, burning, dead)
- **LIDAR data**: 30 rays with distance, hit type, and entity type
- **Other agents**: Position, velocity, health, class, threat (relative to self)
- **Walls**: Position, velocity, size, carried status (relative to self)
- **Total**: 164 observations per agent

## Rewards

- **Sparse rewards only**: +1 for winning side, -1 for losing side at episode end
- **Party wins**: Boss health <= 0
- **Boss wins**: All party members dead
- **No intermediate rewards**: Pure reinforcement learning

## Episode Management

- Episodes automatically reset when win conditions are met
- All agents and walls return to spawn positions
- Health, threat, and class selections are reset
- Episode data is recorded for analysis and replay

## Troubleshooting

### Agents Not Training
- Check that the Python trainer is running and connected
- Verify behavior names match between Unity and YAML config
- Ensure **Behavior Type** is set to **Default** (not Heuristic Only)

### Manual Control Not Working
- Make sure at least one agent has **Behavior Type** set to **Heuristic Only**
- Check that ManualControlManager is in the scene

### Class Selection Not Working
- ML-Agents can only select classes if the agent hasn't selected one yet
- Once a class is selected, it's locked for that episode
- Class selection happens via action branch 5 (values 0-3)

## Next Steps

1. Start training with the default configuration
2. Monitor training progress and adjust hyperparameters if needed
3. Experiment with different reward structures (if desired)
4. Analyze episode data using the Python analysis tools
5. Use trained models for inference and gameplay

