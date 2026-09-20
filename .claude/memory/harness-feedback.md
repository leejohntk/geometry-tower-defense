---
name: harness-feedback
description: Self-referential feedback on the harness itself — what's working, what's broken, distiller observations
metadata:
  type: feedback
---

# Harness Feedback

## What's Working

- **Stop hook + distiller flag** — tool counter correctly triggers distiller_needed flag at threshold (confirmed at 73 calls). Flag mechanism works end-to-end.
- **PreToolUse hook** — correctly blocks `git push main/master`, `git push --force`, `rm -rf`, `sudo`. `hookSpecificOutput` JSON format works with current Claude Code version.
- **Feature pipeline (PR #4)** — successful end-to-end run: spec → implementer → review → holdouts → simplify → merge. Game code healthy.
- **SessionStart hook** — provides good context: branch warnings, crash recovery, distiller status, health checks.
- **Agent-scoped holdouts deny + GODOT_BIN (PR #9)** — global `Read(.claude/holdouts/**)` removed; implementer blocked from holdouts via a frontmatter PreToolUse hook (correct `hookSpecificOutput.permissionDecision` format — the old `{"decision": "deny"}` shape was silently ignored). Reviewer + orchestrator keep access. `env.GODOT_BIN` set so bare `dotnet test` passes. Verified: implementer Read denied, reviewer Read allowed, orchestrator `ls` allowed.

## What's Broken

(None open.)

### Resolved 2026-09-20

- **Distillation run #3 memory lost** — run #3 updated `.last_distillation` but its `harness-evolution.md` log entry and `harness-feedback.md` notes were committed on `main` and then discarded by the `reset --hard` / branch shuffle. Reconstructed in run #4.
- **Distillation publishing rule** — distillations MUST publish through a PR branch (e.g. `chore/distillation-N`), never by committing on `main`. Commits on `main` diverge from `origin/main`, can never be pushed, and are lost the moment a cleanup runs `reset --hard`. This is why run #3's record vanished.

### Resolved 2026-09-18

- **Inert PreToolUse hooks** — reviewer/investigator/distiller migrated to the v2 `hookSpecificOutput.permissionDecision` deny format (PR #10). Distiller's harness-write-scope guard now enforces and reads stdin JSON (not `$1`).
- **Session transcript capture** — distiller now reads the native Claude Code JSONL at `~/.claude/projects/` (full conversation data), not the empty `.claude/transcripts/*.txt` stubs. No SessionEnd hook required after all.

## Distiller Observations

### 2026-07-10 — First Distillation

- CI thrashing is the most significant pattern: 11+ commits in a row to fix CI suggests agents lack local CI validation capability. Consider adding CI iteration guidance.
- Memory files had stale placeholder text. Updated to reflect current state.
- No prior distillation had ever run — counter system works correctly.

### 2026-09-17 — Second Distillation

- **Flag was set but never acted on for ~2.5 months** (flagged 2026-09-17T02:25:51, last run 2026-07-11; counter reached 45 against a threshold of 12). An entire feature — level 2 / swarm / cannon (PR #8) — shipped with no distillation. The flag is advisory: nothing gates on it. `session-start.sh` reports the status and `implement-feature/SKILL.md` step 7 mentions triggering, but neither blocks. Proposal M4 below.
- **Highest-value finding:** the inert agent hooks (see "What's Broken"). PR #9 fixed the implementer and explicitly documented that the legacy format fails, but the three sibling hooks were never migrated.
- **Swarm formation bug** (PR #8 `fix(swarm)` commit): cluster members stacked because `MoveAlongPath` snapped `Position` to the shared waypoint, erasing the per-member offset. Root cause was conflating the simulation anchor with a presentation offset. Resolved in `src/Enemies/Enemy.cs` by keeping `_anchorPosition` + `_formationOffset` separate and recomputing `Position = anchor + offset` each tick. Only one observed occurrence, so proposed as guidance (M2), not a rule change.
- **`.claude/specs/` is orphaned** — no harness document references it. `commands/spec.md` names the *template* (`templates/spec-template.md`) but never an output path, so the orchestrator invented `.claude/specs/`. Result: the level-2 spec exists only as untracked local state and shows as `?? .claude/specs/` in every session's git status. Proposal M1.
- **Thrash check (PR #8):** 6 commits — 3 feature, 2 playtest-driven polish (cost rebalance, explosion animation, orbit), 1 correctness fix, 1 dead-code refactor. No fix-then-break cycles. Healthy iteration, not thrashing.
- Working as intended: counter/threshold mechanism, SessionStart context injection, agent-scoped holdouts deny (implementer verified denied).

### 2026-09-20 — Fourth Distillation

- **Run #3 record was lost** (see "What's Broken → Resolved 2026-09-20"). Reconstructed here and in `harness-evolution.md`. Lesson: distillations publish through a PR branch, never on `main`.
- **Merge cleanup git step drifted to `reset --hard`** — after PRs #14 and #15 the agent ran `git reset --hard origin/main` (three invocations; one denied by the auto-mode classifier for "Irreversible Local Destruction" on a dirty tree). The skill's `git checkout main && git pull` is silent on the dirty-tree case, so agents improvise destructively. Proposal P1 below.
- **Post-merge bookkeeping is stranded on `main`** — Merge Cleanup steps 4–6 write `current-feature.md` / holdouts / `state.json` while on `main`, but "No Local Commits on Main" means those writes can never be committed. `current-feature.md` sat dirty all window. Proposal P2 below.
- **Level-4 review caught a real bug** — multi-lens Security lens found a CRITICAL (`WaveManager` null-forgiving deref after mid-wave reset → NRE + leaked enemy); fixed before merge. Review pipeline working. The null-forgiving `!` idiom recurs in `GameManager.cs`; watch for it, consider a convention later.
- **Flag cadence** — flag re-tripped 30 min after run #3 with counter 31 vs threshold 12. Working as designed.

**Queued proposals (await human approval — do not apply to skills/rules without sign-off):**

**P1 (MEDIUM) — `skills/implement-feature/SKILL.md`, Merge Cleanup step 2.**
Replace:
```
2. `git checkout main && git pull`
```
with:
```
2. `git fetch origin && git checkout main && git pull --ff-only`
```
and append to the step: "If the working tree is dirty, STOP — surface the modified files to the human before any checkout/pull, and never run `git reset --hard` / `git clean -f` / `git restore` to clear them (see `.claude/rules/harness-safety.md` → Destructive Git Ops)."

**P2 (MEDIUM) — `skills/implement-feature/SKILL.md`, Merge Cleanup post-merge bookkeeping.**
After step 6 (`Delete .claude/state.json`), add:
```
6b. Post-merge bookkeeping is NOT committed on `main`. Either (a) commit it on a short-lived `chore/post-merge-{feature}` branch and open a PR, or (b) leave it uncommitted and fold it into the next feature branch's first commit. Pick one per cleanup; never `git commit` on `main` (see `.claude/rules/no-push-to-main.md` → No Local Commits on Main).
```

**H3 (HIGH — blocked, human must implement) — re-escalated from run #3.**
Guard destructive git ops in `pre-tool-use.sh`: deny `git reset --hard`, `git clean -f`, `git checkout -- .`, `git restore` (when the working tree is dirty). Only the guidance version (M6, "Destructive Git Ops" in `harness-safety.md`) has landed; the deterministic hook guard does not exist.

## Harness Change Log

Canonical changelog lives in `.claude/memory/harness-evolution.md` (Change History + Distillation Log). Kept there only, to avoid two copies drifting apart.

---

*Initial harness scaffold created 2026-07-07. First distillation run 2026-07-10.*
