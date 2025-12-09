# ML-Agents Quick Start Guide

A quick reference for starting, managing, and analyzing ML-Agents training.

---

## 🚀 Starting Training

### Step 1: Create Training Scene
1. Open Unity Editor
2. Go to: **ML-Agents > Setup Training Scene**
3. Save the scene (e.g., `MLAgentsTrainingScene.unity`)

### Step 2: Verify Agent Configuration
- Check that all 5 agents have:
  - `BehaviorParameters` with **Behavior Type = Default**
  - `Behavior Name` = `PartyMemberAgent` (for party) or `BossAgent` (for boss)
  - `DecisionRequester` component attached

### Step 3: Start Unity Environment
1. Open the training scene
2. Press **Play** in Unity Editor
3. Unity will wait for Python trainer to connect

### Step 4: Start Python Trainer
Open a terminal in the project root and run:
```bash
mlagents-learn ml-agents.yaml --run-id=bossfight_training
```

**Note**: If `ml-agents.yaml` doesn't exist yet, you'll need to create it first (see "Configuration" section below).

---

## ⏸️ Pausing Training

### Method 1: Stop Python Trainer
- Press `Ctrl+C` in the terminal running `mlagents-learn`
- Training will pause and save current progress
- Unity will continue running but won't train

### Method 2: Stop Unity
- Press **Stop** in Unity Editor
- Python trainer will detect disconnection and pause

---

## ▶️ Resuming Training

### Using the `--resume` Flag
To resume from the last checkpoint:
```bash
mlagents-learn ml-agents.yaml --run-id=bossfight_training --resume
```

This will:
- Load the latest model checkpoint
- Continue training from where it left off
- Preserve all training statistics

### Manual Model Loading
1. Find the trained model in: `results/bossfight_training/`
2. Copy the `.onnx` file to `Assets/Models/`
3. In Unity, select each agent's `BehaviorParameters` component
4. Set **Behavior Type** to **Inference Only**
5. Assign the model to the **Model** field

---

## 📊 Viewing Training Graphs

### Real-Time TensorBoard
Training automatically starts TensorBoard. Access it at:
```
http://localhost:6006
```

Or manually start TensorBoard:
```bash
tensorboard --logdir=results/bossfight_training
```

### What You'll See
- **Cumulative Reward**: Average reward per episode
- **Policy Loss**: How much the policy is changing
- **Value Loss**: How well value estimates match returns
- **Episode Length**: Average episode duration
- **Entropy**: Exploration vs exploitation balance

### Graph Overlay
To compare multiple training runs:
```bash
tensorboard --logdir=results/
```
This shows all runs in the `results/` folder for comparison.

---

## 📈 Post-Training Analysis

### Episode Data Location
Episode recordings are saved to:
```
[Unity Persistent Data Path]/EpisodeData/
```

On Windows, typically:
```
C:\Users\[YourName]\AppData\LocalLow\[CompanyName]\[ProjectName]\EpisodeData\
```

### Python Analysis Scripts
Navigate to `python_analysis/` and run:

#### Win Rate Analysis
```bash
python analyze_episodes.py
```
Shows:
- Win rates (party vs boss)
- Average episode length
- Class distribution

#### Damage Over Time
```bash
python visualize_damage.py
```
Creates graphs showing:
- Damage dealt by each agent
- Damage by class
- Damage over time

#### Social Network Analysis
```bash
python network_analysis.py
```
Generates:
- Attack/heal relationship networks
- Threat relationship graphs
- Interaction matrices

#### Class Performance
```bash
python class_performance.py
```
Analyzes:
- Class selection rates
- Performance by class
- Class-specific win rates

---

## 🗑️ Deleting Training Data

### Delete All Training Data
```bash
# Windows PowerShell
Remove-Item -Recurse -Force results\bossfight_training

# Linux/Mac
rm -rf results/bossfight_training
```

### Delete Specific Run
```bash
# Windows PowerShell
Remove-Item -Recurse -Force results\[run-id]

# Linux/Mac
rm -rf results/[run-id]
```

### Delete Episode Recordings
Delete the `EpisodeData/` folder in Unity's persistent data path.

---

## ⚙️ Configuration

### Creating ml-agents.yaml
If the config file doesn't exist, create `ml-agents.yaml` in the project root:

```yaml
behaviors:
  PartyMemberAgent:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 5000000
    time_horizon: 64
    summary_freq: 10000

  BossAgent:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 5000000
    time_horizon: 64
    summary_freq: 10000
```

---

## 🔍 Troubleshooting

### Training Not Starting
- **Check**: Unity is in Play mode
- **Check**: Behavior Type is set to **Default** (not Heuristic Only)
- **Check**: Behavior names match YAML config (`PartyMemberAgent`, `BossAgent`)
- **Check**: Python trainer is running and connected

### No Graphs Appearing
- **Check**: TensorBoard is running (`tensorboard --logdir=results/`)
- **Check**: Training has run for at least one summary period (default: 10000 steps)
- **Check**: Browser is pointing to `http://localhost:6006`

### Can't Resume Training
- **Check**: `--run-id` matches the original training run
- **Check**: `results/[run-id]/` folder exists
- **Check**: Model checkpoints exist in the run folder

### Episode Data Not Saving
- **Check**: `EpisodeRecorder` component is in the scene
- **Check**: Unity has write permissions to persistent data path
- **Check**: Console for any error messages

---

## 📝 Quick Commands Reference

```bash
# Start training
mlagents-learn ml-agents.yaml --run-id=bossfight_training

# Resume training
mlagents-learn ml-agents.yaml --run-id=bossfight_training --resume

# Start TensorBoard
tensorboard --logdir=results/

# View all runs
tensorboard --logdir=results/

# Delete training data
rm -rf results/bossfight_training  # Linux/Mac
Remove-Item -Recurse -Force results\bossfight_training  # Windows

# Run analysis
cd python_analysis
python analyze_episodes.py
python visualize_damage.py
python network_analysis.py
python class_performance.py
```

---

## 🎯 Next Steps After Training

1. **Evaluate Model**: Load trained model in Unity and test in Play mode
2. **Analyze Results**: Run Python analysis scripts to understand agent behavior
3. **Tune Hyperparameters**: Adjust `ml-agents.yaml` based on training curves
4. **Compare Runs**: Use TensorBoard to overlay multiple training runs
5. **Export for Research**: Use episode data for research paper analysis

---

## 📚 Additional Resources

- **Full Setup Guide**: See `ML_AGENTS_SETUP_GUIDE.md`
- **Training Details**: See `ML_AGENTS_TRAINING_GUIDE.md`
- **Action Branches**: See `ACTION_BRANCHES_AND_TRAINING_FEATURES.md`
- **Unity ML-Agents Docs**: https://github.com/Unity-Technologies/ml-agents

---

**Last Updated**: Based on ML-Agents 2.0.2 implementation

