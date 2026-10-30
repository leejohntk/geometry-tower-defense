---
name: current-feature
description: Feature log — what shipped, gates, and outstanding follow-ups. Live phase lives in state.json
metadata:
  type: project
---

# Current Feature

**Status:** awaiting playtest — visual-overhaul complete, PR open. Live phase in
`.claude/state.json`; spec `.claude/specs/visual-overhaul.md` (approved 2026-10-30).
Branch `feature/visual-overhaul`.

**Live phase lives in `.claude/state.json`** — that file is the single source of truth for what is
active right now (and is deleted at merge, which means "idle"). This file records *history*: what
shipped, its gates, and open follow-ups. Do not track phase here — it goes stale the moment a PR
merges.

**Last feature:** tower-upgrades — in-level ranks, hidden XP gates, archetype specialization (PR #29, merged 2026-10-28)
**Branch:** `feature/tower-upgrades` (merged, deleted) — squash `5db78bb`
**Spec:** `.claude/specs/tower-upgrades.md` (approved 2026-10-27)

Three coin-priced ranks per placed tower (5 / 10 / 15) with +3% damage, +0.5 cell range and
+3% fire rate per rank. Each rank is gated by hidden kill XP, split across every tower that
damaged the enemy in proportion to effective damage. Rank 3 is an archetype choice between the
tower's two skill-tree seed nodes: the chosen seed applies at minimum rank 1, the unchosen seed
contributes nothing. Rank, XP and archetype live on the tower instance and never persist.

**Gates (2026-10-27):** build 0 warnings; 373 tests pass; layout verifier PASS (142 assertions,
0 findings); holdouts 16/16. Security lens 1 WARNING (pooled enemies retained tower refs) fixed
via `Enemy.ClearAttribution` on every release path; 6 performance INFO items (credit lookup,
panel style/icon allocations, polling) left as follow-ups; correctness reviewer agents stalled
twice on harness/API errors, so the orchestrator completed the correctness audit.

**Playtest fix (2026-10-27):** seed mechanics were still active at ranks 0–2 via the
skill tree. They are now inert until rank 3: pierce/crit, cluster/stun, and chain/ramp
read zero on ranks 0–2 even with maxed seed nodes; the chosen seed applies only after
the archetype choice. Trunk nodes are unchanged. Spec and holdout H12 updated.

**Playtest fix 2 (2026-10-27):** rank icons now use one fixed-size 16x30 box with a
vertically centred stack of drawn V chevrons (no text glyphs). A tiny current-rank
badge sits at the tower cell's top-right once rank ≥ 1 (chevrons at rank 1/2,
archetype emblem at rank 3); it appears on purchase, not when the next XP gate opens,
and is visible without selecting the tower. Spec and holdouts H13/H17 updated.

**Playtest fix 3 (2026-10-27):** removed the generic rank-3 base ring/emblem accent.
The tower body silhouette now changes per archetype: Arrow forked spearhead (Pierce)
or star inset (Crit), Cannon lobed cluster (Cluster) or shockwave rings (Stun), Laser
linked rim nodes (Chain) or rising wedge core (Ramp-Up). The tiny badge remains. Spec
and holdout H18 updated.

**Playtest fix 4 (2026-10-27):** panel header is name-only; current-rank chevrons/emblem
removed because the tower badge already shows rank. Only the next-rank preview keeps
the fixed-size vertical chevron stack. Costs lowered to 5 / 10 / 15. Spec and holdouts
H1/H10/H13 updated.

**Playtest fix 5 (2026-10-28):** panel is now a single action with no header. Generic
upgrade = one full-width `Upgrade {cost}` button; rank 2 = two archetype cards; no
panel at all when no upgrade is available (closed XP gate or rank 3). Selection still
shows range; panel appears live if the gate opens. Spec and holdouts H4/H10/H13/H14
updated.

**Merged (2026-10-28):** PR #29 squash `5db78bb`; build 0 warnings / 0 errors, 384 tests pass,
layout verifier PASS, holdouts 16/16. Local and remote feature branches deleted; `state.json`
removed; post-merge `main` re-verified (build clean, 384 tests). Holdouts moved to
`.claude/holdouts/regression/tower-upgrades/`.

**Open follow-ups:** the spec's double-click edge case is worded as "second click is a no-op",
but each valid click buys one rank — a second click can legitimately buy the next rank when its
gate is already open (a challenge at 15 XP bought rank 2). The layout verifier has no assertions
for the new `Panel_TowerUpgrade` groups yet (advisory gate only).

**Previous feature:** level-5-progenitor — three-route Level 5 + The Progenitor boss
**Branch:** `feature/level-5-progenitor`
**Spec:** `.claude/specs/level-5-progenitor.md` (approved 2026-10-25)

Adds Level 5: three simultaneous left-entering lanes (top/middle/bottom) that share only the
final run into the base, with pressure from simultaneous paths rather than a long lane that
lets towers re-hit enemies. Waves escalate 12/15/18/21/24 enemies and lean on Swarm clusters.
The final wave is 14 spawn events; **The Progenitor** is event 7, so it arrives mid-wave.

The Progenitor walks the middle route: 250 HP, 3 armor, speed 1.4, 64 px octagon, geometric HP
bar with 66%/33% ticks. Every interval it slows to 35% speed for 0.5 s, pulses a ring, and
releases a mixed squad directly ahead of itself on the path. Three HP phases tighten cadence
(3/2.25/1.5 s), grow the squad (3/4/5), and shift the mix toward Armored (70/30/0 → 40/35/25 →
15/40/45). Summons drop zero coins, count toward the wave's live total, and stop on boss death
or base arrival. Boss leak costs 1 HP.

