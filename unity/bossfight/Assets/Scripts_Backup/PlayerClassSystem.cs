/*
using UnityEngine;

public class PlayerClassSystem : MonoBehaviour
{
    [Header("Class Settings")]
    [SerializeField] private PlayerClass playerClass = PlayerClass.MeleeDPS;
    
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
    
    void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
        // Delay color update to ensure capsule visual is created
        Invoke(nameof(UpdatePlayerColor), 0.1f);
    }
    
    void UpdatePlayerColor()
    {
        // Find the capsule visual and update its color based on class
        Renderer capsuleRenderer = GetComponentInChildren<Renderer>();
        if (capsuleRenderer != null)
        {
            Material playerMaterial = capsuleRenderer.material;
            if (playerMaterial != null)
            {
                Color classColor = GetClassVisualColor();
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
    
    Color GetClassVisualColor()
    {
        switch (playerClass)
        {
            case PlayerClass.Tank:
                return Color.blue;
            case PlayerClass.Healer:
                return Color.green;
            case PlayerClass.MeleeDPS:
                return Color.red;
            case PlayerClass.RangedDPS:
                return new Color(0.5f, 0f, 0.5f, 1f); // Purple
            default:
                return new Color(0.2f, 0.4f, 0.8f, 1f); // Default blue
        }
    }
    
    public float GetAttackDamage()
    {
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
                return 5f;
        }
    }
    
    void Update()
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
    
    void TryHeal()
    {
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
    
    void TryThreatBoost()
    {
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


*/