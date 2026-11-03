# Holdout Scenarios — tower-hotkeys

Implementer must never see this file. Run every scenario after build + test gates are green.
A scenario passes only when every expected observation is verified; "looks right" is not a pass.
Drive real `InputEventKey` events through `Viewport.PushInput` on the real `Main` graph unless a
step says otherwise. Never place a tower from the key press alone — only a mouse click places.

## H1 — Arrow hotkey enters placement without spending

**Setup:** Start Level 1 through the real TitleScreen button signal. Starting coins are 10 and
Arrow cost is 10.

**Steps:**
1. Read coins, tower count, Start Wave enabled state, placement type.
2. Push mouse motion to a valid non-path cell but do not press any mouse button.
3. Push a `Key.Key1` key-down (not echo).
4. Read the same values plus the preview state, with no further mouse-motion event.
5. Push `1` again.

**Expected:**
- Before: coins 10, no towers, Start Wave enabled, not placing.
- After first press: Arrow placement type, `IsPlacingTower` true, coins still 10, zero towers,
  Start Wave disabled.
- The placement ghost/highlight and range ring are visible immediately at the pointer cell
  without any post-key mouse motion.
- The normal pointer cursor is never replaced by a crosshair; verify by code inspection, because
  the headless display server cannot report cursor shape.
- The second press exits placement: type null, Start Wave enabled again, ghost hidden, no tower
  placed, coins still 10.
- No wave starts on either press.

## H2 — Mouse click after hotkey places exactly one tower

**Setup:** Level 1, pointer already over an in-bounds non-path, unoccupied cell; Arrow selected
via `1`.

**Steps:**
1. Push a left mouse-button press at the current pointer position without any further mouse
   motion.
2. Read towers, coin balance, placement state, Start Wave state.
3. Push `1` again with the new balance.

**Expected:**
- Exactly one Arrow tower exists at the pointer cell; coins decrease by exactly the Arrow cost.
- Placement mode exits automatically; preview hidden; Start Wave button returns to its normal
  state for the current wave activity.
- With 0 coins, the follow-up `1` is a no-op: no placement mode, no tower, coins unchanged.

## H3 — Affordability boundary

**Steps:**
1. On a fresh Level 1 (10 coins), push `1`; confirm placement is allowed (equal-to-cost boundary)
   without spending.
2. Place the tower to reach 0 coins.
3. Without earning coins, push `1`, `2`, and `3` individually; after each, read placement state
   and coins.
4. Earn exactly one coin (kill or scripted real drop), giving 1 coin; push `1` again.
5. Earn/kill until the balance reaches 10; push `1`.

**Expected:**
- Step 1: allowed at equality.
- Step 3: every press is a no-op; no placement type, no spend, no tower.
- Step 4: still a no-op at 1 coin.
- Step 5: placement allowed at 10; coins remain 10 until the click.
- No path enters placement with `Coins < TowerCost(type)`.

## H4 — Toggle and switch semantics

**Setup:** A level that allows all three tower types (Level 3 or later) with at least 10 coins,
pointer over a valid battlefield cell.

**Steps:**
1. Push `1`; read type and preview state.
2. Push `2`; read type, coins, placement active, preview state with no further mouse motion.
3. Push `2` again; read type.
4. Push `3`; read type and preview state.
5. Push `1` while placing Laser; read type.
6. Push `4`, then `5`, then `Enter`; read type and coins each time.

**Expected:**
- `1` → Arrow; preview appears immediately at the pointer cell.
- `2` → Cannon while remaining in placement mode and spending nothing; the preview is redrawn
  for Cannon at the same cell without a mouse-motion event; second `2` cancels placement;
  `3` → Laser; `1` while Laser → Arrow (switch).
- `4`, `5`, and `Enter` change nothing.
- No click ever occurs, so no tower is placed and coins never change.

## H5 — Level availability matches the sidebar

**Setup:** For each of Level 1, Level 2, Level 3, start normally with enough coins.

**Steps:** Push `1`, read type, cancel with `1`. Push `2`, read type, cancel if active. Push `3`,
read type, cancel if active. Compare with the visibility/enabled state of `PlaceArrow`,
`PlaceCannon`, and `PlaceLaser` cards.

