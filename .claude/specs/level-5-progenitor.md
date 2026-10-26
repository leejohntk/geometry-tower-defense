# Feature Spec: level-5-progenitor

**Status:** draft
**Branch:** feature/level-5-progenitor
**Date:** 2026-10-25

## Description

Adds Level 5 — a three-route pressure level — and the game's first boss, **The Progenitor**.

Three short, direct lanes spawn enemies simultaneously and converge on the same base. Difficulty
comes from simultaneous pressure rather than a long winding lane: a long lane gives towers more
time to re-hit the same enemies, which the human explicitly does not want. Waves grow larger than
Level 4's and send more Swarm clusters.

The Progenitor appears **mid-wave**, not at the start, so the player is already fighting and cannot
set up in a prepared state. It walks the middle route. Every interval it enters a 0.5-second
summon state: its movement slows (it does not stop), a ring pulses, and a group of mixed random
enemies spawns directly ahead of it on the same route. Summoned enemies drop **zero coins** and
count toward the wave's live-enemy total. The boss itself drops a large coin reward.

Summoning escalates with the boss's HP in three phases: faster cadence, larger squads, and a
stronger enemy mix as HP falls.

## Acceptance Criteria

- [ ] Level 5 has exactly three routes; each is axis-aligned, connected, in-bounds in the 20×14
      grid, non-self-intersecting, and ends at the shared base cell `(19,7)`.
- [ ] Spawn cells are `(0,2)`, `(0,7)`, `(0,11)`; round-robin spawn events are distributed across
      all three routes.
- [ ] Level 5 waves escalate in total enemy count (12, 15, 18, 21, 24 including the boss).
- [ ] The final wave contains exactly one `Progenitor` spawn, positioned mid-wave: at least two
      normal spawn events before it and at least two after it.
- [ ] The Progenitor always enters on the middle route (route index 1), independent of the
      round-robin counter.
- [ ] `ProgenitorHP` exceeds every normal enemy's HP; the boss is slower than normal enemies.
- [ ] The boss summons only after its first interval elapses; it does not summon on spawn.
- [ ] At each summon interval the boss enters a 0.5 s windup: movement speed is multiplied by the
      slow factor, a visible pulse ring appears, and at the end of the windup a squad spawns.
- [ ] Summoned enemies spawn ahead of the boss on its current route, continue toward the base, and
      count toward the wave's live-enemy total.
- [ ] Summoned enemies drop 0 coins; the boss drops `ProgenitorCoinDrop` coins.
- [ ] Phase cadence is 6.0 s / 4.5 s / 3.0 s, squad size 3 / 4 / 5, phase thresholds at 66% and
      33% max HP, with exact threshold HP belonging to the lower (harder) phase.
- [ ] Summon mix shifts toward tougher enemies by phase (weights below); every summoned slot
      produces exactly one enemy (a Swarm slot is one small circle, not a 3-member cluster).
- [ ] Summon composition is driven by an injectable seeded roll source so tests can script exact
      sequences; production uses a seeded default.
- [ ] The final wave cannot complete and `AllWavesCompleted` cannot fire while any summoned enemy
      is alive, even if every wave-defined spawn and the boss are dead.
- [ ] Boss death or reaching the base stops all future summons; already-spawned summons keep
      walking and are still counted.
- [ ] Pooled bosses reset fully (phase, summon timer, windup, coin override, HP bar) when reused
      as a normal enemy.
- [ ] The Progenitor displays a geometric HP bar above itself with ticks at the 66% and 33%
      phase thresholds; border color shifts by phase.
- [ ] Levels 1–4 are unchanged: same routes, same wave counts, same behavior.
- [ ] Title screen shows a Level 5 button; all six menu buttons remain inside `Block_LevelMenu`
      with uniform stacking gaps, and the layout verifier passes with Level 5 enumerated.
- [ ] `dotnet build` exits 0 with no warnings; `dotnet test` all pass.

## The Progenitor

### Stats (tunable — review before approve)

