# Feature Spec: upgrade-ready-glow

**Status:** approved (playtest fix 1)
**Branch:** feature/upgrade-ready-glow
**Date:** 2026-11-02

## Description

Placed towers get a subtle, pulsing glow overlaid on the tower's own silhouette whenever their
**next upgrade is unlocked by XP** — the same hidden gate that decides whether the upgrade panel
offers an upgrade. The cue means "this tower has earned its next upgrade". It deliberately ignores
the coin balance: an unaffordable upgrade still glows. The glow turns off as soon as the tower is
at max rank or the next gate is not yet filled. It is a gameplay cue, not a new interaction: it
never changes stats, targeting, tower position, or input.

Playtest fix 1: the original behind-the-tower gold halo read poorly and was barely visible on the
circular Cannon and Laser bodies. It is replaced by an additive overlay that redraws the tower's
own body shapes on top of the body, so the glow conforms exactly to the tower silhouette (including
archetype variants) and follows aim and recoil transforms.

## Acceptance Criteria

- [ ] `Tower` exposes a read-only `HasUpgradeReady` (name may be implementation-specific but
      semantics fixed): true exactly when `TowerUpgrades.IsUpgradeVisible(Rank, Xp)` is true.
      This covers rank 0→1, rank 1→2 and the rank 2→3 archetype choice. False at rank 3.
- [ ] A placed tower whose `HasUpgradeReady` is true shows exactly one glow node named
      `UpgradeReadyGlow`, a `Control` parented to the tower's body Control, the same size as the
      body, positioned at the body's local origin, and drawn **on top of** the body arts.
- [ ] The overlay redraws the same silhouette as the body (same fill and outline shapes, including
      the chosen archetype variant) with an additive `CanvasItemMaterial`, so the tower itself
      appears to glow. It follows the body's rotation, recoil offset and aim automatically.
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
- [ ] `UpgradeReadyGlow` has `MouseFilter = Ignore`, draws above the body but below the rank badge
      and range indicator, and never blocks tower selection, placement, or upgrade-panel clicks.
- [ ] The visual uses only the tower's existing procedural draw calls with an additive blend
      material; no textures, sprites or new assets. No separate halo, ring, or circle is drawn.
- [ ] All three tower types (Arrow, Cannon, Laser) get the same cue; tower theme colors are
      unchanged in the base body. The additive layer produces a brighter version of each theme
      color, not a uniform gold. Archetype silhouettes (forked spearhead/star inset, cannon lobes/
      shockwave rings, laser chain nodes/melter wedge) glow with their own shape.
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
- **Tower selected:** the range indicator draws normally above the glow; the glow does not change
  its geometry.
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

- Additive overlay of the tower's own body draw: the same filled shapes and outlines are drawn
  again on top of the body through a `CanvasItemMaterial` with `BlendMode = Add`, tinted by a
  pulsing opacity. The silhouette therefore matches every tower type and archetype exactly.
- The overlay inherits the body Control's transform, so it rotates with aim and follows the recoil
  kick without any extra sync code.
- Pulse: alpha oscillates smoothly with period ≈ 1.6 s between ≈ 0.12 and ≈ 0.40 additive
  opacity. Opacity-only; no scale, no rotation, no position change.
- The rank badge (`RankBadge`) remains the purchase-driven marker; the glow is the XP-driven
  marker. They may be visible together. The badge draws above the body and its glow.
- No AI-generated assets.

**LAYOUT:** `UpgradeReadyGlow` is a Control child of the tower's body Control with the same rect
(local position zero, same size), so it spans exactly the body silhouette and shares the body's
transform. It draws above the body art but below the rank badge and range indicator. Every glow
Control has `MouseFilter = Ignore`. The cue adds no new screen-level group and changes no existing
group's rect.

## Out of Scope

- Changing upgrade costs, XP thresholds, or affordability rules.
- New input (clicking the glow), tooltips, or labels.
- Glow on the tower placement ghost, on enemy units, or on the HUD cards.
- Distinguishing affordable vs unaffordable with different glow colors (explicitly one cue).
- A separate halo, ring, outline-only cue, or gold-tinted overlay; the cue is the body's own
  silhouette redrawn additively.
- Sound effects.
- Persistence of any kind.
