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
- **Reviewer handoff and completion (2026-11-01)** — all 3 lens reviewers and all 4 simplify lenses returned independent reports on a 9,315-line diff. The orchestrator wrote the diff to `/tmp` and passed the path; reviewers read it in chunks. No reviewer complained it had no shell, and no reviewer was killed. The correctness lens took 24m42s / 102 tool calls and found the CRITICAL; the security lens independently found the same issue as an out-of-scope note at 8m59s / 77 calls.
- **P3 playtest batching held at higher volume (2026-11-01)** — nine playtest fix batches (one follow-up) all went to the same live implementer via `SendMessage`; no per-issue respawn and no new implementer spawn.
- **P5 single-`Write` state updates held (2026-11-01)** — `state.json` written 21× with `Write`, 0 `Edit`.
- **P13 holdout IDs did not leak (2026-11-01)** — 0 `H<n>` IDs or scenario names in the implementer prompt or any of the 9 fix messages, versus 4 leaked batches in run #8. Path-only prohibitions ("Do not read `.claude/holdouts`") remained, but the verification set's identity was not disclosed.
- **P16 did not recur (2026-11-01)** — no API-422 forked-skill failures in the window; the implementer stayed alive across ~24 h of playtest batches.
- **Merge Cleanup step 6 honored (2026-11-01)** — PR #31 merged 22:18:35Z; `/evolve-harness` was invoked 28 s later (22:19:03Z). Third consecutive merge to honor the check.
- **Full feature run held P3/P5/P13/P18/P19 (2026-11-03)** — one implementer stayed live across four messages (feature, correctness fixes, simplify batch, playtest batch) with no respawns; `state.json` was written 6× with `Write`, 0 `Edit`; 0 holdout IDs or paths appeared in any `Agent`/`SendMessage` prompt and the implementer made 0 tool calls touching `.claude/`; the playtest fix updated the spec and holdouts and committed them on the feature branch; reviewers received the full diff inline (temp file for simplify), all 3 review lenses completed in 4–6 min and all 4 simplify lenses in 49 s–4m42s with no polling, `ListAgents`, `TaskStop`, or `sleep`; post-merge `dotnet build && dotnet test` ran on `main`; `/evolve-harness` ran 55 s after the squash.
- **Small-diff review handoff (2026-11-03)** — security 33 calls / 5m, performance 44 calls / 4m, correctness 43 calls / 6m37s; correctness found 2 WARNING + 2 INFO, both warnings fixed in one batch. No lens stalled or was killed.

## What's Broken

### Open 2026-11-01

- **Playtest deltas never folded into spec or holdouts (P18)** — nine fix batches changed pinned values (icons 22×22→46×46, `15$`→plain number, parenthetical copy removed, top bar shipped despite being Out of Scope, base-core shield arcs added). Zero spec edits and zero holdout edits after the spec-time draft. The archived regression holdouts were not re-verified against the post-playtest code. Medium.
- **P14/P15 as queued conflict with the evidence (P19)** — the correctness lens used 64 source reads / 102 tool calls over 24m42s and found the CRITICAL; the security lens used 77 calls / 8m59s and found it independently. A ≤10-read/≤15-call cap or a ~6-minute kill floor would have suppressed the most valuable finding. Replace with large-diff temp-file handoff and "never time-kill while the agent is producing activity." Medium.
- **Post-merge bookkeeping staleness recurred (P20)** — a `Status:` line was reintroduced into `current-feature.md` and still read "PR open" after the merge; the Recent Features entry said "PR pending"; the PR body kept the pre-playtest counts (499 tests / 214 assertions / 43 files) while final gates were 507 tests / 335 assertions / 47 files. The remote feature branch was not deleted and `main` was not re-verified after the squash. Medium.
- **Holdout guard false positive on an exclusion glob (H7)** — the implementer's `find … -not -path "*/.claude/holdouts/*"` was denied because the hook matches the literal `.claude/holdouts` string anywhere in the command. The implementer lost one round trip while trying to list project files. High (hook change).
- **Simplify triage volume is high (INFO, healthy)** — 4 lenses produced 57 raw findings; the orchestrator applied 24 and skipped 12 with reasons (21 were subsumed by those). All lenses finished in 4–6 min. Applying 24 late cleanups is at the high end of the useful range; the batch correctly protected review-mandated fixes and re-ran full gates.

