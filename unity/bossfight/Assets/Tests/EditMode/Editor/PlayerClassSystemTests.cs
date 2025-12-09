using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BossFightTests.EditMode
{
    /// <summary>
    /// Edit Mode tests for PlayerClassSystem
    /// </summary>
    public class PlayerClassSystemTests
    {
        private GameObject testObject;
        private PlayerClassSystem classSystem;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestPlayer");
            classSystem = testObject.AddComponent<PlayerClassSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void PlayerClassSystem_Initialization_StartsAsNone()
        {
            // Assert
            Assert.AreEqual(PlayerClass.None, classSystem.CurrentClass, 
                "Player should start with no class");
        }

        [Test]
        public void PlayerClassSystem_SetPlayerClass_Tank_Succeeds()
        {
            // Act
            bool success = classSystem.SetPlayerClass(PlayerClass.Tank);

            // Assert
            Assert.IsTrue(success, "Setting class to Tank should succeed");
            Assert.AreEqual(PlayerClass.Tank, classSystem.CurrentClass, 
                "Class should be set to Tank");
        }

        [Test]
        public void PlayerClassSystem_SetPlayerClass_Healer_Succeeds()
        {
            // Act
            bool success = classSystem.SetPlayerClass(PlayerClass.Healer);

            // Assert
            Assert.IsTrue(success, "Setting class to Healer should succeed");
            Assert.AreEqual(PlayerClass.Healer, classSystem.CurrentClass, 
                "Class should be set to Healer");
        }

        [Test]
        public void PlayerClassSystem_SetPlayerClass_CanOnlySetOnce()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.Tank);

            // Act
            bool secondAttempt = classSystem.SetPlayerClass(PlayerClass.Healer);

            // Assert
            Assert.IsFalse(secondAttempt, "Should not be able to change class after it's locked");
            Assert.AreEqual(PlayerClass.Tank, classSystem.CurrentClass, 
                "Class should remain Tank");
        }

        [Test]
        public void PlayerClassSystem_GetAttackDamage_Tank_ReturnsCorrectDamage()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.Tank);
            float expectedDamage = 2f; // Tank damage from PlayerClassSystem

            // Act
            float actualDamage = classSystem.GetAttackDamage();

            // Assert
            Assert.AreEqual(expectedDamage, actualDamage, 
                "Tank should deal 2 damage");
        }

        [Test]
        public void PlayerClassSystem_GetAttackDamage_None_ReturnsZero()
        {
            // Act
            float damage = classSystem.GetAttackDamage();

            // Assert
            Assert.AreEqual(0f, damage, 
                "No class should deal 0 damage");
        }

        [Test]
        public void PlayerClassSystem_DamageReduction_Tank_HasReduction()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.Tank);
            float expectedReduction = 0.4f; // 40% reduction

            // Act
            float actualReduction = classSystem.DamageReduction;

            // Assert
            Assert.AreEqual(expectedReduction, actualReduction, 
                "Tank should have 40% damage reduction");
        }

        [Test]
        public void PlayerClassSystem_DamageReduction_NonTank_NoReduction()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.Healer);

            // Act
            float reduction = classSystem.DamageReduction;

            // Assert
            Assert.AreEqual(0f, reduction, 
                "Non-tank classes should have no damage reduction");
        }

        [Test]
        public void PlayerClassSystem_AttackRangeMultiplier_RangedDPS_HasMultiplier()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.RangedDPS);
            float expectedMultiplier = 3f; // Triple range

            // Act
            float actualMultiplier = classSystem.AttackRangeMultiplier;

            // Assert
            Assert.AreEqual(expectedMultiplier, actualMultiplier, 
                "RangedDPS should have 3x attack range multiplier");
        }

        [Test]
        public void PlayerClassSystem_ResetClassForEpisode_UnlocksClass()
        {
            // Arrange
            classSystem.SetPlayerClass(PlayerClass.Tank);

            // Act
            classSystem.ResetClassForEpisode();

            // Assert
            Assert.AreEqual(PlayerClass.None, classSystem.CurrentClass, 
                "Class should be reset to None");
            // Note: IsClassLocked is private, so we test by trying to set class again
            bool canSetAgain = classSystem.SetPlayerClass(PlayerClass.Healer);
            Assert.IsTrue(canSetAgain, "Should be able to set class again after reset");
        }
    }
}

