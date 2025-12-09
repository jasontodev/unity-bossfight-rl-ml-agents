using UnityEngine;
using System.Collections.Generic;

public class ThreatSystem : MonoBehaviour
{
    private static ThreatSystem instance;
    
    // Dictionary to track threat per player
    private Dictionary<GameObject, float> playerThreat = new Dictionary<GameObject, float>();
    
    [Header("Threat Settings")]
    [SerializeField] private float threatDecayRate = 0.5f; // Threat decays over time
    [SerializeField] private bool enableThreatDecay = false;
    
    public static ThreatSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ThreatSystem>();
                if (instance == null)
                {
                    GameObject threatManager = new GameObject("Threat System");
                    instance = threatManager.AddComponent<ThreatSystem>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Decay threat over time if enabled
        if (enableThreatDecay)
        {
            List<GameObject> players = new List<GameObject>(playerThreat.Keys);
            foreach (GameObject player in players)
            {
                if (player != null)
                {
                    playerThreat[player] = Mathf.Max(0f, playerThreat[player] - threatDecayRate * Time.deltaTime);
                }
            }
        }
    }
    
    /// <summary>
    /// Add threat to a player based on damage dealt
    /// </summary>
    public void AddThreatFromDamage(GameObject player, float damage)
    {
        if (player == null) return;
        
        if (!playerThreat.ContainsKey(player))
        {
            playerThreat[player] = 0f;
        }
        
        // Threat equals damage dealt
        playerThreat[player] += damage;
    }
    
    /// <summary>
    /// Add threat to a healer when they heal (3x DPS attack threat)
    /// </summary>
    public void AddThreatFromHeal(GameObject healer, float baseDPSDamage)
    {
        if (healer == null) return;
        
        if (!playerThreat.ContainsKey(healer))
        {
            playerThreat[healer] = 0f;
        }
        
        // Healer generates 3x the threat of a DPS single attack
        float healThreat = baseDPSDamage * 3f;
        playerThreat[healer] += healThreat;
    }
    
    /// <summary>
    /// Add threat boost for tank (5x DPS attack threat)
    /// </summary>
    public void AddThreatBoost(GameObject tank, float baseDPSDamage)
    {
        if (tank == null) return;
        
        if (!playerThreat.ContainsKey(tank))
        {
            playerThreat[tank] = 0f;
        }
        
        // Tank generates 5x the threat of a DPS single attack
        float boostThreat = baseDPSDamage * 5f;
        playerThreat[tank] += boostThreat;
    }
    
    /// <summary>
    /// Get current threat for a player
    /// </summary>
    public float GetThreat(GameObject player)
    {
        if (player == null || !playerThreat.ContainsKey(player))
        {
            return 0f;
        }
        return playerThreat[player];
    }
    
    /// <summary>
    /// Get the player with the highest threat (for aggro)
    /// </summary>
    public GameObject GetHighestThreatPlayer()
    {
        GameObject highestThreatPlayer = null;
        float highestThreat = 0f;
        
        foreach (var kvp in playerThreat)
        {
            if (kvp.Key != null && kvp.Value > highestThreat)
            {
                highestThreat = kvp.Value;
                highestThreatPlayer = kvp.Key;
            }
        }
        
        return highestThreatPlayer;
    }
    
    /// <summary>
    /// Alias for GetHighestThreatPlayer (for testing compatibility)
    /// </summary>
    public GameObject GetHighestThreatAgent()
    {
        return GetHighestThreatPlayer();
    }
    
    /// <summary>
    /// Add raw threat amount (for testing purposes)
    /// </summary>
    public void AddThreat(GameObject player, float threatAmount)
    {
        if (player == null) return;
        
        if (!playerThreat.ContainsKey(player))
        {
            playerThreat[player] = 0f;
        }
        
        playerThreat[player] += threatAmount;
    }
    
    /// <summary>
    /// Get all players sorted by threat (highest first)
    /// </summary>
    public List<GameObject> GetPlayersByThreat()
    {
        List<KeyValuePair<GameObject, float>> sortedThreat = new List<KeyValuePair<GameObject, float>>();
        
        foreach (var kvp in playerThreat)
        {
            if (kvp.Key != null)
            {
                sortedThreat.Add(kvp);
            }
        }
        
        sortedThreat.Sort((a, b) => b.Value.CompareTo(a.Value));
        
        List<GameObject> result = new List<GameObject>();
        foreach (var kvp in sortedThreat)
        {
            result.Add(kvp.Key);
        }
        
        return result;
    }
    
    /// <summary>
    /// Clear threat for a specific player
    /// </summary>
    public void ClearThreat(GameObject player)
    {
        if (player != null && playerThreat.ContainsKey(player))
        {
            playerThreat.Remove(player);
        }
    }
    
    /// <summary>
    /// Clear all threat
    /// </summary>
    public void ClearAllThreat()
    {
        playerThreat.Clear();
    }
    
    void OnGUI()
    {
        // Display threat for all players in bottom left
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.LowerLeft;
        
        float yPos = Screen.height - 50f;
        float lineHeight = 20f;
        float xPos = 10f;
        
        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(xPos, yPos - 20f, 300, 30), "Threat Levels:", style);
        
        List<GameObject> playersByThreat = GetPlayersByThreat();
        int displayCount = 0;
        foreach (GameObject player in playersByThreat)
        {
            if (player != null && displayCount < 5) // Show top 5
            {
                float threat = GetThreat(player);
                PlayerClassSystem classSystem = player.GetComponent<PlayerClassSystem>();
                string playerName = classSystem != null ? $"{classSystem.CurrentClass}" : player.name;
                
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(xPos, yPos, 300, 30), $"{playerName}: {threat:F1}", style);
                yPos -= lineHeight;
                displayCount++;
            }
        }
    }
}

