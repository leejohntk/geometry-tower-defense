using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Laser Tower: magenta/purple emitter that drains a single nearest target continuously.
/// No projectile and no cooldown — GameManager applies Dps every frame against the target
/// already computed by its centralized targeting pass. Damage ignores enemy armor.
/// </summary>
public partial class LaserTower : Tower
{
    public override TowerType Type => TowerType.Laser;
    public override int Damage => 0;
    public override float FireRate => 0f;
    public override int Cost => GameConstants.LaserTowerCost;
    public override float Dps => SkillStats.LaserDps(SkillRank(SkillTreeCatalog.LaserDps));
    public override bool IsContinuous => true;
    public override int ChainJumps => SkillMechanics.LaserChainJumps(SkillRank(SkillTreeCatalog.LaserChain));
    public override float IgniteChancePerSecond => SkillMechanics.LaserIgniteChancePerSecond(SkillRank(SkillTreeCatalog.LaserIgnite));
    public override float RampMaxMultiplier => SkillMechanics.LaserRampMaxMultiplier(SkillRank(SkillTreeCatalog.LaserRampUp));
    protected override Color RangeColor => new Color(0.8f, 0.3f, 1.0f);

    private Line2D? _beam;
    private Line2D? _chainBeam;
    private readonly List<Vector2> _chainTargets = new();
    private Vector2[] _chainBeamBuffer = System.Array.Empty<Vector2>();

    // Continuous-beam state: the held target (plus its identity generation), ramp
    // progress, and the ignite-roll accumulator. All reset together when the held
    // target changes, is reissued from the pool, or contact ends.
    private Enemy? _heldTarget;
    private int _heldTargetGeneration = 0;
    private float _rampTime = 0f;
    private float _igniteAccumulator = 0f;

    // Latest dps multiplier from UpdateRamp, mirrored here so the beam visual can read
    // it without recomputing the ramp. 1.0 (un-ramped) whenever contact starts or ends.
    private float _rampMultiplier = 1f;

    /// <summary>
    /// Clears the continuous-beam state (called every frame the beam is not in
    /// contact with a valid target, so ramp and ignite restart on re-contact).
    /// </summary>
    public void ResetBeam()
    {
        _heldTarget = null;
        _heldTargetGeneration = 0;
        _rampTime = 0f;
        _igniteAccumulator = 0f;
        _rampMultiplier = 1f;
    }

    /// <summary>
    /// Advances ramp state for the given target and returns the current dps multiplier.
    /// Holding the same target ramps 1.0 → RampMaxMultiplier over
    /// <see cref="GameConstants.SkillLaserRampTime"/>; switching targets (including a
    /// pooled enemy reissued under the same reference) resets to 1.0 and also resets
    /// the ignite accumulator.
    /// </summary>
    public float UpdateRamp(Enemy target, float delta)
    {
        if (!ReferenceEquals(target, _heldTarget) || target.Generation != _heldTargetGeneration)
        {
            _heldTarget = target;
            _heldTargetGeneration = target.Generation;
            _rampTime = 0f;
            _igniteAccumulator = 0f;
        }

        _rampTime += delta;
        float progress = Mathf.Clamp(_rampTime / GameConstants.SkillLaserRampTime, 0f, 1f);
        _rampMultiplier = 1f + (RampMaxMultiplier - 1f) * progress;
        return _rampMultiplier;
    }

    /// <summary>
    /// Normalized ramp progress for the beam visual: 0 at the base 1.0x multiplier,
    /// 1 at the tower's max multiplier, clamped in between. A tower with no ramp nodes
    /// (max 1.0x) or a non-finite multiplier reads as 0, i.e. the un-ramped beam.
    /// Pure so the beam scaling is testable without a scene.
    /// </summary>
    public static float RampProgress(float rampMultiplier, float maxMultiplier)
    {
        if (!float.IsFinite(rampMultiplier) || !float.IsFinite(maxMultiplier) || maxMultiplier <= 1f)
            return 0f;

        return Mathf.Clamp((rampMultiplier - 1f) / (maxMultiplier - 1f), 0f, 1f);
    }

    /// <summary>
    /// Accumulates continuous contact time and returns how many whole-second ignite
    /// rolls are due this frame (0 or more). Bounded by elapsed seconds, never by
    /// frame count: 60 frames at 1/60s produce exactly one roll, as does a single
    /// 1.0s frame.
    /// </summary>
    public int ConsumeIgniteRolls(float delta)
    {
        // Guard against non-finite/negative deltas poisoning the accumulator: a NaN
        // would disable ignite permanently and a negative delta would suppress rolls.
        if (!(delta > 0f) || !float.IsFinite(delta))
            return 0;

        _igniteAccumulator += delta;
        int rolls = (int)System.MathF.Floor(_igniteAccumulator);
        if (rolls > 0)
            _igniteAccumulator -= rolls;
        return rolls;
    }

