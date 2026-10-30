# Feature Spec: visual-overhaul

**Status:** approved (2026-10-30)
**Branch:** feature/visual-overhaul
**Date:** 2026-10-30

## Description

Raise the production value of the whole game while keeping the geometric theme and the
existing gameplay exactly as tuned. Five workstreams, one PR:

1. **Combat visuals** — enemies, towers, projectiles, and effects become animated and
   readable: distinct silhouettes per enemy kind, turrets that visibly aim, projectile
   trails, muzzle/recoil feedback, impact and death bursts, and a laser impact glow.
2. **Spawn and base** — delete the flat red spawn cell, the orange "house", and the
   `SPAWN` / `BASE` text labels. Replace them with subtle, integrated geometric markers:
   a rotating spawn portal and a pulsing base core.
3. **Tower placement menu** — replace the three text buttons in the HUD sidebar with
   icon cards: a procedural geometric glyph matching each tower's in-world silhouette,
   plus name and cost.
4. **Skill-tree visuals and hover descriptions** — make the tree itself look like a
   designed screen: backdrop lattice, glowing branch lines, distinct node glyphs and
   halos, styled row highlights and Buy buttons, richer tower sidebar, header emblem,
   and SP counter. Add a fixed description band at the bottom of the screen.
   Hovering any node row shows that node's name, current rank, and what it does
   (including its archetype role when it is a seed node).
5. **Title and result screens** — turn the flat title screen into an animated geometric
   composition with styled menu buttons, and give the victory/defeat overlays distinct
   geometric celebrations/shatters, title treatments, and styled return buttons.

Everything is hand-authored procedural geometry (`Draw*` calls / transforms). No image,
sprite, texture, sound, or music assets are added. No AI-generated assets. No `.tscn`
hand-editing: scene changes go through the Godot MCP tools or stay in code-built UI.

**No gameplay changes.** Damage, range, fire rate, speed, HP, armor, collision, spawn
timing, wave composition, coin/XP values, and all input semantics remain byte-for-byte
equivalent in behavior. This is a presentation feature.

## Acceptance Criteria

### Global visual rules

- [ ] Every new visual is drawn with Godot procedural drawing (`DrawCircle`,
      `DrawColoredPolygon`, `DrawPolyline`, `DrawArc`, `Line2D`, `Polygon2D`, etc.) or is
      a transform of an existing draw. No new image/audio asset file is added anywhere.
- [ ] All new tunables live in `GameConstants` (colors, durations, widths, radii,
      speeds). Draw code contains no unexplained magic numbers where a constant is
      reused or tuned.
- [ ] Simulation and presentation stay separate. Visual rotation, scale, recoil,
      pulse, trail, or idle motion never writes to an enemy's `_anchorPosition`, a
      tower's `Position`, target selection, range checks, collision radii, or damage.
- [ ] Every new visual state is reset when a pooled object is reused (`Initialize`,
      `ResetForPool`, or `Configure`), so no trail, recoil, aim, burst, or flash leaks
      into a reissued object.
- [ ] New effects are pooled through `GameManager`, released on finish, and cleared on
      level reset. Steady-state combat allocates no per-frame garbage: no new arrays,
      lists, or LINQ inside `_Process`, `_Draw`, or per-hit paths.
- [ ] Idle animation that can be expressed as a node transform (rotation, scale,
      modulate) is animated by transforms and does **not** call `QueueRedraw` every
      frame. Redraw-per-frame is allowed only for short timed effects (bursts, flashes).
- [ ] Every visual-only node has `MouseFilter = Ignore` and never intercepts a click.
- [ ] Every new visual group is a named `Control` wrapper (`Name` set), consistent with
      the named-visual-groups rule. New world groups use stable names
      (`SpawnPortal_{col}_{row}`, `BaseCore`, `Aim`, `Trail`, etc.).
- [ ] Z-order: spawn/base markers render below enemies and towers; hit/death effects
      render above; the rank badge stays above its tower.
- [ ] All pre-existing tests (`dotnet test`) continue to pass with gameplay assertions
      unchanged. Tests may be updated only where a presentation node's path/shape was
      intentionally redesigned, and no gameplay expectation may be weakened.
- [ ] The layout verifier passes, and is extended with assertions for the new HUD cards
      and the skill-tree info band. Existing title/result/skill-tree/HUD assertions must
      all continue to pass unchanged.

