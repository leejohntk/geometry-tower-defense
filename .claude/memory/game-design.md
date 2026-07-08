---
name: game-design
description: Core game mechanics, tower types, enemy types, wave system for the geometry-themed tower defense
metadata:
  type: project
---

# Game Design

## Theme

Geometry-themed tower defense. All towers, enemies, and projectiles rendered as geometric shapes with clean visual identity.

## Core Mechanics

- **Grid-based placement.** Towers placed on a grid. Enemies follow predefined paths.
- **Wave system.** Enemies spawn in waves. Increasing difficulty. Boss enemies at wave milestones.
- **Resources.** Currency earned from kills. Spent on tower placement and upgrades.
- **Lives.** Enemies that reach the end reduce lives. Game over at zero.

## Planned Tower Types

| Tower | Shape | Behavior | Damage Type |
|-------|-------|----------|-------------|
| Piercing | Triangle (arrow) | Shoots through enemies in a line | Single target, pierces N enemies |
| Splash | Circle | Area damage on impact | AOE around impact point |
| Slow | Hexagon | Slows enemies in range | Low damage, movement speed debuff |
| Sniper | Thin rectangle | Long range, high damage, slow fire rate | Single target |
| Buff | Diamond | Boosts nearby towers | No damage, stat buff aura |

## Planned Enemy Types

| Enemy | Shape | Behavior | Traits |
|-------|-------|----------|--------|
| Basic | Small square | Walks path, low HP | Standard |
| Fast | Small triangle | Moves quickly, low HP | Speed |
| Tank | Large square | Slow, high HP | Durability |
| Swarm | Tiny circles | Many at once, very low HP each | Numbers |
| Boss | Large complex polygon | High HP, special abilities | Milestone enemy |

## Wave Structure

- Waves increase in enemy count and type variety.
- Every 5th wave: boss wave.
- Between waves: brief preparation period.
- Economy scales with wave number.

## Geometry Theme Rules

- **No pixel art.** Clean vector-like geometric shapes.
- **No organic shapes.** Pure geometry (circles, triangles, squares, hexagons, diamonds).
- **Color coding.** Enemy types have distinct colors. Tower types have distinct colors.
- **Clean UI.** Geometric HUD elements. Sans-serif fonts.
