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

- **2D game.** Top-down or side-view perspective using Godot's Node2D. All gameplay on a single 2D plane.
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

- **Simple geometric shapes only.** No pixel art, no organic shapes, no detailed illustrations.
- **Towers vs enemies visually distinct.** Towers use filled shapes with clean borders. Enemies use different shapes, sizes, or border styles so they are immediately distinguishable at a glance.
- **Color coding.** Enemy types have distinct colors. Tower types have distinct colors.
- **Clean UI.** Geometric HUD elements. Sans-serif fonts.
- **2D plane.** All visuals flat on a single 2D plane. No 3D, no isometric perspective.

## Asset Policy

- **No AI/LLM-generated assets.** Art, sprites, textures, sound effects, and music must not be created by generative AI models trained on artists' work without consent.
- **Ethical sourcing only.** Assets must come from:
  - Open source repositories (licensed MIT, CC0, public domain, GPL-compatible)
  - Free game asset sites with clear permissive licensing (e.g., OpenGameArt.org with CC0 or CC-BY)
  - Hand-authored by the development team using Godot's built-in drawing tools or simple geometric shapes
- **Geometric shapes as default.** Since the theme is geometric, most visual assets can be created procedurally in Godot using built-in nodes (ColorRect, Circle, Polygon2D, Line2D). No external sprite files needed for core gameplay elements.
- **Sound.** Same policy as art. Free, openly licensed sound effects (CC0 preferred). Sources like freesound.org with CC0 license, or procedurally generated using Godot's AudioStreamGenerator.