### Enemy visuals

- [ ] Each kind has a distinct silhouette, not just a color/size change:
  - Basic: circle, with a thin inner ring and a gentle scale pulse.
  - Swarm: small triangle (three-sided), slowly spinning around its own center.
  - Armored: diamond (four-sided) with an inner inset border and four corner rivets.
  - Progenitor: octagon with a counter-rotating outer ring around the existing
    phase-colored border, plus the existing health bar and summon telegraph ring.
- [ ] Distinctness is driven by one shared presentation profile per kind (e.g. a
      `SilhouetteSides` helper returning 0/3/4/8 and a spin/pulse config), not by
      scattered per-kind if-statements in draw code. Basic/Swarm/Armored spin or pulse
      only when configured to do so; zero motion is valid for Basic if it has another
      idle cue.
- [ ] Enemies remain immediately recognizable by color: Basic red, Swarm orange,
      Armored grey, Progenitor magenta. Existing stun desaturation, burn tint/pulse,
      burn proc flash, crit flash, and Progenitor phase colors are preserved.
- [ ] Every damaging hit gives visible feedback. Crit keeps its strong white flash;
      normal hits get a weaker, shorter brighten/scale pop (never as strong as a crit).
      A burn proc stays visually distinct from both.
- [ ] On death, a pooled geometric death burst plays at the death position. Color is
      the enemy's fill color; size and shard count scale with kind
      (Swarm 4 shards, Basic 6, Armored 8, Progenitor 16). The boss burst radius is at
      least 2× the Basic radius. It expands (ease-out) and fades, and lasts roughly
      0.3–0.45 s. It must not play when an enemy reaches the base (that is a life
      loss, not a kill).
- [ ] Non-boss enemies show no text and no health bar.
- [ ] Enemy `_Process` movement, formation offset, path logic, and status timing are
      untouched.

### Tower visuals

- [ ] Each tower has a visible aiming element that points at `CurrentTarget` when one is
      in range. Aiming rotates smoothly (capped angular speed or damped approach), not
      instant snapping, and eases back to a default orientation when the target is lost
      or dies. Aim state resets on placement.
- [ ] `Tower` exposes the current aim angle as a read-only public value (radians) so the
      behavior is testable without reading pixels.
- [ ] Arrow: the triangular body/pointer reflects aim; firing shows a brief recoil or
      bow-flash cue. Rank-3 archetype silhouettes (forked spearhead / star inset) remain
      unchanged and recognizable.
- [ ] Cannon: a barrel element reflects aim; every shot shows a brief barrel recoil.
      Cluster volleys keep the existing larger `MuzzleFlash` and its
      `IsMuzzleFlashing` contract (a single shell still does not set it). Rank-3
      archetype silhouettes remain unchanged.
- [ ] Laser: the emitter core/pointer reflects aim; while the beam is active the emitter
      shows a subtle firing pulse. Beam width/color/ramp behavior and chain beam geometry
      are unchanged.
- [ ] Existing range indicator, selection behavior, upgrade panel, and rank badge are
      unaffected. New tower visuals do not cover the top-right rank badge.
- [ ] Tower `_Process` cooldown behavior and all stat properties are unchanged.

### Projectiles

- [ ] Arrow projectile geometry points along travel: with `Rotation` set to the velocity
      angle, the triangle tip is the forward-most vertex (+X local). The current
      sideways-facing arrow is fixed.
- [ ] Arrow leaves a short fading trail (roughly 3 segments over ~0.1–0.15 s) in its
      yellow color. Trail fades/clears on hit, dissipation, and pool reuse.
- [ ] Cannonball leaves a short fading smoke trail (roughly 4 puffs) in a dark
      green/grey. Trail fades/clears on explosion, dissipation, and pool reuse.
- [ ] Trails are visuals only: they do not collide, do not alter `ProjectileSize`,
      collision radius, speed, range, damage, or hit order.
- [ ] Trails are drawn from pooled/preallocated state; no per-frame allocation.

### Effects

- [ ] Cannon explosion keeps its exact damage-radius contract: `RadiusAt` still reaches
      `maxRadius` at progress 0.5 and holds; `AlphaAt` still holds opaque through 0.5
      then fades; duration unchanged. It gains an inner shockwave ring and six radial
      debris spokes for a more dynamic read. Existing effect tests continue to pass.