| Constant | Value | Notes |
|----------|-------|-------|
| `ProgenitorHP` | 120 | Highest in the game (Armored is 14) |
| `ProgenitorArmor` | 3 | Flat reduction; Laser still ignores armor |
| `ProgenitorSpeed` | 1.4 cells/s | Normal enemies are 2.0 |
| `ProgenitorDiameter` | 96 px | Basic/Armored are 48, Swarm 24 |
| `ProgenitorCoinDrop` | 12 | Boss payoff |
| `ProgenitorBaseDamage` | 1 | Same leak cost as any enemy (tunable) |
| `ProgenitorSummonWindup` | 0.5 s | Slow state + pulse ring |
| `ProgenitorSummonSlowFactor` | 0.35 | Speed multiplier during windup |
| `ProgenitorSummonLeadDistance` | 100 px | Adds spawn this far ahead of the boss anchor |
| `ProgenitorSummonOffsetRadius` | 20 px | Ring spread of adds around the lead anchor |

### Phases

| Phase | HP range | Summon interval | Squad size | Mix (Basic / Swarm / Armored) |
|-------|----------|-----------------|------------|-------------------------------|
| 1 | > 66% max | 6.0 s | 3 | 70% / 30% / 0% |
| 2 | 33%–66% max | 4.5 s | 4 | 40% / 35% / 25% |
| 3 | ≤ 33% max | 3.0 s | 5 | 15% / 40% / 45% |

- Summon timer starts at spawn, counts down to the first interval, then resets after each summon.
- Entering a new phase applies the new interval to the current countdown without resetting it to the
  new full duration (no sudden extra wait or instant double-summon at a threshold).
- Exact threshold HP (e.g. `floor(maxHP * 2 / 3)`) belongs to the lower phase.

### Summon placement and behavior

- At windup end, each squad member is acquired from the existing enemy pool, configured by its
  rolled kind, placed on the boss's route just ahead of the boss's current anchor, given the boss's
  current waypoint index as its starting progress, and released into the normal enemy lifecycle.
- If the lead point would cross a waypoint, clamp to that waypoint (the add still continues along
  the route; it must never cut a corner or jump off-path).
- Adds use a static ring offset (`GenerateClusterOffsets`) with zero orbit speed; they do not form
  an orbiting swarm cluster.
- Summoned enemies have `CoinDrop = 0` and cannot be farmed.
- Summons join the wave's live count and emit the same reached-end/destroyed lifecycle signals as
  wave-spawned enemies.

## Level 5 Definition

Base: `(19,7)`. Spawns: route A `(0,2)`, route B `(0,7)`, route C `(0,11)`.

Route A (top, 29 cells):
```
(0,2) (4,2) (4,5) (9,5) (9,3) (14,3) (14,7) (19,7)
```

Route B (middle, 20 cells — straight lane):
```
(0,7) (19,7)
```

Route C (bottom, 28 cells):
```
(0,11) (4,11) (4,8) (9,8) (9,10) (14,10) (14,7) (19,7)
```

Routes share the final `(14,7)→(19,7)` run; `GridManager` draws shared cells once. All three
routes are shorter and more direct than Level 4's weave — pressure comes from three simultaneous
lanes.

Waves (spawn events in order; `P` = Progenitor):

1. 4 Basic + 2 SwarmCluster + 2 Armored — 12 enemies
2. 4 Basic + 3 SwarmCluster + 2 Armored — 15 enemies
3. 3 Basic + 4 SwarmCluster + 3 Armored — 18 enemies
4. 2 Basic + 5 SwarmCluster + 4 Armored — 21 enemies
5. 2 Basic + 2 SwarmCluster + 2 Armored + **P** + 3 SwarmCluster + 2 Armored + 2 Basic — 24 enemies

In wave 5 the boss is the 7th of 14 spawn events, with six normal events before it and seven after.
`AllowCannonTower: true`, `AllowLaserTower: true`.

## Edge Cases

