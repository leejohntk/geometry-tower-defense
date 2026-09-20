---
name: current-feature
description: Active feature tracking — what's being worked on, state, branch, progress
metadata:
  type: project
---

# Current Feature

**Status:** idle

No active feature. Waiting for human to describe next feature.

## Feature Queue

(Empty.)

## Recent Features

- **Map viewport fit** (PR #12) — grid reshaped 20×20 → 20×14 (`GridRows` 20→14, `PlayAreaHeight` 1280→896), window 1460×940, `canvas_items`/`aspect=keep` stretch mode, Level 2 path redrawn (spawn (0,7), base (19,7), rows {3,7,11}). Fixes map-overflow-at-launch; resize-proof. Merged 2026-09-18.
- **Harness fix** (PR #9) — agent-scoped holdouts deny (implementer-only, correct `hookSpecificOutput` format) + `GODOT_BIN` env. Merged 2026-09-17.
- **Level 2 + swarm + cannon** (PR #8) — level select, winding path, swarm ring cluster (orbiting), cannon tower (AoE + explosion animation). Merged 2026-09-16.
- **First playable level** (PR #4) — grid system, enemy movement, arrow tower, wave spawning, HUD, title screen, result screen. Merged 2026-07-10.
