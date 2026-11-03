# Feature Spec: tower-hotkeys

**Status:** draft
**Branch:** feature/tower-hotkeys
**Date:** 2026-11-02

## Description

During a level, the top-row number keys become tower-placement shortcuts. Pressing `1`, `2`, or
`3` selects the Arrow, Cannon, or Laser tower for placement exactly as clicking the matching
sidebar card does: the player enters placement mode, the ghost preview appears immediately at the
cell under the current pointer, and a left-click on a valid cell places the tower. A hotkey only
selects a tower type — it never places one by itself. A press does nothing unless the level
allows that tower type and the player's current coin balance covers its full cost. Pressing the
key of the tower already being placed cancels placement, and pressing a different eligible key
switches the placement type. The normal pointer cursor is never replaced by a crosshair. The
mouse remains required to choose the cell and confirm placement.

## Acceptance Criteria

- [ ] **Digit keys select placement types.** In `GameState.Playing`, with `_gameRunning` true,
      a new `1` key-down enters Arrow placement mode, `2` enters Cannon, and `3` enters Laser.
      Selection uses the same state transition as clicking the sidebar card: `PlacingTowerType`
      is set, the Start Wave button is disabled, the cursor shape is left unchanged (no
      crosshair), and the upgrade panel/selection is cleared.
- [ ] **Preview appears immediately at the pointer.** Entering or switching placement draws the
      ghost/highlight/range preview at the last known pointer position without requiring a
      further mouse-motion event. If the pointer is outside the battlefield or no pointer
      position is known, no preview is shown until it moves over a valid cell.
- [ ] **Affordability gate.** A hotkey is a no-op unless
      `GameManager.Coins >= GameConstants.TowerCost(type)`. A balance exactly equal to the cost
      is eligible. Selecting the type spends nothing; coins are deducted only when a tower is
      actually placed on a cell, through the existing `PlaceTower` path.
- [ ] **Level availability gate.** Hotkeys respect the level's tower permissions. On Level 1
      (`AllowCannonTower == false`, `AllowLaserTower == false`), `2` and `3` are no-ops; on levels
      that allow Cannon or Laser, the matching key works. Keyboard availability always matches
      the sidebar cards' visibility/enabled state.
- [ ] **Same key toggles placement off.** Pressing the number of the tower currently being placed
      exits placement mode, matching a second click on its sidebar card: placement type is
      cleared, the ghost is hidden, the cursor shape is unchanged, and the Start Wave button
      returns to its normal state for the current wave.
- [ ] **Different eligible key switches type.** While placing Arrow, pressing `2` (when allowed
      and affordable) replaces the pending type with Cannon without leaving placement mode or
      consuming coins. The preview at the current pointer is redrawn for Cannon immediately, and
      the next mouse click places the newly selected type.
- [ ] **Ineligible presses do not disturb placement.** Pressing a key for a tower that is
      unaffordable or not allowed by the level is a no-op; if another type is already being
      placed, that placement stays active and unchanged.
- [ ] **Only new presses act.** Key repeat (`Echo == true`) and key release never change
      placement state. Holding a digit produces at most one action per physical press.
- [ ] **Play only.** Hotkeys are inert on the title screen, on the skill-tree screen, after game
      over, after victory, and while a result overlay is visible. No placement state can be
      created outside `GameState.Playing`.
- [ ] **Digits are consumed during play.** Every new `1`/`2`/`3` key-down received during a
      running game is marked handled, including no-op presses (unaffordable, unavailable, or
      during placement). A focused HUD/title Button cannot receive the same event. Other keys,
      including keypad digits and Enter, are unaffected by this feature.
- [ ] **Keyboard selection is not keyboard placement.** Pressing a hotkey never places a tower
      at the cell under the mouse; placement still requires a left-click, which revalidates
      coins, cell occupancy, and path rules through the existing flow.
- [ ] **Existing input paths unchanged.** Sidebar card clicks, mouse placement/selection, Escape
      cancel, right-click cancel, and Space behavior (start wave / toggle speed) are unchanged at
      1.0× and 1.5×, and work identically whether placement was chosen by mouse or key.
