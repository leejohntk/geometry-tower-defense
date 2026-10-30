using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Catalog descriptions and glyph identities: every node has a player-facing description
/// with concrete numbers matching the tuning constants, seed nodes declare their archetype
/// relationship, and every trunk node has a distinct glyph category.
/// </summary>
[TestSuite]
public class SkillDescriptionsTest
{
    [TestCase]
    public void EveryNode_HasANonEmptyUniqueDescription()
    {
        var seen = new HashSet<string>();

        foreach (var node in SkillTreeCatalog.All)
        {
            AssertThat(node.Description.Length).IsGreater(0);
            AssertThat(seen.Add(node.Description)).IsTrue();
        }

        AssertThat(seen.Count).IsEqual(SkillTreeCatalog.All.Count);
    }

    [TestCase]
    public void EveryDescription_MentionsRanks()
    {
        foreach (var node in SkillTreeCatalog.All)
        {
            AssertThat(node.Description.ToLower().Contains("rank")).IsTrue();
        }
    }

    [TestCase]
    public void SeedDescriptions_DeclareTheirArchetypeAndRankThreeRelationship()
    {
        var seedIds = new[]
        {
            SkillTreeCatalog.ArrowPierce,
            SkillTreeCatalog.ArrowCritChance,
            SkillTreeCatalog.CannonCluster,
            SkillTreeCatalog.CannonStunChance,
            SkillTreeCatalog.LaserChain,
            SkillTreeCatalog.LaserRampUp,
        };

        foreach (var id in seedIds)
        {
            string description = SkillTreeCatalog.Description(id);
            AssertThat(description.StartsWith("Archetype:")).IsTrue();
            AssertThat(description.Contains("tower rank 3")).IsTrue();
        }
    }

