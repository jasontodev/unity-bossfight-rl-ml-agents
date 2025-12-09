using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BossFightTests.EditMode
{
    /// <summary>
    /// Edit Mode tests for HealthSystem
    /// These tests run in the Unity Editor without entering Play Mode
    /// </summary>
    public class HealthSystemTests
    {
        private GameObject testObject;
        private HealthSystem healthSystem;

        [SetUp]
        public void SetUp()
        {
            // Create a test GameObject with HealthSystem component
            testObject = new GameObject("TestHealthObject");
            healthSystem = testObject.AddComponent<HealthSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void HealthSystem_Initialization_SetsMaxHealth()
        {
            // Arrange
            float expectedMaxHealth = 100f;

            // Act
            // HealthSystem initializes in Start(), but we can test the property
            float actualMaxHealth = healthSystem.MaxHealth;

            // Assert
            Assert.AreEqual(expectedMaxHealth, actualMaxHealth, "Max health should be 100");
        }

        [Test]
        public void HealthSystem_TakeDamage_ReducesHealth()
        {
            // Arrange
            float damage = 25f;
            float initialHealth = healthSystem.CurrentHealth;

            // Act
            healthSystem.TakeDamage(damage);

            // Assert
            Assert.AreEqual(initialHealth - damage, healthSystem.CurrentHealth, 
                "Health should decrease by damage amount");
        }

        [Test]
        public void HealthSystem_TakeDamage_DoesNotGoBelowZero()
        {
            // Arrange
            float massiveDamage = 1000f;

            // Act
            healthSystem.TakeDamage(massiveDamage);

            // Assert
            Assert.GreaterOrEqual(healthSystem.CurrentHealth, 0f, 
                "Health should not go below zero");
        }

        [Test]
        public void HealthSystem_HealthPercentage_CalculatesCorrectly()
        {
            // Arrange
            float damage = 50f;
            float expectedPercentage = 0.5f; // 50/100 = 0.5

            // Act
            healthSystem.TakeDamage(damage);
            float actualPercentage = healthSystem.HealthPercentage;

            // Assert
            Assert.AreEqual(expectedPercentage, actualPercentage, 0.01f, 
                "Health percentage should be 0.5 after taking 50 damage");
        }

        [Test]
        public void HealthSystem_Death_WhenHealthReachesZero()
        {
            // Arrange
            float lethalDamage = healthSystem.MaxHealth;

            // Act
            healthSystem.TakeDamage(lethalDamage);

            // Assert
            Assert.IsTrue(healthSystem.IsDead, "Agent should be dead when health reaches zero");
        }

        [Test]
        public void HealthSystem_Respawn_ResetsHealth()
        {
            // Arrange
            healthSystem.TakeDamage(50f); // Reduce health
            float healthBeforeRespawn = healthSystem.CurrentHealth;

            // Act
            healthSystem.Respawn();

            // Assert
            Assert.AreEqual(healthSystem.MaxHealth, healthSystem.CurrentHealth, 
                "Health should be reset to max after respawn");
            Assert.IsFalse(healthSystem.IsDead, "Agent should not be dead after respawn");
        }
    }
}

