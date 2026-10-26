---
description: Implement a feature from spec through PR. Dark factory loop: spec → plan → branch → implement → multi-lens review → holdout verify → simplify → PR.
context: fork
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash
  - Glob
  - Grep
  - Agent
  - TaskCreate
  - TaskUpdate
  - mcp__godot__create_scene
  - mcp__godot__add_node
  - mcp__godot__load_sprite
  - mcp__godot__save_scene
  - mcp__godot__run_project
  - mcp__godot__stop_project
  - mcp__godot__get_debug_output
---

# /implement-feature — Dark Factory Loop

Orchestrator runs this skill when human approves spec + holdouts.

## State file updates (applies to every phase below)

`.claude/state.json` is tiny. Whenever this skill says "update state", **read it once, then write the
whole file with a single `Write`** — do not issue one `Edit` for `phase` and a second `Edit` for
`blocked_on`. The two-`Edit` ritual cost ~46 writes per feature pair and produced a failed `Edit`
against stale assumed content. One read + one `Write` is atomic, cheap, and cannot drift.

Note: this file is gitignored, so it never needs committing and never appears in a PR.

## Phase 1: Spec

Human describes feature. Orchestrator writes the spec to `.claude/specs/{feature}.md` (template: `.claude/templates/spec-template.md`) and commits it on the feature branch. Orchestrator drafts holdout scenarios in `.claude/holdouts/{feature}/`. Human reviews both and says "approved."

## Phase 2: Plan

1. Write `.claude/state.json`: `{"feature": "{name}", "phase": "spec_approved", "branch": "feature/{name}", ...}`
2. Create feature branch: `git checkout -b feature/{name}`
3. Update `.claude/memory/current-feature.md`

## Phase 3: Implement

1. **Spawn implementer sub-agent** (Sonnet, `.claude/agents/implementer.md`).
2. Pass the FULL spec. NEVER mention holdout scenarios or file paths.
3. Implementer writes code + tests, runs `dotnet build` and `dotnet test`.
4. SubagentStop hook validates build + tests passed (deterministic gate).
5. Update state: `"phase": "implementing"`

## Phase 3.5: Layout Verification (advisory during rollout)

After the implementer completes and **before** multi-lens review, run the headless layout
verifier against the real C# UI. Bound the invocation with the Bash tool's `timeout`
parameter (this host has no `timeout` binary), for example **120000 ms**:

```bash
godot --headless --path . --script res://tools/layout_verifier.gd
```

Exit codes: `0` = pass with coverage, `1` = layout findings, `2` = fail-closed verifier
failure. The output lists every assertion evaluated, every assertion skipped with its
reason, and a `LAYOUT ASSERTIONS` count excluding harness preconditions; never treat a
skipped assertion as a pass. A host-level timeout (the Bash tool killing the process) is
treated as **fail-closed exit 2**, never a pass. The verifier also carries its own
90-second watchdog so an uncaught error still exits `2` instead of hanging.

1. **Pass (exit 0):** record `COVERAGE: N evaluated, M skipped` and the
   `LAYOUT ASSERTIONS` count in the PR checklist and continue to Phase 4.
2. **Findings (exit 1):** spawn the read-only `layout-verifier` sub-agent with the full
   verifier output. Send its findings to the implementer as a normal fix batch (Phase 8
   batching rules apply — one implementer for the whole batch). Re-run after the fix.
3. **Fail-closed (exit 2) or timeout:** this is a verifier or build failure, not a pass.
   Fix it before review; if the scene cannot build headlessly, stop and surface it.
4. **Rollout:** until stage 3 of the rollout is complete, layout findings are advisory.
   Record them in the PR checklist and in playtest notes, but do not block the PR. It
   becomes a blocking gate only after its findings have matched actual playtest findings
   across two features.

## Phase 4: Multi-Lens Review

1. **Spawn 3 reviewer sub-agents in parallel** (Sonnet, `.claude/agents/reviewer.md`). First generate the diff yourself (`git diff main...HEAD` plus `git diff HEAD` for working-tree changes and the untracked-file list) and paste that diff text into every reviewer prompt. Reviewers have Read/Glob/Grep only — never ask them to run git, build, or test commands:
   - Lens 1: Security
   - Lens 2: Performance
   - Lens 3: Correctness
2. Each returns findings in `path:line: severity: finding. fix.` format.
3. Orchestrator groups findings by file + location.
4. Run adversarial challenge rounds on conflicting findings.
5. Synthesize final verdict. Critical findings = send back to implementer.
6. Update state: `"phase": "reviewing"`

## Phase 5: Holdout Verification

1. Orchestrator reads holdout scenarios from `.claude/holdouts/{feature}/`.
2. Run each scenario against the implementation.
3. All holdouts must pass. If any fail, send back to implementer with specific details.
4. Document holdout results for PR checklist.

## Phase 6: Simplify

1. Run `/simplify` refactor pass on the diff.
2. Remove dead code, consolidate duplication, improve naming.
3. Verify build + tests still pass.

## Phase 7: PR

1. Stage all game-code + test changes, commit with conventional commit message.
2. Final bookkeeping commit on the feature branch (so `main` stays clean after merge):
   - Move holdouts to `.claude/holdouts/regression/{name}/`.
   - Update `.claude/memory/current-feature.md` (mark feature complete + add to Recent Features).
3. Push feature branch.
4. Create PR via `gh pr create` with verification checklist from `.github/pull_request_template.md`.
5. Fill checklist: build status, test results, review findings (summary), holdout results, simplify pass done.
6. Update state: `"phase": "awaiting_playtest"`

## Phase 8: Human Playtest

Human playtests. Two outcomes:
- **Approved:** Human says "merge it." Agent runs merge cleanup (see below).
- **Issues found:** Human provides feedback. **Batch it** — collect *every* issue from that playtest
  pass into a single fix list and spawn **one** implementer for the whole batch. Do not spawn one
  implementer per issue. Re-spawn only for a genuinely new defect discovered *after* the batch lands
  (e.g. a regression the batch introduced, or something the human only sees once the batch is in).
  Loop back to Phase 3.

  Rationale: `tower-skill-tree-framework` needed 8 fix spawns, **all 8 editing the same file**
  (`src/UI/SkillTreeScreen.cs`), each paying a full build/test/gate cycle. Several of those issues
  arrived in the same message and could have ridden one spawn.

## Merge Cleanup (post-approval)

1. `gh pr merge --squash {pr_url}`
2. `git fetch origin && git checkout main && git pull --ff-only`. If the working tree is dirty, STOP and surface the files to the human — never `git reset --hard`, `git clean -f`, or `git restore` to clear it.
3. `git branch -d feature/{name}`
4. Delete `.claude/state.json`
5. Prune merged worktrees — for every `.claude/worktrees/agent-*` whose HEAD is an ancestor of `main`, `git worktree remove --force` it (subagent-isolation scratch is superseded once merged).
6. Check `.claude/transcripts/.distiller_needed`; if set, run `/evolve-harness` (distillation) before declaring merge complete.
