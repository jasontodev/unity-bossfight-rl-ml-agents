/*
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerClassSystem))]
[RequireComponent(typeof(LIDARSystem))]
[RequireComponent(typeof(PlayerAttackSystem))]
public class PartyMemberAgent : Agent
{
    private HealthSystem healthSystem;
    private PlayerController playerController;
    private PlayerClassSystem classSystem;
    private LIDARSystem lidarSystem;
    private PlayerAttackSystem attackSystem;
    private ThreatSystem threatSystem;
    private EpisodeRecorder episodeRecorder;
    
    // Action space: 6 discrete branches
    // 0: Movement (0=no move, 1=forward, 2=backward)
    // 1: Rotation (0=no rotate, 1=left, 2=right)
    // 2: Attack (0=no attack, 1=attack) - Only works if class is selected
    // 3: Heal (0=no heal, 1=heal) - Healer only
    // 4: Threat Boost (0=no boost, 1=boost) - Tank only
    // 5: Class Selection (0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS) - Only works once per episode if no class selected
    
    private float moveInput = 0f;
    private float rotateInput = 0f;
    private bool shouldAttack = false;
    private bool shouldHeal = false;
    private bool shouldThreatBoost = false;
    private int selectedClass = -1; // -1 means no class change
    
    public override void Initialize()
    {
        // Force heuristic mode - disable ML-Agents training, use only manual control
        BehaviorParameters behaviorParams = GetComponent<BehaviorParameters>();
        if (behaviorParams != null)
        {
            behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;
        }
        
        healthSystem = GetComponent<HealthSystem>();
        playerController = GetComponent<PlayerController>();
        classSystem = GetComponent<PlayerClassSystem>();
        lidarSystem = GetComponent<LIDARSystem>();
        attackSystem = GetComponent<PlayerAttackSystem>();
        threatSystem = ThreatSystem.Instance;
        episodeRecorder = FindObjectOfType<EpisodeRecorder>();
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset agent state
        moveInput = 0f;
        rotateInput = 0f;
        shouldAttack = false;
        shouldHeal = false;
        shouldThreatBoost = false;
        selectedClass = -1;
        
        // Reset class selection for new episode
        if (classSystem != null)
        {
            classSystem.ResetClassForEpisode();
        }
        
        // Health and position will be reset by EpisodeManager
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        
        // Use ObservationEncoder to get all observations
        float[] observations = ObservationEncoder.EncodeObservations(gameObject, lidarSystem);
        
        // Add observations to sensor
        foreach (float obs in observations)
        {
            sensor.AddObservation(obs);
        }
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (healthSystem == null || healthSystem.IsDead)
        {
            // Dead agents can't act
            return;
        }
        
        // Check if action space is properly initialized
        if (actions.DiscreteActions.Length < 6)
        {
            Debug.LogWarning($"PartyMemberAgent: Action space not properly initialized. Expected 6 branches, got {actions.DiscreteActions.Length}. Please configure BehaviorParameters component.");
            return;
        }
        
        // Parse discrete actions
        int movementAction = actions.DiscreteActions[0];
        int rotationAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];
        int healAction = actions.DiscreteActions[3];
        int threatBoostAction = actions.DiscreteActions[4];
        int classSelectionAction = actions.DiscreteActions[5];
        
        // Movement: 0=no move, 1=forward, 2=backward
        moveInput = 0f;
        if (movementAction == 1)
        {
            moveInput = 1f; // Forward
        }
        else if (movementAction == 2)
        {
            moveInput = -1f; // Backward
        }
        
        // Rotation: 0=no rotate, 1=left, 2=right
        rotateInput = 0f;
        if (rotationAction == 1)
        {
            rotateInput = -1f; // Left
        }
        else if (rotationAction == 2)
        {
            rotateInput = 1f; // Right
        }
        
        // Attack: 0=no attack, 1=attack
        shouldAttack = (attackAction == 1);
        
        // Heal: 0=no heal, 1=heal (Healer only)
        shouldHeal = (healAction == 1 && classSystem != null && classSystem.CurrentClass == PlayerClass.Healer);
        
        // Threat Boost: 0=no boost, 1=boost (Tank only)
        shouldThreatBoost = (threatBoostAction == 1 && classSystem != null && classSystem.CurrentClass == PlayerClass.Tank);

        // NOTE: Class Selection via ML-Agents is DISABLED for now.
        // We are running in pure heuristic/manual mode, and class changes
        // are driven exclusively by PlayerClassSystem (U/I/O/P on the
        // currently selected agent). This prevents default actions from
        // forcing all agents into the Tank class at episode start.
        
        // Record actions
        if (episodeRecorder != null)
        {
            episodeRecorder.RecordAction(gameObject, "movement", movementAction);
            episodeRecorder.RecordAction(gameObject, "rotation", rotationAction);
            episodeRecorder.RecordAction(gameObject, "attack", attackAction);
            episodeRecorder.RecordAction(gameObject, "heal", healAction);
            episodeRecorder.RecordAction(gameObject, "threat_boost", threatBoostAction);
            episodeRecorder.RecordAction(gameObject, "class_selection", classSelectionAction);
        }
    }
    
    void Update()
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        
        // Execute movement and rotation
        if (playerController != null)
        {
            // Apply rotation
            if (Mathf.Abs(rotateInput) >= 0.1f)
            {
                float rotationSpeed = 180f; // 180 degrees per second
                float rotationAmount = rotateInput * rotationSpeed * Time.deltaTime;
                transform.Rotate(0f, rotationAmount, 0f);
            }
            
            // Apply movement
            if (Mathf.Abs(moveInput) >= 0.1f)
            {
                float moveSpeed = 5f;
                CharacterController charController = GetComponent<CharacterController>();
                if (charController != null)
                {
                    Vector3 moveDirection = transform.forward * moveInput;
                    charController.Move(moveDirection * moveSpeed * Time.deltaTime);
                }
            }
        }
        
        // Execute attack
        if (shouldAttack && attackSystem != null)
        {
            attackSystem.TryAttack();
            shouldAttack = false;
        }
        
        // Execute heal
        if (shouldHeal && classSystem != null)
        {
            classSystem.TryHeal();
            shouldHeal = false;
        }
        
        // Execute threat boost
        if (shouldThreatBoost && classSystem != null)
        {
            classSystem.TryThreatBoost();
            shouldThreatBoost = false;
        }
        
        // Execute class selection (only if not locked and no class selected)
        if (selectedClass >= 0 && selectedClass <= 3 && classSystem != null)
        {
            // Map action value to class (0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS)
            PlayerClass newClass = (PlayerClass)(selectedClass + 1); // +1 because None is 0
            bool success = classSystem.SetPlayerClass(newClass);
            if (success)
            {
                Debug.Log($"{gameObject.name} selected class: {newClass}");
            }
        }
        selectedClass = -1;
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
        // Only respond to input if this agent is selected
        if (ManualControlManager.Instance != null && !ManualControlManager.Instance.IsAgentSelected(gameObject))
        {
            // Set all actions to default (no action)
            if (discreteActions.Length >= 6)
            {
                discreteActions[0] = 0; // No move
                discreteActions[1] = 0; // No rotate
                discreteActions[2] = 0; // No attack
                discreteActions[3] = 0; // No heal
                discreteActions[4] = 0; // No threat boost
                discreteActions[5] = 4; // No class change (4 = ignore sentinel)
            }
            return;
        }
        
        // Heuristic for testing - can be controlled manually
        
        // Check if action space is properly initialized
        if (discreteActions.Length < 6)
        {
            Debug.LogWarning($"PartyMemberAgent: Action space not properly initialized. Expected 6 branches, got {discreteActions.Length}");
            return;
        }
        
        // Movement
        if (Input.GetKey(KeyCode.W))
        {
            discreteActions[0] = 1; // Forward
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActions[0] = 2; // Backward
        }
        else
        {
            discreteActions[0] = 0; // No move
        }
        
        // Rotation
        if (Input.GetKey(KeyCode.A))
        {
            discreteActions[1] = 1; // Left
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActions[1] = 2; // Right
        }
        else
        {
            discreteActions[1] = 0; // No rotate
        }
        
        // Attack
        discreteActions[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        
        // Heal
        discreteActions[3] = Input.GetKey(KeyCode.H) ? 1 : 0;
        
        // Threat Boost
        discreteActions[4] = Input.GetKey(KeyCode.T) ? 1 : 0;
        
        // Class Selection - map U, I, O, P keys to classes (to avoid conflict with agent selection 0-4)
        // U=Tank, I=Healer, O=RangedDPS, P=MeleeDPS
        // Only works if no class is selected yet
        if (classSystem != null && classSystem.CurrentClass == PlayerClass.None && !classSystem.IsClassLocked)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                discreteActions[5] = 0; // Tank
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                discreteActions[5] = 1; // Healer
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                discreteActions[5] = 2; // RangedDPS
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                discreteActions[5] = 3; // MeleeDPS
            }
            else
            {
                discreteActions[5] = 4; // No class selection (invalid value, will be ignored)
            }
        }
        else
        {
            discreteActions[5] = 4; // Class already selected, no selection possible
        }
    }
}


*/
