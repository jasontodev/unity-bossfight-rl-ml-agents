/*
using UnityEngine;

public class HalfHealthDummy : MonoBehaviour
{
    void Start()
    {
        // Delay to ensure HealthSystem.Start() has run first
        Invoke(nameof(SetHalfHealth), 0.1f);
    }
    
    void SetHalfHealth()
    {
        HealthSystem healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // Set health to half of max health using reflection to set currentHealth directly
            var healthField = typeof(HealthSystem).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (healthField != null)
            {
                float maxHealth = healthSystem.MaxHealth;
                healthField.SetValue(healthSystem, maxHealth / 2f);
            }
        }
        
        // Remove this component after setting health
        Destroy(this);
    }
}


*/