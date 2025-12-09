/*
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Lava Damage Settings")]
    [SerializeField] public float lavaDamagePerTick = 8f;
    [SerializeField] public float lavaTickInterval = 1f; // Damage every 1 second
    [SerializeField] public float burnDamagePerTick = 3f;
    [SerializeField] public float burnTickInterval = 1f; // Burn damage every 1 second
    [SerializeField] private float burnDuration = 5f;
    
    private bool isInLava = false;
    private float burnTimer = 0f;
    private bool isBurning = false;
    private float lavaDamageTimer = 0f;
    private float burnDamageTimer = 0f;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsBurning => isBurning;
    
    void Start()
    {
        currentHealth = maxHealth;
    }
    
    void Update()
    {
        // Handle lava damage (tick-based)
        if (isInLava)
        {
            lavaDamageTimer += Time.deltaTime;
            if (lavaDamageTimer >= lavaTickInterval)
            {
                TakeDamage(lavaDamagePerTick);
                lavaDamageTimer = 0f;
            }
        }
        else
        {
            // Reset lava damage timer when not in lava
            lavaDamageTimer = 0f;
        }
        
        // Handle burn effect after leaving lava (tick-based)
        // Burn only happens when NOT in lava and isBurning is true
        if (isBurning && !isInLava)
        {
            burnTimer -= Time.deltaTime;
            burnDamageTimer += Time.deltaTime;
            
            // Apply burn damage on tick
            if (burnDamageTimer >= burnTickInterval)
            {
                TakeDamage(burnDamagePerTick);
                burnDamageTimer = 0f;
            }
            
            // Stop burning when timer expires (exactly 5 seconds)
            if (burnTimer <= 0f)
            {
                isBurning = false;
                burnTimer = 0f;
                burnDamageTimer = 0f;
            }
        }
        else if (!isBurning)
        {
            // Reset burn damage timer when not burning
            burnDamageTimer = 0f;
        }
        
        // Clamp health
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
    
    public void TakeDamage(float damage)
    {
        // Apply damage reduction if this is a tank
        PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
        if (classSystem != null && classSystem.DamageReduction > 0f)
        {
            damage = damage * (1f - classSystem.DamageReduction);
        }
        
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    
    public void SetInLava(bool inLava)
    {
        // Only process state changes to avoid resetting timers unnecessarily
        if (isInLava == inLava) return; // No state change
        
        bool wasInLava = isInLava;
        isInLava = inLava;
        
        // When leaving lava, start the burn timer (only once)
        if (wasInLava && !inLava)
        {
            isBurning = true;
            burnTimer = burnDuration; // Start exactly 5 second burn timer
            burnDamageTimer = 0f; // Reset burn damage timer
        }
        
        // When entering lava, cancel any active burn
        if (inLava)
        {
            isBurning = false;
            burnTimer = 0f;
            burnDamageTimer = 0f;
        }
    }
    
    public float GetBurnTimer() => burnTimer;
    public bool GetIsInLava() => isInLava;
    
    private void OnDeath()
    {
        Debug.Log($"{gameObject.name} has died!");
        // Add death logic here (respawn, game over, etc.)
    }
}


*/