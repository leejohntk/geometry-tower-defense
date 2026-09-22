# Holdout Scenarios: tower-skill-tree-mechanics

Implementer must NOT see this file. Orchestrator runs these during verification. Assumes `tower-skill-tree-framework` is merged.

## H1 — Both seeds buyable, no lockout
- Arrow: buying Barrage-seed (Pierce) rank does NOT lock Sharpshooter-seed (Crit). Both can reach rank 5. Same for Cannon (Cluster/Stun) and Laser (Chain/Ramp-Up).

## H2 — Pierce applies damage to each enemy, max N
- Arrow Pierce rank 2 fires at 3 collinear enemies: projectile hits all 3 (first + 2 pierced), not just the first. Each takes the arrow's damage (armor applies per hit). A 4th collinear enemy behind is NOT hit.

## H3 — Crit is 2× and independent
- Arrow Crit rank 5 (40%): a crit hit deals exactly 2× the skill-boosted damage; non-crit deals 1×. Each pierced hit rolls its own crit.

## H4 — Cluster fires 1+N shells, each normal hit
- Cannon Cluster rank 3 fires 4 shells in one volley, spread across an arc. Each shell applies the cannon's single-target damage; each shell's own explosion uses the boosted splash radius.
- Splash Radius rank 5 → AoE radius 104px (64 + 5·8).

## H5 — Stun freezes movement, refreshes, armor still applies
- Cannon Stun rank 5 (30%): a stunning hit stops the enemy's movement for 1.0s. A second stun before expiry refreshes to 1.0s, does not extend to 2.0s. The stunning hit's damage is still armor-reduced.

## H6 — Ignite rolls once per second, burn refreshes not stacks
- Ignite rank 5 (25%/sec): burn is 2 dps for 2.0s (10 total). Re-proc while burning resets to 2.0s remaining, never overlaps into 4 dps. Burn damage ignores armor.

## H7 — Chain jumps at 60% falloff, no double-hit
- Laser Chain rank 2, 3 enemies each within 1.5 cells of the previous: primary takes full dps, jump 1 takes 60%, jump 2 takes 36%. No enemy hit twice per chain. An enemy >1.5 cells from the previous target is not jumped to.

## H8 — Ramp-Up resets on target switch
- Laser Ramp-Up rank 5: dps multiplier ramps 1.0 → 2.0 over 2.0s on one target. Switching targets immediately resets to 1.0.

## H9 — Ignite rolls once per second, not per frame
- Over N seconds of continuous drain, the number of burn applications is bounded by N (roll once per second), not by the per-frame tick count.

## H10 — No per-frame allocation
- A session with pierce + cluster + chain + ramp + ignite + stun active runs without new allocations in the per-frame update path (reuse existing loops / cached lists).