- [ ] Pierce spark keeps its existing radius/duration/alpha contract and remains visibly
      smaller than the cannon explosion.
- [ ] New `DeathBurstEffect` is pooled, resets on reuse, emits `Finished` once, and is
      removed from the active list and pool-released exactly once.
- [ ] Laser impact: while a laser beam is active, a small glowing ring sits at the beam's
      endpoint (and a smaller one at each chain endpoint). It follows the target, fades
      with the ramp, and disappears when the beam does. It never draws when idle.
- [ ] Explosions, sparks, bursts, and impact glows are cleared from `GameManager` on
      level reset; no effect node survives into the next level.

### Spawn and base

- [ ] The old spawn treatment is removed: no red spawn-cell `ColorRect`, no `SPAWN`
      label. The old base treatment is removed: no orange house body, no roof polygon,
      no `BASE` label.
- [ ] Each spawn cell gets a named `SpawnPortal_{col}_{row}` control: concentric arcs or
      rings plus a small core, in a dark red/orange palette, rotating/pulsing subtly.
      It sits at the spawn cell center, renders below enemies, and has
      `MouseFilter = Ignore`. One portal per `level.SpawnCells` entry.
- [ ] The base cell gets a named `BaseCore` control: a gold/white geometric core
      (hexagon/circle) with one shield ring, pulsing gently. It sits at `BaseCell`,
      renders below enemies, and has `MouseFilter = Ignore`.
- [ ] Both markers are readable at 1× scale but clearly background elements: fill alpha
      stays low (roughly 0.15–0.35) and strokes stay under full opacity. They must not
      obscure the path, enemies, towers, placement preview, or selection.
- [ ] Portal/base nodes are presentation only. They do not block placement, pathing,
      clicks, or targeting. `MouseFilter = Ignore` everywhere in both groups.
- [ ] Level changes rebuild both markers from the new level definition (they live in
      `GridManager`, which is recreated per level).
- [ ] No `Label` in the gameplay scene contains the text `SPAWN` or `BASE`.

### Tower placement menu

- [ ] The three placement controls remain `Button` nodes at the exact existing paths
      `GameHUD/Block_Sidebar/PlaceArrow`, `PlaceCannon`, `PlaceLaser`, and keep
      emitting `PlaceTowerPressed(int)` with the same tower types. `StartWave` keeps its
      path, name, and behavior.
- [ ] Each placement button becomes an icon card (about 160×72) containing:
      - `Icon` — a procedural geometric glyph matching the tower's in-world shape
        (Arrow = triangle, Cannon = circle with barrel, Laser = diamond/emitter),
      - `Name` — the tower name,
      - `Cost` — the coin cost (e.g. `15$`).
      The button's own `Text` is empty; the child labels carry the text.
- [ ] Cards are stacked vertically in the sidebar (Arrow, Cannon, Laser), with a visible
      gap, followed by `StartWave`. No card or the StartWave button overlaps another,
      the top bar, or leaves the sidebar/viewport at 1460×940.
- [ ] Level visibility rules are unchanged: Cannon/Laser cards stay hidden on levels
      that do not allow them.
- [ ] Disabled/affordable behavior is unchanged: a card is disabled when the tower is
      not allowed or coins are below its cost; disabled cards visibly dim their icon and
      labels while remaining readable. Re-enabling restores full brightness.
- [ ] Pressing a card enters placement mode exactly as the old button did, including
      cancel-on-second-press and the sidebar-click-consumption behavior covered by
      `MainTest`.
- [ ] Icons are procedural draws, not `TextureRect` and not external assets.

### Skill-tree visuals

- [ ] A named `Block_TreeBackdrop` sits inside `Panel_Main`, behind `TreeArea`, spans the
      panel, and has `MouseFilter = Ignore`. It draws a faint geometric lattice/dot
      field plus a soft tower-colored glow along the trunk spine. It is static or uses
      at most a cheap modulate/transform pulse; it never repaints every frame.
- [ ] Branch lines get two layers: a wide translucent glow underlay and a thin bright
      core. Lines into unlocked nodes use the selected tower's color; lines into locked
      nodes stay grey. The trunk spine and seed branches are visually continuous.
