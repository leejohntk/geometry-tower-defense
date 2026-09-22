---
name: current-feature
description: Active feature tracking — what's being worked on, state, branch, progress
metadata:
  type: project
---

# Current Feature

**Status:** implementing

**Feature:** tower-skill-tree-mechanics (Part 2 of 2) — implementer running
**Branch:** feature/tower-skill-tree-mechanics
**Spec:** `.claude/specs/tower-skill-tree-mechanics.md` (approved 2026-09-22)

Activates the 7 mechanic nodes Part 1 shipped disabled: Arrow Pierce + Crit Chance, Cannon
Cluster + Stun Chance, Laser Ignite (trunk) + Chain + Ramp-Up. Behavior-changing, not numbers.
Ships the first stun/burn status effects on `Enemy` and the first per-tick laser beam mechanics
(chain jumps at 60% falloff, dps ramp to 2.0× over 2s, ignite burn 2 dps × 2.0s ignoring armor).
Crit/stun/ignite rolls route through a single seeded source so they are unit-testable without
flakiness.

Spec + holdouts committed on the feature branch (`90cff40`). Part 1 merged as PR #18.

## Feature Queue

- (empty — serial execution, rule 7: human queues the next idea while this one runs)

## Recent Features

- **Tower skill tree framework** (part 1/2, PR #18, merged 2026-09-22) —
  persistent Skill Points (Basic 5 / swarm sub-unit 2 / Armored 8), 3 tower trees × 15 nodes,
  5 ranks at a flat 10 SP, 8 stat nodes buyable and 7 mechanic nodes staged greyed. First
  persistence layer in the codebase (`user://skilltree.cfg`), coalesced to ≤1 write/frame and
  written atomically. New `SkillTreeScreen` + title-screen entry; `Tower.RangeCells` int→float.
- **Level 4 multi-path** (PR #15) — two left spawns, converge/split/converge/split/converge weave into one base; all 3 enemy kinds every wave; multi-path refactor (`PathCells` → `Paths`). Merged 2026-09-20.
- **Armored enemy + laser tower + Level 3** (PR #13) — Armored enemy (14 HP, 5 flat armor, basic speed); Laser tower (3-cell range, 4 dps drain, ignores armor, no projectile, cost 10); swarm HP 3→5. Merged 2026-09-20.
- **Map viewport fit** (PR #12) — grid reshaped 20×20 → 20×14, stretch mode. Merged 2026-09-18.
- **Level 2 + swarm + cannon** (PR #8) — level select, winding path, swarm ring cluster, cannon tower (AoE). Merged 2026-09-16.
- **First playable level** (PR #4) — grid, enemy movement, arrow tower, waves, HUD, title/result screens. Merged 2026-07-10.
