# Project Overview 
- **Game Title**: WhiteDragon
- **High-Level Concept**: First-person dark-fantasy roguelite inspired by the design philosophy of The Binding of Isaac, featuring rock-throwing combat, deep item synergies, and room-locked encounters in a gritty Berserk / Dark Souls style setting.
- **Players**: Single player
- **Inspiration / Reference Games**: The Binding of Isaac, Dark Souls, Berserk, Crimson Moon
- **Tone / Art Direction**: Gritty dark fantasy, minimalist/lo-fi procedural, high-contrast dark tones, fast-loading, clean readability.
- **Target Platform**: PC (StandaloneWindows64), keyboard and mouse.
- **Screen Orientation / Resolution**: Landscape 1920x1080.
- **Render Pipeline**: Built-in baseline (transitioning to URP).

# Game Mechanics 
## Core Gameplay Loop
The player enters dungeon rooms, doors seal, enemies spawn, and the player dodges attacks while retaliating with rock throws. Clearing all enemies unseals doors and grants rewards (health pickups, soul/dark hearts, or item pedestals). Player health management is critical: taking damage consumes overlay hearts first, then red containers. Dark hearts burst into an offensive explosion when broken. Losing all red health and overlay hearts results in run defeat.

## Health System Rules (Isaac-style, Half-Heart Units)
- **Red Containers**: Permanent health slots. Tracked as `RedContainers` and `RedCurrent`. Can be healed up to max containers with red heart pickups.
- **Soul Hearts (Fellowship)**: Temporary overlay hearts added above containers. Absorbed first on damage. Cannot be replenished with red heart healing.
- **Dark Hearts (Corruption)**: Temporary overlay hearts with a reactive burst. When a Dark heart point is depleted, it raises `OverlayBroke(HeartType.Dark)`, which triggers an offensive dark burst dealing damage to nearby enemies.
- **Damage Order & Invulnerability**:
  - Overlay hearts absorb damage first (last-in, first-out).
  - Remaining damage penetrates to Red containers.
  - Upon taking damage, the player gains temporary i-frames (e.g. 1.0 second), flashing visually and ignoring further incoming damage during this window.
  - Game feel response: camera trauma shake, hurt sound, and directional damage indicator/vignette.
- **Death**:
  - Occurs when `RedCurrent <= 0` and `Overlay.Count == 0`.
  - Disables player controls, plays death feedback, and presents restart/respawn options.

## Controls and Input Methods
- **WASD**: First-person movement
- **Mouse**: Look and aim
- **Left Click**: Throw rock projectile
- **Space**: Jump
- **F1**: Toggle Debug Panel (inspect stats, shot recipes, items, and health debug actions)
- **Escape**: Toggle cursor lock

# UI
## Heart HUD Layout & Visual Design
- **Position**: Top-left corner of the screen (`x: 24px, y: 24px`), offset from screen edges to avoid bezel clipping.
- **Hierarchy & Wrapping**:
  - Rendered in rows of 6 full hearts (12 half-hearts) per line.
  - Red containers rendered first from left to right.
  - Overlay hearts (Soul and Dark) append immediately after the red heart containers.
- **Icon Representation**:
  - Each full heart consists of two halves: Left half (half-heart 1) and Right half (half-heart 2).
  - Procedurally generated crisp 16x16 / 24x24 heart sprites/textures with distinct silhouettes and palettes:
    - **Empty Container**: Dark metallic border outline (`#2C272E`), hollow interior.
    - **Red Heart**: Deep blood crimson (`#C21818`), brighter inner highlight (`#FF4D4D`).
    - **Soul Heart**: Ethereal sky-blue / silver (`#4FA8D8`), cold highlight (`#A5E3FF`).
    - **Dark Heart**: Abyssal obsidian / black-purple (`#1E1622`), glowing dark violet rim (`#7B2CBF`).
- **HUD Animations & Micro-Interactions**:
  - **Damage Bounce**: Hit heart pulses/scales up briefly (1.2x) on change.
  - **Low Health Warning**: Gentle rhythmic heartbeat pulse when at 1 Red Heart or less with 0 overlay hearts.
  - **Damage Flash Vignette**: Subtle red screen fringe flash when taking damage.

