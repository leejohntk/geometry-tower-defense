# Feature Spec: tower-skill-tree-mechanics

**Status:** approved
**Branch:** feature/tower-skill-tree-mechanics
**Date:** 2026-09-21

Part 2 of 2. Depends on `tower-skill-tree-framework` (merged first). Activates the mechanic nodes left disabled in Part 1.

## Description

Turns on the 7 behavior-changing nodes — the seeds that lean each tower toward its archetype, plus the Laser Ignite trunk node. These change *how* a tower behaves, not just its numbers.

## Mechanic Nodes

### Arrow Tower

| Node | Effect | Per rank | Rank 5 total |
|------|--------|----------|--------------|
| Pierce (→ Barrage) | arrow passes through +N enemies | +1 | pierce 5 |
| Crit Chance (→ Sharpshooter) | % chance to deal 2× damage on hit | +8% | 40% |

### Cannon Tower

| Node | Effect | Per rank | Rank 5 total |
|------|--------|----------|--------------|
| Cluster (→ Bombardier) | +1 shell per volley (spread) | +1 | 6 shells |
| Stun Chance (→ Concussive) | % chance to stun on hit | +6% | 30% |

### Laser Tower

| Node | Effect | Per rank | Rank 5 total |
|------|--------|----------|--------------|
| Ignite (trunk) | % chance per second to apply a burn DoT | +5% | 25% |
| Chain (→ Arc/Chain) | +1 beam jump to a nearby enemy | +1 | 5 jumps |
| Ramp-Up (→ Melter) | dps ramps to +% over 2s while holding one target | +20% cap | 2.0× |

## Effect Semantics

- **Pierce** — an arrow continues along its direction after hitting an enemy, applying its damage to up to N further enemies in its path. Armor applies per hit.
- **Crit** — independent per-hit roll; success deals 2× the (already skill-boosted) damage. Each pierced hit rolls its own crit.
- **Cluster** — each volley fires (1 + r) shells spread across a small arc; each shell applies the normal single-target hit, and each shell's own cannon explosion uses the boosted splash radius.
- **Stun** — enemy stops moving for 1.0s (movement only; armor still reduces the stunning hit's damage). Re-stun refreshes the timer, does not stack.
- **Ignite (Burn)** — roll once per second of continuous beam contact: on success apply a burn of 2 dps for 2.0s (10 total). Re-proc refreshes duration, does not stack. Burn damage ignores armor.
- **Chain** — beam jumps from the current target to up to N further enemies within 1.5 cells of the previous target, each jump at 60% of the previous tick's damage. No enemy hit twice by one chain.
- **Ramp-Up** — while the beam holds the *same* target, its dps multiplier ramps linearly 1.0 → (1 + 0.2·r) over 2.0s; switching targets resets to 1.0.

## Dependencies

- `SkillTreeState` from Part 1 — read ranks for the 7 nodes; the catalog's `enabled` flag flips to true.
- `Enemy` — add stun state (pause movement) and burn-DoT state (or a per-enemy status list). `TakeDamage` signature unchanged; burn/stun tick in a status update pass.
- `Projectile` / `ArrowProjectile` — pierce continuation, crit roll.
- `CannonProjectile` / cannon firing path — cluster multi-shell volley; stun application on hit.
- `GameManager.UpdateTowerDrain` — laser chain, ramp-up, ignite hooks.
- `GameConstants` — mechanic node constants.

## Constraints

- **Performance:** seed mechanics reuse the existing targeting/collision loops; no new per-frame allocation. Burn/stun status handled without allocation per tick.

## Visual / Geometry Theme

- Chain: beam draws a short arc/segment to each jumped target (thin magenta line).
- Burn: burning enemy shows a subtle orange tint/overlay.
- Crit: brief brighter flash on hit.
- Stun: enemy flickers or desaturates while stunned.
- All procedural geometry; no AI-generated assets.

## Out of Scope

- In-game tower upgrade / archetype specialization (future feature).
- Respec / refund.
- Balance beyond initial values.

## Constants (tunable — review before approve)

| Constant | Value | Notes |
|----------|-------|-------|
| Arrow: `SkillArrowPiercePerRank` | 1 | enemies |
| Arrow: `SkillArrowCritChancePerRank` | 0.08 | |
| Cannon: `SkillCannonClusterPerRank` | 1 | shells |
| Cannon: `SkillCannonStunChancePerRank` | 0.06 | |
| Cannon: `SkillCannonStunDuration` | 1.0 | seconds |
| Laser: `SkillLaserIgniteChancePerRank` | 0.05 | per second |
| Laser: `SkillLaserBurnDps` | 2.0 | |
| Laser: `SkillLaserBurnDuration` | 2.0 | seconds |
| Laser: `SkillLaserChainPerRank` | 1 | jumps |
| Laser: `SkillLaserChainRange` | 1.5 | cells between jumps |
| Laser: `SkillLaserChainFalloff` | 0.60 | damage per jump |
| Laser: `SkillLaserRampPerRank` | 0.20 | max dps multiplier (1 + 0.20·r) |
| Laser: `SkillLaserRampTime` | 2.0 | seconds to full ramp |
