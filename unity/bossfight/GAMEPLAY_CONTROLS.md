# Boss Fight Game - Controls & Abilities Guide

## General Controls

### Player Movement
- **A / D Keys**: Rotate left/right (180 degrees per second)
- **W Key**: Move forward (relative to facing direction)
- **S Key**: Move backward (relative to facing direction)

### Camera
- **Position**: y = 7, z = -10
- **Rotation**: x = 45 degrees
- Fixed camera view of the arena

---

## Player Classes

### Tank (Blue)
- **Damage**: 2 per attack
- **Special Ability**: 40% damage reduction from all sources
- **Threat Boost**: Press **T** to generate 5x threat (25 threat, 5 second cooldown)
- **Color**: Blue

### Healer (Green)
- **Damage**: 2 per attack
- **Heal Ability**: Press **H** to heal nearby allies for 10 HP (3 second cooldown)
- **Healing Range**: 5 units (must have line of sight - shown by green LIDAR ray)
- **Threat from Healing**: 3x base DPS threat (15 threat per heal)
- **Color**: Green

### RangedDPS (Purple)
- **Damage**: 5 per attack
- **Attack Range**: 6 units (3x normal range)
- **Color**: Purple

### MeleeDPS (Red)
- **Damage**: 10 per attack
- **Attack Range**: 2 units (normal range)
- **Color**: Red

---

## Combat System

### Attack System
- **Attack Key**: **Space**
- **Attack Cooldown**: 1 second
- **Attack Detection**: Must face target (orange LIDAR ray must hit target)
- **Damage**: Based on class (see class descriptions above)

### LIDAR System
- **Total Rays**: 30 rays in a 360-degree circle
- **Ray Range**: 10 units (full visibility)
- **Orange Ray**: Attack range indicator (shows attack range, varies by class)
- **Yellow Ray**: Full range visibility ray (10 units)
- **Green Ray**: Healing range indicator (5 units, only visible for Healers)

---

## Boss Controls

### Boss Movement
- **A / D Keys**: Rotate left/right
- **W Key**: Move forward
- **S Key**: Move backward

### Boss Abilities
- **Attack**: Press **Space** to attack players (100 damage, 1 second cooldown)
- **Attack Range**: 2 units
- **Wall Pickup**: Press **E** to pick up walls (within 2 units)
- **Wall Placement**: Press **Q** to place carried wall

---

## Environmental Hazards

### Lava
- **Damage**: 8 HP per second while touching
- **Burn Effect**: After leaving lava, take 3 HP per second for 5 seconds
- **Visual**: Red plane surrounding the arena

### Void
- **Effect**: Instant death (falls below the arena)
- **Visual**: Black plane below the lava

---

## Threat System

### Threat Generation
- **Damage = Threat**: Each point of damage generates 1 point of threat
  - MeleeDPS: 10 threat per attack
  - RangedDPS: 5 threat per attack
  - Tank: 2 threat per attack
  - Healer: 2 threat per attack

### Special Threat Abilities
- **Healer Healing**: Generates 15 threat (3x base DPS threat) per heal
- **Tank Threat Boost**: Press **T** to generate 25 threat (5x base DPS threat)

### Threat Display
- **Individual Threat**: Shown in top-left corner for each player
- **Threat Leaderboard**: Bottom-left corner shows top 5 players by threat

---

## Arena Features

### Arena Layout
- **Arena Size**: 10x10 units (green floor)
- **Lava Size**: 14x14 units (red, surrounds arena)
- **Void Size**: 16x16 units (black, below lava)

### Walls
- **Count**: 3 walls spawn in strategic positions
- **Size**: 2 units long, 1.5 units high, 0.2 units thick
- **Collider Size**: 1x1x1
- **Boss Interaction**: Boss can pick up and place walls

---

## Health System

### Health Bar
- **Display**: Billboard-style health bar above each character
- **Color**: Green (full health), Red (low health, below 30%)
- **Shrinks**: Right to left as health decreases

### Health Display
- **Player**: Top-left corner shows current/max health
- **Boss**: Top-left corner shows boss health (when boss exists)

---

## Scene Setup Options

### Menu Items (Boss Fight Menu)
1. **Setup Arena (Player & Boss)**: Creates arena with one player and one boss
2. **Setup Arena (Player Only)**: Creates arena with one player only
3. **Setup Arena (Boss Only)**: Creates arena with one boss only
4. **Setup Attack Test Scene**: Creates arena with stationary player and attacking boss
5. **Setup Player vs Boss Scene**: Creates arena with controllable player and stationary boss

---

## Visual Indicators

### Facing Direction
- **Orange LIDAR Ray**: Shows attack range and forward direction
- **Yellow LIDAR Ray**: Shows full visibility range (10 units)
- **Green LIDAR Ray**: Shows healing range (5 units, Healers only)

### Health Status
- **Top-Left Display**: Health, status (Safe/In Lava), burn timer, threat level
- **Top-Right Display**: Attack info, cooldowns, class abilities
- **Bottom-Right Display**: Class-specific abilities (Healer heal, Tank threat boost)

---

## Quick Reference

### Player Controls
| Action | Key | Description |
|--------|-----|-------------|
| Rotate Left | A | Turn left |
| Rotate Right | D | Turn right |
| Move Forward | W | Move in facing direction |
| Move Backward | S | Move opposite facing direction |
| Attack | Space | Attack target in front |
| Heal (Healer) | H | Heal nearby allies (3s cooldown) |
| Threat Boost (Tank) | T | Generate threat boost (5s cooldown) |

### Boss Controls
| Action | Key | Description |
|--------|-----|-------------|
| Rotate Left | A | Turn left |
| Rotate Right | D | Turn right |
| Move Forward | W | Move in facing direction |
| Move Backward | S | Move opposite facing direction |
| Attack | Space | Attack players (1s cooldown) |
| Pick Up Wall | E | Pick up nearby wall |
| Place Wall | Q | Place carried wall |

### Class Damage Values
| Class | Damage | Attack Range | Special Ability |
|-------|--------|--------------|----------------|
| Tank | 2 | 2 units | 40% damage reduction, Threat boost (T) |
| Healer | 2 | 2 units | Heal allies (H), 3x threat from healing |
| RangedDPS | 5 | 6 units | Triple attack range |
| MeleeDPS | 10 | 2 units | Highest damage |

---

## Tips & Strategies

1. **Tank**: Use threat boost (T) to maintain aggro and protect teammates
2. **Healer**: Position yourself to have line of sight on allies (green ray shows range)
3. **RangedDPS**: Use your extended range to attack from safety
4. **MeleeDPS**: Highest damage but requires close range - watch for lava
5. **Boss**: Use walls strategically to block player movement or create cover
6. **Threat Management**: Tanks should maintain highest threat to keep boss focused on them
7. **Lava Awareness**: Avoid lava - the burn effect continues even after leaving

---

*Last Updated: Based on current game implementation*