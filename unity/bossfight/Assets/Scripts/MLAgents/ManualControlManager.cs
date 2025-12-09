using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents.Policies;

/// <summary>
/// Manages manual control of agents. Press number keys 0-4 to switch between agents.
/// 0-3: Party members (Tank, Healer, RangedDPS, MeleeDPS)
/// 4: Boss
/// </summary>
public class ManualControlManager : MonoBehaviour
{
    public static ManualControlManager Instance { get; private set; }
    
    [Header("Control Settings")]
    [SerializeField] private int selectedAgentIndex = 1; // 1-4 for party, 5 for boss
    
    private List<PartyMemberAgent> partyAgents = new List<PartyMemberAgent>();
    private BossAgent bossAgent;
    private GameObject selectedAgentObject;
    
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
        FindAllAgents();
        SelectAgent(1); // Start with first party member (key 1)
    }
    
    void Update()
    {
        // Only allow manual control if at least one agent is in heuristic mode
        bool anyAgentInHeuristicMode = false;
        foreach (var agent in partyAgents)
        {
            if (agent != null)
            {
                BehaviorParameters behaviorParams = agent.GetComponent<BehaviorParameters>();
                if (behaviorParams != null && behaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
                {
                    anyAgentInHeuristicMode = true;
                    break;
                }
            }
        }
        if (bossAgent != null)
        {
            BehaviorParameters bossBehaviorParams = bossAgent.GetComponent<BehaviorParameters>();
            if (bossBehaviorParams != null && bossBehaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
            {
                anyAgentInHeuristicMode = true;
            }
        }
        
        // Only respond to input if in heuristic mode
        if (!anyAgentInHeuristicMode) return;
        
        // Check for number key presses to switch agents (1-5 instead of 0-4)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectAgent(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectAgent(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            SelectAgent(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            SelectAgent(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            SelectAgent(5);
        }
    }
    
    void FindAllAgents()
    {
        partyAgents.Clear();
        partyAgents.AddRange(FindObjectsOfType<PartyMemberAgent>());
        bossAgent = FindObjectOfType<BossAgent>();
    }
    
    public void SelectAgent(int index)
    {
        selectedAgentIndex = index;
        
        if (index >= 1 && index <= partyAgents.Count)
        {
            // Index 1-4 map to partyAgents[0-3]
            int partyIdx = index - 1;
            selectedAgentObject = partyAgents[partyIdx].gameObject;
            Debug.Log($"Selected Party Member {index} ({GetAgentName(selectedAgentObject)})");
        }
        else if (index == 5 && bossAgent != null)
        {
            selectedAgentObject = bossAgent.gameObject;
            Debug.Log("Selected Boss (5)");
        }
        else
        {
            selectedAgentObject = null;
            Debug.LogWarning($"Invalid agent index: {index}");
        }
    }
    
    public bool IsAgentSelected(GameObject agent)
    {
        return selectedAgentObject == agent;
    }
    
    public GameObject GetSelectedAgent()
    {
        return selectedAgentObject;
    }
    
    public int GetSelectedAgentIndex()
    {
        return selectedAgentIndex;
    }
    
    string GetAgentName(GameObject agent)
    {
        PlayerClassSystem classSystem = agent.GetComponent<PlayerClassSystem>();
        if (classSystem != null)
        {
            return classSystem.CurrentClass.ToString();
        }
        return agent.name;
    }
    
    void OnGUI()
    {
        // Only show GUI if in heuristic mode
        bool anyAgentInHeuristicMode = false;
        foreach (var agent in partyAgents)
        {
            if (agent != null)
            {
                BehaviorParameters behaviorParams = agent.GetComponent<BehaviorParameters>();
                if (behaviorParams != null && behaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
                {
                    anyAgentInHeuristicMode = true;
                    break;
                }
            }
        }
        if (!anyAgentInHeuristicMode && bossAgent != null)
        {
            BehaviorParameters bossBehaviorParams = bossAgent.GetComponent<BehaviorParameters>();
            if (bossBehaviorParams != null && bossBehaviorParams.BehaviorType == BehaviorType.HeuristicOnly)
            {
                anyAgentInHeuristicMode = true;
            }
        }
        
        if (!anyAgentInHeuristicMode)
        {
            // Show ML-Agents training mode indicator
            GUI.Label(new Rect(10, 10, 400, 30), "ML-Agents Training Mode Active");
            return;
        }
        
        // Display which agent is currently selected (heuristic mode)
        if (selectedAgentObject != null)
        {
            string agentName = GetAgentName(selectedAgentObject);
            GUI.Label(new Rect(10, 10, 300, 30), $"Controlling: {agentName} (Key {selectedAgentIndex})");
            GUI.Label(new Rect(10, 40, 400, 30), "Press 1-4 for Party Members, 5 for Boss");
            
            // Show class selection hint if party member has no class
            PlayerClassSystem classSystem = selectedAgentObject.GetComponent<PlayerClassSystem>();
            if (classSystem != null && classSystem.CurrentClass == PlayerClass.None && !classSystem.IsClassLocked)
            {
                GUI.Label(new Rect(10, 70, 500, 30), "Press U=Tank, I=Healer, O=RangedDPS, P=MeleeDPS to select class");
            }
        }
    }
}

