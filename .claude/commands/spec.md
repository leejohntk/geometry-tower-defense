---
description: Human describes feature → orchestrator proposes spec + holdout scenarios → human reviews and approves → dark part begins automatically
argument-hint: "Describe the feature you want"
---

# /spec — Feature Specification

Human invokes: `/spec I want a piercing tower that shoots through multiple enemies in a line.`

## Workflow

1. **Orchestrator reads the description** and fills out `.claude/templates/spec-template.md`:
   - Feature name
   - Description
   - Acceptance criteria
   - Edge cases
   - Dependencies (other features, systems)

2. **Orchestrator drafts holdout scenarios** in `.claude/holdouts/{feature-name}/scenarios.md`:
   - Scenarios the implementer agent must not see
   - Tests the orchestrator will run during verification
   - Cover edge cases, performance bounds, integration points

3. **Orchestrator presents both to human** for review.
   - Human can modify, add, remove spec items and holdout scenarios.
   - Human says "approved" when both look good.

4. **Dark factory loop begins automatically.** No separate kickoff command needed.
   - Orchestrator writes state.json, creates feature branch, spawns implementer.