    [TestCase]
    public void SeedDescriptions_MatchTheMechanicCurves()
    {
        // Pierce passes through one enemy per effective rank (max(1, rank)).
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.ArrowPierce).Contains("1 enemy per rank")).IsTrue();
        // Crit: rank x 8% (at least 8% once specialized).
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.ArrowCritChance).Contains("8%")).IsTrue();
        // Cluster: 1 + rank shells (at least 2 once specialized).
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.CannonCluster).Contains("1 + rank")).IsTrue();
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.CannonCluster).Contains("at least 2")).IsTrue();
        // Stun: rank x 6%.
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.CannonStunChance).Contains("6%")).IsTrue();
        // Chain: one jump per effective rank (max(1, rank)).
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.LaserChain).Contains("1 extra enemy per rank")).IsTrue();
        // Ramp: +20% max damage per rank (rank 5 = +100%).
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.LaserRampUp).Contains("20%")).IsTrue();
        AssertThat(SkillTreeCatalog.Description(SkillTreeCatalog.LaserRampUp).Contains("+100%")).IsTrue();
    }

    [TestCase]
    public void ArrowDescriptions_MatchTheTuningNumbers()
    {
        string damage = SkillTreeCatalog.Description(SkillTreeCatalog.ArrowDamage);
        AssertThat(damage.Contains("+2")).IsTrue();
        AssertThat(damage.Contains((GameConstants.ArrowTowerDamage + GameConstants.SkillMaxRanks * GameConstants.SkillArrowDamagePerRank).ToString())).IsTrue();

        string range = SkillTreeCatalog.Description(SkillTreeCatalog.ArrowRange);
        AssertThat(range.Contains("0.5")).IsTrue();
        AssertThat(range.Contains((GameConstants.ArrowTowerRange + GameConstants.SkillMaxRanks * GameConstants.SkillArrowRangePerRank).ToString("0.0"))).IsTrue();

        string speed = SkillTreeCatalog.Description(SkillTreeCatalog.ArrowAttackSpeed);
        AssertThat(speed.Contains("10%")).IsTrue();
        float rankFiveInterval = GameConstants.ArrowTowerFireRate
            / (1f + GameConstants.SkillMaxRanks * GameConstants.SkillArrowAttackSpeedPerRank);
        AssertThat(speed.Contains(rankFiveInterval.ToString("0.0"))).IsTrue();
    }

    [TestCase]
    public void CannonDescriptions_MatchTheTuningNumbers()
    {
        string powder = SkillTreeCatalog.Description(SkillTreeCatalog.CannonPowderCharge);
        AssertThat(powder.Contains("+1 damage")).IsTrue();
        AssertThat(powder.Contains("10%")).IsTrue();
        AssertThat(powder.Contains((GameConstants.CannonTowerDamage + GameConstants.SkillMaxRanks * GameConstants.SkillCannonPowderDamagePerRank).ToString())).IsTrue();

        string splash = SkillTreeCatalog.Description(SkillTreeCatalog.CannonSplashRadius);
        AssertThat(splash.Contains(GameConstants.SkillCannonSplashRadiusPerRank.ToString())).IsTrue();
        AssertThat(splash.Contains((GameConstants.CannonTowerAoeRadius + GameConstants.SkillMaxRanks * GameConstants.SkillCannonSplashRadiusPerRank).ToString())).IsTrue();
    }

    [TestCase]
    public void LaserDescriptions_MatchTheTuningNumbers()
    {
        string dps = SkillTreeCatalog.Description(SkillTreeCatalog.LaserDps);
        AssertThat(dps.Contains("0.8")).IsTrue();
        AssertThat(dps.Contains((GameConstants.LaserTowerDps + GameConstants.SkillMaxRanks * GameConstants.SkillLaserDpsPerRank).ToString("0.#"))).IsTrue();

        string ignite = SkillTreeCatalog.Description(SkillTreeCatalog.LaserIgnite);
        AssertThat(ignite.Contains("5%")).IsTrue();
        AssertThat(ignite.Contains(GameConstants.SkillLaserBurnDps.ToString("0.#"))).IsTrue();
        AssertThat(ignite.Contains(GameConstants.SkillLaserBurnDuration.ToString("0.#"))).IsTrue();

        string chain = SkillTreeCatalog.Description(SkillTreeCatalog.LaserChain);
        AssertThat(chain.Contains(GameConstants.SkillLaserChainRange.ToString("0.0"))).IsTrue();
        AssertThat(chain.Contains("60%")).IsTrue();

        string ramp = SkillTreeCatalog.Description(SkillTreeCatalog.LaserRampUp);
        AssertThat(ramp.Contains("20%")).IsTrue();
        AssertThat(ramp.Contains("+100%")).IsTrue();
    }

    [TestCase]
    public void UnlockHint_NamesThePrerequisiteAndUnlockRank()
    {
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.ArrowDamage)).IsNull();
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.ArrowAttackSpeed))
            .IsEqual("Unlocks after Damage reaches rank 1.");
        AssertThat(SkillTreeCatalog.UnlockHint(SkillTreeCatalog.ArrowPierce))
            .IsEqual("Unlocks after Range reaches rank 1.");
        AssertThat(SkillTreeCatalog.UnlockHint("does.not.exist")).IsNull();
    }

    [TestCase]
    public void Description_ReturnsEmptyForUnknownId()
    {
        AssertThat(SkillTreeCatalog.Description("does.not.exist")).IsEqual(string.Empty);
    }

    [TestCase]
    public void TrunkGlyphs_AreDistinctWithinEachTower()
    {
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.ArrowDamage)).IsEqual(SkillGlyphKind.Blade);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.ArrowAttackSpeed)).IsEqual(SkillGlyphKind.SpeedLines);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.ArrowRange)).IsEqual(SkillGlyphKind.Crosshair);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.CannonPowderCharge)).IsEqual(SkillGlyphKind.Shell);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.CannonAttackSpeed)).IsEqual(SkillGlyphKind.SpeedLines);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.CannonSplashRadius)).IsEqual(SkillGlyphKind.ConcentricRings);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.LaserDps)).IsEqual(SkillGlyphKind.Beam);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.LaserRange)).IsEqual(SkillGlyphKind.BeamCapped);
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.LaserIgnite)).IsEqual(SkillGlyphKind.Flame);

        foreach (var type in new[] { TowerType.Arrow, TowerType.Cannon, TowerType.Laser })
        {
            var kinds = new HashSet<SkillGlyphKind>();
            foreach (var node in SkillTreeCatalog.ForTower(type))
            {
                var kind = SkillGlyphs.For(node.Id);
                if (node.Kind == SkillNodeKind.Trunk)
                {
                    AssertThat(kind).IsNotEqual(SkillGlyphKind.Seed);
                    AssertThat(kinds.Add(kind)).IsTrue();
                }
                else
                {
                    AssertThat(kind).IsEqual(SkillGlyphKind.Seed);
                }
            }
        }
    }

    [TestCase]
    public void SeedGlyphs_ReuseTheMatchingArchetypeEmblems()
    {
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.ArrowPierce)).IsEqual(TowerArchetype.Pierce);
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.ArrowCritChance)).IsEqual(TowerArchetype.Crit);
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.CannonCluster)).IsEqual(TowerArchetype.Cluster);
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.CannonStunChance)).IsEqual(TowerArchetype.Stun);
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.LaserChain)).IsEqual(TowerArchetype.Chain);
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.LaserRampUp)).IsEqual(TowerArchetype.RampUp);

        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.ArrowDamage)).IsNull();
    }

    [TestCase]
    public void SplashRadiusGlyph_IsVisuallyDistinctFromTheStunEmblem()
    {
        // Both use rings; the splash glyph adds a centre dot and a third ring so the
        // two never read as the same mark inside the cannon tree.
        AssertThat(SkillGlyphs.For(SkillTreeCatalog.CannonSplashRadius))
            .IsNotEqual(SkillGlyphs.For(SkillTreeCatalog.CannonStunChance));
        AssertThat(SkillGlyphs.SeedArchetype(SkillTreeCatalog.CannonStunChance))
            .IsEqual(TowerArchetype.Stun);
    }
}
