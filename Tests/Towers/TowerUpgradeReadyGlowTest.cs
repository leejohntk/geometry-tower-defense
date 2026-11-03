using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Upgrade-ready glow: a subtle pulsing pale-gold halo on a placed tower exactly when
/// the next upgrade's hidden XP gate is filled. Coins never affect it, it is
/// opacity-only, click-through, and lives behind the tower body, range indicator, and
/// rank badge. Rank badge behavior is purchase-driven and independent.
/// </summary>
[TestSuite]
public class TowerUpgradeReadyGlowTest
{
    private static Control? Glow(Tower tower) =>
        tower.GetNodeOrNull<Control>("UpgradeReadyGlow");

    private static Control? Body(Tower tower) => tower.GetNodeOrNull<Control>(
        tower.Type switch
        {
            TowerType.Cannon => "CannonBody",
            TowerType.Laser => "LaserEmitter",
            _ => "ArrowBody"
        });

    private static int GlowNodeCount(Tower tower)
    {
        int count = 0;
        foreach (Node child in tower.GetChildren())
        {
            if (child.Name == "UpgradeReadyGlow")
                count++;
        }
        return count;
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
    public void ReadyTower_ShowsOneCellSizedClickThroughGlow_BehindTheBody()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);

            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(GlowNodeCount(tower)).IsEqual(1);
            AssertThat(glow!.Visible).IsTrue();
            AssertThat(tower.IsUpgradeGlowVisible).IsTrue();

            // One cell, centred on the tower origin (the cell centre).
            AssertThat(glow.Size).IsEqual(
                new Vector2(GameConstants.CellSize, GameConstants.CellSize));
            AssertThat(glow.Position).IsEqual(
                new Vector2(-GameConstants.CellSize / 2f, -GameConstants.CellSize / 2f));

            // Click-through and behind the body (which sits at z 0).
            AssertThat(glow.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            var body = Body(tower);
            AssertThat(body).IsNotNull();
            AssertThat(glow.ZIndex).IsLess(0);
            AssertThat(glow.ZIndex).IsLess(body!.ZIndex);

            // Nominal halo is 60 px across: one cell minus the 2 px inset on each side.
            AssertThat(GameConstants.TowerUpgradeGlowOuterRadius * 2f)
                .IsEqualApprox(GameConstants.CellSize - 4f, 0.0001f);
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
        tower.AddXp(GameConstants.TowerXpThresholdRank1);
        var glow = Glow(tower);
        AssertThat(glow).IsNotNull();
        AssertThat(glow!.Visible).IsTrue();

        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.Rank).IsEqual(1);
        AssertThat(tower.HasUpgradeReady).IsFalse();
        AssertThat(Glow(tower) == glow).IsTrue(); // same node for the tower's whole life
        AssertThat(glow.Visible).IsFalse();

        // Surplus XP: every purchase still leaves the next gate filled.
        var surplus = TestTowers.Create(TowerType.Arrow);
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

    [TestCase]
    public void ArchetypeStep_GlowsBeforeTheChoice_AndStopsAtMaxRank()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
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

    [TestCase]
    public void Coins_NeverDriveTheGlow()
    {
        var manager = new GameManager();
        manager.Initialize(Levels.Level1);
        try
        {
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
            manager.Free();
        }
    }

    [TestCase]
    public void RankBadge_StaysPurchaseDriven_AndIndependentOfTheGlow()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
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
        AssertThat(glow.ZIndex).IsLess(badge.ZIndex);

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

    [TestCase]
    public void SelectedTower_KeepsTheRangeIndicatorOnTop_WithoutChangingItsGeometry()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();

            tower.ShowRange();
            var range = tower.GetNodeOrNull<Control>("RangeIndicator");
            AssertThat(range).IsNotNull();
            AssertThat(glow!.ZIndex).IsLess(range!.ZIndex);
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

            // Opacity is the only thing that moved.
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
            Color themeColor = tower.TowerColor;

            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            AssertThat(glow!.Visible).IsTrue();
            AssertThat(glow.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(glow.Size).IsEqual(new Vector2(
                GameConstants.TowerUpgradeGlowNodeSize,
                GameConstants.TowerUpgradeGlowNodeSize));

            // Tower theme colors are unchanged by the cue.
            AssertThat(tower.TowerColor).IsEqual(themeColor);
        }

        // Shared pale-gold hue, not a per-type color.
        AssertThat(GameConstants.TowerUpgradeGlowColor)
            .IsEqual(new Color(1f, 0.93f, 0.55f));
    }

    [TestCase]
    public void MultipleTowers_TrackTheirOwnGate()
    {
        var first = TestTowers.Create(TowerType.Arrow, 5, 5);
        var second = TestTowers.Create(TowerType.Cannon, 6, 6);

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

    [TestCase]
    public void GlowIsOwnedByTheTower_AndFreedWithIt()
    {
        var tower = TestTowers.Create(TowerType.Arrow);
        NodeTestSupport.Attach(tower);
        try
        {
            tower.AddXp(GameConstants.TowerXpThresholdRank1);
            var glow = Glow(tower);
            AssertThat(glow).IsNotNull();
            // No orphan: the glow lives under the tower, so its lifetime is the tower's.
            AssertThat(glow!.GetParent() == tower).IsTrue();

            tower.Free();
            AssertThat(GodotObject.IsInstanceValid(tower)).IsFalse();
            AssertThat(GodotObject.IsInstanceValid(glow)).IsFalse();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(tower))
                NodeTestSupport.Cleanup(tower);
        }
    }
}
