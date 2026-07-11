# Holdout Scenarios: First Level

Implementer must never see this file. These scenarios are run by the orchestrator during Gate 4 holdout validation.

---

## H1: Tower Button Disabled When Poor

**Setup:** Player has 0-9 coins.
**Action:** Check tower placement button state.
**Expect:** Button disabled. Cannot be clicked. No tower placed.
**Verify:** No coin debt incurred. Button re-enables when coins reach 10 (kill an enemy).

## H2: Mid-Wave Tower Placement

**Setup:** Wave active. Enemies marching. Player has >= 10 coins.
**Action:** Place tower on valid non-path cell.
**Expect:** Tower placed immediately. Begins targeting and firing at enemies in range. Coins deducted.
**Verify:** Tower fires at enemies during same wave it was placed. No delay or "wait until next wave" behavior.

## H3: Tower Placement During Wave — Insufficient Coins Mid-Wave

**Setup:** Wave active. Player has 8 coins (killed 8 enemies, spent 10 on first tower).
**Action:** Check tower button during wave.
**Expect:** Button disabled. Cannot place. Button enables after 2 more kills (coins reach 10).

## H4: Path Cell Rejection

**Setup:** Pre-wave or mid-wave. Player has >= 10 coins.
**Action:** Attempt to place tower on path cell (row 10, any column).
**Expect:** Rejected. Tower not placed. Coins not deducted. Visual feedback (red flash, or click does nothing on path cells).

## H5: Occupied Cell Rejection

**Setup:** Tower already on cell (5, 5).
**Action:** Attempt to place another tower on same cell.
**Expect:** Rejected. One tower per cell. Original tower unchanged.

## H6: Simultaneous House Reach

**Setup:** Two enemies close together, both reach house on same frame.
**Action:** Observe HP deduction.
**Expect:** HP decrements by 2 (one per enemy). If HP was 2, goes to 0 and game over triggers. No skipped deduction. No double-count of same enemy.

## H7: Game Over Mid-Wave

**Setup:** Wave 4 active. Player at 1 HP. Enemy approaching house with no tower in range.
**Action:** Let enemy reach house.
**Expect:** HP becomes 0. Game over screen appears immediately. Remaining active enemies stop marching (or are hidden). Start Wave button not visible. No further HP deductions from enemies that were still on path.

## H8: Victory After Wave 5

**Setup:** All 12 enemies in wave 5 killed. HP > 0.
**Action:** Observe game state after last enemy dies.
**Expect:** Victory screen appears. No "Wave 6" or pre-wave phase. Start Wave button not shown. Return to Title button works.

## H9: Arrow Hits Intended Target — Single Hit, No Pierce

**Setup:** Tower fires at nearest enemy (enemy A). Enemy B is directly behind enemy A on same line from tower.
**Action:** Observe arrow flight and impact.
**Expect:** Arrow flies straight. Hits enemy A, deals 10 damage, enemy A destroyed. Arrow dissipates immediately on hit. Arrow does NOT continue to enemy B. Enemy B unharmed. Coin count increments by 1 (only enemy A).

## H10: Arrow Miss — Target Dead, Hits Next Enemy on Same Line

**Setup:** Tower A and Tower B both target enemy X. Tower A's arrow kills enemy X first. Tower B's arrow already in flight. Enemy Y happens to be on same straight-line trajectory behind where enemy X was.
**Action:** Observe Tower B's arrow behavior.
**Expect:** Arrow continues along original straight-line trajectory (no direction change). Arrow hits enemy Y (first enemy encountered on that line within 4-cell range). Deals 10 damage. Arrow dissipates on hit. Enemy Y destroyed. One hit total. No piercing through to additional enemies.

## H11: Arrow Miss — No Other Enemy on Path

**Setup:** Tower fires at lone enemy. Another tower kills it before arrow arrives. No other enemies in range.
**Action:** Observe arrow behavior.
**Expect:** Arrow continues straight along original trajectory. No enemy encountered. Arrow dissipates at max range (4 cells from tower). Zero hits. Zero damage dealt.

## H12: Tower Targets Nearest Enemy

**Setup:** Two enemies on path. One at distance 2 cells from tower, one at distance 3 cells.
**Action:** Observe which enemy tower targets first.
**Expect:** Tower targets the enemy at distance 2 (nearest). After that enemy dies, switch to enemy at distance 3.

## H13: Economy Balance — Full Clear

**Setup:** Player starts with 10 coins. All 36 enemies killed across 5 waves.
**Action:** Track coin balance throughout.
**Expect:** Max possible coins = 10 (start) + 36 (kills) = 46. Can afford 4 towers (40 coins) with 6 coins left. Verify no coin miscounts or overflow.

## H14: One Tower Cannot Solo

**Setup:** Player places exactly 1 tower before wave 1. Never places another tower.
**Action:** Play through all 5 waves.
**Expect:** Player survives waves 1-2 (may leak 0-2 total). Wave 3 leaks 1-2. Wave 4 leaks 2-3. HP reaches 0 during wave 4 or early wave 5. Game over before victory.
**Verify:** Proves second tower is required to win.

## H15: Title → Game → Game Over → Title Loop

**Setup:** Full cycle.
**Action:** Title screen → Start → Play until game over → Return to Title → Start again.
**Expect:** Second game starts with fresh state (3 HP, 10 coins, no towers, wave 1). No leaked state from previous game.

## H16: Title → Game → Victory → Title Loop

**Setup:** Full cycle.
**Action:** Title screen → Start → Play until victory → Return to Title → Start again.
**Expect:** Second game starts with fresh state. Victory screen shows correct final stats.

## H17: Start Wave Button During Wave

**Setup:** Wave active.
**Action:** Rapid click Start Wave button area.
**Expect:** Button disabled. No effect. Wave does not restart. No duplicate spawns.

## H18: Performance — Wave 5 Peak Load

**Setup:** 12 enemies on screen, 4 towers placed, all firing.
**Action:** Observe frame rate during wave 5.
**Expect:** No visible frame drops. All towers targeting correctly. All enemies moving at consistent speed. FPS stays at target (60).

## H19: Tower Placement at Grid Boundaries

**Setup:** Player has >= 10 coins.
**Action:** Place towers at: cell (0, 0) top-left corner, cell (0, 19) top-right, cell (19, 0) bottom-left, cell (19, 19) bottom-right. Also attempt cell (0, 10) — path cell on left edge, cell (19, 10) — path cell on right edge.
**Expect:** Corner placements succeed (non-path). Path cell placements rejected.
**Verify:** Towers at corners render fully visible, not clipped. Range calculation correct from corners.

## H20: Coin Display Updates During Wave

**Setup:** Wave active. Tower kills enemy.
**Action:** Observe coin counter when enemy dies.
**Expect:** Coin count increments immediately on kill. If count crosses 10, tower placement button transitions from disabled to enabled in same frame or next frame. No delay.
