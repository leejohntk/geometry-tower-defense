using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Tiny current-rank badge on a placed tower: created by the rank purchase itself
/// (not by the hidden XP gate), fixed size at every rank, click-through, docked to
/// the top-right of the tower's cell, and showing the archetype emblem at rank 3.
/// </summary>
[TestSuite]
public class TowerRankBadgeTest
{
    private static Tower MakeTower(TowerType type, int row = 5, int col = 10)
    {
        Tower tower = type switch
        {
            TowerType.Cannon => new CannonTower(),
            TowerType.Laser => new LaserTower(),
            _ => new ArrowTower()
        };
        tower.Initialize(row, col);
        return tower;
    }

    private static Control? Badge(Tower tower) => tower.GetNodeOrNull<Control>("RankBadge");

    private static Vector2 BadgeSize =>
        new(GameConstants.TowerRankBadgeWidth, GameConstants.TowerRankBadgeHeight);

    [TestCase]
    public void RankZero_HasNoBadge_EvenWhenTheXpGateOpens()
    {
        var tower = MakeTower(TowerType.Arrow);
        AssertThat(Badge(tower)).IsNull();

        // The next rank's gate opening is not a purchase: still no badge.
        tower.AddXp(5f);
        AssertThat(tower.Rank).IsEqual(0);
        AssertThat(Badge(tower)).IsNull();

        tower.AddXp(25f); // even with the rank-3 threshold reached
        AssertThat(Badge(tower)).IsNull();
    }

    [TestCase]
    public void RankOnePurchase_ShowsAVisibleClickThroughBadge_AtTheCellTopRight_WithoutAPanel()
    {
        var tower = MakeTower(TowerType.Arrow);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();

        var badge = Badge(tower);
        AssertThat(badge).IsNotNull();
        AssertThat(badge!.Visible).IsTrue();
        AssertThat(badge.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        AssertThat(badge.Size).IsEqual(BadgeSize);

        // Tower origin is the cell centre: the badge sits inside the top-right quadrant.
        float halfCell = GameConstants.CellSize / 2f;
        AssertThat(badge.Position.X).IsGreater(0f);
        AssertThat(badge.Position.X + badge.Size.X).IsLessEqual(halfCell);
        AssertThat(badge.Position.Y).IsLess(0f);
        AssertThat(badge.Position.Y).IsGreaterEqual(-halfCell);

        // Above the tower body.
        AssertThat(badge.ZIndex).IsGreater(0);
    }

    [TestCase]
    public void RankTwoPurchase_KeepsTheSameBadgeNodeAndFixedSize()
    {
        var tower = MakeTower(TowerType.Cannon);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        var badge = Badge(tower);
        AssertThat(badge).IsNotNull();
        var sizeAfterRankOne = badge!.Size;

        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.Rank).IsEqual(2);

        var persisted = Badge(tower);
        AssertThat(persisted == badge).IsTrue();
        AssertThat(persisted!.Visible).IsTrue();
        AssertThat(persisted.Size).IsEqual(sizeAfterRankOne);
        AssertThat(persisted.Size).IsEqual(BadgeSize);
        AssertThat(RankIcon.BadgeUsesEmblem(tower.Rank, tower.Archetype)).IsFalse();
    }

    [TestCase]
    public void RankThree_ShowsTheArchetypeEmblemPath_AndStaysFixedSize()
    {
        var tower = MakeTower(TowerType.Arrow);
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyRankUpgrade()).IsTrue();
        AssertThat(tower.ApplyArchetype(TowerArchetype.Pierce)).IsTrue();

        var badge = Badge(tower);
        AssertThat(badge).IsNotNull();
        AssertThat(badge!.Visible).IsTrue();
        AssertThat(badge.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
        AssertThat(badge.Size).IsEqual(BadgeSize);
        AssertThat(badge.Size).IsEqual(
            new Vector2(GameConstants.TowerRankBadgeWidth, GameConstants.TowerRankBadgeHeight));

        // Rank 3 with a chosen archetype draws the emblem instead of a chevron stack.
        AssertThat(RankIcon.BadgeUsesEmblem(tower.Rank, tower.Archetype)).IsTrue();
        AssertThat(RankIcon.BadgeUsesEmblem(1, TowerArchetype.None)).IsFalse();
        AssertThat(RankIcon.BadgeUsesEmblem(2, TowerArchetype.None)).IsFalse();
        AssertThat(RankIcon.BadgeUsesEmblem(3, TowerArchetype.None)).IsFalse();
    }

    [TestCase]
    public void EveryTowerType_GetsABadgeOnItsFirstPurchase()
    {
        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            var tower = MakeTower(type);
            AssertThat(tower.ApplyRankUpgrade()).IsTrue();

            var badge = Badge(tower);
            AssertThat(badge).IsNotNull();
            AssertThat(badge!.Size).IsEqual(BadgeSize);
            AssertThat(badge.Position.X).IsGreater(0f);
            AssertThat(badge.Position.Y).IsLess(0f);
        }
    }
}
