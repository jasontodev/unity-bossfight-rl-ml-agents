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
    private bool isDead = false;
    private bool isDespawned = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Renderer[] renderers;
    private Collider[] colliders;
    private float lavaGraceEndsAt = 0f; // Track when lava grace period ends (slightly longer than void grace)
    private float voidSafeUntil = 0f; // grace period during which void kills are ignored
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsBurning => isBurning;
    public bool IsDead => isDead;
    public bool IsDespawned => isDespawned;
    public bool IsInVoidGrace => Time.time < voidSafeUntil;
    public bool IsInLavaGrace => Time.time < lavaGraceEndsAt;
    
    void Start()
    {
        currentHealth = maxHealth;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        
        // Reset lava/burn state on spawn to prevent false burn triggers
        isInLava = false;
        isBurning = false;
        burnTimer = 0f;
        lavaDamageTimer = 0f;
        burnDamageTimer = 0f;
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
        float originalDamage = damage;
        
        // Apply damage reduction if this is a tank
        PlayerClassSystem classSystem = GetComponent<PlayerClassSystem>();
        if (classSystem != null)
        {
            float damageReduction = classSystem.DamageReduction;
            if (damageReduction > 0f)
            {
                damage = damage * (1f - damageReduction);
                Debug.Log($"{gameObject.name} (Tank) took {originalDamage} damage, reduced to {damage} ({damageReduction * 100f}% reduction)");
            }
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
        // BUT: Don't trigger burn if we're still in grace period (prevents false burns on respawn)
        // This handles edge cases where physics callbacks fire right after grace ends
        if (wasInLava && !inLava && !IsInLavaGrace)
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
        if (!isDead)
        {
            isDead = true;
            Debug.Log($"{gameObject.name} has died!");
            Despawn();
        }
    }
    
    public void Despawn()
    {
        if (isDespawned) return;
        
        isDespawned = true;
        
        // Make invisible
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        
        // Disable colliders
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        
        // Disable CharacterController if present
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }
        
        // Disable all action systems
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != this && component != null)
            {
                // Keep HealthSystem, HealthBar, and LIDARSystem enabled for observation purposes
                if (!(component is HealthBar) && !(component is LIDARSystem))
                {
                    component.enabled = false;
                }
            }
        }
    }
    
    public void Respawn()
    {
        isDead = false;
        isDespawned = false;
        currentHealth = maxHealth;
        // Reset lava/burn state completely
        isBurning = false;
        isInLava = false;
        burnTimer = 0f;
        lavaDamageTimer = 0f;
        burnDamageTimer = 0f;
        
        // Initialize renderers and colliders if not already done
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
        if (colliders == null)
        {
            colliders = GetComponentsInChildren<Collider>();
        }
        
        // Restore visibility
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }
        
        // Restore colliders
        if (colliders != null)
        {
            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }
        }
        
        // Restore CharacterController
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = true;
        }
        
        // Restore all components
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != null)
            {
                component.enabled = true;
            }
        }
        
        // Reset position and rotation
        // If an EpisodeManager is present (ML-Agents training scene), it is responsible
        // for setting spawn positions. In that case, do NOT override the position/rotation
        // here to avoid race conditions and teleporting between two locations.
        if (EpisodeManager.Instance == null)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
    
    public void SetCurrentHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        if (currentHealth <= 0f && !isDead)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// Starts a short grace period during which void triggers will not kill this
    /// character. Used on respawn to prevent immediate re-death while input
    /// keys are still held down.
    /// Also starts a slightly longer lava grace period to prevent false burn triggers.
    /// </summary>
    public void StartVoidGrace(float duration)
    {
        voidSafeUntil = Time.time + duration;
        // Lava grace is slightly longer to handle physics callback timing edge cases
        lavaGraceEndsAt = Time.time + duration + 0.1f; // Extra 0.1s buffer
    }
}