- [ ] Each node icon keeps its exact 22×22 `Icon` control, anchor position, and row
      containment (the layout verifier measures these), but gains a layered look:
      - an outer halo ring: tower color when unlocked, grey when locked, gold at rank 5,
      - a distinct glyph inside the node instead of one generic circle/diamond:
        trunk nodes get a stat glyph, and seed nodes reuse the existing
        `ArchetypeEmblem` marks. Required trunk glyph mapping:
        Arrow Damage = upward blade/triangle; Attack Speed = three speed lines;
        Range = crosshair (ring + ticks); Cannon Powder Charge = filled shell + spark;
        Cannon Attack Speed = three speed lines; Splash Radius = concentric rings;
        Laser DPS = horizontal beam with endpoints; Laser Range = beam with end caps;
        Laser Ignite = rising flame/wedge. Glyphs must be distinguishable within a
        tower's tree (seed and trunk glyphs never look identical),
      - locked nodes render the glyph dimmed with a slash/lock mark; unlocked nodes are
        bright; rank-5 nodes get a gold accent.
- [ ] Hovering the node icon itself (not only its row) updates the bottom description
      band. The row wrapper or an icon-level hover surface may own the signal, but the
      hit area includes the icon center, and the icon's `MouseFilter` must not swallow
      the Buy button or row click behavior.
- [ ] Unlocked node halos may pulse gently via `Modulate` or scale only, and only while
      the screen is visible. No per-node `QueueRedraw` every frame.
- [ ] Hovering a row highlights that row with a subtle background band/edge glow and
      brightens the icon; the highlight clears on mouse exit, tree rebuild, and tab
      switch. The highlight is a decoration only and never shifts `Icon`, `Name`,
      `Rank`, or `Buy`.
- [ ] Buy buttons are styled with the tower color: rounded/beveled fill, colored border,
      readable text, hover/pressed/focus states, and a clear disabled style. `MAX` gets
      a gold treatment. Button rects, names, text format (`Buy (10 SP)` / `MAX`), and
      pressed behavior stay unchanged.
- [ ] The tower sidebar keeps its exact panel/button rects, gaps, labels, and signals,
      but each selector gets a richer tower glyph, a selected-state glow/accent, and a
      dimmed unselected state. Icons remain procedural.
- [ ] The tree header keeps its centered `Title` label and the same text format, and
      gains a tower emblem beside it (not covering the label) plus color-coded
      specialization names. The header block rect and centered-label assertion must hold.
- [ ] The `Skill Points:` readout keeps its exact text format and centered label but
      gains a styled counter badge (geometric frame/background, small gem/diamond
      accent).
- [ ] All skill-tree decorations have `MouseFilter = Ignore`; sidebar buttons, Buy
      buttons, Back, and row hover signals keep working exactly as specified below.
- [ ] The layout verifier's existing skill-tree assertions (centering, sidebar rects,
      row containment, Buy presence, purchase/rebuild path, seed layout) all pass
      unchanged.

### Skill-tree hover descriptions

- [ ] Every one of the 15 `SkillTreeCatalog` nodes gains a non-empty, player-facing
      description that matches its real mechanics and the tuning constants. Descriptions
      state the per-rank effect with concrete numbers.
- [ ] Seed-node descriptions say they are archetype seeds (e.g. "Archetype: Pierce")
      and explain the rank-3 specialization relationship.
- [ ] A fixed info band sits at the bottom of the skill-tree screen, inside `Panel_Main`
      and below the tree rows. Named group:
      `Block_SkillInfo` (wrapper) with children `Panel`, `Name`, `Rank`, `Description`.
      Its wrapper spans the tree area's width; nothing in it may overlap a node row.
- [ ] Idle state (nothing hovered): the band shows a neutral hint such as
      "Hover a skill to see its description."
- [ ] Hovering a node row (its icon, name, rank, or its Buy button) shows:
      - `Name`: the node's display name,
      - `Rank`: current rank as `r/5`,
      - `Description`: the node's description.
      Hovering a locked row also shows the prerequisite that must be ranked up first
      (e.g. "Unlocks after Damage reaches rank 1"). A disabled Buy button still shows
      its row's info.
- [ ] Moving the pointer off all rows returns the band to the idle hint.
- [ ] Hover works through the real `MouseEntered` / `MouseExited` signals on the row
      wrappers (or a row-level child that emits them), so it is testable by emitting the
      signal and does not depend on pixel-level mouse routing.
