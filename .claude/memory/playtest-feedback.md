---
name: playtest-feedback
description: Human playtest observations, patterns across features, recurring issues
metadata:
  type: feedback
---

# Playtest Feedback

## By Feature

### Visual overhaul (2026-10-30 → 2026-11-01, PR #31)

Nine fix batches, all sent to one implementer via `SendMessage`. Requests in order:
skill-tree node icons 22→40 px, then 40→46 px; icon strokes looked pixel-sharp (per-draw
antialiasing, then global 2D MSAA 4×); remove internal parenthetical values from skill
descriptions while keeping the rank-3 relationship; restyle the top status bar; drop `$`
from placement-card costs; base-core pulse read as static, then the re-added scale
animation shimmered (final: static geometry + counter-rotating shield arcs).

## Patterns

- Visual taste converges over 2–3 passes. The costly rounds were copy, currency format,
  antialiasing, and motion policy — decisions the spec had not pinned — not gameplay.
- Pin player-facing copy and motion defaults in the spec; fold accepted deltas into the
  spec and regression holdouts before merge.

## Recurring Issues

- **Spec drift after playtest.** Visual-overhaul's spec still pins 22×22 icons, `15$`
  costs, and a top bar listed as Out of Scope. See `current-feature.md` follow-ups.
