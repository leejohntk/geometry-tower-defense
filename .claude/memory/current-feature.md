---
name: current-feature
description: Feature log — what shipped, gates, and outstanding follow-ups. Live phase lives in state.json
metadata:
  type: project
---

# Current Feature

**Live phase lives in `.claude/state.json`** — that file is the single source of truth for what is
active right now (and is deleted at merge, which means "idle"). This file records *history*: what
shipped, its gates, and open follow-ups. Do not track phase here — it goes stale the moment a PR
merges.

**Last feature:** tower-skill-tree-mechanics (Part 2 of 2) — all gates green, **merged as PR #19** (`149ccd4`)
**Branch:** `main` (feature branch deleted)
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

## Feature Queue

- (empty — serial execution, rule 7: human queues the next idea while this one runs)

## Post-Merge Follow-Up

- **Spec doc drift (part 1)** — the tower-skill-tree-framework spec's Description line claims Skill
  Points are awarded per kill, tiered by enemy kind; its own Currency Design section and the shipped
  code award them per level outcome (victory = `levelId * 2`, defeat = half). Correct that spec line.
- **Harness staleness — RESOLVED 2026-09-23 (distillation run #5).** This file used to carry its
  own `Status:` line (`awaiting_playtest` etc.), which was written pre-merge and therefore
  misreported after every merge — and was never surfaced anyway (`session-start.sh` does
  `head -5`, which shows only the frontmatter). The line is removed; `.claude/state.json` is the
  sole phase owner. Proposal P4 (a post-merge `chore/post-merge-*` branch to flip the status) was
  **rejected** — it would have created a stray PR after every feature merge to maintain a
  redundant line.

## Recent Features

- **Tower skill tree mechanics** (part 2/2, PR #19, merged) — activates the 7 hidden
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
