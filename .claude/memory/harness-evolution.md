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
| 2026-09-20 | Third distillation run | N/A | Stop hook (completed 2026-09-20T03:54:07, counter 31) | Proposals landed as PR #14 (M3, M6, M7, H4, M8, "No Local Commits on Main" rule); run's own log entry lost (committed on main, then discarded by reset/branch shuffle) — reconstructed below in run #4 |
| 2026-09-20 | Fourth distillation run | N/A | Stop hook (flag set 2026-09-20T04:24:24, counter 31) | 2 low-risk memory updates applied; 2 medium skill proposals queued (merge-cleanup git safety + post-merge bookkeeping); 1 high finding re-escalated (H3 hook guard for destructive git ops) |

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

### 2026-09-20 — Third Distillation Run (record reconstructed in run #4)

**Status:** Completed 2026-09-20T03:54:07Z (`.last_distillation` updated, counter reset). Its proposals landed as PR #14, but its own memory log entry was **lost** — committed on `main`, then discarded by the `git reset --hard origin/main` / branch-delete shuffle (see run #4 Pattern 3). This entry reconstructs the record from PR #14's content and the scope note.

**Window:** PR #13 armored-laser merge review.

**Proposals (all landed via PR #14, squash commit `2954c0d`):**
- **M3 (MEDIUM, applied)** — `Bash` dropped from `reviewer.md` / `investigator.md` tool lists.
- **M6 (MEDIUM, applied)** — "Destructive Git Ops" section added to `.claude/rules/harness-safety.md`.
- **M7 (MEDIUM, applied)** — CLAUDE.md `.NET 8` → `.NET 10`.
- **H4 (HIGH, human-applied)** — `Edit(.claude/settings.json)` deny in `.claude/settings.json`.
- **M8 (MEDIUM, applied)** — stale `.claude/worktrees/agent-*` pruned.
- **NEW rule (MEDIUM, applied)** — "No Local Commits on Main" section in `.claude/rules/no-push-to-main.md`.
- **M5 (SKIPPED)** — prefer Glob/Grep over find/ls. Human answered but ultimately skipped. Treat as open only with fresh evidence; no fresh evidence found this run.
- **H3 (HIGH, still open)** — guard `reset --hard` / `clean -f` / `checkout -- .` / `restore` in the pre-tool-use hook. Only the guidance version (M6) landed; no hook guard exists yet. Re-escalated in run #4.

### 2026-09-20 — Fourth Distillation Run

**Evidence base:** native Claude Code JSONL `3c60a3c1-…-7ff6e0ace051.jsonl` (window after 2026-09-20T03:54:07Z), git history (PRs #14, #15), harness file inspection.

**Patterns found:**
1. **Merge Cleanup git step contradicts Destructive Git Ops rule (MEDIUM)** — `skills/implement-feature/SKILL.md` Merge Cleanup step 2 is `git checkout main && git pull`, but agents drifted to `git reset --hard origin/main` after PRs #14 and #15 (three invocations; one denied by the auto-mode classifier as "Irreversible Local Destruction" with a dirty working tree). The skill is silent on the dirty-tree case, so agents improvise. Proposed: step 2 → `git fetch origin && git checkout main && git pull --ff-only`, plus a line that any dirty working tree must be surfaced to the human before cleanup proceeds.
2. **Post-merge bookkeeping stranded on main (MEDIUM)** — Merge Cleanup steps 4–6 write to the working tree while on `main`, but the new "No Local Commits on Main" rule means those writes can never be committed. `current-feature.md` sat dirty for the whole window. Proposed: explicit step that post-merge bookkeeping is committed on a short-lived `chore/post-merge-{feature}` branch + PR, or stated as committed on the *next* feature branch.
3. **Run #3 memory writes lost (LOW, applied this run)** — `.last_distillation` was updated but `git log --all --grep="distillation memory"` returned nothing and `harness-evolution.md` had no third-run entry. Root cause: distillation committed on `main`, then discarded by the reset/branch shuffle. Fixed here by reconstructing the run #3 record and adding the "publish through a PR branch" rule to `harness-feedback.md`.
4. **Distiller flag cadence (INFO)** — flag set 04:24:24Z ~30 min after run #3, counter 31 vs threshold 12. Working as designed (reset then re-trip). No action.
5. **Null-forgiving `!` idiom (INFO)** — level-4 multi-lens review caught a Security CRITICAL (`WaveManager` null-forgiving deref after mid-wave reset → NRE + leaked enemy). Review process worked correctly. `!` appears in several places in `GameManager.cs`; if it recurs, consider a convention that `!` must immediately follow an explicit null check on the same field. No rule proposed yet.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history rows + this log entry + reconstructed run #3), `harness-feedback.md` (PR-branch publishing rule + queued medium proposals). Two medium skill proposals and the H3 high finding are queued for human review — not applied, per the tier table in `harness-safety.md`.
