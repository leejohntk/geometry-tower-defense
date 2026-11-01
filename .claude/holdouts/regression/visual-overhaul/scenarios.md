# Holdout Scenarios: visual-overhaul

Implementer must NOT see this file. Orchestrator runs these during verification.

Primary commands:
- `dotnet build`
- `dotnet test`
- `godot --headless --path . --script res://tools/layout_verifier.gd` (bounded via Bash timeout)

## H1 — Enemy silhouettes are structurally distinct

- Configure one enemy of each kind and inspect the draw node tree / shared silhouette
  profile.
- Assert four different silhouette signatures: Basic circle (0 polygon sides),
  Swarm triangle (3), Armored diamond (4), Progenitor octagon (8). Colors remain red /
  orange / grey / magenta.
- Static check: one shared per-kind profile drives the shape; no four independent
  hardcoded draw branches that could silently drift (a helper such as
  `SilhouetteSides` returning 0/3/4/8 is the expected shape of the solution).
- Fail if Swarm is still a circle or if Armored/Progenitor silhouettes are unchanged
  circles differentiated only by size/color.

## H2 — Idle animation is transform-based, not redraw-per-frame

- Advance a Swarm enemy ~60 frames: its visual child's rotation changes; its
  `Position`/`AnchorPosition` behavior is unchanged.
- Advance a Basic enemy: no spinning/scaling that changes simulation position.
- Static check: `Enemy._Process` does not call `QueueRedraw` for idle animation, and no
  new per-enemy `_Process` method exists for decoration.
- Fail on per-frame allocation or per-frame `QueueRedraw` while idle.

## H3 — Every damaging hit is visible; crits stay stronger

- Apply a normal hit: enemy flashes/scales briefly (a weaker cue than a crit).
- Apply a crit hit (`ApplyHitFlash`): the white crit flash is still stronger than the
  normal-hit cue.
- Apply a burn proc: its distinct hot flash remains visible and different from both.
- Stun tint, burn tint/pulse, and Progenitor phase border still work.
- Fail if normal hits are still invisible.

## H4 — Death burst lifecycle and scaling

- Kill a Basic enemy: a death burst appears at its death position, colored from the
  Basic fill, with 6 shards; it expands, fades, emits `Finished` once, and returns to
  the pool (active count back to 0).
- Kill a Progenitor: burst has 16 shards and a radius at least 2x the Basic radius.
- Kill Swarm (4 shards) and Armored (8 shards).
- Let an enemy reach the base: no death burst.
- Ten rapid kills: active burst count is bounded by the pool and drains back to zero;
  no missing release, no double release.
- Level reset while bursts are active: all active bursts detached and pooled; none
  survive.

## H5 — Arrow projectile points where it flies

- Spawn an `ArrowProjectile`, initialize it toward a target, inspect the visual
  `Polygon2D`.
- The single tip vertex must be forward along the projectile's local +X (the direction
  `Rotation` maps to travel). It must not point sideways or backward.
- Collision radius/speed/range unchanged.

## H6 — Projectile trails and pool reuse

- Arrow: after a few frames of flight, a trail with about 3 segments is visible and
  fades behind the arrow.
- Cannonball: after a few frames, about 4 smoke puffs are visible.
- On hit/dissipation the trail clears or stops drawing.
- Re-`Initialize` a pooled projectile: no trail from the previous shot survives
  (assert no stale trail points on frame 1 of the new shot).
- Trails do not change collision or hit order.
- Static check: trail updates allocate no new arrays/lists per frame.

## H7 — Turrets visibly aim and recover

- Place a tower, call `SetTarget(enemy)` with the enemy inside range, advance frames:
  the tower's public aim angle converges toward the enemy direction over multiple
  frames (not a one-frame snap).
- Move the target to the opposite side: aim turns toward it over time.
- `SetTarget(null)` or kill the target: aim eases back toward its default orientation
  and no firing pulse remains.
- Simulation position, range, and cooldown are unchanged.
- Fail if the aim value snaps instantly every frame with no smoothing, or if aim points
  at a dead/out-of-range enemy.

## H8 — Cannon recoil on every shot; cluster flash contract preserved

- Fire a single-shell cannon: a brief barrel recoil/kick is visible,
  `IsMuzzleFlashing` remains false (existing contract).
- Fire a cluster volley (ClusterCount > 1): the larger muzzle flash still sets
  `IsMuzzleFlashing` true and expires on the existing duration bounds.
- Existing `TowerTest` muzzle-flash tests pass unchanged.

## H9 — Laser impact glow follows the beam

- With an active laser target, an impact ring is visible at the beam endpoint
  (local position = target - tower position), and smaller rings at chain endpoints when
  chain jumps exist.
- Kill/clear the target: beam hides and the ring hides (no stranded glow).
- Change the chain target set: rings follow the new endpoints; extra rings disappear.
- Beam width/color ramp behavior unchanged.

## H10 — Existing effect contracts intact

- `ExplosionEffect.RadiusAt` / `AlphaAt` monotonicity and exact endpoint tests pass;
  duration and `maxRadius` semantics unchanged.
