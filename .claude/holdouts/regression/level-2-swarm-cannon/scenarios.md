# Holdout Scenarios: level-2-swarm-cannon

Implementer must not see this file. Orchestrator runs these during holdout verification (Phase 5).

## H1 — Spawn and base at vertical center (Level 2)
- **Setup:** Load Level 2. Start wave 1.
- **Check:** First enemy spawns at left edge, Y == vertical center (row 10 cell center). Base indicator sits at right edge, Y == row 10 cell center.
- **Expected:** Enemy Position.Y == CellCenterY(10); base drawn at CellCenterX(19), CellCenterY(10).

## H2 — Path winds, not straight
- **Setup:** Inspect Level 2 path waypoints.
- **Check:** Waypoints not all on one row. Vertical direction changes >= 3. Path connected, all cells in-bounds, no self-intersection.
- **Expected:** Path visits >= 3 distinct rows; first waypoint col 0, last col 19; consecutive waypoints 4-connected (differ by one cell in one axis).

## H3 — Tower placement blocked on off-center path cells
- **Setup:** Load Level 2. Attempt PlaceTower on a cell on the winding path but NOT row 10 (e.g., row 14, col 3).
- **Check:** CanPlaceTower returns false.
- **Expected:** Placement rejected; no coin deducted; no tower created.

## H4 — Swarm cluster spawns exactly 3 separate enemies
- **Setup:** Load Level 2. Trigger a swarm cluster spawn.
- **Check:** Enemy count +3. Each distinct instance, distinct position (~16px offsets), own HP.
- **Expected:** 3 enemies, all Swarm type (diameter 24), wave alive-count +3. Wave not complete until all 3 resolved.

## H5 — Swarm stats: dies to one arrow shot
- **Setup:** Spawn a single swarm enemy. Apply 10 damage (Arrow damage).
- **Check:** Enemy HP 3 destroyed on one hit.
- **Expected:** IsDead true after TakeDamage(10). Coin drop 1.

## H6 — Cannon AoE kills a swarm cluster
- **Setup:** Level 2. Place a cannon. Three swarm enemies clustered within 1 cell of each other enter cannon range.
- **Check:** Cannon fires; on impact all 3 take 15 AoE damage and die.
- **Expected:** All 3 destroyed in one shot. Coins +3, wave count -3, no crash.

## H7 — Cannon AoE radius bound
- **Setup:** Level 2. Enemy A at impact point; enemy B placed >64px away (e.g., 2 cells).
- **Check:** On explosion, A takes damage; B does not.
- **Expected:** B HP unchanged, survives.

## H8 — Cannon fire rate slower than Arrow
- **Setup:** One arrow + one cannon, each with a steady target in range.
- **Check:** Over a 7.5s window, arrow fires ~5 shots (1.5s), cannon ~3 shots (2.5s).
- **Expected:** Cannon shot count < arrow shot count; cannon interval ~2.5s.

## H9 — Cannonball dissipates at max range, no explosion
- **Setup:** Level 2. Fire cannon at a target 4 cells away that moves out of the way / no enemy in range.
- **Check:** Cannonball travels 4 cells, dissipates, no AoE damage applied.
- **Expected:** Projectile dissipates; no enemy damaged; projectile returned to pool.

## H10 — Level 1 regression
- **Setup:** Load Level 1.
- **Check:** Straight path (all waypoints row 10). No cannon button. No swarm enemy ever spawns. Wave counts match existing (3,5,7,9,12).
- **Expected:** Level 1 behavior identical to pre-feature.

## H11 — Level select flows
- **Setup:** Title -> select Level 1 -> play -> return to title -> select Level 2.
- **Check:** Each selection loads correct config; return to title re-shows both buttons.
- **Expected:** Level 2 shows cannon button + winding path; Level 1 does not.

## H12 — Overlapping explosions no double-free
- **Setup:** Two cannons fire at the same cluster of 3 swarm enemies; both cannonballs explode same frame.
- **Check:** No exception; each enemy destroyed exactly once; each released to pool once; no collection-modified error.
- **Expected:** All 3 destroyed, coins +3, active enemy list empty, no crash.

## H13 — Full swarm cluster reaching base costs 3 HP
- **Setup:** Level 2. Let a full swarm cluster (3) reach the base with no towers.
- **Check:** HP decreases by 3 total.
- **Expected:** HP 3 -> 0 (game over) for a full cluster on a fresh 3-HP game.

## H14 — Mixed tower targeting uses per-tower range
- **Setup:** Level 2. One arrow + one cannon placed at different distances from an enemy.
- **Check:** A tower fires only when the enemy is within that tower's own range.
- **Expected:** No tower fires at an enemy outside 4 cells. Each tower's range/damage independent.

## H15 — Performance: many concurrent swarm enemies
- **Setup:** Level 2. Force ~15 concurrent swarm enemies + active projectiles.
- **Check:** No errors, no pool exhaustion (pool grows or sized >= max concurrent), frame updates complete.
- **Expected:** Game runs clean; all enemies spawn and resolve.
