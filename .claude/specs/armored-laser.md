# Feature Spec: armored-laser

**Status:** draft
**Branch:** feature/armored-laser
**Date:** 2026-09-19

## Description

Adds an Armored enemy type, a Laser tower, and a new Level 3 that introduces both.

- **Armored enemy** — tough, grey square, same speed as basic. Has flat armor that reduces damage from each Arrow or Cannon hit, so neither basic tower can one-shot it. Armor does not affect the Laser tower.
- **Laser tower** — fires a continuous beam (no projectile). Deals small damage every frame as a "drain" to a single target: the nearest enemy in range, matching how Arrow/Cannon already target. Damage ignores armor. Range is shorter than the Arrow tower.
- Swarm enemy HP raised from 3 to 5.
- Armored enemies and the Laser tower appear only in Level 3.

## Acceptance Criteria

- [ ] Armored enemy has 14 HP and 5 flat armor.
- [ ] Arrow (10 dmg) needs 3 hits to kill an Armored enemy; Cannon (15 dmg) needs 2.
- [ ] Laser damage bypasses armor entirely.
- [ ] Laser tower has 3-cell range (shorter than Arrow's 4) and drains a single nearest target continuously.
- [ ] Laser tower produces no projectile and no pooling churn.
- [ ] Armored enemies and the Laser tower are available in Level 3 only — absent from Levels 1 and 2.
- [ ] Swarm enemy HP is 5 (was 3).
- [ ] `dotnet build` exits 0; `dotnet test` all pass.

## Edge Cases

- **Armor floor:** damage after armor is clamped to ≥ 0 (armor never produces negative/healing damage).
- **Laser target dies mid-frame:** beam stops / retargets next frame; no null-reference on a dead target.
- **Laser target leaves range:** laser stops draining immediately and retargets to the nearest in-range enemy.
- **Cannon AoE vs Armored:** each armored enemy in the blast takes armor-reduced damage exactly once.
- **Pooled enemy reuse:** a pooled enemy reused as Armored resets armor/HP correctly; a reused Basic has zero armor.
- **Armored reaches the end:** still costs 1 player HP like any other enemy.

## Dependencies

- `Enemy.TakeDamage(float)` — extended with an armor field and an `ignoreArmor` flag.
- Centralized targeting in `GameManager.UpdateTowerTargeting()` — laser reuses the already-computed nearest-in-range target.
- `LevelDefinition.AllowCannonTower` pattern — extended with `AllowLaserTower`.
- `SpawnKind` / `EnemyKind` enums — extended with `Armored`.
- `TowerType` enum — extended with `Laser`.
- `Levels` registry — new `Level3` entry (path + waves + tower flags).

## Constraints

- **Performance:** laser adds no per-frame allocation; it drains against the target the centralized targeting loop already computes. Cache + slow-re-scan targeting is a future optimization, not required here.
- **Memory:** no new object pool needed (laser has no projectile).
- **Platform:** keyboard + mouse placement, same as existing towers.

## Visual / Geometry Theme

- **Armored enemy:** grey square (diamond) with a darker border, ~48px. Distinct from red circle (basic) and orange circle (swarm).
- **Laser tower:** magenta/purple emitter with a thin beam line drawn from tower to current target while draining.
- No AI-generated assets — procedural geometry only.

## Out of Scope

- Laser splitting to multiple targets (future).
- Cache + slow re-scan targeting optimization (future perf work).
- Armored enemy balance beyond the initial values below.

## Level 3 Definition

New level, following the Level-2-introduced-new-content pattern. Allows Arrow + Cannon + Laser. Armored enemies appear in later waves.

Path corners (20×14 grid):

```
(0,5) (4,5) (4,10) (8,10) (8,3) (12,3) (12,10) (16,10) (16,5) (19,5)
```

Waves:

1. 4 Basic
2. 6 Basic
3. 4 Basic + 1 SwarmCluster
4. 3 Basic + 2 Armored
5. 2 Basic + 1 SwarmCluster + 2 Armored

`AllowCannonTower: true`, `AllowLaserTower: true`. Path/waves are tunable.

## Constants (tunable — review before approve)

| Constant | Value | Notes |
|----------|-------|-------|
| `SwarmEnemyHP` | 5 | was 3 |
| `ArmoredEnemyHP` | 14 | 3 arrow hits / 2 cannon hits |
| `ArmoredEnemyArmor` | 5 | flat reduction |
| `ArmoredEnemySpeed` | 2.0f | same as basic — stays in formation with the wave |
| `ArmoredEnemyDiameter` | 48 | same as basic |
| `ArmoredCoinDropPerKill` | 2 | tankier = more reward |
| `LaserTowerRange` | 3 | cells |
| `LaserTowerDps` | 4.0f | float; armor ignored |
| `LaserTowerCost` | 10 | same cost as arrow/cannon |

Armor math check (Armored: 14 HP, 5 armor):
- Arrow 10 − 5 = 5/hit → ceil(14/5) = 3 hits.
- Cannon 15 − 5 = 10/hit → ceil(14/10) = 2 hits.
- Laser 4.0 dps ignores armor → 14/4 = 3.5s.
