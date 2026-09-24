using System.Collections.Generic;
using GeometryTowerDefense;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Deterministic <see cref="SkillRandom"/> for tests: returns scripted values in
/// order, then a fixed fallback forever. Values below a mechanic's chance force a
/// success; values at/above it force a miss.
/// </summary>
public sealed class ScriptedRandom : SkillRandom
{
    private readonly Queue<double> _values = new();
    private readonly double _fallback;

    public ScriptedRandom(double fallback, params double[] values)
        : base(0)
    {
        _fallback = fallback;
        foreach (var value in values)
            _values.Enqueue(value);
    }

    public override double NextDouble() => _values.Count > 0 ? _values.Dequeue() : _fallback;
}
