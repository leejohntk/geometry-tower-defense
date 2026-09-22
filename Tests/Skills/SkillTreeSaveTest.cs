using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Save/load round-trip via an in-memory <see cref="ConfigFile"/> (no file I/O,
/// so the user:// save file is never touched by tests).
/// </summary>
[TestSuite]
public class SkillTreeSaveTest
{
    [TestCase]
    public void RoundTrip_PreservesSkillPointsAndRanks()
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(37);
        state.SetRank(SkillTreeCatalog.ArrowDamage, 3);
        state.SetRank(SkillTreeCatalog.ArrowRange, 1);
        state.SetRank(SkillTreeCatalog.CannonPowderCharge, 5);
        state.SetRank(SkillTreeCatalog.LaserDps, 2);

        var loaded = SkillTreeSave.FromConfigFile(SkillTreeSave.ToConfigFile(state));

        AssertThat(loaded.SkillPoints).IsEqual(37);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(3);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowRange)).IsEqual(1);
        AssertThat(loaded.GetRank(SkillTreeCatalog.CannonPowderCharge)).IsEqual(5);
        AssertThat(loaded.GetRank(SkillTreeCatalog.LaserDps)).IsEqual(2);
        // Untouched nodes round-trip to 0.
        AssertThat(loaded.GetRank(SkillTreeCatalog.LaserRange)).IsEqual(0);
    }

    [TestCase]
    public void RoundTrip_DefaultState_IsAllZeroes()
    {
        var loaded = SkillTreeSave.FromConfigFile(SkillTreeSave.ToConfigFile(new SkillTreeState()));

        AssertThat(loaded.SkillPoints).IsEqual(0);
        foreach (var node in SkillTreeCatalog.All)
            AssertThat(loaded.GetRank(node.Id)).IsEqual(0);
    }

    [TestCase]
    public void FromEmptyConfig_ReturnsDefaults()
    {
        var loaded = SkillTreeSave.FromConfigFile(new ConfigFile());

        AssertThat(loaded.SkillPoints).IsEqual(0);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(0);
    }

    [TestCase]
    public void FromConfig_ClampsOutOfRangeValues()
    {
        var config = new ConfigFile();
        config.SetValue("skilltree", "sp", 9999);
        config.SetValue("skilltree", SkillTreeCatalog.ArrowDamage, 99);

        var loaded = SkillTreeSave.FromConfigFile(config);

        AssertThat(loaded.SkillPoints).IsEqual(9999);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(GameConstants.SkillMaxRanks);
    }

    [TestCase]
    public void FromConfig_RejectsNonIntValues()
    {
        var config = new ConfigFile();
        config.SetValue("skilltree", "sp", "garbage");
        config.SetValue("skilltree", SkillTreeCatalog.ArrowDamage, 3.5f);

        var loaded = SkillTreeSave.FromConfigFile(config);

        AssertThat(loaded.SkillPoints).IsEqual(0);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(0);
    }

    [TestCase]
    public void FromConfig_RejectsDisabledNodeRanks()
    {
        var config = new ConfigFile();
        config.SetValue("skilltree", SkillTreeCatalog.LaserChain, 3);  // disabled mechanic node
        config.SetValue("skilltree", SkillTreeCatalog.ArrowDamage, 2); // enabled stat node

        var loaded = SkillTreeSave.FromConfigFile(config);

        AssertThat(loaded.GetRank(SkillTreeCatalog.LaserChain)).IsEqual(0);
        AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(2);
    }

    [TestCase]
    public void SaveLoad_FileRoundTrip_PersistsToDisk()
    {
        // Unique per run so concurrent `dotnet test` processes never race on the
        // same file (or its .tmp sibling) during the atomic write.
        string path = $"user://skilltree_test_roundtrip_{System.Guid.NewGuid():N}.cfg";
        try
        {
            var state = new SkillTreeState();
            state.SetSkillPoints(42);
            state.SetRank(SkillTreeCatalog.ArrowDamage, 3);
            state.SetRank(SkillTreeCatalog.LaserDps, 1);

            AssertThat(SkillTreeSave.Save(state, path)).IsTrue();

            var loaded = SkillTreeSave.Load(path);
            AssertThat(loaded.SkillPoints).IsEqual(42);
            AssertThat(loaded.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(3);
            AssertThat(loaded.GetRank(SkillTreeCatalog.LaserDps)).IsEqual(1);

            // Successful atomic write leaves no temp file behind.
            AssertThat(FileAccess.FileExists(path + ".tmp")).IsFalse();
        }
        finally
        {
            DirAccess.RemoveAbsolute(path);
            DirAccess.RemoveAbsolute(path + ".tmp");
        }
    }
}
