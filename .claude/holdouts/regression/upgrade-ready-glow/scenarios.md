# Holdout Scenarios — upgrade-ready-glow

Implementer must never see this file. Run every scenario after build + test gates are green.
A scenario passes only when every expected observation is verified; "looks right" is not a pass.

## H1 — XP gate truth table

**Setup:** Fresh tower of each type, coins irrelevant.

**Steps:**
1. Read readiness and glow visibility at rank 0 with XP 0, 4.999, 5, 14.999, 15, 29.999, 30.
2. Buy rank 1 at XP 5; re-read at XP 5, 14.999, 15.
3. Buy rank 2 at XP 15; re-read at XP 15, 29.999, 30.
4. Choose an archetype; re-read at XP 30, 100.

**Expected:**
- Rank 0: hidden below 5, visible at exactly 5 and above.
- Rank 1: hidden below 15, visible at exactly 15 and above (including XP that accrued before the
  purchase).
- Rank 2: hidden below 30, visible at exactly 30 and above.
- Rank 3: hidden at every XP value.
- Readiness never disagrees with `TowerUpgrades.IsUpgradeVisible(Rank, Xp)`.

## H2 — Coins are irrelevant

**Steps:**
1. Put a tower at rank 0 with XP 5 and 0 coins; inspect glow.
2. Add coins to above the upgrade cost; inspect.
3. Spend coins back to 0 (place another tower or use the test economy seam); inspect.
4. Repeat at rank 2 with XP 30.

**Expected:** Glow visible in every coin state, including 0. No coin change toggles it, changes
its alpha, or recreates the node. The upgrade panel's affordability styling still reflects coins.

## H3 — Opacity-only animation

**Steps:**
1. Show a ready glow; sample its node rect, position, rotation, scale and tower position across
   at least two full pulse periods.
2. Sample `Modulate.A` over the same interval.
3. Hide the gate and sample again.

**Expected:**
- Rect, position, rotation and scale are constant; tower `Position` is constant.
- Alpha varies smoothly within the configured additive range and period; never outside the range.
- The additive overlay's geometry calls are the body's existing draws; changing archetype changes
  the glow silhouette with no separate shape code.
- No draw-call/vertex churn is required to animate (opacity on a pre-drawn control).
- When not ready, the node is hidden and adds no per-frame glow work.

## H4 — All tower types and independent towers

**Steps:** Place Arrow, Cannon and Laser towers; bring each to a ready gate one at a time.

**Expected:**
- Additive overlay redraws the tower's own body shapes on top; the silhouette matches each type
  (arrow triangle, cannon circle+barrel, laser diamond/emitter) and each archetype variant.
- Each shows its own `UpgradeReadyGlow`; toggling one never toggles another.
- The glow Control is a child of that tower's body, same rect as the body, click-through, and
  draws above the body art. Tower base theme colors are unchanged.

## H5 — Purchase transitions

**Steps:**
1. Rank 0, XP exactly 5: glow visible. Buy rank 1; verify glow.
2. Rank 1, XP exactly 15: buy rank 2; verify.
3. Rank 2, XP exactly 30: glow visible; choose archetype; verify.
4. Rank 0, XP 30: buy rank 1 (next gate already filled); verify.

**Expected:**
- Steps 1–3: glow hides immediately after the purchase because the next gate is closed, then
  reappears when that tower's XP crosses the next threshold.
- Step 4: glow stays visible because rank 1 with XP 30 already meets the next threshold.
- The same glow node instance persists across purchases; no duplicate node.

## H6 — Badge and range independence

**Steps:**
1. A rank-1 tower with an open next gate: inspect `RankBadge` and glow together.
2. Select the tower to show its range indicator.
3. Move the pointer to hover/selection states over the tower.

**Expected:**
- Badge still appears only from purchases, keeps its size/position, and is not changed by the
  glow.
- Range indicator geometry and color are unchanged; it draws above the body glow.
- Mouse input reaches the tower/grid normally; glow never consumes a click.

## H7 — Click-through and node contract

**Steps:** Enumerate the glow node and its children while ready.

**Expected:**
- Exactly one node named `UpgradeReadyGlow`, a `Control` with `MouseFilter = Ignore`.
- It is a child of the tower's body Control, local position zero, same size as the body, with a
  `CanvasItemMaterial` whose blend mode is Add; it draws above the body and below the badge.
- It reuses the body's draw shapes (no separate halo/ring/circle) and inherits body rotation,
  recoil offset and aim.
- Freed with the tower at level end; no orphan node remains.
- Layout verifier passes with zero findings.

## H8 — No gameplay side effects

**Steps:** With glow visible, compare before/after: damage, fire rate, range, target selection,
projectile spawn, XP/coin values and upgrade cost. Complete a wave and end a level.

**Expected:**
- Every gameplay value and transaction is unchanged; only the visual cue differs.
- Tower positions never shift; enemies/path/grid unaffected.
- No save-file changes from the cue.

## H9 — Edge sequencing

**Steps:** Check XP added in one large jump (0 → 30), in fractional increments, and after max
rank; check at exactly the threshold; check two towers crossing thresholds on the same kill.

**Expected:**
- A single large jump shows the glow on the next update; no intermediate glow state is required.
- Fractional accumulation uses the exact `>=` gate with no rounding drift.
- At max rank the glow never appears, even with huge XP.
- Two towers crossing together both show their own glow; no shared state.
