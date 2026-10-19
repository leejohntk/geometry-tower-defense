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

**LAYOUT:** {State the spatial intent of the UI, not its implementation. Name each visual
group and how it relates to the others: alignment, anchoring, and relationships between
groups (for example "the left-branch rows mirror the right-branch rows about the panel
centre", "the sidebar is anchored to the viewport's right edge", "the title block is
centered on the viewport"). Every named group is measurable by the headless layout
verifier, so state intent in terms of groups and relationships rather than pixel values.}

## Out of Scope

- {Explicitly NOT included in this feature}
- {Saved for future feature}
