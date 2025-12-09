using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

public class MLAgentsSceneSetup
{
    [MenuItem("ML-Agents/Setup Training Scene")]
    static void SetupMLAgentsScene()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        // Remove existing EpisodeManager if any
        EpisodeManager existingManager = Object.FindObjectOfType<EpisodeManager>();
        if (existingManager != null)
        {
            Object.DestroyImmediate(existingManager.gameObject);
        }
        
        // Academy is a singleton, no need to remove it
        
        GameObject bossFightParent = CreateArenaAndLava();
        
        // Create boss in center, facing -Z
        GameObject boss = CreateBossForMLAgents(bossFightParent);
        
        // Create 4 party members in a group, facing +Z
        GameObject[] partyMembers = CreatePartyMembersForMLAgents(bossFightParent);
        
        // Create 3 walls around boss
        GameObject[] walls = CreateStrategicWallsForMLAgents(bossFightParent);
        
        // Set up EpisodeManager
        SetupEpisodeManager(bossFightParent, boss, partyMembers, walls);
        
        // Set up Academy
        SetupAcademy();
        
        // Set up ThreatSystem
        SetupThreatSystem();
        
        // Set up EpisodeRecorder
        SetupEpisodeRecorder(bossFightParent);
        
        // Set up ManualControlManager
        SetupManualControlManager(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("ML-Agents Training Scene created! Boss in center, 4 party members in group, 3 walls around boss.");
    }
    
    static GameObject CreateArenaAndLava()
    {
        // Ensure tags exist
        EnsureTagExists("Lava");
        EnsureTagExists("Void");
        
        // Create parent GameObject
        GameObject bossFightParent = new GameObject("Boss Fight Arena");
        
        // Arena dimensions (2x bigger)
        float arenaSize = 20f;
        float lavaSize = 28f;
        float arenaY = 0f;
        float lavaY = -0.1f;
        
        // Create red lava
        GameObject redLava = GameObject.CreatePrimitive(PrimitiveType.Plane);
        redLava.name = "Red Lava";
        redLava.transform.parent = bossFightParent.transform;
        redLava.transform.localPosition = new Vector3(0f, lavaY, 0f);
        redLava.transform.localScale = new Vector3(lavaSize / 10f, 1f, lavaSize / 10f);
        redLava.transform.localRotation = Quaternion.identity;
        redLava.tag = "Lava";
        
        MeshCollider meshCollider = redLava.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            Object.DestroyImmediate(meshCollider);
        }
        
        BoxCollider boxCollider = redLava.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false;
        boxCollider.size = new Vector3(10f, 0.1f, 10f);
        boxCollider.center = new Vector3(0f, 0f, 0f);
        
        Material redLavaMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        redLavaMaterial.color = Color.red;
        redLavaMaterial.SetFloat("_Metallic", 0.3f);
        redLavaMaterial.SetFloat("_Smoothness", 0.2f);
        redLava.GetComponent<Renderer>().material = redLavaMaterial;
        
        // Create green arena
        GameObject greenArena = GameObject.CreatePrimitive(PrimitiveType.Plane);
        greenArena.name = "Green Arena";
        greenArena.transform.parent = bossFightParent.transform;
        greenArena.transform.localPosition = new Vector3(0f, arenaY, 0f);
        greenArena.transform.localScale = new Vector3(arenaSize / 10f, 1f, arenaSize / 10f);
        greenArena.transform.localRotation = Quaternion.identity;
        
        MeshCollider arenaMeshCollider = greenArena.GetComponent<MeshCollider>();
        if (arenaMeshCollider != null)
        {
            Object.DestroyImmediate(arenaMeshCollider);
        }
        
        BoxCollider arenaBoxCollider = greenArena.AddComponent<BoxCollider>();
        arenaBoxCollider.size = new Vector3(10f, 0.1f, 10f);
        arenaBoxCollider.center = new Vector3(0f, 0f, 0f);
        arenaBoxCollider.isTrigger = false;
        
        Material greenArenaMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        greenArenaMaterial.color = new Color(0.1f, 0.4f, 0.15f, 1f);
        greenArenaMaterial.SetFloat("_Metallic", 0.2f);
        greenArenaMaterial.SetFloat("_Smoothness", 0.3f);
        greenArena.GetComponent<Renderer>().material = greenArenaMaterial;
        
        // Create void (2x bigger)
        float voidSize = 32f;
        float voidY = -0.2f;
        
