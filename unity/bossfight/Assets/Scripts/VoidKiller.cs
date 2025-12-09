using UnityEngine;

public class VoidKiller : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Check if the object has a HealthSystem component
        HealthSystem healthSystem = other.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // Respect void grace (used on respawn in ML-Agents arena)
            if (healthSystem.IsInVoidGrace) return;

            // Instantly kill by setting health to 0
            healthSystem.TakeDamage(healthSystem.MaxHealth);
            Debug.Log($"{other.name} fell into the void and died!");
        }
    }
}

