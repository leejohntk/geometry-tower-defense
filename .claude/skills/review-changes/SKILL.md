---
description: Run multi-lens adversarial code review on current diff or branch. Spawns 3 parallel reviewers + adversarial challenge rounds.
context: fork
allowed-tools:
  - Read
  - Bash
  - Glob
  - Grep
  - Agent
---

# /review-changes — Multi-Lens Adversarial Review

Orchestrator runs this skill during the review phase of feature implementation, or human invokes manually for ad-hoc review.

## Process

1. **Get the diff:** `git diff main...HEAD` or `git diff` for staged changes.
2. **Spawn 3 reviewers in parallel:**
   - Lens: `security` — "Review this diff for security issues. Look for unsafe input handling, resource leaks, missing validation, injection risks."
   - Lens: `performance` — "Review this diff for performance issues. Look for hot-path allocations, GC pressure, missing object pooling, unnecessary _Process calls."
   - Lens: `correctness` — "Review this diff for correctness issues. Look for logic errors, edge cases, null safety, signal wiring, resource loading."
3. **Synthesize findings:**
   - Group by file + approximate location.
   - Identify conflicts (one reviewer says critical, another says fine).
   - Run adversarial challenge: prompt each reviewer with conflicting findings and ask them to argue against each other's positions.
   - Orchestrator makes final judgment on conflicts.
4. **Output final review report** with severity breakdown.
