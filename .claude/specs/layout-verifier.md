# Feature Spec: layout-verifier

**Status:** draft
**Branch:** feature/layout-verifier
**Date:** 2026-10-19

## Description

The dark factory verifies code as text — a build, a test suite, and three reviewers reading the diff. Nothing observes the assembled UI. The result is that layout defects are invisible until a human playtests; the last feature took eight round trips, all on `src/UI/SkillTreeScreen.cs`, and the three that repeated (panel centering and two passes at left-branch label alignment) appear nowhere in that feature's spec.

This feature adds a headless layout verifier. It builds the real C# UI from `src/Main.tscn`, walks the Control tree, and evaluates layout assertions derived from the spec's Visual section. It runs inside the dark part, before human review, and reports named measurements rather than a verdict.

Measured on Godot 4.7.0 mono against this repository: Control layout and font metrics resolve correctly under `--headless`, are bit-identical across runs, and cell-precise rects are available for every node. `--write-movie` and viewport capture are not available headless and are out of scope.

## Acceptance Criteria

- [ ] **AC-1 — Spec template states layout intent.** `.claude/templates/spec-template.md` Visual section gains a `LAYOUT:` field instructing authors to state alignment, anchoring, and relationships between visual groups (for example "left-branch rows mirror right-branch rows about the panel centre"). The field is phrased generically; it does not name any existing screen or defect.

- [ ] **AC-2 — Named visual groups.** Every visual group the UI builds — rank rows, stat rows, title blocks — is wrapped in a `Control` whose `Name` identifies it (for example `Row_arrow_pierce`). `BuildNodeRow` in `src/UI/SkillTreeScreen.cs` is updated to follow this. The convention is recorded in `.claude/rules/godot-csharp-conventions.md` as a general rule, not as a SkillTreeScreen-specific instruction.

- [ ] **AC-3 — Verifier resolves and measures.** A headless GDScript entry point loads `src/Main.tscn`, sets the root viewport size from `project.godot`, waits for deferred container sorting, resolves named groups, and emits one finding per violated assertion.

- [ ] **AC-4 — Viewport comes from the project, never a literal.** The verifier reads `display/window/size/viewport_width` and `viewport_height` at runtime. No dimension is hardcoded.

- [ ] **AC-5 — Assertions compare two independently-positioned things.** No assertion may derive its expected value from the same bounding box it measures. The verifier's contract documents this and gives the failing example (`rowBBox.end.x - button.end.x` is zero by construction and cannot fail).

- [ ] **AC-6 — Coverage is reported, not implied.** The verifier's output lists every assertion it evaluated and every one it skipped, with the reason. Zero findings with skips must be distinguishable from zero findings with full coverage.

- [ ] **AC-7 — Fail-closed.** Scene build failure, unresolved group, zero assertions evaluated, or missing viewport setting each produce a failure result. None may read as a pass.

- [ ] **AC-8 — Findings are actionable.** Each finding names the screen, the group, the measured value, and the expected value, in the existing review format's spirit. A finding that cites a generated node name (`@Label@31`) without a resolved path is a defect in the verifier.

- [ ] **AC-9 — Read-only.** The verifier agent has no `Bash`, `Write`, or `Edit`, following the existing reviewer pattern, so it cannot alter what it measures.

- [ ] **AC-10 — Wired into the dark loop.** `.claude/skills/implement-feature/SKILL.md` runs the verifier after the implementer completes and before multi-lens review. Until stage 3 of the rollout is complete it reports only; it does not block.

- [ ] **AC-11 — Mutation test passes.** Reintroducing each of the **seven layout-observable** defect classes from `feature/tower-skill-tree-framework` one at a time is caught by an assertion. Each mutation is applied to a scratch copy, verified red, and reverted. The eighth class — SP-award rework — has no layout manifestation: it is gameplay logic that never renders at startup, so a layout verifier cannot observe it. It is an **explicitly accepted coverage gap**, recorded in `tools/layout_verifier.md` and reported in the PR coverage output. Coverage gaps are recorded, never silently dropped.

- [ ] **AC-12 — False-positive check passes.** Running the verifier against the current tree produces zero findings.

## Edge Cases

