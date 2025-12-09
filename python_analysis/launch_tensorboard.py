"""
Helper script to launch TensorBoard for viewing training logs.
"""

import subprocess
import sys
import os
import webbrowser
import time

def main():
    log_dir = "tensorboard_logs"
    
    # Check if log directory exists
    if not os.path.exists(log_dir):
        print(f"Error: Log directory '{log_dir}' not found!")
        print("TensorBoard logs are generated automatically during ML-Agents training.")
        print("Start training with: mlagents-learn ml-agents.yaml --run-id=bossfight_training")
        sys.exit(1)
    
    # Check if TensorBoard is available
    try:
        import tensorboard
    except ImportError:
        print("Error: TensorBoard is not installed!")
        print("Please install it with: pip install tensorboard")
        sys.exit(1)
    
    print(f"Starting TensorBoard with logs from '{log_dir}'...")
    print("TensorBoard will open in your browser at http://localhost:6006")
    print("\nPress Ctrl+C to stop TensorBoard\n")
    
    # Start TensorBoard
    try:
        # Open browser after a short delay
        def open_browser():
            time.sleep(2)  # Wait for TensorBoard to start
            webbrowser.open("http://localhost:6006")
        
        import threading
        browser_thread = threading.Thread(target=open_browser)
        browser_thread.daemon = True
        browser_thread.start()
        
        # Start TensorBoard process
        subprocess.run([
            sys.executable, "-m", "tensorboard",
            "--logdir", log_dir,
            "--port", "6006",
            "--host", "localhost"
        ])
    except KeyboardInterrupt:
        print("\n\nTensorBoard stopped.")
    except Exception as e:
        print(f"Error starting TensorBoard: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()


