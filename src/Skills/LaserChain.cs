using System.Collections.Generic;
using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Pure laser-chain math shared by GameManager's drain pass and its unit tests:
/// per-jump damage falloff and nearest not-yet-hit target selection. All hot paths
/// iterate the passed lists by index (no enumerator boxing, no allocation per tick).
/// </summary>
public static class LaserChain
{
    /// <summary>
    /// Damage applied at a given jump depth (0 = first jump). Each jump is the
    /// previous tick's damage times <see cref="GameConstants.SkillLaserChainFalloff"/>,
    /// so the multiplier is falloff^(depth + 1).
    /// </summary>
    public static float JumpDamage(float tickDamage, int jumpDepth)
    {
        float multiplier = 1f;
        for (int i = 0; i <= jumpDepth; i++)
            multiplier *= GameConstants.SkillLaserChainFalloff;
        return tickDamage * multiplier;
    }

    /// <summary>
    /// Finds the nearest live candidate within <see cref="GameConstants.SkillLaserChainRange"/>
    /// cells of <paramref name="from"/> that is not <paramref name="from"/> itself and
    /// not already in <paramref name="alreadyHit"/>. Returns null when none qualifies.
    /// </summary>
    public static Enemy? FindNextTarget(Enemy from, IReadOnlyList<Enemy> candidates, IReadOnlyList<Enemy> alreadyHit)
    {
        float rangePixels = GameConstants.CellDistanceInPixels(GameConstants.SkillLaserChainRange);
        float rangeSq = rangePixels * rangePixels;

        Enemy? nearest = null;
        float nearestDistSq = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            var enemy = candidates[i];
            if (enemy.IsDead || ReferenceEquals(enemy, from))
                continue;
            if (Contains(alreadyHit, enemy))
                continue;

            float distSq = enemy.Position.DistanceSquaredTo(from.Position);
            if (distSq <= rangeSq && distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private static bool Contains(IReadOnlyList<Enemy> list, Enemy enemy)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (ReferenceEquals(list[i], enemy))
                return true;
        }
        return false;
    }
}
