using UnityEngine;

public class PlayerClassSystem : MonoBehaviour
{
    [Header("Class Settings")]
    [SerializeField] private PlayerClass playerClass = PlayerClass.None; // Start with no class
    private bool classLocked = false; // Once class is chosen, lock it for the episode
    
    [Header("Class Stats")]
    [SerializeField] private float tankDamage = 2f;
    [SerializeField] private float healerDamage = 2f;
    [SerializeField] private float rangedDPSDamage = 5f;
    [SerializeField] private float meleeDPSDamage = 10f;
    
    [Header("Tank Abilities")]
    [SerializeField] private float damageReduction = 0.4f; // 40% damage reduction
    [SerializeField] private KeyCode threatBoostKey = KeyCode.T;
    [SerializeField] private float threatBoostCooldown = 5f;
    
    [Header("Healer Abilities")]
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private float healCooldown = 3f;
    [SerializeField] private KeyCode healKey = KeyCode.H;
    
    [Header("Ranged DPS")]
    [SerializeField] private float rangedMultiplier = 3f; // Triple the normal range
    
    private float lastHealTime = 0f;
    private float lastThreatBoostTime = 0f;
    private HealthSystem healthSystem;
    
    public PlayerClass CurrentClass => playerClass;
    public float DamageReduction => (playerClass == PlayerClass.Tank) ? damageReduction : 0f;
    public float AttackRangeMultiplier => (playerClass == PlayerClass.RangedDPS) ? rangedMultiplier : 1f;
    
    void Awake()
    {
        // Force class to None on awake if not already locked (before any other initialization)
        // This ensures party members always start as None, even if serialized data says otherwise
        var field = typeof(PlayerClassSystem).GetField("playerClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            // Always reset to None on awake, regardless of serialized value
            field.SetValue(this, PlayerClass.None);
            playerClass = PlayerClass.None; // Also update the property directly
        }
        classLocked = false; // Always unlock on awake
    }
    
    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        
        // Triple-check class is None on start (in case something set it between Awake and Start)
        var field = typeof(PlayerClassSystem).GetField("playerClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && !classLocked)
        {
            field.SetValue(this, PlayerClass.None);
            playerClass = PlayerClass.None; // Also update the property directly
        }
        
        // Update color immediately and with a delay to ensure it persists
        UpdatePlayerColor();
        Invoke(nameof(UpdatePlayerColor), 0.1f);
        Invoke(nameof(UpdatePlayerColor), 0.5f); // Extra delay to catch any material resets
    }
    
    void LateUpdate()
    {
        // Continuously update color to ensure it persists (in case material gets reset)
        // Only do this occasionally to avoid performance issues
        if (Time.frameCount % 60 == 0) // Every 60 frames (~1 second at 60fps)
        {
            UpdatePlayerColor();
        }
    }
    
    public void UpdatePlayerColor()
    {
        // Find the capsule visual and update its color based on class
        Renderer capsuleRenderer = GetComponentInChildren<Renderer>();
        if (capsuleRenderer != null)
        {
            Color classColor = GetClassVisualColor();
            
            // In play mode, always use material (creates instance if needed)
            // In edit mode, use sharedMaterial to avoid leaks
            if (Application.isPlaying)
            {
                // Ensure we have a unique material instance
                Material playerMaterial = capsuleRenderer.material;
                
                // If the material is still the shared material, create an instance
                if (playerMaterial == capsuleRenderer.sharedMaterial)
                {
                    playerMaterial = new Material(capsuleRenderer.sharedMaterial);
                    capsuleRenderer.material = playerMaterial;
                }
                
                if (playerMaterial != null)
                {
                    playerMaterial.color = classColor;
                    
                    // Also update shader properties for URP
                    if (playerMaterial.HasProperty("_BaseColor"))
                    {
                        playerMaterial.SetColor("_BaseColor", classColor);
                    }
                    else if (playerMaterial.HasProperty("_Color"))
                    {
                        playerMaterial.SetColor("_Color", classColor);
                    }
                }
            }
            else
            {
                // Edit mode - use sharedMaterial
                Material playerMaterial = capsuleRenderer.sharedMaterial;
                if (playerMaterial != null)
                {
                    playerMaterial.color = classColor;
                    
                    // Also update shader properties for URP
                    if (playerMaterial.HasProperty("_BaseColor"))
                    {
                        playerMaterial.SetColor("_BaseColor", classColor);
                    }
                    else if (playerMaterial.HasProperty("_Color"))
                    {
                        playerMaterial.SetColor("_Color", classColor);
                    }
                }
            }
        }
        else
        {
            // If renderer not found, try again after a longer delay (only in play mode)
            if (Application.isPlaying)
            {
                Invoke(nameof(UpdatePlayerColor), 0.2f);
            }
        }
    }
    
