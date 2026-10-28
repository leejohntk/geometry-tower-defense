# Holdout Scenarios: tower-upgrades

Implementer must NOT see this file. Orchestrator runs these during verification.

## H1 — Upgrade costs and transaction safety
- Rank 0 → 1 deducts exactly 5 coins. Rank 1 → 2 deducts exactly 10. Rank 2 → 3 (archetype pick) deducts exactly 15.
- Clicking an upgrade twice in one frame / repeated calls: only one coin deduction, one rank increase.
- With 4 coins and 5 XP, the rank-1 upgrade option is present but disabled and leaves rank 0 and coins 4.
- Upgrade attempts during `GameOver` or `Victory` do nothing.

## H2 — Rank 3 stat math, no skill tree
All asserted on a fresh `SkillTreeState` with 0 ranks.
- Arrow rank 3: effective damage 10.9; range 5.5 cells (352 px); fire interval `1.5 / 1.09` within 1e-4.
- Cannon rank 3: effective damage 16.35; range 5.5 cells; interval `2.5 / 1.09` within 1e-4.
- Laser rank 3: DPS 4.36; range 4.5 cells.
- Damage is float through armor: Arrow rank 3 hit on Armored (5 armor) reduces HP 14 → 8.1, not 8.
- Cannon rank 3 splash on Armored reduces HP by 11.35, not 11.
- Laser rank 3 kills a 14-HP Armored enemy in `14 / 4.36 ≈ 3.211s` (ignoreArmor drain), not the pre-upgrade 3.5s.

## H3 — Rank stacking with a maxed skill tree
Set every node to rank 5.
- Arrow rank 3 damage = `(10 + 2×5) × 1.09 = 21.8`.
- Arrow rank 3 fire interval = `(1.5 / 1.5) / 1.09 = 0.91743…`.
- Arrow rank 3 range = `4 + 2.5 + 1.5 = 8.0` cells.
- Cannon rank 3 damage = `(15 + 1×5) × 1.09 = 21.8`.
- Cannon rank 3 splash radius = `64 + 8×5 = 104` px (rank bonus must not double-apply to splash).
- Laser rank 3 DPS = `(4 + 0.8×5) × 1.09 = 8.72`.

## H4 — Hidden XP gate
- Rank 0 tower with 4.999 XP shows no upgrade action anywhere in its panel: no button, progress bar, label, percentage, tooltip, or colour cue.
- At exactly 5 XP the rank-1 upgrade action appears while the panel is already open, with no reselect.
- 15 XP unlocks rank 2; 30 XP unlocks the rank-3 archetype choice.
- XP is cumulative: a tower with 15 XP can buy rank 1 and rank 2 back to back (coins permitting), and after rank 2 still has 15 XP toward the 30 threshold.
- No public UI text contains "XP", "exp", or an XP count at any rank.

## H5 — Damage credit, single tower and overkill
- Arrow deals 4 damage to a Basic (10 HP), cannon deals the remaining 6 and kills it: in a two-tower scenario, arrow earns 0.4 XP and cannon 0.6 XP.
- A 10-damage arrow hits a Basic with 3 HP: credited damage is 3 and the arrow earns 1.0 XP (Basic XP = 1), not 10/3 or more.
- Multiple hits from the same tower credit one accumulator, not duplicate entries; total credited never exceeds the enemy's max HP.

## H6 — XP split proportions
- Armored (3 XP) takes 5 credited damage from tower A and 9 from tower B (14 HP total): A +`3×5/14 = 1.0714…`, B +`3×9/14 = 1.9286…`, sum 3.0 within 1e-6.
- Progenitor (10 XP) split 40/60: +4.0 / +6.0.
- Swarm XP is 1; Basic XP is 1.
- A tower that damaged an enemy but the enemy reached the base earns 0 XP.

## H7 — Every damage path credits the source tower
- Pierced arrow: each enemy passed through credits the arrow tower for effective damage on that enemy.
- Cannon splash: every enemy damaged inside the blast credits the cannon tower; the directly struck enemy is credited once.
- Laser primary drain credits the laser.
- Laser chain jumps credit the same laser for each jump's damage.
- Laser ignite burn ticks credit the laser, including a burn kill that lands after contact ended (no other tower touched the enemy → laser earns full XP).
- Burn damage that is absorbed with 0 applied damage (e.g. already dead guard) credits nothing.

## H8 — Pooled enemy never leaks credit
- Enemy takes damage from tower A, is killed/released, reacquired and configured as Basic: its contribution table is empty and any kill awards XP only to towers that damage it in the new life.
- Boss → pool → reacquire as Basic and as Progenitor: no stale contributions, no stale XP value.
- A tower reference retained from a prior life cannot receive XP for the reused enemy.

## H9 — Summons award XP, zero coins
- Progenitor summon of each kind awards that kind's XP value on death (Basic 1, Swarm 1, Armored 3) and still drops 0 coins.
- XP split follows the normal damage-share rule.

## H10 — Archetype purchase flow
- At rank 2 with 30 XP, the panel shows exactly two archetype cards and no third-chevron upgrade icon.
- Arrow cards are Pierce and Crit; Cannon cards are Cluster and Stun; Laser cards are Chain and Ramp-Up.
- Clicking a card deducts 15 coins, sets rank 3, records the archetype, and continues to show the chosen archetype name in the panel with no further upgrade or card controls. The top-right tower badge shows the chosen emblem.
- Clicking one card twice / both cards in one frame results in exactly one purchase and one archetype.
- With 14 coins the cards are visible but not purchasable.

