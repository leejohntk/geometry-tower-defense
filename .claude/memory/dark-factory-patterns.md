---
name: dark-factory-patterns
description: Loop phases, handoff protocols, enforcement tiers, and agent orchestration patterns
metadata:
  type: reference
---

# Dark Factory Patterns

## The Loop

```
Human writes feature description
    ↓
Orchestrator proposes spec + holdout scenarios
    ↓
Human reviews both, approves            ← last human touchpoint before implementation
    ↓
═══════ dark part begins ═══════
    ↓
Implementer builds (never sees holdouts)
    ↓
Multi-lens adversarial review (3 parallel, fresh context)
    ↓
Orchestrator verifies against holdout scenarios
    ↓
/simplify refactor pass
    ↓
Orchestrator: "Ready for playtest" + PR open
    ↓
Human playtests
    ├── Issues → feedback → implementer fixes → loop back
    └── Good → human approves → merge cleanup
```

## Phase States

| Phase | state.json | What happens |
|-------|-----------|--------------|
| idle | (deleted) | Waiting for human feature description |
| spec_draft | phase: spec_draft | Orchestrator writing spec + holdouts |
| spec_approved | phase: spec_approved | Human approved. Dark part begins. |
| implementing | phase: implementing | Implementer writing code + tests |
| reviewing | phase: reviewing | 3-lens review + adversarial challenges |
| awaiting_playtest | phase: awaiting_playtest | PR open. Human playtesting. |
| awaiting_fix | phase: awaiting_fix | Playtest found issues. Implementer fixing. |
| awaiting_merge | phase: awaiting_merge | Playtest approved. Ready for merge cleanup. |

## Enforcement Tiers

| Tier | Mechanism | Bypassable? |
|------|-----------|-------------|
| Deterministic | Hook scripts, permission deny, SubagentStop | No — code-enforced |
| Guidance | CLAUDE.md, rules, skills, agent prompts, memory | Yes — model can ignore |

**Decision rule:** If single failure causes lost work, broken builds, or merged bad code → deterministic. If occasional deviation acceptable → guidance.

## Handoff Protocols

### Orchestrator → Implementer
- Pass FULL spec text. NEVER mention holdouts or holdout file paths.
- Include acceptance criteria and edge cases from spec.
- Do NOT include holdout scenarios.

### Implementer → Orchestrator
- Files created/modified list
- Test results summary
- Build status
- Issues/notes

### Reviewer → Orchestrator
- Findings in `path:line: severity: finding. fix.` format
- Severity: CRITICAL | WARNING | INFO
- Lens-specific focus only

### Orchestrator → Human (PR)
- Verification checklist:
  - Build: pass
  - Tests: all pass (N tests)
  - Review: X CRITICAL (fixed), Y WARNING (N addressed), Z INFO
  - Holdouts: all pass (N scenarios)
  - Simplify: done