Gates: build 0 warnings / 0 errors · 261 tests pass, 0 skipped · layout verifier PASS (142
assertions, 144 evaluated, 2 skipped) · review 1 CRITICAL + 4 WARNING + 2 INFO, all fixed
(critical: summons could spawn after the boss reached the base; warnings: game-over kept
spawning, victory could overwrite defeat, pool prewarm too small, test bypassed the production
tick) · holdouts 13/13 PASS · simplify 9 cleanups applied, 14 declined (cross-cutting refactors
deferred).

Holdouts: `.claude/holdouts/regression/level-5-progenitor/scenarios.md`.

Known limitations: one tracked boss per level (a second Progenitor would silently be ignored);
route selection for non-round-robin spawns is still a WaveManager branch; summon spawning uses
a second pipeline beside `EnemySpawned`. Balance is test-verified, not playtested.

Playtest 1 (2026-10-25): boss read as too large and never visibly summoned. Fix batch reduced
diameter 96 → 72 px and cadence 6/4.5/3 → 3/2.25/1.5 s (first summon now at 3.5 s); added a
cadence pin test. Spec + holdouts amended to match.

Playtest 2 (2026-10-25): boss HP raised 120 → 250 so it survives long enough to summon through
its phases (thresholds now 166 / 83 HP); tests re-pinned.

Playtest 3 (2026-10-25): boss still read slightly large; diameter 72 → 64 px (collision radius
32). No other tuning changed.

**Earlier feature:** layout-verifier — headless pre-playtest layout verification
**Branch:** `feature/layout-verifier`
**Spec:** `.claude/specs/layout-verifier.md` (approved 2026-10-19, AC-11 amended 2026-10-19)

Adds `tools/layout_verifier.gd`, a headless `SceneTree` script that loads `Main.tscn`,
walks the Control tree and evaluates 137 layout assertions (centering, alignment, mirroring,
containment, stacking) across the title, skill-tree, HUD and result screens. Introduces the
named-visual-group convention (`Row_{id}`, `Block_{name}`, `Panel_{name}`) so groups resolve by
identity rather than generated node names, and a `LAYOUT:` field in the spec template.

Review caught what would otherwise have shipped broken: three save-safety issues (verification
hooks could write the real save if isolation regressed — now triple-gated behind a debug-build
check, explicit enablement and an active-state query), and three tautological assertions that
could not fail (sidebar panel compared to the block it was sized from, seed mirror measured on
wrappers instead of rendered children, header centering algebraic on a copied width).

Spec change: AC-11 amended from eight to seven layout-observable defect classes. SP-award rework
has no rendered manifestation and is carried as an accepted coverage gap, not silently dropped.

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

- **Progenitor architecture (deferred from /simplify)** — revisit when a second boss or summoner
  ships: wave spawn routing is still a kind branch in `WaveManager.SpawnNext`; `GameManager`
  tracks a single boss reference, so two Progenitors would silently tick only the last; summons
  use a second spawn pipeline beside the `EnemySpawned` seam. Each is a design refactor, not a
  bug.
- **Progenitor balance** — HP 250 / armor 3 / phase cadences are test-verified only; tune after
  the human playtests Level 5.
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

- **Visual overhaul** (PR pending, 2026-10-30) — full procedural visual pass with no
  gameplay changes: per-kind enemy silhouettes + transform idle motion + hit feedback +
  pooled death bursts; turret aim/recoil, arrow orientation fix and trails, richer
  explosion, laser impact glow; spawn portals + base core replace debug markers/labels;
  placement icon cards; skill-tree backdrop/glyphs/halos + bottom hover description band
  with 15 tuning-accurate descriptions; animated title and distinct victory/defeat
  backdrops with styled buttons. Build 0 warnings, 499 tests, layout verifier 214/0,
  holdouts 23/23. Review found one CRITICAL (status tint repainted only the overlay
  container, so body-layer flashes/boss phase border went stale) plus 4 WARNING and 5
  INFO; all fixed in one batch. Simplify pass then removed dead code and consolidated
  button/ornament/test-support duplication.
- **Tower upgrades** (PR #29, merged 2026-10-28) — three coin-priced ranks per placed tower
  (5/10/15), each gated by hidden kill XP split by effective damage; rank 3 picks one of the
  tower's two skill-tree seed nodes, applied at minimum rank 1. Rank/XP/archetype live on the
  tower instance, never persisted. Five playtest rounds produced the final form: seed mechanics
  inert until rank 3, fixed-size drawn V-chevron stacks (no text glyphs), a tiny rank badge on
  the tower at rank ≥ 1, per-archetype body silhouettes, and a single-action panel (one Upgrade
  button, two archetype cards at rank 2, no panel when nothing is available). Build 0 warnings,
  384 tests, holdouts 16/16. Correctness lens stalled twice and was self-audited — see run #8.
- **Level 5 + The Progenitor** (PR #25, merged 2026-10-25) — three simultaneous lanes, waves 12/15/18/21/24,
  first boss mid-wave. Progenitor slows and summons ahead on-path with HP-phase escalation;
  zero-coin summons count toward wave completion. Review caught a critical post-base summon
  leak (synthetic exit waypoint) plus game-over/victory races; all fixed with regression tests.
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
