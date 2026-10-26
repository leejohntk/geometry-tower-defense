# Holdout Scenarios — level-5-progenitor

Implementer must never see this file. Run every scenario against the implementation after the
feature gates (build + tests + review) are green. A scenario passes only when every expected
observation is verified; "looks right" is not a pass.

## H1 — Level 5 topology

**Setup:** Resolve `Levels.Get(5)`.

**Steps:**
1. Read id, display name, `Paths`, `SpawnCells`, `BaseCell`, tower flags.
2. Validate each route with `LevelDefinition.IsPathConnectedAndInBounds`,
   `PathSelfIntersects`, `VerticalDirectionChangeCount`, and consecutive-corner axis alignment.

**Expected:**
- Id 5, three routes.
- Spawn cells exactly `(0,2)`, `(0,7)`, `(0,11)`.
- Every route connected, in-bounds 20×14, does not self-intersect.
- All routes end at base `(19,7)`.
- Shared final cells are deduped in the grid (no double-draw, no buildable shared cell).
- Cannon and Laser allowed.

## H2 — Wave composition and boss placement

**Steps:**
1. Read Level 5's five waves and resolve `TotalEnemies`.
2. Index the final wave's spawn kinds.
3. Count normal spawn events before and after the single `Progenitor`.

**Expected:**
- Five waves with totals 12, 15, 18, 21, 24 (boss included as one enemy).
- Final wave has exactly one `Progenitor`.
- Boss is not first and not last: at least two normal events before and after (spec orders it 7th
  of 14).
- Levels 1–4 wave totals unchanged from their current values.

## H3 — Boss base stats

**Setup:** Spawn or configure a Progenitor.

**Expected:**
- HP strictly greater than every normal enemy (Basic 10, Swarm 5, Armored 14).
- Armor 3; speed 1.4 cells/s (slower than 2.0).
- Diameter 96 px; collision radius 48.
- Coin drop 12.
- Reaching base costs exactly 1 player HP.

## H4 — Phase thresholds and escalation

**Steps:** Probe phase selection at and around 66% and 33% max HP; read interval and squad size
for each phase; inspect composition weights.

**Expected:**
- >66% → phase 1: 6.0 s, 3 adds, weights Basic 70 / Swarm 30 / Armored 0.
- Exactly 66% and down to but not including 33% → phase 2: 4.5 s, 4 adds,
  Basic 40 / Swarm 35 / Armored 25.
- ≤33% → phase 3: 3.0 s, 5 adds, Basic 15 / Swarm 40 / Armored 45.
- Interval strictly decreases and squad size strictly increases across phases.
- Armored weight strictly increases across phases.
- Crossing a threshold mid-countdown does not reset the countdown to the new full duration and
  does not trigger an immediate extra summon.

## H5 — Summon windup behavior

**Steps:**
1. Spawn a Progenitor with no other enemies.
2. Advance the boss update by the phase-1 interval minus epsilon, then epsilon.
3. During the 0.5 s windup, sample speed/movement and visible summon state.
4. Advance past the windup.

**Expected:**
- No squad before the first interval elapses.
- Windup lasts 0.5 s; movement speed is multiplied by 0.35 (it keeps moving, never freezes).
- Windup state is visually observable (pulse ring drawn / state exposed).
- Exactly one squad of the phase's size spawns at windup end.
- Summon timer resets and the next squad arrives one full interval later, adjusted for phase.
- Killing the boss during windup cancels the squad; none spawns.

## H6 — Summoned enemies: place ahead, stay on-path, pay nothing

**Steps:**
1. Put the boss mid-segment on route B, force a summon.
2. Record each add's path anchor, waypoint index, route, coin drop, and kind.
3. Advance the simulation until the adds reach the base.
4. Repeat with the boss near a corner so the lead point would cross the waypoint.

**Expected:**
- Every add starts on route B, ahead of the boss anchor along the route, within the lead/offset
  bounds, and closer to the base than the boss.
