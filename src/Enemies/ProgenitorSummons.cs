using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Pure Progenitor rules: HP phases, summon cadence/squad size, weighted summon
/// composition, and on-path summon lead placement. Kept free of node state so every
/// rule is unit-testable without a Godot scene; <see cref="Enemy"/> owns the per-boss
/// timers and <see cref="WaveManager"/> owns spawning.
/// </summary>
public static class ProgenitorSummons
{
    public const int FirstPhase = 1;
    public const int FinalPhase = 3;

    /// <summary>
    /// The phase a boss at <paramref name="currentHP"/> of <paramref name="maxHP"/>
    /// belongs to. HP exactly on a threshold belongs to the lower (harder) phase, per
    /// the spec: floor(maxHP * 2 / 3) is phase 2, floor(maxHP / 3) is phase 3.
    /// </summary>
    public static int PhaseFor(float currentHP, float maxHP)
    {
        if (!(maxHP > 0f))
            return FirstPhase;

        if (currentHP > Phase2ThresholdHP(maxHP))
            return 1;
        if (currentHP > Phase3ThresholdHP(maxHP))
            return 2;
        return FinalPhase;
    }

    /// <summary>
    /// HP at which phase 1 ends and phase 2 begins (exclusive upper bound of phase 2's
    /// range). floor(maxHP * 2 / 3), matching the spec's exact-threshold rule.
    /// </summary>
    public static int Phase2ThresholdHP(float maxHP) =>
        ThresholdHP(maxHP, GameConstants.ProgenitorPhase2HPFraction);

    /// <summary>
    /// HP at which phase 2 ends and phase 3 begins. floor(maxHP / 3).
    /// </summary>
    public static int Phase3ThresholdHP(float maxHP) =>
        ThresholdHP(maxHP, GameConstants.ProgenitorPhase3HPFraction);

    /// <summary>
    /// floor(maxHP * fraction): the exact threshold HP, which belongs to the lower
    /// (harder) phase.
    /// </summary>
    private static int ThresholdHP(float maxHP, float fraction) =>
        (int)System.MathF.Floor(maxHP * fraction);

    /// <summary>
    /// Seconds between summons for the given phase.
    /// </summary>
    public static float IntervalForPhase(int phase) => phase switch
    {
        1 => GameConstants.ProgenitorPhase1SummonInterval,
        2 => GameConstants.ProgenitorPhase2SummonInterval,
        _ => GameConstants.ProgenitorPhase3SummonInterval
    };

    /// <summary>
    /// Number of enemies summoned per squad for the given phase. Every slot is exactly
    /// one enemy — a Swarm slot is one small circle, never a 3-member cluster.
    /// </summary>
    public static int SquadSizeForPhase(int phase) => phase switch
    {
        1 => GameConstants.ProgenitorPhase1SquadSize,
        2 => GameConstants.ProgenitorPhase2SquadSize,
        _ => GameConstants.ProgenitorPhase3SquadSize
    };

    /// <summary>
    /// Fixed summon mix for a phase (Basic / Swarm / Armored). Each phase's weights sum
    /// to 1; phase 1 has no Armored slots at all.
    /// </summary>
    public static (double Basic, double Swarm, double Armored) MixWeightsForPhase(int phase) => phase switch
    {
        1 => (GameConstants.ProgenitorPhase1BasicWeight,
              GameConstants.ProgenitorPhase1SwarmWeight,
              0.0),
        2 => (GameConstants.ProgenitorPhase2BasicWeight,
              GameConstants.ProgenitorPhase2SwarmWeight,
              GameConstants.ProgenitorPhase2ArmoredWeight),
        _ => (GameConstants.ProgenitorPhase3BasicWeight,
              GameConstants.ProgenitorPhase3SwarmWeight,
              GameConstants.ProgenitorPhase3ArmoredWeight)
    };

    /// <summary>
    /// Rolls one summoned enemy kind for the given phase against the supplied roll
    /// source. Weights are the spec's fixed per-phase mix; a roll lands in the Basic,
    /// Swarm, or Armored band using cumulative weight boundaries. A phase with no
    /// Armored weight (phase 1) has Swarm as its remainder, so even an out-of-range
    /// roll can never produce an Armored slot there.
    /// </summary>
    public static EnemyKind RollKind(int phase, SkillRandom rolls)
    {
        double roll = rolls.NextDouble();
        var mix = MixWeightsForPhase(phase);

        if (roll < mix.Basic)
            return EnemyKind.Basic;

        if (mix.Armored <= 0.0 || roll < mix.Basic + mix.Swarm)
            return EnemyKind.Swarm;

        return EnemyKind.Armored;
    }

    /// <summary>
    /// The point <paramref name="leadDistance"/> px ahead of <paramref name="anchor"/>
    /// along the segment toward <paramref name="nextWaypoint"/>. The lead point clamps
    /// to the waypoint when the segment is shorter than the lead distance, so a summon
    /// near a corner never overshoots the turn or leaves the route.
    /// </summary>
    public static Vector2 LeadPoint(Vector2 anchor, Vector2 nextWaypoint, float leadDistance)
    {
        Vector2 toWaypoint = nextWaypoint - anchor;
        float segmentLength = toWaypoint.Length();

        if (!(segmentLength > 0.0001f))
            return anchor;

        float lead = Mathf.Clamp(leadDistance, 0f, segmentLength);
        return anchor + toWaypoint / segmentLength * lead;
    }

    /// <summary>
    /// Border color for a phase: white (1) -> amber (2) -> red (3). Pure so the visual
    /// contract is testable and the draw path allocates nothing.
    /// </summary>
    public static Color BorderColorForPhase(int phase) => phase switch
    {
        1 => GameConstants.ProgenitorPhase1BorderColor,
        2 => GameConstants.ProgenitorPhase2BorderColor,
        _ => GameConstants.ProgenitorPhase3BorderColor
    };
}
