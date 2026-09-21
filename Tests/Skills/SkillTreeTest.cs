using GeometryTowerDefense;
using GdUnit4;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Coalesced persistence behavior of the <see cref="SkillTree"/> facade. The real
/// save function is injected so no file I/O is needed: we count calls instead.
/// </summary>
[TestSuite]
public class SkillTreeTest
{
    [TestCase]
    public void AwardSkillPoints_MarksDirtyWithoutPersisting()
    {
        int saves = 0;
        var tree = new SkillTree(new SkillTreeState(), _ => { saves++; return true; });

        tree.AwardSkillPoints(5);
        tree.AwardSkillPoints(8);

        AssertThat(tree.State.SkillPoints).IsEqual(13);
        AssertThat(saves).IsEqual(0); // coalesced — nothing written yet
    }

    [TestCase]
    public void Flush_PersistsOnceForMultipleAwards()
    {
        int saves = 0;
        var tree = new SkillTree(new SkillTreeState(), _ => { saves++; return true; });

        tree.AwardSkillPoints(5);
        tree.AwardSkillPoints(5);
        tree.Flush();

        AssertThat(saves).IsEqual(1);

        tree.Flush(); // clean → no-op
        AssertThat(saves).IsEqual(1);
    }

    [TestCase]
    public void BuyRank_PersistsImmediately()
    {
        int saves = 0;
        var state = new SkillTreeState();
        state.SetSkillPoints(50);
        var tree = new SkillTree(state, _ => { saves++; return true; });

        AssertThat(tree.BuyRank(SkillTreeCatalog.ArrowDamage)).IsTrue();

        AssertThat(saves).IsEqual(1);
        AssertThat(state.GetRank(SkillTreeCatalog.ArrowDamage)).IsEqual(1);
        AssertThat(state.SkillPoints).IsEqual(40);
    }

    [TestCase]
    public void BuyRank_FailedPurchase_DoesNotPersist()
    {
        int saves = 0;
        var tree = new SkillTree(new SkillTreeState(), _ => { saves++; return true; }); // 0 SP

        AssertThat(tree.BuyRank(SkillTreeCatalog.ArrowDamage)).IsFalse();
        AssertThat(saves).IsEqual(0);
    }

    [TestCase]
    public void Flush_RetriesAfterFailedPersist()
    {
        int saves = 0;
        var tree = new SkillTree(new SkillTreeState(), _ =>
        {
            saves++;
            return saves >= 2; // first write fails, second succeeds
        });

        tree.AwardSkillPoints(5);
        tree.Flush();
        AssertThat(saves).IsEqual(1); // failed, still dirty

        tree.Flush();
        AssertThat(saves).IsEqual(2); // retried and succeeded

        tree.Flush();
        AssertThat(saves).IsEqual(2); // clean now
    }
}
