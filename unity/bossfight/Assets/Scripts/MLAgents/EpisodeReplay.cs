using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

[DefaultExecutionOrder(-100)] // Run early to ensure actions are applied before agent Updates
public class EpisodeReplay : MonoBehaviour
{
    [Header("Replay Settings")]
    [SerializeField] private bool isPlaying = false;
    public bool IsPlaying => isPlaying; // Public property to check if replay is active
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool pauseOnFrame = false;
    
    private EpisodeData episodeData;
    private int currentReplayFrame = 0;
    private float replayTime = 0f;
    private Dictionary<string, GameObject> agentMap;
    private List<EpisodeAction> sortedActions;
    private int maxFrame = 0;
    private int lastExecutedFrame = -1;
    
    void Awake()
    {
        // Initialize agentMap early to avoid null reference
        if (agentMap == null)
        {
            agentMap = new Dictionary<string, GameObject>();
        }
    }
    
    void Start()
    {
        // Ensure agentMap is initialized
        if (agentMap == null)
        {
            agentMap = new Dictionary<string, GameObject>();
        }
    }
    
    void Update()
    {
        // Handle keyboard input
        if (Input.GetKeyDown(KeyCode.R) && episodeData != null && !isPlaying)
        {
            StartReplay();
        }
        
        if (Input.GetKeyDown(KeyCode.Space) && isPlaying)
        {
            if (pauseOnFrame)
            {
                ResumeReplay();
            }
            else
            {
                PauseReplay();
            }
        }
        
        if (!isPlaying || episodeData == null || sortedActions == null) return;
        
        if (pauseOnFrame)
        {
            // Step frame by frame
            return;
        }
        
        // Execute actions for CURRENT frame BEFORE advancing (so agent Update() uses correct frame)
        // This matches the recording: actions were recorded for frame N, so we apply them for frame N
        if (currentReplayFrame != lastExecutedFrame && currentReplayFrame <= maxFrame)
        {
            ExecuteActionsForFrame(currentReplayFrame);
            lastExecutedFrame = currentReplayFrame;
        }
        
        // Advance to next frame AFTER applying actions (matching the recording rate - one frame per Update call)
        // Apply playback speed by skipping frames (speed > 1) or waiting (speed < 1)
        if (playbackSpeed > 1f)
        {
            // Fast forward: advance multiple frames per update
            int framesToSkip = Mathf.FloorToInt(playbackSpeed);
            currentReplayFrame += framesToSkip;
        }
        else if (playbackSpeed < 1f)
        {
            // Slow motion: only advance some frames based on time
            replayTime += Time.deltaTime;
            float frameInterval = 1f / playbackSpeed; // Time between frames at this speed
            if (replayTime >= frameInterval)
            {
                currentReplayFrame++;
                replayTime = 0f;
            }
        }
        else
        {
            // Normal speed (1.0): advance one frame per update
            currentReplayFrame++;
        }
        
        // Check if we've reached the end
        if (currentReplayFrame > maxFrame)
        {
            StopReplay();
            Debug.Log($"Replay completed. Reached frame {maxFrame}");
            return;
        }
    }
    
