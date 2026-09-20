# Holdout Scenarios: armored-laser

Implementer must NOT see this file. Orchestrator runs these during verification.

## H1 — Armor math (flat reduction)
- Armored (14 HP, 5 armor) hit by Arrow (10): HP goes 14 → 9 (5 dmg), NOT 14 → 4.
- Armored hit by Cannon (15): HP goes 14 → 4 (10 dmg).
- Assert `TakeDamage(10, ignoreArmor: false)` on a 14-HP armored enemy leaves 9 HP.

## H2 — No one-shot by basic towers
- Arrow does NOT kill an armored enemy in one hit. Cannon does NOT kill it in one hit.
- Arrow kills in exactly 3 hits; Cannon in exactly 2.

## H3 — Laser ignores armor
- Laser `TakeDamage(x, ignoreArmor: true)` reduces HP by the full `x`, no armor subtraction.
- Laser kills armored (14 HP) in 3.5s at 4.0 dps.

## H4 — Armor floor never negative
- Any damage < armor (e.g. a 3-dmg source vs 5 armor) clamps to 0, never heals or goes negative.

## H5 — Laser produces no projectile
- No ArrowProjectile/CannonProjectile is spawned by a Laser tower across any frame.
- `_activeProjectiles` count is unchanged while only lasers are firing.

## H6 — Laser retargets on death
- A laser draining a target that dies mid-frame does not throw; next frame it targets a new nearest enemy (or none).

## H7 — Laser range is shorter than arrow
- An enemy exactly 3 cells from the laser takes damage; at >3 cells it does not.
- Laser range (3) < arrow range (4).

## H8 — Swarm HP is 5
- Swarm enemy spawns with 5 HP (not 3). Arrow still one-shots it (10 ≥ 5).

## H9 — Armored is a distinct EnemyKind, pooled correctly
- Enemy acquired → `Configure(Armored)` → released → re-acquired as `Basic` ends with armor 0 and HP 10.

## H10 — Cannon AoE applies armor once per armored enemy
- Two armored enemies inside a cannon blast each take exactly 10 (15 − 5), not 15, not doubled.
