/*
using UnityEngine;
using UnityEditor;

public class BossFightSetup
{
    [MenuItem("Boss Fight/Setup Arena (Player & Boss)")]
    static void SetupArena()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreatePlayer(bossFightParent);
        CreateBoss(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("Boss Fight Arena, Player, and Boss created successfully!");
    }
    
    [MenuItem("Boss Fight/Setup Arena (Player Only)")]
    static void SetupArenaPlayerOnly()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreatePlayer(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("Boss Fight Arena and Player created successfully!");
    }
    
    [MenuItem("Boss Fight/Setup Arena (Boss Only)")]
    static void SetupArenaBossOnly()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreateBoss(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("Boss Fight Arena and Boss created successfully!");
    }
    
    [MenuItem("Boss Fight/Setup Attack Test Scene")]
    static void SetupAttackTestScene()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreateStationaryPlayerAsTank(bossFightParent); // Player is Tank to test damage reduction
        CreateBossWithAttack(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("Attack Test Scene created! Boss can move and attack Tank player. Tank takes 40% less damage.");
    }
    
    [MenuItem("Boss Fight/Setup Player vs Boss Scene (MeleeDPS)")]
    static void SetupPlayerVsBossSceneMeleeDPS()
    {
        SetupPlayerVsBossSceneWithClass(PlayerClass.MeleeDPS);
    }
    
    [MenuItem("Boss Fight/Setup Player vs Boss Scene (Tank)")]
    static void SetupPlayerVsBossSceneTank()
    {
        SetupPlayerVsBossSceneWithClass(PlayerClass.Tank);
    }
    
    [MenuItem("Boss Fight/Setup Player vs Boss Scene (Healer)")]
    static void SetupPlayerVsBossSceneHealer()
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreatePlayerWithAttackAndClass(bossFightParent, PlayerClass.Healer);
        CreateStationaryBoss(bossFightParent);
        CreatePlayerDummyAtHalfHP(bossFightParent); // Add dummy player for healing test
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        Debug.Log("Player vs Boss Scene (Healer) created! Player (Healer) can move and heal. Dummy player at half HP for testing.");
    }
    
    [MenuItem("Boss Fight/Setup Player vs Boss Scene (RangedDPS)")]
    static void SetupPlayerVsBossSceneRangedDPS()
    {
        SetupPlayerVsBossSceneWithClass(PlayerClass.RangedDPS);
    }
    
    static void SetupPlayerVsBossSceneWithClass(PlayerClass playerClass)
    {
        // Remove any existing Boss Fight Arena to prevent duplicates
        GameObject existingArena = GameObject.Find("Boss Fight Arena");
        if (existingArena != null)
        {
            Object.DestroyImmediate(existingArena);
        }
        
        GameObject bossFightParent = CreateArenaAndLava();
        CreatePlayerWithAttackAndClass(bossFightParent, playerClass);
        CreateStationaryBoss(bossFightParent);
        
        // Select the parent object in the hierarchy
        Selection.activeGameObject = bossFightParent;
        
        string className = playerClass.ToString();
        Debug.Log($"Player vs Boss Scene created! Player ({className}) can move and attack boss. Boss is stationary.");
    }
    
    static GameObject CreateArenaAndLava()
    {
        // Ensure tags exist
        EnsureTagExists("Lava");
        EnsureTagExists("Void");
        
        // Create parent GameObject
        GameObject bossFightParent = new GameObject("Boss Fight Arena");
        
        // Arena dimensions
        float arenaSize = 10f; // Size of the green arena square
        float lavaSize = 14f; // Size of the red lava (bigger than arena to create moat effect)
        float arenaY = 0f; // Y position of the green arena
        float lavaY = -0.1f; // Y position of the red lava (slightly underneath)
        
        // Create red lava (bigger, underneath)
        GameObject redLava = GameObject.CreatePrimitive(PrimitiveType.Plane);
        redLava.name = "Red Lava";
        redLava.transform.parent = bossFightParent.transform;
        redLava.transform.localPosition = new Vector3(0f, lavaY, 0f);
        redLava.transform.localScale = new Vector3(lavaSize / 10f, 1f, lavaSize / 10f);
        redLava.transform.localRotation = Quaternion.identity;
        redLava.tag = "Lava"; // Set tag for lava detection
        
        // Remove MeshCollider and add BoxCollider as solid collider (so player can stand on it)
        MeshCollider meshCollider = redLava.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            Object.DestroyImmediate(meshCollider);
        }
        
        // Add BoxCollider as solid collider so player can stand on lava
        BoxCollider boxCollider = redLava.AddComponent<BoxCollider>();
        boxCollider.isTrigger = false; // Solid collider so CharacterController can collide with it
        boxCollider.size = new Vector3(10f, 0.1f, 10f); // 10x10 box to match visual lava area
        boxCollider.center = new Vector3(0f, 0f, 0f);
        
        Material redLavaMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        redLavaMaterial.color = Color.red;
        redLavaMaterial.SetFloat("_Metallic", 0.3f);
        redLavaMaterial.SetFloat("_Smoothness", 0.2f);
        redLava.GetComponent<Renderer>().material = redLavaMaterial;
        
        // Create green arena square (on top of the lava)
        GameObject greenArena = GameObject.CreatePrimitive(PrimitiveType.Plane);
        greenArena.name = "Green Arena";
        greenArena.transform.parent = bossFightParent.transform;
        greenArena.transform.localPosition = new Vector3(0f, arenaY, 0f);
        greenArena.transform.localScale = new Vector3(arenaSize / 10f, 1f, arenaSize / 10f);
        greenArena.transform.localRotation = Quaternion.identity;
        
        // Replace MeshCollider with BoxCollider for better collision (MeshColliders can be problematic)
        MeshCollider arenaMeshCollider = greenArena.GetComponent<MeshCollider>();
        if (arenaMeshCollider != null)
        {
            Object.DestroyImmediate(arenaMeshCollider);
        }
        
        // Add BoxCollider for solid collision
        BoxCollider arenaBoxCollider = greenArena.AddComponent<BoxCollider>();
        arenaBoxCollider.size = new Vector3(arenaSize, 0.1f, arenaSize); // Thin box for the floor
        arenaBoxCollider.center = new Vector3(0f, 0f, 0f);
        arenaBoxCollider.isTrigger = false;
        
        Material greenArenaMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        greenArenaMaterial.color = new Color(0.1f, 0.4f, 0.15f, 1f); // Dark forest green
        greenArenaMaterial.SetFloat("_Metallic", 0.2f);
        greenArenaMaterial.SetFloat("_Smoothness", 0.3f);
        greenArena.GetComponent<Renderer>().material = greenArenaMaterial;
        
        // Create void area below the lava (instant death zone)
        // Void is a flat plane, slightly bigger than lava, positioned just below it
        float voidSize = 16f; // A bit bigger than lava (14f)
        float voidY = -0.2f; // Just below the lava (lava is at -0.1f)
        
        GameObject voidArea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        voidArea.name = "Void";
        voidArea.transform.parent = bossFightParent.transform;
        voidArea.transform.localPosition = new Vector3(0f, voidY, 0f);
        voidArea.transform.localScale = new Vector3(voidSize / 10f, 1f, voidSize / 10f);
        voidArea.transform.localRotation = Quaternion.identity;
        voidArea.tag = "Void";
        
        // Remove MeshCollider and add BoxCollider as trigger
        MeshCollider voidMeshCollider = voidArea.GetComponent<MeshCollider>();
        if (voidMeshCollider != null)
        {
            Object.DestroyImmediate(voidMeshCollider);
        }
        
        // Add BoxCollider as trigger for void detection (thin and flat)
        BoxCollider voidCollider = voidArea.AddComponent<BoxCollider>();
        voidCollider.isTrigger = true;
        voidCollider.size = new Vector3(10f, 0.1f, 10f); // 10x10 thin flat box
        voidCollider.center = new Vector3(0f, 0f, 0f);
        
        // Add VoidKiller component to handle instant death
        voidArea.AddComponent<VoidKiller>();
        
        // Make void black/dark
        Material voidMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        voidMaterial.color = new Color(0f, 0f, 0f, 0.8f); // Black with some transparency
        voidArea.GetComponent<Renderer>().material = voidMaterial;
        
        // Create strategic walls around the arena
        CreateStrategicWalls(bossFightParent);
        
        // Setup camera
        SetupCamera();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(bossFightParent, "Create Boss Fight Arena");
        
        return bossFightParent;
    }
    
    static void SetupCamera()
    {
        // Find or create main camera
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
        
        // Set camera position and rotation
        mainCamera.transform.position = new Vector3(0f, 7f, -10f);
        mainCamera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
        
        Debug.Log("Camera positioned at y=7, z=-10 with rotation x=45");
    }
    
    static void CreateStrategicWalls(GameObject bossFightParent)
    {
        // Create a parent object for all walls
        GameObject wallsParent = new GameObject("Walls");
        wallsParent.transform.parent = bossFightParent.transform;
        wallsParent.transform.localPosition = Vector3.zero;
        
        // Wall dimensions
        float wallLength = 2f;
        float wallHeight = 1.5f;
        float wallThickness = 0.2f;
        
        // Strategic positions around the arena (10x10 arena, so positions from -5 to 5)
        // Only 3 walls at strategic points
        // Y position is wallHeight/2f to place bottom of wall on ground (y=0)
        Vector3[] wallPositions = new Vector3[]
        {
            // Three strategic positions (world positions, not local)
            new Vector3(-3f, wallHeight / 2f, 0f),  // Left side
            new Vector3(3f, wallHeight / 2f, 0f),   // Right side
            new Vector3(0f, wallHeight / 2f, -3f), // Front side
        };
        
        // Create walls at each position
        for (int i = 0; i < wallPositions.Length; i++)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall {i + 1}";
            wall.transform.parent = wallsParent.transform;
            wall.transform.localPosition = wallPositions[i];
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = new Vector3(wallLength, wallHeight, wallThickness);
            
            // Create material for walls (gray/stone color)
            Material wallMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMaterial.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray
            wallMaterial.SetFloat("_Metallic", 0.1f);
            wallMaterial.SetFloat("_Smoothness", 0.2f);
            wall.GetComponent<Renderer>().material = wallMaterial;
            
            // Update the existing BoxCollider to size 1, 1, 1
            BoxCollider existingCollider = wall.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                existingCollider.size = new Vector3(1f, 1f, 1f);
                existingCollider.center = Vector3.zero;
            }
            
            // Add Wall component (it will set collider to 1, 1, 1)
            Wall wallComponent = wall.AddComponent<Wall>();
        }
        
        Undo.RegisterCreatedObjectUndo(wallsParent, "Create Strategic Walls");
    }
    
    static void CreatePlayer(GameObject bossFightParent)
    {
        // Create player character as child of boss fight arena
        GameObject player = new GameObject("Player");
        player.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor; // Scale radius too
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor; // Scale step offset
        characterController.skinWidth = 0.08f; // Prevent falling through
        
        // Position player so bottom of capsule is at y=0 (arena level)
        player.transform.localPosition = new Vector3(0f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem healthSystem = player.AddComponent<HealthSystem>();
        
        // Add PlayerController script
        player.AddComponent<PlayerController>();
        
        // Add visual capsule representation (scaled down)
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = player.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f); // Match CharacterController center
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor; // Scale down by 1.5x
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the player
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Blue color
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = player.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
    }
    
    static void CreatePlayerWithAttack(GameObject bossFightParent)
    {
        // Create player character as child of boss fight arena
        GameObject player = new GameObject("Player");
        player.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor; // Scale radius too
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor; // Scale step offset
        characterController.skinWidth = 0.08f; // Prevent falling through
        
        // Position player away from center so it doesn't overlap with boss
        player.transform.localPosition = new Vector3(-2f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem healthSystem = player.AddComponent<HealthSystem>();
        
        // Add PlayerController script
        player.AddComponent<PlayerController>();
        
        // Add PlayerClassSystem for class selection (defaults to MeleeDPS)
        PlayerClassSystem classSystem = player.AddComponent<PlayerClassSystem>();
        
        // Add LIDARSystem for attack detection
        player.AddComponent<LIDARSystem>();
        
        // Add PlayerAttackSystem script (player can attack boss)
        player.AddComponent<PlayerAttackSystem>();
        
        // Add visual capsule representation (scaled down)
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = player.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f); // Match CharacterController center
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor; // Scale down by 1.5x
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the player (color will be set by PlayerClassSystem based on class)
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Default blue, will be overridden by class
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = player.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(player, "Create Player with Attack");
    }
    
    static void CreatePlayerWithAttackAndClass(GameObject bossFightParent, PlayerClass playerClass)
    {
        // Create player character as child of boss fight arena
        GameObject player = new GameObject("Player");
        player.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor; // Scale radius too
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor; // Scale step offset
        characterController.skinWidth = 0.08f; // Prevent falling through
        
        // Position player away from center so it doesn't overlap with boss
        player.transform.localPosition = new Vector3(-2f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem healthSystem = player.AddComponent<HealthSystem>();
        
        // Add PlayerController script
        player.AddComponent<PlayerController>();
        
        // Add PlayerClassSystem for class selection and set the class
        PlayerClassSystem classSystem = player.AddComponent<PlayerClassSystem>();
        // Use SerializedObject to set the serialized field
        SerializedObject serializedClassSystem = new SerializedObject(classSystem);
        SerializedProperty classProperty = serializedClassSystem.FindProperty("playerClass");
        if (classProperty != null)
        {
            classProperty.enumValueIndex = (int)playerClass;
            serializedClassSystem.ApplyModifiedProperties();
        }
        
        // Add LIDARSystem for attack detection
        player.AddComponent<LIDARSystem>();
        
        // Add PlayerAttackSystem script (player can attack boss)
        player.AddComponent<PlayerAttackSystem>();
        
        // Add visual capsule representation (scaled down)
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = player.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f); // Match CharacterController center
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor; // Scale down by 1.5x
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the player (color will be set by PlayerClassSystem based on class)
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Default blue, will be overridden by class
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = player.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(player, $"Create Player with Attack ({playerClass})");
    }
    
    static void CreateBoss(GameObject bossFightParent)
    {
        // Create boss character as child of boss fight arena
        GameObject boss = new GameObject("Boss");
        boss.transform.parent = bossFightParent.transform;
        
        // Boss is 1.5x bigger than player
        // Player scale factor is 1/1.5 = 0.666...
        // Boss scale = player scale * 1.5 = 0.666... * 1.5 = 1.0 (normal size)
        float bossScaleFactor = 1.0f; // Normal size (1.5x the player's scaled size)
        
        // Add CharacterController (uses capsule collider internally)
        float bossHeight = 2f * bossScaleFactor; // 2.0 (normal size)
        CharacterController bossCharacterController = boss.AddComponent<CharacterController>();
        bossCharacterController.height = bossHeight;
        bossCharacterController.radius = 0.5f * bossScaleFactor; // Normal radius
        bossCharacterController.center = new Vector3(0f, bossHeight / 2f, 0f);
        bossCharacterController.slopeLimit = 45f;
        bossCharacterController.stepOffset = 0.3f * bossScaleFactor;
        bossCharacterController.skinWidth = 0.08f;
        
        // Position boss so bottom of capsule is at y=0 (arena level)
        boss.transform.localPosition = new Vector3(0f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem bossHealthSystem = boss.AddComponent<HealthSystem>();
        
        // Add BossController script
        boss.AddComponent<BossController>();
        
        // Add BossWallSystem for wall manipulation
        boss.AddComponent<BossWallSystem>();
        
        // Add visual capsule representation
        GameObject bossCapsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossCapsuleVisual.name = "Capsule Visual";
        bossCapsuleVisual.transform.parent = boss.transform;
        bossCapsuleVisual.transform.localPosition = new Vector3(0f, bossHeight / 2f, 0f); // Match CharacterController center
        bossCapsuleVisual.transform.localRotation = Quaternion.identity;
        bossCapsuleVisual.transform.localScale = Vector3.one * bossScaleFactor; // Normal size
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(bossCapsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the boss (red color)
        Material bossMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bossMaterial.color = Color.red;
        bossCapsuleVisual.GetComponent<Renderer>().material = bossMaterial;
        
        // Create health bar for boss (with higher offset since boss is 1.5x bigger)
        GameObject bossHealthBarObj = new GameObject("Health Bar");
        bossHealthBarObj.transform.parent = boss.transform;
        bossHealthBarObj.transform.localPosition = Vector3.zero;
        HealthBar bossHealthBar = bossHealthBarObj.AddComponent<HealthBar>();
        bossHealthBar.SetOffset(2.5f);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(boss, "Create Boss");
    }
    
    static void CreateStationaryBoss(GameObject bossFightParent)
    {
        // Create boss character as child of boss fight arena
        GameObject boss = new GameObject("Boss");
        boss.transform.parent = bossFightParent.transform;
        
        // Boss is 1.5x bigger than player
        // Player scale factor is 1/1.5 = 0.666...
        // Boss scale = player scale * 1.5 = 0.666... * 1.5 = 1.0 (normal size)
        float bossScaleFactor = 1.0f; // Normal size (1.5x the player's scaled size)
        
        // Add CharacterController (uses capsule collider internally)
        float bossHeight = 2f * bossScaleFactor; // 2.0 (normal size)
        CharacterController bossCharacterController = boss.AddComponent<CharacterController>();
        bossCharacterController.height = bossHeight;
        bossCharacterController.radius = 0.5f * bossScaleFactor; // Normal radius
        bossCharacterController.center = new Vector3(0f, bossHeight / 2f, 0f);
        bossCharacterController.slopeLimit = 45f;
        bossCharacterController.stepOffset = 0.3f * bossScaleFactor;
        bossCharacterController.skinWidth = 0.08f;
        
        // Position boss at center (away from player at -2,0,0)
        boss.transform.localPosition = new Vector3(2f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem bossHealthSystem = boss.AddComponent<HealthSystem>();
        
        // Add StationaryBoss script (no movement, no input)
        boss.AddComponent<StationaryBoss>();
        
        // Add visual capsule representation
        GameObject bossCapsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossCapsuleVisual.name = "Capsule Visual";
        bossCapsuleVisual.transform.parent = boss.transform;
        bossCapsuleVisual.transform.localPosition = new Vector3(0f, bossHeight / 2f, 0f); // Match CharacterController center
        bossCapsuleVisual.transform.localRotation = Quaternion.identity;
        bossCapsuleVisual.transform.localScale = Vector3.one * bossScaleFactor; // Normal size
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(bossCapsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the boss (red color)
        Material bossMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bossMaterial.color = Color.red;
        bossCapsuleVisual.GetComponent<Renderer>().material = bossMaterial;
        
        // Create health bar for boss (with higher offset since boss is 1.5x bigger)
        GameObject bossHealthBarObj = new GameObject("Health Bar");
        bossHealthBarObj.transform.parent = boss.transform;
        bossHealthBarObj.transform.localPosition = Vector3.zero;
        HealthBar bossHealthBar = bossHealthBarObj.AddComponent<HealthBar>();
        bossHealthBar.SetOffset(2.5f);
        
        // No facing direction indicator for stationary boss (not needed)
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(boss, "Create Stationary Boss");
    }
    
    static void CreateStationaryPlayer(GameObject bossFightParent)
    {
        // Create stationary player character as child of boss fight arena
        GameObject player = new GameObject("Player");
        player.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor; // Scale radius too
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor; // Scale step offset
        characterController.skinWidth = 0.08f; // Prevent falling through
        
        // Position player so bottom of capsule is at y=0 (arena level)
        player.transform.localPosition = new Vector3(0f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem healthSystem = player.AddComponent<HealthSystem>();
        
        // Add StationaryPlayer script (no movement, just takes damage)
        player.AddComponent<StationaryPlayer>();
        
        // Add visual capsule representation (scaled down)
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = player.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f); // Match CharacterController center
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor; // Scale down by 1.5x
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the player
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Blue color
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = player.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(player, "Create Stationary Player");
    }
    
    static void CreateStationaryPlayerAsTank(GameObject bossFightParent)
    {
        // Create stationary player character as Tank (for damage reduction testing)
        GameObject player = new GameObject("Player");
        player.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor; // Scale radius too
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor; // Scale step offset
        characterController.skinWidth = 0.08f; // Prevent falling through
        
        // Position player so bottom of capsule is at y=0 (arena level)
        player.transform.localPosition = new Vector3(0f, 0f, 0f);
        
        // Add HealthSystem
        HealthSystem healthSystem = player.AddComponent<HealthSystem>();
        
        // Add PlayerClassSystem and set to Tank
        PlayerClassSystem classSystem = player.AddComponent<PlayerClassSystem>();
        SerializedObject serializedClassSystem = new SerializedObject(classSystem);
        SerializedProperty classProperty = serializedClassSystem.FindProperty("playerClass");
        if (classProperty != null)
        {
            classProperty.enumValueIndex = (int)PlayerClass.Tank;
            serializedClassSystem.ApplyModifiedProperties();
        }
        
        // Add StationaryPlayer script (no movement, just takes damage)
        player.AddComponent<StationaryPlayer>();
        
        // Add visual capsule representation (scaled down)
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = player.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f); // Match CharacterController center
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor; // Scale down by 1.5x
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the player (color will be set by PlayerClassSystem to blue for Tank)
        Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMaterial.color = new Color(0.2f, 0.4f, 0.8f); // Default blue, will be set by PlayerClassSystem
        capsuleVisual.GetComponent<Renderer>().material = playerMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = player.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(player, "Create Stationary Player as Tank");
    }
    
    static void CreatePlayerDummyAtHalfHP(GameObject bossFightParent)
    {
        // Create a dummy player for healing tests
        GameObject dummyPlayer = new GameObject("Dummy Player");
        dummyPlayer.transform.parent = bossFightParent.transform;
        
        // Scale factor: 1.5x smaller = 1/1.5 = 2/3 of original size
        float scaleFactor = 1f / 1.5f; // 0.666...
        
        // Add CharacterController (uses capsule collider internally)
        float scaledHeight = 2f * scaleFactor; // 1.333...
        CharacterController characterController = dummyPlayer.AddComponent<CharacterController>();
        characterController.height = scaledHeight;
        characterController.radius = 0.5f * scaleFactor;
        characterController.center = new Vector3(0f, scaledHeight / 2f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f * scaleFactor;
        characterController.skinWidth = 0.08f;
        
        // Position dummy player away from healer
        dummyPlayer.transform.localPosition = new Vector3(0f, 0f, -3f);
        
        // Add HealthSystem and set to half health
        HealthSystem healthSystem = dummyPlayer.AddComponent<HealthSystem>();
        // Use reflection to set currentHealth to half of maxHealth after Start runs
        // We'll do this via a coroutine or by directly setting it after a frame
        
        // Add StationaryPlayer script (no movement)
        dummyPlayer.AddComponent<StationaryPlayer>();
        
        // Add visual capsule representation
        GameObject capsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsuleVisual.name = "Capsule Visual";
        capsuleVisual.transform.parent = dummyPlayer.transform;
        capsuleVisual.transform.localPosition = new Vector3(0f, scaledHeight / 2f, 0f);
        capsuleVisual.transform.localRotation = Quaternion.identity;
        capsuleVisual.transform.localScale = Vector3.one * scaleFactor;
        
        // Remove the collider from the visual
        Object.DestroyImmediate(capsuleVisual.GetComponent<Collider>());
        
        // Create material for dummy (gray color to distinguish)
        Material dummyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dummyMaterial.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray color
        capsuleVisual.GetComponent<Renderer>().material = dummyMaterial;
        
        // Create health bar
        GameObject healthBarObj = new GameObject("Health Bar");
        healthBarObj.transform.parent = dummyPlayer.transform;
        healthBarObj.transform.localPosition = Vector3.zero;
        healthBarObj.AddComponent<HealthBar>();
        
        // Set health to half after component is initialized
        // Use a helper component to set health after Start
        HalfHealthDummy halfHealthComponent = dummyPlayer.AddComponent<HalfHealthDummy>();
        
        Undo.RegisterCreatedObjectUndo(dummyPlayer, "Create Dummy Player at Half HP");
    }
    
    static void CreateBossWithAttack(GameObject bossFightParent)
    {
        // Create boss character as child of boss fight arena
        GameObject boss = new GameObject("Boss");
        boss.transform.parent = bossFightParent.transform;
        
        // Boss is 1.5x bigger than player
        float bossScaleFactor = 1.0f; // Normal size (1.5x the player's scaled size)
        
        // Add CharacterController (uses capsule collider internally)
        float bossHeight = 2f * bossScaleFactor; // 2.0 (normal size)
        CharacterController bossCharacterController = boss.AddComponent<CharacterController>();
        bossCharacterController.height = bossHeight;
        bossCharacterController.radius = 0.5f * bossScaleFactor; // Normal radius
        bossCharacterController.center = new Vector3(0f, bossHeight / 2f, 0f);
        bossCharacterController.slopeLimit = 45f;
        bossCharacterController.stepOffset = 0.3f * bossScaleFactor;
        bossCharacterController.skinWidth = 0.08f;
        
        // Position boss so bottom of capsule is at y=0 (arena level)
        // Position boss away from player (at a different location)
        boss.transform.localPosition = new Vector3(3f, 0f, 0f); // 3 units away from center
        
        // Add HealthSystem
        HealthSystem bossHealthSystem = boss.AddComponent<HealthSystem>();
        
        // Add BossController script (for movement)
        boss.AddComponent<BossController>();
        
        // Add BossWallSystem for wall manipulation
        boss.AddComponent<BossWallSystem>();
        
        // Add LIDARSystem for attack detection
        boss.AddComponent<LIDARSystem>();
        
        // Add BossAttackSystem script (for attacking players)
        boss.AddComponent<BossAttackSystem>();
        
        // Add visual capsule representation
        GameObject bossCapsuleVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bossCapsuleVisual.name = "Capsule Visual";
        bossCapsuleVisual.transform.parent = boss.transform;
        bossCapsuleVisual.transform.localPosition = new Vector3(0f, bossHeight / 2f, 0f); // Match CharacterController center
        bossCapsuleVisual.transform.localRotation = Quaternion.identity;
        bossCapsuleVisual.transform.localScale = Vector3.one * bossScaleFactor; // Normal size
        
        // Remove the collider from the visual since CharacterController handles collision
        Object.DestroyImmediate(bossCapsuleVisual.GetComponent<Collider>());
        
        // Create a simple material for the boss (red color)
        Material bossMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        bossMaterial.color = Color.red;
        bossCapsuleVisual.GetComponent<Renderer>().material = bossMaterial;
        
        // Create health bar for boss (with higher offset since boss is 1.5x bigger)
        GameObject bossHealthBarObj = new GameObject("Health Bar");
        bossHealthBarObj.transform.parent = boss.transform;
        bossHealthBarObj.transform.localPosition = Vector3.zero;
        HealthBar bossHealthBar = bossHealthBarObj.AddComponent<HealthBar>();
        bossHealthBar.SetOffset(2.5f);
        
        // Register undo
        Undo.RegisterCreatedObjectUndo(boss, "Create Boss with Attack");
    }
    
    static void EnsureTagExists(string tag)
    {
        // Open tag manager
        UnityEngine.Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
        {
            Debug.LogWarning("Could not load TagManager.asset. Please create the 'Lava' tag manually in Edit > Project Settings > Tags and Layers.");
            return;
        }
        
        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        if (tagsProp == null)
        {
            Debug.LogWarning("Could not find tags property. Please create the 'Lava' tag manually in Edit > Project Settings > Tags and Layers.");
            return;
        }
        
        // Check if tag already exists
        bool exists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag))
            {
                exists = true;
                break;
            }
        }
        
        // Add tag if it doesn't exist
        if (!exists)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"Created tag: {tag}");
        }
    }
}


*/