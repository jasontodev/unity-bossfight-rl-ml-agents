# Heuristic Testing Guide

## Setup

1. **Create the Heuristic Test Scene:**
   - In Unity Editor, go to: **Heuristic Tests > Setup Heuristic Test Scene**
   - This creates:
     - Arena (20x20, 2x bigger than original)
     - Boss in center, facing -Z (toward party)
     - 4 Party Members in formation, facing +Z (toward boss)
     - 3 Strategic walls around boss
     - EpisodeManager for episode reset functionality
     - ManualControlManager for switching between agents
     - ThreatSystem for threat tracking

2. **Press Play** to start testing

## Controls

### Agent Selection
- **Keys 1-4**: Select Party Member 1-4
- **Key 5**: Select Boss

### Movement (WASD)
- **W**: Move forward
- **S**: Move backward
- **A**: Rotate left
- **D**: Rotate right

### Actions
- **Space**: Attack (if in range)
- **E**: Heal (Healer class only, if in range)
- **Q**: Threat Boost (Tank class only)

### Class Selection (Party Members Only)
- **U**: Select Tank class (blue)
- **I**: Select Healer class (green)
- **O**: Select RangedDPS class (yellow)
- **P**: Select MeleeDPS class (red)

**Note:** Class selection can only be done ONCE per episode. Once selected, the class is locked for that episode.

### LIDAR Visualization Toggles
- **\**: Toggle yellow LIDAR rays (general detection)
- **[**: Toggle orange LIDAR ray (attack range)
- **]**: Toggle green LIDAR ray (heal range)

## Robust Test Scenarios

### 1. Basic Movement and Positioning
**Objective:** Verify agents can move and position correctly

**Steps:**
1. Select Party Member 1 (Key 1)
2. Move forward (W) toward boss
3. Rotate left/right (A/D) to face different directions
4. Move backward (S) to retreat
5. Switch to Boss (Key 5) and repeat movement tests

**Expected Results:**
- Agents move smoothly in all directions
- Rotation works correctly
- No clipping through arena floor
- Agents maintain proper height above ground

---

### 2. Class Selection and Locking
**Objective:** Verify class selection works and locks correctly

**Steps:**
1. Select Party Member 1 (Key 1)
2. Press U to select Tank class
3. Verify agent turns blue
4. Try pressing I, O, or P - class should NOT change
5. Select Party Member 2 (Key 2)
6. Press I to select Healer class
7. Verify agent turns green
8. Let episode end (boss dies or all party die)
9. After episode reset, verify classes are reset to None (white)

**Expected Results:**
- Class selection works with U/I/O/P keys
- Class locks after first selection
- Agent color changes to match class
- Classes reset to None after episode reset

---

### 3. Attack System
**Objective:** Test attack mechanics for all classes

**Steps:**
1. Select Party Member 1 (Key 1)
2. Select Tank class (U)
3. Move close to boss (within attack range - orange LIDAR ray should show)
4. Press Space to attack
5. Verify boss takes damage (check health bar)
6. Wait for attack cooldown
7. Attack again
8. Repeat with other classes:
   - MeleeDPS (P) - should have same range as Tank
   - RangedDPS (O) - should have longer range (2x)
   - Healer (I) - should have same range as Tank

**Expected Results:**
- Attacks only work when in range
- Attack cooldown prevents spam
- Damage values differ by class:
  - Tank: 10 damage
  - MeleeDPS: 15 damage
  - RangedDPS: 12 damage (but longer range)
  - Healer: 8 damage
- Boss health decreases correctly

---

### 4. Healing System (Healer Only)
**Objective:** Test healing mechanics

**Steps:**
1. Select Party Member 1 (Key 1)
2. Select Healer class (I)
3. Select Party Member 2 (Key 2)
4. Select Tank class (U)
5. Switch back to Party Member 1 (Healer)
6. Move close to Party Member 2 (within heal range - green LIDAR ray should show)
7. Press E to heal
8. Verify Party Member 2's health increases
9. Wait for heal cooldown
10. Heal again

**Expected Results:**
- Healing only works when in range
- Healing only works for Healer class
- Heal cooldown prevents spam
- Target's health increases correctly
- Healer generates threat when healing

---

### 5. Threat Boost (Tank Only)
**Objective:** Test threat generation for Tank

**Steps:**
1. Select Party Member 1 (Key 1)
2. Select Tank class (U)
3. Move close to boss
4. Press Q to use Threat Boost
5. Check threat display (bottom left of screen)
6. Verify Tank has highest threat
7. Attack boss (Space)
8. Verify threat increases further

**Expected Results:**
- Threat Boost only works for Tank class
- Threat Boost generates significant threat (5x DPS attack threat)
- Threat display shows correct values
- Boss should target highest threat agent

---

### 6. Tank Damage Reduction
**Objective:** Verify Tank takes 40% less damage

**Steps:**
1. Select Party Member 1 (Key 1)
2. Select Tank class (U)
3. Select Boss (Key 5)
4. Move boss close to Party Member 1
5. Attack Party Member 1 with boss (Space)
6. Note the damage taken
7. Select Party Member 2 (Key 2)
8. Select MeleeDPS class (P)
9. Attack Party Member 2 with boss
10. Compare damage values

