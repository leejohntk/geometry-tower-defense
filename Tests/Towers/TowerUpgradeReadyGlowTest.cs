using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Upgrade-ready glow: an additive overlay that redraws the tower's own body silhouette
/// on top of it exactly when the next upgrade's hidden XP gate is filled. Coins never
/// affect it, it is opacity-only, click-through, inherits the body's aim/recoil
/// transform, and sits below the range indicator and rank badge. Rank badge behavior is
/// purchase-driven and independent.
/// </summary>
[TestSuite]
public class TowerUpgradeReadyGlowTest
{
    private static Control? Body(Tower tower) => tower.GetNodeOrNull<Control>(
        tower.Type switch
        {
            TowerType.Cannon => "CannonBody",
            TowerType.Laser => "LaserEmitter",
            _ => "ArrowBody"
        });

    private static Control? Glow(Tower tower) =>
        Body(tower)?.GetNodeOrNull<Control>("UpgradeReadyGlow");

    private static int GlowNodeCount(Tower tower) =>
        tower.FindChildren("UpgradeReadyGlow", "", recursive: true, owned: false).Count;

    private static void AssertAdditiveOverlay(Control glow, Control body)
    {
        // A child of the body draws after it, shares its transform, and spans its rect.
        AssertThat(glow.GetParent() == body).IsTrue();
        AssertThat(glow.Position).IsEqual(Vector2.Zero);
        AssertThat(glow.Size).IsEqual(body.Size);
        AssertThat(glow.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        AssertThat(glow.Material is CanvasItemMaterial).IsTrue();
        AssertThat(((CanvasItemMaterial)glow.Material!).BlendMode)
            .IsEqual(CanvasItemMaterial.BlendModeEnum.Add);
    }

    [TestCase]
    public void HasUpgradeReady_IsExactlyTheXpGate_AtEveryRank()
    {
        var tower = TestTowers.Create(TowerType.Arrow);

        AssertThat(tower.HasUpgradeReady).IsFalse(); // rank 0, no XP

        // Rank 0 -> 1 gate: visible at exactly 5, not one point below.
        tower.AddXp(GameConstants.TowerXpThresholdRank1 - 0.25f);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(0.25f);
        AssertThat(tower.HasUpgradeReady).IsTrue();
        AssertThat(tower.HasUpgradeReady)
            .IsEqual(TowerUpgrades.IsUpgradeVisible(tower.Rank, tower.Xp));

        // Rank 1 -> 2 gate: visible at exactly 15.
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(GameConstants.TowerXpThresholdRank2
                    - GameConstants.TowerXpThresholdRank1 - 0.25f);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(0.25f);
        AssertThat(tower.HasUpgradeReady).IsTrue();
        AssertThat(tower.HasUpgradeReady)
            .IsEqual(TowerUpgrades.IsUpgradeVisible(tower.Rank, tower.Xp));

        // Rank 2 -> 3 archetype gate: visible at exactly 30, before any choice.
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.Rank).IsEqual(2);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(GameConstants.TowerXpThresholdRank3
                    - GameConstants.TowerXpThresholdRank2 - 0.25f);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(0.25f);
        AssertThat(tower.HasUpgradeReady).IsTrue();

        // Max rank has no next gate, ever.
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
        AssertThat(tower.Rank).IsEqual(GameConstants.TowerMaxRank);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(1000f);
        AssertThat(tower.HasUpgradeReady).IsFalse();
    }

    [TestCase]
    public void FractionalXp_ReachesTheGateWithoutRounding()
    {
        var tower = TestTowers.Create(TowerType.Arrow);

        tower.AddXp(4.75f);
        AssertThat(tower.Xp).IsEqual(4.75f);
        AssertThat(tower.HasUpgradeReady).IsFalse();

        // Fractional accumulation reaching the threshold counts; nothing is rounded.
        tower.AddXp(0.25f);
        AssertThat(tower.Xp).IsEqual(5f);
        AssertThat(tower.HasUpgradeReady).IsTrue();

        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        tower.AddXp(9.9f);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        tower.AddXp(0.1f);
        AssertThat(tower.HasUpgradeReady).IsTrue();
    }

