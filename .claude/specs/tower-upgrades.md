# Feature Spec: tower-upgrades

**Status:** approved (2026-10-27)
**Branch:** feature/tower-upgrades
**Date:** 2026-10-27

## Description

Adds in-level per-tower upgrades. Each placed tower starts at rank 0 and can be upgraded three times with coins (5 / 10 / 15). Every rank adds a small stat boost (+3% damage, +0.5 cell range, +3% fire rate). Upgrades are gated by hidden combat experience: a tower must earn enough kill XP before each rank's upgrade option appears. Kill XP is awarded per enemy kind and split between every tower that damaged the enemy, in proportion to effective damage dealt. At rank 2 → 3, instead of a third chevron, the upgrade panel offers the tower's two archetype branches (matching the skill-tree seed nodes). Choosing one spends the 15 coins, sets rank 3 permanently for that tower, and specializes the tower into that seed. Seed mechanics are inert until specialization: a rank 0–2 tower applies no seed effect even if the skill tree has ranks in it. At rank 3 only the chosen seed mechanic applies; the unchosen seed has no effect on that tower. Trunk nodes (damage, attack speed, range, laser ignite) still apply normally.

## Acceptance Criteria

### Ranks and costs

- [ ] A placed tower starts at rank 0 with 0 XP and no archetype.
- [ ] Rank 1 costs 5 coins, rank 2 costs 10 coins, rank 3 costs 15 coins. Coins are deducted exactly once per successful upgrade.
- [ ] Upgrades only happen during `GameState.Playing`. Game over / victory / level reset leaves towers at their rank, but towers do not persist across levels.
- [ ] At rank 0–2, clicking a selected tower's upgrade option raises its rank by exactly 1 when the XP gate and coin cost are satisfied.
- [ ] The upgrade is rejected with no state change when coins are insufficient, XP is below the next threshold, rank is already 3, or the tower is not part of the active game.

### Rank stat effects

- [ ] Damage multiplier = `1 + 0.03 × rank`, applied to the final skill-tree-modified base damage. Damage remains a float through `Enemy.TakeDamage` (no integer truncation before armor).
- [ ] Range = skill-tree range + `0.5 × rank` cells. This stacks additively with range trunk nodes and applies to all three tower types (including cannon, which has no range node).
- [ ] Fire rate: arrow/cannon fire interval = skill-tree-modified interval ÷ `(1 + 0.03 × rank)`. Laser has no interval, so its DPS = skill-tree-modified DPS × `(1 + 0.03 × rank)`.
- [ ] Rank 3 total (with no skill-tree ranks): arrow damage 10.9, range 5.5 cells, interval `1.5 / 1.09 ≈ 1.376s`; cannon damage 16.35, range 5.5 cells, interval `2.5 / 1.09 ≈ 2.294s`; laser DPS 4.36, range 4.5 cells.
- [ ] Rank bonuses are per tower instance, not global.

### XP attribution

- [ ] Every damage path credits the firing tower: arrow hits and pierced hits, cannon splash hits, laser primary drain, laser chain jumps, and laser ignite burn ticks.
- [ ] `Enemy.TakeDamage` accepts an optional source tower. Effective damage credited = `min(applied damage, HP remaining before the hit)`, so overkill is not credited.
- [ ] An enemy tracks credited damage per source tower for its current life. The tracking is cleared on pool reset and on every reconfigure, so a pooled enemy never carries credit into reuse.
- [ ] On death, enemy XP is split across contributing towers in proportion to credited damage: `towerXp += enemyXp × creditedDamage / totalCreditedDamage`. Shares sum to the enemy's XP value. No source means no XP (existing tests calling `TakeDamage` without a source still compile and behave normally).
- [ ] XP is only awarded on enemy death. An enemy that reaches the base awards no XP.
- [ ] XP per enemy kind: Basic 1, Swarm 1, Armored 3, Progenitor 10. Progenitor summons use their rolled kind's XP value (they still drop 0 coins).
- [ ] Tower XP is a float accumulator per tower. Fractional shares are kept, never rounded per hit.

### Hidden XP gate

