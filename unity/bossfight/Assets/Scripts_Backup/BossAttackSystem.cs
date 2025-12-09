/*
using UnityEngine;

[RequireComponent(typeof(LIDARSystem))]
public class BossAttackSystem : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 100f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;
    
    private float lastAttackTime = 0f;
    private BossController bossController;
    private LIDARSystem lidarSystem;
    
    void Start()
    {
        bossController = GetComponent<BossController>();
        lidarSystem = GetComponent<LIDARSystem>();
        
        if (lidarSystem == null)
        {
            Debug.LogError("BossAttackSystem requires LIDARSystem component!");
        }
    }
    
    void Update()
    {
        // Check for attack input
        if (Input.GetKeyDown(attackKey) && Time.time >= lastAttackTime + attackCooldown)
        {
            TryAttack();
        }
    }
    
    void OnGUI()
    {
        // Display attack info in top right corner
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;
        
        float yPos = 10f;
        float lineHeight = 25f;
        float xPos = Screen.width - 310f;
        
        // Attack instructions
        GUI.Label(new Rect(xPos, yPos, 300, 30), $"Press {attackKey} to Attack", style);
        yPos += lineHeight;
        
        // Attack damage
        style.normal.textColor = Color.red;
        GUI.Label(new Rect(xPos, yPos, 300, 30), $"Attack Damage: {attackDamage}", style);
        yPos += lineHeight;
        
        // LIDAR status
        if (lidarSystem != null && lidarSystem.IsForwardRayHittingTarget())
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(xPos, yPos, 300, 30), "Target in Front: YES", style);
        }
        else
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(xPos, yPos, 300, 30), "Target in Front: NO", style);
        }
        yPos += lineHeight;
        
        // Cooldown status
        float cooldownRemaining = Mathf.Max(0f, (lastAttackTime + attackCooldown) - Time.time);
        if (cooldownRemaining > 0f)
        {
            style.normal.textColor = new Color(1f, 0.5f, 0f, 1f); // Orange
            GUI.Label(new Rect(xPos, yPos, 300, 30), $"Cooldown: {cooldownRemaining:F1}s", style);
        }
        else
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(xPos, yPos, 300, 30), "Ready to Attack!", style);
        }
    }
    
    void TryAttack()
    {
        // Only attack if forward LIDAR ray is hitting a target
        if (lidarSystem == null || !lidarSystem.IsForwardRayHittingTarget())
        {
            return;
        }
        
        // Get the target from the forward ray
        GameObject target = lidarSystem.GetForwardRayTarget();
        if (target == null) return;
        
        // Check if it's a player (either PlayerController or StationaryPlayer)
        PlayerController player = target.GetComponent<PlayerController>();
        StationaryPlayer stationaryPlayer = target.GetComponent<StationaryPlayer>();
        
        if (player != null || stationaryPlayer != null)
        {
            // Get the player's health system and deal damage
            HealthSystem playerHealth = target.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Boss attacked player for {attackDamage} damage! Player health: {playerHealth.CurrentHealth}");
                lastAttackTime = Time.time;
            }
        }
    }
    
}


*/