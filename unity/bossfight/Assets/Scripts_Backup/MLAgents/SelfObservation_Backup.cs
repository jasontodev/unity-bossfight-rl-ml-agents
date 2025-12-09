/*
using UnityEngine;

public class SelfObservation
{
    public static float[] GetSelfObservations(GameObject self)
    {
        float[] observations = new float[15]; // Self state size
        int index = 0;
        
        // Position (normalized relative to arena center)
        Vector3 pos = self.transform.position;
        observations[index++] = pos.x / 20f; // Arena is 20x20, normalize
        observations[index++] = pos.y / 20f;
        observations[index++] = pos.z / 20f;
        
        // Velocity (simplified - forward direction * estimated speed)
        // For now, use 0 (could track actual velocity)
        observations[index++] = 0f;
        
        // Rotation (Y rotation normalized 0-1)
        float yRotation = self.transform.eulerAngles.y;
        observations[index++] = yRotation / 360f;
        
        // Health (normalized 0-1)
        HealthSystem health = self.GetComponent<HealthSystem>();
        if (health != null)
        {
            observations[index++] = health.HealthPercentage;
            observations[index++] = health.CurrentHealth / 100f; // Normalize by max health
        }
        else
        {
            observations[index++] = 0f;
            observations[index++] = 0f;
        }
        
        // Class (one-hot normalized: Tank=0, Healer=1, RangedDPS=2, MeleeDPS=3, Boss=4)
        PlayerClassSystem classSystem = self.GetComponent<PlayerClassSystem>();
        BossController bossController = self.GetComponent<BossController>();
        StationaryBoss stationaryBoss = self.GetComponent<StationaryBoss>();
        
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
        else if (bossController == null && stationaryBoss == null)
        {
            classValue = 0f; // Unknown
        }
        observations[index++] = classValue / 4f; // Normalize
        
        // Threat (normalized)
        ThreatSystem threatSystem = ThreatSystem.Instance;
        float threat = threatSystem != null ? threatSystem.GetThreat(self) : 0f;
        observations[index++] = Mathf.Clamp01(threat / 100f); // Normalize
        
        // Cooldowns (normalized 0-1, where 1 = ready, 0 = on cooldown)
        PlayerAttackSystem attackSystem = self.GetComponent<PlayerAttackSystem>();
        BossAttackSystem bossAttackSystem = self.GetComponent<BossAttackSystem>();
        PlayerClassSystem playerClass = self.GetComponent<PlayerClassSystem>();
        
        // Attack cooldown
        float attackCooldown = 0f;
        if (attackSystem != null)
        {
            // Get cooldown remaining (would need to expose this from PlayerAttackSystem)
            attackCooldown = 1f; // Assume ready for now
        }
        else if (bossAttackSystem != null)
        {
            attackCooldown = 1f; // Assume ready for now
        }
        observations[index++] = attackCooldown;
        
        // Heal cooldown (if healer)
        float healCooldown = 1f;
        if (playerClass != null && playerClass.CurrentClass == PlayerClass.Healer)
        {
            // Would need to expose cooldown from PlayerClassSystem
            healCooldown = 1f; // Assume ready for now
        }
        observations[index++] = healCooldown;
        
        // Threat boost cooldown (if tank)
        float threatBoostCooldown = 1f;
        if (playerClass != null && playerClass.CurrentClass == PlayerClass.Tank)
        {
            // Would need to expose cooldown from PlayerClassSystem
            threatBoostCooldown = 1f; // Assume ready for now
        }
        observations[index++] = threatBoostCooldown;
        
        // Is in lava
        bool inLava = health != null && health.GetIsInLava();
        observations[index++] = inLava ? 1f : 0f;
        
        // Is burning
        bool isBurning = health != null && health.IsBurning;
        observations[index++] = isBurning ? 1f : 0f;
        
        // Is dead
        bool isDead = health != null && health.IsDead;
        observations[index++] = isDead ? 1f : 0f;
        
        return observations;
    }
}


*/