- Pierce spark radius/duration still smaller than cannon explosion.
- The explosion's new inner ring/spokes do not change its radius or alpha curves.

## H11 — Spawn/base old visuals removed and replaced

- Start Level 1. No `Label` in the gameplay scene has text `SPAWN` or `BASE`.
- No red spawn-cell `ColorRect` alpha 0.15 treatment remains; no orange house body or
  roof polygon remains.
- `SpawnPortal_{col}_{row}` exists for every `level.SpawnCells` entry, centered on the
  cell, with a radial (arc/ring) draw and rotation/pulse.
- `BaseCore` exists at `level.BaseCell` with a core + shield ring.
- All nodes in both groups have `MouseFilter = Ignore`; they render below enemies and
  towers; they do not block placement clicks or targeting.
- Multi-spawn level (Level 4): portal count equals spawn-cell count.
- Restart/switch level: markers match the new level's spawn/base cells, with no
  duplicates from the prior level.

## H12 — Placement menu cards

- `GameHUD/Block_Sidebar/PlaceArrow`, `PlaceCannon`, `PlaceLaser` still resolve as
  `Button` and still emit the same `PlaceTowerPressed` values.
- Each card has children `Icon` (Control, procedural draw, no `TextureRect`), `Name`
  (Label, tower name), and `Cost` (Label containing the cost).
- Arrow/Cannon/Laser cards stack without overlapping; `StartWave` is below them and
  also non-overlapping; all inside the sidebar and viewport at 1460×940.
- Card disabled when unaffordable or disallowed; enabled when affordable; visibility
  rules for Cannon/Laser per level unchanged.
- Pressing a card enters placement mode; clicking a card while placing cancels and is
  consumed (existing `MainTest` behavior).
- Fail if any card depends on an external image or lacks an icon.

## H13 — Skill descriptions complete and accurate

- All 15 catalog nodes have non-empty descriptions; no duplicate boilerplate-only text.
- Numbers match the real constants: Arrow damage +2/rank; attack speed +10%/rank;
  range +0.5 cell/rank; pierce +1 enemy/rank; crit +8%/rank; Cannon powder +1 damage
  and +10% projectile speed; cannon attack speed +10%; splash +8 px; cluster +1
  shell/rank; stun +6%/rank; Laser dps +0.8/rank; laser range +0.5 cell/rank; ignite
  +5%/rank per second with the existing burn; chain +1 jump/rank; ramp +20% max
  dps/rank.
- All six seed nodes mention their archetype (Pierce/Crit/Cluster/Stun/Chain/Ramp-Up)
  and the rank-3 specialization relationship.
- Fail if any description promises behavior the code does not have.

## H14 — Hover band behavior

- Idle: `Block_SkillInfo/Description` shows the hover hint; `Name`/`Rank` are empty or
  neutral.
- Emit `MouseEntered` on `TreeArea/Row_arrow_damage`: `Name` = "Damage",
  `Rank` = "0/5" (with a zeroed state), `Description` non-empty and matches catalog.
- Hover a locked row (`Row_arrow_attack_speed` with zero ranks, no Buy child): info
  still shows, and the description names the prerequisite "Damage".
- Hover a row's `Buy` button: same row info appears (button hover is covered).
- Emit `MouseExited`: band returns to the idle hint.
- Switch tower tab: band resets to idle; hovering rows on the new tower works.
- Buy a rank: after rebuild, band is idle; hovering the rebuilt row shows the updated
  rank.
- `Block_SkillInfo` sits inside `Panel_Main`, below all tree rows at startup; all its
  nodes have `MouseFilter = Ignore`; clicking Buy still works while hovering.
- Fail if hover requires per-frame mouse polling that tests cannot drive, or if any row
  (including locked rows) cannot show info.

## H15 — Layout verifier extended and green

- `tools/layout_verifier.gd` contains assertions for:
  - each placement card's icon/name/cost inside the card and cards not overlapping each
    other or StartWave;
  - `Block_SkillInfo` inside `Panel_Main` and below the tree rows, spanning the tree
    width.
- Verifier exits 0 with zero findings and evaluated assertions > previous baseline.
- Fail-closed exit 2 is a failure.

## H16 — No gameplay regression

- Full `dotnet test` suite passes (all pre-existing gameplay tests unchanged or
  legitimately extended for presentation-only paths).
- Spot-check stats: Arrow 10 dmg / 4 cells / 1.5 s; Cannon 15 dmg / 4 cells / 2.5 s;
  Laser 4 dps / 3 cells; enemy HP/speed/armor; coin/XP values; projectile speed/size.
- Wave composition, spawn timing, targeting, and collision unchanged.
- `MainTest` placement input flows pass.

## H17 — Asset and procedural-draw policy

- `git diff --stat` shows no new `.png`, `.jpg`, `.svg`, `.webp`, `.wav`, `.ogg`,
  `.mp3`, `.ttf`, `.otf`, or imported asset files.
- No `TextureRect`, `Sprite2D`, or `AnimatedSprite2D` added for the new visuals.
- All new visuals are `Draw*` calls/`Polygon2D`/`Line2D`/transforms.
- No AI-generated art/audio anywhere.

