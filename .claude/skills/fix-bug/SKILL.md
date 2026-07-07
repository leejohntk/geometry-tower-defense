---
description: Fix a bug reported from playtest or review. Systematic debugging → root cause → fix → test → review.
context: fork
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash
  - Glob
  - Grep
  - Agent
  - mcp__godot__get_debug_output
---

# /fix-bug — Bug Fix Workflow

Orchestrator runs this skill when human reports a bug from playtest or when review finds critical issues.

## Phase 1: Investigate

1. Spawn investigator sub-agent (Haiku) to locate relevant code.
2. Ask targeted questions: "Where is X defined?" "What calls Y?"
3. Get `file:line` tables for all relevant code paths.

## Phase 2: Debug

1. Reproduce the bug if possible (headless run, specific test case).
2. Use investigator to trace the full call path.
3. Identify root cause. Document it.

## Phase 3: Fix

1. Spawn implementer sub-agent with:
   - Root cause analysis
   - Specific files that need changes
   - Expected behavior after fix
2. Implementer writes fix + regression test.
3. Verify `dotnet build` and `dotnet test` pass.

## Phase 4: Verify

1. Run the specific reproduction steps. Confirm bug is fixed.
2. Run full test suite. Confirm no regressions.
3. If bug was found in playtest, re-run holdout scenarios for the affected feature.

## Phase 5: Commit

1. Conventional commit: `fix({scope}): {description}`
2. Push to same feature branch.
3. Update PR with fix details.