    /// <summary>
    /// Stores the chain jump target positions (world space) for this frame's chain
    /// beam visual. A pre-warmed list is reused; the call itself allocates nothing
    /// per frame once capacity has been reached.
    /// </summary>
    public void SetChainTargets(IReadOnlyList<Vector2> targets)
    {
        _chainTargets.Clear();
        for (int i = 0; i < targets.Count; i++)
            _chainTargets.Add(targets[i]);
    }

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4;

        // Magenta/purple emitter body.
        var body = new Control();
        body.Name = "LaserEmitter";
        body.Size = new Vector2(cell, cell);
        body.Position = new Vector2(-cell / 2f, -cell / 2f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.Draw += () =>
        {
            if (!IsInstanceValid(body)) return;

            Vector2 center = new Vector2(cell / 2f, cell / 2f);

            body.DrawCircle(center, radius, new Color(0.8f, 0.3f, 1.0f));            // Magenta fill
            body.DrawCircle(center, radius, new Color(0.4f, 0.1f, 0.6f), false, 2.0f); // Darker outline
            body.DrawCircle(center, radius * 0.4f, new Color(0.5f, 0.15f, 0.8f));    // Inner core
        };
        AddChild(body);

        // Beam line drawn from the tower center to the current target.
        _beam = new Line2D();
        _beam.Name = "Beam";
        _beam.Width = GameConstants.LaserBeamWidthMin;
        _beam.DefaultColor = GameConstants.LaserBeamColorMin;
        _beam.Points = new Vector2[] { Vector2.Zero, Vector2.Zero };
        _beam.Visible = false;
        AddChild(_beam);

        // Thin magenta polyline from the primary target through each chain jump.
        // The points buffer is pre-allocated to the largest possible chain (primary +
        // max jumps) so the per-frame update never allocates.
        _chainBeam = new Line2D();
        _chainBeam.Name = "ChainBeam";
        _chainBeam.Width = GameConstants.LaserChainBeamWidthMin;
        _chainBeam.DefaultColor = GameConstants.LaserChainBeamColorMin;
        _chainBeam.Visible = false;
        AddChild(_chainBeam);
        _chainBeamBuffer = new Vector2[GameConstants.SkillMaxRanks * GameConstants.SkillLaserChainPerRank + 1];
        _chainTargets.Capacity = GameConstants.SkillMaxRanks;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_beam == null)
            return;

        var target = CurrentTarget;
        if (target != null && !target.IsDead && IsTargetInRange(target))
        {
            // Beam endpoints are in this node's local space; the tower is at its origin.
            _beam.SetPointPosition(0, Vector2.Zero);
            _beam.SetPointPosition(1, target.Position - Position);
            ApplyBeamRamp();
            _beam.Visible = true;
        }
        else
        {
            _beam.Visible = false;
        }

        UpdateChainBeam();
    }

    /// <summary>
    /// Scales both beams' width and color with the current ramp multiplier
    /// (1.0x → the tower's max), so the dps climb is visible. At 1.0x this is exactly
    /// the un-ramped beam look. Line2D's setters are no-ops when the value is unchanged
    /// and neither Lerp allocates, so this is free to run every frame the beam is up.
    /// </summary>
    private void ApplyBeamRamp()
    {
        float ramp = RampProgress(_rampMultiplier, RampMaxMultiplier);

        if (_beam != null)
        {
            _beam.Width = Mathf.Lerp(
                GameConstants.LaserBeamWidthMin, GameConstants.LaserBeamWidthMax, ramp);
            _beam.DefaultColor = GameConstants.LaserBeamColorMin
                .Lerp(GameConstants.LaserBeamColorMax, ramp);
        }

        if (_chainBeam != null)
        {
            _chainBeam.Width = Mathf.Lerp(
                GameConstants.LaserChainBeamWidthMin, GameConstants.LaserChainBeamWidthMax, ramp);
            _chainBeam.DefaultColor = GameConstants.LaserChainBeamColorMin
                .Lerp(GameConstants.LaserChainBeamColorMax, ramp);
        }
    }

    /// <summary>
    /// Rebuilds the chain polyline from the current target through the stored jump
    /// positions. Trailing unused points are collapsed onto the last used point so
    /// they draw zero-length (invisible) segments; the buffer is reused every frame.
    /// </summary>
    private void UpdateChainBeam()
    {
        if (_chainBeam == null)
            return;

        var target = CurrentTarget;
        if (target == null || target.IsDead || !IsTargetInRange(target) || _chainTargets.Count == 0)
        {
            _chainBeam.Visible = false;
            return;
        }

        int maxPoints = _chainBeamBuffer.Length;
        int used = System.Math.Min(_chainTargets.Count + 1, maxPoints);

        _chainBeamBuffer[0] = target.Position - Position;
        for (int i = 0; i < used - 1; i++)
            _chainBeamBuffer[i + 1] = _chainTargets[i] - Position;

        Vector2 last = _chainBeamBuffer[used - 1];
        for (int i = used; i < maxPoints; i++)
            _chainBeamBuffer[i] = last;

        _chainBeam.Points = _chainBeamBuffer;
        _chainBeam.Visible = true;
    }
}
