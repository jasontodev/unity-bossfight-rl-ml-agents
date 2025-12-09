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
        // ML-Agents training mode: Use Default behavior type
        // This will use remote trainer if available, fall back to inference/model, or heuristic
        BehaviorParameters behaviorParams = GetComponent<BehaviorParameters>();
        if (behaviorParams != null)
        {
            // Only set to HeuristicOnly if explicitly needed for testing
            // For training, use BehaviorType.Default (or leave as configured in Inspector)
            // behaviorParams.BehaviorType = BehaviorType.Default;
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

        // Class Selection: ML-Agents can now select classes (0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS)
        // Only works if no class is selected yet and class is not locked
        if (classSelectionAction >= 0 && classSelectionAction <= 3 && classSystem != null)
        {
            if (classSystem.CurrentClass == PlayerClass.None && !classSystem.IsClassLocked)
            {
                // Map action value to class (0=Tank, 1=Healer, 2=RangedDPS, 3=MeleeDPS)
                PlayerClass newClass = (PlayerClass)(classSelectionAction + 1); // +1 because None is 0
                selectedClass = classSelectionAction;
            }
        }
        
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
        
        // Check if we're in replay mode (EpisodeReplay exists and is playing)
        EpisodeReplay episodeReplay = FindObjectOfType<EpisodeReplay>();
        bool isReplayMode = episodeReplay != null && episodeReplay.IsPlaying;
        
        // Record actions every frame in heuristic mode (not just when Heuristic() is called)
        BehaviorParameters behaviorParams = GetComponent<BehaviorParameters>();
        bool isHeuristicMode = behaviorParams != null && behaviorParams.BehaviorType == BehaviorType.HeuristicOnly;
        bool isSelected = ManualControlManager.Instance == null || ManualControlManager.Instance.IsAgentSelected(gameObject);
        
        if (isHeuristicMode && !isReplayMode && episodeRecorder != null && isSelected)
        {
            // Record current input state every frame
            int movementAction = 0;
            if (Input.GetKey(KeyCode.W))
                movementAction = 1; // Forward
            else if (Input.GetKey(KeyCode.S))
                movementAction = 2; // Backward
            
            int rotationAction = 0;
            if (Input.GetKey(KeyCode.A))
                rotationAction = 1; // Left
            else if (Input.GetKey(KeyCode.D))
                rotationAction = 2; // Right
            
            int attackAction = Input.GetKey(KeyCode.Space) ? 1 : 0;
            int healAction = Input.GetKey(KeyCode.H) ? 1 : 0;
            int threatBoostAction = Input.GetKey(KeyCode.T) ? 1 : 0;
            
            int classSelectionAction = -1;
            if (classSystem != null && classSystem.CurrentClass == PlayerClass.None && !classSystem.IsClassLocked)
            {
                if (Input.GetKeyDown(KeyCode.U))
                    classSelectionAction = 0; // Tank
                else if (Input.GetKeyDown(KeyCode.I))
                    classSelectionAction = 1; // Healer
                else if (Input.GetKeyDown(KeyCode.O))
                    classSelectionAction = 2; // RangedDPS
                else if (Input.GetKeyDown(KeyCode.P))
                    classSelectionAction = 3; // MeleeDPS
            }
            
            // Record all actions every frame
            episodeRecorder.RecordAction(gameObject, "movement", movementAction);
            episodeRecorder.RecordAction(gameObject, "rotation", rotationAction);
            episodeRecorder.RecordAction(gameObject, "attack", attackAction);
            episodeRecorder.RecordAction(gameObject, "heal", healAction);
            episodeRecorder.RecordAction(gameObject, "threat_boost", threatBoostAction);
            episodeRecorder.RecordAction(gameObject, "class_selection", classSelectionAction);
        }
        
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
        
        // In replay mode, reset inputs AFTER using them to prevent persistence
        // The replay system applies new inputs at the start of the next frame (before agent Update runs)
        if (isReplayMode)
        {
            moveInput = 0f;
            rotateInput = 0f;
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
        
        // Reset inputs at the end of Update() during replay
        // Replay will set new inputs in LateUpdate() for the next frame
        if (isReplayMode)
        {
            moveInput = 0f;
            rotateInput = 0f;
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        
        // Check if we're in heuristic mode (for manual testing)
        BehaviorParameters behaviorParams = GetComponent<BehaviorParameters>();
        bool isHeuristicMode = behaviorParams != null && behaviorParams.BehaviorType == BehaviorType.HeuristicOnly;
        
        // Only respond to manual input if in heuristic mode and this agent is selected
        // But always record actions if in heuristic mode (for recording purposes)
        bool isSelected = ManualControlManager.Instance == null || ManualControlManager.Instance.IsAgentSelected(gameObject);
        
        if (!isHeuristicMode || !isSelected)
        {
            // Set all actions to default (no action) when not in heuristic mode or not selected
            if (discreteActions.Length >= 6)
            {
                discreteActions[0] = 0; // No move
                discreteActions[1] = 0; // No rotate
                discreteActions[2] = 0; // No attack
                discreteActions[3] = 0; // No heal
                discreteActions[4] = 0; // No threat boost
                discreteActions[5] = 4; // No class change (4 = ignore sentinel)
            }
            
            // Note: Actions are now recorded in Update() every frame, not here
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
        
        // Note: Actions are now recorded in Update() every frame, not here
        // This avoids duplicate recordings and ensures every frame is captured
    }
    
    // Method for replay system to apply recorded actions
    public void ApplyRecordedAction(string branch, int value)
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        switch (branch)
        {
            case "movement":
                moveInput = value == 1 ? 1f : value == 2 ? -1f : 0f;
                break;
            case "rotation":
                rotateInput = value == 1 ? -1f : value == 2 ? 1f : 0f;
                break;
            case "attack":
                shouldAttack = (value == 1);
                break;
            case "heal":
                shouldHeal = (value == 1 && classSystem != null && classSystem.CurrentClass == PlayerClass.Healer);
                break;
            case "threat_boost":
                shouldThreatBoost = (value == 1 && classSystem != null && classSystem.CurrentClass == PlayerClass.Tank);
                break;
            case "class_selection":
                if (value >= 0 && value <= 3 && classSystem != null)
                {
                    if (classSystem.CurrentClass == PlayerClass.None && !classSystem.IsClassLocked)
                    {
                        selectedClass = value;
                    }
                }
                break;
        }
    }
}

