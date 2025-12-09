/*
using UnityEngine;

public class VoidKiller : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Check if the object has a HealthSystem component
        HealthSystem healthSystem = other.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // Instantly kill by setting health to 0
            healthSystem.TakeDamage(healthSystem.MaxHealth);
            Debug.Log($"{other.name} fell into the void and died!");
        }
    }
}


*/