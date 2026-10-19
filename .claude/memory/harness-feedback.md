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

- **Merge Cleanup step 6 is advisory** — the distiller flag was set 2026-09-21T04:37:00Z and PR #18 merged 2026-09-22T04:44:57Z with no check; PR #19 did run the check. Recurred 2026-09-24: the flag was set at 02:12:12Z and PR #21 merged at 02:34:25Z with no check, then the flag sat 25 days until PR #22's merge (2026-10-19T23:47Z) ran it. The step works when invoked; nothing makes it load-bearing.

### Resolved 2026-10-19

- **Post-merge `current-feature.md` staleness** — resolved by deleting the volatile `Status:` line rather than adding post-merge bookkeeping (P4 was rejected; see below). The file records history only, and `.claude/state.json` is the sole phase owner. Verified in run #6: PR #22 updated `current-feature.md` pre-merge on the feature branch.
- **H3 — destructive git ops hook guard** — implemented in PR #21. The PreToolUse guard now gates `git reset --hard`, `git clean -f`, path `checkout`, and `restore` on repo state (clean tree allowed; dirty tree denied with a `git stash push -u` pointer). It also fixed two real defects found while implementing it: force-push false positives on branch names containing `-f`, and command-extraction truncation at the first double quote (`jq` now parses stdin JSON). Verified 17/17. Zero destructive git ops executed in run #6's window.

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
- **Run #4's P1/P2 verified landed** (PR #17): zero `reset --hard` this window (three in run #4's), clean fast-forward merges, bookkeeping folded onto the feature branch pre-merge. P2's removal of the post-merge writes is what created the staleness in "What's Broken" — the fix traded one problem for another. **Long-term rule the human set 2026-09-23:** memory/config files must be updated **pre-merge** on the feature branch; post-merge stray PRs to sync them are not acceptable. Distillation runs are the deliberate exception and keep their own `chore/distillation-N` PRs.
- **Holdout discipline held** — 0 of 12 implementer transcripts contained an `H<n>_*` scenario name; the frontmatter deny fires. Security lens and reviewer `Bash`-freedom (run #3's M3) both hold.
- **Review produced real value** — `src/Grid/GridManager.cs:286`, placement preview uses base range instead of the skill-modified range after Range ranks are bought. Lenses used `Warning:`/`Info:` casing instead of the protocol's `WARNING`/`INFO` (cosmetic).
- **No code thrash** — 2 error classes across 27 subagents, 6 compiler errors total (`CS0246 List<> missing using`, `CS1503 void→string?`). No fix-then-break cycles.

### 2026-10-19 — Sixth Distillation

Window: PR #21 (run #5 follow-ups + PreToolUse guard) and PR #22 (headless layout verifier). Evidence: main session `93e5a1e3-…` plus 14 subagent transcripts; 110 main-session tool calls.

- **Run #5 proposals verified landed** — P3 (playtest batching), P5 (single-`Write` state updates), P6 (`--quit-after`), P7 (host facts) all in PR #21. H3 closed by the same PR (see above).
- **Stale test convention (highest-value finding)** — `[RequireGodotRuntime]` appears in CLAUDE.md, a rule, and the implementer prompt, but not in gdUnit4.api 4.3.1 and not in any test. The implementer prompt also uses `[Test]` instead of the repo's `[TestCase]`. Proposal P8.
- **Holdout enumeration** — `find src Tests .claude` exposed holdout folder/file names to the implementer. No content read, no `H<n>_*` names. Proposal P9 + H5.
- **Host friction** — 3 `rm -rf` denials and 3 zsh unmatched-glob aborts. Proposal P10.
- **Review pipeline healthy** — 13 findings (0C/9W/4I) across 3 Bash-free lenses on PR #22; security caught save-safety, correctness caught 3 tautologies, performance caught the unbounded verifier run. All fixed in one batch; simplify pass was output-identical. 203 tests pass; verifier 139 evaluated / 137 assertions / 2 skipped / 0 findings.
- **Pre-merge bookkeeping held** — PR #22 committed `current-feature.md`, spec, and tool docs on the feature branch. No post-merge sync PR.
- **Artifact unavailable** — one refused publish (ANTHROPIC_AUTH_TOKEN takes precedence over claude.ai login). One round trip; retries futile. No action.
- **Flag cadence** — flag set 2026-09-24T02:12:12Z, skipped at PR #21's merge 22 minutes later, honored at PR #22's merge 25 days later. Step 6 remains advisory (see "Open 2026-09-23").

**Queued proposals (await human approval — do not apply to skills/rules/CLAUDE.md without sign-off):**

*(Run #4's P1 and P2 were approved and landed as PR #17 — closed. Run #5's P3/P5/P6/P7 landed as PR #21 — closed. H3 closed by PR #21.)*

**P3 — APPLIED 2026-09-23 (human-approved).** `skills/implement-feature/SKILL.md` Phase 8 now says to batch: collect every issue from one playtest pass into a single fix list and spawn **one** implementer for the whole batch, re-spawning only for a genuinely new defect found after the batch lands. The 8-spawns-one-file evidence is quoted inline as the rationale.

**P4 — REJECTED by human 2026-09-23. Do not re-propose.** The original proposal was to restore the
post-merge status flip on a short-lived `chore/post-merge-{feature}` branch + PR. Rejected because it
would create a stray PR after **every** feature merge to maintain a line nobody reads. Investigation
that settled it:
- `session-start.sh:40` surfaces this file with `head -5` — frontmatter only. The `Status:` line was
  line 10 and was **never shown** at session start.
- `.claude/state.json` is already the authoritative phase owner: gitignored (`.gitignore:29`), read
  by `session-start.sh:28`, and deleted at merge → the next session prints `No state.json — clean
  start.` Correct by construction, no rule required.
- The `Status:` line was therefore a redundant second copy of a fact `state.json` already owned.

**Resolution applied instead:** the volatile `Status:` line was deleted from
`current-feature.md`, whose frontmatter description now states that live phase lives in
`state.json` and the file records history only. This eliminates the staleness class rather than
managing it — no new rule, no stray PR. General principle for future runs: when a staleness gap
appears, prefer deleting the redundant state over adding bookkeeping to sync it.

**P5 — APPLIED 2026-09-23 (human-approved).** `skills/implement-feature/SKILL.md` gained a "State file updates (applies to every phase below)" section: read `state.json` once, then write the whole file with a **single** `Write` — never one `Edit` for `phase` plus a second for `blocked_on`. Documents the ~46-writes-per-feature-pair cost and the stale-content `Edit` failure it removes.

**P6 — APPLIED 2026-09-23 (human-approved).** Build & Run now reads `godot --headless --quit-after 300` with a comment explaining that the game loop never exits on its own so an unbounded invocation hangs. `--quit-after <int>` verified present in `godot --help`; `--run-stdout` verified absent (count 0). No remaining `run-stdout` references in `CLAUDE.md`.

**P7 — APPLIED 2026-09-23 (human-approved).** `rules/verification-gates.md` gained a "Host: macOS (darwin)" section above Gate 1: no `timeout` / `gtimeout` (use the Bash tool's own timeout parameter), and bound headless Godot runs with `--quit-after <frames>` since the game loop never exits on its own (`--run-stdout` is not a flag).

**H3 — CLOSED 2026-09-24 (PR #21).** The hook guard landed, along with two guard defects fixed while implementing it (standalone-token force matching; `jq`-parsed command extraction covering `HEAD:main` / `refs/heads/main`). Verified 17/17; zero destructive git ops in run #6's window. See "Resolved 2026-10-19".

**P8 (MEDIUM — proposed, run #6) — remove the non-existent `[RequireGodotRuntime]` convention.**
`CLAUDE.md:34,76`, `.claude/rules/godot-csharp-conventions.md:44`, and `.claude/agents/implementer.md:55,67,90` all reference an attribute that does not exist in gdUnit4.api 4.3.1 and is used by zero tests. The repo's actual pattern is `[TestSuite]` + `[TestCase]` + `AssertThat` (see `Tests/Towers/TowerTest.cs`). A review-fix implementer burned ~12 tool calls investigating the attribute. Full replacement text in `harness-evolution.md` → run #6, P8. CLAUDE.md lines are outside distiller scope — human applies.

**P9 (MEDIUM — proposed, run #6) — prohibit holdout enumeration.**
The implementer's `find src Tests .claude -type f` listed `.claude/holdouts/{armored-laser,first-level,level-4-multi-path,…}/scenarios.md`. The hook matches only the literal `.claude/holdouts/` string, so enumerating the parent directory passes. No scenario content or `H<n>_*` name leaked (0/3 transcripts), but coverage topics did. Proposed wording: "NEVER read **or enumerate** `.claude/holdouts/`; do not run recursive listings/searches over `.claude/`." Full text in run #6, P9.

**P10 (LOW–MEDIUM — proposed, run #6) — host shell facts.**
(a) `rm -rf` is hook-denied even under `/tmp`; use `mktemp -d` or a unique `$(date +%s)` suffix. Three denials this window. (b) zsh aborts the whole command on an unmatched glob (`no matches found`) before anything runs, so `rm -f x GdUnitRunner_*.cfg` never deletes `x`; quote patterns or use `find`. Three occurrences. Proposed addition to `rules/verification-gates.md` → "Host: macOS (darwin)". Full text in run #6, P10.

**H5 (HIGH — blocked, human must implement) — guard recursive `.claude` enumeration.**
Extend the implementer's PreToolUse Bash pattern to deny recursive listings/searches rooted at `.claude/` (`find … .claude`, `ls -R .claude`, `tree .claude`, `grep -r … .claude`) while still allowing explicit reads of named harness files (e.g. `cat .claude/settings.json`). P9 is the prompt-level mitigation; this is the deterministic backstop.

## Harness Change Log

Canonical changelog lives in `.claude/memory/harness-evolution.md` (Change History + Distillation Log). Kept there only, to avoid two copies drifting apart.

---

*Initial harness scaffold created 2026-07-07. First distillation run 2026-07-10.*
