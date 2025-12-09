# AWS ML-Agents Training Setup Guide

Complete guide for setting up and running Unity ML-Agents training on AWS EC2.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [AWS Account Setup](#aws-account-setup)
3. [EC2 Instance Setup](#ec2-instance-setup)
4. [Instance Configuration](#instance-configuration)
5. [Connecting to Your Instance](#connecting-to-your-instance)
6. [Environment Setup](#environment-setup)
7. [Transferring Files to AWS](#transferring-files-to-aws)
8. [Running Training](#running-training)
9. [Cost Estimates](#cost-estimates)
10. [Best Practices](#best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- AWS account with credits ($100+ recommended)
- Unity build executable (Linux or Windows)
- `ml-agents.yaml` configuration file
- Basic familiarity with command line (SSH for Linux, RDP for Windows)

---

## AWS Account Setup

### 1. Create AWS Account

1. Go to [aws.amazon.com](https://aws.amazon.com)
2. Sign up for an account
3. Complete verification (credit card required, but won't be charged if using credits)
4. Navigate to EC2 dashboard

### 2. Complete AWS Tutorial Tasks

- AWS often offers $100 credits for completing tutorial tasks
- First task: Launch an EC2 instance
  - **AMI**: Amazon Linux 2 (for tutorial) or Ubuntu 22.04 LTS (for ML-Agents)
  - **Instance Type**: t2.micro or t3.micro (free tier, for tutorial only)
  - **Storage**: 8 GB gp3 (free tier)
  - **Security Group**: Allow SSH (port 22) from your IP

### 3. Understanding Free Tier vs Paid Instances

**Important**: You don't need to "upgrade" your account to use paid instances!

- **Free Tier Account**: You can use both free tier AND paid instances
- **Free Tier Instances**: Only t2.micro or t3.micro (1 vCPU, 1 GB RAM) - too small for ML-Agents
- **Paid Instances**: c7, m7, c5, m5, etc. - required for ML-Agents training
- **Your $100 Credits**: Will cover paid instance costs

**What You'll See**:
- If you only see **t3, c7, m7** instances, that's fine! These are newer generation instances
- **c7i** instances are actually better than c5 (newer, more efficient)
- **m7i** instances have more RAM (useful if you need it)

**Recommendation**: 
- Start with **c7i.large** (2 vCPUs, 4 GB) for testing (~$0.085/hour)
- Use **c7i.xlarge** (4 vCPUs, 8 GB) for actual training (~$0.17/hour)
- Your $100 credits will last ~588 hours of training on c7i.xlarge

---

## EC2 Instance Setup

### Choosing Your Instance

#### For ML-Agents Training:

**Recommended: Ubuntu 22.04 LTS (Linux)**
- Better Python/ML library support
- More tutorials and documentation
- Lower cost than Windows
- Headless operation (no GUI needed)

**Alternative: Amazon Linux 2023**
- Works but less common for ML workloads
- Good if you prefer Amazon's ecosystem

**Not Recommended: Windows**
- 2-3x more expensive
- Requires RDP (more complex)
- No significant advantages for headless training

### Instance Types

**Important**: Free tier only includes **t2.micro** or **t3.micro** (1 vCPU, 1 GB RAM) - these are too small for ML-Agents training. You'll need to use paid instances, but you can still use your free tier account. Your $100 credits will cover the costs.

#### CPU-Only Training (Recommended for starting):

**Newer Generation (c7/m7 - More Efficient, Better Performance):**

| Instance Type | vCPUs | RAM | Cost/Hour (Linux) | Cost/Hour (Windows) | Use Case |
|--------------|-------|-----|-------------------|---------------------|----------|
| **c7i.large** | 2 | 4 GB | ~$0.085 | ~$0.17 | Small scale (1-4 envs) |
| **c7i.xlarge** | 4 | 8 GB | ~$0.17 | ~$0.34 | Medium scale (4-12 envs) |
| **c7i.2xlarge** | 8 | 16 GB | ~$0.34 | ~$0.68 | Large scale (12-24 envs) |
| **c7i.4xlarge** | 16 | 32 GB | ~$0.68 | ~$1.36 | Very large scale (24+ envs) |
| **m7i.large** | 2 | 8 GB | ~$0.096 | ~$0.192 | More RAM needed (1-4 envs) |
| **m7i.xlarge** | 4 | 16 GB | ~$0.192 | ~$0.384 | More RAM needed (4-12 envs) |

**Older Generation (c5/m5 - Still Available, Slightly Cheaper):**

| Instance Type | vCPUs | RAM | Cost/Hour (Linux) | Cost/Hour (Windows) | Use Case |
|--------------|-------|-----|-------------------|---------------------|----------|
| **c5.large** | 2 | 4 GB | ~$0.085 | ~$0.17 | Small scale (1-4 envs) |
| **c5.xlarge** | 4 | 8 GB | ~$0.17 | ~$0.34 | Medium scale (4-12 envs) |
| **c5.2xlarge** | 8 | 16 GB | ~$0.34 | ~$0.68 | Large scale (12-24 envs) |
| **c5.4xlarge** | 16 | 32 GB | ~$0.68 | ~$1.36 | Very large scale (24+ envs) |

**Recommendation**: Use **c7i** instances if available (newer, more efficient). If not available in your region, use **c5** instances.

#### GPU Training (Faster but more expensive):

| Instance Type | vCPUs | RAM | GPU | Cost/Hour (Linux) | Cost/Hour (Windows) | Use Case |
|--------------|-------|-----|-----|-------------------|---------------------|----------|
| **g4dn.xlarge** | 4 | 16 GB | 1x T4 | ~$0.526 | ~$0.95 | GPU training (4-12 envs) |
| **g4dn.2xlarge** | 8 | 32 GB | 1x T4 | ~$0.752 | ~$1.35 | GPU training (12-24 envs) |
| **g4dn.4xlarge** | 16 | 64 GB | 1x T4 | ~$1.204 | ~$2.15 | GPU training (24+ envs) |

**Note**: GPU instances are only beneficial if you're using GPU-accelerated ML frameworks. For ML-Agents, CPU instances are usually sufficient.

### Launching Your Instance

1. **Go to EC2 Dashboard** → "Launch Instance"

2. **Name your instance**: e.g., "ML-Agents-Training"

3. **Choose AMI**: 
   - **Ubuntu Server 22.04 LTS** (recommended)
   - Or Amazon Linux 2023

4. **Instance Type**: 
   - **For testing**: Start with **c7i.large** or **c5.large** (2 vCPUs, 4 GB RAM) - ~$0.085/hour
   - **For training**: Use **c7i.xlarge** or **c5.xlarge** (4 vCPUs, 8 GB RAM) - ~$0.17/hour
   - Can scale up/down later
   - **Note**: If you only see t3, c7, m7 - use **c7i.large** (testing) or **c7i.xlarge** (training)

5. **Key Pair**:
   - Create new key pair or use existing
   - **Name**: e.g., "ml-agents-key"
   - **Type**: RSA
   - **Format**: .pem (for Linux) or .ppk (for PuTTY)
   - **Download** the key file (you'll need it to connect)

6. **Network Settings**:
   - **Security Group**: Create new or use existing
   - **SSH (22)**: Allow from "My IP" (your current IP address)
   - **Custom TCP (6006)**: Allow from "My IP" (for TensorBoard)

7. **Storage**:
   - **Size**: 50-100 GB (gp3 SSD)
   - Training logs and models can get large

8. **Launch Instance**

---

## Instance Configuration

### Storage Considerations

- **Root Volume**: 50-100 GB (for OS, Python, ML-Agents)
- **Additional EBS Volume** (optional): 100-500 GB for training data/logs
- **EBS gp3**: Cheaper and sufficient for training
- **EBS io1/io2**: Only needed for high IOPS workloads

### Security Groups

**Inbound Rules** (minimum):
- **SSH (22)**: Your IP only
- **Custom TCP (6006)**: Your IP only (TensorBoard)

**Outbound Rules**:
- All traffic (default)

---

## Connecting to Your Instance

### Linux/Mac

```bash
# Change permissions on key file
chmod 400 ml-agents-key.pem

# Connect via SSH
ssh -i ml-agents-key.pem ubuntu@<YOUR_INSTANCE_IP>
```

### Windows (PowerShell)

```powershell
# Connect via SSH
ssh -i ml-agents-key.pem ubuntu@<YOUR_INSTANCE_IP>
```

### Windows (PuTTY)

1. Download PuTTY and PuTTYgen
2. Convert .pem to .ppk using PuTTYgen
3. Connect using PuTTY with the .ppk file

**Find your instance IP**:
- EC2 Dashboard → Your Instance → "Public IPv4 address"

---

## Environment Setup

### 1. Update System Packages

```bash
# Ubuntu
sudo apt update
sudo apt upgrade -y

# Install essential tools
sudo apt install -y software-properties-common git wget curl
```

### 2. Install Python 3.8

```bash
# Add deadsnakes PPA (provides older Python versions)
sudo add-apt-repository ppa:deadsnakes/ppa -y
sudo apt update

# Install Python 3.8 and related packages
sudo apt install -y python3.8 python3.8-venv python3.8-dev python3.8-distutils

# Install pip for Python 3.8 (choose one method)

# Method 1: Install pip via apt (simplest and recommended)
sudo apt install -y python3.8-pip

# Method 2: Using curl with Python 3.8-specific URL
curl -sS https://bootstrap.pypa.io/pip/3.8/get-pip.py | python3.8

# Method 3: Using wget with Python 3.8-specific URL
wget https://bootstrap.pypa.io/pip/3.8/get-pip.py
python3.8 get-pip.py
rm get-pip.py

# Verify Python 3.8 installation
python3.8 --version  # Should show Python 3.8.x
python3.8 -m pip --version  # Should show pip version
```

### 3. Install Python Dependencies

```bash
# Install build dependencies
sudo apt install -y build-essential gfortran libopenblas-dev liblapack-dev

# Create virtual environment with Python 3.8
python3.8 -m venv ~/ml-agents-env

# Activate virtual environment
source ~/ml-agents-env/bin/activate

# Verify you're using Python 3.8
python --version  # Should show Python 3.8.x

# Upgrade pip
pip install --upgrade pip

# Install numpy (Python 3.8 has better compatibility)
pip install "numpy<1.20"

# Install ML-Agents 0.28.0 (specific version to match Unity project)
pip install "mlagents==0.28.0"

# Install specific versions (if needed for compatibility)
pip install "protobuf<4.0"
pip install "cattrs<1.7,>=1.1.0"
pip install packaging six

# Install TensorBoard
pip install "tensorboard>=2.0"
```

### 4. Verify Installation

```bash
# Check ML-Agents version
mlagents-learn --help

# Check Python version (should be 3.8-3.11)
python3 --version
```

**Note**: ML-Agents 0.28.0 requires Python 3.8-3.11. Python 3.12+ is not supported.

---

## Transferring Files to AWS

**Important**: You must be in your **project root directory** (`bossv2`) when running these commands.

### Option 1: SCP (Linux/Mac)

```bash
# Navigate to your project root directory first
cd /path/to/bossv2
# Or on Windows: cd "D:\Your\Project\Path\bossv2"

# Make sure your key file is accessible (in Downloads or project root)
# If key is in Downloads, use full path: -i ~/Downloads/ml-agents-key.pem

# Create remote directory first (if it doesn't exist)
ssh -i ml-agents-key.pem ubuntu@<INSTANCE_IP> "mkdir -p ~/bossfight-build"

# Transfer Unity build
scp -i ml-agents-key.pem -r unity/bossfight/Builds/MLAgentsTraining/* ubuntu@<INSTANCE_IP>:~/bossfight-build/

# Transfer config file
scp -i ml-agents-key.pem ml-agents.yaml ubuntu@<INSTANCE_IP>:~/
```

### Option 2: SCP (Windows PowerShell)

```powershell
# Navigate to your project root directory first
cd "D:\Your\Project\Path\bossv2"

# Make sure your key file is accessible
# If key is in Downloads, use full path: -i "C:\Users\YourUsername\Downloads\ml-agents-key.pem"

# Create remote directory first (if it doesn't exist)
ssh -i ml-agents-key.pem ubuntu@<INSTANCE_IP> "mkdir -p ~/bossfight-build"

# Transfer Unity build
scp -i ml-agents-key.pem -r unity/bossfight/Builds/MLAgentsTraining/* ubuntu@<INSTANCE_IP>:~/bossfight-build/

# Transfer config file
scp -i ml-agents-key.pem ml-agents.yaml ubuntu@<INSTANCE_IP>:~/
```

### Option 3: AWS S3 (For Large Files)

```bash
# On your local machine
aws s3 cp unity/bossfight/Builds/MLAgentsTraining/ s3://your-bucket/bossfight-build/ --recursive

# On EC2 instance
aws s3 cp s3://your-bucket/bossfight-build/ ~/bossfight-build/ --recursive
```

### Option 4: Git (If Using Version Control)

```bash
# On EC2 instance
git clone <your-repo-url>
cd bossv2
```

### Make Build Executable (Linux)

```bash
# On EC2 instance
chmod +x ~/bossfight-build/bossfight.x86_64
```

---

## Running Training

### 1. Basic Training Command

```bash
# SSH into your instance
ssh -i ml-agents-key.pem ubuntu@<INSTANCE_IP>

# Activate virtual environment
source ~/ml-agents-env/bin/activate

# Navigate to directory with config
cd ~

# Start training
cd ~
mlagents-learn /home/ubuntu/ml-agents.yaml \
    --env=/home/ubuntu/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_test_001 \
    --num-envs=12
```

### 2. Training with TensorBoard

**Terminal 1** (Training):
```bash
cd ~
mlagents-learn /home/ubuntu/ml-agents.yaml \
    --env=/home/ubuntu/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_test_001 \
    --num-envs=12 \
    --no-graphics \
    --force
```

**Terminal 2** (TensorBoard):
```bash
# On EC2 instance
tensorboard --logdir=~/results --port=6006 --host=0.0.0.0
```

**Access TensorBoard**:
- In your browser, go to: `http://<YOUR_INSTANCE_IP>:6006`
- Make sure security group allows port 6006 from your IP

### 3. Running in Background (Screen/Tmux)

**Using Screen**:
```bash
# Install screen
sudo apt install screen -y

# Start new screen session
screen -S training

# Run training command
mlagents-learn ml-agents.yaml --env=~/bossfight-build/bossfight.x86_64 --run-id=bossfight_training_001 --num-envs=12

# Detach: Press Ctrl+A, then D
# Reattach: screen -r training
# List sessions: screen -ls
```

**Using Tmux**:
```bash
# Install tmux
sudo apt install tmux -y

# Start new tmux session
tmux new -s training

# Run training command
mlagents-learn ml-agents.yaml --env=~/bossfight-build/bossfight.x86_64 --run-id=bossfight_training_001 --num-envs=12

# Detach: Press Ctrl+B, then D
# Reattach: tmux attach -t training
# List sessions: tmux ls
```

### 4. Resume Training

```bash
mlagents-learn ml-agents.yaml \
    --env=~/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_training_001 \
    --resume \
    --num-envs=12
```

### 5. Force Overwrite (New Training Run)

```bash
mlagents-learn ml-agents.yaml \
    --env=~/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_training_002 \
    --force \
    --num-envs=12
```

---

## Downloading and Viewing Episodes

If your Unity project has `EpisodeRecorder` enabled, episodes are automatically saved during training. You can download and view them locally.

### 1. Find Episode Files on EC2

**On EC2 (SSH'd in):**

```bash
# Check where episodes are saved (usually in build directory)
ls -la ~/bossfight-build/episode_*.json

# Or check home directory
ls -la ~/episode_*.json

# Search for episode files
find ~ -name "episode_*.json" -type f 2>/dev/null

# Check how many episodes have been saved
ls -1 ~/bossfight-build/episode_*.json 2>/dev/null | wc -l

# View most recent episodes
ls -lt ~/bossfight-build/episode_*.json 2>/dev/null | head -n 5
```

**Note**: With parallel environments, episodes may have instance IDs in filenames like `episode_env_123_0.json` to prevent conflicts.

### 2. Download Episodes to Local Machine

**On your local machine (PowerShell):**

```powershell
# Navigate to project root
cd "D:\Your\Project\Path\bossv2"

# Create folder for downloaded episodes
mkdir episodes -ErrorAction SilentlyContinue

# Download all episode files from build directory
scp -i "C:\Users\YourUsername\Downloads\ml-agents-key.pem" ubuntu@<YOUR_INSTANCE_IP>:~/bossfight-build/episode_*.json ./episodes/

# Or if episodes are in home directory
scp -i "C:\Users\YourUsername\Downloads\ml-agents-key.pem" ubuntu@<YOUR_INSTANCE_IP>:~/episode_*.json ./episodes/

# Download specific episode
scp -i "C:\Users\YourUsername\Downloads\ml-agents-key.pem" ubuntu@<YOUR_INSTANCE_IP>:~/bossfight-build/episode_0.json ./episodes/
```

**Replace `<YOUR_INSTANCE_IP>` with your actual EC2 instance IP address.**

### 3. View Episodes

**Option A: Use EpisodeReplay in Unity**

1. Open your Unity project
2. Load an episode file using the `EpisodeReplay` script
3. Play it back in the Unity editor to see the agents' actions

**Option B: View JSON Directly**

Episodes are saved as JSON files containing:
- Episode metadata (number, duration, win condition)
- Agent actions for each frame
- Agent states and observations

You can:
- Open in a text editor (VS Code, Notepad++)
- Use a JSON viewer/formatter
- Write a Python script to analyze the data

**Option C: Check Episode Content**

```bash
# On EC2, view episode structure
head -n 50 ~/bossfight-build/episode_0.json

# Or download and view locally
cat ./episodes/episode_0.json | head -n 50
```

### 4. Download Training Models

**On your local machine (PowerShell):**

```powershell
# Download trained models (if you want to use them locally)
scp -i "C:\Users\jason\Downloads\ml-agents-key.pem" -r ubuntu@34.236.237.142:~/results/bossfight_training_001 ./models/

# Download specific checkpoint
scp -i "C:\Users\jason\Downloads\ml-agents-key.pem" ubuntu@34.236.237.142:~/results/bossfight_training_001/checkpoints/*.pt ./models/
```

### 5. Download Training Logs

```powershell
# Download TensorBoard logs
scp -i "C:\Users\jason\Downloads\ml-agents-key.pem" -r ubuntu@34.236.237.142:~/results/bossfight_training_001 ./training_logs/

# Then view in TensorBoard locally
tensorboard --logdir=./training_logs
```

---

## Cost Estimates

### Monthly Cost Examples

#### Scenario 1: Testing (2 hours/day, 30 days)
- **Instance**: c7i.large or c5.large (Linux)
- **Cost/Hour**: $0.085
- **Hours**: 2 × 30 = 60 hours
- **Total**: 60 × $0.085 = **$5.10/month**

#### Scenario 2: Light Training (4 hours/day, 30 days)
- **Instance**: c7i.xlarge or c5.xlarge (Linux)
- **Cost/Hour**: $0.17
- **Hours**: 4 × 30 = 120 hours
- **Total**: 120 × $0.17 = **$20.40/month**

#### Scenario 3: Medium Training (8 hours/day, 30 days)
- **Instance**: c7i.xlarge or c5.xlarge (Linux)
- **Cost/Hour**: $0.17
- **Hours**: 8 × 30 = 240 hours
- **Total**: 240 × $0.17 = **$40.80/month**

#### Scenario 4: Heavy Training (24/7, 30 days)
- **Instance**: c7i.xlarge or c5.xlarge (Linux)
- **Cost/Hour**: $0.17
- **Hours**: 24 × 30 = 720 hours
- **Total**: 720 × $0.17 = **$122.40/month**

#### Scenario 4: GPU Training (8 hours/day, 30 days)
- **Instance**: g4dn.xlarge (Linux)
- **Cost/Hour**: $0.526
- **Hours**: 8 × 30 = 240 hours
- **Total**: 240 × $0.526 = **$126.24/month**

### Additional Costs

- **EBS Storage**: ~$0.10/GB/month (50 GB = $5/month)
- **Data Transfer**: First 100 GB free, then ~$0.09/GB
- **Total**: Usually <$10/month for storage and transfer

### Cost Optimization Tips

1. **Use Spot Instances**: 50-90% cheaper (but can be interrupted)
2. **Stop Instance When Not Training**: Only pay for storage (~$5/month)
3. **Use Smaller Instances**: Start with c5.large, scale up if needed
4. **Monitor Usage**: Set up AWS billing alerts
5. **Use Reserved Instances**: 30-50% discount for 1-3 year commitments

### Spot Instances (Advanced)

**Pros**:
- 50-90% cheaper than On-Demand
- Good for training (can tolerate interruptions)

**Cons**:
- Can be terminated with 2-minute warning
- May not be available in all regions

**How to Use**:
1. EC2 Dashboard → "Spot Requests" → "Request Spot Instances"
2. Choose same configuration as On-Demand
3. Set max price (or use On-Demand price)
4. Launch

**Save Training Progress**:
- Use `--resume` flag to continue after interruption
- Save models regularly (ML-Agents does this automatically)

---

## Best Practices

### 1. Instance Management

- **Stop (not terminate)** when not training: Saves compute costs, keeps data
- **Create AMI** before major changes: Easy rollback
- **Use tags**: Track costs by project (e.g., "Project: ML-Agents")

### 2. Training Workflow

- **Start small**: Test with 1-2 environments first
- **Monitor resources**: Use `htop` or `nvidia-smi` (for GPU)
- **Save frequently**: ML-Agents auto-saves, but backup important runs
- **Use version control**: Track config changes in Git

### 3. Security

- **Never commit keys**: Use `.gitignore` for `.pem` files
- **Restrict SSH access**: Only allow your IP in security group
- **Use IAM roles**: For S3 access instead of access keys
- **Rotate keys**: Change key pairs periodically

### 4. Monitoring

- **CloudWatch**: Monitor CPU, memory, network usage
- **Billing Alerts**: Set up alerts at $50, $100, etc.
- **Training Logs**: Check `results/` directory regularly

### 5. Backup

- **EBS Snapshots**: Backup training data and models
- **S3**: Upload important models to S3 for long-term storage
- **Git**: Version control for configs and code

---

## Troubleshooting

### Common Issues

#### 1. "Permission denied" when connecting via SSH

**Solution**:
```bash
# Linux/Mac
chmod 400 ml-agents-key.pem

# Windows (PowerShell)
icacls ml-agents-key.pem /inheritance:r
icacls ml-agents-key.pem /grant:r "$($env:USERNAME):(R)"
```

#### 2. "Connection timeout" when SSHing

**Causes**:
- Security group doesn't allow SSH from your IP
- Instance is stopped/terminated
- Wrong IP address

**Solution**:
- Check security group rules
- Verify instance is running
- Use public IP from EC2 dashboard

#### 3. "No module named 'mlagents'"

**Solution**:
```bash
# Activate virtual environment
source ~/ml-agents-env/bin/activate

# Verify installation
pip list | grep mlagents
```

#### 4. "Couldn't launch the environment"

**Causes**:
- Wrong path to executable
- Executable not marked as executable (Linux)
- Missing dependencies (e.g., libSDL2)

**Solution**:
```bash
# Make executable
chmod +x bossfight.x86_64

# Install dependencies (Ubuntu)
sudo apt install -y libsdl2-2.0-0 libsdl2-dev

# Verify path
ls -la ~/bossfight-build/bossfight.x86_64
```

#### 5. "Out of memory" errors / Worker killed (SIGKILL)

**Symptoms**:
- `[WARNING] Restarting worker[X] after 'Environment shut down with return code -9 (SIGKILL)'`
- Workers keep crashing and restarting
- Training eventually fails with "Couldn't launch the environment"

**Causes**:
- Too many environments for available memory
- Each Unity instance uses ~500MB-1GB RAM
- 12 environments × 1GB = 12GB+ RAM needed (plus OS and Python)

**Solution**:
```bash
# Check current memory usage
free -h

# Check which processes are using memory
htop
# Or
ps aux --sort=-%mem | head -n 10

# Reduce number of environments
mlagents-learn /home/ubuntu/ml-agents.yaml \
    --env=/home/ubuntu/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_training_002 \
    --num-envs=6 \
    --no-graphics \
    --force

# Or use even fewer for testing
--num-envs=4
```

**Prevention**:
- Start with 4-6 environments on c5.xlarge (8 GB RAM)
- Use c5.2xlarge (16 GB RAM) for 8-12 environments
- Use c5.4xlarge (32 GB RAM) for 16+ environments
- Monitor memory: `watch -n 1 free -h`

#### 6. Training is slow

**Causes**:
- Too many environments for instance size
- Network latency (if training locally)
- CPU throttling (check CloudWatch metrics)

**Solution**:
- Reduce `--num-envs`
- Use larger instance
- Check CloudWatch for CPU utilization

#### 7. TensorBoard shows "No scalar data"

**Causes**:
- Training hasn't started yet
- Wrong log directory
- Training crashed before first save

**Solution**:
- Wait for training to start (check terminal)
- Verify log directory: `ls -la ~/results/`
- Check training logs for errors

#### 9. Worker restart failures after crash

**Symptoms**:
- Worker crashes with SIGKILL (-9)
- ML-Agents tries to restart but fails
- Error: "Couldn't launch the environment"

**Solution**:
```bash
# Verify the executable still exists
ls -la /home/ubuntu/bossfight-build/bossfight.x86_64

# Check if it's still executable
chmod +x /home/ubuntu/bossfight-build/bossfight.x86_64

# Restart training with fewer environments
mlagents-learn /home/ubuntu/ml-agents.yaml \
    --env=/home/ubuntu/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_training_002 \
    --num-envs=6 \
    --no-graphics \
    --force

# Or resume previous training (if it saved checkpoints)
mlagents-learn /home/ubuntu/ml-agents.yaml \
    --env=/home/ubuntu/bossfight-build/bossfight.x86_64 \
    --run-id=bossfight_test_001 \
    --num-envs=6 \
    --no-graphics \
    --resume
```

#### 10. Instance terminated unexpectedly

**Causes**:
- Spot instance interruption
- Billing issue (credits expired)
- Manual termination

**Solution**:
- Use On-Demand instances for critical training
- Set up billing alerts
- Use `--resume` to continue training

### Getting Help

- **AWS Support**: [AWS Support Center](https://console.aws.amazon.com/support/)
- **ML-Agents Docs**: [Unity ML-Agents Documentation](https://github.com/Unity-Technologies/ml-agents)
- **AWS Forums**: [AWS Forums](https://forums.aws.amazon.com/)

---

## Quick Reference

### Essential Commands

```bash
# Connect to instance
ssh -i ml-agents-key.pem ubuntu@<INSTANCE_IP>

# Activate environment
source ~/ml-agents-env/bin/activate

# Start training
mlagents-learn ml-agents.yaml --env=~/bossfight-build/bossfight.x86_64 --run-id=training_001 --num-envs=12

# Start TensorBoard
tensorboard --logdir=~/results --port=6006 --host=0.0.0.0

# Check training status
ps aux | grep mlagents-learn

# Stop training
pkill -f mlagents-learn

# Transfer files
scp -i ml-agents-key.pem file.txt ubuntu@<INSTANCE_IP>:~/

# Download episodes
scp -i ml-agents-key.pem ubuntu@<INSTANCE_IP>:~/bossfight-build/episode_*.json ./episodes/

# Download training models
scp -i ml-agents-key.pem -r ubuntu@<INSTANCE_IP>:~/results/bossfight_training_001 ./models/
```

### Cost Calculator

- **AWS Pricing Calculator**: [calculator.aws](https://calculator.aws/)
- **EC2 Pricing**: [EC2 Pricing](https://aws.amazon.com/ec2/pricing/)

---

## Next Steps

1. **Launch your first instance** using this guide
2. **Run a test training** with 1-2 environments
3. **Monitor costs** via AWS billing dashboard
4. **Scale up** as needed (more environments, larger instances)
5. **Optimize** using Spot Instances and Reserved Instances

**Good luck with your training!** 🚀

