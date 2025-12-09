/*
using UnityEngine;
using System.Collections.Generic;

public class ObservationEncoder
{
    public static float[] EncodeObservations(GameObject agent, LIDARSystem lidarSystem)
    {
        if (agent == null) return new float[164]; // Return default size if agent is null
        
        List<float> allObservations = new List<float>();
        
        // 1. Self observations
        float[] selfObs = SelfObservation.GetSelfObservations(agent);
        if (selfObs != null) allObservations.AddRange(selfObs);
        
        // 2. LIDAR observations (circular 1D convolution input)
        float[] lidarObs = LIDARObservation.GetLIDARObservations(lidarSystem, agent);
        if (lidarObs != null) allObservations.AddRange(lidarObs);
        
        // 3. Entity observations (other agents, walls)
        float[] entityObs = EntityObservation.GetEntityObservations(agent, maxAgents: 4, maxWalls: 3);
        allObservations.AddRange(entityObs);
        
        // 4. Generate masks and apply to entity observations
        // Get all entities for masking
        List<GameObject> allEntities = new List<GameObject>();
        
        // Add agents
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.gameObject != agent && !player.GetComponent<HealthSystem>()?.IsDead == true)
            {
                allEntities.Add(player.gameObject);
            }
        }
        
        StationaryPlayer[] stationaryPlayers = Object.FindObjectsOfType<StationaryPlayer>();
        foreach (StationaryPlayer player in stationaryPlayers)
        {
            if (player.gameObject != agent && !player.GetComponent<HealthSystem>()?.IsDead == true)
            {
                allEntities.Add(player.gameObject);
            }
        }
        
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss != null && boss.gameObject != agent && !boss.GetComponent<HealthSystem>()?.IsDead == true)
        {
            allEntities.Add(boss.gameObject);
        }
        
        StationaryBoss stationaryBoss = Object.FindObjectOfType<StationaryBoss>();
        if (stationaryBoss != null && stationaryBoss.gameObject != agent && !stationaryBoss.GetComponent<HealthSystem>()?.IsDead == true)
        {
            allEntities.Add(stationaryBoss.gameObject);
        }
        
        // Add walls
        Wall[] walls = Object.FindObjectsOfType<Wall>();
        foreach (Wall wall in walls)
        {
            allEntities.Add(wall.gameObject);
        }
        
        // Generate visibility mask (frontal vision cone + line of sight)
        bool[] visibilityMask = MaskGenerator.GenerateVisibilityMask(agent, allEntities, visionConeAngle: 90f);
        
        // Apply mask to entity observations (entity observations are at the end)
        // Entity observations: 4 agents * 8 values + 3 walls * 9 values = 32 + 27 = 59 values
        int entityObsStartIndex = selfObs.Length + lidarObs.Length;
        int observationsPerAgent = 8;
        int observationsPerWall = 9;
        
        // Create masked entity observations
        float[] maskedEntityObs = new float[entityObs.Length];
        int maskIndex = 0;
        
        // Apply mask to agents (first 4 entities)
        for (int i = 0; i < 4; i++)
        {
            bool visible = maskIndex < visibilityMask.Length && visibilityMask[maskIndex];
            int startIdx = i * observationsPerAgent;
            
            for (int j = 0; j < observationsPerAgent; j++)
            {
                if (visible)
                {
                    maskedEntityObs[startIdx + j] = entityObs[startIdx + j];
                }
                else
                {
                    maskedEntityObs[startIdx + j] = 0f;
                }
            }
            maskIndex++;
        }
        
        // Apply mask to walls (next 3 entities)
        for (int i = 0; i < 3; i++)
        {
            bool visible = maskIndex < visibilityMask.Length && visibilityMask[maskIndex];
            int startIdx = (4 * observationsPerAgent) + (i * observationsPerWall);
            
            for (int j = 0; j < observationsPerWall; j++)
            {
                if (visible)
                {
                    maskedEntityObs[startIdx + j] = entityObs[startIdx + j];
                }
                else
                {
                    maskedEntityObs[startIdx + j] = 0f;
                }
            }
            maskIndex++;
        }
        
        // Replace entity observations with masked version
        allObservations.RemoveRange(entityObsStartIndex, entityObs.Length);
        allObservations.InsertRange(entityObsStartIndex, maskedEntityObs);
        
        return allObservations.ToArray();
    }
}


*/