    Color GetClassVisualColor()
    {
        switch (playerClass)
        {
            case PlayerClass.None:
                return Color.white; // White when no class selected
            case PlayerClass.Tank:
                return Color.blue;
            case PlayerClass.Healer:
                return Color.green;
            case PlayerClass.MeleeDPS:
                return Color.red;
            case PlayerClass.RangedDPS:
                return new Color(0.5f, 0f, 0.5f, 1f); // Purple
            default:
                return Color.white; // Default white
        }
    }
    
    public float GetAttackDamage()
    {
        // No damage if no class is selected
        if (playerClass == PlayerClass.None)
        {
            return 0f;
        }
        
        switch (playerClass)
        {
            case PlayerClass.Tank:
                return tankDamage;
            case PlayerClass.Healer:
                return healerDamage;
            case PlayerClass.RangedDPS:
                return rangedDPSDamage;
            case PlayerClass.MeleeDPS:
                return meleeDPSDamage;
            default:
                return 0f; // No damage by default
        }
    }
    
    public bool IsClassLocked => classLocked;
    
    public bool SetPlayerClass(PlayerClass newClass)
    {
        // Only allow class selection if not locked and current class is None
        if (classLocked)
        {
            return false; // Class already locked
        }
        
        if (playerClass != PlayerClass.None)
        {
            return false; // Class already chosen
        }
        
        if (newClass == PlayerClass.None)
        {
            return false; // Can't set to None
        }
        
        // Set the class and lock it
        var field = typeof(PlayerClassSystem).GetField("playerClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, newClass);
            classLocked = true;
            UpdatePlayerColor();
            Debug.Log($"{gameObject.name} selected class: {newClass}");

            // If we have a LIDARSystem and this is a ranged DPS, update attack range immediately
            LIDARSystem lidar = GetComponent<LIDARSystem>();
            if (lidar != null)
            {
                float baseRange = 2f;
                lidar.SetAttackRange(baseRange * AttackRangeMultiplier);
            }
            return true;
        }
        
        return false;
    }
    