    [TestCase]
    public void ReadyTower_ShowsOneBodySizedAdditiveClickThroughOverlay_OnTheBody()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);

            var body = Body(tower);
            var glow = Glow(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow).IsNotNull();
            AssertThat(GlowNodeCount(tower)).IsEqual(1);
            AssertThat(glow!.Visible).IsTrue();
            AssertThat(tower.IsUpgradeGlowVisible).IsTrue();

            // The overlay spans exactly the body rect and carries no halo/ring geometry
            // of its own: the silhouette is the body renderer re-invoked additively.
            AssertAdditiveOverlay(glow, body!);
            AssertThat(glow.Size).IsEqual(
                new Vector2(GameConstants.CellSize, GameConstants.CellSize));
            AssertThat(glow.GetChildCount()).IsEqual(0);
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void PurchaseWithoutSurplusXp_HidesTheGlow_AndSurplusXpKeepsItVisible()
    {
        // Exactly at the rank-1 gate: after the purchase the next gate is closed.
        var tower = TestTowers.Create(TowerType.Cannon);
        var surplus = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        NodeTestSupport.Attach(surplus);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue();

            AssertThat(tower.ApplyRankUpgrade()).IsTrue();
            AssertThat(tower.Rank).IsEqual(1);
            AssertThat(tower.HasUpgradeReady).IsFalse();
            AssertThat(Glow(tower) == glow).IsTrue(); // same node for the tower's whole life
            AssertThat(glow.Visible).IsFalse();

            // Crossing the next gate reveals the same node again, with no duplicate.
            tower.AddXp(GameConstants.TowerXpThresholdRank2
                        - GameConstants.TowerXpThresholdRank1);
            AssertThat(tower.HasUpgradeReady).IsTrue();
            AssertThat(Glow(tower) == glow).IsTrue();
            AssertThat(glow.Visible).IsTrue();
            AssertThat(GlowNodeCount(tower)).IsEqual(1);

            // Surplus XP: every purchase still leaves the next gate filled.
            surplus.AddXp(GameConstants.TowerXpThresholdRank3 + 10f);
            var surplusGlow = Glow(surplus);
            AssertThat(surplusGlow).IsNotNull();
            AssertThat(surplusGlow!.Visible).IsTrue();

            AssertThat(surplus.ApplyRankUpgrade()).IsTrue(); // rank 1, 40 >= 15
            AssertThat(surplusGlow.Visible).IsTrue();
            AssertThat(surplus.ApplyRankUpgrade()).IsTrue(); // rank 2, 40 >= 30
            AssertThat(surplusGlow.Visible).IsTrue();
            AssertThat(Glow(surplus) == surplusGlow).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
            NodeTestSupport.Cleanup(surplus);
        }
    }

    [TestCase]
    public void ArchetypeStep_GlowsBeforeTheChoice_AndStopsAtMaxRank()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank3);

            AssertThat(tower.ApplyRankUpgrade()).IsTrue(); // rank 1 (surplus XP)
            AssertThat(tower.ApplyRankUpgrade()).IsTrue(); // rank 2
            AssertThat(tower.Rank).IsEqual(2);

            // The archetype choice is the next upgrade, so the cue stays on until chosen.
            AssertThat(tower.HasUpgradeReady).IsTrue();
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue();

            AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
            AssertThat(tower.Rank).IsEqual(GameConstants.TowerMaxRank);
            AssertThat(tower.HasUpgradeReady).IsFalse();
            AssertThat(glow.Visible).IsFalse();

            // Permanently off for the rest of the tower's life.
            tower.AddXp(1000f);
            AssertThat(tower.HasUpgradeReady).IsFalse();
            AssertThat(glow.Visible).IsFalse();
            AssertThat(tower.IsUpgradeGlowVisible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void Coins_NeverDriveTheGlow()
    {
        var manager = NodeTestSupport.Attach(new GameManager());
        try
        {
            manager.Initialize(Levels.Level1);
            manager.PlacingTowerType = TowerType.Arrow;
            AssertThat(manager.PlaceTower(0, 0)).IsTrue(); // all 10 starting coins spent
            var tower = manager.GetActiveTowers()[0];
            AssertThat(manager.Coins).IsEqual(0);

            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            tower._Process(1f / 60f);
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue(); // zero coins: the XP gate still glows

            // A rejected purchase (coins short) must neither hide nor show it.
            AssertThat(manager.TryUpgradeTower(tower)).IsFalse();
            AssertThat(tower.Rank).IsEqual(0);
            AssertThat(glow.Visible).IsTrue();

            // Coins changing never toggles the cue either.
            manager.AddCoinsForTesting(20);
            AssertThat(glow.Visible).IsTrue();
            AssertThat(tower.HasUpgradeReady).IsTrue();

            // A successful purchase that closes the next gate does hide it.
            AssertThat(manager.TryUpgradeTower(tower)).IsTrue();
            AssertThat(tower.Rank).IsEqual(1);
            AssertThat(glow.Visible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(manager);
        }
    }

    [TestCase]
    public void RankBadge_StaysPurchaseDriven_AndIndependentOfTheGlow()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank2); // 15 XP opens the rank-1 gate

            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue();
            // Earning XP never creates the purchase-driven badge.
            AssertThat(tower.GetNodeOrNull<Control>("RankBadge")).IsNull();

            AssertThat(tower.ApplyRankUpgrade()).IsTrue(); // rank 1; 15 >= 15 so the glow stays
            var badge = tower.GetNodeOrNull<Control>("RankBadge");
            AssertThat(badge).IsNotNull();
            AssertThat(badge!.Visible).IsTrue();
            AssertThat(glow.Visible).IsTrue();

            // The badge is a sibling above the body, so it draws above the body and its
            // child overlay.
            var body = Body(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow.GetParent() == body).IsTrue();
            AssertThat(badge.GetParent() == tower).IsTrue();
            AssertThat(badge.ZIndex).IsGreater(body!.ZIndex);

            AssertThat(tower.ApplyRankUpgrade()).IsTrue(); // rank 2; next gate is 30 and closed
            AssertThat(glow.Visible).IsFalse();
            AssertThat(badge.Visible).IsTrue();
            AssertThat(badge.Size).IsEqual(new Vector2(
                GameConstants.TowerRankBadgeWidth,
                GameConstants.TowerRankBadgeHeight));

            // The glow hiding and showing never repaints or resizes the badge.
            AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();
            AssertThat(badge.Visible).IsTrue();
            AssertThat(badge.Size).IsEqual(new Vector2(
                GameConstants.TowerRankBadgeWidth,
                GameConstants.TowerRankBadgeHeight));
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void SelectedTower_KeepsTheRangeIndicatorOnTop_WithoutChangingItsGeometry()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            var body = Body(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(body).IsNotNull();

            tower.ShowRange();
            var range = tower.GetNodeOrNull<Control>("RangeIndicator");
            AssertThat(range).IsNotNull();

            // The overlay is a child of the body; the range control is added later as a
            // sibling, so it draws after the whole body subtree.
            AssertThat(glow!.GetParent() == body).IsTrue();
            AssertThat(range!.GetParent() == tower).IsTrue();
            AssertThat(range.GetIndex()).IsGreater(body!.GetIndex());
            AssertThat(glow.Visible).IsTrue();

            // Selection geometry is untouched by the cue.
            AssertThat(range.Position).IsEqual(
                new Vector2(-tower.RangePixels, -tower.RangePixels));
            AssertThat(range.Size).IsEqual(
                new Vector2(tower.RangePixels * 2f, tower.RangePixels * 2f));
            AssertThat(range.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void GlowPulsesByOpacityOnly_AndNeverMovesTheTowerOrBody()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            var body = Body(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(body).IsNotNull();

            Vector2 towerPosition = tower.Position;
            Vector2 glowPosition = glow!.Position;
            Vector2 glowSize = glow.Size;
            Vector2 glowScale = glow.Scale;
            float glowRotation = glow.Rotation;
            Vector2 bodyPosition = body!.Position;
            float bodyRotation = body.Rotation;
            Vector2 bodyScale = body.Scale;

            // Quarter period: pulse peak.
            tower._Process(GameConstants.TowerUpgradeGlowPulsePeriod / 4f);
            AssertThat(glow.Modulate.A)
                .IsEqualApprox(GameConstants.TowerUpgradeGlowAlphaMax, 0.0001f);

            // Half a period later: pulse trough.
            tower._Process(GameConstants.TowerUpgradeGlowPulsePeriod / 2f);
            AssertThat(glow.Modulate.A)
                .IsEqualApprox(GameConstants.TowerUpgradeGlowAlphaMin, 0.0001f);

            // Half a period later: peak again.
            tower._Process(GameConstants.TowerUpgradeGlowPulsePeriod / 2f);
            AssertThat(glow.Modulate.A)
                .IsEqualApprox(GameConstants.TowerUpgradeGlowAlphaMax, 0.0001f);

            // Opacity is the only thing that moved. Every sampled frame stays inside the
            // configured pulse range, derived from the constants (min must be < max).
            float minAlpha = GameConstants.TowerUpgradeGlowAlphaMin;
            float maxAlpha = GameConstants.TowerUpgradeGlowAlphaMax;
            AssertThat(minAlpha).IsLess(maxAlpha);
            for (int i = 0; i < 32; i++)
            {
                tower._Process(GameConstants.TowerUpgradeGlowPulsePeriod / 32f);
                AssertThat(glow.Modulate.A).IsGreaterEqual(minAlpha - 0.0001f);
                AssertThat(glow.Modulate.A).IsLessEqual(maxAlpha + 0.0001f);
            }
            AssertThat(tower.Position).IsEqual(towerPosition);
            AssertThat(glow.Position).IsEqual(glowPosition);
            AssertThat(glow.Size).IsEqual(glowSize);
            AssertThat(glow.Scale).IsEqual(glowScale);
            AssertThat(glow.Rotation).IsEqual(glowRotation);
            AssertThat(body.Position).IsEqual(bodyPosition);
            AssertThat(body.Rotation).IsEqual(bodyRotation);
            AssertThat(body.Scale).IsEqual(bodyScale);
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void NoReadyUpgrade_CreatesNoGlowAndDoesNoGlowWork()
    {
        var tower = TestTowers.Create(TowerType.Laser);
        NodeTestSupport.Attach(tower);
        try
        {
            for (int i = 0; i < 120; i++)
                tower._Process(1f / 60f);

            AssertThat(tower.HasUpgradeReady).IsFalse();
            AssertThat(Glow(tower)).IsNull();
            AssertThat(GlowNodeCount(tower)).IsEqual(0);
            AssertThat(tower.IsUpgradeGlowVisible).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void EveryTowerType_GetsTheSameCue_AndKeepsItsThemeColor()
    {
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            var tower = TestTowers.Create(type);
            NodeTestSupport.Attach(tower);
            try
            {
                Color themeColor = tower.TowerColor;
                tower.AddXp(GameConstants.TowerXpThresholdRank1);

                var body = Body(tower);
                var glow = Glow(tower);
                AssertThat(body).IsNotNull();
                AssertThat(glow).IsNotNull();
                AssertThat(glow!.Visible).IsTrue();

                // Same additive body-silhouette treatment, not a per-type halo.
                AssertAdditiveOverlay(glow, body!);

                // Tower theme colors are unchanged by the cue.
                AssertThat(tower.TowerColor).IsEqual(themeColor);
            }
            finally
            {
                NodeTestSupport.Cleanup(tower);
            }
        }
    }

    [TestCase]
    public void MultipleTowers_TrackTheirOwnGate()
    {
        var first = TestTowers.Create(TowerType.Arrow, 5, 5);
        var second = TestTowers.Create(TowerType.Cannon, 6, 6);
        NodeTestSupport.Attach(first);
        NodeTestSupport.Attach(second);
        try
        {
            first.AddXp(GameConstants.TowerXpThresholdRank1);
            second.AddXp(GameConstants.TowerXpThresholdRank1 - 0.25f);

            AssertThat(first.HasUpgradeReady).IsTrue();
            AssertThat(second.HasUpgradeReady).IsFalse();
            AssertThat(Glow(first)).IsNotNull();
            AssertThat(Glow(second)).IsNull();

            // Buying on one never changes the other.
            AssertThat(first.ApplyRankUpgrade()).IsTrue();
            AssertThat(Glow(first)!.Visible).IsFalse();
            AssertThat(Glow(second)).IsNull();

            second.AddXp(0.25f);
            AssertThat(second.HasUpgradeReady).IsTrue();
            var secondGlow = Glow(second);
            AssertThat(secondGlow).IsNotNull();
            AssertThat(secondGlow!.Visible).IsTrue();
            AssertThat(Glow(first)!.Visible).IsFalse();
            AssertThat(Glow(first) == secondGlow).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(first);
            NodeTestSupport.Cleanup(second);
        }
    }

    [TestCase]
    public void GlowIsOwnedByTheTower_AndFreedWithIt()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            var body = Body(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(body).IsNotNull();
            // The overlay lives under the body, so its lifetime is the tower's.
            AssertThat(glow!.GetParent() == body).IsTrue();

            tower.Free();
            AssertThat(GodotObject.IsInstanceValid(tower)).IsFalse();
            AssertThat(GodotObject.IsInstanceValid(body)).IsFalse();
            AssertThat(GodotObject.IsInstanceValid(glow)).IsFalse();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(tower))
                NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void ArchetypeChange_KeepsTheSameAttachedOverlay_HiddenAtMaxRank()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank3);
            var body = Body(tower);
            var glow = Glow(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.GetParent() == body).IsTrue();

            AssertThat(tower.ApplyRankUpgrade()).IsTrue();
            AssertThat(tower.ApplyRankUpgrade()).IsTrue();
            AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

            // The same overlay node stays attached to the body with the same rect; the
            // rank-3 body renderer is archetype-aware if the cue ever shows again.
            AssertThat(Glow(tower) == glow).IsTrue();
            AssertAdditiveOverlay(glow, body!);

            // Rank 3 has no next gate, so the cue stays hidden for the tower's life.
            AssertThat(glow.Visible).IsFalse();
            AssertThat(tower.HasUpgradeReady).IsFalse();
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }

    [TestCase]
    public void Overlay_FollowsBodyAimAndRecoil_ThroughParenting()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        var enemy = new Enemy();
        enemy.Configure(EnemyKind.Basic);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var body = Body(tower);
            var glow = Glow(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow).IsNotNull();

            // Resting arrow points up; a target to the right rotates the body toward +X.
            Transform2D restingGlobal = body!.GetGlobalTransform();
            enemy.Position = tower.Position + new Vector2(100f, 0f);
            tower.SetTarget(enemy);
            AssertThat(tower.TryFire(enemy, out _)).IsTrue();
            tower._Process(0.05f);

            Transform2D bodyGlobal = body.GetGlobalTransform();
            Transform2D glowGlobal = glow!.GetGlobalTransform();

            // The body actually moved (rotation toward the target + recoil kick).
            AssertThat(bodyGlobal.IsEqualApprox(restingGlobal)).IsFalse();
            // The overlay has no transform of its own, so it matches the body exactly.
            // Reparenting it to the tower instead of the body breaks this assertion.
            AssertThat(glowGlobal.IsEqualApprox(bodyGlobal)).IsTrue();
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
            enemy.Free();
        }
    }

    [TestCase]
    public void XpBeforePlacement_CreatesTheOverlayWhenTheBodyRegisters()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        tower.AddXp(GameConstants.TowerXpThresholdRank1);

        // No body is registered until the tower enters the tree: no fallback halo,
        // but the gate itself already reports ready.
        AssertThat(tower.HasUpgradeReady).IsTrue();
        AssertThat(Glow(tower)).IsNull();

        NodeTestSupport.Attach(tower);
        try
        {
            var body = Body(tower);
            var glow = Glow(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.GetParent() == body).IsTrue();
            AssertThat(glow.Visible).IsTrue();
            AssertThat(GlowNodeCount(tower)).IsEqual(1);
        }
        finally
        {
            NodeTestSupport.Cleanup(tower);
        }
    }
}
