# Layout Verifier Contract

The headless layout verifier measures the real C# UI. It builds `res://src/Main.tscn`,
walks the Control tree, and evaluates layout assertions derived from the spec's stated
layout intent. It reports named measurements and full coverage, not a verdict without
evidence.

## Run

```bash
godot --headless --path . --script res://tools/layout_verifier.gd
```

Bound the invocation with the Bash tool's `timeout` parameter (for example `120000`
ms). This host has no `timeout`/`gtimeout` binary. A host-level timeout is treated
exactly like exit `2`: fail-closed, never a pass. The verifier also carries its own
watchdog (below), so an uncaught error path terminates the process instead of leaving
it running.

`dotnet build` must have run first: the scene instantiates the compiled C# assembly.
The verifier must run as a `SceneTree` script; `EditorScript` does not run headless.

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Pass: zero findings, at least one layout assertion evaluated |
| `1` | Layout findings (red) |
| `2` | Fail-closed verifier failure (including watchdog/timeout) |

## Watchdog

Uncaught GDScript errors abort the measuring coroutine without calling `quit()`, which
would leave the headless process running forever. The verifier arms a `SceneTreeTimer`
(and a `MainLoop._process` deadline) at startup. If the run has not finished within
`WATCHDOG_DEFAULT_MS` (90 s), it prints `RESULT: FAIL (fail-closed: watchdog fired ...)`
and exits `2`. Tests may shorten the deadline with the `LAYOUT_VERIFIER_WATCHDOG_MS`
environment variable (clamped to 1–600 s).

## Fail-Closed

A verifier failure never reads as a pass. Each of these produces exit `2` or a finding:

- `project.godot` is missing, non-integral, or out-of-range
  `display/window/size/viewport_width` / `viewport_height`
- the root viewport size assignment did not take
- `src/Main.tscn` cannot be loaded or instantiated as a `PackedScene`
- the scene builds no children, or TitleScreen/SkillTreeScreen/GameHUD cannot be resolved
- zero `ResultScreen` overlays resolve (the screen family silently vanishes otherwise)
- `SkillTreeScreen` refuses verification mode, fails to install the isolated tree, or
  reports `IsVerificationStateActive() == false` when isolation is required
- a required named group cannot be resolved (this is a finding, not a skip)
- a required node resolves to the wrong class (finding plus fail-closed exit `2`)
- zero layout assertions are evaluated (preconditions do not count)
- the watchdog fires

## Viewport

The verifier reads the viewport from `project.godot` at runtime. No dimension is
hardcoded, and only integral values in `1..16384` are accepted (a fractional value such
as `0.5` would truncate to `0` and bypass the guard). Under `--headless` the root viewport
is `64x64` regardless of the project, and `get_root().size` reads `(100,100)` during
`_initialize` before settling to `(64,64)`. The verifier therefore assigns
`get_root().size` on frame 1 (an assignment in `_initialize` is overwritten) and verifies
the assignment took before measuring anything.

## Deferred Sorting

Container sorting is deferred: rects are stale until at least one frame after a subtree is
built or mutated. The verifier waits a fixed number of frames after loading the scene and
after every tree rebuild, and asserts the tree is non-empty before measuring.

## Named Groups

Assertions resolve groups by node identity, never by y-coordinate. A wrapper's `Name`
identifies the group (`Row_arrow_pierce`, `Block_Title`, `Panel_Main`,
`Block_Sidebar`). Geometric grouping by y merges unrelated rows and is forbidden.

Every finding cites a resolved path built from those names. A finding that cites a
generated node name such as `@Label@31` without a resolved path is a defect in the
verifier.

## The Assertion Rule

**Every assertion must compare two independently-positioned things** — a row against its
mirror, a panel against the viewport, a column against its sibling column, a rendered
label against a named wrapper.

An assertion that derives its expected value from the same bounding box it measures is
tautological and cannot fail. The recorded failure this rule exists to prevent:

```text
rowBBox.end.x - button.end.x
```

This difference is zero by construction — the button is inside the row — so an 8px
mutation still passed. Do not write anything shaped like that.

## Deterministic State

Rank-dependent rendering (locked rows, greyed labels, buy-button gating) is measured
against a zeroed in-memory state. The screen exposes a three-step handshake:

1. `EnableVerificationMode()` — debug builds only; returns `false` (and changes nothing)
   otherwise, so normal gameplay can never leave the screen in a non-saving state.
2. `ResetProgressForVerification()` — requires step 1; installs an isolated in-memory
   tree whose save delegate is a no-op and returns `true` only on success.
3. `IsVerificationStateActive()` — `true` only after step 2 actually installed the
   isolated tree.

The verifier fails closed if any step is missing, refused, or reports inactive **before**
granting SP or pressing Buy. The hooks never flush, so a developer's
`user://skilltree.cfg` is never modified. Without them, a local save with purchased ranks
would silently remove every locked row from coverage.

Screens that require gameplay state (HUD mid-wave, victory with a real score) cannot be
produced by instantiating `Main.tscn`. They are reported as **skipped with a reason**, never
as passed. Static HUD layout is reachable at startup and is measured; only the dynamic
values are skipped.

## Coverage

The output always prints:

- `EVALUATED (N)` — every assertion, `PASS` or `FAIL`, with measured and expected values
- `SKIPPED (M)` — every skipped assertion and why
- `FINDINGS (K)` — one actionable line per violated assertion
- `LAYOUT ASSERTIONS (L)` — evaluated assertions with harness preconditions excluded
- `COVERAGE: N evaluated, M skipped, K findings`

Zero findings with skips is distinguishable from zero findings with full coverage by
design. A run with `L == 0` is fail-closed even when its own preconditions passed.

## Finding Format

```text
src/UI/SkillTreeScreen.cs: SkillTreeScreen/TreeArea/Row_arrow_pierce/Name: CRITICAL: measured ...; expected ...; <fix>.
```

Each finding names the screen source, the resolved group path, the measured value, the
expected value, and a concrete fix.

## Out of Scope

- Pixel or screenshot verification (`--write-movie` segfaults on the headless renderer)
- Runtime error / crash verification
- Scope-creep detection
- CI integration
- Gameplay logic (SP award rules, damage, timing) that never renders at startup

Coverage gaps from these areas must be recorded explicitly rather than implied away. A
mutation that only changes gameplay logic (for example, awarding SP per kill instead of
per level outcome) produces no layout change and is a coverage gap, not a silent pass.

One targeted lifetime assertion is included despite the runtime-error exclusion: after a
real Buy-button press, the verifier checks that the emitting button is still valid when
the signal returns. `ClearTreeArea` must defer that free (`RemoveChild` + `QueueFree`);
freeing the emitter synchronously is the historical signal-lock crash. This is a state
check on the measured UI, not general error monitoring.
