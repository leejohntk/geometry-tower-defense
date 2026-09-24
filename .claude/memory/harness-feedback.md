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

### Open 2026-09-23

- **Post-merge `current-feature.md` never gets corrected** — the status flip only happens pre-merge (set to `awaiting_playtest`), and PR #17 removed the post-merge writes because they could not be committed on `main`. Each feature's bookkeeping marks its *predecessor* merged, so with nothing queued the last feature stays `awaiting_playtest` indefinitely. `session-start.sh:37-41` surfaces this file as the "Current Feature Context" block (via `head -5`) and orchestrators read it for status, so the designated status file disagreed with reality until this run corrected it. The structural gap is queued as P4.
- **Merge Cleanup step 6 is advisory** — the distiller flag was set 2026-09-21T04:37:00Z and PR #18 merged 2026-09-22T04:44:57Z with no check; the flag then sat ~2.6 days across a full feature. PR #19 did run the check. Nothing makes the step load-bearing.

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

### 2026-09-23 — Fifth Distillation

First run with **subagent transcripts** in the evidence base (`36b5e55a-…/subagents/*.jsonl`, 27 files). They are where all build/test activity lives — the main transcript carries no sidechains, so prior runs were effectively blind to implementation churn. Reads: `jq` the main JSONL for tool_use; grep the subagent files for `error CS`, `Failed!`, and per-agent tool histograms. Window: PRs #16–#19.

- **Playtest loop costs one implementer spawn per UI tweak** — 8 fix spawns for `feature/tower-skill-tree-framework`, **all 8 on `src/UI/SkillTreeScreen.cs`**, each with its own `state.json` edits and full gate re-run. The 7th and 8th were the same defect class twice (right-align left-branch labels → left-branch rank/coming-soon alignment, overlapping lines). Proposal P3.
- **`--run-stdout` in CLAUDE.md is not a Godot flag** — `godot --help | grep -c run-stdout` → 0; the flag is silently ignored, exits 0. Worse, the documented form has no exit condition, so a headless game never quits and an agent following the doc literally hangs. Agents converged on `--headless --quit-after 120` unaided. Proposal P6.
- **`timeout` does not exist on this host** — `timeout 600 dotnet test …` → `command not found: timeout`; no `gtimeout` either (darwin). One wasted round trip. Proposal P7.
- **`state.json` is written ~46 times per feature pair** (39 Edit + 7 Write + 4 Read) in a fixed two-step ritual: a `phase` edit, then a `blocked_on` edit ~7 s later. One Edit failed on a stale assumed value (2026-09-22T00:21:16.835Z), recovered by `grep -n '"phase"\|"blocked_on"'`. Proposal P5.
- **Run #4's P1/P2 verified landed** (PR #17): zero `reset --hard` this window (three in run #4's), clean fast-forward merges, bookkeeping folded onto the feature branch pre-merge. P2's removal of the post-merge writes is what created the staleness in "What's Broken" — the fix traded one problem for another.
- **Holdout discipline held** — 0 of 12 implementer transcripts contained an `H<n>_*` scenario name; the frontmatter deny fires. Security lens and reviewer `Bash`-freedom (run #3's M3) both hold.
- **Review produced real value** — `src/Grid/GridManager.cs:286`, placement preview uses base range instead of the skill-modified range after Range ranks are bought. Lenses used `Warning:`/`Info:` casing instead of the protocol's `WARNING`/`INFO` (cosmetic).
- **No code thrash** — 2 error classes across 27 subagents, 6 compiler errors total (`CS0246 List<> missing using`, `CS1503 void→string?`). No fix-then-break cycles.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

*(Run #4's P1 and P2 were approved and landed as PR #17 — closed.)*

**P3 (MEDIUM) — `skills/implement-feature/SKILL.md`, Phase 8.** Batch playtest feedback: collect every issue from one playtest pass into a single fix list and spawn **one** implementer for the batch; re-spawn only for a genuinely new defect found after the batch lands. Grounded in 8 spawns that all hit the same file.

**P4 (MEDIUM) — `skills/implement-feature/SKILL.md`, Merge Cleanup.** Restore the post-merge status flip, on a branch. After step 5 add:
```
5b. Flip `.claude/memory/current-feature.md` to the merged/idle status and commit it on a
    short-lived `chore/post-merge-{feature}` branch + PR. Never `git commit` on `main`
    (see `.claude/rules/no-push-to-main.md`). Until this runs, `current-feature.md` keeps
    reporting the merged feature as `awaiting_playtest`.
```
This is run #4's P2 option (a); PR #17 implemented neither option, it just deleted the steps.

**P5 (MEDIUM) — `skills/implement-feature/SKILL.md`, state updates.** Wherever the skill says to update `.claude/state.json`, add: "read it once, then write the whole file with a **single** `Write` when both `phase` and `blocked_on` change." Removes the two-`Edit` ritual and the stale-content failure mode.

**P6 (MEDIUM — CLAUDE.md, outside distiller write scope) — Build & Run.** Replace `godot --headless --run-stdout` with `godot --headless --quit-after 300`. The current flag does not exist and the command has no exit condition.

**P7 (LOW–MEDIUM) — host facts.** Record in `rules/verification-gates.md` or `memory/godot-mcp.md` that the agent shell is macOS `darwin`: no `timeout`, no `gtimeout`; bound long runs with `--quit-after <frames>` instead.

**H3 (HIGH — blocked, human must implement) — re-escalated from runs #3 and #4.**
Guard destructive git ops in `pre-tool-use.sh`: deny `git reset --hard`, `git clean -f`, `git checkout -- .`, `git restore` (when the working tree is dirty). Only the guidance version (M6, "Destructive Git Ops" in `harness-safety.md`) has landed; the deterministic hook guard does not exist. Lower pressure this window — zero occurrences — but still open.

## Harness Change Log

Canonical changelog lives in `.claude/memory/harness-evolution.md` (Change History + Distillation Log). Kept there only, to avoid two copies drifting apart.

---

*Initial harness scaffold created 2026-07-07. First distillation run 2026-07-10.*
