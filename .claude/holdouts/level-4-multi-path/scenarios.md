# Holdout Scenarios: level-4-multi-path

Implementer must NOT see this file. Orchestrator runs these during verification.

## H1 — Two routes, single shared base
- Level 4 has exactly 2 routes. Both `route[0]` are at col 0 (left). Both `route[^1]` are the same cell.
- Assert `Level4.Paths.Count == 2` and both routes share the same base cell.

## H2 — Each route is valid
- Each route passes `IsPathConnectedAndInBounds` (axis-aligned, connected, in 20×14) and does NOT `PathSelfIntersects`.

## H3 — Converge/split topology
- Routes share cells at the three converge runs and diverge at exactly two split cells.
- Route A and Route B differ only in the split segments (rows 4 vs 10); the converge runs are identical.
- Verify: shared-cell count matches converge runs; the two routes are NOT identical lists.

## H4 — Levels 1–3 unchanged
- Level 1/2/3 still have a single route (`Paths.Count == 1`), spawn at `route[0]`, base at `route[^1]` — same cells as before this feature.

## H5 — Round-robin spawn distribution
- A wave of N single-enemy spawns sends roughly half to route A and half to route B (alternating).
- The first spawned enemy is on route A (index 0), second on route B (index 1), etc.

## H6 — Swarm cluster shares one route
- A SwarmCluster spawn assigns ALL members the same route index (never split across routes).

## H7 — All three tower types allowed
- `Level4.AllowCannonTower == true` and `Level4.AllowLaserTower == true`. HUD shows Cannon and Laser buttons for Level 4.

## H8 — All three enemy kinds present
- Level 4 waves contain at least one Basic, one SwarmCluster, and one Armored spawn.

## H9 — No cross-route contamination
- An enemy assigned route A walks only route A's split segment (row 4), never route B's (row 10), and vice versa.

## H10 — Build + tests
- `dotnet build` 0 errors; `dotnet test` all pass.
