---
name: harness-evolution
description: History of harness changes — what was changed, why, risk classification, outcome
metadata:
  type: project
---

# Harness Evolution

## Change History

| Date | Change | Risk | Source | Outcome |
|------|--------|------|--------|---------|
| 2026-07-07 | Initial scaffold created | N/A | Human spec | Awaiting first feature |
| 2026-07-10 | First distillation run | N/A | Stop hook (73 tool calls) | 3 low-risk updates applied, 2 medium-risk proposals queued |
| 2026-09-17 | Second distillation run | N/A | Stop hook (flag set 2026-09-17, counter 45) | 2 low-risk memory updates applied, 4 medium proposals queued, 1 high-risk finding escalated |

## Distillation Log

### 2026-07-10 — First Distillation Run

**Patterns found:**
1. **CI iteration thrashing** — 11+ "fix:" commits on `.github/workflows/ci.yml` in sequence. Agents iterated blind (no local CI validation tool) causing thrash. MEDIUM risk — propose CI iteration guidance.
2. **Hook format thrashing (RESOLVED)** — 4 commits on PreToolUse hook output format. Current state (hookSpecificOutput JSON format, relative hook paths) is correct and stable. No action needed.
3. **Empty session transcripts** — The only transcript file is 38 bytes (end timestamp only). No meaningful session conversation data captured. LOW risk — noted.
4. **Stale memory files** — `harness-feedback.md` and `harness-evolution.md` had placeholder text; `current-feature.md` had no recent features despite PR #4 merged. LOW risk — updated.
5. **First distillation** — No `.last_distillation` file existed. Counter initialized and flag cleared.

**Proposals made:**
- MEDIUM: Add CI iteration guidance to game-design.md or skills — agents should test `dotnet build` and `godot --headless` steps locally before pushing CI changes.
- MEDIUM: Consider adding SessionEnd hook (human action) for session transcript capture — blocked for distiller (high risk).

### 2026-09-17 — Second Distillation Run

**Evidence base:** git history since 2026-07-11 (PRs #8, #9, c532f69), harness file inspection. Session transcripts were again empty, so no conversation-level analysis was possible.

**Patterns found:**
1. **Inert PreToolUse hooks in agent frontmatter (HIGH)** — `.claude/agents/reviewer.md:12`, `investigator.md:12`, `distiller.md:19` use the legacy `{"decision": "deny"}` output that PR #9 empirically proved Claude Code v2+ ignores. The implementer was migrated; these three were not. `distiller.md` additionally parses `$1` instead of stdin JSON. Net effect: the reviewer/investigator read-only guards and the distiller write-scope guard never fire, contradicting the "deterministic, cannot be bypassed" claims in `harness-safety.md` and `dark-factory-patterns.md`.
2. **Distillation skipped ~2.5 months (MEDIUM)** — flag set 2026-09-17T02:25:51, previous run 2026-07-11, counter at 45 vs threshold 12. Nothing gates on the flag, so a whole feature shipped undistilled.
3. **Undefined spec output path (MEDIUM)** — `commands/spec.md` and `skills/implement-feature/SKILL.md` reference only the template, never an output location. `.claude/specs/` was invented ad hoc, is referenced by no harness file, and is untracked in git.
4. **Simulation/presentation conflation (MEDIUM, single occurrence)** — swarm formation offsets were erased by `MoveAlongPath` snapping `Position`. Fixed in `src/Enemies/Enemy.cs` via `_anchorPosition` + `_formationOffset`. Worth encoding as guidance; not enough evidence for a deterministic rule.
5. **Reviewer `Bash` bypasses read-only (MEDIUM)** — reviewer and investigator hold the `Bash` tool, so even a correctly wired `Write|Edit` guard would not make them read-only.
6. **No thrashing detected (INFO)** — PR #8's 6 commits are feature + playtest polish + one correctness fix + one refactor. No fix-then-break cycles. The CI thrashing from the first run did not recur.

**Proposals made:**

- **H1 (HIGH — blocked, human must implement).** Migrate the three remaining agent hooks to the v2 contract. For each of `reviewer.md`, `investigator.md`, `distiller.md`, replace the legacy command block with the pattern already proven in `implementer.md`: read stdin (`INPUT=$(cat)`), match the path/command, then

  ```
  printf '{"hookSpecificOutput": {"hookEventName": "PreToolUse", "permissionDecision": "deny", "permissionDecisionReason": "<reason>"}}\n'
  ```

  Note the distiller block must switch from `$1` to stdin JSON *and* from a `case` allow-list to a deny-on-non-harness-path decision, or it will keep falling through. Also consider `exit 0` rather than `exit 1` on deny.

- **M1 (MEDIUM).** Name the spec output path. In `commands/spec.md` step 1 and `skills/implement-feature/SKILL.md` Phase 1, state that the spec is written to `.claude/specs/{feature}.md` (matching the `level-2-swarm-cannon` name already in use) and that it is committed on the feature branch so it survives alongside the holdouts moved to `regression/`.

- **M2 (MEDIUM).** Add to `rules/godot-csharp-conventions.md`: keep simulation position and visual/formation offset as separate fields; never snap or overwrite the simulation anchor to achieve a presentation effect. Cite `src/Enemies/Enemy.cs` `_anchorPosition` / `_formationOffset` as the reference implementation.

- **M3 (MEDIUM).** Reviewer/investigator agent prompts: either drop `Bash` from their tool lists, or extend the read-only rule to say that shell redirection and in-place edits (`>` `>>` `tee` `sed -i`) are also prohibited. Today the read-only guarantee rests on a hook that does not fire.

- **M4 (MEDIUM).** Make the distiller flag actionable. In `agents/orchestrator.md` responsibility 8 and `skills/implement-feature/SKILL.md` Merge Cleanup, require checking `.claude/transcripts/.distiller_needed` and running `/evolve-harness` before declaring `awaiting_merge` / completing merge.

- **H2 (HIGH — blocked, carried forward from 2026-07-10).** Session transcript capture requires a SessionEnd hook or a `stop.sh` change. Both are human-only. Still the single largest limit on distillation quality.

**Applied this run (both LOW risk, memory only):** refresh of `harness-feedback.md` (new "What's Broken" entry for the inert hooks; 2026-09-17 observations; changelog pointer deduplicated) and `harness-evolution.md` (change-history row + this log entry). No rule, skill, agent-prompt, or command file was modified — all four medium proposals await human approval, per the tier table in `harness-safety.md`.
