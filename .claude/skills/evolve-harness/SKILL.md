---
description: Distiller workflow. Read session transcripts, detect patterns, propose harness improvements. Auto-applies low-risk changes, escalates medium/high-risk to human.
context: fork
allowed-tools:
  - Read
  - Write
  - Edit
  - Bash
  - Glob
  - Grep
  - Agent
---

# /evolve-harness — Distiller Workflow

Triggered automatically by stop hook when activity threshold met (12+ tool calls, 2+ file edits since last distillation). Human can also invoke manually.

## Process

1. **Spawn distiller sub-agent** (Sonnet, `.claude/agents/distiller.md`).
2. Pass the latest transcript files from `.claude/transcripts/`.
3. Distiller analyzes for patterns:
   - **Repeated errors:** Same build failure, same test pattern failure across sessions
   - **Thrashing:** Fix-then-break cycles on same file
   - **Tool misuse:** Agent calling wrong MCP tool, wrong bash command
   - **Missed conventions:** Code violating CLAUDE.md rules
   - **Missing guidance:** Areas where agents lacked instructions
4. Distiller proposes changes with risk classification (low/medium/high):
   - **Low risk:** Auto-applied by distiller
   - **Medium risk:** Orchestrator presents diff to human for approval
   - **High risk:** Logged, human must manually implement

## Safety Boundaries

**Distiller can modify:**
- `.claude/skills/`, `.claude/rules/`, `.claude/memory/`
- `.claude/agents/` (prompt body only, not hook frontmatter)
- `.claude/commands/`, `.claude/templates/`

**Distiller CANNOT touch:**
- `.claude/settings.json`, `.claude/hooks/`
- Agent hook frontmatter
- Any game code (`src/`, `Tests/`, `*.cs`, `*.tscn`, `project.godot`)
