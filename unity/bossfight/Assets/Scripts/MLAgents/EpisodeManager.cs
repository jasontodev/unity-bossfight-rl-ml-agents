using UnityEngine;
using Unity.MLAgents;

public class EpisodeManager : MonoBehaviour
{
    [Header("Episode Settings")]
    [SerializeField] private float maxEpisodeLength = 300f; // 5 minutes max
    [SerializeField] private float timeScale = 1f; // Can be increased for faster training
    
    [Header("Spawn Positions")]
    [SerializeField] private Vector3 bossSpawnPosition = Vector3.zero;
    [SerializeField] private Vector3[] partySpawnPositions = new Vector3[4];
    [SerializeField] private Vector3[] wallSpawnPositions = new Vector3[3];
    
    private GameObject boss;
    private GameObject[] partyMembers = new GameObject[4];
    private GameObject[] walls = new GameObject[3];
    private float episodeStartTime;
    private bool episodeActive = false;
    private string winCondition = "";
    private EpisodeRecorder episodeRecorder;
    
    public static EpisodeManager Instance { get; private set; }
    
    public bool EpisodeActive => episodeActive;
    public string WinCondition => winCondition;
    public float EpisodeDuration => Time.time - episodeStartTime;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    void Start()
    {
        Time.timeScale = timeScale;
        episodeRecorder = FindObjectOfType<EpisodeRecorder>();
        FindAllAgents();
        StartEpisode();
    }
    
    void Update()
    {
        if (!episodeActive) return;
        
        // Check for episode end conditions
        CheckEpisodeEnd();
        
        // Check for timeout
        if (EpisodeDuration >= maxEpisodeLength)
        {
            EndEpisode("timeout");
        }
    }
    
    void FindAllAgents()
    {
        // Find boss
        BossController bossController = FindObjectOfType<BossController>();
        if (bossController != null)
        {
            boss = bossController.gameObject;
        }
        
        // Find party members
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        for (int i = 0; i < Mathf.Min(players.Length, 4); i++)
        {
            partyMembers[i] = players[i].gameObject;
        }
        
        // Find walls
        Wall[] wallObjects = FindObjectsOfType<Wall>();
        for (int i = 0; i < Mathf.Min(wallObjects.Length, 3); i++)
        {
            walls[i] = wallObjects[i].gameObject;
        }
    }
    
    public void StartEpisode()
    {
        episodeActive = true;
        episodeStartTime = Time.time;
        winCondition = "";
        
        // Reset all agents
        ResetBoss();
        ResetPartyMembers();
        ResetWalls();
        
        // Clear threat
        ThreatSystem threatSystem = ThreatSystem.Instance;
        if (threatSystem != null)
        {
            threatSystem.ClearAllThreat();
        }
        
        // Notify EpisodeRecorder
        if (episodeRecorder != null)
        {
            episodeRecorder.OnEpisodeStart();
        }
        
        // Notify Academy
        if (Academy.Instance != null)
        {
            Academy.Instance.EnvironmentStep();
        }
    }
    
    void CheckEpisodeEnd()
    {
        // Check if boss is dead
        if (boss != null)
        {
            HealthSystem bossHealth = boss.GetComponent<HealthSystem>();
            if (bossHealth != null && bossHealth.IsDead)
            {
                EndEpisode("party");
                return;
            }
        }
        
        // Check if all party members are dead
        bool allPartyDead = true;
        foreach (GameObject partyMember in partyMembers)
        {
            if (partyMember != null)
            {
                HealthSystem partyHealth = partyMember.GetComponent<HealthSystem>();
                if (partyHealth != null && !partyHealth.IsDead)
                {
                    allPartyDead = false;
                    break;
                }
            }
        }
        
        if (allPartyDead && partyMembers[0] != null) // At least one party member exists
        {
            EndEpisode("boss");
        }
    }
    
    void EndEpisode(string condition)
    {
        if (!episodeActive) return;
        
        episodeActive = false;
        winCondition = condition;
        
        float duration = EpisodeDuration;
        
        // Record episode end
        if (episodeRecorder != null)
        {
            episodeRecorder.EndEpisode(condition, duration);
        }
        
        // Distribute rewards
        DistributeRewards(condition);
        
        // Reset after a short delay
        Invoke(nameof(StartEpisode), 0.1f);
    }
    
