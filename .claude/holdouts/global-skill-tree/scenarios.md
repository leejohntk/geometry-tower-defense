# Holdout Scenarios — global-skill-tree

Implementer must never see this file. Run every scenario after build + test gates are green.
A scenario passes only when every expected observation is verified; "looks right" is not a pass.

## H1 — Catalog identity and max ranks

**Steps:**
1. Enumerate `SkillTreeCatalog.All`; count per tower and per global.
2. Read each global node's id, display name, effect, max rank.
3. Call `ForTower` for Arrow, Cannon, Laser and the global accessor.

**Expected:**
- 19 definitions total: 15 tower + 4 global.
- Global ids are exactly `global.starting_hp`, `global.starting_gold`,
  `global.tower_discount`, `global.upgrade_discount`, in that order.
- Max ranks are 5, 5, 3, 3; tower nodes remain 5.
- `ForTower(Arrow/Cannon/Laser)` returns only that tower's 5 nodes, never a global node.
- Global list contains no tower node; all ids unique.

## H2 — Sequential unlock chain

**Setup:** Fresh state, grant SP as needed.

**Steps:**
1. At all ranks 0, check `IsUnlocked` and buy eligibility for all four global nodes.
2. Buy node 1 rank 1; re-check nodes 2–4.
3. Raise node 2 to rank 1; re-check nodes 3–4.
4. Raise node 3 to rank 1; re-check node 4.
5. Attempt to buy a locked node directly through `BuyRank`.

**Expected:**
- Before any purchase only node 1 is unlocked and buyable.
- Buying node 1 unlocks node 2 only; node 3/4 stay locked.
- Each rank-1 milestone unlocks exactly the next node.
- Locked `BuyRank` returns false, spends no SP, and rank stays 0.

## H3 — SP cost, ranks and caps

**Steps:**
1. With 30 SP, buy each node to its max.
2. Record SP after each purchase; read rank labels.
3. Attempt one more buy on each maxed node.
4. With 9 SP, attempt any affordable-rank purchase.

**Expected:**
- Every successful rank costs exactly 10 SP.
- Caps: 5 / 5 / 3 / 3; labels read `5/5`, `5/5`, `3/3`, `3/3`; buy action reads `MAX`.
- Extra buys return false, increment nothing, spend nothing.
- 9 SP buys nothing.

## H4 — Starting HP

**Setup:** Fresh game flow. Set Fortified Core rank to 0, then 1, then 5 (separate runs).

**Steps:** Start Level 1 each time; read `GameManager.HP` immediately after `Initialize`.

**Expected:** Starting HP is 3 / 4 / 8 respectively, with coins at their rank-0 value. No other
stat changes. Starting the next level re-reads the current rank.

## H5 — Starting gold

**Setup:** War Chest rank 0, then 1, then 5; Fortified Core at rank ≥ 1 as needed to unlock.

**Steps:** Start a level each time; read `GameManager.Coins` after `Initialize`.

**Expected:** Starting gold is 10 / 12 / 20 (base 10 plus 2 per rank). Tower costs are unaffected
by this node; kill rewards still add their normal amount.

## H6 — Tower price discount everywhere

**Setup:** Master Blueprints rank 0, then 1, then 3 (chain unlocked).

**Steps:**
1. Read the placement-card price for Arrow, Cannon, Laser.
2. With exactly the discounted price in coins, place each tower type through the real input path.
3. With one coin less than the discounted price, attempt placement (mouse and number hotkey).
4. Read coins before/after each successful placement.
5. Open the HUD at rank 3 and inspect disabled/enabled card styling against the coin count.

**Expected:**
- Prices display 10 / 9 / 7 at ranks 0 / 1 / 3 for all three types.
- Placement at exactly the effective price succeeds and deducts exactly that price.
- One coin short fails for both mouse and hotkey; no tower, no deduction.
- At rank 3 with 7–9 coins a card is enabled and the hotkey places; at 6 coins it is disabled
  and the hotkey is inert.
