# Feature Spec: level-2-swarm-cannon

**Status:** draft
**Branch:** feature/level-2-swarm-cannon
**Date:** 2026-09-16

## Description

Add a second level plus two new unit types. Title screen gains Level 1 / Level 2 selection. Level 2 uses a winding path — spawn on the left edge at vertical center, home base on the right edge at vertical center, path winds a few times between them. Two new units: a Swarm enemy (a tight cluster of 3 small circles spawned together) and a Cannon tower (similar range to Arrow, slower fire rate, projectile explodes on impact dealing AoE damage at the point of contact). Cannon and Swarm are gated to Level 2 — Level 1 keeps its straight path, Arrow-only towers, and basic-only enemies.

## Acceptance Criteria

### Level Selection
- [ ] Title screen shows two buttons: "Level 1" and "Level 2".
- [ ] Selecting a level starts that level's config and hides the title screen.
- [ ] Returning to title (game over / victory) re-shows level select.

### Level 2 Winding Path
- [ ] Spawn point at left edge, vertical center (grid row 10).
- [ ] Home base at right edge, vertical center (grid row 10).
- [ ] Path winds >= 3 times (at least 3 vertical direction changes), stays within grid bounds, is connected, and does not self-intersect.
- [ ] Towers cannot be placed on any path cell (not just row 10).
- [ ] Reference route (implementer may adjust as long as criteria hold): `(0,10) -> (3,10) -> (3,14) -> (7,14) -> (7,6) -> (11,6) -> (11,14) -> (15,14) -> (15,10) -> (19,10)`.

### Swarm Enemy
- [ ] New enemy type: small circle, diameter 24 (half of basic 48), distinct color from basic.
- [ ] HP 3 (dies to one Arrow shot of 10 damage), speed 2 (same as basic).
- [ ] Spawns in clusters of exactly 3, arranged as an even ring around the spawn point with a hollow center.
  - **Superseded 2026-09-16 by playtest:** originally "each at a tight offset (~16px apart)". At 16px apart with a 24px diameter the members overlapped and the cluster rendered as a single blob, and the offset was additionally erased by path following. Now `SwarmClusterRadius` 20px: adjacent members ~34.6px apart (~10.6px clearance), 8px-radius gap in the middle.
  - **Superseded:** the offset must now persist for the whole path, not just at spawn. Each member keeps a constant `FormationOffset` from its own path anchor; `Position = anchor + offset` is recomputed every tick, so the cluster travels as a rigid ring rather than collapsing onto the shared waypoints.
- [ ] Each cluster member is a separate enemy: own HP, own kill, own coin drop (1 each).
- [ ] All 3 follow the same winding path; each reaching base costs 1 HP.

### Cannon Tower
- [ ] New tower type: range 4 cells (same as Arrow), fire rate 2.5s (slower than Arrow 1.5s), cost 10.
- [ ] Projectile (cannonball) travels straight; on first enemy contact, explodes.
- [ ] Explosion deals 15 damage to every enemy within 1 cell (64px) of the impact point, including the struck enemy.
- [ ] If the cannonball reaches max range without contact, it dissipates with no explosion.

### Tower Selection UI
- [ ] HUD shows two buttons: "Place Arrow" and "Place Cannon".
- [ ] Cannon button hidden/disabled in Level 1; enabled in Level 2.
- [ ] Each button enters placement mode for the correct tower type.

### Level 1 Regression
- [ ] Level 1 unchanged: straight path (row 10), Arrow-only, basic-only, existing wave counts.

## Edge Cases

- **AoE kills multiple enemies in one frame:** all damaged enemies emit Destroyed and release to pool exactly once; no collection-modified exception, no double-free.
- **Two cannonballs explode in the same frame:** overlapping explosions each apply damage; enemies killed once; no crash.
- **Cannonball target dies before impact:** ball continues until it hits any other enemy or dissipates at max range.
- **Cannonball hits an enemy already dead this frame:** treat as miss for that enemy — ball keeps flying (does not explode on a corpse).
- **Swarm cluster spawn near wave end:** wave completion counts every individual swarm enemy; wave not marked complete until all 3 are resolved.
- **Tower placement on winding path cell that is not row 10:** rejected (path occupancy is per-cell, not per-row).
- **Mixed towers on the field:** each tower uses its own range, damage, and fire rate; targeting picks nearest enemy within that tower's range.

## Dependencies

- **Enemy refactor:** current `Enemy` hardcodes diameter/HP/speed/color. Introduce a base `Enemy` (or configurable params) so Swarm and Basic share movement/damage logic.
- **Tower base class:** current `GameManager` stores `List<ArrowTower>` and targets with `ArrowTowerRange`. Introduce a `Tower` base class (range/damage/fireRate/cost) with `ArrowTower` and `CannonTower` variants.
- **Projectile base class:** current `Projectile` is arrow-specific (`SourceTower = ArrowTower`, damage = `ArrowTowerDamage`). Introduce a base projectile + `CannonProjectile` (explodes, emits AoE).
- **Path generalization:** `GridManager` uses `PathRow` for path drawing, occupancy, and waypoints. Replace with a per-level path cell list.
- **LevelDefinition:** encapsulate path cells, tower availability, and wave composition. Two instances: `Level1`, `Level2`.
- **UI:** `TitleScreen` (two buttons), `GameHUD` (two tower buttons).

## Constraints

- **Performance:** object pools sized for max concurrent enemies/projectiles in Level 2 (swarm waves raise concurrent enemy count). AoE is O(n) per explosion over active enemies — acceptable.
- **Memory:** separate pools for arrow vs cannon projectiles (or a type-aware pool). No per-frame allocation in collision/explosion loops.
- **Platform:** keyboard + mouse; no new input dependencies.

## Visual / Geometry Theme

- **Swarm enemy:** small orange circle (basic stays red circle). Border distinguishes it from basic.
- **Cannon tower:** filled circle (cannon barrel) with dark outline, distinct color (e.g., dark green/gray). Range indicator circle same style as Arrow.
- **Cannon projectile:** small dark circle. Explosion: brief expanding circle (procedural, fading) — optional visual flourish, no external assets.
- **Winding path:** tan path cells along the winding route (same palette as Level 1 path).
- All procedural via Godot draw nodes — no AI/external assets.

## Proposed Tuning (constants — human may adjust)

| Constant | Value |
|----------|-------|
| SwarmEnemyHP | 3 |
| SwarmEnemyDiameter | 24 |
| SwarmEnemySpeed | 2 |
| SwarmClusterSize | 3 |
| SwarmClusterRadius | 20px (ring radius; superseded 2026-09-16, was SwarmClusterSpacing 16px — too tight, members overlapped the 24px diameter) |
| CannonTowerRange | 4 cells |
| CannonTowerFireRate | 2.5s |
| CannonTowerDamage | 15 (AoE) |
| CannonTowerAoeRadius | 1 cell (64px) |
| CannonTowerCost | 10 |

## Out of Scope

- Boss enemies, other planned tower types (Sniper, Slow, Buff), meta-progression/XP/skill tree.
- Level unlock persistence (both levels freely selectable — no save file).
- Per-level tower cost/damage differences beyond what's specified.
- Tower upgrades/selling.