    public void LoadEpisode(string fileName = null)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "episode_0.json";
        }
        string fullPath = Path.Combine(Application.persistentDataPath, "EpisodeData", fileName);
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Episode file not found: {fullPath}");
            return;
        }
        
        // Try to load as JSON first
        if (fileName.EndsWith(".json"))
        {
            string json = File.ReadAllText(fullPath);
            episodeData = JsonUtility.FromJson<EpisodeData>(json);
        }
        else if (fileName.EndsWith(".bin"))
        {
            // Load as binary
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(fullPath, FileMode.Open))
            {
                episodeData = (EpisodeData)formatter.Deserialize(stream);
            }
        }
        
        if (episodeData != null)
        {
            // Convert agent classes from lists to dictionary if needed
            if (episodeData.agentIds != null && episodeData.agentClassValues != null)
            {
                // This is handled by the GetAgentClasses method
            }
            
            // Sort actions by frame
            sortedActions = episodeData.actions.OrderBy(a => a.frame).ToList();
            
            // Find maximum frame number
            if (sortedActions.Count > 0)
            {
                maxFrame = sortedActions.Max(a => a.frame);
            }
            else
            {
                maxFrame = 0;
                Debug.LogWarning("Episode has no actions!");
            }
            
            // Map agents
            MapAgents();
            
            Debug.Log($"Loaded episode {episodeData.episode} with {episodeData.actions.Count} actions, max frame: {maxFrame}");
        }
    }
    
    void MapAgents()
    {
        // Ensure agentMap is initialized
        if (agentMap == null)
        {
            agentMap = new Dictionary<string, GameObject>();
        }
        
        agentMap.Clear();
        
        // Find all agents in scene
        PartyMemberAgent[] partyAgents = FindObjectsOfType<PartyMemberAgent>();
        foreach (PartyMemberAgent agent in partyAgents)
        {
            agentMap[agent.gameObject.name] = agent.gameObject;
            Debug.Log($"Mapped party agent: {agent.gameObject.name}");
        }
        
        BossAgent bossAgent = FindObjectOfType<BossAgent>();
        if (bossAgent != null)
        {
            agentMap[bossAgent.gameObject.name] = bossAgent.gameObject;
            Debug.Log($"Mapped boss agent: {bossAgent.gameObject.name}");
        }
        
        // Log all unique agent IDs from episode data
        if (episodeData != null && episodeData.actions != null)
        {
            var uniqueAgentIds = episodeData.actions.Select(a => a.agentId).Distinct().ToList();
            Debug.Log($"Episode contains actions for {uniqueAgentIds.Count} agents: {string.Join(", ", uniqueAgentIds)}");
            
            // Check if any agent IDs don't match
            foreach (string agentId in uniqueAgentIds)
            {
                if (!agentMap.ContainsKey(agentId))
                {
                    Debug.LogWarning($"Episode contains actions for agent '{agentId}' but this agent was not found in scene!");
                }
            }
        }
        
        Debug.Log($"Total agents mapped: {agentMap.Count}");
    }
    
    public void StartReplay()
    {
        if (episodeData == null)
        {
            Debug.LogError("No episode loaded. Load an episode first.");
            return;
        }
        
        if (sortedActions == null || sortedActions.Count == 0)
        {
            Debug.LogError("No actions to replay!");
            return;
        }
        
        // Re-map agents in case scene changed
        MapAgents();
        
        isPlaying = true;
        currentReplayFrame = 0;
        lastExecutedFrame = -1;
        replayTime = 0f;
        
        // Reset scene to initial state
        ResetSceneForReplay();
        
        Debug.Log($"Starting replay of episode {episodeData.episode} (frames 0-{maxFrame}, {sortedActions.Count} total actions)");
    }
    
    public void StopReplay()
    {
        isPlaying = false;
        Debug.Log("Replay stopped");
    }
    
    public void PauseReplay()
    {
        pauseOnFrame = true;
    }
    
    public void ResumeReplay()
    {
        pauseOnFrame = false;
    }
    
    public void StepFrame()
    {
        if (episodeData == null || currentReplayFrame >= sortedActions.Count) return;
        
        ExecuteActionsForFrame(currentReplayFrame);
        currentReplayFrame++;
    }
    
    void ExecuteActionsForFrame(int frame)
    {
        // Get all actions for this frame
        List<EpisodeAction> frameActions = sortedActions.Where(a => a.frame == frame).ToList();
        
        if (frameActions.Count == 0 && frame % 60 == 0) // Log every second if no actions (for debugging)
        {
            // Only log occasionally to avoid spam
            return;
        }
        
        foreach (EpisodeAction action in frameActions)
        {
            if (!agentMap.ContainsKey(action.agentId))
            {
                Debug.LogWarning($"Frame {frame}: Agent '{action.agentId}' not found in agentMap");
                continue;
            }
            
            GameObject agent = agentMap[action.agentId];
            
            // Execute action based on branch
            ExecuteAction(agent, action.branch, action.value);
        }
    }
    
    void ExecuteAction(GameObject agent, string branch, int value)
    {
        // Apply action to the agent
        PartyMemberAgent partyAgent = agent.GetComponent<PartyMemberAgent>();
        if (partyAgent != null)
        {
            partyAgent.ApplyRecordedAction(branch, value);
            return;
        }
        
        BossAgent bossAgent = agent.GetComponent<BossAgent>();
        if (bossAgent != null)
        {
            bossAgent.ApplyRecordedAction(branch, value);
            return;
        }
        
        Debug.LogWarning($"Could not find agent component on {agent.name} for replay action {branch} = {value}");
    }
    
    void ResetSceneForReplay()
    {
        // Reset all agents to initial positions
        EpisodeManager episodeManager = EpisodeManager.Instance;
        if (episodeManager != null)
        {
            episodeManager.StartEpisode();
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        
        int yOffset = Screen.height - 150;
        
        if (episodeData == null)
        {
            GUI.Label(new Rect(10, yOffset, 500, 30), "Replay: No episode loaded", style);
            return;
        }
        
        // Display replay info
        string status = isPlaying ? (pauseOnFrame ? "PAUSED" : "PLAYING") : "STOPPED";
        GUI.Label(new Rect(10, yOffset, 500, 30), $"Replay: {status}", style);
        yOffset += 30;
        GUI.Label(new Rect(10, yOffset, 500, 30), $"Episode: {episodeData.episode}", style);
        yOffset += 30;
        GUI.Label(new Rect(10, yOffset, 500, 30), $"Frame: {currentReplayFrame} / {maxFrame} (Actions: {sortedActions?.Count ?? 0})", style);
        yOffset += 30;
        GUI.Label(new Rect(10, yOffset, 500, 30), $"Win: {episodeData.winCondition}", style);
        yOffset += 30;
        GUI.Label(new Rect(10, yOffset, 500, 30), $"Agents Mapped: {agentMap?.Count ?? 0}", style);
        
        // Show controls
        if (!isPlaying && episodeData != null)
        {
            style.fontSize = 14;
            yOffset += 40;
            GUI.Label(new Rect(10, yOffset, 500, 30), "Press 'R' to start replay", style);
        }
    }
    
    void OnEnable()
    {
        // Allow keyboard input to start replay
    }
    
    void OnDisable()
    {
        // Cleanup if needed
    }
}