- **Headless root is 64×64.** `get_root().size` reports `(100,100)` at `_initialize` and `(64,64)` thereafter, regardless of the project's configured viewport or `--resolution`. The verifier must assign `get_root().size` on frame 1 and verify the assignment took before measuring. Assertions evaluated against 64×64 produce confident nonsense, not errors.

- **Deferred container sorting.** Control rects are stale until one frame after the subtree is built or mutated. The verifier waits a fixed number of frames and asserts the tree is non-empty before measuring.

- **A screen is not reachable at startup.** Screens that require gameplay state (HUD mid-wave, victory with a real score) cannot be produced by instantiating `Main.tscn`. The verifier reports these as skipped with the reason. It does not silently pass them.

- **A layout intent cannot be resolved to nodes.** The group is missing or ambiguous. This fails; it does not skip. An unresolvable assertion means the convention was not followed.

- **A row shares its y with an unrelated row.** Grouping must use node identity (the named wrapper), never y-coordinate heuristics. Geometric grouping merges unrelated rows and was observed doing so during design.

## Dependencies

- `src/Main.tscn` reachable and buildable headlessly after `dotnet build`
- `project.godot` `display/window/size/viewport_width` and `viewport_height`
- Godot 4.7 mono binary on PATH (`GODOT_BIN` set in `.claude/settings.json`)

## Constraints

- **No new dependencies.** No npm packages, no Godot addons, no new MCP servers.
- **No CI changes.** The verifier runs locally in the dark loop only. `ci.yml` stays headless build-and-test.
- **No game-code behaviour change.** AC-2 adds wrapper nodes; it must not alter positions, sizes, input, or rendering.
- **Runtime:** a full verification run should complete in seconds, not minutes.

## Visual / Geometry Theme

Not applicable. This feature produces text output only and adds no visual elements.

## Out of Scope

- Screenshot or pixel verification (requires a real display; `--write-movie` segfaults headless)
- Runtime error / crash verification
- Scope-creep detection
- The readiness gate and per-criterion `Check:` tags — deferred until the layout verifier has been measured
- CI integration
- Assertions for screens not reachable at startup

## Verification Plan

This feature is harness code, so the standard gates map differently:

**Gate 1 — Build.** `dotnet build` exits 0.

**Gate 2 — Tests.** `dotnet test` unchanged and passing. The verifier's own assertions are exercised by AC-11 and AC-12 rather than by the unit suite.

**Gate 3 — Mutation test (AC-11).** This replaces holdout verification for this feature. The defect classes are taken from the distilled record of `feature/tower-skill-tree-framework`. Seven are layout-observable and must be caught: UI layout rework, node-unlock gating, greyed locked-node colours, `QueueFree` runtime error, tree centering and rank-pip removal, right-aligned left-branch labels, and the left-branch rank/coming-soon alignment. Each is reintroduced in a scratch copy of the project, the verifier is run, and the mutation is expected to produce a finding.

The eighth — SP-award rework — is **out of scope by construction**: it is gameplay logic in `GameManager` that never renders at startup, so no layout assertion can observe it. It is carried as an accepted coverage gap in `tools/layout_verifier.md` and reported in the PR, not silently dropped. Any other class that no assertion catches is likewise recorded as a gap rather than dropped.

**Gate 4 — False-positive check (AC-12).** Verifier run against the clean tree; zero findings expected.

**Gate 5 — Review.** Standard three-lens review.

**Rollout.** After this feature merges, the verifier runs advisory for the next two features. Its findings are compared against actual playtest findings. It is promoted to a blocking gate only after its findings match reality across those features.

## Notes for the Implementer

- Read `BuildNodeRow` in `src/UI/SkillTreeScreen.cs` before changing it. The wrapper must not change any child's global position; reparent with global transform preserved, or add children to a wrapper positioned at the origin.
- The `ContentX` helper already mirrors content for left-branch rows. The wrapper should follow that existing structure rather than introduce a parallel one.
- The verifier script must run under `godot --headless --path . --script <res:// path>`. A script run this way must extend `SceneTree` or `MainLoop`; `EditorScript` will not run headless.
- Do not add `[GlobalClass]` or parameterless constructors to UI classes. The verifier loads `Main.tscn` as a `PackedScene`; direct `.new()` from GDScript is known to fail and is not required.
