/*
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class EpisodeAction
{
    public int frame;
    public string agentId;
    public string branch;
    public int value;
}

[System.Serializable]
public class EpisodeData
{
    public int episode;
    public string winCondition;
    public float duration;
    public List<string> agentIds;
    public List<string> agentClassValues;
    public List<EpisodeAction> actions;
    
    public Dictionary<string, string> GetAgentClasses()
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        if (agentIds != null && agentClassValues != null)
        {
            for (int i = 0; i < Mathf.Min(agentIds.Count, agentClassValues.Count); i++)
            {
                result[agentIds[i]] = agentClassValues[i];
            }
        }
        return result;
    }
    
    public void SetAgentClasses(Dictionary<string, string> classes)
    {
        agentIds = new List<string>();
        agentClassValues = new List<string>();
        foreach (var kvp in classes)
        {
            agentIds.Add(kvp.Key);
            agentClassValues.Add(kvp.Value);
        }
    }
}

public class EpisodeRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private bool recordEpisodes = true;
    [SerializeField] private int saveEveryNEpisodes = 1000;
    [SerializeField] private string saveDirectory = "EpisodeData";
    
    private EpisodeData currentEpisode;
    private int currentFrame = 0;
    private int episodeCount = 0;
    private List<GameObject> allAgents;
    
    void Start()
    {
        if (!recordEpisodes) return;
        
        // Create save directory if it doesn't exist
        string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        
        FindAllAgents();
        
        // Wait for EpisodeManager to start first episode
        Invoke(nameof(StartNewEpisode), 0.2f);
    }
    
    public void OnEpisodeStart()
    {
        if (!recordEpisodes) return;
        StartNewEpisode();
    }
    
    void Update()
    {
        if (!recordEpisodes) return;
        
        currentFrame++;
        
        // Record actions from all agents
        RecordAgentActions();
    }
    
    void FindAllAgents()
    {
        allAgents = new List<GameObject>();
        
        // Find all party members
        PartyMemberAgent[] partyAgents = FindObjectsOfType<PartyMemberAgent>();
        foreach (PartyMemberAgent agent in partyAgents)
        {
            allAgents.Add(agent.gameObject);
        }
        
        // Find boss
        BossAgent bossAgent = FindObjectOfType<BossAgent>();
        if (bossAgent != null)
        {
            allAgents.Add(bossAgent.gameObject);
        }
    }
    
    void StartNewEpisode()
    {
        currentEpisode = new EpisodeData
        {
            episode = episodeCount,
            winCondition = "",
            duration = 0f,
            agentIds = new List<string>(),
            agentClassValues = new List<string>(),
            actions = new List<EpisodeAction>()
        };
        
        currentFrame = 0;
        
        // Record agent classes
        Dictionary<string, string> agentClasses = new Dictionary<string, string>();
        foreach (GameObject agent in allAgents)
        {
            string agentId = agent.name;
            string agentClass = "Unknown";
            
            PlayerClassSystem classSystem = agent.GetComponent<PlayerClassSystem>();
            if (classSystem != null)
            {
                agentClass = classSystem.CurrentClass.ToString();
            }
            else if (agent.GetComponent<BossAgent>() != null)
            {
                agentClass = "Boss";
            }
            
            agentClasses[agentId] = agentClass;
        }
        if (currentEpisode != null)
        {
            currentEpisode.SetAgentClasses(agentClasses);
        }
    }
    
    void RecordAgentActions()
    {
        // This would need to be called from agents when they take actions
        // For now, we'll record based on agent state changes
        // In a full implementation, agents would call RecordAction() directly
    }
    
    public void RecordAction(GameObject agent, string branch, int value)
    {
        if (!recordEpisodes || currentEpisode == null) return;
        
        EpisodeAction action = new EpisodeAction
        {
            frame = currentFrame,
            agentId = agent.name,
            branch = branch,
            value = value
        };
        
        currentEpisode.actions.Add(action);
    }
    
    public void EndEpisode(string winCondition, float duration)
    {
        if (!recordEpisodes || currentEpisode == null) return;
        
        currentEpisode.winCondition = winCondition;
        currentEpisode.duration = duration;
        
        // Save episode
        SaveEpisode(currentEpisode);
        
        episodeCount++;
        
        // Start new episode
        StartNewEpisode();
        
        // Save snapshot if needed
        if (episodeCount % saveEveryNEpisodes == 0)
        {
            SaveSnapshot(episodeCount);
        }
    }
    
    void SaveEpisode(EpisodeData episode)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory);
        
        // Save as JSON
        string jsonPath = Path.Combine(fullPath, $"episode_{episode.episode}.json");
        string json = JsonUtility.ToJson(episode, true);
        File.WriteAllText(jsonPath, json);
        
        // Save as binary
        string binaryPath = Path.Combine(fullPath, $"episode_{episode.episode}.bin");
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(binaryPath, FileMode.Create))
        {
            formatter.Serialize(stream, episode);
        }
        
        Debug.Log($"Saved episode {episode.episode} to {jsonPath} and {binaryPath}");
    }
    
    void SaveSnapshot(int episodeNumber)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, saveDirectory);
        string snapshotPath = Path.Combine(fullPath, $"snapshot_{episodeNumber}.json");
        
        // Create snapshot data (would include model weights, stats, etc.)
        Dictionary<string, object> snapshot = new Dictionary<string, object>
        {
            ["episode"] = episodeNumber,
            ["timestamp"] = System.DateTime.Now.ToString(),
            ["note"] = "Training snapshot"
        };
        
        string json = JsonUtility.ToJson(snapshot, true);
        File.WriteAllText(snapshotPath, json);
        
        Debug.Log($"Saved snapshot at episode {episodeNumber} to {snapshotPath}");
    }
}


*/
