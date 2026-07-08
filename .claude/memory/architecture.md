---
name: architecture
description: Project structure, scene hierarchy, data flow, and architectural decisions for the geometry tower defense game
metadata:
  type: project
---

# Architecture

## Project Structure

```
geometry-tower-defense/
├── src/
│   ├── GameManager.cs          — global state, wave orchestration, economy
│   ├── Towers/
│   │   ├── Tower.cs            — base tower class
│   │   ├── PiercingTower.cs
│   │   ├── SplashTower.cs
│   │   ├── SlowTower.cs
│   │   ├── SniperTower.cs
│   │   └── BuffTower.cs
│   ├── Enemies/
│   │   ├── Enemy.cs            — base enemy class
│   │   ├── BasicEnemy.cs
│   │   ├── FastEnemy.cs
│   │   ├── TankEnemy.cs
│   │   ├── SwarmEnemy.cs
│   │   └── BossEnemy.cs
│   ├── Projectiles/
│   │   ├── Projectile.cs       — base projectile class
│   │   ├── PiercingProjectile.cs
│   │   ├── SplashProjectile.cs
│   │   └── SlowProjectile.cs
│   ├── UI/
│   │   ├── HUD.cs
│   │   ├── TowerPlacer.cs
│   │   ├── WaveIndicator.cs
│   │   └── GameOverScreen.cs
│   ├── Levels/
│   │   ├── LevelData.cs        — path definitions, grid layout
│   │   └── WaveData.cs         — wave composition data
│   └── Systems/
│       ├── ObjectPool.cs       — generic object pool
│       ├── EconomyManager.cs   — currency tracking
│       └── WaveManager.cs      — wave spawning logic
├── Tests/
│   ├── Towers/
│   ├── Enemies/
│   ├── Projectiles/
│   ├── Systems/
│   └── TestLevels/             — dedicated test scenes for integration tests
└── Resources/
    └── (geometry shapes, colors, UI assets)
```

## Scene Hierarchy (planned)

```
Main (Node2D)
├── GameManager (Node)
├── Grid (TileMap or Node2D)
├── Towers (Node2D)
│   └── (instantiated tower scenes)
├── Enemies (Node2D)
│   └── (instantiated enemy scenes)
├── Projectiles (Node2D)
│   └── (instantiated projectile scenes)
├── UI (CanvasLayer)
│   ├── HUD (Control)
│   ├── TowerPlacer (Control)
│   └── WaveIndicator (Control)
└── Path (Path2D)
    └── PathFollow2D (for enemy movement)
```

## Data Flow

1. **GameManager** orchestrates wave lifecycle (start wave, track enemies, end wave).
2. **WaveManager** reads WaveData, spawns enemies at intervals.
3. **EconomyManager** tracks gold, validates purchases, processes kill rewards.
4. **Towers** detect enemies in range (Area2D), fire projectiles at intervals.
5. **Projectiles** travel, hit enemies, apply damage + effects.
6. **Enemies** follow Path2D, take damage, die (trigger gold reward).
7. **HUD** observes GameManager signals, updates UI.

## Key Architectural Decisions

- **Godot signals** for loose coupling between systems (no direct references between unrelated nodes).
- **Object pooling** for projectiles and enemies (frequent instantiation/destruction is expensive).
- **Composition over inheritance** for tower/enemy behaviors (damage types, movement patterns, effects as components).
- **C# primary** — all game logic in C#. GDScript only for trivial scene wiring where C# interop is impractical.
- **Test-first** — each feature includes unit tests. Scene-runner tests for integration.