- No surface shows the base 10 while the discount is active.

## H7 — Upgrade discount everywhere

**Setup:** Advanced Training rank 0, then 3. Placed tower with the XP gates open.

**Steps:**
1. Read the upgrade-panel action labels for rank 1→2, 2→3 and the archetype choice.
2. Buy each with exactly the displayed coins; verify deduction.
3. One coin short: verify the action is unavailable (button/disabled) and no deduction.
4. Repeat for Arrow, Cannon and Laser.

**Expected:**
- Base prices are 5 / 10 / 15; at rank 3 they are 2 / 7 / 12 for all tower types.
- Labels, affordability and deduction all use the discounted price.
- Archetype choice is discounted too.
- No discounted price ever reaches 0 or below.

## H8 — Clamp and no free purchases

**Setup:** Max both discounts. Compare against the lowest base costs in the catalog.

**Steps:** Enumerate every tower placement and upgrade base cost; subtract the max rank (3).

**Expected:**
- Every result is ≥ 1; with current data the minimum is 2 (5 − 3).
- No code path can reach 0; a synthetic clamp check returns 1 if fed a base cost of 3 or less.
- No placement/upgrade succeeds with 0 coins.

## H9 — Persistence and old saves

**Steps:**
1. Buy global ranks, force a save, construct a fresh `SkillTree`, load the file.
2. Load a synthetic config containing the 15 old tower keys and no global keys.
3. Load a synthetic config with `global.tower_discount = 9` and a negative global rank.
4. Load a config with an unknown `global.*` id.

**Expected:**
- All global ranks round-trip exactly; SP round-trips exactly.
- Old config loads with global ranks 0 and tower ranks unchanged.
- 9 clamps to 3; negative clamps to 0.
- Unknown ids are ignored/rejected without corrupting valid ranks.

## H10 — Global tab and tree rendering

**Steps:**
1. Open the skill tree screen; enumerate sidebar entries.
2. Select `Tab_Global`; enumerate tree rows and their named children.
3. Hover each row and read the info band; inspect locked row 2 at fresh state.
4. Buy node 1 with enough SP; verify rebuild in place; repeat to unlock node 2.

**Expected:**
- Fourth sidebar entry labeled `Global`, below Laser, same size/spacing; selecting it does not
  disturb tower tabs.
- Exactly 4 rows in unlock order: `Row_global.starting_hp` … `Row_global.upgrade_discount`,
  each with named `Icon`, `Name`, `Rank`, and `Buy` (or `MAX` at cap).
- Header names the Global branch; info band shows the node description with per-rank numbers and
  the correct predecessor hint for locked rows.
- Buying updates rank, SP badge and unlock state without leaving the screen or duplicating rows.
- Tower tabs still render their 5 nodes and keep their state after visiting Global.

## H11 — Visual and layout contract

**Steps:** Capture the global tab at fresh, partially bought and maxed states. Run the headless
layout verifier.

**Expected:**
- Global accent is cyan and distinct from the three tower colors; node glyphs are distinct
  geometric marks (shield, coins, discount square, stacked chevrons).
- Locked rows grey with slash; max rows use the gold halo/dot; hover highlight and pulse rules
  match tower rows; no visible jank or per-frame position change.
- `Tab_Global` does not overlap sidebar tabs, tree area, info band, title block or Back button.
- Layout verifier passes with zero findings; all wrapper Controls ignore mouse.

## H12 — Cross-system isolation

**Steps:**
1. With max global discounts, read tower stats (damage, range, fire rate) and skill-tree tower
   node effects.
2. Buy tower-tree ranks; verify starting HP/gold and discount prices do not change.
3. Start a new run with tower nodes maxed but global ranks 0.

**Expected:**
- Global discounts affect only coin prices, never combat stats.
- Tower-node purchases never alter global effects or starting resources.
- Both systems share SP but not effects; SP is deducted once per purchase across both branches.
