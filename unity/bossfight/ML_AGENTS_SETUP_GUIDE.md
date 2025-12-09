# ML-Agents Setup and Usage Guide

## Table of Contents
1. [Initial Setup](#initial-setup)
2. [Creating the Training Scene](#creating-the-training-scene)
3. [Starting Training](#starting-training)
4. [Episode Recording and Saving](#episode-recording-and-saving)
5. [Visualizing Data](#visualizing-data)
6. [Replaying Episodes](#replaying-episodes)
7. [Continuing from Checkpoint](#continuing-from-checkpoint)
8. [Resetting Progress](#resetting-progress)

---

## Initial Setup

### 1. Unity Project Setup
- Open the Unity project in Unity Editor (2022.3.62f3 or compatible)
- ML-Agents 2.0.2 package is already installed
- Ensure the project uses Universal Render Pipeline (URP)

### 2. Python Environment Setup
1. Install Python 3.8 or higher
2. Install ML-Agents and dependencies:
   ```bash
   pip install mlagents
   pip install torch
   ```
3. Install data analysis dependencies:
   ```bash
   cd python_analysis
   pip install -r requirements.txt
   ```
   Or manually:
   ```bash
   pip install pandas matplotlib networkx seaborn pillow
   ```

---

## Creating the Training Scene

### Step 1: Create the ML-Agents Scene
1. In Unity Editor, go to: **Boss Fight > Setup ML-Agents Training Scene**
2. This will automatically create:
   - 20x20 arena (2x larger than original)
   - Red lava surrounding the arena
   - Void area below (instant death)
   - Boss in center, facing +Z (toward party)
   - 4 party members in a 2x2 formation on -Z side, facing -Z (toward boss)
   - 3 walls positioned around the boss
   - EpisodeManager
   - EpisodeRecorder
   - All ML-Agent components configured

### Step 2: Verify Scene Setup
- Check that all 5 agents (1 boss + 4 party members) have:
  - `BossAgent` or `PartyMemberAgent` component
  - `DecisionRequester` component
  - `BehaviorParameters` component (auto-added by ML-Agents)
  - All required systems (HealthSystem, LIDARSystem, etc.)

### Step 3: Save the Scene
- Save the scene as `MLAgentsTrainingScene.unity`
- This scene will be used for training

---

## Starting Training

### Method 1: Training from Unity Editor
1. Open the `MLAgentsTrainingScene.unity` scene
2. Press Play in Unity Editor
3. Agents will use heuristic controllers (if configured) or random actions
4. To train with Python, you need to use the command line (see Method 2)

### Method 2: Training with Python (Recommended)
1. Open a terminal/command prompt
2. Navigate to the project root directory:
   ```bash
   cd "D:\Gamer to Developer\Code\bossv2"
   ```
3. Start training:
   ```bash
   mlagents-learn ml-agents.yaml --run-id=bossfight_training --env=unity/bossfight
   ```
4. In Unity, press Play to start the training environment
5. Training will begin automatically when Unity connects to Python

### Training Configuration
- Configuration file: `ml-agents.yaml`
- Training algorithm: PPO (Proximal Policy Optimization)
- Decision frequency: 20 actions/second (configurable in DecisionRequester)
- Max steps: 50,000,000 per behavior
- Time horizon: 64 steps

---

## Episode Recording and Saving

### How Episodes are Saved
Episodes are automatically recorded by the `EpisodeRecorder` component:

**Save Location:**
- Windows: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\bossfight\EpisodeData\`
- Or: `C:\Users\[YourUsername]\AppData\LocalLow\DefaultCompany\bossfight\EpisodeData\`

**File Formats:**
- **JSON**: `episode_0.json`, `episode_1.json`, etc. (human-readable, for analysis)
- **Binary**: `episode_0.bin`, `episode_1.bin`, etc. (compact, for replay)

**What Gets Saved:**
- Episode number
- Win condition (party/boss/timeout)
- Episode duration
- Agent classes for each agent
- All actions taken by each agent (frame, agent ID, action branch, action value)

**Snapshot System:**
- Snapshots are saved every 1000 episodes (configurable in EpisodeRecorder)
- Snapshot files: `snapshot_1000.json`, `snapshot_2000.json`, etc.
- Contains: episode number, timestamp, training notes

### Configuring Recording
In Unity Editor, select the "Episode Recorder" GameObject:
- **Record Episodes**: Enable/disable recording
- **Save Every N Episodes**: Change snapshot frequency (default: 1000)
- **Save Directory**: Change save location (default: "EpisodeData")

---

## Visualizing Data

### Step 1: Locate Episode Data
1. Navigate to the save directory (see above)
2. Episode JSON files are saved here: `episode_0.json`, `episode_1.json`, etc.

### Step 2: Run Analysis Scripts

#### Option A: Analyze All Episodes
```bash
cd python_analysis
python analyze_episodes.py
```
This will:
- Show win rates (party vs boss)
- Display episode durations
- Show class distribution
- Display action statistics

**Note:** You may need to update the path in the script to point to your EpisodeData directory.

#### Option B: Visualize Damage Over Time
```bash
python visualize_damage.py
```
Creates plots showing:
- Attacks by class over time
- Total attacks per episode
- Attacks by agent
- Class distribution

Output: `damage_over_time.png`

#### Option C: Network Analysis
```bash
python network_analysis.py
```
Analyzes:
- Agent interaction networks
- Threat generation patterns
- Network centrality metrics

Output: `agent_network.png`

#### Option D: Class Performance
```bash
python class_performance.py
```
Shows:
- Win rates by class
- Total attacks/heals by class
- Class participation statistics

Output: `class_performance.png`

### Step 3: Customize Analysis
Edit the Python scripts to:
- Change data directory path
- Adjust visualization styles
- Add custom metrics
- Export to CSV for Excel analysis

---

## Replaying Episodes

### Method 1: Using EpisodeReplay Component (In Unity)

1. **Add EpisodeReplay Component:**
   - In Unity Editor, select any GameObject in the scene
   - Add Component > `EpisodeReplay`

2. **Load an Episode:**
   - In the Inspector, the component has replay controls
   - Or call `LoadEpisode("episode_0.json")` from code

3. **Control Playback:**
   - `StartReplay()`: Begin playback
   - `StopReplay()`: Stop playback
   - `PauseReplay()`: Pause at current frame
   - `ResumeReplay()`: Resume from pause
   - `StepFrame()`: Advance one frame

4. **Playback Settings:**
   - **Playback Speed**: Adjust replay speed (1.0 = normal, 2.0 = 2x speed)
   - **Pause On Frame**: Enable frame-by-frame stepping

### Method 2: Using Python Visualization
```bash
cd python_analysis
python episode_replay.py episode_0.json
```
This creates an animated visualization of the episode.

### Method 3: Manual Replay (Advanced)
1. Load episode JSON file
2. Parse actions by frame
3. Recreate scene state
4. Execute actions frame-by-frame

**Example Code:**
```csharp
EpisodeReplay replay = FindObjectOfType<EpisodeReplay>();
replay.LoadEpisode("episode_1234.json");
replay.StartReplay();
```

---

## Continuing from Checkpoint

### Method 1: Continue Training from Checkpoint

1. **Locate Checkpoint Files:**
   - Checkpoints are saved by ML-Agents during training
   - Location: `results/[run-id]/[behavior-name]/`
   - Files: `checkpoint.pt`, `checkpoint-[step].pt`

2. **Resume Training:**
   ```bash
   mlagents-learn ml-agents.yaml --run-id=bossfight_training --resume
   ```
   This will automatically load the latest checkpoint.

3. **Load Specific Checkpoint:**
   ```bash
   mlagents-learn ml-agents.yaml --run-id=bossfight_training --load
   ```
   Then specify the checkpoint file when prompted.

### Method 2: Load Trained Model in Unity

1. **Export Model:**
   - After training, models are saved as `.onnx` files
   - Location: `results/[run-id]/[behavior-name]/[behavior-name].onnx`

2. **Import to Unity:**
   - Copy the `.onnx` file to `Assets/Models/` (create folder if needed)
   - In Unity, select an agent's `BehaviorParameters` component
   - Set **Behavior Type** to **Inference Only**
   - Drag the `.onnx` model to the **Model** field

3. **Test the Model:**
   - Press Play in Unity
   - Agents will use the trained model for decisions

### Method 3: Continue from Episode Snapshot

1. **Load Snapshot Data:**
   - Snapshots contain episode statistics and metadata
   - Use Python scripts to analyze snapshot data
   - Recreate training conditions from snapshot

2. **Resume Recording:**
   - EpisodeRecorder continues from last episode number
   - No manual intervention needed
   - Episodes are numbered sequentially

---

## Resetting Progress

### Option 1: Reset Episode Data Only

**Delete Episode Files:**
1. Navigate to: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\bossfight\EpisodeData\`
2. Delete all `episode_*.json` and `episode_*.bin` files
3. Delete all `snapshot_*.json` files
4. EpisodeRecorder will start from episode 0 again

**Via Code:**
```csharp
// In Unity Editor Console or custom script
string dataPath = Path.Combine(Application.persistentDataPath, "EpisodeData");
if (Directory.Exists(dataPath))
{
    Directory.Delete(dataPath, true);
    Directory.CreateDirectory(dataPath);
}
```

### Option 2: Reset Training Progress (ML-Agents Checkpoints)

**Delete Training Results:**
1. Navigate to: `results/` directory (in project root or ML-Agents installation)
2. Delete the folder for your run-id: `results/bossfight_training/`
3. This removes all checkpoints and trained models
4. Start training fresh with a new run-id

**Command Line:**
```bash
# Windows PowerShell
Remove-Item -Recurse -Force "results\bossfight_training"

# Or manually delete the folder
```

### Option 3: Complete Reset (Everything)

**Delete All Data:**
1. **Episode Data:**
   - Delete: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\bossfight\EpisodeData\`

2. **Training Results:**
   - Delete: `results/bossfight_training/` (or entire `results/` folder)

3. **Unity PlayerPrefs (if any):**
   - In Unity Editor: Edit > Project Settings > Player > Other Settings
   - Or delete: `HKEY_CURRENT_USER\Software\[CompanyName]\[ProductName]` in Registry

4. **Scene Reset:**
   - Delete the scene and recreate using: **Boss Fight > Setup ML-Agents Training Scene**

### Option 4: Reset Individual Components

**Reset EpisodeManager:**
- In Unity, select the "Episode Manager" GameObject
- The `StartEpisode()` method will reset all agents to spawn positions

**Reset Threat System:**
```csharp
ThreatSystem.Instance.ClearAllThreat();
```

**Reset All Agents:**
- Use EpisodeManager's `StartEpisode()` method
- Or manually call `Respawn()` on each agent's HealthSystem

### Option 5: Nuclear Reset (Game-Breaking Bug)

If you encounter a game-breaking bug that requires a complete reset:

1. **Stop Training:**
   - Stop the Python training process (Ctrl+C)
   - Stop Unity Play mode

2. **Delete All Data:**
   ```powershell
   # Episode data
   Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Low\DefaultCompany\bossfight\EpisodeData"
   
   # Training results (adjust path as needed)
   Remove-Item -Recurse -Force "results\bossfight_training"
   ```

3. **Reset Scene:**
   - Delete existing "Boss Fight Arena" GameObject
   - Run: **Boss Fight > Setup ML-Agents Training Scene** again

4. **Clear Unity Cache (if needed):**
   - Close Unity
   - Delete `Library/` folder (Unity will regenerate)
   - Reopen Unity (will take longer to load)

5. **Start Fresh:**
   - Begin training with a new run-id:
   ```bash
   mlagents-learn ml-agents.yaml --run-id=bossfight_training_v2
   ```

---

## Troubleshooting

### Episodes Not Saving
- Check that `EpisodeRecorder` component exists in scene
- Verify `recordEpisodes` is enabled in Inspector
- Check file permissions for save directory
- Look for errors in Unity Console

### Python Scripts Can't Find Data
- Update the path in Python scripts to match your EpisodeData location
- Default path assumes: `AppData\LocalLow\DefaultCompany\bossfight\EpisodeData`
- Check `Application.persistentDataPath` in Unity to find actual path

### Training Not Starting
- Ensure Unity is in Play mode
- Check that Python training process is running
- Verify `ml-agents.yaml` is in the correct location
- Check Unity Console for connection errors

### Agents Not Learning
- Verify rewards are being assigned (check EpisodeManager)
- Ensure observations are being collected (check agent logs)
- Check that BehaviorParameters are configured correctly
- Verify DecisionRequester is attached and configured

---

## Quick Reference

### Key File Locations
- **Training Config**: `ml-agents.yaml` (project root)
- **Episode Data**: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\bossfight\EpisodeData\`
- **Python Scripts**: `python_analysis/`
- **Training Results**: `results/[run-id]/`

### Key Unity Menu Items
- **Boss Fight > Setup ML-Agents Training Scene**: Create training scene

### Key Components
- **EpisodeManager**: Manages episode lifecycle
- **EpisodeRecorder**: Records episodes to disk
- **EpisodeReplay**: Replays saved episodes
- **PartyMemberAgent**: ML-Agent for party members
- **BossAgent**: ML-Agent for boss

### Key Commands
```bash
# Start training
mlagents-learn ml-agents.yaml --run-id=bossfight_training

# Resume training
mlagents-learn ml-agents.yaml --run-id=bossfight_training --resume

# Analyze episodes
cd python_analysis
python analyze_episodes.py
```

---

## Additional Resources

- **ML-Agents Documentation**: https://github.com/Unity-Technologies/ml-agents
- **Training Best Practices**: See ML-Agents documentation for hyperparameter tuning
- **Custom Observations**: Modify `ObservationEncoder.cs` to add/remove observations
- **Custom Actions**: Modify agent scripts to add/remove action branches

---

## Notes

- Episodes are saved automatically - no manual save required
- Training checkpoints are managed by ML-Agents Python package
- Episode data and training checkpoints are separate - deleting one doesn't affect the other
- Always backup important training runs before deleting data
- Use version control (Git) for code, but exclude `Library/`, `EpisodeData/`, and `results/` folders