    public void ResetClassForEpisode()
    {
        // Reset class and unlock for new episode
        var field = typeof(PlayerClassSystem).GetField("playerClass", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, PlayerClass.None);
            classLocked = false;
            UpdatePlayerColor();
        }
    }
    
    void Update()
    {
        // Class selection only works for the currently selected agent (via ManualControlManager)
        // Use U, I, O, P keys to avoid conflict with agent selection (0-4)
        bool isSelectedAgent = ManualControlManager.Instance != null && 
                              ManualControlManager.Instance.IsAgentSelected(gameObject);
        
        if (isSelectedAgent && !classLocked && playerClass == PlayerClass.None)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                SetPlayerClass(PlayerClass.Tank);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                SetPlayerClass(PlayerClass.Healer);
            }
            else if (Input.GetKeyDown(KeyCode.O))
            {
                SetPlayerClass(PlayerClass.RangedDPS);
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                SetPlayerClass(PlayerClass.MeleeDPS);
            }
        }
        
        // Skip other input processing if ML-Agent is controlling this
        if (GetComponent<Unity.MLAgents.Agent>() == null)
        {
            // Healer can heal nearby agents
            if (playerClass == PlayerClass.Healer && Input.GetKeyDown(healKey))
            {
                TryHeal();
            }
            
            // Tank can boost threat
            if (playerClass == PlayerClass.Tank && Input.GetKeyDown(threatBoostKey))
            {
                TryThreatBoost();
            }
        }
    }
    
    // Make TryHeal and TryThreatBoost public for ML-Agents
    public void TryHeal()
    {
        // Only healers can heal
        if (playerClass != PlayerClass.Healer)
        {
            return;
        }
        
        if (Time.time < lastHealTime + healCooldown)
        {
            return; // Still on cooldown
        }
        
        // Use LIDAR system to find targets within healing line of sight
        LIDARSystem lidarSystem = GetComponent<LIDARSystem>();
        if (lidarSystem == null)
        {
            Debug.LogWarning("Healer requires LIDARSystem for healing!");
            return;
        }
        
        // Get all targets within healing line of sight
        System.Collections.Generic.List<GameObject> targetsInRange = lidarSystem.GetTargetsInHealRange();
        
        if (targetsInRange.Count == 0)
        {
            Debug.Log("Healer: No targets found in healing range. Make sure you're facing the target and within 5 units.");
            return;
        }
        
        bool healedSomeone = false;
        foreach (GameObject target in targetsInRange)
        {
            HealthSystem targetHealth = target.GetComponent<HealthSystem>();
            if (targetHealth != null)
            {
                if (targetHealth.CurrentHealth < targetHealth.MaxHealth)
                {
                    targetHealth.Heal(healAmount);
                    healedSomeone = true;
                    Debug.Log($"Healer healed {target.name} for {healAmount} health! New health: {targetHealth.CurrentHealth}/{targetHealth.MaxHealth}");
                }
                else
                {
                    Debug.Log($"Healer: {target.name} is already at full health ({targetHealth.CurrentHealth}/{targetHealth.MaxHealth})");
                }
            }
            else
            {
                Debug.LogWarning($"Healer: Found target {target.name} but it has no HealthSystem component!");
            }
        }
        
        if (healedSomeone)
        {
            // Generate threat from healing (3x DPS attack threat)
            ThreatSystem threatSystem = ThreatSystem.Instance;
            if (threatSystem != null)
            {
                // Use average DPS damage (5 for RangedDPS) as base threat
                float baseDPSDamage = rangedDPSDamage; // Use RangedDPS damage as base
                threatSystem.AddThreatFromHeal(gameObject, baseDPSDamage);
            }
            
            lastHealTime = Time.time;
        }
    }
    
    public void TryThreatBoost()
    {
        // Only tanks can boost threat
        if (playerClass != PlayerClass.Tank)
        {
            return;
        }
        
        if (Time.time < lastThreatBoostTime + threatBoostCooldown)
        {
            return; // Still on cooldown
        }
        
        // Generate threat boost (5x DPS attack threat)
        ThreatSystem threatSystem = ThreatSystem.Instance;
        if (threatSystem != null)
        {
            // Use average DPS damage (5 for RangedDPS) as base threat
            float baseDPSDamage = rangedDPSDamage; // Use RangedDPS damage as base
            threatSystem.AddThreatBoost(gameObject, baseDPSDamage);
            Debug.Log($"Tank used threat boost! Generated {baseDPSDamage * 5f} threat.");
            lastThreatBoostTime = Time.time;
        }
    }
    
    
    void OnGUI()
    {
        if (playerClass == PlayerClass.Healer)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
            
            float yPos = Screen.height - 150f;
            float lineHeight = 20f;
            float xPos = Screen.width - 250f;
            
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(xPos, yPos, 240, 30), $"Class: {playerClass}", style);
            yPos += lineHeight;
            
            float cooldownRemaining = Mathf.Max(0f, (lastHealTime + healCooldown) - Time.time);
            if (cooldownRemaining > 0f)
            {
                style.normal.textColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                GUI.Label(new Rect(xPos, yPos, 240, 30), $"Heal Cooldown: {cooldownRemaining:F1}s", style);
            }
            else
            {
                style.normal.textColor = Color.cyan;
                GUI.Label(new Rect(xPos, yPos, 240, 30), $"Press {healKey} to Heal (10 HP)", style);
            }
        }
        else if (playerClass == PlayerClass.Tank)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
            
            float yPos = Screen.height - 150f;
            float lineHeight = 20f;
            float xPos = Screen.width - 250f;
            
            style.normal.textColor = new Color(0.8f, 0.2f, 0.2f, 1f);
            GUI.Label(new Rect(xPos, yPos, 240, 30), $"Class: {playerClass}", style);
            yPos += lineHeight;
            
            float cooldownRemaining = Mathf.Max(0f, (lastThreatBoostTime + threatBoostCooldown) - Time.time);
            if (cooldownRemaining > 0f)
            {
                style.normal.textColor = new Color(1f, 0.5f, 0f, 1f); // Orange
                GUI.Label(new Rect(xPos, yPos, 240, 30), $"Threat Boost Cooldown: {cooldownRemaining:F1}s", style);
            }
            else
            {
                style.normal.textColor = Color.cyan;
                GUI.Label(new Rect(xPos, yPos, 240, 30), $"Press {threatBoostKey} to Boost Threat", style);
            }
        }
        else
        {
            // Show class for all classes
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.UpperRight;
            
            float yPos = Screen.height - 150f;
            float xPos = Screen.width - 250f;
            
            Color classColor = GetClassColor();
            style.normal.textColor = classColor;
            GUI.Label(new Rect(xPos, yPos, 240, 30), $"Class: {playerClass}", style);
        }
    }
    
    public Color GetClassColor()
    {
        switch (playerClass)
        {
            case PlayerClass.Tank:
                return new Color(0.8f, 0.2f, 0.2f, 1f); // Dark red
            case PlayerClass.Healer:
                return Color.green;
            case PlayerClass.RangedDPS:
                return Color.cyan;
            case PlayerClass.MeleeDPS:
                return Color.yellow;
            default:
                return Color.white;
        }
    }
}