- **Boss dies mid-windup:** windup is cancelled, no squad spawns, pulse ring stops.
- **Boss reaches base:** costs `ProgenitorBaseDamage` player HP; no further summons; existing
  summons continue and remain counted.
- **Boss killed while summons alive:** wave stays active until the last summon dies or leaks;
  victory cannot fire early.
- **Game over / level reset while summons alive:** spawning and boss timers stop; pooled enemies
  are released safely; no timer callback after reset.
- **Phase threshold crossed during windup:** windup finishes with its rolled composition; the next
  summon uses the new phase.
- **Corner during summon:** lead anchor clamps inside the current segment; adds stay on-path.
- **Pool reuse:** a boss released and reused as Basic/Armored/Swarm has normal stats, coin drop,
  no HP bar, no summon state.
- **Three routes share cells:** grid drawing dedupes shared cells; buildable tiles exclude every
  route's cells.
- **Round-robin and boss:** the forced middle-route boss spawn does not advance the round-robin
  counter.
- **Level 1–4 regression:** single/two-route levels keep their existing route indices and wave
  counts.

## Dependencies

- `GameConstants` — Progenitor stats, phase constants, mix weights, base damage.
- `EnemyKind` / `SpawnKind` — add `Progenitor`.
- `WaveDefinition.Resolve` — `SpawnKind.Progenitor → (EnemyKind.Progenitor, 1)`.
- `Enemy` — Progenitor configuration/visual, HP bar, summon state, path-progress accessors,
  zero-coin override, full pool reset.
- `WaveManager` — forced middle-route boss spawn; summon spawn-at-progress entry; live-count
  increment for summons.
- `GameManager` — boss update/summon wiring, summon lifecycle, stop-on-reset.
- `Levels` — `Level5`, `Get(5)`.
- `TitleScreen` — Level 5 button; six-button stack must still fit the viewport.
- `tools/layout_verifier.gd` — enumerate `Level5` in the title menu checks.
- Tests — pure phase/composition rules, route/wave validation, boss lifecycle, pool reset.

## Constraints

- **Performance:** no per-frame allocation in the boss path. Composition uses a constant weight
  table; summons reuse the existing enemy pool and WaveManager lifecycle. Boss adds no per-frame
  scan of the enemy list.
- **Determinism:** summon rolls go through one injectable seeded source, matching the existing
  `SkillRandom` pattern; tests script exact sequences without flakiness.
- **Backward compatibility:** adding enum members must not change existing enemy/wave behavior or
  serialized save state.
- **Platform:** keyboard + mouse, same as existing.

## Visual / Geometry Theme

- **Progenitor:** large octagon (96 px), deep magenta fill, thick border. Phase border colors shift
  white → amber → red as HP falls. A pulsing ring expands during the 0.5 s summon windup so the
  summon is telegraphed.
- **HP bar:** thin geometric bar above the octagon, with tick marks at 66% and 33%; fill uses the
  current phase border color.
- **Summoned enemies:** existing Basic circle / Swarm small circle / Armored diamond; zero-coin
  kills show no coin award.
- No AI-generated assets — procedural geometry only.

**LAYOUT:** In-game boss visuals are dynamic and outside the headless layout verifier's reach; no
new named UI groups are introduced. On the title screen, `Block_LevelMenu` keeps every button
centered on the viewport's vertical axis, inside the block, with uniform stack gaps; adding
`Level5` must not cause overlap with `SkillTree` or push any button outside the block/viewport.

## Out of Scope

- Level locking/unlock progression; Level 5 remains directly selectable like Levels 1–4.
- Boss health persistence, boss-specific music/sound, cutscenes, or death animations beyond the
  shape disappearing.
- Additional Progenitor abilities (shielding, healing, tower disabling, enrage beyond the summons).
- Guaranteed variety inside a summon squad — the mix is weighted random, not shuffled.
- Changes to tower targeting (towers still target the nearest in-range enemy, so adds can body
  block for the boss by design).
- Balance beyond the initial constants; tuning after playtest is a follow-up.
