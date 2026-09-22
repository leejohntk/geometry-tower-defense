# Feature Spec: tower-skill-tree-framework

**Status:** approved
**Branch:** feature/tower-skill-tree-framework
**Date:** 2026-09-21

Part 1 of 2. Part 2 (`tower-skill-tree-mechanics`) activates the mechanic nodes.

## Description

Builds the per-tower skill tree system: persistent currency, the tree data model, persistence, the title-screen skill tree UI, and the **stat nodes** (pure number upgrades). The **mechanic nodes** (pierce, crit, cluster, stun, ignite, chain, ramp-up) are rendered in the tree but disabled with a "coming soon" label — they activate in Part 2.

- **Persistent currency** — "Skill Points" (SP), earned per kill, tiered by enemy kind. Separate from in-level coins (coins unchanged). Persists across levels and sessions.
- **Three trees** — Arrow, Cannon, Laser. Each tree = a trunk of stat nodes + two seed (mechanic) nodes. Both seeds present, no lockout.
- **Five ranks max** per node, bought with SP.

## Currency Design

New persistent currency: **Skill Points (SP)**. Awarded per **successful level run**, not per kill.

| Outcome | SP |
|---------|----|
| Victory on level N | `2 × N` (L1=2, L2=4, L3=6, L4=8) |
| Defeat on level N | `N` (half of a win, partial credit) |

Replay pays every time (no cap), so players can grind earlier levels and experiment with builds. In-level coins (`CoinDrop`) are unchanged and still per-kill. Node cost: **flat 10 SP per rank** (tunable).

## Skill Tree Catalog

All 15 nodes defined now (so the full tree shape renders). Mechanic nodes are `enabled: false` in Part 1 — shown greyed, non-buyable, labeled "coming soon". Only the 8 stat nodes are buyable and effective here.

### Arrow Tower — archetypes: Sharpshooter | Barrage

| Node | Kind | Status | Effect (Part 1) | Per rank | Rank 5 total |
|------|------|--------|-----------------|----------|--------------|
| Damage | trunk | enabled | flat +damage | +2 | 20 |
| Attack Speed | trunk | enabled | fire interval ÷ (1 + 0.1·r) | +10% | 1.0s |
| Range | trunk | enabled | +range | +0.5 cell | 6.5 cells |
| Pierce | seed | disabled | — (Part 2) | +1 | — |
| Crit Chance | seed | disabled | — (Part 2) | +8% | — |

### Cannon Tower — archetypes: Bombardier | Concussive

| Node | Kind | Status | Effect (Part 1) | Per rank | Rank 5 total |
|------|------|--------|-----------------|----------|--------------|
| Powder Charge | trunk | enabled | +damage AND +projectile speed | +1 dmg, +10% speed | 20 dmg, +50% speed |
| Attack Speed | trunk | enabled | fire interval ÷ (1 + 0.1·r) | +10% | ≈1.67s |
| Splash Radius | trunk | enabled | +AoE radius | +8px | 104px |
| Cluster | seed | disabled | — (Part 2) | +1 | — |
| Stun Chance | seed | disabled | — (Part 2) | +6% | — |

Cannon stays the **slowest** tower even at max attack speed (≈1.67s > Arrow 1.0s).

### Laser Tower — archetypes: Arc/Chain | Melter

| Node | Kind | Status | Effect (Part 1) | Per rank | Rank 5 total |
|------|------|--------|-----------------|----------|--------------|
| DPS | trunk | enabled | flat +dps | +0.8 | 8 |
| Range | trunk | enabled | +range | +0.5 cell | 5.5 cells |
| Ignite | trunk | disabled | — (Part 2) | +5% | — |
| Chain | seed | disabled | — (Part 2) | +1 | — |
| Ramp-Up | seed | disabled | — (Part 2) | +20% | — |

## Data Model & Persistence

- `SkillTreeState` — holds SP balance and current rank of every node; loadable/savable.
- `SkillTreeCatalog` — node definitions (id, tower type, name, kind trunk/seed, `enabled` flag, effect spec).
- `SkillTreeSave` — flat map node id → rank, plus SP. Saved to `user://skilltree.cfg` (Godot `ConfigFile`). Loaded on startup; default 0 SP / 0 ranks.
- SP and ranks persist across level replay, game over, victory, and app restart.

## Stat Application

Tower final stats layer skill modifiers onto the base constants. `RangeCells` becomes `float` (and `GameConstants.TowerRange` returns `float`) to support +0.5-cell steps.

## UI

- Title screen gets a **"Skill Tree"** button (next to level buttons).
- New `SkillTreeScreen` (Control):
  - Shows current SP.
  - One panel per tower, listing nodes with name, rank ("3/5"), and cost.
  - Enabled nodes: click to buy a rank if SP ≥ cost and rank < 5.
  - Disabled (mechanic) nodes: greyed, "coming soon", not buyable.
  - Back button returns to title.
- Wired into `Main.cs` scene flow (title ↔ skill tree ↔ title). Created via Godot MCP, not hand-edited `.tscn`.

## Dependencies

- `GameManager.OnEnemyDestroyed` — award SP alongside coins.
- `Tower` base / variants — final stats apply skill modifiers; `RangeCells` → `float`.
- `Main.cs` — skill tree screen wiring.
- `GameConstants` — new SP constants, stat-node effect constants, cost.
- No existing persistence system — this feature adds the first one.

## Constraints

- **Performance:** skill modifiers are cheap reads; no per-frame allocation.
- **Memory:** save file tiny; no new object pools.
- **Platform:** keyboard + mouse.

## Visual / Geometry Theme

- **Skill nodes** as small geometric shapes: trunk = circle, seed = triangle/diamond, color-coded per tower (Arrow blue, Cannon green, Laser magenta). Rank = filled pips (0–5). Disabled nodes greyed.
- **Skill tree screen** — dark background matching title, clean sans-serif, geometric HUD.
- No AI-generated assets.

## Out of Scope

- Mechanic node effects (Part 2, `tower-skill-tree-mechanics`).
- In-game tower upgrade / archetype specialization (future feature).
- Respec / refund.
- Global meta-progression skill tree (separate system, untouched).

## Constants (tunable — review before approve)

| Constant | Value | Notes |
|----------|-------|-------|
| `SkillPointVictoryMultiplier` | 2 | win = 2 × level id |
| `SkillPointLossDivisor` | 2 | loss = victory / 2 (half credit) |
| `SkillNodeRankCost` | 10 | flat, SP per rank |
| `SkillMaxRanks` | 5 | |
| Arrow: `SkillArrowDamagePerRank` | 2 | |
| Arrow: `SkillArrowAttackSpeedPerRank` | 0.10 | fire interval ÷ (1 + 0.10·r) |
| Arrow: `SkillArrowRangePerRank` | 0.5 | cells |
| Cannon: `SkillCannonPowderDamagePerRank` | 1 | |
| Cannon: `SkillCannonPowderSpeedPerRank` | 0.10 | projectile speed |
| Cannon: `SkillCannonAttackSpeedPerRank` | 0.10 | |
| Cannon: `SkillCannonSplashRadiusPerRank` | 8 | px |
| Laser: `SkillLaserDpsPerRank` | 0.8 | |
| Laser: `SkillLaserRangePerRank` | 0.5 | cells |
