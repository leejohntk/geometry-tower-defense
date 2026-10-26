# Feature Spec: {feature-name}

**Status:** draft | approved | implementing | done
**Branch:** feature/{feature-name}
**Date:** {YYYY-MM-DD}

## Description

{One paragraph describing what this feature does from the player's perspective.}

## Acceptance Criteria

- [ ] {Criterion 1 — specific, testable}
- [ ] {Criterion 2}
- [ ] {Criterion 3}

## Edge Cases

- **{Scenario}:** {Expected behavior}
- **{Scenario}:** {Expected behavior}
- **{Scenario}:** {Expected behavior}

## Dependencies

- {System or feature this depends on}
- {Godot node type or resource needed}

## Constraints

- **Performance:** {e.g., must handle 50+ enemies without frame drops}
- **Memory:** {e.g., must use object pooling for projectiles}
- **Platform:** {e.g., must work with keyboard + mouse input}

## Visual / Geometry Theme

- {Visual representation — which geometric shapes?}
- {Color scheme}
- {Animation style}
- {Entity scale — for in-game objects, state rendered size in grid-cell units (cell = 64 px,
  `GameConstants.CellSize`) as well as pixels, and how the silhouette compares with existing
  entities. A 96 px boss reads as 1.5 cells; say that before implementation.}
- {Telegraph timing — if the entity has a signature windup or attack tell, state when the first
  tell appears relative to spawn or lifetime, not just the repeat interval. A first tell that
  lands after the entity is usually dead reads as "never happens."}

**LAYOUT:** {State the spatial intent of the UI, not its implementation. Name each visual
group and how it relates to the others: alignment, anchoring, and relationships between
groups (for example "the left-branch rows mirror the right-branch rows about the panel
centre", "the sidebar is anchored to the viewport's right edge", "the title block is
centered on the viewport"). Every named group is measurable by the headless layout
verifier, so state intent in terms of groups and relationships rather than pixel values.}

## Out of Scope

- {Explicitly NOT included in this feature}
- {Saved for future feature}