**Expected Results:**
- Tank takes 40% less damage than other classes
- Damage reduction applies to all incoming damage
- Health bars reflect correct damage amounts

---

### 7. Episode Reset and Respawn
**Objective:** Test episode reset functionality

**Steps:**
1. Play until boss dies OR all party members die
2. Observe episode reset
3. Verify:
   - All agents return to spawn positions
   - All health is reset to max
   - All classes reset to None (white)
   - Threat is cleared
   - Walls return to original positions (if moved)

**Expected Results:**
- Episode resets automatically when win condition is met
- All agents respawn at correct positions
- All state is properly reset
- No agents stuck in death state
- No burning/void effects on respawn

---

### 8. Lava and Void Hazards
**Objective:** Test environmental hazards

**Steps:**
1. Select any agent
2. Move agent onto red lava area
3. Verify agent takes lava damage over time
4. Move agent off lava
5. Verify agent starts burning (takes burn damage)
6. Move agent to edge of arena
7. Fall into void (black area)
8. Verify agent dies instantly

**Expected Results:**
- Lava deals damage while standing on it
- Burning effect triggers after leaving lava
- Void kills instantly
- Agents don't burn/void kill immediately after respawn (grace period)

---

### 9. Wall Interaction (Boss Only)
**Objective:** Test boss wall pickup and placement

**Steps:**
1. Select Boss (Key 5)
2. Move boss close to a wall
3. Press wall pickup key (check BossController for key binding)
4. Verify wall is picked up
5. Move boss to different location
6. Press wall place key
7. Verify wall is placed at new location

**Expected Results:**
- Boss can pick up walls
- Boss can place walls at new locations
- Walls can be used strategically
- Walls reset to original positions on episode reset

---

### 10. Multi-Agent Coordination
**Objective:** Test coordination between multiple agents

**Steps:**
1. Select Party Member 1 (Key 1), choose Tank (U)
2. Use Threat Boost (Q) to generate threat
3. Select Party Member 2 (Key 2), choose Healer (I)
4. Heal Party Member 1 (E)
5. Select Party Member 3 (Key 3), choose RangedDPS (O)
6. Attack boss from range (Space)
7. Select Party Member 4 (Key 4), choose MeleeDPS (P)
8. Attack boss from melee range (Space)
9. Monitor threat values and boss targeting

**Expected Results:**
- Multiple agents can work together
- Threat system tracks all agents correctly
- Boss targets highest threat agent
- Different classes complement each other
- No conflicts between agent actions

---

### 11. LIDAR System Visualization
**Objective:** Test LIDAR ray visualization

**Steps:**
1. Select any agent
2. Press \ to toggle yellow LIDAR rays
3. Press [ to toggle orange attack range ray
4. Press ] to toggle green heal range ray
5. Move agent around and observe ray behavior
6. Verify rays show correct ranges for different classes

**Expected Results:**
- LIDAR rays toggle correctly
- Rays show accurate detection ranges
- Attack range (orange) shows correct distance
- Heal range (green) shows correct distance
- Rays update position when agent moves

---

### 12. Death and Despawn
**Objective:** Test death mechanics

**Steps:**
1. Select any party member
2. Take damage until health reaches 0
3. Verify agent despawns (becomes invisible)
4. Verify agent cannot be selected or controlled
5. Verify other agents cannot interact with dead agent
6. Wait for episode reset
7. Verify agent respawns correctly

**Expected Results:**
- Agents despawn when health reaches 0
- Dead agents are invisible and non-interactive
- Dead agents cannot be selected
- Dead agents cannot attack, heal, or move
- Agents respawn correctly after episode reset

---

## Test Checklist

Before considering testing complete, verify:

- [ ] All agents can move and rotate correctly
- [ ] Class selection works for all 4 classes
- [ ] Classes lock after selection
- [ ] Classes reset after episode reset
- [ ] Attack system works for all classes
- [ ] Attack ranges are correct (RangedDPS has 2x range)
- [ ] Healing works for Healer class
- [ ] Threat Boost works for Tank class
- [ ] Tank takes 40% less damage
- [ ] Episode reset works correctly
- [ ] Lava and void hazards work
- [ ] Boss can pick up and place walls
- [ ] Multi-agent coordination works
- [ ] LIDAR visualization works
- [ ] Death and despawn work correctly
- [ ] No agents get stuck or glitch
- [ ] No burning/void effects on respawn
- [ ] Threat system tracks correctly
- [ ] Health bars display correctly

## Common Issues to Watch For

1. **Agents spawning inside each other** - Check spawn positions
2. **Agents not facing correct direction** - Boss should face -Z, party should face +Z
3. **Class not changing color** - Check PlayerClassSystem
4. **Attack not working** - Check LIDAR range and cooldown
5. **Episode not resetting** - Check EpisodeManager win conditions
6. **Agents burning on spawn** - Check grace period logic
7. **Threat not updating** - Check ThreatSystem
8. **LIDAR rays not showing** - Check toggle keys and LIDARSystem

## Performance Testing

While testing, also monitor:
- Frame rate (should be stable)
- Memory usage (no leaks)
- Physics performance (no stuttering)
- Agent update performance (all agents update correctly)

---

**Happy Testing!** 🎮

