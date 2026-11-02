# Feature Spec: space-bar-speedup

**Status:** approved
**Branch:** feature/space-bar-speedup
**Date:** 2026-11-01

## Description

The space bar becomes a context-sensitive control during a level. When the next wave is ready
(the Start Wave button is enabled), pressing space starts that wave exactly as clicking the
button does. While a wave is active, pressing space toggles a 1.5× whole-game-clock speedup on
and off. Only one of the two actions can fire per press. While speedup is active, the top bar
shows an animated fast-forward cue. Speedup persists across waves and resets when the level
ends or the player returns to the title.

## Acceptance Criteria

- [ ] **Space starts a wave when ready.** In `GameState.Playing`, with no active wave, not in
      tower-placement mode, pressing Space starts the next wave through the same path as the
      Start Wave button: wave increments/spawns, the Start Wave button becomes disabled, and no
      tower-placement state changes.
- [ ] **Space toggles speed mid-wave.** In `GameState.Playing` with an active wave, pressing
      Space toggles `Engine.TimeScale` between `1.0` and `1.5` (the +50% mode). Starting a wave
      and toggling speed can never both happen on one press.
- [ ] **Speed persists across waves.** Completing a wave does not reset the clock; the next
      wave starts at the current speed. Speedup stays active until the level ends, the player
      returns to the title, or a new level starts.
- [ ] **Reset points.** `Engine.TimeScale` returns to `1.0` on game over, victory, return to
      title, and new-level initialization, regardless of the previous toggle state.
- [ ] **No-op states.** Space does nothing (and does not change speed) on the title screen,
      while a result overlay is visible, in tower-placement mode between waves, or after the
      last wave has completed.
- [ ] **Space is consumed during play.** A Space key-down handled by Main is marked handled so
      a focused HUD/title `Button` cannot also activate through `ui_accept` in the same press.
      Only Space triggers this feature; Enter and gamepad `ui_accept` do not.
- [ ] **Key repeat is ignored.** Holding Space produces one action per physical press; the key
      release does not act.
- [ ] **Top-bar cue appears only while active.** At `1.0×` the cue is hidden. At `1.5×` it is
      visible and animating (fast-forward chevrons sweeping right with an alpha pulse). The cue
      is a visual-only, click-through `Control` group named `Block_Speedup` containing a named
      `FastForwardIcon`; it never consumes input and does not overlap the wave stat block or the
      sidebar.
- [ ] **Gameplay actually runs faster.** Wave spawn cadence, enemy movement, projectile motion,
      and tower fire timing all scale with the engine clock while speedup is active; game logic
      (kills, coins, wave completion) remains correct with no double-processing.
- [ ] **Tests isolate the global clock.** Every test that changes the speed restores
      `Engine.TimeScale` to `1.0` before completing, so suite ordering cannot leak speed.

## Edge Cases

- **Space during tower placement while a wave is active:** toggles speed (a wave is running);
  it never starts a wave while placement mode is active.
- **Space during tower placement between waves:** no-op; Start Wave is disabled while placing.
- **Wave ends while speedup is on:** speed remains 1.5×; the next Space press starts the next
  wave (it does not toggle speed because no wave is active).
- **Last wave completes while speedup is on:** victory path resets to 1.0× and hides the cue.
- **Game over / victory fires while Space is held:** reset once; key repeat cannot re-enable it.
- **Result overlay visible / returning to title:** Space is inert; the cue is gone.
- **HUD button has keyboard focus:** Space routes to the context action only, never to the
  focused button.
- **Player toggles speed rapidly:** each press flips once; no intermediate values other than
  1.0 and 1.5 are ever set.
- **Existing input paths:** mouse placement/selection, Escape cancel, and upgrade-panel clicks
  are unchanged at either speed.

## Dependencies

- `Main._Input` routing and the existing `OnStartWavePressed` path (single source of truth for
  starting a wave and updating the Start Wave button).
- `GameManager` game state and `WaveManager.IsWaveActive` / `StartNextWave`.
- `GameHUD` top bar (`Block_TopBar`, `Block_Wave`) and its signal wiring.
- Godot `Engine.TimeScale`; `Timer` nodes and `_Process(delta)` scale with it by default.
- Project input setup: Space must be distinguishable from Enter (`ui_accept` includes both).
  A dedicated Space check or action is required.

## Constraints

- **Performance:** the top-bar cue animates in `_Process` without per-frame allocations; stop
  processing while hidden.
- **Memory:** no new per-frame objects; cue geometry drawn procedurally like the other HUD icons.
- **Platform:** keyboard Space only; mouse behavior unchanged; works at both `1.0×` and `1.5×`.
- **Determinism:** speed state lives in one owner (`GameManager`) and is exposed to the HUD via a
  signal; Main does not independently track a second copy.
- **Global clock hygiene:** every exit path (game over, victory, title, new level, test
  teardown) restores `Engine.TimeScale = 1.0`.

## Visual / Geometry Theme

- **Cue:** two right-pointing solid triangles (classic fast-forward), each ~14 px tall with a
  ~4 px gap, drawn in the HUD accent color (cyan/white family already used by top-bar values).
- **Animation:** triangles sweep ~6 px to the right on a ~0.8 s loop with an alpha pulse
  (roughly 0.55 → 1.0); the animation runs only while the cue is visible. At `1.0×` the group is
  hidden and stops processing.
- **Placement:** vertically centred in the 40 px top bar, right-aligned inside the play-area
  portion of the bar (left of the sidebar), with a clear gap from `Block_Wave`.

**LAYOUT:** The `Block_Speedup` visual group is anchored to the top bar's right edge over the
play area, vertically centred, and sits to the right of `Block_Wave` with no overlap. It is
inside `Block_TopBar`, never covers the sidebar or battle grid, and every descendant is
`MouseFilter = Ignore`. It is hidden at 1× and occupies the same rect at 1.5× (appearance
animation only; no layout shift). The group and `FastForwardIcon` have stable names resolvable
by the headless layout verifier.

## Out of Scope

- Speeds other than 1.0× and 1.5× (no 2×/3× cycle).
- Pause.
- Clickable speed control or remappable keybinds.
- Gamepad/controller binding for the toggle.
- Changing wave difficulty or balance to compensate for speedup.