### Open 2026-11-03

- **Holdout test code written into the implementer-accessible tree (P21/H8)** — three writes to `Tests/HoldoutTowerHotkeysTest.cs` (15,538 B, 02:56:40) and `Tests/HoldoutTowerHotkeysFinalTest.cs` (12,838 B, 03:04:44; 14,773 B, 03:17:10), each deleted 41–63 s later. The implementer was live and idle for the first two and made zero holdout-path tool calls, so no content leaked. The deny hook covers only `.claude/holdouts/`, not `Tests/Holdout*`; a `git status`, listing, or concurrent `dotnet test` could expose the file. Medium (skill/agent prompt), High for the deterministic guard.
- **P20 recurred — post-merge memory and branch cleanup** — `current-feature.md` still said "Active feature (awaiting playtest)", "PR #37 open", and "Tower hotkeys (PR open)"; `gh pr merge --delete-branch` deleted the remote branch, then the skill's `git branch -d` failed and aborted the chain; `origin/feature/tower-hotkeys` remains as a stale tracking ref; old remote branches `feature/layout-verifier` and `chore/distillation-6` remain. Post-merge build/test did run. Medium.
- **Spec status drift (P22)** — `.claude/specs/tower-hotkeys.md:3` says `draft` after merge; the implementer prompt said `approved`; acceptance boxes unchecked; five other merged specs also say `draft`. Medium.
- **P10 recurred twice (declined)** — orchestrator and implementer each hit `(eval):1: no matches found: --include=*.cs`; both recovered in 3 s. No action per the 2026-10-26 decision.
- **Headless cursor assertion** — first test run failed on `Input.GetCurrentCursorShape()` (headless stays `Arrow`); fixed by dropping the assertion and verifying via code inspection. Recorded in `memory/godot-mcp.md`. Low (memory applied).

### Open 2026-10-28

- **Holdout IDs leaked through orchestrator `SendMessage` prompts (P13)** — four playtest fix batches named H12, H13/H17, H18, and H4/H10/H13/H14. The implementer's deny blocked reads, but the verification set's identity was disclosed, violating the software-factory handoff rule. Medium.
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
- **Holdout-ID leak (new P13, highest-value finding)** — four fix batches named H12, H13/H17, H18, H4/H10/H13/H14. The frontmatter deny blocked reads (implementer handback confirmed), but the orchestrator's prompt text disclosed the verification set. `software-factory-patterns.md` already forbids mentioning holdouts; it needs IDs and scenario names named explicitly.
- **Review-gate integrity weakened (new P14)** — correctness was self-audited after four failed reviewer attempts. `reviewer.md` has no budget, and the diff was 4,387 lines; the first lens read 38 source files / 23 greps before being killed. Two kills happened before a normal successful lens would have finished.
- **Cover-waiting (new P15)** — 116 sleeps totaling 7,170 s nominal (119.5 min) in a 2h56m session, 101 no-op `true`, 85 `ListAgents`. Notifications work; polling is pure overhead and drives the premature kills.
- **Forked-skill API 422 recurred (new P16)** — four failures / two sessions on `deepseek-flash`; recovery by adopting the live child agent worked. Document it so the next run doesn't restart the loop.
- **UI state matrix missing (new P17)** — five playtest rounds, all state/visibility fidelity; 22 spec edits, 15 holdout edits. A states × visible-groups × triggers table would have front-loaded most of it.
- **Minor: NUnit `Assert.That` slipped in** — implementer wrote 8 occurrences in `Tests/UI/TowerUpgradePanelTest.cs`, converted to `AssertThat` after a failing test run. Final repo clean. No formal proposal; consider an explicit "not NUnit" line if it recurs.
- **Healthy:** P3 batching, P5 single-Write, pre-merge bookkeeping, fast-forward merge, merge cleanup step 6 all held; 0 destructive git ops; 0 `.tscn` hand edits; build 0 warnings, 384 tests, verifier PASS, holdouts 16/16; holdouts moved to `regression/tower-upgrades/`.

