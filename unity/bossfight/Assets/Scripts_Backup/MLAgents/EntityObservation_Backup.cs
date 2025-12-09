/*
using UnityEngine;
using System.Collections.Generic;

public class EntityObservation
{
    public static float[] GetEntityObservations(GameObject self, int maxAgents = 4, int maxWalls = 3)
    {
        List<float> observations = new List<float>();
        
        // Get all agents (party members and boss)
        List<GameObject> allAgents = new List<GameObject>();
        
        // Find party members
        PlayerController[] players = Object.FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.gameObject != self && !player.GetComponent<HealthSystem>()?.IsDead == true)
            {
                allAgents.Add(player.gameObject);
            }
        }
        
        StationaryPlayer[] stationaryPlayers = Object.FindObjectsOfType<StationaryPlayer>();
        foreach (StationaryPlayer player in stationaryPlayers)
        {
            if (player.gameObject != self && !player.GetComponent<HealthSystem>()?.IsDead == true)
            {
                allAgents.Add(player.gameObject);
            }
        }
        
        // Find boss
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss != null && boss.gameObject != self && !boss.GetComponent<HealthSystem>()?.IsDead == true)
        {
            allAgents.Add(boss.gameObject);
        }
        
        StationaryBoss stationaryBoss = Object.FindObjectOfType<StationaryBoss>();
        if (stationaryBoss != null && stationaryBoss.gameObject != self && !stationaryBoss.GetComponent<HealthSystem>()?.IsDead == true)
        {
            allAgents.Add(stationaryBoss.gameObject);
        }
        
        // Encode agents (position, velocity, health, class, threat relative to self)
        Vector3 selfPos = self.transform.position;
        CharacterController selfController = self.GetComponent<CharacterController>();
        if (selfController != null)
        {
            selfPos += selfController.center;
        }
        
        for (int i = 0; i < maxAgents; i++)
        {
            if (i < allAgents.Count)
            {
                GameObject agent = allAgents[i];
                Vector3 agentPos = agent.transform.position;
                CharacterController agentController = agent.GetComponent<CharacterController>();
                if (agentController != null)
                {
                    agentPos += agentController.center;
                }
                
                // Relative position (normalized)
                Vector3 relativePos = agentPos - selfPos;
                float distance = relativePos.magnitude;
                float normalizedDistance = Mathf.Clamp01(distance / 20f); // Max arena distance ~14, use 20 for safety
                
                // Position (relative, normalized)
                observations.Add(relativePos.x / 20f);
                observations.Add(relativePos.y / 20f);
                observations.Add(relativePos.z / 20f);
                observations.Add(normalizedDistance);
                
                // Velocity (simplified - use transform forward direction * speed estimate)
                float speed = 0f; // Could track velocity, for now use 0
                observations.Add(speed / 10f); // Normalize
                
                // Health (normalized 0-1)
                HealthSystem health = agent.GetComponent<HealthSystem>();
                float healthNormalized = health != null ? health.HealthPercentage : 0f;
                observations.Add(healthNormalized);
                
                // Class (one-hot: Tank=0, Healer=1, RangedDPS=2, MeleeDPS=3, Boss=4)
                PlayerClassSystem classSystem = agent.GetComponent<PlayerClassSystem>();
                BossController bossController = agent.GetComponent<BossController>();
                StationaryBoss stationaryBossController = agent.GetComponent<StationaryBoss>();
                
                float classValue = 4f; // Default to boss
                if (classSystem != null)
                {
                    switch (classSystem.CurrentClass)
                    {
                        case PlayerClass.None:
                            classValue = -1f; // No class selected
                            break;
                        case PlayerClass.Tank:
                            classValue = 0f;
                            break;
                        case PlayerClass.Healer:
                            classValue = 1f;
                            break;
                        case PlayerClass.RangedDPS:
                            classValue = 2f;
                            break;
                        case PlayerClass.MeleeDPS:
                            classValue = 3f;
                            break;
                    }
                }
                else if (bossController == null && stationaryBossController == null)
                {
                    classValue = 0f; // Unknown party member
                }
                observations.Add(classValue / 4f); // Normalize
                
                // Threat (normalized)
                ThreatSystem threatSystem = ThreatSystem.Instance;
                float threat = threatSystem != null ? threatSystem.GetThreat(agent) : 0f;
                observations.Add(Mathf.Clamp01(threat / 100f)); // Normalize (assuming max threat ~100)
            }
            else
            {
                // No agent - pad with zeros
                observations.Add(0f); // pos x
                observations.Add(0f); // pos y
                observations.Add(0f); // pos z
                observations.Add(1f); // distance (max)
                observations.Add(0f); // velocity
                observations.Add(0f); // health
                observations.Add(0f); // class
                observations.Add(0f); // threat
            }
        }
        
        // Get walls
        Wall[] wallObjects = Object.FindObjectsOfType<Wall>();
        List<GameObject> walls = new List<GameObject>();
        foreach (Wall wall in wallObjects)
        {
            walls.Add(wall.gameObject);
        }
        
        // Encode walls (position, velocity, size relative to self)
        for (int i = 0; i < maxWalls; i++)
        {
            if (i < walls.Count)
            {
                GameObject wall = walls[i];
                Vector3 wallPos = wall.transform.position;
                Vector3 relativePos = wallPos - selfPos;
                float distance = relativePos.magnitude;
                float normalizedDistance = Mathf.Clamp01(distance / 20f);
                
                // Position (relative, normalized)
                observations.Add(relativePos.x / 20f);
                observations.Add(relativePos.y / 20f);
                observations.Add(relativePos.z / 20f);
                observations.Add(normalizedDistance);
                
                // Velocity (walls are stationary, so 0)
                observations.Add(0f);
                
                // Size (normalized)
                Vector3 scale = wall.transform.localScale;
                observations.Add(scale.x / 5f); // Normalize
                observations.Add(scale.y / 5f);
                observations.Add(scale.z / 5f);
                
                // Is being carried
                Wall wallComponent = wall.GetComponent<Wall>();
                observations.Add(wallComponent != null && wallComponent.IsBeingCarried() ? 1f : 0f);
            }
            else
            {
                // No wall - pad with zeros
                observations.Add(0f); // pos x
                observations.Add(0f); // pos y
                observations.Add(0f); // pos z
                observations.Add(1f); // distance (max)
                observations.Add(0f); // velocity
                observations.Add(0f); // size x
                observations.Add(0f); // size y
                observations.Add(0f); // size z
                observations.Add(0f); // is carried
            }
        }
        
        return observations.ToArray();
    }
}


*/
