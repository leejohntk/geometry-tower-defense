# Holdout Scenarios: tower-skill-tree-framework

Implementer must NOT see this file. Orchestrator runs these during verification.

## H1 — Victory awards 2×level, defeat awards half (level)
- Clearing level 1 awards 2 SP; level 2 → 4; level 3 → 6; level 4 → 8.
- Losing level 1 awards 1 SP; level 2 → 2; level 3 → 3; level 4 → 4 (half of a win).
- Award happens on the victory/defeat transition, NOT per kill. In-level coins per kill are unchanged.

## H2 — No per-kill SP; award fires exactly once per run
- Destroying enemies never changes the SP balance (per-kill award removed).
- A run ending in victory awards SP exactly once (not per enemy).
- A run ending in defeat awards SP exactly once — the killing-blow enemy that drops HP to 0 does not double-award, and later enemies are ignored because `_Process` returns early on GameOver.

## H3 — rank cap at 5, no overflow
- `BuyRank(node)` at rank 5 returns false, does not charge SP, rank stays 5.
- Buy with insufficient SP returns false, leaves SP + rank unchanged.

## H4 — persistence round-trip
- Save state with SP=37 and ranks {Arrow.Damage=3, Laser.DPS=5}; load into a fresh `SkillTreeState`; assert SP and every rank match exactly. Default (no save file) = 0 SP, 0 ranks.

## H5 — Arrow stat math
- Damage rank 3 → Arrow damage 16 (10 + 3·2).
- Attack speed rank 5 → fire interval 1.0s (1.5 / 1.5).
- Range rank 5 → 6.5 cells (4 + 2.5). Range is float, not rounded.

## H6 — Cannon stays slowest
- Cannon attack-speed rank 5 → interval ≈1.67s (2.5 / 1.5), still > Arrow 1.0s.
- Powder Charge rank 5 → damage 20 and projectile speed +50%.

## H7 — Laser stat math
- DPS rank 5 → 8 (4 + 5·0.8). Range rank 5 → 5.5 cells.

## H8 — Title screen → skill tree → back, game still playable
- Title screen has a "Skill Tree" button that opens `SkillTreeScreen`; Back returns to title.
- After visiting the skill tree, level select still starts a game normally.

## H9 — Mechanic nodes disabled in Part 1
- Pierce, Crit, Cluster, Stun, Ignite, Chain, Ramp-Up render in the tree but are not buyable (greyed, "coming soon"). `BuyRank` on a disabled node returns false and charges nothing.

## H10 — No per-frame allocation
- A level run with stat nodes active adds no new allocations in the per-frame update path.
