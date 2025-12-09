using UnityEngine;
using System.Collections.Generic;

public class MaskGenerator
{
    public static bool[] GenerateVisibilityMask(GameObject self, List<GameObject> entities, float visionConeAngle = 90f)
    {
        bool[] mask = new bool[entities.Count];
        
        Vector3 selfForward = self.transform.forward;
        Vector3 selfPos = self.transform.position;
        CharacterController selfController = self.GetComponent<CharacterController>();
        if (selfController != null)
        {
            selfPos += selfController.center;
        }
        
        float halfConeAngle = visionConeAngle / 2f;
        
        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] == null)
            {
                mask[i] = false;
                continue;
            }
            
            Vector3 entityPos = entities[i].transform.position;
            CharacterController entityController = entities[i].GetComponent<CharacterController>();
            if (entityController != null)
            {
                entityPos += entityController.center;
            }
            
            Vector3 directionToEntity = (entityPos - selfPos).normalized;
            
            // Check if within vision cone
            float angle = Vector3.Angle(selfForward, directionToEntity);
            bool inVisionCone = angle <= halfConeAngle;
            
            // Check line of sight
            bool hasLineOfSight = CheckLineOfSight(selfPos, entityPos, entities[i]);
            
            mask[i] = inVisionCone && hasLineOfSight;
        }
        
        return mask;
    }
    
    static bool CheckLineOfSight(Vector3 from, Vector3 to, GameObject target)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);
        
        RaycastHit[] hits = Physics.RaycastAll(from, direction, distance);
        
        foreach (RaycastHit hit in hits)
        {
            // Ignore self
            if (hit.collider.transform.root == target.transform.root)
            {
                continue;
            }
            
            // If we hit something that's not the target, no line of sight
            if (hit.collider.transform.root != target.transform.root)
            {
                // Check if it's a wall (walls can block line of sight)
                if (hit.collider.GetComponent<Wall>() != null)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    public static float[] ApplyMaskToObservations(float[] observations, bool[] mask, int observationsPerEntity)
    {
        // Apply mask by zeroing out observations for non-visible entities
        // Assumes observations are structured as [entity0_obs, entity1_obs, ...]
        
        float[] maskedObservations = new float[observations.Length];
        
        int entityCount = mask.Length;
        for (int i = 0; i < entityCount; i++)
        {
            int startIndex = i * observationsPerEntity;
            int endIndex = startIndex + observationsPerEntity;
            
            if (mask[i])
            {
                // Copy observations for visible entities
                for (int j = startIndex; j < endIndex && j < observations.Length; j++)
                {
                    maskedObservations[j] = observations[j];
                }
            }
            else
            {
                // Zero out observations for non-visible entities
                for (int j = startIndex; j < endIndex && j < observations.Length; j++)
                {
                    maskedObservations[j] = 0f;
                }
            }
        }
        
        return maskedObservations;
    }
}