### 2026-11-01 — Ninth Distillation

Window: PR #31 (visual-overhaul: full procedural visual pass) plus run #8's memory PR #30. Evidence: main session `45b9d3bc-…` (2026-10-30T20:55:51Z → 2026-11-01T22:19:06Z; 140 Bash, 29 Read, 23 Write, 20 Edit, 11 SendMessage, 8 Agent, 3 Skill, 0 ListAgents, 0 TaskStop, 1 `sleep 1`) and its 8 subagent transcripts (1 implementer adopted for the whole feature, 3 lens reviewers, 4 simplify lenses). The implementer used 247 Edit / 138 Bash / 57 Read / 30 Write and made 2 errors (one denied holdout-exclusion `find`, one failed `Edit`). All reviewers and simplify lenses finished.

- **Reviewer handoff via temp file worked, and the queued budgets would have broken it.** The orchestrator wrote `/tmp/visual-overhaul.diff` (9,315 lines) and `/tmp/visual-overhaul-simplify.diff` (9,707 lines), passed the paths, and told reviewers to read every line in chunks. All 3 lenses completed (security 8m59s/77 calls, performance 6m38s/49 calls, correctness 24m42s/102 calls). P14's hard cap (≤10 source reads / ≤15 tool calls) and P15's ~6-minute floor would both have killed the correctness and security lenses before they reported the CRITICAL. Revise both under P19.
- **Simplify volume was high but healthy.** 4 lenses produced 57 raw findings (reuse 10, complexity 28, efficiency 8, altitude 11) in 4m00s–5m41s. The orchestrator applied 24 in one batch, skipped 12 with reasons, and subsumed 21 duplicates. The batch explicitly forbade undoing review-mandated fixes and re-ran build/tests/verifier. No stalls and no regressions.
- **Playtest micro-fixes exposed spec drift, not agent thrash.** Nine batches over ~46 h: icons 22→40→46 px, per-draw AA then global 2D MSAA 4×, parenthetical copy removed, top-bar restyle, `$` removed from costs, base-core scale pulse → alpha pulse → counter-rotating arcs. None updated the spec; the spec still pins 22×22 icons and `15$` and lists the top bar as Out of Scope. Final code: 507 tests, 335 layout assertions; PR body still says 499 tests / 214 assertions / 43 files. Proposal P18.
- **Post-merge staleness recurred in the memory file.** `current-feature.md` was edited at 21:24 on Oct 30 to add an `Active:` line and at 22:53 to change it to `Status: awaiting playtest — PR open`; the merge at 22:18 on Nov 1 left it stale, and the Recent Features entry said "PR pending". This is the exact class run #5 eliminated by deleting the `Status:` line; the orchestrator reintroduced it despite the file's own note. Fixed in this run (memory); P20 proposes a skill-level instruction. The remote branch `origin/feature/visual-overhaul` also remains, and no post-merge build/test ran.
- **Holdout guard false positive.** The implementer's file-listing `find` included `-not -path "*/.claude/holdouts/*"`; the PreToolUse guard matched the literal string and denied the whole command. One wasted round trip. This is the deterministic counterpart of P9/H5; H7 proposes an exclusion-aware hook. No holdout content was read.
- **P13/P16/P15 behavior improved without the queued rules.** 0 holdout IDs leaked (vs 4 batches in run #8), no API-422, and no polling/TaskStop loops. The orchestrator waited for notifications (one trivial `sleep 1`). Keep P13 queued for prompt hygiene; P16 remains documentation-only.
- **Merge hygiene:** squash `7cf39e1` on `main`; local branch deleted; `state.json` removed; distiller invoked 28 s after merge. Missing: remote branch deletion and post-merge re-verification.
- **Scope change:** the human added title/result screens and skill-tree visuals after the initial spec draft and later expanded into the top bar. That is normal human iteration, but the Out of Scope list was not updated when the scope expanded (P18).

### 2026-11-03 — Tenth Distillation

Window: PR #37 tower-hotkeys (small input feature). Evidence: main `d890fb16-…` (59 Bash, 19 Edit, 17 Read, 14 Write, 8 Agent, 4 SendMessage, 3 Skill; 02:06–03:21Z) and 8 subagent transcripts (1 implementer, 3 lens reviewers, 4 simplify lenses).

- **Highest-value finding:** holdout test code was written into `Tests/` while the implementer was live. No content leaked (0 holdout-path tool calls by the implementer), but the guard covers only `.claude/holdouts/`. P21/H8.
- **P20 recurred for the third time** — both the memory file and merge cleanup drifted. The remote branch was actually deleted by `gh --delete-branch`, but the subsequent `git branch -d` failed and left a stale tracking ref. Revise the skill to one sequence and add `--prune`.
- **Spec status is decorative** — says `draft` after shipping; the orchestrator passed `approved` only in the prompt. P22.
- **Healthy:** one implementer across four messages; 0 holdout-ID leaks; 0 `.claude` accesses; 0 polling/TaskStop/sleep; reviewers all completed; 560 tests; holdouts 11/11 after the playtest fix; one glob abort each in orchestrator and implementer (P10, declined); no `.tscn`, MCP, or destructive git.

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
`orchestrator.md`, `review-changes/SKILL.md`, `implement-feature/SKILL.md`, `reviewer.md`, and `software-factory-patterns.md` now require the orchestrator to generate the diff and paste it into every reviewer prompt; reviewers are explicitly shell-free and never asked to run git/build/test.
All three Level 5 lens prompts said “Run `git diff`”, but `.claude/agents/reviewer.md` grants only Read/Glob/Grep; every report disclaimed that git/build/test could not be run. Proposed edits: `orchestrator.md:61` → pass the diff text (or changed-file contents), never tell a reviewer to run git/build/test; `review-changes/SKILL.md` step 2 → include the diff in each reviewer prompt; `reviewer.md` Review Process step 1 → “You have no shell: do not attempt git diff, build, or test commands”; `software-factory-patterns.md` → add an Orchestrator → Reviewer handoff line. Full text in run #7, P11.

**P12 — APPLIED 2026-10-26 (human-approved, PR #26): specify visual scale in grid-cell units.**
`.claude/templates/spec-template.md` now asks for rendered size in grid-cell units (cell = 64 px) and first-telegraph timing relative to spawn/lifetime.
Progenitor diameter was 96 px (1.5 × 64 px cell) → 72 px → 64 px across two playtest passes; first summon at 6.5 s read as “never summoned” and the cadence was halved. Proposed addition to `.claude/templates/spec-template.md` → `## Visual / Geometry Theme`, before LAYOUT: “State rendered sizes for new entities in grid-cell units (cell = 64 px, `GameConstants.CellSize`) and state when the first signature behavior becomes visible.” The matching design heuristic is already applied in `game-design.md`. Full text in run #7, P12.

**Proposal (LOW — DECLINED 2026-10-26 by human) — ignore the GdUnitRunner cfg.**
`dotnet test` leaves `GdUnitRunner_<uuid>.cfg` untracked at the repo root. Add `GdUnitRunner_*.cfg` to `.gitignore`.

**H5 — DECLINED 2026-10-26 by human (deterministic guard not implemented) — guard recursive `.claude` enumeration.**
Extend the implementer's PreToolUse Bash pattern to deny recursive listings/searches rooted at `.claude/` (`find … .claude`, `ls -R .claude`, `tree .claude`, `grep -r … .claude`) while still allowing explicit reads of named harness files (e.g. `cat .claude/settings.json`). P9 is the prompt-level mitigation; this is the deterministic backstop.

**P13 — QUEUED 2026-10-28 (MEDIUM): never name holdout files, IDs, or scenario names to the implementer.**
`orchestrator.md` Delegation Rules, `implement-feature/SKILL.md` Phase 8, and `software-factory-patterns.md` handoff should say: pass the full spec; never mention holdout scenarios, files, IDs (`H<n>`), or paths; for a playtest fix, update the spec and pass the affected spec sections. Evidence: four leaked batches (H12; H13/H17; H18; H4/H10/H13/H14) while the Read deny held. Full text in run #8, P13. **Run #9 (2026-11-01):** no recurrence — 0 IDs or scenario names in the implementer prompt or 9 fix messages. Refine the wording: IDs and scenario names are the actual leak; a generic "do not access verification assets" is enough, and the literal path is best avoided because it can also trigger the guard when the implementer copies it into a command (see H7).

**P14 — QUEUED 2026-10-28 (MEDIUM): reviewer budget + large-diff splitting. SUPERSEDED by P19 (2026-11-01).**
The original proposal was `reviewer.md`: diff is primary target; ≤10 source reads / ≤15 tool calls; return partial findings rather than no report; no rereads; use Glob/Grep for paths; one pass on diffs over ~1,500 lines; split large diffs by subsystem. **Do not apply the hard cap as written.** On 2026-11-01 the correctness lens used 64 source reads / 102 tool calls over 24m42s and found the only CRITICAL; the security lens used 45 reads / 77 calls over 8m59s and independently found it. A ≤15-call cap would have suppressed both. Keep the useful parts (diff is primary, no rereads, use Glob/Grep for paths, partial findings better than none) and replace the cap/split with P19's temp-file handoff and stall-only splitting. Full text in run #8, P14.

**P15 — QUEUED 2026-10-28 (MEDIUM): wait for task notifications, do not poll. REVISED 2026-11-01 under P19.**
The original proposal was: after spawning background agents, end the turn; rely on task notifications; no `sleep`/`true`/`ListAgents` loops; allow ≥~6 min for lens reviewers; stop only after 3+ min with no new tool activity. **The 6-minute floor and 3-minute kill rule are unsafe.** On 2026-11-01 the correctness lens ran 24m42s and found the CRITICAL; the security lens ran 8m59s. The run followed notifications without polling (0 ListAgents, 0 TaskStop, one `sleep 1`), so the "wait, don't poll" half is confirmed. P19 keeps that and removes the wall-clock kill. Evidence from run #8: 116 sleeps / 7,170 s, 101 `true`, 85 `ListAgents`, 11 `TaskStop`. Full text in run #8, P15.

**P16 — QUEUED 2026-10-28 (MEDIUM): forked-skill API-422 recovery.**
`implement-feature/SKILL.md` (optionally `fix-bug` / `evolve-harness`): on `API Error: 422 … unknown variant tool_definition`, resume the same task-id via `SendMessage` or adopt its live child agents; do not re-spawn the whole loop; keep fork context lean. Evidence: four failures / two sessions on `deepseek-flash`, bodies 292–456 KB; PR #29 recovered by adopting `aceb750…`. Full text in run #8, P16. **Run #9 (2026-11-01):** no recurrence; the implementer stayed alive and was adopted across nine playtest batches. Keep as documentation, not a blocking gate.

**P17 — QUEUED 2026-10-28 (MEDIUM): UI state matrix in the spec template.**
`templates/spec-template.md` new `## UI States` (or table under Visual / Geometry Theme): each state × visible named groups/controls × transition trigger × single source of truth for duplicated facts; explicit "drawn geometry, not text glyph" wording. Evidence: five playtest rounds, 22 spec edits, 15 holdout edits. Full text in run #8, P17. **Run #9 (2026-11-01):** reinforced, but the visual-overhaul rounds were copy/format/motion rather than state visibility. Pair the state matrix with P18's `## Player-Facing Copy` and visual defaults sections so one template change covers both classes.

**P18 — QUEUED 2026-11-01 (MEDIUM): fold playtest deltas back into the spec and holdouts.**
- `.claude/skills/implement-feature/SKILL.md` Phase 8: after a playtest fix batch, update the spec with a short "Playtest adjustments" subsection listing accepted values, copy, and scope changes; if the human expands scope, update `Out of Scope`. Re-run the affected holdouts after the final batch and record the result in the PR before merge.
- `.claude/templates/spec-template.md`: add `## Player-Facing Copy` (exact strings; no internal formula values in parentheses; currency format) and `## Visual Defaults` (antialiasing/MSAA policy; motion must not move or scale static geometry per frame; UI icon sizes relative to row/card). This subsumes the copy/format/motion half of P17.
- Evidence: 9 playtest batches, 0 spec edits and 0 holdout edits after the spec draft; spec still pins 22×22 icons (shipped 46×46), `15$` (shipped plain number), top-bar redesign as Out of Scope (shipped), and the 23/23 holdout result predates the post-playtest commits; final gates were 507 tests / 335 assertions, while the PR body still said 499 / 214 / 43 files.

**P19 — QUEUED 2026-11-01 (MEDIUM): large-diff reviewer handoff and no time-kill.**
- `.claude/agents/orchestrator.md` Delegation Rules → Reviewer, `.claude/skills/review-changes/SKILL.md` steps 1–2, `.claude/skills/implement-feature/SKILL.md` Phase 4, and `.claude/memory/software-factory-patterns.md`: for diffs under ~1,500 lines paste the text; for larger diffs write the diff to a temp file and pass the absolute path, telling the reviewer to read every line in chunks. Do not cap source reads or tool calls. Split by subsystem only after a lens has stalled once, because cross-cutting bugs span subsystems.
- `.claude/agents/reviewer.md`: cover the full supplied diff first; read source files only to confirm a suspicion; never reread; use Glob/Grep for paths (`src/WaveManager.cs` does not exist; use `src/Levels/WaveManager.cs`); return partial findings rather than no report.
- `.claude/agents/orchestrator.md` new "Waiting for subagents" section: after spawning background agents, end the turn and wait for the completion notification; no `sleep`/`true`/`ListAgents` loops. Never TaskStop a lens that is still producing tool activity. Only after 15+ minutes of no new activity, send one nudge; stop only if it remains idle. Never stop a reviewer before one full pass.
- Evidence: 3/3 lenses completed on a 9,315-line diff; correctness used 64 reads / 102 calls / 24m42s and found the CRITICAL; security used 45 reads / 77 calls / 8m59s and found it independently. P14's ≤10-read cap and P15's ~6-minute floor would have killed both. This run followed the wait policy with 0 ListAgents / 0 TaskStop.

**P20 — QUEUED 2026-11-01 (MEDIUM): merge hygiene for memory and branches.**
- `.claude/skills/implement-feature/SKILL.md` Phase 2 step 3: update `current-feature.md` with the feature name, branch, and spec only — never a `Status:`/phase line; `.claude/state.json` is the sole phase owner.
- Merge Cleanup: after `state.json` is deleted, update `current-feature.md` to replace "PR open"/"PR pending" with the merge date, PR number, and squash SHA; run `git push origin --delete feature/{name}`; optionally run `dotnet build && dotnet test` on `main` once and record it.
- Evidence: run #9 had to correct a `Status: awaiting playtest — PR open` line and a "PR pending" Recent Features entry after PR #31 merged; `origin/feature/visual-overhaul` and `origin/feature/layout-verifier` remain; no post-merge build/test ran after the squash. This does not re-propose P4: no post-merge PR, just a local memory edit that the immediately-following distillation PR carries.
- **Run #10 (2026-11-03):** third recurrence. `current-feature.md` again contained an "Active feature (awaiting playtest)" line and "PR #37 open". `gh pr merge --delete-branch` did delete the remote branch, so the queued `git push origin --delete` is unnecessary — change step 1 to include `--delete-branch`, add `git fetch origin --prune`, and make step 3 conditional (it failed with "branch not found" and aborted the chain). Post-merge build/test did run this time.

**H7 — QUEUED 2026-11-01 (HIGH — human only): make the holdout Bash guard exclusion-aware.**
The PreToolUse hook denies any command containing the literal `.claude/holdouts` string. On 2026-10-30 the implementer's `find … -not -path "*/.claude/holdouts/*"` was denied even though it was only excluding the directory, costing one round trip. Allow the literal path when it appears solely in a negation/exclusion argument (`-not -path`, `! -path`, `--exclude`/`--exclude-dir`) and deny actual read/listing forms. P9 remains the prompt-level mitigation; H6 remains the prompt-leak guard.

**H6 — QUEUED 2026-10-28 (HIGH — human only): deterministic guard against holdout IDs in orchestrator→implementer prompts.**
Extend the PreToolUse hook to inspect `Agent` / `SendMessage` inputs addressed to the implementer and deny holdout paths or `H<n>_` tokens. False-positive caveat: legitimate "do NOT read `.claude/holdouts/`" prohibitions must remain allowed. Logged only; not implemented.

**P21 — QUEUED 2026-11-03 (MEDIUM): never materialize holdout test code in the implementer-accessible workspace.**
`.claude/skills/implement-feature/SKILL.md` Phase 5: "Holdout test artifacts must not be written into `Tests/` while the implementer is live. Use a scratch test project outside the repo, or write → run → delete in one tool call with the implementer stopped. Before the next implementer message, confirm `git status` contains no `Holdout*` file." Mirror in `.claude/agents/orchestrator.md` Holdout Verification and `.claude/memory/software-factory-patterns.md` Orchestrator → Implementer. Evidence: three writes totalling ~43 KB to `Tests/HoldoutTowerHotkeysTest.cs` and `Tests/HoldoutTowerHotkeysFinalTest.cs` while implementer `ad7fe195…` was live (02:56, 03:04, 03:17); the implementer made zero holdout-path tool calls, so no content leaked. The deny hook covers only `.claude/holdouts/`.

**P22 — QUEUED 2026-11-03 (MEDIUM): stop the spec `Status:` line from going stale.**
Preferred: remove `**Status:** draft | approved | implementing | done` from `.claude/templates/spec-template.md` (git/PR is the status; P4 precedent), or replace it with a static `**Approved:** {date}`. If kept: after human approval, edit the committed spec to `approved`; Merge Cleanup sets `done` and checks acceptance boxes. Evidence: `.claude/specs/tower-hotkeys.md:3` says `draft` after PR #37; the implementer prompt quoted `approved`; five other merged specs also say `draft`.

**H8 — QUEUED 2026-11-03 (HIGH — human only): holdout-artifact deny for the implementer workspace.**
Extend the implementer PreToolUse deny to a dedicated holdout-test path convention (`Tests/Holdout*` or `Tests/.holdout/**`) so holdout test names/content cannot be listed or read even if written into the shared tree. Use a dedicated prefix to avoid false positives. Evidence: three `Tests/Holdout*` writes during a live implementer session (P21).

## Harness Change Log

Canonical changelog lives in `.claude/memory/harness-evolution.md` (Change History + Distillation Log). Kept there only, to avoid two copies drifting apart.

---

*Initial harness scaffold created 2026-07-07. First distillation run 2026-07-10.*
