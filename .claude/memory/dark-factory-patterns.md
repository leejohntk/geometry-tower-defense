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
- Pass FULL spec text. NEVER mention holdout scenarios, their paths, IDs (`H<n>`), or scenario
  names.
- Include acceptance criteria and edge cases from spec.
- Do NOT include holdout scenarios.
- Scope file listings and searches to `src/`, `Tests/`, and named harness files. Do not root a
  recursive listing at the repo or put the holdouts path in an exclusion glob: the PreToolUse
  guard matches the literal string and denies the whole command. This cost one round trip on
  2026-10-30, when `find … -not -path "*/.claude/holdouts/*"` was denied.

### Implementer → Orchestrator
- Files created/modified list
- Test results summary
- Build status
- Issues/notes

### Orchestrator → Reviewer
- Generate the diff (`git diff main...HEAD`, plus `git diff HEAD` and the untracked-file list).
  For diffs under ~1,500 lines, paste the text. For larger diffs, write it to a temp file and pass
  the absolute path; tell the reviewer to read it in chunks until every line is covered.
- Reviewers have Read/Glob/Grep only — no shell. Never ask them to run `git diff`, build, or test.
- Give each reviewer one lens; do not cross-contaminate.
- Never time-kill a reviewer that is still producing tool activity. Wait for the completion
  notification; the correctness lens took 24m42s on a 9,315-line diff on 2026-10-30. Split by
  subsystem only after one attempt stalls, because cross-cutting bugs span subsystems.

### Reviewer → Orchestrator
- Findings in `path:line: severity: finding. fix.` format
- Severity: CRITICAL | WARNING | INFO
- Lens-specific focus only

### Playtest Deltas
- Accepted playtest fixes update the spec and affected holdouts before merge. If the human expands
  scope, update `Out of Scope` too.
- Re-run holdouts after the final playtest batch. A pre-playtest "23/23" is not evidence about
  post-playtest code; nine fix batches on visual-overhaul changed pinned values with no spec or
  holdout update.

### Orchestrator → Human (PR)
- Verification checklist:
  - Build: pass
  - Tests: all pass (N tests)
  - Review: X CRITICAL (fixed), Y WARNING (N addressed), Z INFO
  - Holdouts: all pass (N scenarios)
  - Simplify: done