- [ ] **Tests cover routing.** A new `Main`-level test suite drives real `InputEventKey` events
      through the viewport for: each mapping, affordability boundary, level availability,
      toggle/switch semantics, no-op presses, repeat/release, and non-playing states. Build and
      test gates pass.

## Edge Cases

- **Coins below cost:** `1` with 9 coins does nothing; placement mode is not entered.
- **Coins exactly equal to cost:** `1` with 10 coins (the Level 1 start balance) enters
  placement; placement deducts the full cost and leaves 0.
- **Unaffordable while already placing:** pressing `2` with too few coins while placing Arrow
  leaves Arrow placement active.
- **Unavailable while already placing:** pressing `3` on Level 2 or lower while placing Arrow
  leaves Arrow placement active.
- **Pointer over sidebar/HUD/off-grid when selecting:** no preview appears until the pointer
  moves over a valid battlefield cell.
- **No pointer position known yet:** selecting shows no preview rather than guessing a cell.
- **Pointer already over a valid cell:** the preview appears immediately; no mouse movement is
  required after the key press.
- **Mid-wave selection:** hotkeys work while a wave is running; the wave, clock, and enemies are
  unaffected. Space still toggles speedup mid-wave even in placement mode.
- **Between waves:** a running wave is not required; selection works whenever play is active.
- **Same key twice:** second press cancels; it does not place anything.
- **Key held down:** OS key repeat is ignored; placement does not flicker.
- **Focused Button:** digits never activate a focused Button; the hotkey owns the press.
- **Game over / victory mid-placement:** existing handlers clear placement; subsequent digits are
  inert until a new level starts.
- **Returning to title then starting another level:** no stale placement type, affordability
  state, or last-pointer position carries across levels; a new level shows no preview until the
  pointer moves over a valid cell during that level.
- **Numpad:** numpad digits are out of scope and need not map.

## Dependencies

- `Main._Input` event routing and `_gameRunning` guard.
- `Main.OnPlaceTowerPressed`, `ExitTowerPlacementMode`, and `UpdateStartWaveButtonState` as the
  single placement-state transition path shared with the sidebar cards.
- `Main._selectedLevel` (`LevelDefinition.AllowCannonTower` / `AllowLaserTower`).
- `GameManager.Coins`, `PlacingTowerType`, `IsPlacingTower`, and `PlaceTower`.
- `GameConstants.TowerCost`, `TowerType`, and `GameHUD` placement-card state (`Block_Sidebar`).
- Viewport `SetInputAsHandled` for input ownership.

## Constraints

- **Performance:** mapping is a constant-time switch; no per-frame work, allocations, or
  `_Process` additions.
- **Memory:** no new nodes, timers, or allocations.
- **Platform:** top-row keyboard digits only; mouse behavior unchanged; works at both clock
  speeds.
- **Single source of truth:** affordability uses `GameConstants.TowerCost`; tower permissions
  use the selected `LevelDefinition`; no duplicated cost or availability tables.
- **Input hygiene:** only new key-down events act; every owned press is consumed; no change to
  Space, Escape, Enter, or mouse handling.

## Visual / Geometry Theme

No new visuals. The feature reuses the existing geometric placement ghost, cell highlight, and
range ring. Sidebar cards, their icons, and their disabled dim state are unchanged. The normal
pointer cursor stays visible throughout placement; no crosshair or other cursor replacement is
introduced.

**LAYOUT:** No new visual groups and no layout changes. The existing sidebar card group and
battlefield preview keep their current geometry. The only observable UI change is the Start Wave
button disabling/enabling when placement mode toggles; the pointer is unchanged.

## Out of Scope

- Placing a tower immediately at the mouse cursor without a click.
- Keyboard-only grid cursor or keyboard cell navigation.
- Numpad digit bindings, remappable hotkeys, or gamepad bindings.
- On-screen key hints on the sidebar cards.
- Audio feedback, toast text, or a red flash for unaffordable/unavailable presses.
- Changing tower costs, unlocks, or level availability.
