using UnityEngine;
using System.Collections.Generic;

public class LIDARObservation
{
    public static float[] GetLIDARObservations(LIDARSystem lidarSystem, GameObject self)
    {
        if (lidarSystem == null || self == null) return new float[30 * 3]; // 30 rays * 3 values (distance, hitType, entityType)
        
        RaycastHit[] rayHits = lidarSystem.GetAllRayHits();
        bool[] rayHitsTarget = lidarSystem.GetRayHitsTarget();
        float rayRange = 10f;
        float attackRange = lidarSystem.AttackRange;
        
        float[] observations = new float[30 * 3]; // 30 rays, 3 values each
        
        // Safety checks
        if (rayHits == null) rayHits = new RaycastHit[0];
        if (rayHitsTarget == null) rayHitsTarget = new bool[0];
        
        for (int i = 0; i < 30; i++)
        {
            int baseIndex = i * 3;
            
            if (i < rayHits.Length && rayHits[i].collider != null && i < rayHitsTarget.Length)
            {
                float distance = Vector3.Distance(
                    self.transform.position + (self.GetComponent<CharacterController>()?.center ?? Vector3.zero),
                    rayHits[i].point
                );
                
                // Normalize distance (0-1 range)
                float normalizedDistance = Mathf.Clamp01(distance / rayRange);
                observations[baseIndex] = normalizedDistance;
                
                // Hit type: 0=nothing, 1=agent, 2=wall, 3=lava, 4=void
                float hitType = 0f;
                GameObject hitObject = rayHits[i].collider.gameObject;
                
                // Check for agents
                if (hitObject.GetComponent<PlayerController>() != null || 
                    hitObject.GetComponent<BossController>() != null ||
                    hitObject.GetComponent<StationaryPlayer>() != null ||
                    hitObject.GetComponent<StationaryBoss>() != null)
                {
                    hitType = 1f;
                }
                // Check for walls
                else if (hitObject.GetComponent<Wall>() != null)
                {
                    hitType = 2f;
                }
                // Check for lava
                else if (hitObject.CompareTag("Lava"))
                {
                    hitType = 3f;
                }
                // Check for void
                else if (hitObject.CompareTag("Void"))
                {
                    hitType = 4f;
                }
                
                observations[baseIndex + 1] = hitType;
                
                // Entity type (for agents: 0=party, 1=boss; normalized)
                float entityType = 0f;
                if (hitType == 1f) // Agent
                {
                    if (hitObject.GetComponent<BossController>() != null || 
                        hitObject.GetComponent<StationaryBoss>() != null)
                    {
                        entityType = 1f; // Boss
                    }
                    else
                    {
                        entityType = 0f; // Party member
                    }
                }
                observations[baseIndex + 2] = entityType;
            }
            else
            {
                // No hit - max distance
                observations[baseIndex] = 1f; // Normalized max distance
                observations[baseIndex + 1] = 0f; // No hit type
                observations[baseIndex + 2] = 0f; // No entity type
            }
        }
        
        return observations;
    }
}

