---
paths: ["**/*"]
---

# Verification Gates

Gates that must pass before work can be claimed done.

## Gate 1: Build

```bash
dotnet build
```
Must exit 0. No warnings preferred. Warnings count as soft-fail (orbiter notes, doesn't block).

## Gate 2: Tests

```bash
dotnet test
```
All tests pass. No skipped tests without documented reason.

## Gate 3: Multi-Lens Review

Three reviewer sub-agents (security, performance, correctness) review all changes. Findings categorized:
- **Critical:** Must fix before PR
- **Warning:** Should fix. Orchestrator decides.
- **Info:** Noted for future cleanup.

Adversarial challenge rounds resolve conflicting findings. Orchestrator synthesizes final verdict.

## Gate 4: Holdout Validation

Orchestrator runs holdout scenarios against implementation. Scenarios stored in `.claude/holdouts/` (implementer never sees them). All holdouts must pass.

## Gate 5: Simplify

`/simplify` refactor pass after all other gates green. Removes dead code, consolidates duplication, improves naming.

## Gate 6: SubagentStop Validation

Implementer SubagentStop hook validates:
- `dotnet build` was run and passed (deterministic — blocks "done" if not)
- `dotnet test` was run and passed (deterministic)

Reviewer SubagentStop hook validates:
- Findings follow format: `path:line: severity: finding. fix.`
