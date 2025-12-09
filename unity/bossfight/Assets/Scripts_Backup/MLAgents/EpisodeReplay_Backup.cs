/*
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

public class EpisodeReplay : MonoBehaviour
{
    [Header("Replay Settings")]
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool pauseOnFrame = false;
    
    private EpisodeData episodeData;
    private int currentReplayFrame = 0;
    private float replayTime = 0f;
    private Dictionary<string, GameObject> agentMap;
    private List<EpisodeAction> sortedActions;
    
    void Start()
    {
        agentMap = new Dictionary<string, GameObject>();
    }
    
    void Update()
    {
        if (!isPlaying || episodeData == null) return;
        
        if (pauseOnFrame)
        {
            // Step frame by frame
            return;
        }
        
        replayTime += Time.deltaTime * playbackSpeed;
        
        // Execute actions for current frame
        ExecuteActionsForFrame(currentReplayFrame);
        
        // Advance to next frame if enough time has passed
        if (replayTime >= (1f / 60f)) // Assuming 60 FPS
        {
            currentReplayFrame++;
            replayTime = 0f;
            
            if (currentReplayFrame >= episodeData.actions.Count)
            {
                StopReplay();
            }
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
            
            // Map agents
            MapAgents();
            
            Debug.Log($"Loaded episode {episodeData.episode} with {episodeData.actions.Count} actions");
        }
    }
    
    void MapAgents()
    {
        agentMap.Clear();
        
        // Find all agents in scene
        PartyMemberAgent[] partyAgents = FindObjectsOfType<PartyMemberAgent>();
        foreach (PartyMemberAgent agent in partyAgents)
        {
            agentMap[agent.gameObject.name] = agent.gameObject;
        }
        
        BossAgent bossAgent = FindObjectOfType<BossAgent>();
        if (bossAgent != null)
        {
            agentMap[bossAgent.gameObject.name] = bossAgent.gameObject;
        }
    }
    
    public void StartReplay()
    {
        if (episodeData == null)
        {
            Debug.LogError("No episode loaded. Load an episode first.");
            return;
        }
        
        isPlaying = true;
        currentReplayFrame = 0;
        replayTime = 0f;
        
        // Reset scene to initial state
        ResetSceneForReplay();
        
        Debug.Log($"Starting replay of episode {episodeData.episode}");
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
        
        foreach (EpisodeAction action in frameActions)
        {
            if (!agentMap.ContainsKey(action.agentId)) continue;
            
            GameObject agent = agentMap[action.agentId];
            
            // Execute action based on branch
            ExecuteAction(agent, action.branch, action.value);
        }
    }
    
    void ExecuteAction(GameObject agent, string branch, int value)
    {
        // This would need to interface with agents to replay actions
        // For now, this is a placeholder that would need to be implemented
        // based on how actions are structured in the agents
        
        Debug.Log($"Replaying: {agent.name} - {branch} = {value}");
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
        if (episodeData == null) return;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        
        // Display replay info
        string status = isPlaying ? (pauseOnFrame ? "PAUSED" : "PLAYING") : "STOPPED";
        GUI.Label(new Rect(10, Screen.height - 100, 300, 30), $"Replay: {status}", style);
        GUI.Label(new Rect(10, Screen.height - 70, 300, 30), $"Episode: {episodeData.episode}", style);
        GUI.Label(new Rect(10, Screen.height - 40, 300, 30), $"Frame: {currentReplayFrame} / {sortedActions?.Count ?? 0}", style);
        GUI.Label(new Rect(10, Screen.height - 10, 300, 30), $"Win: {episodeData.winCondition}", style);
    }
}


*/