**Expected:**
- Level 1: only `1` enters placement; `2` and `3` are no-ops.
- Level 2: `1` and `2` work; `3` is a no-op.
- Level 3: all three work.
- For every level, a key works exactly when its sidebar card is visible and enabled (given the
  same coin balance). No key can select a tower type the level forbids.

## H6 — Ineligible press leaves an active placement untouched

**Setup:** Level 3, Arrow selected via `1`.

**Steps:**
1. Cancel and place towers/kill enemies until coins are below the Cannon cost while Arrow
   placement is active (or start another placement with a reduced balance).
2. With Arrow placement active and coins below Cannon cost, push `2`.
3. On Level 1, with Arrow placement active, push `2` and `3`.

**Expected:**
- Placement remains Arrow in every case; ghost/range and Start Wave disabled state persist.
- Coins unchanged; no error or warning spam.

## H7 — Key repeat and release

**Steps:**
1. Push `1` with `Echo = true`, then a release with `Pressed = false`.
2. Push a normal `1` (enters placement), then an echo `1` and a release `1`.
3. Push a normal `1` again.

**Expected:**
- Step 1: inert; no placement.
- Step 2: echo/release do not toggle placement off or place anything; placement stays Arrow.
- Step 3: toggles placement off (one action per physical press).
- No repeated toggling while a key is "held".

## H8 — Play-state gating and input ownership

**Steps:**
1. On the title screen, push `1`, `2`, `3`.
2. Start Level 1, trigger game over (leak enemies), and push the digits on the result overlay.
3. Repeat with victory on a completed level.
4. During normal play, focus the `PlaceArrow` sidebar Button, then push `1`. Check
   `IsInputHandled()` and whether the card's Pressed signal fired.
5. During normal play, push `1` with 0 coins and check `IsInputHandled()`.

**Expected:**
- Title, game-over, and victory states: no placement state, no tower, no crash, no wave change.
- Focused card: the digit does not activate the card; the hotkey action runs once; the event is
  marked handled.
- No-op press during play is still consumed (handled), so no GUI control receives it.

## H9 — Space, mouse, and cancel paths unchanged

**Setup:** Level 1 with a wave active and Arrow placement entered via `1`.

**Steps:**
1. Push Space; read `Engine.TimeScale`, wave number, placement state.
2. Push Escape; read placement state.
3. Re-enter with `1`, push a right mouse-button press; read placement state.
4. Start/complete waves and confirm sidebar card clicks still enter/exit placement and mouse
   clicks still place towers.
5. Restore `Engine.TimeScale` to 1.0 before teardown.

**Expected:**
- Space during the active wave toggles speed to 1.5 and leaves Arrow placement active.
- Escape and right-click both cancel placement.
- Sidebar and mouse paths behave exactly as before the feature.
- No test leaves the clock sped up.

## H10 — Mapping and key ownership are exact

**Steps:**
1. From a fresh play state with full coins, push `1`; record type. Cancel.
2. Push `2`; record type. Cancel.
3. Push `3`; record type. Cancel.
4. Push `Key.Kp1`, `Key.Kp2`, `Key.Kp3` (numpad); record types.

**Expected:**
- `1` → `TowerType.Arrow`, `2` → `TowerType.Cannon`, `3` → `TowerType.Laser`, with no crossed
  mapping.
- Keypad digits are not required to map; they must not crash or select an unintended type.
- Each mapping spends no coins until a cell click.

## H11 — Pointer outside the battlefield at selection

**Setup:** Level 1, pointer last moved over the sidebar or HUD (outside the grid).

**Steps:**
1. Push `1` and read placement/preview state with no subsequent mouse motion.
2. Move the pointer over a valid battlefield cell.
3. Read the preview state.
4. Press `1` to cancel, then repeat with the pointer never moved since app start where the
   harness allows it.
5. Return to title, start another level, press a digit before any mouse motion in that level.

**Expected:**
- The hotkey still enters placement mode, but no preview is drawn while the pointer is off the
  battlefield or no pointer position is known.
- Moving over a valid cell shows the ghost/highlight/range immediately.
- Placement never guesses a cell on its own, and the normal pointer cursor is unchanged.
- The previous level's pointer position never leaks into the new level; no preview until the
  pointer moves during that level.

