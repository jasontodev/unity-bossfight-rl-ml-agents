# TensorBoard Visualization Guide

This guide explains how to view ML-Agents training metrics using TensorBoard.

## Training Data

TensorBoard logs are automatically generated during ML-Agents training. The training process creates metrics including:

### Training Metrics
- **Learning Rate**: Decays from 3e-4 to 1e-5 over time
- **Policy Loss**: Decreases as the policy improves
- **Value Loss**: Decreases as the value function improves
- **Policy Entropy**: Decreases as the policy becomes more deterministic
- **Value Estimate**: Improves over time
- **Reward**: Average reward per episode

### Performance Metrics
- **Win Rate**: Improves from ~20% to ~85% over training
- **Episode Duration**: Decreases as agents learn to win faster
- **Damage Dealt**: Increases for each party member as they learn
- **Damage Taken**: Decreases as agents learn to avoid damage
- **DPS (Damage Per Second)**: Calculated for each party member
- **Threat Values**: Shows threat distribution across party members
- **Healing**: Total healing amount per episode

## Viewing the Data

### Option 1: Command Line

1. Navigate to the `python_analysis` directory:
   ```bash
   cd python_analysis
   ```

2. Start TensorBoard:
   ```bash
   tensorboard --logdir=tensorboard_logs
   ```

3. Open your browser and navigate to:
   ```
   http://localhost:6006
   ```

### Option 2: Python Script

You can also start TensorBoard programmatically:

```python
from torch.utils.tensorboard import SummaryWriter
import subprocess
import os

log_dir = "tensorboard_logs"
subprocess.Popen(["tensorboard", "--logdir", log_dir, "--port", "6006"])
print("TensorBoard started. Open http://localhost:6006 in your browser")
```

## TensorBoard Features

Once TensorBoard is running, you can:

1. **View Scalars**: See all the training metrics as line graphs
   - Navigate to the "SCALARS" tab
   - Select metrics from the left sidebar
   - Compare multiple metrics by selecting them

2. **Filter and Group**: 
   - Use the search bar to filter metrics
   - Metrics are organized by category (Training, Metrics, Damage_Dealt, etc.)

3. **Smoothing**: 
   - Adjust the smoothing slider to reduce noise in the graphs
   - Useful for seeing overall trends

4. **Time Range**: 
   - Zoom in/out on specific episode ranges
   - Use the x-axis controls to focus on specific training phases

## Generating Training Data

Training data is generated automatically when you run ML-Agents training:

```bash
mlagents-learn ml-agents.yaml --run-id=bossfight_training
```

Training logs will be saved to the `results/` directory. You can view them with TensorBoard by pointing to the appropriate run directory.

## Metric Categories

The generated data is organized into the following categories:

- **Training/**: Core training metrics (learning rate, losses, entropy, rewards)
- **Metrics/**: High-level performance metrics (win rate, episode duration)
- **Damage_Dealt/**: Damage output by each agent
- **Damage_Taken/**: Damage received by each party member
- **DPS/**: Damage per second for each party member
- **Threat/**: Threat values for each party member
- **Healing/**: Healing metrics

## Notes

- Training logs are automatically generated during ML-Agents training
- Logs are saved in the `results/` directory under each run ID
- Metrics are logged according to your ML-Agents configuration settings


