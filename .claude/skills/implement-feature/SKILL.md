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

## Phase 1: Spec

Human describes feature. Orchestrator proposes spec using `.claude/templates/spec-template.md`. Orchestrator drafts holdout scenarios in `.claude/holdouts/{feature}/`. Human reviews both and says "approved."

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

## Phase 4: Multi-Lens Review

1. **Spawn 3 reviewer sub-agents in parallel** (Sonnet, `.claude/agents/reviewer.md`):
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

1. Stage all changes, commit with conventional commit message.
2. Push feature branch.
3. Create PR via `gh pr create` with verification checklist from `.github/pull_request_template.md`.
4. Fill checklist: build status, test results, review findings (summary), holdout results, simplify pass done.
5. Update state: `"phase": "awaiting_playtest"`

## Phase 8: Human Playtest

Human playtests. Two outcomes:
- **Approved:** Human says "merge it." Agent runs merge cleanup (see below).
- **Issues found:** Human provides feedback. Orchestrator spawns implementer fix. Loop back to Phase 3.

## Merge Cleanup (post-approval)

1. `gh pr merge --squash {pr_url}`
2. `git checkout main && git pull`
3. `git branch -d feature/{name}`
4. Move holdouts to `.claude/holdouts/regression/{name}/`
5. Mark feature complete in `.claude/memory/current-feature.md`
6. Delete `.claude/state.json`
7. Trigger distiller on session transcript