    void DistributeRewards(string condition)
    {
        if (condition == "party")
        {
            // Party wins: +1 for party, -1 for boss
            foreach (GameObject partyMember in partyMembers)
            {
                if (partyMember != null)
                {
                    var agent = partyMember.GetComponent<Unity.MLAgents.Agent>();
                    if (agent != null)
                    {
                        agent.AddReward(1f);
                        agent.EndEpisode();
                    }
                }
            }
            
            if (boss != null)
            {
                var bossAgent = boss.GetComponent<Unity.MLAgents.Agent>();
                if (bossAgent != null)
                {
                    bossAgent.AddReward(-1f);
                    bossAgent.EndEpisode();
                }
            }
        }
        else if (condition == "boss")
        {
            // Boss wins: +1 for boss, -1 for party
            if (boss != null)
            {
                var bossAgent = boss.GetComponent<Unity.MLAgents.Agent>();
                if (bossAgent != null)
                {
                    bossAgent.AddReward(1f);
                    bossAgent.EndEpisode();
                }
            }
            
            foreach (GameObject partyMember in partyMembers)
            {
                if (partyMember != null)
                {
                    var agent = partyMember.GetComponent<Unity.MLAgents.Agent>();
                    if (agent != null)
                    {
                        agent.AddReward(-1f);
                        agent.EndEpisode();
                    }
                }
            }
        }
        else if (condition == "timeout")
        {
            // Timeout: no rewards (or small negative for both)
            if (boss != null)
            {
                var bossAgent = boss.GetComponent<Unity.MLAgents.Agent>();
                if (bossAgent != null)
                {
                    bossAgent.EndEpisode();
                }
            }
            
            foreach (GameObject partyMember in partyMembers)
            {
                if (partyMember != null)
                {
                    var agent = partyMember.GetComponent<Unity.MLAgents.Agent>();
                    if (agent != null)
                    {
                        agent.EndEpisode();
                    }
                }
            }
        }
    }
    
    void ResetBoss()
    {
        if (boss == null) return;
        
        HealthSystem bossHealth = boss.GetComponent<HealthSystem>();
        if (bossHealth != null)
        {
            bossHealth.Respawn();
            bossHealth.StartVoidGrace(0.2f);
        }
        
        // Use localPosition if boss is a child, otherwise use world position
        if (boss.transform.parent != null)
        {
            boss.transform.localPosition = bossSpawnPosition;
        }
        else
        {
            boss.transform.position = bossSpawnPosition;
        }
        boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face -Z (toward party)
        
        // Reset boss components
        BossController bossController = boss.GetComponent<BossController>();
        if (bossController != null)
        {
            bossController.enabled = true;
            bossController.ResetMovementState();
        }
    }
    
    void ResetPartyMembers()
    {
        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] == null) continue;
            
            HealthSystem partyHealth = partyMembers[i].GetComponent<HealthSystem>();
            if (partyHealth != null)
            {
                partyHealth.Respawn();
                partyHealth.StartVoidGrace(0.2f);
            }
            
            if (i < partySpawnPositions.Length)
            {
                // Use localPosition if party member is a child, otherwise use world position
                if (partyMembers[i].transform.parent != null)
                {
                    partyMembers[i].transform.localPosition = partySpawnPositions[i];
                }
                else
                {
                    partyMembers[i].transform.position = partySpawnPositions[i];
                }
                    partyMembers[i].transform.rotation = Quaternion.identity; // Face +Z (toward boss)
            }
            
            // Reset player components
            PlayerController playerController = partyMembers[i].GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.ResetMovementState();
            }
            
            // Reset class to None for new episode
            PlayerClassSystem classSystem = partyMembers[i].GetComponent<PlayerClassSystem>();
            if (classSystem != null)
            {
                classSystem.ResetClassForEpisode();
            }
        }
    }
    
    void ResetWalls()
    {
        for (int i = 0; i < walls.Length; i++)
        {
            if (walls[i] == null) continue;
            
            Wall wall = walls[i].GetComponent<Wall>();
            if (wall != null && wall.IsBeingCarried())
            {
                wall.PlaceDown();
            }
            
            if (i < wallSpawnPositions.Length)
            {
                walls[i].transform.position = wallSpawnPositions[i];
                walls[i].transform.rotation = Quaternion.identity;
            }
        }
    }
    
    public void SetSpawnPositions(Vector3 bossPos, Vector3[] partyPositions, Vector3[] wallPositions)
    {
        bossSpawnPosition = bossPos;
        partySpawnPositions = partyPositions;
        wallSpawnPositions = wallPositions;
    }
}