## H18 — Performance and stability smoke

- Headless run with a busy scenario (many enemies, lasers, explosions, bursts) for
  ~300 frames: no errors, no orphaned effect nodes, no growth in active effect lists
  after effects finish.
- Static check: no `new` arrays/lists/LINQ in `_Process`/`_Draw`/per-hit paths of the
  changed files.
- Idle enemies/towers do not repaint every frame for decoration (H2).
- Frame pacing is not measured headlessly; playtest covers subjective smoothness.

## H19 — Input and UI non-interference

- New world visuals never consume clicks: placement preview still follows the mouse,
  clicks place/cancel towers, empty-cell clicks deselect, tower clicks select.
- New skill info band never consumes clicks: Buy buttons, sidebar tabs, and Back still
  work while hovering.
- New placement cards preserve the sidebar click-consumption path in `MainTest`.

## H20 — Level reset hygiene

- Reset or switch levels while effects are active: all active explosions, pierce sparks,
  death bursts, trails, and impact nodes are gone; pools contain no visible/stale nodes;
  no references to freed towers/enemies remain in effect state.
- A pooled enemy reused after a visual-heavy level shows no leftover flash, tint, spin,
  burst, or trail.

## H21 — Title screen visuals

- `TitleScreen/Block_Backdrop` exists, spans the viewport, has `MouseFilter = Ignore`,
  and draws procedurally (no TextureRect/Sprite2D/external file).
- The backdrop animates while the title is visible (transform rotation/pulse) and stops
  processing when hidden; no per-frame `QueueRedraw` of the full backdrop.
- `Block_Title/Title` and `Subtitle` keep their text, size, and positions; the layout
  verifier passes its existing title assertions.
- Level buttons `Level1`–`Level5` and `SkillTree` keep exact names, rects, and signals.
  They have a custom normal/hover/pressed/focus style; decorative children are
  `MouseFilter = Ignore`.
- Pressing Level 1 and Skill Tree still routes exactly as before (MainTest path).
- Instructions block still present at the same rect and readable.
- Returning to the title after a game: backdrop resumes without errors; no duplicated
  backdrop nodes.

## H22 — Victory / defeat screen visuals

- Both `GameOverScreen` and `VictoryScreen` have a `Block_Backdrop` with
  `MouseFilter = Ignore`; the two backdrops use different shape language (defeat:
  falling/shattering shards or fractured rings; victory: expanding rings/rising
  starburst).
- Backdrop animation runs while visible and stops when hidden; it never covers or
  captures the `ReturnToTitle` button.
- `Overlay`, `Block_Title/Title`, `Block_Title/Subtitle`, and `ReturnToTitle` keep their
  exact paths; title/subtitle text unchanged; layout verifier result assertions pass.
- `ReturnToTitle` keeps its rect and signal; a first-frame click works.
- Trigger a loss: game-over screen shows defeat visuals, not victory visuals. Trigger a
  win: victory visuals, not defeat visuals. Trigger the other outcome afterward: the
  screen restyles for the new outcome with no leaked shapes/colors.
- Victory/defeat flow unchanged: upgrade panel closed, correct screen visible, Return
  goes to title.

## H23 — Skill-tree screen styling

- `Panel_Main/Block_TreeBackdrop` (or equivalent named group inside `Panel_Main`) exists
  behind `TreeArea`, spans the panel, has `MouseFilter = Ignore`, draws procedurally,
  and does not repaint per frame.
- Lines show a glow layer plus a bright core; unlocked lines use the selected tower's
  color, locked lines stay grey. Switching towers recolors the tree.
- Every row still resolves `Icon` (22×22), `Name`, `Rank`, and (when unlocked) `Buy` at
  the same relative positions. The layout verifier's row containment, mirror symmetry,
  spine centering, and seed Buy assertions all pass unchanged.
- Node icons are no longer identical generic circles: trunk glyphs follow the required
  mapping (blade / speed lines / crosshair / shell+spark / concentric rings / beam /
  flame) and differ from each other within a tower; seed glyphs match the archetype
  emblem. Halos: tower color unlocked, grey locked, gold at rank 5. Locked rows show a
  locked/slashed treatment.
- Hovering the icon center of a row (real mouse motion, not just the row signal)
  updates the bottom description band to that node; the icon does not swallow Buy
  clicks or row hover exit/enter.
- Hovering a row shows a highlight band/edge glow and brighter icon; leaving clears it;
  tab switch/rebuild clears it; the highlight never moves Icon/Name/Rank/Buy.
- Buy buttons are styled (normal/hover/pressed/disabled/MAX) and still emit the purchase;
  SP text stays exactly `Skill Points: N`; sidebar labels and rects unchanged.
- Header `Title` stays centered with the same text format; sidebar buttons keep labels,
  rects, gaps, and signals; all new decorations have `MouseFilter = Ignore`.
- Unlocked halo pulse runs only while the screen is visible and uses modulate/scale, not
  per-frame redraw.
- Skill-tree hover band from H14 still works after the restyle.
