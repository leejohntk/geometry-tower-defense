# Feature Spec: upgrade-ready-glow

**Status:** draft
**Branch:** feature/upgrade-ready-glow
**Date:** 2026-11-02

## Description

Placed towers get a subtle, pulsing geometric glow whenever their **next upgrade is unlocked
by XP** — the same hidden gate that decides whether the upgrade panel offers an upgrade. The
cue means "this tower has earned its next upgrade". It deliberately ignores the coin balance:
an unaffordable upgrade still glows. The glow turns off as soon as the tower is at max rank or
the next gate is not yet filled. It is a gameplay cue, not a new interaction: it never changes
stats, targeting, tower position, or input.

## Acceptance Criteria

- [ ] `Tower` exposes a read-only `HasUpgradeReady` (name may be implementation-specific but
      semantics fixed): true exactly when `TowerUpgrades.IsUpgradeVisible(Rank, Xp)` is true.
      This covers rank 0→1, rank 1→2 and the rank 2→3 archetype choice. False at rank 3.
- [ ] A placed tower whose `HasUpgradeReady` is true shows exactly one glow node named
      `UpgradeReadyGlow`, a `Control` the size of one cell (64×64 px) centered on the tower origin
      and drawn behind the tower body.
- [ ] The glow is visible when the XP gate is filled, regardless of current coins. Setting coins
      to 0 or changing coins never hides or shows it.
- [ ] The glow is hidden when the XP gate is not filled, after a purchase that leaves the next
      gate closed, and permanently at rank 3 / max rank.
- [ ] Crossing the XP threshold makes the glow visible without a reselect, purchase, or extra
      input; the same tower instance owns the same glow node for its whole life (created lazily
      on first need or on placement).
- [ ] The glow animates by opacity only. It may pulse alpha but must not scale, rotate, translate,
      or otherwise move; tower `Position`, body transform, aim, recoil, range indicator and rank
      badge keep their existing behavior.
- [ ] `UpgradeReadyGlow` has `MouseFilter = Ignore`, sits below the tower body and rank badge, and
      never blocks tower selection, placement, or upgrade-panel clicks.
- [ ] The visual is procedural geometry: a soft pale-gold radial halo of two or three concentric
      circles plus a thin ring, antialiased, no textures or new assets. Nominal outer diameter is
      60 px (≈0.94 cell), leaving a 2 px inset from the cell edge.
- [ ] All three tower types (Arrow, Cannon, Laser) get the same cue; tower theme colors are
      unchanged.
- [ ] Unit tests cover the truth table, coin independence, purchase/clamp transitions, rank
      badge independence, click-through, and the opacity-only animation. `dotnet build` and
      `dotnet test` stay green.

## Edge Cases

- **XP exactly at threshold:** visible at `xp == threshold` (existing `>=` gate), not one point
  below.
- **Fractional XP:** any accumulated fractional XP that reaches the threshold counts; no rounding
  is introduced.
- **Purchase with surplus XP:** if XP already meets the next threshold, the glow stays visible
  immediately after the purchase (the next upgrade is already earned).
- **Purchase without surplus XP:** glow hides immediately after the rank rises and the next gate
  is closed.
- **Archetype step:** rank 2 with XP ≥ 30 glows even before the player chooses an archetype; the
  choice is the next upgrade.
- **Rank 3:** no glow for the rest of the tower's life.
- **Zero coins:** glow remains visible; the affordable-state styling of the upgrade panel is
  unchanged.
- **Multiple towers:** each tower's glow follows its own rank and XP; buying on one never changes
  another.
- **Tower selected:** the range indicator draws normally; the glow stays behind it and does not
  change its geometry.
- **Enemy killed by several towers:** each tower's XP share updates independently; a tower whose
  share crosses the threshold starts glowing on the next update.
- **Tower removed / level ends:** the glow is freed with the tower; no orphan node or process.
- **No ready towers:** no per-frame glow work beyond the existing tower process and no allocation
  per frame.

## Dependencies

- `Tower` — owns the cue, `Rank`, `Xp`, lifecycle, `_Process`, and the rank badge.
- `TowerUpgrades.IsUpgradeVisible` / `XpThresholdToReachRank` — single source of truth for the
  XP gate; the glow must not duplicate threshold logic.
- `GameConstants` — new glow colors, radius, pulse period, alpha range, node size.
- `GameManager` — no changes expected; XP is already credited to towers.
- `TowerUpgradePanel` — unchanged; its gate remains the same predicate.
- Tests: `Tests/Towers/` mirroring the existing `TowerRankBadgeTest` patterns; no new scene.

## Constraints

- **Performance:** per-tower check is O(1) arithmetic; animation updates only `Modulate.A` of an
  already-drawn control. No per-frame draw calls, allocations, or array rebuilds. No glow work at
  all when `HasUpgradeReady` is false.
- **Memory:** one lazily created small `Control` per tower; no new pools.
- **Platform:** keyboard + mouse; visual only.
- **Behavior:** zero change to damage, fire rate, range, targeting, XP, coins, save data, or the
  upgrade transaction.

## Visual / Geometry Theme

- Pale-gold cue, distinct from enemy reds, projectile trails and the cyan skill accent:
  core `(1.0, 0.93, 0.55)`, ring same hue.
- Halo: two filled concentric circles (radius ≈ 30 px and ≈ 23 px) at low alpha plus a 1.5 px
  ring at radius ≈ 30 px. Antialiased.
- Pulse: alpha oscillates smoothly with period ≈ 1.6 s between ≈ 0.55 and 1.0 of the drawn
  opacity. Opacity-only; no scale, no rotation, no position change.
- The rank badge (`RankBadge`) remains the purchase-driven marker; the glow is the XP-driven
  marker. They may be visible together.
- No AI-generated assets.

**LAYOUT:** `UpgradeReadyGlow` is one 64×64 cell-sized wrapper centered on the tower's origin,
behind the tower body and range indicator, and must not overlap or alter `RankBadge`. Every glow
wrapper has `MouseFilter = Ignore`. The cue adds no new screen-level group and changes no existing
group's rect.

## Out of Scope

- Changing upgrade costs, XP thresholds, or affordability rules.
- New input (clicking the glow), tooltips, or labels.
- Glow on the tower placement ghost, on enemy units, or on the HUD cards.
- Distinguishing affordable vs unaffordable with different glow colors (explicitly one cue).
- Sound effects.
- Persistence of any kind.