## H11 — Chosen seed only, minimum rank 1
Fresh skill tree (all seed ranks 0):
- Arrow + Crit: crit chance 8%, pierce 0.
- Arrow + Pierce: pierce 1, crit 0%.
- Cannon + Cluster: 2 shells, stun 0%.
- Cannon + Stun: 1 shell, stun 6%.
- Laser + Chain: 1 jump, ramp max multiplier 1.0.
- Laser + Ramp-Up: 0 jumps, ramp max multiplier 1.2.
Maxed skill tree (all ranks 5):
- Arrow + Crit: crit 40%, pierce 0. Arrow + Pierce: pierce 5, crit 0%.
- Cannon + Cluster: 6 shells, stun 0%. Cannon + Stun: 1 shell, stun 30%.
- Laser + Chain: 5 jumps, ramp max 1.0. Laser + Ramp-Up: 0 jumps, ramp max 2.0.
- Trunk nodes still apply: maxed Arrow + Crit keeps 21.8 damage, 0.917s interval, 8.0 range; maxed Laser + Chain keeps DPS 8.72 and Ignite at rank 5.

## H12 — Pre-rank-3 seed mechanics are inert
- Rank 0, 1, and 2 towers with every seed node maxed still have: pierce 0, crit 0%, cluster exactly 1 shell, stun 0%, chain 0 jumps, ramp max multiplier 1.0×.
- Trunk nodes still apply at ranks 0–2: maxed Damage / Attack Speed / Range on Arrow; Powder Charge / Attack Speed / Splash Radius on Cannon; DPS / Range / Ignite on Laser.
- Seed ranks are not deleted or consumed; once the tower reaches rank 3 and chooses a branch, the chosen seed applies at `max(1, skill-tree rank)`.
- The archetype gate only opens the chosen seed; the unchosen seed remains zero at rank 3.

## H13 — Upgrade preview icons; no current-rank duplication
- The panel never shows the tower's current rank as chevrons or an emblem; `Block_Rank` contains only the tower/archetype name. The top-right tower badge is the current-rank display.
- When a rank-1/2 upgrade is available, `Block_Upgrade/Chevrons` shows exactly the target rank: one chevron for target rank 1, two for target rank 2.
- The target-rank icon container has the same fixed size for 1 and 2 chevrons; the stack is centred vertically, every visible chevron shares x, and later chevrons have greater y.
- Chevrons are drawn V strokes, not text caret characters.
- Rank 2 with 30 XP: two archetype emblems/cards, never a three-chevron upgrade icon.
- Rank 3 panel: chosen archetype name only, zero chevrons, zero emblem, zero upgrade buttons; the badge shows the emblem.
- Chevron and emblem nodes are named groups (`Block_Rank`, `Block_Upgrade`, `Block_ArchetypeChoice`) with stable child names; no generated `@Label@…` names appear in findings.
- All icons are drawn procedurally; no new external or AI-generated asset files exist in the repo.

## H17 — Tiny tower rank badge
- A rank-0 tower has no rank badge.
- A rank-0 tower whose rank-1 XP gate is open (5 XP) but has not purchased still has no badge — the badge follows the purchase, not availability.
- After buying rank 1, the tower shows a tiny badge at the top-right of its cell (`x > 0`, `y < 0` in tower-local coordinates), visible with no panel open.
- After buying rank 2, the badge is still present and represents rank 2.
- After choosing an archetype at rank 3, the badge shows the chosen archetype emblem instead of chevrons.
- The badge has `MouseFilter = Ignore`, uses a fixed tiny icon size at rank 1 and rank 2, and never blocks tower selection or panel input.
- The badge is present whether the tower is selected or not, and survives opening/closing the upgrade panel.

## H18 — Rank-3 body variants replace the base accent
- No tower ever gains an `ArchetypeAccent` child; the old base ring/emblem overlay is gone.
- Rank 3 modifies the tower's own body drawing:
  - Arrow/Pierce: forked spearhead silhouette (two narrow triangle halves with a visible centre slit).
  - Arrow/Crit: triangle body with a four-point star at its centre.
  - Cannon/Cluster: three overlapping body circles in a triangular cluster, not one plain circle.
  - Cannon/Stun: body reads as concentric shockwave rings.
  - Laser/Chain: circle body with three linked rim nodes.
  - Laser/Ramp-Up: circle body with a rising wedge core.
- The three base silhouettes remain recognisable as Arrow (triangle), Cannon (circle/lobed), Laser (circle) at 1× scale.
- The tiny top-right badge remains visible at rank 3 and shows the chosen archetype emblem.
- Body variants are procedural geometry; no sprites or external assets.

## H14 — Panel input isolation
- Clicking an upgrade button does not deselect the tower, toggle its range indicator, place a tower, or start a wave.
- Clicking empty battlefield space closes the panel and deselects the tower.
- Clicking another tower closes the old panel and opens the new tower's panel.
- Escape closes the panel. Entering placement mode closes it.
- Panel is clamped inside the battlefield: row-0 tower → panel below; left/right edge columns → panel stays within play-area X bounds; never under the top bar.
- Panel closes before the Game Over / Victory overlay becomes visible; no upgrade succeeds after play ends.

## H15 — Reset and lifecycle
- Returning to title / starting a new level leaves no ranked or specialized tower from the previous run; the new level's towers start at rank 0 with 0 XP.
- A tower freed during `ResetGame` leaves no reference in any enemy credit table (reset clears enemies too; no exception on later damage/kill).
- No upgrade state persists through `user://skilltree.cfg` or any save file.

## H16 — Performance and pooling sanity
- XP credit bookkeeping allocates nothing per damage hit after an enemy's first credit entry (steady-state).
- Killing 20 enemies in one cannon volley frame does not throw collection-modified exceptions and distributes XP per enemy correctly.
- Panel redraws do not run every frame while open and idle (no per-frame `QueueRedraw` from the panel).
