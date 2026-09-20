using System.Collections.Generic;
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

    [TestCase]
    public void NextRouteIndex_AlternatesBetweenRoutes()
    {
        int counter = 0;

        AssertThat(WaveManager.NextRouteIndex(ref counter, 2)).IsEqual(0);
        AssertThat(WaveManager.NextRouteIndex(ref counter, 2)).IsEqual(1);
        AssertThat(WaveManager.NextRouteIndex(ref counter, 2)).IsEqual(0);
        AssertThat(WaveManager.NextRouteIndex(ref counter, 2)).IsEqual(1);
    }

    [TestCase]
    public void NextRouteIndex_WrapsCleanlyAcrossManyWaves()
    {
        int counter = 0;
        int routeCount = Levels.Level4.Paths.Count;
        int expected = 0;

        // Thousands of spawn events: the modulo keeps the index in range forever.
        for (int i = 0; i < 1000; i++)
        {
            AssertThat(WaveManager.NextRouteIndex(ref counter, routeCount)).IsEqual(expected);
            expected = (expected + 1) % routeCount;
        }
    }

    [TestCase]
    public void NextRouteIndex_NeverReturnsNegativeRouteOnCounterOverflow()
    {
        // Start just below int.MaxValue so the counter wraps to negative mid-loop.
        // The unsigned modulo must still produce a non-negative route index.
        int counter = int.MaxValue - 2;
        int routeCount = Levels.Level4.Paths.Count;

        for (int i = 0; i < 6; i++)
        {
            int route = WaveManager.NextRouteIndex(ref counter, routeCount);
            AssertThat(route).IsGreaterEqual(0);
            AssertThat(route).IsLessEqual(routeCount - 1);
        }
    }

    [TestCase]
    public void RoundRobin_SwarmClusterMembersAllShareOneRoute()
    {
        // Level 4 wave 1: 3 Basic + 1 SwarmCluster + 1 Armored = 5 spawn events.
        var spawns = Levels.Level4.Waves[0].Spawns;
        int routeCount = Levels.Level4.Paths.Count;

        // The counter advances once per spawn EVENT — a cluster is one event.
        int counter = 0;
        var eventRoutes = new List<int>();
        foreach (var spawn in spawns)
            eventRoutes.Add(WaveManager.NextRouteIndex(ref counter, routeCount));

        AssertThat(eventRoutes.Count).IsEqual(5); // five events, not seven enemies
        AssertThat(eventRoutes[0]).IsEqual(0);
        AssertThat(eventRoutes[1]).IsEqual(1);
        AssertThat(eventRoutes[2]).IsEqual(0);
        AssertThat(eventRoutes[3]).IsEqual(1); // the SwarmCluster event
        AssertThat(eventRoutes[4]).IsEqual(0);

        // Expanding the same sequence into individual enemies, every cluster member
        // shares the cluster's single route index (members never straddle routes).
        counter = 0;
        var enemyRoutes = new List<int>();
        foreach (var spawn in spawns)
        {
            int route = WaveManager.NextRouteIndex(ref counter, routeCount);
            int count = WaveDefinition.Resolve(spawn).Count;
            for (int i = 0; i < count; i++)
                enemyRoutes.Add(route);
        }

        AssertThat(enemyRoutes.Count).IsEqual(7);
        // The cluster occupies enemy slots 3, 4, 5 and all share one route.
        AssertThat(enemyRoutes[3]).IsEqual(enemyRoutes[4]);
        AssertThat(enemyRoutes[4]).IsEqual(enemyRoutes[5]);
        AssertThat(enemyRoutes[3]).IsNotEqual(enemyRoutes[2]);
        AssertThat(enemyRoutes[5]).IsNotEqual(enemyRoutes[6]);
    }
}
