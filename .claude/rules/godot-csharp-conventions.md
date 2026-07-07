---
paths: ["src/**/*.cs", "Tests/**/*.cs", "**/*.cs"]
---

# Godot C# Conventions

## Naming

- Classes: PascalCase. Nodes: PascalCase. Methods: PascalCase. Fields: _camelCase with underscore prefix.
- Signals: PascalCase, declared with `[Signal]` attribute. Handler methods: `OnSignalName`.
- Resource files: snake_case (e.g., `piercing_tower.tscn`, `enemy_spawner.gd`).
- Test classes: `{Feature}Test` (e.g., `TowerTest`, `WaveManagerTest`).

## Project Structure

```
src/
├── GameManager.cs          — global game state, wave orchestration
├── Towers/                 — tower base class + variants
├── Enemies/                — enemy base class + variants
├── Projectiles/            — projectile base class + variants
├── UI/                     — HUD, menus, overlays
└── Levels/                 — level logic, path definitions
Tests/
├── Towers/
├── Enemies/
├── ...
└── TestLevels/             — dedicated test scenes
```

## C# Patterns

- Use `[Export]` for inspector-exposed properties. Prefer constructor injection where feasible.
- Use `[Signal]` delegate for signal declarations.
- Prefer composition over deep inheritance. Tower types share behavior via components, not 6-deep class trees.
- Use `Vector2`/`Vector2I` for 2D positions. Snap to grid using `Mathf.Round` when needed.
- `_Ready()` for node initialization, `_Process(double delta)` for per-frame updates.
- `GD.Print()` for debug logging. Remove before PR unless intentionally permanent.

## Performance

- Object pooling for projectiles and enemies (frequent spawn/despawn).
- `[RequireGodotRuntime]` only on tests that need nodes/scenes/engine APIs.
- Avoid `_Process` on every node. Centralize update loops where possible.
- Use `VisibleOnScreenNotifier2D` for culling checks.

## Scene Conventions

- Use Godot MCP tools for all scene creation and editing. Never hand-edit `.tscn`.
- Root node type matches scene purpose (Node2D for gameplay, Control for UI).
- Scene files in relevant `src/` subdirectory alongside their C# script.
