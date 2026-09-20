# Feature Spec: level-4-multi-path

**Status:** draft
**Branch:** feature/level-4-multi-path
**Date:** 2026-09-20

## Description

Adds Level 4 — a hard multi-path level. Two spawn points on the left feed two paths that converge in the center, split, converge, split, then a final converge into a single home base. Level 4 mixes all three enemy kinds (Basic, Swarm, Armored) and allows all three tower types. Not expected to be beatable with the current tower lineup — this ships the level first so the human can playtest it.

> **Scope note:** this is an architectural change, not just "another level." The current level system supports a single path (`LevelDefinition.PathCells`). Two spawn points with converge/split topology requires multi-path support. Levels 1–3 must keep working unchanged.

## Acceptance Criteria

- [ ] Level 4 has exactly two routes, each a valid path (axis-aligned, connected, in-bounds 20×14, non-self-intersecting).
- [ ] Both routes start on the left (col 0) and end at a single shared base cell.
- [ ] Topology is converge → split → converge → split → converge into base (3 converges, 2 splits).
- [ ] Enemies spawn from both spawn points, distributed round-robin across the two routes.
- [ ] A swarm cluster's members all share one route (a cluster never straddles two routes).
- [ ] Level 4 waves contain all three enemy kinds.
- [ ] Level 4 allows Arrow, Cannon, and Laser towers.
- [ ] Levels 1–3 are unchanged and still work (single-path backward compatibility).
- [ ] `dotnet build` exits 0; `dotnet test` all pass.

## Multi-Path Model

- `LevelDefinition.PathCells` (single list) becomes `Paths` (`IReadOnlyList<IReadOnlyList<Vector2I>>`), one entry per route.
- `SpawnCell` becomes `SpawnCells` (one per route, `route[0]`). `BaseCell` stays single — all routes must end at the same cell (validated).
- `GridManager` draws every route (shared converge cells drawn once) and exposes waypoints per route.
- `WaveManager` keeps a round-robin route counter: each spawn event (Basic/Armored = 1 enemy, SwarmCluster = 3) is assigned `routeIndex = counter++ % routes.Count`. Cluster members share that one route.
- `Enemy` is unchanged (it already accepts a waypoint list); the assigned route's waypoints are set at spawn.

## Level 4 Definition

Two routes (corner points, expanded with `LevelDefinition.BuildPath`):

Route A (top spawn):
```
(0,3) (5,3) (5,7) (8,7) (8,4) (11,4) (11,7) (14,7) (14,10) (17,10) (17,7) (19,7)
```

Route B (bottom spawn):
```
(0,11) (5,11) (5,7) (8,7) (8,10) (11,10) (11,7) (14,7) (14,4) (17,4) (17,7) (19,7)
```

- Spawns: `(0,3)` and `(0,11)`. Base: `(19,7)`.
- Converge cells (shared): `(5,7)`, `(11,7)`, `(17,7)` (+ the runs between).
- Split cells: `(8,7)` (to rows 4/10), `(14,7)` (to rows 10/4).

Waves (all three enemy kinds, escalating):

1. 6 Basic
2. 4 Basic + 2 SwarmCluster
3. 4 Basic + 2 Armored
4. 3 Basic + 2 SwarmCluster + 2 Armored
5. 2 Basic + 2 SwarmCluster + 4 Armored

`AllowCannonTower: true`, `AllowLaserTower: true`.

## Edge Cases

- **Shared base:** every route ends at the same cell; a level with divergent bases is rejected.
- **Route assignment overflow:** round-robin counter wraps cleanly across waves.
- **Cluster on one route:** all 3 swarm members get the same route index.
- **Single-path levels:** Levels 1–3 pass a one-element `Paths` list and behave exactly as before.
- **Grid drawing:** shared converge cells drawn once (no double-draw artifacts).

## Dependencies

- `LevelDefinition` — `PathCells` → `Paths`; `SpawnCell` → `SpawnCells`; constructor update.
- `GridManager` — multi-route drawing + per-route waypoint accessor.
- `WaveManager` — route counter + route-indexed spawning.
- `GameManager.OnEnemySpawned` — set path from the enemy's assigned route.
- `Levels` registry — new `Level4`; `Get(4)` resolves it.
- `TitleScreen` — Level 4 button.

## Constraints

- **Performance:** no per-frame allocation added; route counter is O(1). Shared-cell drawing uses a `HashSet` to dedupe.
- **Backward compatibility:** Levels 1–3 use the new `Paths` shape with one entry; no gameplay change.
- **Platform:** keyboard + mouse, same as existing.

## Visual / Geometry Theme

- Paths rendered as the existing geometric grid lines; the two routes share converge segments.
- Spawn points and base marked the same way as other levels. No AI-generated assets.

## Out of Scope

- Runtime branch *choice* for an enemy (each enemy follows a fixed assigned route — no mid-path decisions).
- Per-wave explicit route control (round-robin only for now).
- Difficulty tuning beyond the initial waves below.