- [ ] Cumulative XP thresholds: rank 1 = 5 XP, rank 2 = 15 XP, rank 3 = 30 XP.
- [ ] The next upgrade option is visible only when `tower.Xp >= threshold(rank + 1)` and `rank < 3`. Below the threshold the UI shows no progress bar, counter, percentage, tooltip, colour change, or any other XP hint.
- [ ] The XP gate is evaluated live: if the selected tower crosses a threshold while its panel is open, the upgrade option appears without requiring a reselect.
- [ ] Reaching a threshold does not auto-buy. The player must click.

### Upgrade UI

- [ ] Clicking a placed tower selects it (existing behaviour) and opens `Panel_TowerUpgrade`.
- [ ] The panel is positioned above the selected tower's cell, kept inside the battlefield bounds. For row-0 towers it opens below the cell so it never sits under the top bar.
- [ ] `Block_Rank` shows only the tower's name (its archetype name at rank 3). It does **not** repeat the current rank as chevrons or an emblem: the tiny tower badge already shows the current rank on the battlefield. No duplicate rank display exists inside the panel.
- [ ] When an upgrade is available, `Block_Upgrade` shows the target rank as a fixed-size vertical chevron stack plus the coin cost (rank 1 = one chevron, rank 2 = two chevrons). The button is disabled while coins are below the cost. This target-rank icon is the only chevron display in the panel.
- [ ] At rank 2, when 30 XP is reached, `Block_Upgrade` is replaced by `Block_ArchetypeChoice` with exactly two labelled cards. No three-chevron upgrade icon is ever shown.
- [ ] Selecting an archetype card spends 15 coins, sets rank 3, records the archetype, and permanently removes all further upgrade actions for that tower.
- [ ] At rank 3 the panel shows the chosen archetype's name and no upgrade/archetype controls. It does not repeat the archetype emblem; the top-right tower badge shows it.
- [ ] The panel closes on: clicking empty battlefield space, clicking another tower (the new tower's panel opens), entering placement mode, Escape, game over, victory, or level reset. It never lingers over a result screen.
- [ ] The panel consumes mouse input (`MouseFilter = Stop`) so clicking an upgrade action never also deselects the tower, toggles its range, or places a tower.
- [ ] The panel works for all three tower types and is rebuilt/refreshed when the selected tower's rank, XP gate, coins, or archetype changes.
- [ ] The existing range-indicator toggle on tower click still works alongside the panel.
- [ ] A placed tower shows a tiny current-rank badge at the top-right of its cell once rank ≥ 1, whether or not it is selected: a fixed-size vertical chevron stack for rank 1/2, and a tiny archetype emblem at rank 3. Rank 0 shows no badge. The badge is created/refreshed by the rank purchase itself, not by the next rank's XP gate opening, and never consumes mouse input.

### Archetypes

- [ ] Arrow options: **Pierce** (`arrow.pierce`) and **Crit** (`arrow.crit_chance`).
- [ ] Cannon options: **Cluster** (`cannon.cluster`) and **Stun** (`cannon.stun_chance`).
- [ ] Laser options: **Chain** (`laser.chain`) and **Ramp-Up** (`laser.ramp_up`).
- [ ] Before rank 3, **no seed mechanic applies**: even with skill-tree ranks in the seed nodes, a rank 0–2 tower has pierce 0, crit 0%, cluster 1 shell, stun 0%, chain 0 jumps, and ramp max 1.0×. Trunk nodes still apply.
- [ ] After rank 3, the chosen seed mechanic applies at effective rank `max(1, skill-tree rank of that seed)`. The unchosen seed mechanic contributes zero. Trunk nodes are unaffected.
  - Arrow/Pierce: pierce count `max(1, rank)`.
  - Arrow/Crit: crit chance `max(1, rank) × 0.08`.
  - Cannon/Cluster: shells `1 + max(1, rank)`.
  - Cannon/Stun: stun chance `max(1, rank) × 0.06`.
  - Laser/Chain: jumps `max(1, rank)`; unchosen ramp means max multiplier 1.0.
  - Laser/Ramp-Up: max dps multiplier `1 + max(1, rank) × 0.20`; unchosen chain means 0 jumps.
- [ ] Laser Ignite is a trunk node and continues to apply regardless of archetype.
- [ ] Example (maxed tree, arrow, choose Crit): 40% crit, 0 pierce, maxed Damage/Attack Speed/Range trunk bonuses still applied. Choose Pierce instead: 5 pierce, 0% crit, same trunk bonuses.
- [ ] Archetype choice is per tower and final. No respec, no refund.

### Visuals

- [ ] Chevrons and archetype emblems are procedural `Control.Draw*` geometry. No AI-generated or external art assets.
- [ ] Chevrons are drawn as thick V strokes, never as text caret characters. Rank icons stack the chevrons vertically and use one fixed icon box for 1, 2, or 3 chevrons; the stack is centred inside the box. The panel only uses this icon for the next-rank upgrade preview; the current rank lives on the tower badge.
- [ ] Rank chevrons use the tower's colour (arrow blue, cannon green, laser magenta); locked/disabled states are dimmed.
- [ ] The tower badge is a scaled-down version of the current-rank icon: chevrons for rank 1/2, the chosen archetype emblem for rank 3. It sits inside the top-right of the tower cell and must not intercept clicks.
- [ ] Archetype emblems are distinct geometric marks: Pierce = double chevron / arrow trail; Crit = four-point star; Cluster = three small circles; Stun = concentric ring; Chain = three linked nodes; Ramp-Up = rising wedge.
- [ ] At rank 3 the tower body itself changes per archetype; no separate base ring/emblem accent overlay is added. The base silhouette stays recognisable:
  - Arrow/Pierce: forked spearhead triangle (two narrow halves with a centre slit).
  - Arrow/Crit: base triangle with a four-point star inset at its centre.
  - Cannon/Cluster: body becomes three overlapping circles in a triangular cluster.
  - Cannon/Stun: body becomes a circle with concentric shockwave rings.
  - Laser/Chain: body keeps its circle and gains three linked rim nodes.
  - Laser/Ramp-Up: body keeps its circle and gains a rising wedge core.
  The tiny top-right badge still shows the chosen archetype emblem.
- [ ] Panel and emblems are readable at 1× scale and do not cover more than roughly one cell of the battlefield around the tower. The panel is a UI overlay, not world geometry.

## Edge Cases

- **XP threshold reached but coins short:** Upgrade option is visible but disabled; no XP hint beyond the option's presence. Buying after coins arrive works without reselecting.
- **Multiple towers damage one enemy:** XP is split by effective damage. Example: Basic (1 XP) takes 4 damage from an arrow and 6 from a cannon → arrow +0.4 XP, cannon +0.6 XP. Total 1.0.
- **Overkill:** A 10-damage hit on a 3-HP enemy credits 3, not 10. Split shares use credited damage only.
- **Killing blow by burn:** The burn's source laser is credited for the burn damage and receives its share even after contact has ended.
- **Killing blow by splash:** Every enemy hit inside the blast credits the cannon; the direct target is credited once, not twice.
- **Same tower hits an enemy multiple times:** Credits accumulate on one entry, never duplicate the tower.
- **Pooled enemy reuse:** After release and reacquire as any kind, previous credits and XP are gone. A boss reused as Basic cannot leak contributions.
- **Summoned adds:** Grant kind XP but still 0 coins. XP distribution follows the same damage-share rule.
- **Enemy reaches the base:** No XP is awarded to towers that damaged it.
- **Archetype with zero skill-tree ranks:** Effective seed rank is 1, so the chosen mechanic is always active at minimum strength.
- **Both seeds maxed in the skill tree:** Before rank 3 neither applies; after choosing one, the chosen seed applies at its effective rank and the other contributes nothing.
- **Rank 3 while both upgrade and archetype conditions are true:** Only the two archetype cards render. There is no generic rank-3 purchase.
- **Double click / repeated click on an upgrade action:** Coins and rank change once; the second click is a no-op because the panel has refreshed or the tower is maxed.
- **Game over or victory with the panel open:** Panel closes before the result overlay appears; no upgrade can be bought after play ends.
- **Placement mode:** Clicking a tower or the panel while placing a tower does not open or interact with the upgrade panel; placement rules win.
- **Level reset / returned to title:** All rank, XP, and archetype state is discarded with the towers.
- **Tower freed during a frame:** No dangling tower reference survives in enemy credit tracking because the level reset clears enemies too.

## Dependencies

- `Tower` + `ArrowTower`, `CannonTower`, `LaserTower` — rank, XP, archetype state; stat layering.
- `SkillStats` / `SkillMechanics` — final skill-modified stats feed the rank multipliers; archetype selects which seed applies.
- `Enemy` — source-aware `TakeDamage`, per-source damage credit, XP value, credit reset on pool/reconfigure.
- `Projectile`, `ArrowProjectile`, `CannonProjectile` — pass the source tower into damage.
- `GameManager` — upgrade transaction, coin deduction, XP distribution on `Destroyed`, reset cleanup.
- `Main` — tower selection already exists; wire panel open/close and Escape handling.
- `GameHUD` or a new `TowerUpgradePanel` Control — panel rendering/input.
- `GameConstants` — rank, cost, XP, and presentation constants.
- `SkillTreeCatalog` / `SkillTreeState` — archetype ↔ seed node mapping and rank reads.

## Constraints

- **Performance:** Damage crediting adds no per-frame allocation. Reuse a small per-enemy credit buffer or dictionary cleared on pool reset. XP split runs once per death. Panel redraws only on state change, not every frame.
- **Memory:** One small credit structure per pooled enemy; no new object pools required. Panel is one Control per selection.
- **Platform:** keyboard + mouse; panel must not rely on hover-only interactions.
- **Determinism:** All tests must be deterministic; no unseeded randomness added.

## Visual / Geometry Theme

- Caret/chevron rank icons: `^`-shaped strokes, stacked vertically-centre or in a row, one per target rank. Rank 0 = no filled chevrons.
- Archetype cards: geometric emblem + name + 25 coin cost, in the tower colour family, with a distinct selected/hover state.
- Rank-3 body variants: the tower's own procedural body silhouette changes per archetype (no separate accent ring/emblem overlay).
- Panel: dark translucent rounded rectangle matching HUD sidebar (`0.05, 0.05, 0.1`), thin separator, geometric sans-serif labels. No sprites.

**LAYOUT:** `Panel_TowerUpgrade` is docked to the selected tower — horizontally centred on the tower's cell and offset above it, or below it when the tower is in the top row, clamped inside the battlefield. Inside the panel, `Block_Rank` sits at the top, `Block_Upgrade` or `Block_ArchetypeChoice` sits directly beneath it. The two archetype cards in `Block_ArchetypeChoice` mirror each other about the panel's vertical centre line. Every group is wrapped in a named `Control` with `MouseFilter = Ignore` on the wrapper; the panel itself uses `Stop`. The panel may overlap world geometry but must not extend outside the play area or under the top bar.

## Out of Scope

- Tower selling, refunds, or respec of archetype choice.
- Persistent tower ranks across levels or sessions.
- New standalone tower types; archetypes are specializations of the existing three towers.
- Rebalancing existing skill-tree node costs or effects.
- Changes to the skill-tree screen layout beyond any necessary archetype labelling.
- Enemy XP visual feedback, level-up effects, or XP progress display of any kind.

## Constants (tunable — review before approve)

| Constant | Value | Notes |
|----------|-------|-------|
| `TowerMaxRank` | 3 | |
| `TowerUpgradeCostRank1` | 5 | coins |
| `TowerUpgradeCostRank2` | 10 | coins |
| `TowerUpgradeCostRank3` | 15 | coins |
| `TowerRankDamageBonusPerRank` | 0.03 | multiplier = 1 + 0.03 × rank |
| `TowerRankRangeBonusPerRank` | 0.5 | cells |
| `TowerRankFireRateBonusPerRank` | 0.03 | interval ÷ (1 + 0.03 × rank); laser dps × same |
| `TowerXpThresholdRank1` | 5 | cumulative XP |
| `TowerXpThresholdRank2` | 15 | cumulative XP |
| `TowerXpThresholdRank3` | 30 | cumulative XP |
| `EnemyXpBasic` | 1 | |
| `EnemyXpSwarm` | 1 | |
| `EnemyXpArmored` | 3 | |
| `EnemyXpProgenitor` | 10 | |
| `ArchetypeMinSeedRank` | 1 | effective rank = max(1, skill-tree rank) |
