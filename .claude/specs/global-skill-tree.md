# Feature Spec: global-skill-tree

**Status:** draft
**Branch:** feature/global-skill-tree
**Date:** 2026-11-02

## Description

Adds a fourth, tower-independent branch to the persistent skill tree: a **Global** tab that
spends the same Skill Points (SP) as the Arrow/Cannon/Laser trees and grants permanent bonuses
to every run instead of one tower type. Four chained nodes, purchased in order, each at the
existing flat 10 SP per rank:

| Order | Node id | Display name | Effect per rank | Max ranks |
|-------|---------|--------------|-----------------|-----------|
| 1 | `global.starting_hp` | Fortified Core | +1 starting HP | 5 |
| 2 | `global.starting_gold` | War Chest | +2 starting gold | 5 |
| 3 | `global.tower_discount` | Master Blueprints | tower placement costs 1 less coin | 3 |
| 4 | `global.upgrade_discount` | Advanced Training | every tower rank upgrade costs 1 less coin | 3 |

Sequential unlock: node N becomes buyable once node N-1 reaches rank 1 (existing
`SkillUnlockRank`). Node 1 is always unlocked. Ranks persist in the existing
`user://skilltree.cfg` through `SkillTreeSave`; no new save file or currency.

## Acceptance Criteria

- [ ] Catalog defines the 4 global nodes with the ids, names, max ranks and per-rank effects in
      the table; `All` contains 19 nodes (15 tower + 4 global) and `ForTower` still returns only
      the 5 nodes of the requested tower.
- [ ] Global nodes are not associated with any `TowerType`; the data model supports a
      tower-independent node without showing global nodes on tower tabs (or vice versa).
- [ ] Sequential unlock is enforced by `SkillTreeState.IsUnlocked`: rank 0 on the previous node
      leaves the next locked; rank 1 unlocks it. A locked buy is rejected and spends no SP.
- [ ] Every global purchase costs exactly 10 SP per rank, increments rank by 1, and never
      exceeds that node's max (5, 5, 3, 3). `MAX` replaces the buy action at cap.
- [ ] Starting a level sets HP to `StartingHP (3) + rank(Fortified Core)` and coins to
      `StartingCoins (10) + 2 × rank(War Chest)`.
- [ ] Effective tower placement cost is `max(1, base cost − rank(Master Blueprints))` for Arrow,
      Cannon and Laser. All three bases are 10, so ranks 0–3 yield 10/9/8/7.
- [ ] Effective rank-upgrade cost is `max(1, base cost − rank(Advanced Training))` for every
      purchase: rank 1→2 (base 5), rank 2→3 (base 10) and the rank-3 archetype choice (base 15).
      At rank 3 those become 2/7/12.
- [ ] The clamp is defensive only: with max 3 discount ranks and the lowest base cost of 5, a
      cost can never reach 0. No gameplay path produces a free tower or free upgrade.
- [ ] Every price surface uses the effective cost, not the base constant: placement
      affordability and coin deduction, placement-card labels and disabled styling, number-key
      hotkey eligibility, upgrade-panel button labels and affordability, and upgrade/archetype
      coin deduction.
- [ ] A player whose coins are below the base price but at or above the discounted price can
      place the tower / buy the upgrade; the deduction matches the displayed price.
- [ ] Global ranks round-trip through `SkillTreeSave`, are clamped to each node's own max on
      load, and unknown/disabled ids are still rejected. Saves written before this feature load
      with all global ranks at 0.
- [ ] Skill tree screen gets a fourth sidebar entry, **Global**, below the three tower entries,
      with the same size/spacing/interaction. Selecting it renders the 4-node chain in the same
      tree area with the same row anatomy (icon, name, rank `r/max`, buy), hover info band and
      locked/unlocked styling.
- [ ] Visual styling matches the tower trees: geometric node icons, rank pips/halo, locked
      slash, max-rank gold treatment, spine and branch lines, hover highlight, fading pulse on
      locked/hovered nodes disabled. Global accent color is cyan, distinct from Arrow blue,
      Cannon green and Laser magenta.
- [ ] `dotnet build` clean and `dotnet test` green with new coverage for catalog, state,
      persistence, starting stats, both discounts and the screen tab.

## Edge Cases

- **Buy at max rank:** Buy action is replaced by `MAX`; clicking does nothing and spends no SP.
- **Buy locked node:** Button absent (existing behavior for locked nodes); row shows the
  "Unlocks after {previous node} reaches rank 1" hint in the info band.
