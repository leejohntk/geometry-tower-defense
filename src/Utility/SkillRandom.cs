using System;

namespace GeometryTowerDefense;

/// <summary>
/// Injectable, seeded source for skill-mechanic rolls (arrow crit, cannon stun,
/// laser ignite). One instance is shared across a game so rolls can be scripted
/// reproducibly in tests: subclasses override <see cref="NextDouble"/> to return
/// exact sequences. Cheap — a single field, no allocation per roll.
/// </summary>
public class SkillRandom
{
    private readonly Random _random;

    /// <summary>
    /// Shared fallback instance used when no roll source is injected (e.g. towers
    /// constructed directly in tests or outside a game).
    /// </summary>
    public static readonly SkillRandom Default = new(Environment.TickCount);

    public SkillRandom(int seed)
    {
        _random = new Random(seed);
    }

    /// <summary>
    /// Next uniform double in [0, 1).
    /// </summary>
    public virtual double NextDouble() => _random.NextDouble();

    /// <summary>
    /// True when the next roll lands below <paramref name="chance"/>.
    /// </summary>
    public bool Roll(double chance) => NextDouble() < chance;
}
