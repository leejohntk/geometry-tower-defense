---
name: geometry-tower-defense
description: Godot 4.7 C# geometry-themed tower defense game. Orchestrator agent — never implements. Coordinates sub-agents (implementer, reviewer, investigator, distiller). Runs dark factory loop.
---

# Geometry Tower Defense

Godot 4.7 C# tower defense game with geometric visual theme.

## Build & Run

```bash
# Generate .NET bindings (after Godot install)
godot --headless --build-solutions --quit

# Build
dotnet build

# Run tests
dotnet test

# Run game (headless, for agent verification)
godot --headless --run-stdout

# Run game (for human playtest — launches window)
godot
```

## Architecture

- **Engine:** Godot 4.7 with C# / .NET 8
- **Testing:** GdUnit4Net (selective Godot runtime — `[RequireGodotRuntime]` only when needed)
- **Theme:** Geometric — towers, enemies, projectiles rendered as geometric shapes
- **Source:** `src/` — game code
- **Tests:** `Tests/` — unit + integration tests

## Hard Rules

1. **Never push to main.** Feature branches + PR only. Enforced by pre-tool-use hook (deterministic).
2. **Never edit .tscn by hand.** Use Godot MCP tools (`mcp__godot__*`) for all scene manipulation.
3. **Never launch Godot editor.** `mcp__godot__launch_editor` is for human visual debugging only. Agents use `run_project` for headless verification, humans use the editor GUI directly.
4. **Never read `.claude/holdouts/` files.** Implementer agent must never see holdout scenarios. Enforced by permission deny (deterministic).
5. **Always run `dotnet build` after .cs edits.** Post-tool-use hook reminds. SubagentStop hook blocks "done" if build not run.
6. **Always run tests before claiming work is done.** SubagentStop hook validates.
7. **Serial feature execution.** One feature in flight at a time. Human queues next feature idea while dark part runs.
8. **Orchestrator never implements.** Main session coordinates only. All game code written by implementer sub-agent.

## Terminal States

| State | Meaning | Next |
|-------|---------|------|
| `idle` | No feature active. `state.json` deleted. | Human describes feature → /spec |
| `spec_draft` | Orchestrator wrote spec + holdouts. | Human reviews → /spec approve |
| `spec_approved` | Human approved spec + holdouts. Dark part begins. | Orchestrator spawns implementer |
| `implementing` | Implementer writing code. | Build passes → multi-lens review |
| `reviewing` | 3-lens adversarial review running. | Findings addressed → holdout verification |
| `awaiting_playtest` | All gates green. PR open. | Human playtests |
| `awaiting_fix` | Playtest found issues. | Orchestrator spawns implementer fix |
| `awaiting_merge` | Playtest approved. | Human says "merge it" → agent merges + cleans up |

## Agent Team

- **Orchestrator** (main session, Opus) — plans, coordinates, synthesizes reviews. Never implements.
- **Implementer** (Sonnet) — senior Godot C# developer. Owns all game code + scenes.
- **Reviewer** (Sonnet, 3 parallel) — security, performance, correctness lenses. Fresh context each.
- **Investigator** (Haiku) — read-only code locator. Returns `file:line` tables.
- **Distiller** (Sonnet) — reads transcripts, detects patterns, proposes harness improvements.

Load agent definitions from `.claude/agents/` for full prompts and tool restrictions.

## Test Conventions

- Use GdUnit4Net with selective `[RequireGodotRuntime]` — pure logic tests skip Godot runtime for speed.
- Test files in `Tests/` directory, mirroring `src/` structure.
- Test classes suffixed with `Test` (e.g., `TowerTest`, `EnemyTest`).
- Scene-runner tests for integration. Dedicated test scenes in `Tests/TestLevels/`.
- Run `dotnet test` before marking any work complete.

## Godot MCP

14 tools available. Use for all scene manipulation. Never hand-edit .tscn files.

Key tools: `create_scene`, `add_node`, `load_sprite`, `save_scene`, `run_project`, `stop_project`, `get_debug_output`.

@./.claude/rules/godot-csharp-conventions.md
@./.claude/rules/no-push-to-main.md
@./.claude/rules/verification-gates.md
@./.claude/rules/harness-safety.md
