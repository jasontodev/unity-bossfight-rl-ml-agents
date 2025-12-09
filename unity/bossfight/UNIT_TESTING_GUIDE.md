# Unit Testing Guide for Boss Fight Game

## Overview

This project uses **Unity Test Framework** (UTF) for unit testing. Tests are organized into **Edit Mode** and **Play Mode** tests.

## Test Structure

```
Assets/
└── Tests/
    ├── EditMode/
    │   └── Editor/        # Edit Mode tests (legacy Editor folder structure)
    │       ├── HealthSystemTests.cs
    │       ├── PlayerClassSystemTests.cs
    │       └── ThreatSystemTests.cs
    └── PlayMode/          # Play Mode tests (in Tests folder)
        ├── HealthSystemPlayModeTests.cs
        └── EpisodeManagerTests.cs
```

**Note**: EditMode tests are in an `Editor` subfolder (legacy Unity approach) so they can access game scripts without assembly definitions. PlayMode tests are in the `Tests/PlayMode` folder and will be compiled into the default assembly, allowing them to access game scripts.

## How to Run Tests

### Method 1: Test Runner Window (Recommended)

1. **Open Test Runner Window**
   - Go to **Window > General > Test Runner**
   - Or press `Ctrl+Shift+T` (Windows) / `Cmd+Shift+T` (Mac)

2. **Select Test Mode**
   - Click **EditMode** tab for editor tests
   - Click **PlayMode** tab for play mode tests

3. **Run Tests**
   - Click **Run All** to run all tests
   - Click **Run Selected** to run only selected tests
   - Click individual test checkboxes to select specific tests

4. **View Results**
   - Tests show as ✅ (passed) or ❌ (failed)
   - Click on a test to see details
   - Check the console for error messages

### Method 2: Command Line

```bash
# Run all Edit Mode tests
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testPlatform EditMode

# Run all Play Mode tests
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testPlatform PlayMode

# Run specific test
Unity.exe -runTests -batchmode -projectPath "path/to/project" -testFilter "HealthSystemTests"
```

### Method 3: From Code

Tests can be run programmatically, but the Test Runner window is recommended.

## Test Types

### Edit Mode Tests

**Location**: `Tests/EditMode/`

**Characteristics**:
- Run in Unity Editor without entering Play Mode
- Fast execution
- Good for testing logic, calculations, data structures
- Cannot test time-based behaviors or physics

**Example Use Cases**:
- Health calculations
- Class selection logic
- Threat system math
- Data structure operations

**Example**:
```csharp
[Test]
public void HealthSystem_TakeDamage_ReducesHealth()
{
    healthSystem.TakeDamage(25f);
    Assert.AreEqual(75f, healthSystem.CurrentHealth);
}
```

### Play Mode Tests

**Location**: `Tests/PlayMode/`

**Characteristics**:
- Run in Play Mode (like the actual game)
- Can test time-based behaviors, coroutines, physics
- Slower than Edit Mode tests
- Use `UnityTest` attribute with `IEnumerator`

**Example Use Cases**:
- Burn damage over time
- Episode resets
- Movement and physics
- Coroutine behaviors

**Example**:
```csharp
[UnityTest]
public IEnumerator HealthSystem_BurnDamage_AppliesOverTime()
{
    healthSystem.SetInLava(true);
    yield return null;
    healthSystem.SetInLava(false);
    yield return new WaitForSeconds(5f);
    Assert.Less(healthSystem.CurrentHealth, initialHealth);
}
```

## Writing New Tests

### Step 1: Create Test File

1. In Unity, navigate to `Assets/Tests/EditMode` or `Assets/Tests/PlayMode`
2. Right-click > **Create > Testing > C# Test Script**
3. Name it appropriately (e.g., `MySystemTests.cs`)

