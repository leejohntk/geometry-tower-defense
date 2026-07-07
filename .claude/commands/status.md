---
description: Show current project status — active feature, phase, branch, last checkpoint
---

# /status — Project Status

Show current state of the dark factory loop.

## What It Shows

- **Active feature:** From `.claude/memory/current-feature.md`
- **Current phase:** From `.claude/state.json`
- **Branch:** Current git branch
- **Last checkpoint:** Timestamp from state.json
- **Build status:** Last `dotnet build` result
- **Test status:** Last `dotnet test` result
- **Open PR:** URL if PR exists
- **Next step:** What should happen next

## Implementation

Orchestrator reads:
1. `.claude/state.json` for phase and feature
2. `.claude/memory/current-feature.md` for feature details
3. `git branch --show-current` for branch
4. Last build/test output (if cached)

Presents as clean summary table.
