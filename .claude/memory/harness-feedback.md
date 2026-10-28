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
- **Merge Cleanup step 6 honored (2026-10-25)** — the flag was set at 21:55:42 and `/evolve-harness` ran immediately after PR #25 merged at 22:28, so the advisory gap from runs #5–#6 did not recur.
- **Fix-batch re-verification (2026-10-25)** — after the Level 5 fix batch, the orchestrator sent the batch back to the same correctness lens with a per-finding checklist; it returned CLEAN with line-level evidence for every item. This caught the “test bypasses the production tick” warning and is worth repeating.
- **Agent-scoped holdouts deny + GODOT_BIN (PR #9)** — global `Read(.claude/holdouts/**)` removed; implementer blocked from holdouts via a frontmatter PreToolUse hook (correct `hookSpecificOutput.permissionDecision` format — the old `{"decision": "deny"}` shape was silently ignored). Reviewer + orchestrator keep access. `env.GODOT_BIN` set so bare `dotnet test` passes. Verified: implementer Read denied, reviewer Read allowed, orchestrator `ls` allowed.
- **P3 playtest batching held (2026-10-28)** — five human playtest passes produced exactly five fix batches, each sent to one still-live implementer via `SendMessage` (`aceb750…`, then `a525fc…`); no per-issue respawn, no per-fix gate cycle.
- **P5 single-`Write` state updates held (2026-10-28)** — `state.json` written 12× with `Write`, 0 `Edit`; no stale-content edit failure.
- **Merge Cleanup step 6 honored (2026-10-28)** — flag set 2026-10-26T04:31:03; PR #29 merged 05:10:41Z and `/evolve-harness` invoked 05:11:31Z, 50 s later. Second consecutive merge to honor the check (PRs #25, #29).
- **Implementer holdout deny held under pressure (2026-10-28)** — despite four fix prompts naming holdout IDs, the implementer's Read deny fired and its handback explicitly stated it did not open `.claude/holdouts/`. The deterministic guard works; the leak was in the orchestrator's prompt text, not the data path.
- **Pre-merge bookkeeping held (2026-10-28)** — holdouts moved to `regression/tower-upgrades/`, `current-feature.md` updated, and PR #29 checklist filled on the feature branch before squash merge; no post-merge sync PR.
- **Gates all green (2026-10-28)** — build 0 warnings / 0 errors, 384 tests pass, layout verifier PASS, holdouts 16/16; squash `5db78bb`, post-merge fast-forward re-verified (build clean, 384 tests), local + remote branch deleted, `state.json` removed. No destructive git ops, no `.tscn` hand-edits, no MCP calls (feature was procedural C#).

## What's Broken

### Open 2026-10-28

- **Holdout IDs leaked through orchestrator `SendMessage` prompts (P13)** — four playtest fix batches named H12, H13/H17, H18, and H4/H10/H13/H14. The implementer's deny blocked reads, but the verification set's identity was disclosed, violating the dark-factory handoff rule. Medium.
- **Correctness lens never returned an independent report (P14/P15)** — first reviewer killed at 9m26s, retry at 2m03s, caveman fallback at 3m26s, general-purpose audit at 1m42s; orchestrator self-audited instead ("I completed the correctness audit manually"). Successful lenses took 4m36s–5m44s, so two kills were earlier than a normal successful run. Medium.
- **Orchestrator busy-waits on background agents (P15)** — 116 `sleep`, 101 no-op `true`, 85 `ListAgents`, 5 "start editing now" nudges, 11 `TaskStop` in one 2h56m feature. Task notifications already work. Medium.
- **Forked skills die with API 422 `tool_definition` (P16)** — four failures across two sessions (`/fix-bug` ×2, `/evolve-harness`, `/implement-feature`), request bodies 292–456 KB on `deepseek-flash`. Recovery that worked: adopt the still-live child implementer via `SendMessage` instead of re-spawning. Medium.
- **Five playtest rounds on UI state/visibility (P17)** — 22 spec edits, 15 holdout edits; most fixes were state-table facts (when a badge appears, when a panel hides, which fact is shown where). Medium.

### Open 2026-10-25

- **Reviewer handoff cannot be executed as written (P11)** — three lens prompts told reviewers to run `git diff`, but the reviewer tool list is Read/Glob/Grep only. Every report carried a “no shell” disclaimer. The orchestrator must supply diff text (or changed-file contents); reviewers should never be asked to run git/build/test.

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

### 2026-10-25 — Seventh Distillation

Window: PR #23 (run #6 memory updates), PR #24 (skill-tree tab-switch fix via `/fix-bug`), PR #25 (Level 5 + Progenitor). Evidence: main session `99297c2c-…` (102 Bash, 29 Edit, 9 Agent spawns, 5 skills) plus 11 subagent transcripts; the Oct 25 Open Design investigation sessions were also in the window but contained no game/harness work.

- **P8 recurred, then APPLIED 2026-10-26** — `[RequireGodotRuntime]` was in `implementer.md`, `rules/godot-csharp-conventions.md`, and `CLAUDE.md` while the repo has 0 uses and 24 `[TestSuite]` classes / 261 `[TestCase]` methods. Fixed in PR #26 across all three files; the invalid `[Test]` / `SceneRunner.Load` sample was also replaced.
- **Reviewer handoff is impossible as written (new P11) — FIXED 2026-10-26 (PR #26)** — all 3 lens prompts said “Run `git diff`”, but `.claude/agents/reviewer.md` grants only Read/Glob/Grep. Orchestrator and skill prompts now require passing diff text; reviewers are told they are shell-free.
- **P10 recurred + one new guard — DECLINED 2026-10-26** — one `rm -rf` hook denial (Level 5 implementer) and one zsh unmatched-glob abort (`GdUnitRunner_*.cfg`); the Bash tool also blocked a chained foreground `sleep 30`. Human declined adding host guidance to `verification-gates.md`.
- **Spec-time visual scale (new P12 + memory applied) — TEMPLATE APPLIED 2026-10-26 (PR #26)** — Progenitor 96 → 72 → 64 px across two playtest passes; cadence 6.0 → 3.0 s because a first summon at 6.5 s read as “never summoned”. Grid-relative sizing + telegraph timing are now in `game-design.md` and the spec template.
- **`GdUnitRunner_<uuid>.cfg` leaks — DECLINED 2026-10-26** — `dotnet test` leaves an untracked cfg at the repo root; human declined the `.gitignore` entry.
- **Flag honored at merge** — set 21:55:42, PR #25 merged 22:28, `/evolve-harness` ran immediately. Step 6 worked; run #6's 25-day gap did not recur.
- **P3 batching, P5 single-Write, merge hygiene held** — 3 playtest passes → 3 implementer batches; state.json written 4× with `Write`, never edited; fast-forward main, no destructive git, no stray post-merge PR.
- **Review/simplify healthy** — 1 CRITICAL (post-base summon leak, found independently by security and correctness) + 4 WARNING + 2 INFO, fixed in one batch, re-verified CLEAN by the correctness lens. 4 simplify lenses, 9 cleanups, 14 declined. Build 0 warnings, 261 tests, verifier PASS, holdouts 13/13.
- **Holdout discipline held** — 0/2 implementer transcripts enumerated `.claude` or touched a holdout path.
- **Unrelated activity tripped the flag** — the Oct 25 Open Design debugging session (~100 Bash/MCP calls, no game edits) drove the counter to 62. Scoping the stop hook to game edits would be a HIGH (human-only) change; no action taken.

### 2026-10-28 — Eighth Distillation

Window: PR #29 (tower-upgrades: ranks, hidden XP gates, archetype specialization) plus run #7's memory PR #26. Evidence: main session `2d66f169-…` (462 Bash, 85 ListAgents, 73 Edit, 57 Read, 17 Write, 14 SendMessage, 11 TaskStop, 11 Agent) and its 13 subagent transcripts. Prior session `99297c2c-…` used only for API-422 recurrence.

- **P11/P12 verified landed** (PR #26): reviewer prompts carried diff text and no longer asked for git; spec template carried grid-cell scale + telegraph timing. No reviewer complained it had no shell this window.
- **Holdout-ID leak (new P13, highest-value finding)** — four fix batches named H12, H13/H17, H18, H4/H10/H13/H14. The frontmatter deny blocked reads (implementer handback confirmed), but the orchestrator's prompt text disclosed the verification set. `dark-factory-patterns.md` already forbids mentioning holdouts; it needs IDs and scenario names named explicitly.
- **Review-gate integrity weakened (new P14)** — correctness was self-audited after four failed reviewer attempts. `reviewer.md` has no budget, and the diff was 4,387 lines; the first lens read 38 source files / 23 greps before being killed. Two kills happened before a normal successful lens would have finished.
- **Cover-waiting (new P15)** — 116 sleeps totaling 7,170 s nominal (119.5 min) in a 2h56m session, 101 no-op `true`, 85 `ListAgents`. Notifications work; polling is pure overhead and drives the premature kills.
- **Forked-skill API 422 recurred (new P16)** — four failures / two sessions on `deepseek-flash`; recovery by adopting the live child agent worked. Document it so the next run doesn't restart the loop.
- **UI state matrix missing (new P17)** — five playtest rounds, all state/visibility fidelity; 22 spec edits, 15 holdout edits. A states × visible-groups × triggers table would have front-loaded most of it.
- **Minor: NUnit `Assert.That` slipped in** — implementer wrote 8 occurrences in `Tests/UI/TowerUpgradePanelTest.cs`, converted to `AssertThat` after a failing test run. Final repo clean. No formal proposal; consider an explicit "not NUnit" line if it recurs.
- **Healthy:** P3 batching, P5 single-Write, pre-merge bookkeeping, fast-forward merge, merge cleanup step 6 all held; 0 destructive git ops; 0 `.tscn` hand edits; build 0 warnings, 384 tests, verifier PASS, holdouts 16/16; holdouts moved to `regression/tower-upgrades/`.

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

**P8 — APPLIED 2026-10-26 (human-approved, PR #26): remove the non-existent `[RequireGodotRuntime]` convention.**
`CLAUDE.md`, `.claude/rules/godot-csharp-conventions.md`, and `.claude/agents/implementer.md` now describe the real `[TestSuite]` + `[TestCase]` + `AssertThat` pattern; the invalid `[Test]` / `SceneRunner.Load` sample was replaced with a verified pattern. There is no `[RequireGodotRuntime]` attribute in gdUnit4.api 4.3.1.

**P9 — DECLINED 2026-10-26 by human.** Prohibit holdout enumeration in `.claude/agents/implementer.md`; current wording and hook guard left unchanged.
The implementer's `find src Tests .claude -type f` listed `.claude/holdouts/{armored-laser,first-level,level-4-multi-path,…}/scenarios.md`. The hook matches only the literal `.claude/holdouts/` string, so enumerating the parent directory passes. No scenario content or `H<n>_*` name leaked (0/3 transcripts), but coverage topics did. Proposed wording: "NEVER read **or enumerate** `.claude/holdouts/`; do not run recursive listings/searches over `.claude/`." Full text in run #6, P9.

**P10 — DECLINED 2026-10-26 by human.** Host shell facts (rm -rf denial, zsh glob abort, blocked chained sleep) were not added to the host section.
(a) `rm -rf` is hook-denied even under `/tmp`; use `mktemp -d` or a unique `$(date +%s)` suffix. Three denials in run #6, one in run #7. (b) zsh aborts the whole command on an unmatched glob (`no matches found`) before anything runs, so `rm -f x GdUnitRunner_*.cfg` never deletes `x`; quote patterns or use `find`. Three occurrences in run #6, one in run #7. (c) New in run #7: the Bash tool blocks a foreground `sleep` chained with another command, pointing at Monitor/until-loop or `run_in_background`. Proposed addition to `rules/verification-gates.md` → "Host: macOS (darwin)". Full text in run #7, P10.

**P11 — APPLIED 2026-10-26 (human-approved, PR #26; diff-text variant): make the reviewer handoff match the reviewer's shell-free tool set.**
`orchestrator.md`, `review-changes/SKILL.md`, `implement-feature/SKILL.md`, `reviewer.md`, and `dark-factory-patterns.md` now require the orchestrator to generate the diff and paste it into every reviewer prompt; reviewers are explicitly shell-free and never asked to run git/build/test.
All three Level 5 lens prompts said “Run `git diff`”, but `.claude/agents/reviewer.md` grants only Read/Glob/Grep; every report disclaimed that git/build/test could not be run. Proposed edits: `orchestrator.md:61` → pass the diff text (or changed-file contents), never tell a reviewer to run git/build/test; `review-changes/SKILL.md` step 2 → include the diff in each reviewer prompt; `reviewer.md` Review Process step 1 → “You have no shell: do not attempt git diff, build, or test commands”; `dark-factory-patterns.md` → add an Orchestrator → Reviewer handoff line. Full text in run #7, P11.

**P12 — APPLIED 2026-10-26 (human-approved, PR #26): specify visual scale in grid-cell units.**
`.claude/templates/spec-template.md` now asks for rendered size in grid-cell units (cell = 64 px) and first-telegraph timing relative to spawn/lifetime.
Progenitor diameter was 96 px (1.5 × 64 px cell) → 72 px → 64 px across two playtest passes; first summon at 6.5 s read as “never summoned” and the cadence was halved. Proposed addition to `.claude/templates/spec-template.md` → `## Visual / Geometry Theme`, before LAYOUT: “State rendered sizes for new entities in grid-cell units (cell = 64 px, `GameConstants.CellSize`) and state when the first signature behavior becomes visible.” The matching design heuristic is already applied in `game-design.md`. Full text in run #7, P12.

**Proposal (LOW — DECLINED 2026-10-26 by human) — ignore the GdUnitRunner cfg.**
`dotnet test` leaves `GdUnitRunner_<uuid>.cfg` untracked at the repo root. Add `GdUnitRunner_*.cfg` to `.gitignore`.

**H5 — DECLINED 2026-10-26 by human (deterministic guard not implemented) — guard recursive `.claude` enumeration.**
Extend the implementer's PreToolUse Bash pattern to deny recursive listings/searches rooted at `.claude/` (`find … .claude`, `ls -R .claude`, `tree .claude`, `grep -r … .claude`) while still allowing explicit reads of named harness files (e.g. `cat .claude/settings.json`). P9 is the prompt-level mitigation; this is the deterministic backstop.

**P13 — QUEUED 2026-10-28 (MEDIUM): never name holdout files, IDs, or scenario names to the implementer.**
`orchestrator.md` Delegation Rules, `implement-feature/SKILL.md` Phase 8, and `dark-factory-patterns.md` handoff should say: pass the full spec; never mention holdout scenarios, files, IDs (`H<n>`), or paths; for a playtest fix, update the spec and pass the affected spec sections. Evidence: four leaked batches (H12; H13/H17; H18; H4/H10/H13/H14) while the Read deny held. Full text in run #8, P13.

**P14 — QUEUED 2026-10-28 (MEDIUM): reviewer budget + large-diff splitting.**
`reviewer.md`: diff is primary target; ≤10 source reads / ≤15 tool calls; return partial findings rather than no report; no rereads; use Glob/Grep for paths; one pass on diffs over ~1,500 lines. `review-changes/SKILL.md` and `implement-feature/SKILL.md` Phase 4: split large diffs by subsystem, one reviewer per slice; a lens requires an independent report — if two attempts stall, split or use a general-purpose reviewer with Bash and record the fallback. `orchestrator.md`: no silent self-audit. Evidence: reviewer kill/read counts above. Full text in run #8, P14.

**P15 — QUEUED 2026-10-28 (MEDIUM): wait for task notifications, do not poll.**
`orchestrator.md` new "Waiting for subagents" section and `implement-feature/SKILL.md` Phase 4: after spawning background agents, end the turn; rely on task notifications; no `sleep`/`true`/`ListAgents` loops; inspect the output file once after a notification; allow ≥~6 min for lens reviewers; stop only after 3+ min with no new tool activity, one nudge first, never before one full review pass. Evidence: 116 sleeps / 7,170 s, 101 `true`, 85 `ListAgents`, 11 `TaskStop`; successes 4m36s–5m44s. Full text in run #8, P15.

**P16 — QUEUED 2026-10-28 (MEDIUM): forked-skill API-422 recovery.**
`implement-feature/SKILL.md` (optionally `fix-bug` / `evolve-harness`): on `API Error: 422 … unknown variant tool_definition`, resume the same task-id via `SendMessage` or adopt its live child agents; do not re-spawn the whole loop; keep fork context lean. Evidence: four failures / two sessions on `deepseek-flash`, bodies 292–456 KB; PR #29 recovered by adopting `aceb750…`. Full text in run #8, P16.

**P17 — QUEUED 2026-10-28 (MEDIUM): UI state matrix in the spec template.**
`templates/spec-template.md` new `## UI States` (or table under Visual / Geometry Theme): each state × visible named groups/controls × transition trigger × single source of truth for duplicated facts; explicit "drawn geometry, not text glyph" wording. Evidence: five playtest rounds, 22 spec edits, 15 holdout edits. Full text in run #8, P17.

**H6 — QUEUED 2026-10-28 (HIGH — human only): deterministic guard against holdout IDs in orchestrator→implementer prompts.**
Extend the PreToolUse hook to inspect `Agent` / `SendMessage` inputs addressed to the implementer and deny holdout paths or `H<n>_` tokens. False-positive caveat: legitimate "do NOT read `.claude/holdouts/`" prohibitions must remain allowed. Logged only; not implemented.

## Harness Change Log

Canonical changelog lives in `.claude/memory/harness-evolution.md` (Change History + Distillation Log). Kept there only, to avoid two copies drifting apart.

---

*Initial harness scaffold created 2026-07-07. First distillation run 2026-07-10.*