### Step 2: Write Test Structure

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BossFightTests.EditMode  // or PlayMode
{
    public class MySystemTests
    {
        private GameObject testObject;
        private MySystem mySystem;

        [SetUp]
        public void SetUp()
        {
            // Runs before each test
            testObject = new GameObject("TestObject");
            mySystem = testObject.AddComponent<MySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            // Runs after each test - cleanup
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void MySystem_SomeMethod_ExpectedBehavior()
        {
            // Arrange - Set up test conditions
            float expectedValue = 10f;

            // Act - Execute the code being tested
            float actualValue = mySystem.SomeMethod();

            // Assert - Verify the result
            Assert.AreEqual(expectedValue, actualValue);
        }
    }
}
```

### Step 3: Use Appropriate Attributes

- `[Test]` - Standard test (Edit Mode or Play Mode)
- `[UnityTest]` - Play Mode test that can yield (use with `IEnumerator`)
- `[SetUp]` - Runs before each test
- `[TearDown]` - Runs after each test
- `[Ignore("Reason")]` - Skip a test temporarily

## Assertions

Common NUnit assertions:

```csharp
// Equality
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(expected, actual);

// Null checks
Assert.IsNull(object);
Assert.IsNotNull(object);

// Boolean
Assert.IsTrue(condition);
Assert.IsFalse(condition);

// Comparisons
Assert.Greater(actual, expected);
Assert.Less(actual, expected);
Assert.GreaterOrEqual(actual, expected);
Assert.LessOrEqual(actual, expected);

// Exceptions
Assert.Throws<ExceptionType>(() => { code(); });
```

## Test Naming Convention

Use the pattern: `SystemName_MethodName_ExpectedBehavior`

Examples:
- `HealthSystem_TakeDamage_ReducesHealth`
- `PlayerClassSystem_SetPlayerClass_Tank_Succeeds`
- `EpisodeManager_StartEpisode_ResetsAgents`

## Current Test Coverage

### Edit Mode Tests
- ✅ `HealthSystemTests` - Health calculations, damage, death
- ✅ `PlayerClassSystemTests` - Class selection, stats, abilities
- ✅ `ThreatSystemTests` - Threat generation and management

### Play Mode Tests
- ✅ `HealthSystemPlayModeTests` - Time-based behaviors (burn, lava)
- ✅ `EpisodeManagerTests` - Episode lifecycle and resets

## Running Specific Tests

### In Test Runner Window:
1. Expand the test tree
2. Check/uncheck specific tests
3. Click **Run Selected**

### From Command Line:
```bash
# Run tests matching a pattern
Unity.exe -runTests -testFilter "HealthSystem*"

# Run a specific test class
Unity.exe -runTests -testFilter "HealthSystemTests"
```

## Best Practices

1. **One Assert Per Test** (when possible)
   - Makes it clear what failed
   - Easier to debug

2. **Arrange-Act-Assert Pattern**
   - Arrange: Set up test conditions
   - Act: Execute the code
   - Assert: Verify results

3. **Test One Thing**
   - Each test should verify one behavior
   - If a test has multiple assertions, they should all verify the same behavior

4. **Clean Up Resources**
   - Always use `[TearDown]` to destroy GameObjects
   - Clean up singletons if modified

5. **Use Descriptive Names**
   - Test names should explain what is being tested
   - Follow the naming convention

6. **Test Edge Cases**
   - Zero values
   - Negative values
   - Null references
   - Boundary conditions

## Troubleshooting

### Tests Not Appearing
- Check that test files are in `Tests/EditMode` or `Tests/PlayMode`
- Ensure files have `.cs` extension
- Check for compilation errors in Console

### Tests Fail to Run
- Check Console for errors
- Ensure all required components are added in `[SetUp]`
- Verify test attributes are correct (`[Test]` vs `[UnityTest]`)

### Play Mode Tests Don't Work
- Ensure using `[UnityTest]` attribute
- Return type must be `IEnumerator`
- Use `yield return null` or `yield return new WaitForSeconds()`

### Singleton Issues
- Be careful with singletons in tests
- May need to destroy and recreate in `[TearDown]`
- Consider using dependency injection for testability

## Next Steps

1. **Add More Tests**
   - `LIDARSystemTests` - Raycasting, detection
   - `AttackSystemTests` - Damage calculations, cooldowns
   - `EpisodeRecorderTests` - Data serialization
   - `ObservationEncoderTests` - ML-Agents observations

2. **Integration Tests**
   - Test multiple systems working together
   - Test full episode flow

3. **Performance Tests**
   - Test frame rate with many agents
   - Test memory usage

4. **CI/CD Integration**
   - Run tests automatically on commits
   - Generate test reports

## Example: Adding a New Test

Let's add a test for `LIDARSystem`:

```csharp
// Tests/EditMode/LIDARSystemTests.cs
using NUnit.Framework;
using UnityEngine;

namespace BossFightTests.EditMode
{
    public class LIDARSystemTests
    {
        private GameObject testObject;
        private LIDARSystem lidarSystem;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestLIDAR");
            lidarSystem = testObject.AddComponent<LIDARSystem>();
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
        public void LIDARSystem_GetRayDistances_ReturnsCorrectCount()
        {
            // Act
            float[] distances = lidarSystem.GetRayDistances();

            // Assert
            Assert.AreEqual(30, distances.Length, "Should have 30 rays");
        }
    }
}
```

## Resources

- [Unity Test Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Documentation](https://docs.nunit.org/)
- [Unity Testing Best Practices](https://docs.unity3d.com/Manual/testing-editortestsrunner.html)