- No add starts behind the boss or off-path; near a corner the lead clamps and the add still
  follows the route to the base.
- Each add's coin drop is 0. Killing all adds awards zero coins.
- Each add's kind is one of Basic, Swarm, Armored; a Swarm slot is a single enemy, not a cluster.
- Adds do not orbit (static formation offset).

## H7 — Wave completion cannot fire early

**Setup:** Final wave with the boss and its summons alive.

**Steps:**
1. Kill every wave-defined spawn and the boss while at least one summoned add is alive.
2. Observe `WaveCompleted` / `AllWavesCompleted`.
3. Kill or leak the remaining adds.

**Expected:**
- No completion signal while a summoned add is alive.
- After the last add dies or reaches base, the wave completes exactly once and victory fires.
- Live-enemy count never goes negative.

## H8 — Summons stop on boss death or leak

**Steps:**
1. Force a summon, then kill the boss before the next interval.
2. Advance well past several intervals with surviving adds.
3. Repeat by letting the boss reach the base with 2 player HP remaining.

**Expected:**
- No new summons after boss death or leak.
- Existing adds continue walking and are counted until they die/leak.
- Boss leak costs 1 HP; remaining summons still cost 1 HP each if they leak.

## H9 — Pool reset

**Steps:** Spawn a Progenitor, run a summon windup partway, kill it, release to the pool, then
re-acquire the same instance as Basic, Swarm, and Armored.

**Expected:** Normal HP/speed/diameter/coin/armor/visual for each kind; no HP bar; no summon timer
or windup state; no zero-coin override; no leftover phase visuals.

## H10 — Seeded random composition

**Steps:** Inject a scripted/seeded roll source and force summons in each phase. Run the same seed
twice.

**Expected:**
- Same seed → identical composition sequence.
- Rolls only produce Basic/Swarm/Armored and never the boss.
- With many samples, phase 3 contains strictly more Armored than phase 1 in expectation (the
  weighting is real, not decorative).
- No out-of-range or null enemy is produced.

## H11 — Title screen and layout verifier

**Steps:**
1. Open the title screen.
2. Resolve each button in `Block_LevelMenu`: Level1–Level5 plus SkillTree.
3. Run `godot --headless --path . --script res://tools/layout_verifier.gd`.

**Expected:**
- Level 5 button resolves, is labeled "Level 5", emits level id 5 on press.
- All six buttons are inside the menu block, centered on the viewport's vertical axis, uniformly
  stacked, non-overlapping.
- Layout verifier enumerates Level5 (not silently omitting it) and reports `RESULT: PASS` with
  zero findings and at least as many assertions as before plus the new Level5 checks.

## H12 — Regression

**Steps:** Run `dotnet build --no-incremental`, `dotnet test`, and the layout verifier; diff
Levels 1–4 definitions and their expected enemy totals.

**Expected:**
- Build 0 warnings / 0 errors.
- All tests pass, 0 skipped without documented reason.
- Levels 1–4 routes, tower flags, and wave totals unchanged.
- Existing skill-tree, laser, cannon, swarm, and armor tests unchanged and passing.
- Layout verifier PASS.

## H13 — Soak / stability

**Setup:** Level 5 final wave, boss at phase 3, several towers firing.

**Steps:** Run headless for at least 2,000 frames (`godot --headless --quit-after 2000` or the
test harness equivalent), forcing repeated summon intervals.

**Expected:**
- No runtime errors, null references, or freed-instance access in debug output.
- No timer callback fires after game over/reset.
- Enemy pool reuse stays stable; concurrent enemies do not grow without bound after the boss dies.
- No per-frame allocation regression attributable to the boss path (spot-check with existing perf
  conventions if measurable).

## Holdout verdict format

Report each scenario as `Hn PASS` with the observed evidence (counts, timings, paths, coin
awards) or `Hn FAIL` with the exact observed vs expected values and the source location. A failed
holdout is Critical and blocks the PR until fixed or the spec is amended by the human.
