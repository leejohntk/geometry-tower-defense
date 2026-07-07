---
paths: ["**/*"]
---

# Harness Safety — Enforcement vs Guidance

Two-tier enforcement framework. Every harness rule classified.

## Enforcement Tiers

| Harness Component | Enforcement | Distiller Can Auto-Modify? |
|---|---|---|
| `settings.json` deny rules | Deterministic (hook-enforced) | No — human only |
| Hook scripts (`.sh`) | Deterministic | No — human only |
| SubagentStop validation | Deterministic | No — human only |
| Rules (`.md`) | Guidance (prompt-level) | Yes |
| Skills (`.md`) | Guidance | Yes |
| Agent prompts (frontmatter) | Guidance | Yes |
| Memory files (`.md`) | Guidance | Yes |
| Commands (`.md`) | Guidance | Yes |

## Decision Rule for New Rules

Ask: "If a single failure causes lost work, broken builds, or merged bad code?"

- **Yes** → deterministic enforcement (hook, permission deny, SubagentStop)
- **No** → guidance (rule file, CLAUDE.md, skill prompt)

## Distiller Safety Classification

Distiller must classify every harness change proposal:

| Risk | Criteria | Action |
|------|----------|--------|
| **Low** | Typo fix, clarification, memory update, non-semantic rewording | Auto-apply |
| **Medium** | New rule, modified skill flow, agent prompt change, new memory file | Propose diff → human approves |
| **High** | Hook change, permission change, SubagentStop change, setting.json change | Blocked — human must manually implement |

## Examples

- Fixing a typo in game-design.md → **low risk** → auto-apply
- Adding "must use object pooling" to godot-csharp-conventions.md → **medium risk** → propose diff
- Changing implementer SubagentStop build validation → **high risk** → blocked
- Updating agent prompt to remind about null checks → **medium risk** → propose diff
- Adding new permission allow for a new tool → **high risk** → blocked