- [ ] Switching tower tabs, buying a rank, or rebuilding the tree resets the band to
      the idle hint; the newly built rows hover correctly.
- [ ] The band never intercepts clicks (`MouseFilter = Ignore` on all its nodes) and
      does not change Buy behavior.
- [ ] Text wraps and fits within the band at 1460×940; no clipped lines, no horizontal
      overflow.

### Title screen

- [ ] A named `Block_Backdrop` control sits behind the title and menu content, spans the
      full viewport, and has `MouseFilter = Ignore`. It draws a layered geometric
      composition (for example: large faint triangle/hexagon rings, a drifting dot or
      line field, and a soft glow behind the title). No external image, no shader file.
- [ ] The backdrop animates slowly (rotation/pulse/drift) using node transforms and a
      single `_Process` on `TitleScreen`. It does not repaint the procedural drawing
      every frame, and processing stops while the screen is hidden.
- [ ] The title block gains a geometric decoration group (angular brackets, underline,
      or flanking shapes) that frames the existing `Title` and `Subtitle` labels without
      moving or resizing them. The layout verifier's title assertions must still pass.
- [ ] Menu buttons keep their exact node names (`Level1`–`Level5`, `SkillTree`), sizes,
      positions, gaps, and signal behavior. They gain a cohesive geometric button style:
      dark fill, a tower-colored accent bar/glyph, a hover state that brightens, a
      pressed state that visibly depresses/shifts, and a focus outline. Text remains
      readable and the labels are unchanged.
- [ ] Each menu button keeps a single mouse target; decorative children have
      `MouseFilter = Ignore` and never steal presses.
- [ ] The instructions block keeps its position/text and remains readable over the
      backdrop.
- [ ] No title-screen gameplay or signal behavior changes (`LevelSelected`,
      `SkillTreePressed`).

### Victory and defeat screens

- [ ] Each `ResultScreen` gains a named `Block_Backdrop` behind its title/button, with
      `MouseFilter = Ignore`, drawing a distinct geometric composition:
      - Victory: expanding gold/white rings and a rising starburst or upward shards.
      - Defeat: red/orange shattering shards, fractured rings, or a collapsing diamond.
      The two must be distinguishable at a glance by shape language as well as color.
- [ ] The backdrop animation runs only while the screen is visible, uses transform or
      short timed redraws, and settles instead of looping forever at full cost.
- [ ] `Overlay` keeps its node name and current base color, but may gain a subtle
      vignette/edge darkening so the backdrop and text read clearly. It must not become
      fully opaque; the battlefield stays faintly visible.
- [ ] `Block_Title/Title` and `Block_Title/Subtitle` keep their node paths and text
      values. The title gains a geometric treatment (underline/accent frame/shadow) in
      the result color. Existing layout verifier assertions for result title centering
      and containment must pass.
- [ ] `ReturnToTitle` keeps its node path, size, position, and signal. It gets the same
      geometric button styling as the title menu, including hover/pressed/focus states.
- [ ] Backdrop decoration never covers or intercepts the return button, and the screen
      still appears instantly enough that the player can click Return immediately.
- [ ] Game-over/victory flow is unchanged: the panel closes, the correct screen becomes
      visible, and Return goes back to the title exactly as today.
- [ ] The parameterized constructor remains source-compatible with both `Main` call
      sites (an optional mode flag may be added), so existing tests that resolve the
      screens by node name still compile and pass.

## Edge Cases

- **Pooled enemy killed as a different kind:** no stale silhouette, spin, trail, burst
  color, or hit flash leaks into reuse. `ResetForPool`/`Configure` reset every new visual
  field.
- **Enemy dies during a summon windup:** death burst plays; the telegraph ring is
  cleared as today; no portal/base marker is affected.
- **Enemy reaches the base:** no death burst. Only a kill produces one.
- **Simultaneous hits on one frame:** hit flash and death burst trigger once per life;
  no duplicate bursts from overkill or two projectiles landing together.
- **Burst cap / many kills:** pooling bounds active death bursts; a large wave cannot
  create unbounded nodes. Effects finish and return to the pool.
- **Laser target dies mid-frame:** impact ring hides with the beam; no ring stranded at
  an old position.
