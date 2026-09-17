using System.Linq;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tests for swarm cluster formation geometry and spawn logic.
/// </summary>
[TestSuite]
public class WaveManagerTest
{
    [TestCase]
    public void GenerateClusterOffsets_FormsEvenRingWithHollowCenter_NoOverlap()
    {
        const float epsilon = 0.0001f;

        var offsets = WaveManager.GenerateClusterOffsets(
            GameConstants.SwarmClusterSize,
            GameConstants.SwarmClusterRadius);

        AssertThat(offsets.Count).IsEqual(GameConstants.SwarmClusterSize);

        // Every member sits on the ring (equidistant from the center).
        foreach (var offset in offsets)
            AssertThat(Mathf.Abs(offset.Length() - GameConstants.SwarmClusterRadius) < epsilon).IsTrue();

        // Evenly spaced angularly around the ring.
        var angles = offsets
            .Select(o =>
            {
                float a = Mathf.Atan2(o.Y, o.X);
                return a < 0f ? a + Mathf.Tau : a;
            })
            .OrderBy(a => a)
            .ToArray();

        float expectedStep = Mathf.Tau / offsets.Count;
        for (int i = 0; i < angles.Length; i++)
        {
            float next = angles[(i + 1) % angles.Length];
            float step = next - angles[i];
            if (step < 0f)
                step += Mathf.Tau;
            AssertThat(Mathf.Abs(step - expectedStep) < epsilon).IsTrue();
        }

        // Hollow center: every member center is farther than its own radius from the
        // origin, so no member covers the middle of the ring.
        foreach (var offset in offsets)
            AssertThat(offset.Length() > GameConstants.SwarmEnemyDiameter / 2f).IsTrue();

        // No overlap: minimum pairwise center distance exceeds the swarm diameter.
        float minDistance = float.MaxValue;
        for (int i = 0; i < offsets.Count; i++)
        {
            for (int j = i + 1; j < offsets.Count; j++)
                minDistance = Mathf.Min(minDistance, offsets[i].DistanceTo(offsets[j]));
        }

        AssertThat(minDistance > GameConstants.SwarmEnemyDiameter).IsTrue();
    }
}
