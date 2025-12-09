# Boss Fight ML-Agents

[![Paper](https://img.shields.io/badge/📄_Research_Paper-View-blue)](https://docs.google.com/document/d/1Q5GdZmZrukk5LC7ekWNSx0ECbu1ALatR96Ujfnf27cg/edit?usp=sharing)
[![PDF](https://img.shields.io/badge/📥_Download-PDF-green)](https://github.com/jasontodev/unity-bossfight-rl-ml-agents/raw/master/Multi-Agent%20Reinforcement%20Learning%20for%20Large-Scale%20MMO%20Combat.pdf)

> **📄 Research Paper**: [**View on Google Docs**](https://docs.google.com/document/d/1Q5GdZmZrukk5LC7ekWNSx0ECbu1ALatR96Ujfnf27cg/edit?usp=sharing) | [Download PDF](https://github.com/jasontodev/unity-bossfight-rl-ml-agents/raw/master/Multi-Agent%20Reinforcement%20Learning%20for%20Large-Scale%20MMO%20Combat.pdf) | [View on GitHub](https://github.com/jasontodev/unity-bossfight-rl-ml-agents/blob/master/Multi-Agent%20Reinforcement%20Learning%20for%20Large-Scale%20MMO%20Combat.pdf)

A Unity ML-Agents reinforcement learning project featuring a boss fight game where a party of 4 members (Tank, Healer, MeleeDPS, RangedDPS) battles against an AI-controlled Boss.

## 🎮 Overview

This project implements a complete ML-Agents training environment for a boss fight scenario with:
- **5 AI Agents**: 4 party members + 1 boss, all controlled by ML-Agents
- **Custom Observation System**: LIDAR-based perception with entity embeddings
- **Sparse Rewards**: +1/-1 reward structure for win/loss conditions
- **Episode Recording & Replay**: Full episode tracking and visualization
- **Social Network Analysis**: Python tools for analyzing agent interactions
- **TensorBoard Integration**: Training metrics visualization

## ✨ Features

### Unity Game
- Party members with distinct roles (Tank, Healer, MeleeDPS, RangedDPS)
- Boss with wall manipulation abilities
- LIDAR-based observation system
- Health, threat, and healing systems
- Episode recording and replay functionality

### Python Analysis Tools
- **SNA Visualization**: Interactive HTML network graphs showing agent relationships
- **TensorBoard Integration**: Training metrics and learning curves
- **Episode Analysis**: Damage, healing, threat, and class performance analysis
- **Network Analysis**: Centrality metrics and relationship graphs

## 📁 Project Structure

```
bossfight-ml-agents/
├── unity/bossfight/          # Unity ML-Agents project
│   ├── Assets/Scripts/       # Game scripts and ML-Agent implementations
│   └── Builds/              # Training builds
├── python_analysis/          # Python analysis tools
│   ├── sna_visualization/    # Social Network Analysis tools
│   └── analyze_episodes.py
├── ml-agents.yaml           # ML-Agents training configuration
└── results/                 # Training results and models
```

## 🚀 Quick Start

### Prerequisites
- Unity 2022.3+ with ML-Agents package
- Python 3.8-3.11
- ML-Agents Python package

### Training Setup

1. **Open Unity Project**:
   ```bash
   # Open unity/bossfight in Unity Editor
   ```

2. **Setup Training Scene**:
   - Use menu: `Boss Fight > Setup ML-Agents Training Scene`
   - Or manually configure the scene with ML-Agent components

3. **Start Training**:
   ```bash
   mlagents-learn ml-agents.yaml --run-id=bossfight_training
   ```

4. **View Training Metrics**:
   ```bash
   cd python_analysis
   tensorboard --logdir=tensorboard_logs
   ```

### Analysis Tools

**Generate SNA Visualization**:
```bash
cd python_analysis
python sna_visualization/interactive_sna.py \
  --input episodes.json \
  --output interactive_sna.html \
  --compare \
  --early-range 0 15000 \
  --late-range 15001 30000
```

**Analyze Episodes**:
```bash
cd python_analysis
python analyze_episodes.py
```

## 📊 Visualization Features

### Interactive SNA Graphs
- Draggable nodes
- Adjustable edge thickness, curvature, and type
- Per-agent controls for all visual properties
- Save/load configuration
- Compare early vs. late training phases

### TensorBoard Metrics
- Learning rate decay
- Win rate progression
- Damage dealt/taken by agent
- Policy and value losses
- Episode duration trends
- Threat distribution

## 📚 Documentation

- **[ML-Agents Setup Guide](unity/bossfight/ML_AGENTS_SETUP_GUIDE.md)**: Complete setup instructions
- **[SNA Visualization Guide](python_analysis/sna_visualization/SNA_README.md)**: Network analysis documentation
- **[TensorBoard Guide](python_analysis/TENSORBOARD_README.md)**: Training metrics visualization
- **[AWS Setup Guide](AWS_ML_AGENTS_SETUP.md)**: Running training on AWS EC2

## 🛠️ Technologies

- **Unity**: Game engine and ML-Agents framework
- **ML-Agents**: Reinforcement learning framework
- **Python**: Analysis and visualization tools
- **NetworkX**: Social network analysis
- **Vis.js**: Interactive graph visualization
- **TensorBoard**: Training metrics visualization

## 📝 License

This project is licensed under the MIT License.

Copyright (c) 2024

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

**Note**: This project uses ML-Agents 0.28.0+ and requires Python 3.8-3.11.

