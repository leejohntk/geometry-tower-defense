---
description: Run harness integrity check — dotnet build, Godot MCP, hooks, agents, settings, memory files
---

# /health — Harness Health Check

Runs `.claude/hooks/health-check.sh` to verify harness integrity.

## What It Checks

1. **dotnet build** — do project files compile?
2. **Godot MCP** — is Godot installed and reachable?
3. **Hook scripts** — do all 6 .sh files exist and are they executable?
4. **Agent definitions** — do all 5 agent .md files exist?
5. **settings.json** — is it valid JSON?
6. **Memory files** — are at least 5 memory .md files present?
7. **MEMORY.md index** — does the index file exist?
8. **Git branch** — are you on main? (warns if yes)

## Usage

- Orchestrator: run at session start to verify harness is intact.
- Human: run anytime to check system health.
