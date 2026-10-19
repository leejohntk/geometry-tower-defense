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
- Keep simulation position and presentation/formation offset as separate fields. Never snap or overwrite the simulation anchor to achieve a visual effect — recompute `Position = anchor + offset` each tick. Reference: `src/Enemies/Enemy.cs` `_anchorPosition` / `_formationOffset`.

## Performance

- Object pooling for projectiles and enemies (frequent spawn/despawn).
- `[RequireGodotRuntime]` only on tests that need nodes/scenes/engine APIs.
- Avoid `_Process` on every node. Centralize update loops where possible.
- Use `VisibleOnScreenNotifier2D` for culling checks.

## Scene Conventions

- Use Godot MCP tools for all scene creation and editing. Never hand-edit `.tscn`.
- Root node type matches scene purpose (Node2D for gameplay, Control for UI).
- Scene files in relevant `src/` subdirectory alongside their C# script.

## Named Visual Groups

- Every visual group built in code is wrapped in a `Control` whose `Name` identifies the
  group: rows (`Row_{id}`), title blocks (`Block_Title`), panels (`Panel_{Name}`), and
  button groups (`Block_{Name}`). Give the important children stable names too
  (`Name`, `Rank`, `Buy`, `Icon`).
- Position the wrapper so its rect spans the group, and place children relative to it.
  Wrapping must not change any child's global position, size, input, or rendering; set
  `MouseFilter = Ignore` on wrapper Controls so they cannot consume input.
- Group identity is the wrapper node, never a y-coordinate heuristic or a generated name.
  Generated names (`@Label@31`) are not resolvable by the headless layout verifier and
  must never appear in a finding. This is a general UI rule, not specific to one screen.
