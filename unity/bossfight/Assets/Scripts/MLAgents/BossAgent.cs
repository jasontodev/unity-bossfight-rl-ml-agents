using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(LIDARSystem))]
[RequireComponent(typeof(BossAttackSystem))]
[RequireComponent(typeof(BossWallSystem))]
public class BossAgent : Agent
{
    private HealthSystem healthSystem;
    private BossController bossController;
    private LIDARSystem lidarSystem;
    private BossAttackSystem attackSystem;
    private BossWallSystem wallSystem;
    private EpisodeRecorder episodeRecorder;
    
    // Action space: 5 discrete branches
    // 0: Movement (0=no move, 1=forward, 2=backward)
    // 1: Rotation (0=no rotate, 1=left, 2=right)
    // 2: Attack (0=no attack, 1=attack)
    // 3: Wall Pickup (0=no pickup, 1=pickup)
    // 4: Wall Place (0=no place, 1=place)
    
    private float moveInput = 0f;
    private float rotateInput = 0f;
    private bool shouldAttack = false;
    private bool shouldPickupWall = false;
    private bool shouldPlaceWall = false;
    
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
        bossController = GetComponent<BossController>();
        lidarSystem = GetComponent<LIDARSystem>();
        attackSystem = GetComponent<BossAttackSystem>();
        wallSystem = GetComponent<BossWallSystem>();
        episodeRecorder = FindObjectOfType<EpisodeRecorder>();
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset agent state
        moveInput = 0f;
        rotateInput = 0f;
        shouldAttack = false;
        shouldPickupWall = false;
        shouldPlaceWall = false;
        
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
        if (actions.DiscreteActions.Length < 5)
        {
            Debug.LogWarning($"BossAgent: Action space not properly initialized. Expected 5 branches, got {actions.DiscreteActions.Length}. Please configure BehaviorParameters component.");
            return;
        }
        
        // Parse discrete actions
        int movementAction = actions.DiscreteActions[0];
        int rotationAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];
        int wallPickupAction = actions.DiscreteActions[3];
        int wallPlaceAction = actions.DiscreteActions[4];
        
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
        
        // Wall Pickup: 0=no pickup, 1=pickup
        shouldPickupWall = (wallPickupAction == 1);
        
        // Wall Place: 0=no place, 1=place
        shouldPlaceWall = (wallPlaceAction == 1);
        
        // Record actions
        if (episodeRecorder != null)
        {
            episodeRecorder.RecordAction(gameObject, "movement", movementAction);
            episodeRecorder.RecordAction(gameObject, "rotation", rotationAction);
            episodeRecorder.RecordAction(gameObject, "attack", attackAction);
            episodeRecorder.RecordAction(gameObject, "wall_pickup", wallPickupAction);
            episodeRecorder.RecordAction(gameObject, "wall_place", wallPlaceAction);
        }
    }
    
    void Update()
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        
        // Record actions every frame in heuristic mode (not just when Heuristic() is called)
        BehaviorParameters behaviorParams = GetComponent<BehaviorParameters>();
        bool isHeuristicMode = behaviorParams != null && behaviorParams.BehaviorType == BehaviorType.HeuristicOnly;
        bool isSelected = ManualControlManager.Instance == null || ManualControlManager.Instance.IsAgentSelected(gameObject);
        
        if (isHeuristicMode && episodeRecorder != null && isSelected)
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
            int wallPickupAction = Input.GetKey(KeyCode.E) ? 1 : 0;
            int wallPlaceAction = Input.GetKey(KeyCode.Q) ? 1 : 0;
            
            // Record all actions every frame
            episodeRecorder.RecordAction(gameObject, "movement", movementAction);
            episodeRecorder.RecordAction(gameObject, "rotation", rotationAction);
            episodeRecorder.RecordAction(gameObject, "attack", attackAction);
            episodeRecorder.RecordAction(gameObject, "wall_pickup", wallPickupAction);
            episodeRecorder.RecordAction(gameObject, "wall_place", wallPlaceAction);
        }
        
        // Execute movement and rotation
        if (bossController != null)
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
        
        // Execute wall pickup
        if (shouldPickupWall && wallSystem != null)
        {
            wallSystem.TryPickupWall();
            shouldPickupWall = false;
        }
        
        // Execute wall place
        if (shouldPlaceWall && wallSystem != null)
        {
            wallSystem.PlaceWall();
            shouldPlaceWall = false;
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
            if (discreteActions.Length >= 5)
            {
                discreteActions[0] = 0; // No move
                discreteActions[1] = 0; // No rotate
                discreteActions[2] = 0; // No attack
                discreteActions[3] = 0; // No wall pickup
                discreteActions[4] = 0; // No wall place
            }
            
            // Note: Actions are now recorded in Update() every frame, not here
            return;
        }
        
        // Heuristic for testing - can be controlled manually
        
        // Check if action space is properly initialized
        if (discreteActions.Length < 5)
        {
            Debug.LogWarning($"BossAgent: Action space not properly initialized. Expected 5 branches, got {discreteActions.Length}");
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
        
        // Wall Pickup
        discreteActions[3] = Input.GetKey(KeyCode.E) ? 1 : 0;
        
        // Wall Place
        discreteActions[4] = Input.GetKey(KeyCode.Q) ? 1 : 0;
        
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
            case "wall_pickup":
                shouldPickupWall = (value == 1);
                break;
            case "wall_place":
                shouldPlaceWall = (value == 1);
                break;
        }
    }
}

