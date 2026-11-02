# Holdout Scenarios — space-bar-speedup

Implementer must never see this file. Run every scenario after build + test gates are green.
A scenario passes only when every expected observation is verified; "looks right" is not a pass.

## H1 — Space starts a ready wave

**Setup:** Start Level 1 through the real Main graph. Do not click Start Wave.

**Steps:**
1. Confirm the Start Wave button is enabled and no wave is active.
2. Push a real Space key-down through the viewport.
3. Read wave number, `IsWaveActive`, Start Wave button disabled state, tower count.

**Expected:**
- Exactly one wave starts (wave label increments once; `IsWaveActive` true).
- Start Wave button becomes disabled in the same path a click uses.
- No tower is placed and placement mode is not entered.

## H2 — Space toggles 1.0× / 1.5× mid-wave

**Setup:** Wave active.

**Steps:**
1. Read `Engine.TimeScale` before input.
2. Push Space; read scale, cue group visibility, cue animation state.
3. Push Space again; read scale and cue again.

**Expected:**
- First press: `1.5`, cue visible/animating.
- Second press: `1.0`, cue hidden and no longer processing.
- No value other than 1.0/1.5; no wave restart or extra spawn on either press.

## H3 — One action per press, correct priority

**Steps:**
1. Between waves with speedup off, push Space → wave starts, speed stays 1.0.
2. Mid-wave push Space → speed becomes 1.5, wave count does not change.
3. Toggle speed on, then let the wave complete; push Space with the next wave ready.

**Expected:**
- Between-wave press starts exactly one wave and never toggles speed.
- Mid-wave press toggles speed and never starts/restarts a wave.
- After completion, Space starts the next wave; speed remains 1.5 (persistence).

## H4 — Reset points

**Setup:** Speedup active (1.5×).

**Steps:** Individually trigger each exit: game over, victory, return to title, start a new level.

**Expected:** Every path leaves `Engine.TimeScale == 1.0`, cue hidden/stopped, and a fresh level
starts at 1.0. No path leaves a stale 1.5× clock behind the title or result screen.

## H5 — No-op states

**Steps:** Push Space in each: title screen; game-over overlay; victory overlay; tower-placement
mode between waves; after the final wave completed.

**Expected:**
- No wave starts, no tower placed, no speed change, cue state unchanged.
- In particular, placement mode between waves does not start a wave on Space.

## H6 — Key repeat, release, and GUI focus

**Steps:**
1. Push Space pressed with `Echo = true`; observe no action.
2. Push Space with `Pressed = false`; observe no action.
3. Give the Start Wave button keyboard focus, then press Space mid-wave.
4. Push an Enter key-down mid-wave and between waves.

**Expected:**
- Echo and release are inert.
- A focused button never activates from the Space press; the context action runs once.
- Enter triggers nothing from this feature.

## H7 — Top-bar cue contract

**Steps:** Inspect the HUD at 1.0 and 1.5.

**Expected:**
- Group `Block_Speedup` with named child `FastForwardIcon` exists under `Block_TopBar`.
- Hidden at 1.0; visible and animating at 1.5; same rect at both states (appearance only).
- All descendants `MouseFilter = Ignore`; clicks through it still reach the battlefield.
- Inside the top bar, right of `Block_Wave`, no overlap with `Block_Wave`, the sidebar, or the
  grid. Layout verifier passes with zero findings.

## H8 — The clock really scales gameplay

**Setup:** Wave active; a controlled observer (headless test scene or instrumented manager).

**Steps:**
1. At 1.0×, record enemy position/ spawn-count progress over a fixed number of real frames.
2. At 1.5×, record the same for the same number of real frames.

**Expected:**
- Movement/spawn progress is ~1.5× (within timing tolerance), not unchanged.
- Toggling back to 1.0× returns progress to 1×; no duplicated or skipped logic updates.

## H9 — Game logic under speedup

**Steps:** Play/kill enemies while speedup is on; complete a wave; let a leak occur.

**Expected:**
- Coins/kills increment once per enemy; wave completion fires once; no double-destroy,
  double-credit, or double base damage. Wave counter and HUD stay consistent.

## H10 — Mouse and other input unaffected

**Steps:** With speedup active, place a tower by mouse, select a tower, close the upgrade panel,
and press Escape while placing.

**Expected:** Every mouse/Escape path behaves exactly as at 1.0×; the top-bar cue never eats a
click; tower placement still lands on the clicked cell.
