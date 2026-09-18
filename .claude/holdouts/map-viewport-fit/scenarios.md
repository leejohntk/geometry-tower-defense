# Holdout Scenarios: map-viewport-fit

Implementer must not see this file. Orchestrator runs these during holdout verification (Phase 5).

## H1 — Grid dimensions are landscape
- **Setup:** Inspect `GameConstants`.
- **Check:** GridCols == 20, GridRows == 14, CellSize == 64, PlayAreaWidth == 1280, PlayAreaHeight == 896.
- **Expected:** PlayAreaWidth > PlayAreaHeight. No leftover `PlayAreaHeight == 1280`.

## H2 — Full map fits design resolution, no vertical overflow
- **Setup:** Read project.godot viewport + stretch settings.
- **Check:** viewport 1460 × 940; stretch mode canvas_items; aspect keep. `PlayAreaHeight (896) + TopBarHeight (40) = 936 <= 940`.
- **Expected:** Bottom grid row (row 13, y 832–896) fully within window height. No scroll/resize needed to see the base row.

## H3 — Level 1 regression
- **Setup:** Load Level 1.
- **Check:** Straight path, all waypoints row 10. Spawn (0,10), base (19,10). Waves (3,5,7,9,12). Arrow-only, no cannon button.
- **Expected:** Identical to pre-feature behavior.

## H4 — Level 2 spawn/base at left/right edge, near vertical center
- **Setup:** Inspect Level 2 path.
- **Check:** SpawnCell == (0,7), BaseCell == (19,7). First waypoint col 0, last col 19.
- **Expected:** Left/right edge, row 7 (near vertical center of a 14-row grid).

## H5 — Level 2 path valid in 14-row grid
- **Setup:** Inspect Level 2 path waypoints.
- **Check:** All cells in-bounds (row 0–13, col 0–19). Connected. No self-intersection. VerticalDirectionChangeCount >= 3.
- **Expected:** `IsPathConnectedAndInBounds(path, 20, 14)` true; `PathSelfIntersects` false; vertical changes >= 3. No cell row >= 14.

## H6 — Tower placement blocked on off-center path cells (Level 2)
- **Setup:** Load Level 2. Attempt PlaceTower on a winding-path cell not in row 7 (e.g. (3, 11) or (7, 5)).
- **Check:** CanPlaceTower false.
- **Expected:** Rejected, no coin deducted, no tower created.

## H7 — Stretch: smaller window keeps full map visible
- **Setup:** Launch at a window smaller than design res (e.g. 1280×800 or simulate scaled viewport). Screenshot/verify layout.
- **Check:** Whole map (all 14 rows) visible, sidebar visible, no clipping.
- **Expected:** Canvas scales down proportionally; map + HUD all on screen.

## H8 — Mouse → grid mapping correct after scaling
- **Setup:** In a scaled window, move mouse to a known cell corner and read PixelToGrid.
- **Check:** PixelToGrid returns the intended (col,row) for the cell under the cursor.
- **Expected:** Placement preview highlights the cell under the cursor; a click places the tower there. No offset drift from stretch.

## H9 — Headless verification gates pass
- **Setup:** Run `dotnet build` then `dotnet test`.
- **Check:** Build exit 0; all tests pass, including updated GameConstantsTest / LevelDefinitionTest / GridManagerTest.
- **Expected:** No test still asserts GridRows == 20, PlayAreaHeight == 1280, or Level 2 spawn/base (0,10)/(19,10).

## H10 — No hardcoded out-of-range rows remain
- **Setup:** grep src/ and Tests/ for row indices.
- **Check:** No level/code hardcodes row 14 or row 19 as a valid cell coordinate (Level 2 old route removed; GridManagerTest boundary case updated to row 13).
- **Expected:** Only rows 0–13 referenced as valid grid rows.
