---
description: Run the game and collect structured playtest feedback using the playtest feedback template
---

# /playtest — Human Playtest Session

## What This Does

1. Runs the game: `godot` (launches window for human playtest).
2. After human exits, prompts for feedback using `.claude/templates/playtest-feedback-template.md`.
3. Orchestrator saves feedback to `.claude/memory/playtest-feedback.md`.

## Feedback Collection

Human answers:
- **What I tested:** Which feature or scenario
- **What worked:** Behaviors that feel right
- **What didn't work:** Behaviors that feel wrong, with exact steps to reproduce
- **What felt wrong:** Subjective experience issues (pacing, feel, balance)
- **What I want next:** Priority for next feature or fix

## After Feedback

If issues found:
- Orchestrator writes bug report + spawns fix-bug workflow.
- Update state.json to `awaiting_fix`.

If all good:
- Orchestrator says: "Ready to merge. Say 'merge it' when you are ready."
- Human approves → merge cleanup begins.
