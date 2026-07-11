# Feature Spec: First Level — Basic Map, Enemy, and Tower

**Status:** draft
**Branch:** feature/first-level
**Date:** 2026-07-08

## Description

First playable level of Geometry Tower Defense. Player defends a house on the right side of a 20×20 grid from circle enemies that march along a straight horizontal path from left to right. Player places Arrow Towers (geometric triangle design) on the grid at any time — pre-wave or mid-wave — as long as they have the coins. Player clicks "Start Wave" to begin each wave. Each enemy killed drops 1 coin. Player starts with 10 coins (enough for 1 tower). Survive 5 waves to win. If 3 enemies reach the house, game over. One tower cannot solo all 5 waves — player must earn coins and place additional towers to survive.

## Acceptance Criteria

- [ ] Title screen displays "Geometry Tower Defense" with a Start button that loads the game level
- [ ] 20×20 grid renders on screen with a horizontal path from left edge to right edge
- [ ] Left side shows enemy spawn zone; right side shows house/base indicator
- [ ] Player can place Arrow Towers on non-path grid cells at any time (pre-wave and mid-wave): cost 10 coins
- [ ] Tower placement button disabled when player has fewer than 10 coins. Re-enables when coins >= 10
- [ ] Tower placement rejected if: cell is on path, cell already occupied
- [ ] "Start Wave" button begins enemy spawn for current wave. Button disabled during active wave. Re-enables when wave ends
- [ ] Enemies (circles, 10 HP) follow path left to right at consistent speed (2 cells/sec)
- [ ] Arrow Towers auto-target nearest enemy within 4-cell range, fire once per 1.5s, deal 10 damage
- [ ] Projectile behavior: Arrow flies in straight line toward target. First enemy hit takes 10 damage and arrow dissipates. If intended target dies before arrow arrives, arrow continues along same straight-line trajectory — will hit first enemy encountered on that line within max range (4 cells). If no enemy on path, dissipates at max range. Arrow never changes direction. One hit per arrow. No piercing (does not pass through enemy to hit another).
- [ ] Enemy reaching house: player loses 1 HP, enemy removed. Player starts with 3 HP
- [ ] Enemy destroyed: +1 coin to player
- [ ] Game over screen when HP reaches 0
- [ ] Victory screen when all 5 waves cleared
- [ ] HUD shows: current HP, coin count, wave number (e.g., "Wave 1/5"), tower placement button (disabled when < 10 coins), Start Wave button (disabled during active wave)
- [ ] 5 waves with increasing enemy counts (3, 5, 7, 9, 12 enemies per wave respectively)
- [ ] Between waves: pre-wave phase resumes, Start Wave button re-enables. Towers persist between waves

## Edge Cases

- **Insufficient coins for tower:** Tower placement button disabled. Player cannot click it. Re-enables once coins reach 10 (e.g., after kills during wave)
- **Placing tower on path cell:** Rejected. Tower only placeable on non-path grid cells
- **Placing tower on occupied cell:** Rejected. One tower per cell
- **Enemy killed mid-cell:** Enemy removed immediately. Projectiles already targeting that enemy continue straight and may hit next enemy on same line, or dissipate at max range
- **Tower placed mid-wave:** Immediate. Tower begins targeting and firing as soon as placed, even during active wave
- **Last enemy of wave killed:** Wave complete. Pre-wave phase begins immediately. Start Wave button re-enables
- **Player HP = 1, two enemies reach house simultaneously:** HP goes to -1. Game over triggers on <= 0
- **Start Wave clicked during active wave:** Button disabled. No effect
- **All 5 waves cleared with 0 HP:** Impossible (game over at 0 HP prevents reaching wave 5). But defense: victory check requires HP > 0
- **Tower placed on last cell before house:** Enemies pass through most of range before being targeted. Tactically suboptimal but allowed

## Dependencies

- Godot 4.7 with C# / .NET 8 (already set up)
- GdUnit4Net for testing (already in .csproj)
- No external assets — all visuals are procedural geometric shapes using Godot drawing nodes
- No other features — this is the foundational feature

## Constraints

- **Performance:** Must handle 12 enemies + 5 towers + 5 projectiles simultaneously without frame drops
- **Grid:** 20×20 cells. Each cell is 64×64 pixels (1280×1280 play area)
- **Path:** Single row. Row index 10 (middle row, 0-indexed), columns 0 through 19 (left to right)
- **Platform:** Desktop (macOS first). Keyboard + mouse input.
- **No AI-generated assets.** All visuals use Godot built-in drawing nodes (ColorRect, Polygon2D, Line2D, etc.)

## Visual / Geometry Theme

- **Grid:** Thin gray lines on dark background
- **Path:** Light brown/tan cells along row 10
- **House (base):** Simple geometric house shape — square base + triangle roof, red/orange
- **Enemy (circle):** Red circle with dark border. 48px diameter within 64px cell
- **Arrow Tower:** Upward-pointing triangle (isosceles), blue/cyan fill, dark border. Fits within 64px cell
- **Projectile:** Small yellow triangle/arrow shape, flies in straight line
- **HUD:** White sans-serif text. Geometric icon indicators (heart shapes for HP, circle for coins)
- **Title screen:** Dark background, centered title text, geometric Start button (rectangle with hover highlight)
- **Game over:** Red overlay with "GAME OVER" text and "Return to Title" button
- **Victory:** Gold overlay with "VICTORY" text and "Return to Title" button

## Wave Table

| Wave | Enemy Count | Spawn Interval | Notes |
|------|------------|----------------|-------|
| 1 | 3 | 0.8s | Tutorial wave. 1 tower clears easily |
| 2 | 5 | 0.8s | 1 tower may leak 0-1 |
| 3 | 7 | 0.8s | 1 tower leaks 1-2. Second tower needed |
| 4 | 9 | 0.8s | 2 towers needed to clear |
| 5 | 12 | 0.8s | Final wave. 2-3 towers needed |

Enemies spawn one at a time with 0.8 second interval between spawns within a wave.

## Tuning Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Grid cell size | 64×64 px | 1280×1280 play area |
| Path row | Row 10 (middle) | Columns 0-19 |
| Enemy speed | 2 cells/sec | Crosses grid in ~10 seconds |
| Enemy HP | 10 | One-shot by Arrow Tower |
| Arrow Tower range | 4 cells | 256px radius |
| Arrow Tower fire rate | 1.5s | 0.67 shots/sec |
| Arrow Tower damage | 10 | One-shots basic enemy |
| Arrow Tower cost | 10 coins | Starting budget = 1 tower |
| Projectile speed | 8 cells/sec | Fast visual, near-instant at close range |
| Starting coins | 10 | |
| Starting HP | 3 | |
| Coin per kill | 1 | |

## Out of Scope

- Skill tree / XP / meta-progression
- Tower upgrades or selling
- Additional tower types (Splash, Slow, Sniper, Buff)
- Additional enemy types (Fast, Tank, Swarm, Boss)
- Additional levels
- Sound effects / music
- Pause menu / settings
- Tower targeting priority selection
- Selling towers
- Fast-forward button