## Debug Panel Additions (F1)
- Under the Held Items / Stats panel, display:
  - `Health: X/Y Red Hearts, Z Overlay (Soul: A, Dark: B)`
  - Quick action buttons: `Damage 1 Half-Heart`, `Damage 1 Full Heart`, `Heal 1 Heart`, `+1 Container`, `+1 Soul Heart`, `+1 Dark Heart`, `Kill Player`, `Respawn/Refill`.

# Key Asset & Context
### Existing Assets & Code Reused:
- `Assets/_Project/Scripts/Combat/Health.cs`:
  - `HeartType` enum (`Red, Soul, Dark`).
  - `HealthState` pure C# class with container math, `Damage`, `Heal`, `AddContainer`, `AddOverlay`, `Changed`, `OverlayBroke`.
- `Assets/_Project/Scripts/Combat/IDamageable.cs`:
  - `void TakeDamage(float amount, Vector3 hitPoint);`
- `Assets/_Project/Scripts/Core/GameFeel.cs`:
  - Camera trauma, screen shake, audio synthesis, hit stop.
- `Assets/_Project/Scripts/Debugging/DebugPanel.cs`:
  - F1 in-game debug interface.

### New Assets & Scripts To Create:
1. `Assets/_Project/Scripts/Player/PlayerHealth.cs`:
   - Component attached to the Player GameObject.
   - Implements `IDamageable`.
   - Wraps and owns `HealthState`.
   - Manages i-frames, damage routing, Dark heart burst event reaction, and death handling.
2. `Assets/_Project/Scripts/UI/HeartHUD.cs`:
   - HUD component rendering Isaac-style heart slots with procedural textures.
   - Observes `HealthState.Changed` and `HealthState.OverlayBroke`.
   - Renders red, soul, and dark hearts with damage animation and low-health pulsation.
3. `Assets/_Project/Scripts/Combat/DamageHazard.cs`:
   - Simple test hazard volume / trigger in `TestRoom` (e.g. spike trap or damage zone) to verify real-time physical damage interactions without relying solely on debug buttons.

# Implementation Steps

### Step 1: Implement `PlayerHealth.cs` Component
- **Description**: Create `Assets/_Project/Scripts/Player/PlayerHealth.cs` in assembly `WD.Player`.
  - Holds `public HealthState Health { get; private set; } = new HealthState();`
  - Inspector properties: `startingContainers` (default 6 = 3 full hearts), `invulnerabilityDuration` (default 1.0s), `darkBurstRadius` (default 6.0m), `darkBurstDamage` (default 10f).
  - In `Awake()`, initialize `Health.AddContainer(startingContainers)`.
  - Subscribe to `Health.OverlayBroke`: if `HeartType.Dark`, trigger area dark burst damaging all `IDamageable` targets within `darkBurstRadius` (using `Physics.OverlapSphere`) and call `GameFeel.Kill(transform.position)`.
  - Implement `IDamageable.TakeDamage(float amount, Vector3 hitPoint)`:
    - Check i-frame timer; if invulnerable, return.
    - Set `iFrameTimer = invulnerabilityDuration`.
    - Apply `Health.Damage(Mathf.Max(1, Mathf.RoundToInt(amount)))`.
    - Trigger `GameFeel.Hit(hitPoint)`.
    - Check `Health.IsDead`: if true, disable `PlayerController`, play death sound/shake, and invoke death events.
  - Provide helper API: `Heal(int halfHearts)`, `AddContainers(int halfHearts)`, `AddOverlay(HeartType type, int halfHearts)`, `Revive()`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes (with Step 2)

