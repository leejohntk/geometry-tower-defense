---
name: harness-evolution
description: History of harness changes — what was changed, why, risk classification, outcome
metadata:
  type: project
---

# Harness Evolution

## Change History

| Date | Change | Risk | Source | Outcome |
|------|--------|------|--------|---------|
| 2026-07-07 | Initial scaffold created | N/A | Human spec | Awaiting first feature |
| 2026-07-10 | First distillation run | N/A | Stop hook (73 tool calls) | 3 low-risk updates applied, 2 medium-risk proposals queued |

## Distillation Log

### 2026-07-10 — First Distillation Run

**Patterns found:**
1. **CI iteration thrashing** — 11+ "fix:" commits on `.github/workflows/ci.yml` in sequence. Agents iterated blind (no local CI validation tool) causing thrash. MEDIUM risk — propose CI iteration guidance.
2. **Hook format thrashing (RESOLVED)** — 4 commits on PreToolUse hook output format. Current state (hookSpecificOutput JSON format, relative hook paths) is correct and stable. No action needed.
3. **Empty session transcripts** — The only transcript file is 38 bytes (end timestamp only). No meaningful session conversation data captured. LOW risk — noted.
4. **Stale memory files** — `harness-feedback.md` and `harness-evolution.md` had placeholder text; `current-feature.md` had no recent features despite PR #4 merged. LOW risk — updated.
5. **First distillation** — No `.last_distillation` file existed. Counter initialized and flag cleared.

**Proposals made:**
- MEDIUM: Add CI iteration guidance to game-design.md or skills — agents should test `dotnet build` and `godot --headless` steps locally before pushing CI changes.
- MEDIUM: Consider adding SessionEnd hook (human action) for session transcript capture — blocked for distiller (high risk).
