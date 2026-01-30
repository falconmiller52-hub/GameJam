# CLAUDE.md — AI Assistant Guide for GameJam

## Project Overview

This is a **Unity 2D top-down action game** built for a game jam. The player fights through waves of enemies while a boss monster consumes the defeated foes. The game features three scenes: a main menu, the core gameplay level, and a death/dialogue sequence.

**Engine:** Unity (C#)
**Input System:** Unity New Input System (`UnityEngine.InputSystem`)
**UI Framework:** Unity Canvas + TextMeshPro
**Physics:** 2D (Rigidbody2D, Collider2D)
**Language in Comments:** Russian (identifiers are in English)

---

## Repository Structure

```
GameJam/
├── Code/                    # All C# scripts (22 files, ~2,268 lines)
│   ├── Gameplay/            # Core game mechanics (11 scripts)
│   │   ├── PlayerMovement.cs      # WASD movement, Space dash, sprite flip
│   │   ├── PlayerHealth.cs        # 10 HP, auto-regen, death sequence
│   │   ├── PlayerAttack.cs        # Mouse-click attacks with cooldown
│   │   ├── DynamicCamera.cs       # Smooth follow camera with mouse bias
│   │   ├── WaveSpawner.cs         # Main game orchestrator, enemy wave spawning
│   │   ├── EnemyAI.cs             # Simple chase-player behavior
│   │   ├── EnemyHealth.cs         # Enemy damage, death, fly-to-monster
│   │   ├── EnemyDamage.cs         # Collision-based damage to player
│   │   ├── SwordDamage.cs         # Attack hit detection & knockback
│   │   ├── KatanaAim.cs           # Weapon rotation toward mouse cursor
│   │   └── MonsterEater.cs        # Singleton boss that eats dead enemies
│   └── UI/                  # User interface scripts (11 scripts)
│       ├── Menu.cs                # Main menu play/quit buttons
│       ├── Pause.cs               # ESC pause menu, settings, music toggle
│       ├── Reticle.cs             # Custom mouse cursor positioning
│       ├── FloatingHP.cs          # Player HP indicator
│       ├── PlayerTutorial.cs      # First-dash tutorial tooltip
│       ├── PreIntroSequence.cs    # Pre-dialogue click sequence
│       ├── IntroDialogue.cs       # Boss dialogue, monster animation
│       ├── DeathDialogue.cs       # Death scene dialogue manager
│       ├── Gameover.cs            # Game over cinematic sequence
│       ├── GameoverClick.cs       # Game over menu buttons
│       └── AnimationEndListener.cs # Animation completion events
├── Art/                     # Sprites, models, animations (~24MB)
│   ├── Models/              # Character sprite sheets & animations
│   │   ├── Player/          # Idle, Run, Dash, Dead animations
│   │   ├── Enemy1/          # Mushroom enemy (run, attack, death)
│   │   ├── Enemy2/          # Fly enemy (fly, dash, death)
│   │   ├── Enemy3/          # Large enemy (walk, jump, death)
│   │   ├── Monster/         # Boss entity animations
│   │   └── Sword/           # Katana aim/idle animations
│   └── Environment/         # Background/map transition animations
├── OST/                     # Audio assets (~18MB)
│   ├── Music/               # Background tracks (menu, gameplay, game over)
│   └── SFX/                 # Sound effects (dash, katana, punch, eat, etc.)
├── Scenes/                  # Unity scene files
│   ├── MainMenu.unity       # Build Index 0 — entry point
│   ├── Level1.unity         # Build Index 1 — main gameplay
│   └── DeathDialogue.unity  # Build Index 2 — death/boss dialogue
├── Prefabs/                 # Enemy prefab GameObjects (3 prefabs)
├── Fonts/                   # Pixel art fonts (VyazPixel, bold_pixels)
├── icon.png                 # Game icon
├── .gitignore               # Standard Unity .gitignore
└── CLAUDE.md                # This file
```

---

## Game Flow

```
MainMenu (Scene 0)
    │
    ▼
Level1 (Scene 1)
    ├── Wave 1: Enemy spawns
    ├── Wave 2: More enemies + map transition
    ├── Wave 3: Final wave + map transition
    │       │
    │       ▼ (Player dies)
    │   DeathDialogue (Scene 2)
    │       ├── PreIntroSequence (click events)
    │       ├── IntroDialogue (boss speaks)
    │       ├── Gameover (cinematic)
    │       └── GameoverClick → Back to MainMenu
    │
    └── Monster eats dead enemies throughout
```

---

## Key Architecture Patterns

### Singleton
- `MonsterEater.Instance` — the boss entity is a singleton accessed globally for enemy death fly-to behavior.

### Coroutine-Heavy
- Nearly all timed sequences (waves, transitions, animations, dialogues) use `IEnumerator` coroutines with `StartCoroutine()`.
- Both `Time.timeScale`-dependent and unscaled (`WaitForSecondsRealtime`) timing are used.

### Component-Based
- Standard Unity component model. Each behavior is a separate `MonoBehaviour` script.
- Heavy use of `GetComponent<>()` and `[SerializeField]` for inspector wiring.

### Collision-Driven
- Game logic is largely driven by `OnTriggerEnter2D` and `OnCollisionEnter2D` callbacks.
- Tags used: `"Player"`, `"Sword"`, `"Enemy"`, `"Monster"`.

### Audio Management
- Sound effects use randomized pitch variation (0.9x–1.1x) for variety.
- Audio sources are wired via the Inspector with `[Header]` groups.

---

## Code Conventions

### Naming
| Element         | Convention    | Example                          |
|-----------------|---------------|----------------------------------|
| Classes         | PascalCase    | `PlayerMovement`, `EnemyHealth`  |
| Public fields   | camelCase     | `moveSpeed`, `dashDuration`      |
| Private fields  | camelCase     | `isDashing`, `moveInput`         |
| Methods         | PascalCase    | `TakeDamage()`, `SpawnWave()`    |
| Coroutines      | PascalCase    | `DashCooldown()`, `DeathSequence()` |

### Inspector Organization
- Use `[Header("Section Name")]` to group public fields in the Unity Inspector.
- Use `[Range(min, max)]` for numeric sliders.
- Use `[TextArea]` for multi-line string fields (dialogue text).
- Use `[SerializeField]` to expose private fields to the Inspector.

### Comments
- Code comments are written in **Russian**.
- Important notes are marked with the fire emoji: `// 🔥 ДОБАВЛЕНО` (Added), `// 🔥 ВАЖНО` (Important).
- When adding comments, follow the existing Russian-language convention or use English if context is unclear.

### File Organization
- **One class per file**, file named after the class.
- Gameplay scripts go in `Code/Gameplay/`.
- UI/menu scripts go in `Code/UI/`.

---

## Scene Build Indices

| Index | Scene              | Purpose                         |
|-------|--------------------|---------------------------------|
| 0     | MainMenu.unity     | Main menu, entry point          |
| 1     | Level1.unity       | Core gameplay (waves + boss)    |
| 2     | DeathDialogue.unity| Player death dialogue + game over |

Scene transitions use `SceneManager.LoadScene(index)`.

---

## Key Gameplay Parameters

| Parameter           | Value      | Location                  |
|---------------------|------------|---------------------------|
| Player Max HP       | 10         | `PlayerHealth.cs`         |
| HP Regen Amount     | 1 per tick | `PlayerHealth.cs`         |
| HP Regen Cooldown   | ~4 seconds | `PlayerHealth.cs`         |
| Dash Duration       | Inspector  | `PlayerMovement.cs`       |
| Attack Cooldown     | Inspector  | `PlayerAttack.cs`         |
| Enemy Count/Wave    | Inspector  | `WaveSpawner.cs`          |

Most gameplay values are exposed as `[SerializeField]` fields and tuned in the Unity Inspector, not hardcoded.

---

## Development Guidelines

### Modifying Scripts
1. Read the existing file before making changes.
2. Preserve the existing code style (camelCase fields, PascalCase methods, Russian comments).
3. Keep the `Code/Gameplay/` vs `Code/UI/` separation. Put new gameplay scripts in `Gameplay/`, new UI scripts in `UI/`.
4. Use coroutines for any timed or sequenced behavior.
5. Wire dependencies through `[SerializeField]` Inspector fields rather than `Find()` or hardcoded references.

### Adding New Enemies
1. Create sprite assets in `Art/Models/<EnemyName>/`.
2. Create a prefab in `Prefabs/`.
3. The prefab needs: `EnemyAI`, `EnemyHealth`, `EnemyDamage` components, a `Rigidbody2D`, and a `Collider2D`.
4. Tag the prefab as `"Enemy"`.
5. Add the prefab to `WaveSpawner`'s spawn list in the Inspector.

### Adding New Scenes
1. Create the scene file in `Scenes/`.
2. Add it to Build Settings with the next available index.
3. Use `SceneManager.LoadScene(index)` for transitions.

### Audio
- Place music in `OST/Music/`, sound effects in `OST/SFX/`.
- Use `AudioSource` components with `[SerializeField]` references.
- Apply randomized pitch (`Random.Range(0.9f, 1.1f)`) for repeated SFX to avoid repetition.

---

## Build & Run

This is a Unity project. To build or run:

1. Open the project in **Unity Editor** (compatible version required).
2. Ensure scenes are added to **File > Build Settings** in order: MainMenu (0), Level1 (1), DeathDialogue (2).
3. Use **File > Build and Run** or press Play in the Editor.

There is no CLI build pipeline, CI/CD, or automated test suite configured.

### Debug Shortcuts
- Press **T** in gameplay to instantly kill the player (debug feature in `PlayerHealth.cs`).

---

## Git Workflow

- **No formal branching strategy** — development uses direct commits and hotfixes.
- **Commit messages** have been informal (`"upd"`, `"hotfix"`, `"Add files via upload"`).
- **No CI/CD** or pre-commit hooks are configured.
- The `.gitignore` follows the standard Unity template (excludes Library/, Temp/, Build/, IDE files, etc.).

### When committing:
- Write descriptive commit messages summarizing what changed.
- Do not commit Unity auto-generated directories (`Library/`, `Temp/`, `Obj/`, `Logs/`).
- Binary assets (sprites, audio) should be committed alongside their `.meta` files.
- Never delete a `.meta` file without deleting its corresponding asset (and vice versa). Unity uses `.meta` files for GUID references.

---

## Dependencies

This project uses only built-in Unity packages:
- `UnityEngine` (core)
- `UnityEngine.UI` (Canvas UI)
- `UnityEngine.SceneManagement` (scene loading)
- `UnityEngine.InputSystem` (New Input System)
- `TMPro` (TextMeshPro for text rendering)

No external package manager (npm, NuGet) or third-party libraries are used.

---

## Important Notes for AI Assistants

1. **Unity Inspector values matter.** Many gameplay parameters are set in the Unity Inspector, not in code. When reading code, `[SerializeField]` fields with no default may have values assigned in scene/prefab files.
2. **Russian comments.** The codebase uses Russian for comments. Preserve this convention or use English when intent is ambiguous.
3. **No tests exist.** There is no automated test suite. Validate changes by reasoning about correctness and checking for compilation errors.
4. **Meta files.** Every asset in Unity has a corresponding `.meta` file. If creating or deleting assets, the `.meta` file must be handled accordingly.
5. **Scene references are by index.** Scene transitions use build index numbers (0, 1, 2), not scene names.
6. **Coroutine lifecycle.** Many game sequences depend on coroutine timing. Be careful when modifying coroutine yields as they affect gameplay pacing.
7. **Singleton access.** `MonsterEater.Instance` is the only singleton. Other cross-object communication uses Inspector references or `GetComponent`.
