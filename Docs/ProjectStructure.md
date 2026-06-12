# Project Structure

This document explains the intended directory layout under `Assets/Project`.

`Assets/Project` is the root folder for game-specific files. Unity-generated template folders such as `Assets/Settings` and the default `Assets/Scenes` folder may still exist, but new MP game code, data, prefabs, scenes, and resources should live under `Assets/Project`.

## Directory Tree

```text
Assets/
  Project/
    Scripts/                  C# code
      Core/                   Shared foundations not tied to a specific game feature
        Events/
        StateMachine/
        Utilities/
      Gameplay/               Runtime gameplay logic used during combat
        Entity/
        Movement/
        Combat/
        Damage/
        StatusEffects/
      Progression/            Character growth and progression systems
        Jobs/
        SkillTree/
        Level/
      Items/
      Stages/
      Tower/
      Network/
      UI/
      Optimization/           Systems for large-scale late-game combat
        SpatialHash/
        Pooling/

    Data/                     ScriptableObject asset instances
      Jobs/
      Skills/
      Items/
      Enemies/
      Bosses/
      Stages/
      Elements/
      RewardTables/

    Prefabs/                  GameObject templates for scenes and runtime spawning
      Players/
      Enemies/
      Bosses/
      Projectiles/
      Tower/
      UI/
      VFX/

    Scenes/                   Game and test scenes
      Main/
      Test/

    Art/                      Visual source assets and 2D game art
      Sprites/
      Animations/
      Tiles/

    Materials/                Unity Material assets
      Characters/
      Environment/
      VFX/
      UI/

    VFX/                      Visual effects assets
      Particles/
      HitEffects/
      Projectiles/
      BossPatterns/

    Audio/                    Audio assets
      BGM/
      SFX/
      UI/

    UI/                       UI-only resources
      Sprites/
      Fonts/
      Layouts/
      Themes/
```

## Top-Level Folders

### `Scripts`

Contains C# source code. In Unreal terms, this is the closest equivalent to part of a `Source` folder, although Unity keeps scripts inside `Assets`.

Use this folder for gameplay rules, runtime systems, entity components, UI behavior scripts, networking bridges, and shared utilities.

### `Data`

Contains ScriptableObject instances. In Unreal terms, these are similar to Data Assets or Data Tables.

Use this folder for content definitions such as jobs, skills, items, enemies, bosses, stages, elements, and reward tables.

Code defines the data type in `Scripts`; actual tuned content lives here as `.asset` files.

Example:

```text
Scripts/Progression/Jobs/JobDefinition.cs
Data/Jobs/Warrior.asset
```

### `Prefabs`

Contains reusable GameObject templates. In Unreal terms, Prefabs are similar to Blueprint Actor assets used as spawnable or placeable templates.

Use this folder for players, enemies, bosses, visual projectiles, tower objects, UI prefabs, and reusable VFX prefabs.

### `Scenes`

Contains Unity scenes. In Unreal terms, these are closest to maps.

Use `Scenes/Main` for actual playable scenes and `Scenes/Test` for focused test scenes such as combat tests, stage flow tests, or UI tests.

### `Art`

Contains visual game art such as sprites, animation clips, and tile assets.

Character, enemy, object, and environment animation assets should generally live here unless they are specifically VFX-related.

### `Materials`

Contains Unity Material assets.

Keep materials grouped by their main usage: characters, environment, VFX, or UI.

### `VFX`

Contains visual effect resources such as particles, hit effects, projectile effects, and boss pattern effects.

This folder is for presentation assets. Gameplay damage, targeting, and collision rules should stay in `Scripts/Gameplay`.

### `Audio`

Contains sound assets.

Use `BGM` for music, `SFX` for gameplay sound effects, and `UI` for interface sounds.

### `UI`

Contains UI-only resources such as UI sprites, fonts, layouts, and themes.

UI behavior code should go in `Scripts/UI`; UI visual resources should go here.

## Script Folder Guidelines

### `Scripts/Core`

Shared foundation code that is not specific to combat, jobs, items, stages, or networking.

Typical contents:

```text
Events        Shared event channels and event payloads
StateMachine  Generic state machine or phase interfaces
Utilities     Small reusable helpers
```

### `Scripts/Gameplay`

Combat-time gameplay logic.

Typical contents:

```text
Entity         PlayerEntity, EnemyEntity, TowerEntity, shared entity state
Movement       Movement components and movement-related systems
Combat         Attack requests, attack scheduling, attack simulation types
Damage         DamageRequest, DamageResult, DamageSystem
StatusEffects  Poison, burn, slow, freeze, shock, and related systems
```

### `Scripts/Progression`

Character growth systems.

Typical contents:

```text
Jobs       JobDefinition, SubJobDefinition, JobComponent
SkillTree  SkillTreeDefinition, SkillNodeDefinition, SkillTreeState
Level      Level or experience systems if needed
```

### `Scripts/Optimization`

Systems that exist mainly to keep late-game combat scalable.

Typical contents:

```text
SpatialHash  Large-scale target search and collision candidate lookup
Pooling      Object pools for projectiles, enemies, UI effects, and VFX
```

## Placement Rules

- Put game-specific files under `Assets/Project`.
- Keep Unity template and render pipeline settings in their existing Unity folders unless there is a clear reason to move them.
- Put C# code in `Scripts`.
- Put ScriptableObject content instances in `Data`.
- Put spawnable or reusable GameObject templates in `Prefabs`.
- Put actual playable scenes in `Scenes/Main`.
- Put small validation scenes in `Scenes/Test`.
- Put UI code in `Scripts/UI` and UI assets in `UI`.
- Put VFX presentation assets in `VFX`; keep combat rules in `Scripts/Gameplay`.
