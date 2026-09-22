---
name: current-feature
description: Active feature tracking — what's being worked on, state, branch, progress
metadata:
  type: project
---

# Current Feature

**Status:** awaiting_playtest

**Feature:** tower-skill-tree-mechanics (Part 2 of 2) — all gates green, PR open
**Branch:** feature/tower-skill-tree-mechanics (PR #19)
**Spec:** `.claude/specs/tower-skill-tree-mechanics.md` (approved 2026-09-22)

Activates the 7 mechanic nodes Part 1 shipped disabled: Arrow Pierce + Crit Chance, Cannon
Cluster + Stun Chance, Laser Ignite (trunk) + Chain + Ramp-Up. Behavior-changing, not numbers.
Ships the first stun/burn status effects on `Enemy` and the first per-tick laser beam mechanics
(chain jumps at 60% falloff, dps ramp to 2.0× over 2s, ignite burn 2 dps × 2.0s ignoring armor).
Crit/stun/ignite rolls route through a single seeded source so they are unit-testable without
flakiness.

Spec + holdouts committed on the feature branch (`90cff40`). Part 1 merged as PR #18.

**Gates:** build 0 warnings / 0 errors · 171 feature tests pass, 0 skipped · review aggregate
0 CRITICAL / 4 WARNING / 5 INFO (5 fixed, 4 declined) · holdouts 10/10 PASS re-verified on the
final post-simplify artifact · `/simplify` done (1 cleanup applied, 7 declined with reasons).
Holdouts moved to `.claude/holdouts/regression/tower-skill-tree-mechanics/`.

**Follow-up owed (doc drift, not code):** the part 1 framework spec's Description line claims Skill
Points are awarded per kill, tiered by enemy kind — its own Currency Design section and the shipped
code award them per level outcome (victory = `levelId * 2`, defeat = half). Correct that spec line.

## Feature Queue

- (empty — serial execution, rule 7: human queues the next idea while this one runs)

## Recent Features

- **Tower skill tree mechanics** (part 2/2, PR #19, opened 2026-09-22) — activates the 7 hidden
  mechanic nodes: Arrow pierce + crit, Cannon cluster + stun, Laser ignite + chain (60%
  compounding falloff) + ramp (1.0→2.0× over 2s). First stun/burn status effects on `Enemy`;
  status and beam hot paths kept allocation-free. Review caught a real bug: `LaserTower`'s held
  target used `ReferenceEquals`, so a LIFO-pooled enemy reissued before the next drain pass
  inherited the old ramp — fixed with an `Enemy.Generation` counter bumped in `ResetForPool()`.

- **Tower skill tree framework** (part 1/2, PR #18, merged 2026-09-22) —
  persistent Skill Points awarded per level outcome (victory = 2 × level id, defeat = half
  that), 3 tower trees × 15 nodes,
  5 ranks at a flat 10 SP, 8 stat nodes buyable and 7 mechanic nodes staged greyed. First
  persistence layer in the codebase (`user://skilltree.cfg`), coalesced to ≤1 write/frame and
  written atomically. New `SkillTreeScreen` + title-screen entry; `Tower.RangeCells` int→float.
- **Level 4 multi-path** (PR #15) — two left spawns, converge/split/converge/split/converge weave into one base; all 3 enemy kinds every wave; multi-path refactor (`PathCells` → `Paths`). Merged 2026-09-20.
- **Armored enemy + laser tower + Level 3** (PR #13) — Armored enemy (14 HP, 5 flat armor, basic speed); Laser tower (3-cell range, 4 dps drain, ignores armor, no projectile, cost 10); swarm HP 3→5. Merged 2026-09-20.
- **Map viewport fit** (PR #12) — grid reshaped 20×20 → 20×14, stretch mode. Merged 2026-09-18.
- **Level 2 + swarm + cannon** (PR #8) — level select, winding path, swarm ring cluster, cannon tower (AoE). Merged 2026-09-16.
- **First playable level** (PR #4) — grid, enemy movement, arrow tower, waves, HUD, title/result screens. Merged 2026-07-10.
