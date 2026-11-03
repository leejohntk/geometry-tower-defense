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
| 2026-09-23 | Fifth distillation run | N/A | Stop hook (flag set 2026-09-21T04:37:00, counter 53) | 3 low-risk memory updates applied (this log, `harness-feedback.md`, `current-feature.md` staleness); 5 medium proposals queued (playtest-loop batching, state.json write pattern, post-merge status flip, CLAUDE.md `--run-stdout`, macOS `timeout`). H3 re-escalated. Run #4's P1/P2 verified landed via PR #17 |
| 2026-09-24 | PR #21 — run #5 follow-ups applied | MEDIUM/HIGH | Human-approved distillation follow-ups | P3, P5, P6, P7 landed. H3 hook guard implemented: force-push matching now uses standalone tokens, command extraction parses stdin with jq, `HEAD:main`/`refs/heads/main` refspecs covered, destructive git ops gated on repo state. Guard verified 17/17 |
| 2026-10-19 | Sixth distillation run | N/A | Stop hook (flag set 2026-09-24T02:12:12, counter 64) | 2 low-risk memory files updated; 3 medium proposals queued (stale `[RequireGodotRuntime]` convention, holdout enumeration, host shell facts); 1 high option (recursive `.claude` enumeration guard) |
| 2026-10-25 | Seventh distillation run | N/A | Stop hook (flag set 2026-10-25T21:55:42, counter 62) | 4 low-risk memory files updated (run log, feedback, stale PR #25 status, grid-relative sizing + telegraph timing); P8 and P10 recurred and were re-queued with new evidence; 3 new proposals (P11 reviewer diff handoff, P12 spec visual scale, `.gitignore` test artifact); P9 and H5 unchanged |
| 2026-10-28 | Eighth distillation run | N/A | Stop hook (flag set 2026-10-26T04:31:03, counter 44 at merge) | 3 low-risk memory files updated (run log, feedback, `current-feature.md` merge cleanup); 5 medium proposals queued (P13 holdout-ID leak, P14 reviewer convergence budget, P15 orchestrator wait/kill policy, P16 fork API-422 recovery, P17 UI state matrix); 1 high option logged (H6 holdout-ID prompt guard) |
| 2026-11-01 | Ninth distillation run | N/A | Stop hook (flag set 2026-10-30T22:32:13; `/evolve-harness` invoked 28 s after PR #31 merged) | 6 low-risk memory files updated (run log, feedback, `current-feature.md` post-merge/status cleanup, `software-factory-patterns.md` handoff guidance, `game-design.md` copy/AA/motion conventions, `playtest-feedback.md`); P14/P15 revised after evidence their caps would have suppressed the CRITICAL; 3 new proposals (P18 playtest-delta fold-back, P19 large-diff review/wait policy, P20 merge hygiene) + H7 exclusion-aware guard; P13/P16/P17 no recurrence |
| 2026-11-03 | Tenth distillation run | N/A | Stop hook (flag set 2026-11-02T01:09:50; `/evolve-harness` invoked 55 s after PR #37 merged) | 4 low-risk memory files updated (run log, feedback, `current-feature.md` post-merge status, `godot-mcp.md` headless cursor fact); P20 revised (branch cleanup + stale tracking refs); 2 new proposals (P21 holdout test artifacts in the implementer workspace, P22 spec status lifecycle) + H8 holdout-artifact guard; P19/P13/P18 followed; P10 recurred twice, remains declined |
| 2026-11-03 | Eleventh distillation run | N/A | Stop hook (flag set 2026-11-03T04:19:54Z; `/evolve-harness` invoked 55 s after PR #39 merged) | 3 low-risk memory files updated (run log, feedback, `current-feature.md` PR #39 merge status + Recent Features); P20 recurred (4th), P21 recurred inside the implementer SubagentStop window, P22 recurred; new P23 + H9 (unshipped holdout scenarios readable through `git show` on the feature branch); P13/P19/P18/P5/P3 followed |

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
1. **Inert PreToolUse hooks in agent frontmatter (HIGH)** — `.claude/agents/reviewer.md:12`, `investigator.md:12`, `distiller.md:19` use the legacy `{"decision": "deny"}` output that PR #9 empirically proved Claude Code v2+ ignores. The implementer was migrated; these three were not. `distiller.md` additionally parses `$1` instead of stdin JSON. Net effect: the reviewer/investigator read-only guards and the distiller write-scope guard never fire, contradicting the "deterministic, cannot be bypassed" claims in `harness-safety.md` and `software-factory-patterns.md`.
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

### 2026-09-23 — Fifth Distillation Run

**Evidence base:** native Claude Code JSONL — session `36b5e55a-8638-4970-9a35-a6b12a9185ab.jsonl` (1,790 lines, 2026-09-21T03:21Z → 2026-09-24T00:25Z) **plus its 27 subagent transcripts** in `36b5e55a-…/subagents/` (previously unexamined; this is where all build/test activity lives — the main transcript carries no sidechains), and the post-run-#4 tail of `3c60a3c1-…` (2026-09-20T05:01Z → 16:22Z). Window covers PR #16, #17, #18, #19.

**Verification of run #4's proposals:** P1 and P2 landed as PR #17 and both hold — the merge-18/19 cleanups used `git checkout main && git pull --ff-only` with no `reset --hard` (three `reset --hard` invocations appeared in run #4's window; zero this window), and bookkeeping is folded into the feature branch pre-merge, so the tree stayed clean through both merges.

**Patterns found:**

1. **Playtest micro-iteration loop — one implementer spawn per UI tweak (MEDIUM).** The `awaiting_playtest ↔ awaiting_fix` cycle ran 8 times for `feature/tower-skill-tree-framework`, each trip costing 2–4 `state.json` edits plus a full implementer spawn, build, test, and SubagentStop gate. **All 8 fix spawns edited the same file** (`src/UI/SkillTreeScreen.cs`): UI layout rework, node-unlock gating, grey-locked-node colors, SP-award rework, `QueueFree` runtime error, tree centering/rank-pip removal, right-align left-branch labels, then *left-branch rank/coming-soon alignment again* — the last two are the same defect class re-touched in overlapping lines (`nameLabel.Size` / `rankLabel.Size` / `comingSoon.Size` sizing block, then its `AddThemeColorOverride` colors). Not a regression, but two full gate cycles for one visual area.

2. **Post-merge `current-feature.md` is permanently stale (MEDIUM).** `session-start.sh:9,37-41` prints this file as the "Current Feature Context" block (`head -5`, so the frontmatter; orchestrators then read the file itself for status). The run #4 fix (PR #17) moved bookkeeping *pre-merge* because post-merge writes land on `main` and can never be committed — but that dropped the only step that flipped the status **after** a merge. Each feature's bookkeeping therefore marks its *predecessor* merged, and when the human queues nothing, the last feature stays `awaiting_playtest` forever. Live evidence: `current-feature.md:10` still read `**Status:** awaiting_playtest` and `:12` "PR open" while PR #19 is merged (2026-09-24T00:24:30Z), `state.json` is deleted, and the repo is `idle` — so the designated status file disagreed with reality until this run corrected it. Run #4's P2 offered option (a) — a short-lived `chore/post-merge-{feature}` branch + PR — and PR #17 took neither option; it just deleted the steps.

3. **Merge Cleanup step 6 (distiller check) skipped once, followed once (MEDIUM).** The flag was set 2026-09-21T04:37:00Z. PR #18 merged 2026-09-22T04:44:57Z — the session went straight from `gh pr merge` to `state.json` edits, no flag check; the flag then sat ~2.6 days across a whole feature. PR #19 merged 2026-09-24T00:24:30Z and *did* run step 6 (Skill `evolve-harness` at 00:24:53Z, 12s after `rm .claude/state.json`). The step works when followed; nothing makes it load-bearing. Same class as run #2's M4.

4. **`state.json` churn and one guess-based Edit failure (LOW–MEDIUM).** 39 `Edit` + 7 `Write` + 4 `Read` on `.claude/state.json` in this window. The shape is ritualised: a `phase` edit followed ~7 s later by a `blocked_on` edit, every transition. At 2026-09-22T00:21:16.835Z an Edit failed — `<tool_use_error>String to replace not found in file. String:   "phase": "awaiting_fix",</tool_use_error>` — the orchestrator wrote a stale assumed value; recovery was a `grep -n '"phase"\|"blocked_on"'` (00:21:21Z) then the real edit at 00:21:24Z.

5. **CLAUDE.md documents a verification command that does not exist (MEDIUM, out of distiller write scope).** Build & Run says `godot --headless --run-stdout`. `--run-stdout` is not a Godot 4.7 flag: `godot --help | grep -c run-stdout` → `0`, and `godot --headless --run-stdout --quit` exits 0 with the flag silently ignored. It also has **no exit condition** — a headless game with no `--quit-after` runs forever, so an agent following the doc literally hangs. Agents ran the documented form verbatim twice and converged empirically on `/opt/homebrew/bin/godot --headless --quit-after 120` (4 uses this window). CLAUDE.md is outside the distiller's allowed-modify list, so this is proposal-only.

6. **`timeout` does not exist on this host (LOW).** `timeout 600 dotnet test --filter "FullyQualifiedName~SkillMechanicsTest"` → `(eval):1: command not found: timeout` (2026-09-24T00:09:44Z), retried bare 2 s later. Confirmed: no `timeout`, no `gtimeout` (darwin 25.6.0). One wasted round trip; a one-line host fact prevents the next one.

7. **Holdout discipline held (INFO, positive).** 0 of 12 implementer transcripts contained any `H<n>_*` holdout scenario name; the implementer frontmatter deny fires on `.claude/holdouts/` paths. The leak surface is closed.

8. **Review pipeline healthy (INFO, positive).** 6 reviewer spawns (3 lenses × 2 features), all **Bash-free** — run #3's M3 holds. Findings were substantive, e.g. correctness lens caught `src/Grid/GridManager.cs:286`: the placement preview draws the base `GameConstants.TowerRange(towerType)` and ignores the skill-modified range, so after buying Arrow/Laser Range ranks the preview understates the placed tower's reach (4 cells/256px vs 6.5 cells/416px). Minor: lens text used `Warning:`/`Info:` casing rather than the `WARNING`/`INFO` the handoff protocol specifies in `software-factory-patterns.md`.

9. **Spec granularity rework (INFO).** A combined `tower-skill-trees` spec + holdout set was deleted mid-flight and re-split into `tower-skill-tree-framework` + `tower-skill-tree-mechanics`; both parts merged cleanly. First attempt at a two-part feature had the wrong cut, and the two-part pattern is now proven — worth recording as the default for features this size, but not as a rule.

10. **No build/test thrash (INFO).** Across 27 subagent transcripts: 2 distinct error classes totalling 6 compiler errors — `CS0246 List<> not found` (missing `using System.Collections.Generic;`, 4× in one agent) and `CS1503 cannot convert from 'void' to 'string?'` (2×). No repeated failure class, no fix-then-break cycle on game code.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P3 (MEDIUM) — `skills/implement-feature/SKILL.md`, Phase 8.** Batch playtest feedback: collect every issue from one playtest pass into a single fix list and spawn **one** implementer for the batch; re-spawn only for a genuinely new defect found after the batch lands. Cite Pattern 1 (8 spawns, all `SkillTreeScreen.cs`).
- **P4 (MEDIUM) — `skills/implement-feature/SKILL.md`, Merge Cleanup.** Re-add the post-merge status flip, on a branch: after step 5, flip `.claude/memory/current-feature.md` to the merged/idle status and commit it on a short-lived `chore/post-merge-{feature}` branch + PR (run #4's P2 option (a)) — never on `main`. Otherwise the file keeps reporting the merged feature as `awaiting_playtest` (Pattern 2).
- **P5 (MEDIUM) — `skills/implement-feature/SKILL.md`, state updates.** Wherever the skill says to update `.claude/state.json`, say: read it once, then write the whole file with a **single** `Write` when both `phase` and `blocked_on` change. Replaces the two-`Edit` ritual and the stale-content failure in Pattern 4.
- **P6 (MEDIUM, CLAUDE.md — outside distiller scope) — Build & Run.** Replace `godot --headless --run-stdout` with `godot --headless --quit-after 300` (or similar), since `--run-stdout` is not a Godot flag and the documented form has no exit condition (Pattern 5).
- **P7 (LOW–MEDIUM) — host facts.** Record in `rules/verification-gates.md` (or `memory/godot-mcp.md`) that agent shell is macOS `darwin`: no `timeout`, no `gtimeout`; bound long runs with `--quit-after <frames>` or the tool's own timeout (Pattern 6).
- **H3 (HIGH — blocked, human must implement) — re-escalated from runs #3 and #4.** Guard `git reset --hard`, `git clean -f`, `git checkout -- .`, `git restore` in `pre-tool-use.sh` when the working tree is dirty. Guidance (M6) has landed and no `reset --hard` occurred this window, so the pressure is lower — but the deterministic guard still does not exist.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #5 observations + queued proposals), `current-feature.md` (corrected the stale `awaiting_playtest` status to idle/merged — the live instance of Pattern 2). No rule, skill, agent-prompt, command, or template file was modified; all five medium proposals and H3 await human approval per the tier table in `harness-safety.md`.

### 2026-10-19 — Sixth Distillation Run

**Evidence base:** native JSONL session `93e5a1e3-…` (main transcript, 1,321 lines, 110 tool calls) plus 14 subagent transcripts — 7 general-purpose research agents (2026-09-24T03:00–03:39Z), 3 implementers, 3 reviewers, and 1 claude-code-guide (2026-10-19) — covering PR #21 (run #5 follow-ups + PreToolUse guard) and PR #22 (headless layout verifier). Window starts at run #5's `.last_distillation` (2026-09-24T00:30:07Z) and ends at the PR #22 merge (2026-10-19T23:47Z). No implementer transcript read or enumerated holdout contents; holdout *paths* did leak (Pattern 2).

**Verification of run #5 proposals:** P3 (playtest batching), P5 (single-`Write` state updates), P6 (`--quit-after`), and P7 (host facts) all landed in PR #21. **H3 — the deterministic destructive-git guard re-escalated since run #3 — was implemented in the same PR.** The guard now matches force-push flags as standalone tokens (fixing the false positive on branch names containing `-f`), parses the command from stdin JSON with `jq` (fixing the quote-truncation bypass), covers `HEAD:main` / `refs/heads/main` refspecs, and gates `reset --hard` / `clean -f` / path `checkout` / `restore` on repo state: clean tree allowed, dirty tree denied with a pointer to `git stash push -u`. Zero destructive git operations executed in this window; none appear as Bash commands in the three implementer transcripts.

**Patterns found:**

1. **Test conventions document an attribute that does not exist (MEDIUM).** `CLAUDE.md:34,76`, `.claude/rules/godot-csharp-conventions.md:44`, and `.claude/agents/implementer.md:55,67,90` all instruct `[RequireGodotRuntime]`. The attribute is absent from gdUnit4.api 4.3.1 — an assembly scan exposes `TestSuite`, `TestCase`, `BeforeTest`, `AfterTest`, `IgnoreUntil`, `Fuzzer`, `Stage`, and zero `*GodotRuntime*` symbols — and from every test in `Tests/`, which use `[TestSuite]` + `[TestCase]` + `AssertThat`. The review-fix implementer spent ~12 tool calls (2026-10-19T23:08–23:15Z) reading the csproj, scanning DLL strings, and building a reflection probe to decide whether the attribute existed, then added no runtime test. The implementer prompt's Test Patterns block is independently wrong: it uses `[Test]` instead of `[TestCase]` and an unverified `SceneRunner.Load` example. Proposal **P8**.

2. **The holdout directory is enumerable through a recursive `.claude` listing (MEDIUM).** The implementer's first command, `find src Tests .claude -type f | sort` (2026-10-19, ~17:44Z), printed `.claude/holdouts/armored-laser/scenarios.md`, `first-level/…`, `level-4-multi-path/…`, and others. The PreToolUse guard matches only the literal `.claude/holdouts/` string in tool input, so enumeration of the parent directory passed. The implementer then reasoned about the folder names and correctly refused to read them; no `H<n>_*` scenario name appears in any of the three implementer transcripts (0/3). Content discipline held, but holdout topics leaked through file names. Proposal **P9** (prompt wording, medium) and **H5** (hook enumeration guard, high).

3. **The deterministic `rm -rf` guard still costs retries (LOW).** Three commands were denied in this window — main at 2026-09-24T03:23:31Z, implementer at 2026-10-19T22:51:10Z, review-fix implementer at 2026-10-19T23:11:37Z — all attempts to reset `/tmp` scratch directories. Each recovered by switching to a unique timestamped directory, but only after the denial. Proposal **P10**.

4. **zsh unmatched globs abort the whole command (LOW).** Three occurrences: `rm -f … GdUnitRunner_*.cfg` (main, 2026-10-19T23:30:24Z) aborted before deleting the intended `.swp`; `/opt/homebrew/Cellar/godot*` (review-fix implementer, 23:09:05Z); `out*.png` and `/tmp/gdtest/*.png` (research agent, 2026-09-24T03:25:34Z). `no matches found` is emitted before execution, so chaining cleanup with a possibly-empty glob silently skips the cleanup. Proposal **P10**.

5. **Distiller flag skipped at the first merge after it was set (INFO, recurrence).** Flag set 2026-09-24T02:12:12Z; PR #21 merged 2026-09-24T02:34:25Z with no distillation; the flag then sat 25 days until PR #22 merged 2026-10-19T23:47Z, where Merge Cleanup step 6 was followed (the `evolve-harness` skill started at 23:47Z). Same class as run #5 Pattern 3: the gate works when invoked and remains advisory. No new proposal.

6. **Review pipeline healthy (INFO, positive).** Three Bash-free lenses produced 13 findings (0 CRITICAL / 9 WARNING / 4 INFO) on PR #22. Security caught three save-safety holes in the verification hooks (release-reachable isolation swap, SP grant without active verification state, unconfirmed isolation); correctness caught three tautological assertions — the exact defect class the verifier exists to prevent — plus two silent coverage losses (missing ResultScreen/HUD families, wrong-class nodes causing a hang instead of exit 2); performance caught the unbounded Phase 3.5 invocation. All findings were fixed in one implementer batch, then a `/simplify` pass reduced `layout_verifier.gd` 1719→1683 lines with byte-identical output. Build 0 warnings/0 errors; 203 tests pass; verifier 139 evaluated / 137 assertions / 2 skipped / 0 findings. The security lens also confirmed the new layout-verifier agent's hook uses the v2 deny shape and includes `Bash` in its matcher.

7. **Pre-merge bookkeeping held (INFO, positive).** `current-feature.md` and the feature's spec/tool docs were committed on `feature/layout-verifier` before merge (PR #22), per the human's 2026-09-23 rule. No stray post-merge sync PR.

8. **Artifact tool unavailable in this session (INFO).** The orchestrator attempted one Artifact publish for a design diagram (2026-10-19T22:20:03Z) and was refused: the session authenticates with `ANTHROPIC_AUTH_TOKEN`, which takes precedence over a claude.ai login. One round trip; the tool reports that retries are futile. Not a harness defect; no proposal.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P8 (MEDIUM) — replace the non-existent `[RequireGodotRuntime]` convention.**
  - `.claude/rules/godot-csharp-conventions.md:44` → `- Tests are `[TestSuite]` classes with `[TestCase]` methods using `GdUnit4` + `AssertThat` (see `Tests/Towers/TowerTest.cs`). There is no `[RequireGodotRuntime]` attribute in gdUnit4.api 4.3.1 — do not use it. Scene tests use GdUnit4's `SceneRunner` / `ISceneRunner`.`
  - `.claude/agents/implementer.md:55` → drop `[RequireGodotRuntime]`; point at `[TestSuite]` + `[TestCase]`.
  - `.claude/agents/implementer.md:67` → replace with the same "does not exist" statement and a pointer to `Tests/Towers/TowerTest.cs`.
  - `.claude/agents/implementer.md:80–98` (Test Patterns) → replace `[Test]` with a `[TestSuite]` class containing `[TestCase]`; delete the `[RequireGodotRuntime]` + `SceneRunner.Load` example or update it to the verified GdUnit4 API.
  - `CLAUDE.md:34,76` — **outside distiller write scope; human applies:** remove `[RequireGodotRuntime]`, state `[TestSuite]` + `[TestCase]`.

- **P9 (MEDIUM) — prohibit holdout enumeration.** `.claude/agents/implementer.md:61` → `- **NEVER** read **or enumerate** `.claude/holdouts/`. Do not run recursive listings or searches over `.claude/` (`find .claude`, `ls -R .claude`, `grep -r … .claude`); the hook matches the literal path only. Scope searches to `src/`, `Tests/`, and named harness files.` Also propose the same wording for CLAUDE.md hard rule 4 (human applies).

- **P10 (LOW–MEDIUM) — add host shell facts.** In `.claude/rules/verification-gates.md` → `## Host: macOS (darwin)`, after the two existing bullets:
  - `- **`rm -rf` is denied by the PreToolUse hook**, including under `/tmp`. Create scratch dirs with `mktemp -d` or a unique `$(date +%s)` suffix instead of deleting and recreating.`
  - `- **zsh aborts the entire command on an unmatched glob** (`no matches found`) before anything runs, so a preceding `rm -f` in the same command never executes. Quote patterns or use `find`; do not chain cleanup with a possibly-empty glob.`

- **H5 (HIGH — blocked, human must implement) — guard recursive `.claude` enumeration.** Extend the implementer's PreToolUse Bash pattern to deny recursive listings/searches rooted at `.claude/` (`find … .claude`, `ls -R .claude`, `tree .claude`, `grep -r … .claude`) while still allowing explicit reads of named harness files. Narrow the pattern so `cat .claude/settings.json` and `grep src .claude/specs/foo.md` remain allowed. Prompt-only mitigation is P9; this is the deterministic backstop.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history rows + this entry), `harness-feedback.md` (run #6 observations, H3 marked closed, stale current-feature item moved to Resolved, new proposals queued). No rule, skill, agent-prompt, command, template, or game file was modified; P8–P10 and H5 await human approval per the tier table in `harness-safety.md`.

### 2026-10-25 — Seventh Distillation Run

**Evidence base:** native JSONL. Main session `99297c2c-…` (Oct 25 20:22–22:29 CDT; 102 `Bash`, 29 `Edit`, 12 `Read`, 9 `Agent` spawns, 5 skills) covering PR #24 (skill-tree tab-switch fix via `/fix-bug`) and PR #25 (Level 5 + Progenitor via `/spec` → implementer → 3-lens review → fix batch → re-verify → 4-lens `/simplify` → 3 playtest batches → merge), plus its 11 subagent transcripts (8 reviewer lenses, 2 implementers, 1 forked `/fix-bug` investigator). Also the tail of `93e5a1e3-…` (PR #23 merge plus an unrelated Open Design desktop-app investigation, ~100 Bash/MCP calls) and `ad099386-…` (continuation of that investigation). No implementer transcript enumerated `.claude` or touched a holdout path; holdout content was not read.

**Verification of run #6 proposals:** P8, P9, and P10 remain unapplied — the same references are present at the same anchors, and H5 remains open. P8 and P10 both recurred this window (Patterns 1 and 3).

**Patterns found:**

1. **P8 recurrence — the non-existent `[RequireGodotRuntime]` convention still costs every agent search time (MEDIUM).** The repo has 24 `[TestSuite]` classes and 261 `[TestCase]` methods and **zero** `RequireGodotRuntime` uses, but the attribute is still prescribed at `.claude/agents/implementer.md:55,67,90`, `.claude/rules/godot-csharp-conventions.md:44`, and `CLAUDE.md:34,76`. This window the forked `/fix-bug` agent grepped it 4×, both implementers 2× each, and the orchestrator checked once before concluding it does not exist. `implementer.md:80–98` still shows `[Test]` + `SceneRunner.Load` instead of the repo's `[TestCase]` pattern. P8 text is unchanged; re-queued.

2. **Reviewers are told to run `git diff` but have no shell (MEDIUM, new P11).** All three lens prompts began “Run `git diff`”; `.claude/agents/reviewer.md` grants only Read/Glob/Grep. Every reviewer report closed with a disclaimer that `git diff`, `dotnet build`, and `dotnet test` were unavailable, so review happened by direct file inspection. `.claude/agents/orchestrator.md:61` says “Pass the diff or branch” — a branch is unactionable without a shell. Proposal P11.

3. **P10 recurrence plus one new host guard (LOW–MEDIUM).** (a) The Level 5 implementer hit `PreToolUse:Bash hook error: rm -rf is blocked` during `/tmp` cleanup. (b) The main session hit `(eval):1: no matches found: GdUnitRunner_*.cfg` when chaining a cleanup glob (the whole command aborts before the intended removal). (c) New: the Bash tool blocked a foreground `sleep 30` with guidance to use Monitor/until-loop or `run_in_background`; one round trip. P10 is re-queued with a third bullet for the sleep guard.

4. **Playtest-tuned visuals were under-specified at spec time (MEDIUM P12 + LOW memory applied).** Progenitor diameter 96 px (1.5 × 64 px cell) → 72 px (1.125 cells) after playtest 1 → 64 px (1 cell) after playtest 3. Cadence 6.0/4.5/3.0 s put the first summon at 6.5 s, which the human read as “never summoned”; it was halved to 3.0/2.25/1.5 s. Two of the three playtest passes were spent on values that could have been chosen relative to the cell and a first-telegraph budget. Applied a grid-relative sizing + telegraph-timing bullet to `game-design.md`; propose matching spec-template wording as P12.

5. **`GdUnitRunner_<uuid>.cfg` leaks into the repo root (LOW, outside distiller write scope).** `dotnet test` left an untracked cfg at the repo root; it appeared in two git-status snapshots, and the later cleanup glob failed because the file had already gone. Add `GdUnitRunner_*.cfg` to `.gitignore`. Human applies (distiller cannot write repo-root files).

6. **Flag honored at merge (INFO, positive).** Flag set 2026-10-25T21:55:42 (counter 62); PR #25 merged 22:28; Merge Cleanup step 6 ran `/evolve-harness` immediately. Step 6 worked when invoked — contrast run #6's 25-day advisory gap.

7. **P3 batching followed (INFO, positive).** Three playtest passes produced exactly three implementer batches, one per pass; no per-issue spawns.

8. **P5 single-`Write` state updates followed (INFO, positive).** `.claude/state.json` was written four times with `Write` and never edited; no stale-content `Edit` failure.

9. **Review and simplify pipeline healthy (INFO, positive).** Three shell-free lenses produced 1 CRITICAL (found independently by both security and correctness — the post-base summon leak), 4 WARNING, and 2 INFO; all were fixed in one implementer batch, and the correctness lens re-verified every item with per-finding evidence and reported CLEAN. `/simplify` ran four quality lenses, applied 9 cleanups, and declined 14 with reasons. Final gates: build 0 warnings / 0 errors, 261 tests pass / 0 skipped, layout verifier PASS, holdouts 13/13.

10. **Holdout discipline held (INFO, positive).** 0/2 implementer transcripts enumerated `.claude` or touched a holdout path; the P9 prompt language was not needed this window.

11. **Merge hygiene held (INFO, positive).** Bookkeeping was committed on the feature branch pre-merge; `main` fast-forwarded; no destructive git operations; no stray post-merge PR. The only stale item was `current-feature.md`'s “awaiting playtest”, corrected by this run.

12. **Unrelated session tripped the activity flag (INFO).** The Oct 25 afternoon session used this project directory to debug the unrelated Open Design desktop app (~100 Bash/MCP calls, no game edits), driving the tool counter to 62. The flag is activity-based, not project-work-based; scoping it to `src/`/`Tests/` edits would require a stop-hook change (HIGH, human-only). The `open-design` MCP server also failed to connect during this run. No action.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P8 (MEDIUM — re-queued from run #6, unchanged).** Remove `[RequireGodotRuntime]`:
  - `.claude/rules/godot-csharp-conventions.md:44` → `- Tests are [TestSuite] classes with [TestCase] methods using GdUnit4 + AssertThat (see Tests/Towers/TowerTest.cs). There is no [RequireGodotRuntime] attribute in gdUnit4.api 4.3.1 — do not use it. Scene tests use GdUnit4's SceneRunner / ISceneRunner.`
  - `.claude/agents/implementer.md:55` → drop `[RequireGodotRuntime]`; point at `[TestSuite]` + `[TestCase]`.
  - `.claude/agents/implementer.md:67` → replace with the same “does not exist” statement and a pointer to `Tests/Towers/TowerTest.cs`.
  - `.claude/agents/implementer.md:80–98` (Test Patterns) → replace `[Test]` with a `[TestSuite]` class containing `[TestCase]`; delete or correct the `[RequireGodotRuntime]` + `SceneRunner.Load` example.
  - `CLAUDE.md:34,76` — **outside distiller write scope; human applies:** remove `[RequireGodotRuntime]`, state `[TestSuite]` + `[TestCase]`.

- **P9 (MEDIUM — unchanged from run #6, no new evidence).** Prohibit holdout enumeration in `.claude/agents/implementer.md:61`: “NEVER read **or enumerate** `.claude/holdouts/`; do not run recursive listings or searches over `.claude/` (`find .claude`, `ls -R .claude`, `grep -r … .claude`). Scope searches to `src/`, `Tests/`, and named harness files.” Mirror in CLAUDE.md hard rule 4 (human applies).

- **P10 (LOW–MEDIUM — re-queued from run #6 with new evidence).** In `.claude/rules/verification-gates.md` → `## Host: macOS (darwin)`, after the two existing bullets add:
  - `- **`rm -rf` is denied by the PreToolUse hook**, including under `/tmp`. Create scratch dirs with `mktemp -d` or a unique `$(date +%s)` suffix instead of deleting and recreating.`
  - `- **zsh aborts the entire command on an unmatched glob** (`no matches found`) before anything runs, so a preceding `rm -f` in the same command never executes. Quote patterns or use `find`; do not chain cleanup with a possibly-empty glob.`
  - `- **Foreground `sleep` is blocked** when chained with another command. To wait for a condition, use Monitor with an until-loop (`until <check>; do sleep 2; done`) or `run_in_background: true`.`

- **P11 (MEDIUM — new).** Make the reviewer handoff match the reviewer's tool set:
  - `.claude/agents/orchestrator.md:61` → `- **Reviewer:** Reviewers have Read/Glob/Grep only — no shell. Pass the diff text (output of `git diff --stat` plus `git diff`, or the changed-file contents) in each spawn prompt alongside a focused lens. Never tell a reviewer to run git, build, or tests.`
  - `.claude/skills/review-changes/SKILL.md` step 2 → `Include the diff text (or a `git diff --stat` plus each changed file) in every reviewer prompt. Reviewers are shell-free and cannot run `git diff` themselves.`
  - `.claude/agents/reviewer.md` Review Process step 1 → `Read the diff or changed files provided by the orchestrator. You have no shell: do not attempt git diff, build, or test commands.`
  - `.claude/memory/software-factory-patterns.md` handoff section → add `### Orchestrator → Reviewer` with `- Supply the diff text or an explicit changed-file list; reviewers have no shell.`

- **P12 (MEDIUM — new).** In `.claude/templates/spec-template.md` under `## Visual / Geometry Theme`, before `LAYOUT`, add:
  - `- **Scale:** State rendered sizes for new entities in grid-cell units (cell = 64 px, GameConstants.CellSize) and state when the first signature behavior becomes visible (for example "first summon by 3.5 s").`
  - Evidence: Progenitor 96 → 72 → 64 px over two playtest passes; cadence 6.0 → 3.0 s because a first summon at 6.5 s read as “never summoned”.

- **Proposal (LOW — outside distiller write scope).** Add `GdUnitRunner_*.cfg` to `.gitignore`; the test adapter leaves it at the repo root and it shows up as untracked after `dotnet test`.

- **H5 (HIGH — blocked, human must implement; unchanged).** Guard recursive `.claude` enumeration in the implementer PreToolUse Bash pattern while still allowing explicit reads of named harness files.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #7 observations, recurrences, and queued proposals), `current-feature.md` (PR #25 marked merged), `game-design.md` (grid-relative sizing + telegraph timing). No rule, skill, agent-prompt, command, template, or game file was modified; P8–P12, the `.gitignore` proposal, and H5 await human approval per the tier table in `harness-safety.md`.

**Post-run approval (2026-10-26):** Human approved **P8**, **P11** (diff-text handoff variant), and **P12**. Applied on this branch (PR #26): removed every `[RequireGodotRuntime]` reference from `CLAUDE.md`, `rules/godot-csharp-conventions.md`, and `agents/implementer.md` and replaced the invalid `[Test]` / `SceneRunner.Load` sample with the verified `[TestSuite]` + `[TestCase]` pattern; orchestrator/skill/reviewer prompts now require the orchestrator to generate and pass diff text because reviewers have no shell; spec template now asks for entity scale in grid-cell units and first-telegraph timing. P9, P10, H5, and the `GdUnitRunner_*.cfg` `.gitignore` proposal were **declined by the human** and remain unapplied.

### 2026-10-28 — Eighth Distillation Run

**Evidence base:** native JSONL. Main session `2d66f169-…` (2026-10-28 02:15:49–05:11:37Z; 462 `Bash`, 85 `ListAgents`, 73 `Edit`, 57 `Read`, 17 `Write`, 14 `SendMessage`, 11 `TaskStop`, 11 `Agent`) covering PR #29 (tower-upgrades: spec → implementer → 3-lens review + fallbacks → 4-lens simplify → holdout probes → five playtest batches → merge), plus its 13 subagent transcripts (1 forked `/implement-feature`, 2 implementers, 3 lens reviewers plus 1 correctness retry and 2 correctness fallbacks — caveman builder and general-purpose audit — and 4 simplify lenses). The previous session `99297c2c-…` was used only to check API-422 recurrence. No implementer transcript read holdout content; the frontmatter deny held.

**Patterns found:**

1. **Holdout IDs leaked to the implementer through the orchestrator's own `SendMessage` prompts (MEDIUM, new P13).** Four fix batches named holdouts: H12 at 03:42:45, H13/H17 at 04:00:44, H18 at 04:15:08, and H4/H10/H13/H14 at 05:04:20. The implementer was spawned with "Do NOT read or edit anything under `.claude/holdouts`" and its Read deny held; its handback explicitly said it did not open them and worked from the updated spec. But the prompts disclosed the verification set's IDs, violating the software-factory handoff rule ("NEVER mention holdouts"). `software-factory-patterns.md:61` already says this; the orchestrator ignored it four times. Proposal P13.

2. **The correctness lens never produced an independent report; the orchestrator self-audited (MEDIUM, new P14 + P15).** First correctness reviewer: 38 `Read` + 23 `Grep` + 4 `Glob` over 9m26s, killed with no findings. Retry: 21 `Read` + 3 `Grep` over 2m03s, killed. Caveman fallback: 25 `Read` + 14 `Bash` over 3m26s, killed. General-purpose audit: 16 `Read` + 15 `Bash` over 1m42s, killed. The two successful lenses ran 4m36s–5m44s with 15–20 source reads each. Orchestrator note at 03:34:12: "Correctness reviewer agents stalled on reading (harness issue), so I completed the correctness audit manually." Two simplify lenses were also killed after <2 min (`a804b2`, `ab7a5e`). The kill timings were shorter than the successful lens runtimes, so impatience compounded the convergence problem. Proposals P14 (reviewer budget + diff splitting) and P15 (orchestrator wait/kill policy).

3. **The orchestrator busy-waits and micromanages background agents (MEDIUM, new P15).** In one 2h56m feature run: 116 `sleep` commands summing to 7,170 s (119.5 min nominal), 101 no-op `true` commands, 85 `ListAgents` calls, 5 "Start editing now — skip further reading" nudges, and 11 `TaskStop` calls. Task notifications already work — implementer and reviewer completion notifications arrived and were used. The sleep/ListAgents loops burn context and produce the premature kills above. Proposal P15.

4. **API 422 `tool_definition` kills forked skills (OPEN infra, new P16).** Four distinct failures across two sessions: `/fix-bug` at 2026-10-26T01:28:50Z (4m), `/evolve-harness` at 03:39:58Z (10m54s), `/fix-bug` at 04:01:23Z (16m), and `/implement-feature` at 2026-10-28T03:15:57Z (30m46s). All were `context: fork` general-purpose skills on the `deepseek-flash` API with request bodies of 292–456 KB (`unknown variant tool_definition, expected tool_reference`). The task notification says the same task-id can be resumed with another message. In PR #29 the orchestrator instead adopted the still-live implementer child (`aceb750…`) via `SendMessage`, which worked. Proposal P16 documents the failure and recovery.

5. **Five playtest rounds on UI state and visuals (MEDIUM, new P17).** Human fixes at 03:42 (seed mechanics inert until rank 3), 03:56 + 03:59 (vertical chevrons, badge trigger), 04:14 (archetype body silhouettes), 04:55 (remove duplicated rank from panel), and 05:02 (single-button panel, hidden when unavailable). The orchestrator edited the spec 22 times and holdouts 15 times, then sent five batches to the same implementer. The final spec now pins exact geometry, visibility triggers, and state rules that the initial spec did not. Most rounds were state/visibility questions answerable in a state table. Proposal P17.

6. **NUnit-style assertions slipped into a new test file (INFO).** The implementer initially wrote `Assert.That(...)` in `Tests/UI/TowerUpgradePanelTest.cs`, then converted eight occurrences to GdUnit4 `AssertThat` with a Python one-liner after the test run failed. Final repo has zero `Assert.That` and zero NUnit usings. The implementer prompt already shows `AssertThat`; no new proposal, but an explicit "not NUnit" line would save one build/test cycle if it recurs.

7. **Gates and discipline held (INFO, positive).** Build 0 warnings / 0 errors; 384 tests pass; layout verifier PASS; holdouts 16/16. Merge was a squash to `main` (`5db78bb`), verified after fast-forward (build clean, 384 tests), local and remote feature branches deleted, `state.json` removed. `state.json` was written 12× with `Write` and never `Edit` (P5 held). The five playtest rounds used one implementer via `SendMessage`, no per-issue respawns (P3 held). Holdouts were moved to `regression/` and `current-feature.md` updated on the feature branch pre-merge. No destructive git operations, no push to `main`. Merge Cleanup step 6 checked the distiller flag and invoked this run at 05:11:31Z.

8. **Scene and MCP discipline (INFO, positive).** No `.tscn` hand-edits and no Godot MCP calls; the feature was implemented entirely as procedural C# UI/geometry, so no scene manipulation was needed.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P13 (MEDIUM — new). Never name holdout files, IDs, or scenario names to the implementer.**
  - `.claude/agents/orchestrator.md` Delegation Rules → Implementer: "Pass the FULL spec. NEVER mention holdout scenarios, files, IDs (`H<n>`), or paths. The implementer is denied read access, but naming them still discloses the verification set. For a playtest fix, update the spec and pass the affected spec sections only."
  - `.claude/skills/implement-feature/SKILL.md` Phase 8: replace any "holdout `H<n>` is updated" instruction with "the spec's acceptance criteria are updated; read the affected spec section". Keep holdout IDs in orchestrator-only space.
  - `.claude/memory/software-factory-patterns.md` Orchestrator → Implementer: extend "NEVER mention holdouts or holdout file paths" with "IDs, or scenario names".
  - Evidence: four `SendMessage` batches listed above; implementer handback at 05:07:16Z.

- **P14 (MEDIUM — new). Give reviewers a budget and split large diffs.**
  - `.claude/agents/reviewer.md` → add under Rules: "The supplied diff is the primary review target. Read a source file only to confirm a suspicion raised by a diff hunk; target at most 10 source-file reads and 15 tool calls total. Return partial findings rather than no report. Never reread a file. Use Glob/Grep to locate a path; never guess (`src/WaveManager.cs` does not exist; use `src/Levels/WaveManager.cs`). On a diff over ~1,500 lines, complete one pass and report instead of tracing every caller."
  - `.claude/skills/review-changes/SKILL.md` step 2: for diffs over ~1,500 lines, split by subsystem (for example towers/combat vs UI/state) and spawn one reviewer per slice with an explicit file list and the budget above.
  - `.claude/skills/implement-feature/SKILL.md` Phase 4: same split; "a lens is satisfied only by an independent reviewer report. If two attempts stall, split the remaining diff or use a general-purpose reviewer with Bash, and record the fallback in the PR checklist."
  - `.claude/agents/orchestrator.md` Review synthesis: same fallback sentence; do not silently substitute a self-audit for a lens.
  - Evidence: reviewer tool counts and durations above; successful lenses at 15–20 source reads / 4m36s–5m44s.

- **P15 (MEDIUM — new). Wait for task notifications instead of polling or sleeping.**
  - `.claude/agents/orchestrator.md` → new "Waiting for subagents" section: after spawning background agents, end the turn; a task notification arrives when an agent stops. Do not run `sleep` / `true` / `ListAgents` loops. Inspect the output file once after a notification. Give lens reviewers at least ~6 minutes (successful lenses took 4m36s–5m44s); only stop an agent after 3+ minutes with no new tool activity, and send one `SendMessage` nudge before stopping. Never stop a reviewer before one full review pass.
  - `.claude/skills/implement-feature/SKILL.md` Phase 4: same minimum wait before declaring a lens failed.
  - Evidence: 116 sleeps / 7,170 s, 101 `true`, 85 `ListAgents`, 5 nudges, 11 `TaskStop`; successful vs killed durations above.

- **P16 (MEDIUM — new). Document forked-skill API-422 failure and recovery.**
  - `.claude/skills/implement-feature/SKILL.md` (and optionally `fix-bug` / `evolve-harness`): add "Forked-skill failure recovery": if the skill agent stops with `API Error: 422 … unknown variant tool_definition`, resume the same task-id with `SendMessage` (the task notification says this is allowed) or adopt its still-running child agents by name; do not re-spawn the whole loop from scratch. Keep fork context lean (batch reads; avoid dumping whole diffs into a long-lived fork).
  - Evidence: four failures / two sessions, request bodies 292–456 KB; PR #29 recovered by adopting `aceb750…`.

- **P17 (MEDIUM — new). Add a UI state matrix to the spec template.**
  - `.claude/templates/spec-template.md` → new `## UI States` section (or a table under Visual / Geometry Theme): list each state (for example rank 0 below gate, rank 0 at gate, ranks 1/2, rank 3, each archetype) against the visible named groups/controls, the trigger for each transition, and the single source of truth for any duplicated fact (for example "rank is shown only by the tower badge"). Require explicit "drawn geometry, not text glyph" wording for procedural icons.
  - Evidence: five playtest rounds, 22 spec edits, 15 holdout edits; final spec's precise visibility/geometry rules.

- **H6 (HIGH — blocked, human must implement). Deterministic guard against holdout IDs in orchestrator→implementer prompts.** Extend the PreToolUse hook to inspect `Agent` / `SendMessage` inputs addressed to the implementer and deny holdout paths or `H<n>_` tokens. False-positive risk: legitimate "do NOT read `.claude/holdouts/`" prohibitions must remain allowed. Logged as an option; not implemented.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #8 observations, open issues, queued proposals), `current-feature.md` (tower-upgrades marked merged, added to Recent Features). No rule, skill, agent prompt, command, template, or game file was modified; P13–P17 and H6 await human approval per the tier table in `harness-safety.md`.

### 2026-11-01 — Ninth Distillation Run

**Evidence base:** native JSONL. Main session `45b9d3bc-…` (2026-10-30T20:55:51Z → 2026-11-01T22:19:06Z; 140 `Bash`, 29 `Read`, 23 `Write`, 20 `Edit`, 11 `SendMessage`, 8 `Agent`, 3 `Skill`, 0 `ListAgents`, 0 `TaskStop`, one `sleep 1`) covering PR #31 (visual-overhaul: spec → implementer → 3-lens review → 4-lens simplify → nine playtest fix batches → merge), plus its 8 subagent transcripts: 1 implementer adopted across the whole feature (247 `Edit`, 138 `Bash`, 57 `Read`, 30 `Write`, 12 handbacks), 3 lens reviewers, and 4 simplify lenses. All reviewers and simplify lenses finished. The implementer made 2 errors: one failed `Edit` and one denied `find` whose exclusion glob contained `.claude/holdouts`. No holdout content was read.

**Patterns found:**

1. **Large-diff reviewer handoff via temp file worked; the queued P14/P15 limits would have suppressed the CRITICAL (MEDIUM, revisions P19).** The orchestrator wrote `/tmp/visual-overhaul.diff` (9,315 lines) and `/tmp/visual-overhaul-simplify.diff` (9,707 lines), passed the paths, and told reviewers to read every line in chunks. All 3 lenses completed: security 45 `Read` + 30 `Grep` + 2 `Glob` / 8m59s, performance 30+18+1 / 6m38s, correctness 64+36+2 / 24m42s. Security and correctness independently found the same CRITICAL (`Enemy.RedrawVisual` repainted only the overlay container). P14's proposed ≤10 source reads / ≤15 tool calls and P15's ~6-minute kill floor would have stopped both before the finding. This run also followed the "wait, don't poll" half: 0 `ListAgents`, 0 `TaskStop`, one trivial sleep. Keep P19's temp-file handoff and notification-only waiting; drop the hard caps and wall-clock kills.

2. **Simplify lens volume was high but healthy (INFO).** Four lenses on the 9,707-line post-fix diff produced 57 raw findings (reuse 10, complexity 28, efficiency 8, altitude 11) in 4m00s–5m41s. The orchestrator applied 24 in one batch, skipped 12 with reasons, and subsumed 21 duplicates. The batch explicitly protected review-mandated fixes (redraw layering, overlay z-order, trail versioning, burst cap, process gating, hover flags) and re-ran build/tests/verifier. No stalls, no regressions. Keep 4 lenses for large diffs; the useful control is triage (apply/skip with reasons), not fewer lenses.

3. **Playtest micro-fixes exposed spec drift (MEDIUM, P18).** Nine batches over ~46 h: skill-tree icons 22→40→46 px; per-draw antialiasing then global 2D MSAA 4×; internal parenthetical values removed from 15 descriptions; top-bar restyle; `$` removed from card costs; base-core scale pulse → alpha pulse → counter-rotating shield arcs. Zero spec edits and zero holdout edits after the spec-time draft. The spec still pins 22×22 icons, `15$`, and the top-bar redesign as Out of Scope; the 23/23 holdout result predates the post-playtest commits. Final gates were 507 tests and 335 layout assertions, while the PR body kept the initial 499 / 214 / 43-file figures. The agent did not thrash — the human's visual taste converged — but accepted deltas must be folded back into the contract and the regression set.

4. **Post-merge status staleness recurred (MEDIUM, P20).** `current-feature.md` gained an `Active:` line at 21:24 on Oct 30 and a `Status: awaiting playtest — PR open` line at 22:53; after the Nov 1 merge it still said "PR open", and Recent Features said "PR pending". This is the class run #5 eliminated by deleting the `Status:` line; the file's own note says not to track phase, but the orchestrator did. Run #9 corrected the file (memory) and queued a skill-level instruction. Also missing: remote branch deletion (`origin/feature/visual-overhaul`, `origin/feature/layout-verifier` remain) and post-merge build/test on `main`.

5. **Holdout guard false positive on an exclusion glob (HIGH, H7).** The implementer ran `find /Users/… -type f … -not -path "*/.claude/holdouts/*"`; the PreToolUse guard matched the literal string and denied the whole command, costing one round trip. The guard is working as designed but cannot distinguish an exclusion from a read. H7 proposes an exclusion-aware match; P9's prompt-level scoping remains the declined mitigation.

6. **P13/P16/P15 behavior improved without new rules (INFO, positive).** 0 holdout IDs or scenario names in the implementer prompt or nine fix messages (run #8 had four leaked batches); no API-422; no polling or TaskStop; one implementer adopted for the whole feature; `state.json` written 21× with `Write`, 0 `Edit`; no destructive git ops, no `.tscn` hand-edits, no Godot MCP calls. P13 stays queued for prompt hygiene; P16 is documentation-only.

7. **Scope expansion was human-driven (INFO).** After the initial spec draft the human added title/result screens and skill-tree visuals, then later the top bar. Normal iteration, but `Out of Scope` was not updated when the top bar shipped (P18) — the spec-to-playtest loop has no step for it.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P18 (MEDIUM — new). Fold playtest deltas back into the spec and holdouts.** `implement-feature/SKILL.md` Phase 8: record accepted values/copy/scope changes in a "Playtest adjustments" subsection; update `Out of Scope`; re-run affected holdouts after the final batch and put the result in the PR. `templates/spec-template.md`: add `## Player-Facing Copy` (exact strings; no internal formula values in parentheses; currency format) and `## Visual Defaults` (AA/MSAA policy; no per-frame movement/scale of static geometry; UI icon sizes relative to row/card). Evidence: nine batches, zero spec/holdout edits, spec-code contradictions above. Subsumes the copy/format/motion half of P17.
- **P19 (MEDIUM — new; supersedes P14's cap and P15's timeout).** Reviewer handoff: paste small diffs, pass a temp-file path for large diffs and require full coverage; no read cap. `reviewer.md`: full diff first, sources only to confirm, no rereads, Glob/Grep for paths, partial findings better than none. `orchestrator.md`: wait for notifications, no polling; never TaskStop a lens that is producing activity; after 15+ minutes idle send one nudge, stop only if still idle; never stop before one full pass. Evidence: correctness 24m42s / 102 calls found the CRITICAL; security 8m59s / 77 calls found it independently.
- **P20 (MEDIUM — new). Merge hygiene.** Phase 2: never add a `Status:`/phase line to `current-feature.md`; state.json owns phase. Merge Cleanup: replace "PR open"/"PR pending" with merge date, PR number, and squash SHA; delete the remote feature branch; optionally run `dotnet build && dotnet test` on `main` once. Evidence: run #9 corrected both stale lines, two remote branches remain, no post-merge re-verification after PR #31.
- **H7 (HIGH — human only). Exclusion-aware holdout Bash guard.** Allow `.claude/holdouts` only when it appears in `-not -path`, `! -path`, or `--exclude`/`--exclude-dir`; deny actual reads/listings. Evidence: one denied `find` on 2026-10-30. H6 remains the prompt-leak guard; P9 remains the declined prompt scoping.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #9 observations, open issues, P13–P20/H7 annotations), `current-feature.md` (visual-overhaul marked merged, status line removed, spec-drift/remote-branch/post-merge follow-ups added), `software-factory-patterns.md` (implementer/reviewer handoff guidance + playtest deltas), `game-design.md` (player-facing copy, anti-aliasing, motion-without-jitter), `playtest-feedback.md` (visual-overhaul playtest record). No rule, skill, agent prompt, command, template, or game file was modified; P18–P20 and H7 await human approval per the tier table in `harness-safety.md`.

**Post-run human decisions (fill in on approval):** P13–P20/H7 pending. Note that P14 and P15 should not be applied as originally written; P19 replaces their unsafe parts.

### 2026-11-03 — Tenth Distillation Run

**Evidence base:** native JSONL. Main session `d890fb16-…` (2026-11-03T02:06:47Z → 03:21:43Z; 59 `Bash`, 19 `Edit`, 17 `Read`, 14 `Write`, 8 `Agent`, 4 `SendMessage`, 3 `Skill`, 1 `ToolSearch`, 1 `AskUserQuestion`) covering PR #37 (tower-hotkeys: spec → implementer → 3-lens review → 4-lens simplify → one playtest fix batch → merge), plus its 8 subagent transcripts: 1 implementer adopted across four messages (79 `Bash`, 58 `Edit`, 35 `Read`, 1 `Write`) and 7 reviewer/simplify lenses. All lenses completed. No polling (`ListAgents`/`TaskStop`/`sleep` = 0), no holdout IDs or paths in any Agent/SendMessage prompt, and the implementer made zero tool calls touching `.claude/`.

**Verification of run #9 proposals:** P19, P13, and P18 were followed: reviewers received the diff inline (small feature; temp file for the simplify diff), all lenses finished without time-kills, no holdout IDs leaked, and the playtest fix updated the spec and holdouts on the feature branch before merge. P20 recurred (Pattern 2). P21/P22 below are new. P10 recurred twice but remains declined by the human.

**Patterns found:**

1. **Holdout verification materialized test code in the implementer-accessible tree (MEDIUM, new P21 + H8).** The orchestrator wrote `Tests/HoldoutTowerHotkeysTest.cs` (15,538 B) at 02:56:40 and `Tests/HoldoutTowerHotkeysFinalTest.cs` (12,838 B, then 14,773 B) at 03:04:44 and 03:17:10, ran `dotnet test --filter`, and deleted each file 41–63 s later. The implementer agent's last activity was 03:16:10 and its final hand-back 03:16:50; the first two files were written while it was live and idle between messages, the third 20 s after its final hand-back (its `SubagentStop` hook runs `dotnet test`). The implementer's deny hook matches only `.claude/holdouts/` — not `Tests/Holdout*` — and it made zero holdout-path tool calls, so no content leaked. A `git status`, directory listing, or concurrent `dotnet test` could have exposed the file. Proposal P21 (skill/agent prompt) and H8 (deterministic deny).

2. **P20 recurrence — post-merge memory and branch cleanup (MEDIUM).** `current-feature.md` on `main` still read "Active feature (awaiting playtest)", "PR #37 open", and "Tower hotkeys (PR open, 2026-11-03)" after the squash. The orchestrator used `gh pr merge 37 --squash --delete-branch`; the remote branch was deleted, but the skill's step 3 `git branch -d feature/tower-hotkeys` then failed (`error: branch 'feature/tower-hotkeys' not found`) and aborted the `&&` chain, leaving `rm -f .claude/state.json` to a later command. `git branch -a` retains a stale `origin/feature/tower-hotkeys` tracking ref (`git remote prune origin --dry-run` would remove it); older remote branches `feature/layout-verifier` and `chore/distillation-6` remain. Post-merge `dotnet build && dotnet test` did run on `main` (positive). P20 revised with the exact cleanup sequence.

3. **Spec status drift (MEDIUM, new P22).** `.claude/specs/tower-hotkeys.md:3` says `**Status:** draft` after the feature shipped as PR #37; the implementer prompt quoted `**Status:** approved` while the committed file said `draft`, and all acceptance checkboxes remain unchecked. Five other merged specs also say `draft`. The template offers `draft | approved | implementing | done` but no phase updates it. Prefer deleting the mutable status line (P4 principle) or setting it once at approval and leaving it.

4. **Headless cursor assertion failed once (LOW, memory applied).** The first `dotnet test` failed: `HotkeySelection_MatchesTheSidebarCardTransition` expected cursor `Cross` but got `Arrow`. The headless display server does not apply cursor-shape changes; the implementer dropped the cursor assertions and verified by reusing the mouse path. Recorded in `memory/godot-mcp.md` as a headless limitation. One 20 s test cycle.

5. **P10 recurred twice (LOW, declined).** The orchestrator's first `grep --include=*.cs` at 02:07:47 and the implementer's at 02:38:29 both aborted with `(eval):1: no matches found: --include=*.cs`; both recovered within 3 s by quoting the pattern. Human declined P10 on 2026-10-26; no action.

6. **Gates and discipline held (INFO, positive).** Build 0 warnings / 0 errors; final 560 tests; layout verifier PASS (344 assertions / 346 evaluated / 3 skipped / 0 findings); holdouts 11/11 scenarios (13 checks) re-run after the playtest fix; review 0 CRITICAL / 2 WARNING / 2 INFO (both warnings fixed in one batch, both test-gap INFOs addressed); simplify 4 cleanups applied. No `.tscn` hand-edits, no Godot MCP calls, no destructive git ops, no API-422, no polling. One implementer stayed live across four messages. Spec and holdouts were updated during the playtest fix and committed on the feature branch (P18). The distiller flag was set in advance, checked at 03:20:36, and `/evolve-harness` ran 55 s after the merge.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P21 (MEDIUM — new). Never materialize holdout test code in the implementer-accessible workspace.**
  - `.claude/skills/implement-feature/SKILL.md` Phase 5: add "Holdout test artifacts must not be written into `Tests/` while the implementer is live. Use a scratch test project outside the repo, or write → run → delete in one tool call with the implementer stopped. Before the next implementer message, confirm `git status` contains no `Holdout*` file."
  - `.claude/agents/orchestrator.md` Holdout Verification: same rule; note the implementer's deny hook covers only `.claude/holdouts/`.
  - `.claude/memory/software-factory-patterns.md` Orchestrator → Implementer: "Do not materialize holdout tests or scenario text in the implementer-accessible tree; the deny hook covers `.claude/holdouts/` only."
  - Evidence: three test-file writes totalling ~43 KB in `Tests/` while implementer `ad7fe195…` was live; 0 reads by the implementer.
- **P22 (MEDIUM — new). Stop the spec `Status:` line from going stale.**
  - Preferred: `.claude/templates/spec-template.md` — remove `**Status:** draft | approved | implementing | done` (git/PR is the status; P4 precedent), or replace it with a static `**Approved:** {date}`.
  - If kept: `.claude/skills/implement-feature/SKILL.md` Phase 1/2 — after human approval, edit the committed spec to `approved`; Merge Cleanup sets `done` and checks acceptance boxes.
  - Evidence: `tower-hotkeys.md` says `draft` after PR #37; five other merged specs say `draft`; the orchestrator's prompt copy said `approved`.
- **P20 (MEDIUM — revised with run #10 evidence).** Merge Cleanup: (1) `gh pr merge --squash --delete-branch {pr_url}`; (2) `git fetch origin --prune && git checkout main && git pull --ff-only`; (3) confirm the local branch is gone, delete only if present; do not chain optional steps after a command that can fail; (4) delete `state.json`; (5) prune worktrees; (6) distiller check. Phase 7 step 2 must not write "PR open"/"awaiting playtest"; the distillation PR replaces those with merge date, PR number, and squash SHA. Delete remote `feature/layout-verifier` and `chore/distillation-6`. Evidence: `current-feature.md` stale lines, `git branch -d` failure, stale tracking ref, P20's third recurrence.
- **H8 (HIGH — blocked, human must implement).** Extend the implementer PreToolUse deny to a dedicated holdout-test path convention (`Tests/Holdout*` or `Tests/.holdout/**`) so holdout test names/content cannot be listed or read even if written into the shared tree. Use a dedicated prefix to avoid false positives. Evidence: three `Tests/Holdout*` writes during a live implementer session.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #10 observations, P20 recurrence, P21/P22/H8 queued), `current-feature.md` (PR #37 marked merged; stale phase/PR-open lines removed; Recent Features updated), `godot-mcp.md` (headless cursor limitation). No rule, skill, agent prompt, command, template, or game file was modified.

### 2026-11-03 — Eleventh Distillation Run

**Evidence base:** native JSONL. Main session `9b2f85f6-…` (2026-11-03T03:33:21Z → 04:55:20Z; 66 `Bash`, 20 `Edit`, 9 `Write`, 9 `Read`, 9 `Agent`, 3 `Skill`, 2 `SendMessage`, 2 `AskUserQuestion`, 1 `ToolSearch`, 0 `ListAgents`, 0 `TaskStop`) covering PR #39 (global-skill-tree: `/spec` → investigator → implementer → 3-lens review → fix batch → 4-lens simplify → temporary holdout harness → merge), plus 9 subagent transcripts: 1 investigator, 1 implementer (266 tool calls, three batches on one live agent), 3 lens reviewers, and 4 simplify lenses. All 9 subagents finished. The forked `/evolve-harness` agent at 04:55 is this run and is excluded from the evidence. Window starts at run #10's `.last_distillation` (03:27:49Z); PR #38 (run #10 memory) merged at 03:31:54Z but was authored by the prior session, so the substantive window is PR #39.

**Verification of run #10 proposals:** **P13 followed** — 0 holdout IDs, scenario names, or paths in all 11 `Agent`/`SendMessage` prompts (the implementer prompt was 9,492 chars, the two fix batches 1,404 and 3,137). **P19 followed** — the review diff was written to `/tmp/gtd-global-skill-review.diff` (2,916 lines) and the simplify diff to `/tmp/gtd-global-skill-simplify.diff` (3,052 lines); reviewers were told to read every line and not run git/build/tests. All 3 lenses completed (security 36 calls / 22 reads / 3m45s; performance 22 / 18 / 3m16s; correctness 118 / 67 / 14m37s), and all 4 simplify lenses completed (28–43 calls, 2m51s–4m15s). No lens stalled or was killed. **P18 followed** — the holdout suite was re-run after simplify and passed 12/12 (620 tests including the temporary harness). **P5 followed** — `state.json` was written 5× with `Write` and never `Edit`. **P3 followed** — one implementer handled the feature, review fix batch, and simplify batch. **P20 partially followed** — merge used `gh pr merge --squash 39`, then `git fetch origin && git checkout main && git pull --ff-only`; local and remote branches were deleted, `state.json` was removed, post-merge `dotnet build` + `dotnet test` ran on `main` (0 warnings, 608 tests), and the distiller flag was checked at 04:54:27 before `/evolve-harness` at 04:55:17. **P21 and P22 recurred**; **P10 recurred** (still declined).

**Patterns found:**

1. **P21 recurrence — holdout test code materialized inside the implementer's SubagentStop window (MEDIUM, recurrence).** The orchestrator wrote `Tests/HoldoutVerification/GlobalSkillTreeHoldoutTest.cs` at 04:24:31 and again at 04:35:28, editing it 6× and deleting it at 04:26:35 and 04:36:19. The implementer was live for both windows: the first write landed 0.6 s before its fix-batch task completed (handback 04:23:47, completion 04:24:31.825), and the second landed 19 s before its final task completed (handback 04:35:09, completion 04:35:43). The implementer's `SubagentStop` hook runs `dotnet build` and `dotnet test` in the same working tree (`agents/implementer.md`), so the holdout file was present during that deterministic gate; a failure would have printed `tail -20` of the test log back to the agent. The holdout file also broke the repo build with 5 compile errors (`SkillTreeCatalog.MaxRanksFor` missing, `IReadOnlyList<T>` has no `Contains`, `float`→`int`), which took 5 edits in 24 s to compile, and holdout `H7_UpgradeDiscountEverywhere` then failed on test setup (missing archetype argument) and was fixed by editing the test. The implementer made 0 tool calls touching `HoldoutVerification` / `GlobalSkillTreeHoldoutTest` (grep count 0), so no content leaked. New evidence refines P21: "implementer stopped" must include "SubagentStop validation completed", not just the end of the agent loop. P21 and H8 remain queued.

2. **P20 recurrence #4 — post-merge `current-feature.md` staleness.** The file was committed on the feature branch at 04:37:36 with `**Global skill tree (PR #39, awaiting playtest, 2026-11-02):**` and `branch feature/global-skill-tree`; PR #39 merged at 04:54:22 and no post-merge memory update followed. The Recent Features list has no global-skill-tree entry. Unlike run #10, the merge sequence itself was clean (local + remote branches deleted, `state.json` removed, post-merge build/test on `main`), so P20's remaining gap is specifically the memory sync. P20 remains queued; this run corrected the file (LOW).

3. **P22 recurrence — spec `Status:` is still decorative.** `.claude/specs/global-skill-tree.md:3` says `**Status:** draft` after merge; the file has 14 unchecked `- [ ]` acceptance boxes and 0 checked. Six merged specs still say `draft`. The implementer prompt carried the full spec but no status update; the PR body is the only accurate record. P22 remains queued.

4. **New H9 + P23 — unshipped holdout scenarios are readable through the feature branch's git history.** The orchestrator committed the spec and holdouts together at 03:47:13 (`872e9e6 docs(spec): add global skill tree spec and holdouts`), which includes `.claude/holdouts/global-skill-tree/scenarios.md` (172 lines). The implementer runs in that same branch and at 03:58:58 ran `git show --stat 872e9e6 | head -40 && git log --oneline -3`; its transcript shows the commit message, the holdout file path, and the 172-line stat. It did not read the content, but a plain `git show 872e9e6` (or `git log -p`, or `git diff main`) would have printed the scenario text, and the implementer's PreToolUse deny matches only the literal `.claude/holdouts/` string in tool input, so it would not fire. This is a new leak surface distinct from P9/H5 (enumeration) and H6/H8 (prompts/artifacts). Proposal P23 (workflow) and H9 (deterministic guard).

5. **P19/P15 polling reappeared in a mild form (LOW).** While the four simplify lenses were running (spawned 04:26:44, completed 04:29:40–04:31:02), the orchestrator ran `sleep 1` (04:29:43), `sleep 30` (04:29:52, blocked by the Bash guard with the Monitor/until-loop message), `echo waiting` (04:30:27), and `echo pending` ×3 (04:30:32–04:30:40). Notifications arrived at 04:30:24, 04:30:34, and 04:31:02 and were used. No `ListAgents` or `TaskStop`. The "wait for the notification" half of P19 held; the short echo/sleep loop is the residual habit. P19 remains queued.

6. **P10 recurred five times (LOW, declined).** Main: `(eval):1: no matches found: --include=*.cs` (04:05:47) and `GdUnitRunner_*.cfg` (04:36:20) — the latter aborted the cleanup chain before the intended removal. Implementer: `--include=*.cs` ×2 plus `Tests/*.csproj` and `src/*.csproj`. Each recovered immediately. No action per the human's 2026-10-26 decline.

7. **Implementer iteration was healthy (INFO).** 266 tool calls over 48m16s (121 `Edit`, 81 `Bash`, 59 `Read`, 2 `Write`), concentrated in `src/UI/SkillTreeScreen.cs` (36 edits) and `src/Skills/SkillTreeCatalog.cs` (10) — expected for a new tab. Two self-test failures (`HotkeyEligibility_UsesTheDiscountedTowerCost` NRE, then expected `"7"` got `"10"`) and one `CS1061 IReadOnlyList.Contains` were fixed in place; no fix-then-break cycle. No `.tscn` hand edits, no Godot MCP calls, no destructive git, and no `.claude/` reads.

8. **Gates and review pipeline healthy (INFO, positive).** Build 0 warnings / 0 errors; 608 tests; layout verifier PASS (382 assertions, 384 evaluated, 3 skipped, 0 findings); holdouts 12/12 (620 with the harness). Review: 0 CRITICAL / 1 WARNING / 5 INFO (the warning — Main-based tests depended on the player's real `skilltree.cfg` — plus two correctness INFO were fixed in one batch; three performance INFO deferred). Simplify: 10 cleanups applied. The PR body matches the final numbers. Human said "looks good merge it" at 04:54:13; no playtest fix batches.

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

- **P20 (MEDIUM — recurrence #4, text unchanged from run #10's revision).** Merge Cleanup: (1) `gh pr merge --squash {pr}` then `git fetch origin --prune && git checkout main && git pull --ff-only`; (2) delete the local branch only if present and do not chain optional steps after a command that can fail; (3) delete `state.json`; (4) prune worktrees; (5) distiller check; (6) update `current-feature.md` with the merge date, PR number, and squash SHA and add the feature to Recent Features. Phase 7 must not write "awaiting playtest"/"PR open" into `current-feature.md`. Evidence: run #11's 04:37:36 commit still said awaiting playtest after the 04:54 merge; run #10 was the third recurrence.

- **P21 (MEDIUM — recurrence, stronger evidence).** Never materialize holdout test code in the implementer-accessible workspace. Use a scratch project outside the repo, or write → run → delete in one tool call **after the implementer's SubagentStop validation has completed**. Before the next implementer message and before the final handback, confirm `git status` contains no `Holdout*` file. Mirror the rule in `orchestrator.md` Holdout Verification and `software-factory-patterns.md`. Evidence: `Tests/HoldoutVerification/GlobalSkillTreeHoldoutTest.cs` at 04:24:31 and 04:35:28, the second inside the SubagentStop window (handback 04:35:09 → completion 04:35:43); the file broke the build with 5 compile errors and took 5 edits to compile; the implementer made 0 holdout-path tool calls.

- **P22 (MEDIUM — recurrence, text unchanged).** Remove `**Status:** draft | approved | implementing | done` from `templates/spec-template.md` (preferred; git/PR is the status) or replace it with a static `**Approved:** {date}`; if kept, update it at approval and set `done` + check boxes at Merge Cleanup. Evidence: `global-skill-tree.md:3` says `draft` after PR #39; 14 boxes unchecked; six merged specs say `draft`.

- **P23 (MEDIUM — new). Keep unshipped holdout scenarios out of the implementer-visible git history.**
  - `.claude/skills/implement-feature/SKILL.md` Phase 1: commit the spec and memory update on the feature branch, but keep `.claude/holdouts/{feature}/` untracked until after the implementer's final handback/SubagentStop; at Merge Cleanup `git mv` it to `.claude/holdouts/regression/{feature}/` and commit then (the archived copy lands pre-merge, after the implementer is done).
  - `orchestrator.md` Delegation Rules → Implementer: "Do not inspect repository history while the feature branch contains unpublished holdouts; `git show <sha>`, `git log -p`, and `git diff main` can print them even though the path deny would not match. Use working-tree `git diff` on named source paths only."
  - `software-factory-patterns.md` Orchestrator → Implementer: same note.
  - Evidence: commit `872e9e6` contains the 172-line scenario file on the implementer's branch; the implementer ran `git show --stat 872e9e6` at 03:58:58 and saw the path and line count; no content was read, but a plain `git show` would have leaked it.

- **H9 (HIGH — blocked, human must implement). Deterministic guard for git-history access to holdouts.** Extend the implementer's PreToolUse Bash matcher to deny `git show <sha>`, `git log -p`, and `git diff main`/`git diff <sha>` when the target commit touches `.claude/holdouts/`, or isolate the implementer's workspace from unpublished holdout commits (separate worktree/clone or untracked holdout storage). Match only history forms; keep `git diff` on named working-tree source paths allowed. Evidence: implementer `git show --stat 872e9e6` at 03:58:58; the commit includes `.claude/holdouts/global-skill-tree/scenarios.md`; the path deny does not fire on a bare SHA.

- **H8 (HIGH — blocked, human must implement; unchanged).** Extend the implementer PreToolUse deny to a dedicated holdout-test path convention. Run #11 used `Tests/HoldoutVerification/`, which the current deny does not cover and which the implementer could have listed or compiled. Prefer a prefix the hook can match (`Tests/Holdout*` or `Tests/.holdout/**`) and treat it as holdout-equivalent.

**Applied this run (LOW risk, memory only):** `harness-evolution.md` (change-history row + this entry), `harness-feedback.md` (run #11 observations, P20/P21/P22 recurrences, P23/H9 queued), `current-feature.md` (global-skill-tree marked merged with PR/squash, stale awaiting-playtest line removed, Recent Features updated). No rule, skill, agent prompt, command, template, or game file was modified.
