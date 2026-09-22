using System;

namespace GeometryTowerDefense;

/// <summary>
/// Game-side facade over a <see cref="SkillTreeState"/> plus its persistence.
///
/// Save trigger: dirty-flag coalescing. Mutations mark the state dirty rather than
/// writing immediately. <see cref="Flush"/> (called once per frame from GameManager's
/// update loop, and on the game-over/victory transitions) writes <c>user://skilltree.cfg</c>
/// at most once per frame. Rank purchases (<see cref="BuyRank"/>) flush immediately
/// because they happen on the skill-tree screen outside the game loop and are rare
/// user actions, never per-frame.
///
/// This keeps the durability guarantee — SP and ranks survive level replay, game over,
/// victory, and app restart — without a synchronous full-file write per enemy kill
/// (a cannon AoE blast can destroy several enemies in a single frame).
/// </summary>
public sealed class SkillTree
{
    private readonly Func<SkillTreeState, bool> _persist;
    private bool _dirty;

    public SkillTreeState State { get; }

    public SkillTree(SkillTreeState state)
        : this(state, SkillTreeSave.Save)
    {
    }

    public SkillTree(SkillTreeState state, Func<SkillTreeState, bool> persist)
    {
        State = state;
        _persist = persist;
    }

    /// <summary>
    /// Awards Skill Points (ignores non-positive amounts). Marks the state dirty for
    /// a later coalesced flush; does not write immediately.
    /// </summary>
    public void AwardSkillPoints(int amount)
    {
        if (amount <= 0)
            return;
        State.AddSkillPoints(amount);
        _dirty = true;
    }

    /// <summary>
    /// Buys one rank for the given node. On success the change is persisted
    /// immediately (purchases happen outside the game loop and are user-initiated).
    /// </summary>
    public bool BuyRank(string nodeId)
    {
        if (!State.BuyRank(nodeId))
            return false;

        _dirty = true;
        Flush();
        return true;
    }

    /// <summary>
    /// Writes the state to disk if it has changed since the last flush. No-op when
    /// clean. A failed write leaves the dirty flag set so a later flush retries.
    /// </summary>
    public void Flush()
    {
        if (!_dirty)
            return;

        if (_persist(State))
            _dirty = false;
    }
}