- **Exactly enough SP:** 10 SP buys one rank and leaves 0; 9 SP buys nothing.
- **Two ranks in one visit:** After buying rank 1 of node 1, node 2 unlocks immediately; the
  rebuilt tree reflects it without leaving the screen, provided SP remains.
- **Discount rank 3 on a 5-cost upgrade:** cost 2, never 1 or 0 via clamping; the floor is
  unreachable through normal play.
- **Insufficient coins at base price:** with rank 3 Master Blueprints and 7–9 coins, a 10-cost tower
  can be placed; with 6 coins it cannot. Same pattern for upgrades at their discounted price.
- **New level / replay:** starting HP and gold are recomputed from current ranks on every
  `Initialize`; no carry-over of previous run values.
- **Save tampering:** a stored rank above the node max (e.g. 9 on a 3-rank node) clamps to the
  max; negative ranks clamp to 0.
- **Old save:** missing global keys default to rank 0; the 15 tower nodes load unchanged.
- **Global tab with no SP:** all four nodes render; only node 1 is unlocked; every buy action is
  disabled, node 1 reads `Buy (10 SP)` disabled.

## Dependencies

- `SkillNodeDefinition` / `SkillTreeCatalog` — need a tower-independent node identity and a
  per-node max rank; tower nodes keep max 5.
- `SkillTreeState` — prerequisite and max-rank checks must handle global nodes.
- `SkillTreeSave` — no format change; the flat node-id map already covers new ids.
- `SkillStats` — new global effect helpers for starting HP, starting gold and both discounts.
- `GameManager.Initialize` — set starting HP/coins from global ranks.
- `GameManager` placement and upgrade transactions — use effective costs.
- `GameHUD` placement cards and `Main` hotkey checks — display and gate on effective costs.
- `TowerUpgradePanel` — display and gate on effective upgrade costs. `TowerUpgrades` remains the
  source of base costs.
- `SkillTreeScreen` — fourth tab, global selection, tree rendering; reuse existing row, icon,
  info-band and line builders.
- `SkillGlyphs` — new glyph identities for the four global nodes and the sidebar tab.
- Tests mirroring `Tests/Skills` and `Tests/UI`; no new scene required, UI tests attach the
  screen programmatically.

## Constraints

- **Performance:** effect reads are O(1) dictionary lookups, evaluated at level start and on
  price surfaces; no per-frame work, no allocation in update loops, no new pools.
- **Memory:** no new save file or asset; four extra catalog definitions.
- **Platform:** keyboard + mouse, consistent with the existing skill screen.
- **Compatibility:** existing save file and existing tower trees must behave unchanged.

## Visual / Geometry Theme

- Global accent: cyan `(0.35, 0.85, 0.92)` — distinct from Arrow blue `(0.2, 0.5, 1)`,
  Cannon green `(0.3, 0.6, 0.4)` and Laser magenta `(0.8, 0.3, 1)`.
- Node icons stay in the existing 46×46 icon, halo radius 19.5, glyph radius 13 language:
  - Fortified Core: hexagon shield outline with an inner vertical bar (HP core).
  - War Chest: two offset coin circles with a square center cutout.
  - Master Blueprints: rectangular blueprint sheet with grid lines and a downward chevron.
  - Advanced Training: three stacked upward chevrons with a downward notch.
- Sidebar Global glyph: hollow circle with four square nodes at the compass points.
- Rank display: same filled pips and `r/max` text; max rank uses the existing gold halo/dot.
  Locked nodes use the existing grey `(0.3, 0.3, 0.35)` and slash.
- All geometry procedural via `CanvasItem` draws with `antialiased: true`; no sprites, textures
  or AI-generated assets.

**LAYOUT:** The existing sidebar gains a fourth tab named `Tab_Global` directly below the Laser
tab inside the same sidebar group, identical width, height and spacing, so the four tabs form one
evenly spaced column. Selecting it places the global tree in the existing `TreeArea` using the
same header block (`Block_TreeHeader`), vertical trunk spine, row geometry and info band
(`Block_SkillInfo`) as a tower tree; the four rows are named `Row_{global node id}` and stack
top-to-bottom in unlock order. The info band keeps its current position and width; the tree area
keeps its current rect. No new group overlaps the sidebar, info band or title block. Every
wrapper Control has `MouseFilter = Ignore`.

## Out of Scope

- Additional global nodes beyond the four listed.
- Respec, refund, or sell-back of ranks.
- Mid-run application: buying is only available from the title-screen skill tree, so effects
  next apply at the next level start.
- Per-level starting HP/gold overrides.
- Changes to SP reward rates or node cost (flat 10 SP).
- Balance tuning beyond the values in this spec.
