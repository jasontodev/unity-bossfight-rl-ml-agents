using NUnit.Framework;
using UnityEngine;

namespace BossFightTests.EditMode
{
    /// <summary>
    /// Edit Mode tests for ThreatSystem
    /// </summary>
    public class ThreatSystemTests
    {
        private ThreatSystem threatSystem;
        private GameObject agent1;
        private GameObject agent2;

        [SetUp]
        public void SetUp()
        {
            // ThreatSystem is a singleton, so we need to handle it carefully
            threatSystem = ThreatSystem.Instance;
            if (threatSystem == null)
            {
                GameObject threatSystemObj = new GameObject("Threat System");
                threatSystem = threatSystemObj.AddComponent<ThreatSystem>();
            }
            
            agent1 = new GameObject("Agent 1");
            agent2 = new GameObject("Agent 2");
        }

        [TearDown]
        public void TearDown()
        {
            if (threatSystem != null)
            {
                threatSystem.ClearAllThreat();
                Object.DestroyImmediate(threatSystem.gameObject);
            }
            if (agent1 != null) Object.DestroyImmediate(agent1);
            if (agent2 != null) Object.DestroyImmediate(agent2);
        }

        [Test]
        public void ThreatSystem_AddThreat_IncreasesThreat()
        {
            // Arrange
            float threatAmount = 10f;
            float initialThreat = threatSystem.GetThreat(agent1);

            // Act
            threatSystem.AddThreat(agent1, threatAmount);

            // Assert
            Assert.AreEqual(initialThreat + threatAmount, threatSystem.GetThreat(agent1), 
                "Threat should increase by the amount added");
        }

        [Test]
        public void ThreatSystem_GetThreat_ReturnsZeroForNewAgent()
        {
            // Act
            float threat = threatSystem.GetThreat(agent1);

            // Assert
            Assert.AreEqual(0f, threat, "New agent should have zero threat");
        }

        [Test]
        public void ThreatSystem_ClearAllThreat_ResetsAllThreat()
        {
            // Arrange
            threatSystem.AddThreat(agent1, 50f);
            threatSystem.AddThreat(agent2, 30f);

            // Act
            threatSystem.ClearAllThreat();

            // Assert
            Assert.AreEqual(0f, threatSystem.GetThreat(agent1), 
                "Agent 1 threat should be cleared");
            Assert.AreEqual(0f, threatSystem.GetThreat(agent2), 
                "Agent 2 threat should be cleared");
        }

        [Test]
        public void ThreatSystem_GetHighestThreat_ReturnsCorrectAgent()
        {
            // Arrange
            threatSystem.AddThreat(agent1, 20f);
            threatSystem.AddThreat(agent2, 50f);

            // Act
            GameObject highestThreatAgent = threatSystem.GetHighestThreatAgent();

            // Assert
            Assert.AreEqual(agent2, highestThreatAgent, 
                "Agent 2 should have highest threat");
        }
    }
}