        GameObject voidArea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        voidArea.name = "Void";
        voidArea.transform.parent = bossFightParent.transform;
        voidArea.transform.localPosition = new Vector3(0f, voidY, 0f);
        voidArea.transform.localScale = new Vector3(voidSize / 10f, 1f, voidSize / 10f);
        voidArea.transform.localRotation = Quaternion.identity;
        voidArea.tag = "Void";
        
        MeshCollider voidMeshCollider = voidArea.GetComponent<MeshCollider>();
        if (voidMeshCollider != null)
        {
            Object.DestroyImmediate(voidMeshCollider);
        }
        
        BoxCollider voidCollider = voidArea.AddComponent<BoxCollider>();
        voidCollider.isTrigger = true;
        voidCollider.size = new Vector3(10f, 0.1f, 10f);
        voidCollider.center = new Vector3(0f, 0f, 0f);
        
        voidArea.AddComponent<VoidKiller>();
        
        Material voidMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        voidMaterial.color = new Color(0f, 0f, 0f, 0.8f);
        voidArea.GetComponent<Renderer>().material = voidMaterial;
        
        // Setup camera
        SetupCamera();
        
        Undo.RegisterCreatedObjectUndo(bossFightParent, "Create ML-Agents Arena");
        
        return bossFightParent;
    }
    
    static void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        
        // Adjust camera for larger arena (higher and further back)
        mainCamera.transform.position = new Vector3(0f, 14f, -20f); // 2x distance
        mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }
    
    static GameObject CreateBossForMLAgents(GameObject bossFightParent)
    {
        GameObject boss = new GameObject("Boss");
        boss.transform.parent = bossFightParent.transform;
        
        float bossScaleFactor = 1.0f;
        float bossHeight = 2f * bossScaleFactor;
        CharacterController bossCharacterController = boss.AddComponent<CharacterController>();
        bossCharacterController.height = bossHeight;
        bossCharacterController.radius = 0.5f * bossScaleFactor;
        bossCharacterController.center = new Vector3(0f, bossHeight / 2f, 0f);
        bossCharacterController.slopeLimit = 45f;
        bossCharacterController.stepOffset = 0.3f * bossScaleFactor;
        bossCharacterController.skinWidth = 0.08f;
        
        // Boss in center, facing +Z (toward party)
        boss.transform.localPosition = Vector3.zero;
        boss.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Face -Z (toward party)
        
        boss.AddComponent<HealthSystem>();
        boss.AddComponent<BossController>();
        boss.AddComponent<BossWallSystem>();
        // Only add LIDARSystem if it doesn't already exist
        if (boss.GetComponent<LIDARSystem>() == null)
        {
            boss.AddComponent<LIDARSystem>();
        }
        boss.AddComponent<BossAttackSystem>();
        
        // Add ML-Agent component
        BossAgent bossAgent = boss.AddComponent<BossAgent>();
        
        // Configure BehaviorParameters for Boss (5 discrete action branches)
        BehaviorParameters bossBehavior = boss.GetComponent<BehaviorParameters>();
        if (bossBehavior == null)
        {
            bossBehavior = boss.AddComponent<BehaviorParameters>();
        }
        bossBehavior.BehaviorName = "BossAgent";
        bossBehavior.TeamId = 1;
        // Action space: 5 discrete branches (movement: 3, rotation: 3, attack: 2, wall pickup: 2, wall place: 2)
        bossBehavior.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 3, 3, 2, 2, 2 });
        // Observation space: 164 observations (will be set automatically, but we can set a default)
        bossBehavior.BrainParameters.VectorObservationSize = 164;
        
        // Add Decision Requester (20 actions/second = 0.05s decision interval)
        DecisionRequester decisionRequester = boss.AddComponent<DecisionRequester>();
        decisionRequester.DecisionPeriod = 1; // Decision every frame (can be adjusted)
        decisionRequester.TakeActionsBetweenDecisions = true;
        
        GameObject bossCapsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossCapsuleVisual.name = "Capsule Visual";
        bossCapsuleVisual.transform.parent = boss.transform;
        bossCapsuleVisual.transform.localPosition = new Vector3(0f, bossHeight / 2f, 0f);
        bossCapsuleVisual.transform.localRotation = Quaternion.identity;
        bossCapsuleVisual.transform.localScale = Vector3.one * bossScaleFactor;
        Object.DestroyImmediate(bossCapsuleVisual.GetComponent<Collider>());
        
        Material bossMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bossMaterial.color = Color.red;
        bossCapsuleVisual.GetComponent<Renderer>().material = bossMaterial;
        
        GameObject bossHealthBarObj = new GameObject("Health Bar");
        bossHealthBarObj.transform.parent = boss.transform;
        bossHealthBarObj.transform.localPosition = Vector3.zero;
        HealthBar bossHealthBar = bossHealthBarObj.AddComponent<HealthBar>();
        bossHealthBar.SetOffset(2.5f);
        
        Undo.RegisterCreatedObjectUndo(boss, "Create Boss for ML-Agents");
        
        return boss;
    }
    
    static GameObject[] CreatePartyMembersForMLAgents(GameObject bossFightParent)
    {
        GameObject[] partyMembers = new GameObject[4];
        
        // Party formation: 2x2 grid, closer to boss (Z position not exceeding -9)
        // All facing -Z (toward boss)
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-3f, 0f, -7f),    // Front left (from party perspective)
            new Vector3(3f, 0f, -7f),     // Front right
            new Vector3(-1.5f, 0f, -8.5f), // Back left
            new Vector3(1.5f, 0f, -8.5f), // Back right
        };
        
        for (int i = 0; i < 4; i++)
        {
            GameObject partyMember = new GameObject($"Party Member {i + 1}");
            partyMember.transform.parent = bossFightParent.transform;
            
            float scaleFactor = 1f / 1.5f;
            float scaledHeight = 2f * scaleFactor;
            CharacterController characterController = partyMember.AddComponent<CharacterController>();
            characterController.height = scaledHeight;
            characterController.radius = 0.5f * scaleFactor;
            characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
            characterController.slopeLimit = 45f;
            characterController.stepOffset = 0.3f * scaleFactor;
            characterController.skinWidth = 0.08f;
            
            partyMember.transform.localPosition = positions[i];
            partyMember.transform.localRotation = Quaternion.identity; // Face +Z (toward boss)
            
            partyMember.AddComponent<HealthSystem>();
            partyMember.AddComponent<PlayerController>();
            PlayerClassSystem classSystem = partyMember.AddComponent<PlayerClassSystem>();
            
            // Party members start with no class (None) - they must choose their class during gameplay
            // Explicitly set to None using reflection to ensure it's not set to a default value
            var field = typeof(PlayerClassSystem).GetField("playerClass", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(classSystem, PlayerClass.None);
            }
            
            // Use EditorApplication.delayCall to ensure renderer is ready
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (classSystem != null)
                {
                    classSystem.UpdatePlayerColor();
                }
            };
            
            partyMember.AddComponent<LIDARSystem>();
            partyMember.AddComponent<PlayerAttackSystem>();
            
            // Add ML-Agent component
            PartyMemberAgent partyAgent = partyMember.AddComponent<PartyMemberAgent>();
            
            // Configure BehaviorParameters for Party Member (6 discrete action branches)
            BehaviorParameters partyBehavior = partyMember.GetComponent<BehaviorParameters>();
            if (partyBehavior == null)
            {
                partyBehavior = partyMember.AddComponent<BehaviorParameters>();
            }
            partyBehavior.BehaviorName = "PartyMemberAgent";
            partyBehavior.TeamId = 0;
            // Action space: 6 discrete branches (movement: 3, rotation: 3, attack: 2, heal: 2, threat boost: 2, class: 4)
            partyBehavior.BrainParameters.ActionSpec = new ActionSpec(0, new[] { 3, 3, 2, 2, 2, 4 });
            // Observation space: 164 observations
            partyBehavior.BrainParameters.VectorObservationSize = 164;
            
            // Add Decision Requester
            DecisionRequester decisionRequester = partyMember.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 1;
            decisionRequester.TakeActionsBetweenDecisions = true;
            
            GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleVisual.name = "Capsule Visual";
            capsuleVisual.transform.parent = partyMember.transform;
            capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f);
            capsuleVisual.transform.localRotation = Quaternion.identity;
            capsuleVisual.transform.localScale = Vector3.one * scaleFactor;
            Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
            
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        // Color will be set by PlayerClassSystem based on class
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Default, will be overridden
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
            
            GameObject healthBarObj = new GameObject("Health Bar");
            healthBarObj.transform.parent = partyMember.transform;
            healthBarObj.transform.localPosition = Vector3.zero;
            healthBarObj.AddComponent<HealthBar>();
            
            Undo.RegisterCreatedObjectUndo(partyMember, $"Create Party Member {i + 1} for ML-Agents");
            
            partyMembers[i] = partyMember;
        }
        
        return partyMembers;
    }
    
    static GameObject[] CreateStrategicWallsForMLAgents(GameObject bossFightParent)
    {
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.parent = bossFightParent.transform;
        wallsParent.transform.localPosition = Vector3.zero;
        
        float wallLength = 2f;
        float wallHeight = 1.5f;
        float wallThickness = 0.2f;
        
        // 3 walls positioned around boss (scaled for larger arena)
        Vector3[] wallPositions = new Vector3[]
        {
            new Vector3(-6f, wallHeight / 2f, 0f),  // Left side
            new Vector3(6f, wallHeight / 2f, 0f),   // Right side
            new Vector3(0f, wallHeight / 2f, 3f),  // Behind boss (from boss perspective, +Z is behind when facing party)
        };
        
        GameObject[] walls = new GameObject[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall {i + 1}";
            wall.transform.parent = wallsParent.transform;
            wall.transform.localPosition = wallPositions[i];
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);
            
            Material wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMaterial.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            wallMaterial.SetFloat("_Metallic", 0.1f);
            wallMaterial.SetFloat("_Smoothness", 0.2f);
            wall.GetComponent<Renderer>().material = wallMaterial;
            
            BoxCollider existingCollider = wall.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                existingCollider.size = new Vector3(1f, 1f, 1f);
                existingCollider.center = Vector3.zero;
            }
            wall.AddComponent<Wall>();
            
            Undo.RegisterCreatedObjectUndo(wall, $"Create Wall {i + 1}");
            
            walls[i] = wall;
        }
        
        return walls;
    }
    
    static void SetupEpisodeManager(GameObject bossFightParent, GameObject boss, GameObject[] partyMembers, GameObject[] walls)
    {
        GameObject managerObj = new GameObject("Episode Manager");
        managerObj.transform.parent = bossFightParent.transform;
        
        EpisodeManager episodeManager = managerObj.AddComponent<EpisodeManager>();
        
        // Set spawn positions
        Vector3 bossSpawnPos = Vector3.zero;
        Vector3[] partySpawnPositions = new Vector3[4];
        Vector3[] wallSpawnPositions = new Vector3[3];
        
        for (int i = 0; i < partyMembers.Length; i++)
        {
            if (partyMembers[i] != null)
            {
                partySpawnPositions[i] = partyMembers[i].transform.localPosition;
            }
        }
        
        for (int i = 0; i < walls.Length; i++)
        {
            if (walls[i] != null)
            {
                wallSpawnPositions[i] = walls[i].transform.localPosition;
            }
        }
        
        episodeManager.SetSpawnPositions(bossSpawnPos, partySpawnPositions, wallSpawnPositions);
        
        // Debug: Verify positions are set
        Debug.Log($"Boss spawn position: {bossSpawnPos}");
        for (int i = 0; i < partySpawnPositions.Length; i++)
        {
            Debug.Log($"Party member {i} spawn position: {partySpawnPositions[i]}");
        }
        
        Undo.RegisterCreatedObjectUndo(managerObj, "Create Episode Manager");
    }
    
    static void SetupAcademy()
    {
        // Academy is a singleton that initializes automatically when accessed
        // Just access it to ensure it's initialized
        Academy academy = Academy.Instance;
        if (academy != null)
        {
            Debug.Log("Academy initialized successfully");
        }
    }
    
    static void SetupThreatSystem()
    {
        // ThreatSystem is a singleton, will create itself if needed
        ThreatSystem threatSystem = ThreatSystem.Instance;
        if (threatSystem == null)
        {
            GameObject threatSystemObj = new GameObject("Threat System");
            threatSystemObj.AddComponent<ThreatSystem>();
        }
    }
    
    static void SetupEpisodeRecorder(GameObject bossFightParent)
    {
        GameObject recorderObj = new GameObject("Episode Recorder");
        recorderObj.transform.parent = bossFightParent.transform;
        recorderObj.AddComponent<EpisodeRecorder>();
        
        Undo.RegisterCreatedObjectUndo(recorderObj, "Create Episode Recorder");
    }
    
    static void SetupManualControlManager(GameObject bossFightParent)
    {
        GameObject controlManagerObj = new GameObject("Manual Control Manager");
        controlManagerObj.transform.parent = bossFightParent.transform;
        controlManagerObj.AddComponent<ManualControlManager>();
        
        Undo.RegisterCreatedObjectUndo(controlManagerObj, "Create Manual Control Manager");
    }
    
    static void EnsureTagExists(string tag)
    {
        string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
        bool tagExists = false;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tag)
            {
                tagExists = true;
                break;
            }
        }
        
        if (!tagExists)
        {
            UnityEditorInternal.InternalEditorUtility.AddTag(tag);
        }
    }
}