- **Laser chain endpoints change:** impact rings follow the current chain, never a stale
  target; extra rings hide when the chain shrinks.
- **Projectile pierces multiple enemies:** trail continues cleanly; pierce spark still
  plays per pass-through; trail does not duplicate on each hit.
- **Cannon explosion at max range / no hit:** explosion visual still spawns only on an
  impact, as today; dissipation alone shows no explosion.
- **Tower has no target, target leaves range, or target dies:** aim eases back to the
  default orientation and firing pulse stops; no residual beam/aim points at a dead
  enemy.
- **Placement while upgrade panel open:** unchanged; the new aim/visual nodes do not
  intercept the placement click.
- **Level reset with effects active:** all active bursts/sparks/explosions are detached
  and returned to their pools; nothing survives into the next level.
- **Skill tree rebuild while hovering:** old row references are discarded; the band
  reverts to idle; hovering a rebuilt row updates it.
- **Locked node with no Buy button:** hover still works via the row wrapper.
- **All nodes maxed:** rank shows `5/5`; description stays visible; no Buy button is
  required for hover.
- **Small/large viewport:** the info band and card stack stay inside their panels at the
  project viewport; the layout verifier is the gate.
- **Title screen hidden while animating:** the backdrop animation stops; returning to
  the title resumes from a consistent state without a burst of catch-up frames.
- **Result screen reappears for the other outcome:** a victory after a defeat (or vice
  versa) rebuilds/restyles the backdrop and title accent for the new result; no gold
  shapes leak into a defeat screen or red shards into a victory screen.
- **Rapid Return click:** the return button works on the first frame; backdrop animation
  never delays or blocks the click.
- **Returning to title after a result:** the title backdrop resumes cleanly; no result
  backdrop node or animation timer survives the transition.

## Dependencies

- `Enemy` — silhouette/profile, hit feedback, death burst hook.
- `GameManager` — death burst pool, effect lifecycle, level reset cleanup.
- `Tower`, `ArrowTower`, `CannonTower`, `LaserTower` — aim presentation, recoil/muzzle,
  emitter pulse; existing rank/archetype body paths.
- `Projectile`, `ArrowProjectile`, `CannonProjectile` — orientation and trails.
- `ExplosionEffect`, `PierceSparkEffect` — preserved contracts; new `DeathBurstEffect`.
- `GridManager` — spawn portals and base core.
- `GameHUD` — placement icon cards.
- `SkillTreeCatalog`, `SkillNodeDefinition` — descriptions.
- `SkillTreeScreen` — backdrop, node glyphs/halos, line glow, styled buttons, bottom
  info band and row hover signals.
- `TowerUpgradeIcons` (`ArchetypeEmblem`) — reused for seed glyphs; no new asset path.
- `TitleScreen` — backdrop composition, title decoration, menu button styling.
- `ResultScreen` — victory/defeat backdrops, title treatment, return button styling.
- `GameConstants` — all new presentation constants.
- `tools/layout_verifier.gd` — new assertions for cards and info band; existing
  title/result assertions preserved.

## Out of Scope

- In-game HUD top bar, fonts, and overall color-scheme redesign.
- New towers, enemies, waves, skills, or any gameplay/balance change.
- Audio or music.
- External art assets, sprite sheets, shaders, particles purchased/generated elsewhere.
- Pixel/screenshot verification (the headless renderer cannot do it; playtest covers
  subjective quality).

## Playtest Checklist (human)

- [ ] Enemy kinds are distinguishable at a glance while moving; trails and bursts read
      without hiding the path.
- [ ] Turrets visibly aim; firing feels responsive; no visual jitter or snapping.
- [ ] Spawn/base markers feel integrated, not like debug placeholders, and do not
      compete with the action.
- [ ] Placement cards are readable at a glance and match the towers on the field.
- [ ] Hovering skill nodes makes the bottom band readable; no row is clipped; moving off
      returns the hint.
- [ ] Skill tree reads as a designed screen: node glyphs distinguishable, locked vs
      unlocked vs maxed obvious at a glance, branch glow not noisy.
- [ ] Title screen feels alive without distracting from level selection; menu buttons
      read as one designed set.
- [ ] Victory and defeat are instantly distinguishable and feel like different events,
      not one recolored overlay.
- [ ] Frame rate holds on a busy wave (many enemies + laser + explosions).
