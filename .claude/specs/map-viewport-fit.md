# Feature Spec: map-viewport-fit

**Status:** draft
**Branch:** feature/map-viewport-fit
**Date:** 2026-09-17

## Description

Fix the map/window mismatch so the full play area is visible at launch without manual window resizing, on any screen size. Two phases. **Phase A:** reshape the square 20×20 grid into a shorter grid that fits the landscape window. **Phase B:** add Godot stretch mode so the landscape canvas scales to fit any window (big monitor today, laptop when shared). Phase A fixes the current annoyance; Phase B makes the game resize-proof.

## Acceptance Criteria

### Phase A — Shorter grid

- [ ] `GameConstants.GridRows` 20 → 14. `GridCols` stays 20. `CellSize` stays 64.
- [ ] Play area becomes 1280 × 896. `PlayAreaWidth` 1280 unchanged, `PlayAreaHeight` 1280 → 896.
- [ ] Window (design resolution) set to 1460 × 940 (project.godot `viewport_width`/`viewport_height`).
- [ ] Full map visible at design resolution: bottom grid row (row 13, y 832–896) plus 40px topbar all fit inside 940px height. No vertical overflow.
- [ ] Level 1 unchanged: straight path on `PathRow` (10), spawn (0,10), base (19,10), same waves (3,5,7,9,12), Arrow-only.
- [ ] Level 2 winding path redrawn into the 14-row grid. Reference route (implementer may adjust as long as criteria hold):
  `(0,7) → (3,7) → (3,11) → (7,11) → (7,3) → (11,3) → (11,11) → (15,11) → (15,7) → (19,7)`.
  - [ ] Spawn at left edge (0,7); base at right edge (19,7).
  - [ ] Path winds ≥ 3 times (≥ 3 vertical direction changes), connected, all cells in-bounds (rows 0–13, cols 0–19), no self-intersection.
  - [ ] Tower placement blocked on every path cell.
- [ ] All existing tests that assert a 20-row grid, `PlayAreaHeight == 1280`, or Level 2 spawn/base `(0,10)`/`(19,10)` are updated to the new values and pass.

### Phase B — Stretch / resize-proof

- [ ] project.godot adds `display/window/stretch/mode = "canvas_items"` and `display/window/stretch/aspect = "keep"`.
- [ ] At any window size larger or smaller than the design resolution, the whole canvas (map + HUD + sidebar) scales proportionally and the full map remains visible. Design aspect 1460×940 ≈ 1.55, so mild side letterbox on 16:9 (acceptable).
- [ ] Mouse → grid mapping still correct after scaling: clicking a cell places/selects the intended cell. `PixelToGrid` behavior unchanged in canvas space.
- [ ] Title and result screens still fill the visible area (they size to the viewport).

## Edge Cases

- **Window smaller than design res (laptop):** canvas scales down, map fully visible, input mapping correct, no clipping.
- **Window larger than design res (big monitor):** canvas scales up, map fully visible, crisp geometry, no layout drift.
- **Non-16:9 window (e.g. 16:10, 4:3):** `aspect=keep` letterboxes minimally; HUD/map stay centered and usable.
- **Topbar overlap:** topbar overlays the top ~40px of row 0 — unchanged from current behavior, no gameplay impact.
- **Level 2 path rows near grid edge:** new route keeps all cells in rows 0–13; no cell touches the clipped boundary.
- **Grid coordinate assumptions:** no game or test code may hardcode a row index ≥ 14 (e.g. old row 14, 19) as a valid cell.

## Dependencies

- `GameConstants` — `GridRows` value; `PlayAreaHeight` derives automatically.
- `Levels.cs` — Level 2 corner list redraw.
- `project.godot` — viewport size + stretch settings.
- Tests — `GameConstantsTest` (`GridRows`, `PlayAreaHeight`), `LevelDefinitionTest` (Level 2 spawn/base cells), `GridManagerTest` (boundary pixel-to-grid case) updated to new dimensions.

## Constraints

- **Performance:** stretch is a Godot render-path feature, zero per-frame cost. No new runtime math.
- **Memory:** none.
- **Platform:** desktop windowed; must be resizable and scale cleanly on 13–16" laptop and larger external monitors.

## Visual / Geometry Theme

- No new visuals. Same geometric grid, towers, enemies — just a shorter play area that always fits on screen.
- Procedural geometry only; no AI/external assets.

## Proposed Tuning (constants — human may adjust)

| Constant | Old | New |
|----------|-----|-----|
| GridRows | 20 | 14 |
| GridCols | 20 | 20 (unchanged) |
| CellSize | 64 | 64 (unchanged) |
| PathRow | 10 | 10 (unchanged, Level 1) |
| PlayAreaWidth | 1280 | 1280 (unchanged) |
| PlayAreaHeight | 1280 | 896 |
| viewport_width | 1460 | 1460 (unchanged) |
| viewport_height | 800 | 940 |
| stretch/mode | (unset) | canvas_items |
| stretch/aspect | (unset) | keep |

## Out of Scope

- Truly dynamic per-level grid dimensions (grid size is a global constant, not per-level data).
- Aspect mode `expand` (kept out — `keep` gives a stable centered layout).
- Camera pan/zoom, minimap.
- Any change to gameplay balance beyond the reduced buildable row count.
