---
name: current-feature
description: Active feature tracking — what's being worked on, state, branch, progress
metadata:
  type: project
---

# Current Feature

**Status:** implementing

**Feature:** tower-skill-tree-framework (Part 1 of 2)
**Branch:** feature/tower-skill-tree-framework
**Spec:** `.claude/specs/tower-skill-tree-framework.md` (approved 2026-09-21)

Persistent Skill Points (SP) currency earned per kill, plus the per-tower skill tree: 3 trees
(Arrow / Cannon / Laser), 15 nodes total, 5 ranks each, flat 10 SP per rank. Part 1 ships the
stat nodes (8 buyable) and renders the 7 mechanic nodes greyed ("coming soon"). Adds the first
persistence layer (`user://skilltree.cfg` via `ConfigFile`) and a title-screen `SkillTreeScreen`.
`Tower.RangeCells` widens from `int` to `float`.

## Feature Queue

- **tower-skill-tree-mechanics** (Part 2 of 2) — spec drafted at
  `.claude/specs/tower-skill-tree-mechanics.md`, status `draft`, awaiting human approval.
  Gated: its spec states Part 1 must merge first (serial execution, rule 7).

## Recent Features

- **Level 4 multi-path** (PR #15) — two left spawns, converge/split/converge/split/converge weave into one base; all 3 enemy kinds every wave; multi-path refactor (`PathCells` → `Paths`). Merged 2026-09-20.
- **Armored enemy + laser tower + Level 3** (PR #13) — Armored enemy (14 HP, 5 flat armor, basic speed); Laser tower (3-cell range, 4 dps drain, ignores armor, no projectile, cost 10); swarm HP 3→5. Merged 2026-09-20.
- **Map viewport fit** (PR #12) — grid reshaped 20×20 → 20×14, stretch mode. Merged 2026-09-18.
- **Level 2 + swarm + cannon** (PR #8) — level select, winding path, swarm ring cluster, cannon tower (AoE). Merged 2026-09-16.
- **First playable level** (PR #4) — grid, enemy movement, arrow tower, waves, HUD, title/result screens. Merged 2026-07-10.