### Step 2: Implement `HeartHUD.cs` Component
- **Description**: Create `Assets/_Project/Scripts/UI/HeartHUD.cs` in assembly `WD.UI`.
  - Procedurally generate 24x24 pixel heart textures for Empty Container, Full Red, Half Red, Full Soul, Half Soul, Full Dark, and Half Dark using a stylized heart bitmask.
  - Find or bind `PlayerHealth` reference in `Start()`.
  - Subscribe to `PlayerHealth.Health.Changed` to trigger a heart bounce pulse (`scaleTimer`).
  - In `OnGUI()` (or Canvas equivalent):
    - Compute layout starting at top-left `(24, 24)`.
    - Render red containers first: for each container pair (2 half-hearts), render empty container outline, then draw half or full red heart fill.
    - Render overlay hearts next: group into pairs of Soul or Dark hearts, drawing appropriate full or half icons.
    - If `Health.RedCurrent <= 2` and `Health.Overlay.Count == 0`, apply a subtle sine-wave pulsing scale to simulate a low-health heartbeat.
    - Draw a subtle red screen-border vignette during active i-frames.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes (with Step 1)

### Step 3: Implement `DamageHazard.cs` for Physical Room Interaction
- **Description**: Create `Assets/_Project/Scripts/Combat/DamageHazard.cs` in assembly `WD.Combat` (or `WD.Enemies`).
  - A trigger volume component that periodically deals 1 half-heart damage to any `IDamageable` entering or staying inside its collider (with configurable tick rate, e.g. 1 damage per 1.0s).
  - Allows natural testing of player movement into danger zones alongside the rock-throwing loop.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 4: Extend `DebugPanel.cs` with Health Controls
- **Description**: Update `Assets/_Project/Scripts/Debugging/DebugPanel.cs` to query `PlayerHealth`.
  - Add GUI section displaying current Red Containers, Red Current, and count of Soul and Dark overlay hearts.
  - Add interactive debug buttons:
    - "Damage 1/2 Heart"
    - "Damage 1 Full Heart"
    - "Heal 1 Heart"
    - "+1 Red Container"
    - "+1 Soul Heart"
    - "+1 Dark Heart"
    - "Kill Player" / "Full Restore"
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

### Step 5: Scene Wiring in `TestRoom.unity`
- **Description**: Wire the new components in `Assets/_Project/Scenes/TestRoom.unity`:
  - Attach `PlayerHealth` to the `Player` GameObject.
  - Attach `HeartHUD` to the `DebugTools` or `Main Camera` GameObject.
  - Place a test `DamageHazard` object (e.g. a red-tinted floor pad or spike placeholder) in `TestRoom` to demonstrate physical collision damage.
- **Assigned role**: developer
- **Dependencies**: Steps 1, 2, 3, 4
- **Parallelizable**: No

### Step 6: Testing & Verification
- **Description**: Verify all EditMode tests continue to pass (all 14 existing tests plus any new ones). Enter PlayMode in `TestRoom` and verify:
  - Red hearts appear in the top-left HUD.
  - Damaging the player reduces hearts, triggers i-frames and camera shake, and prevents multi-frame damage spam.
  - Soul and Dark overlay hearts appear to the right of red containers.
  - Overlay hearts absorb damage prior to red containers.
  - Dark heart break triggers the dark burst explosion and damages nearby dummies.
  - Depleting all health triggers death state and disables movement.
  - F1 Debug panel buttons function cleanly.
- **Assigned role**: developer
- **Dependencies**: Step 5
- **Parallelizable**: No

# Verification & Testing
1. **Existing Test Suite**: Run all 14 EditMode unit tests in `WD.Tests.EditMode` to confirm zero regressions in `StatBlockTests`, `HealthStateTests`, and `ItemLoadoutTests`.
2. **Heart HUD Visual Inspection**:
   - Verify empty containers, half-red, full-red, soul, and dark heart textures render crisply at 1080p without blur or artifacting.
   - Verify heart wrapping across lines when exceeding 6 containers.
3. **i-Frame Validation**:
   - Stand inside `DamageHazard` trigger; confirm player is only damaged once per i-frame window rather than every physics tick.
4. **Dark Heart Burst**:
   - Add a Dark Heart via debug panel, stand next to `Dummy1`, take damage; confirm `Dummy1` takes damage from the Dark heart burst.
5. **Death State**:
   - Reduce player health to 0; verify movement is halted and death feedback triggers.
