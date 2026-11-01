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
    public override float Damage => 0f;
    public override float FireRate => 0f;
    public override int Cost => GameConstants.LaserTowerCost;

    /// <summary>
    /// Continuous DPS: the skill-tree DPS scaled by the per-rank fire-rate bonus (the
    /// laser has no fire interval, so the interval divisor applies to DPS as a
    /// multiplier instead).
    /// </summary>
    public override float Dps =>
        SkillStats.LaserDps(SkillRank(SkillTreeCatalog.LaserDps))
        * TowerUpgrades.FireRateMultiplier(Rank);
    public override bool IsContinuous => true;

    /// <summary>
    /// Beam jumps. Seed mechanics are inert before rank 3 (zero jumps). After
    /// choosing Chain it is max(1, skill rank); after choosing Ramp-Up it is zero.
    /// </summary>
    public override int ChainJumps =>
        SkillMechanics.LaserChainJumps(
            SeedRankFor(TowerArchetype.Chain, SkillTreeCatalog.LaserChain));

    /// <summary>
    /// Per-second ignite chance. Ignite is a trunk node, so it keeps applying
    /// regardless of the chosen archetype.
    /// </summary>
    public override float IgniteChancePerSecond => SkillMechanics.LaserIgniteChancePerSecond(SkillRank(SkillTreeCatalog.LaserIgnite));

    /// <summary>
    /// Maximum ramp multiplier. Inert before rank 3 (1.0x). After choosing Ramp-Up it
    /// is 1 + max(1, skill rank) x 0.20; after choosing Chain it stays 1.0x.
    /// </summary>
    public override float RampMaxMultiplier =>
        SkillMechanics.LaserRampMaxMultiplier(
            SeedRankFor(TowerArchetype.RampUp, SkillTreeCatalog.LaserRampUp));

    protected override Color RangeColor => GameConstants.LaserTowerFillColor;

    /// <summary>
    /// The emitter's aim pointer is drawn along +X, so its resting aim points right.
    /// </summary>
    public override float DefaultAimAngle => 0f;

    /// <summary>
    /// True while the primary impact glow is visible (a live beam is hitting a target).
    /// Presentation only; exposes the glow state without reading pixels.
    /// </summary>
    public bool IsImpactGlowVisible => _impactGlow?.Visible ?? false;

    /// <summary>Number of visible chain impact glows right now.</summary>
    public int VisibleChainGlowCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _chainGlows.Length; i++)
            {
                if (_chainGlows[i] != null && _chainGlows[i].Visible)
                    count++;
            }
            return count;
        }
    }

    private Line2D? _beam;
    private Line2D? _chainBeam;
    private readonly List<Vector2> _chainTargets = new();
    private Vector2[] _chainBeamBuffer = System.Array.Empty<Vector2>();

    // The tower's own procedural emitter body. Its drawing branches on the chosen
    // archetype (linked rim nodes or a rising wedge core); no separate overlay.
    private Control? _body;
    private Vector2 _bodyBasePosition;

    // Laser impact glows: one ring at the primary beam endpoint and a small one at
    // each chain endpoint. Their positions/modulate are written each frame the beam is
    // up (transform/property only); they are never drawn while idle.
    private Control? _impactGlow;
    private Control[] _chainGlows = System.Array.Empty<Control>();
    private float _pulsePhase = 0f;
    private bool _emitterPulseActive = false;

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

        // Magenta/purple emitter body.
        _bodyBasePosition = new Vector2(-cell / 2f, -cell / 2f);
        _body = new Control();
        _body.Name = "LaserEmitter";
        _body.Size = new Vector2(cell, cell);
        _body.Position = _bodyBasePosition;
        // Rotate around the cell centre so the aim pointer reflects the target.
        _body.PivotOffset = new Vector2(cell / 2f, cell / 2f);
        _body.MouseFilter = Control.MouseFilterEnum.Ignore;
        _body.Draw += DrawBody;
        AddChild(_body);

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

        CreateImpactGlows();
        OnAimUpdated();
    }

    /// <summary>
    /// Creates the primary impact glow and the fixed pool of chain impact glows. All
    /// controls are click-through decorations; only their transform/modulate changes
    /// while the beam is up.
    /// </summary>
    private void CreateImpactGlows()
    {
        _impactGlow = CreateGlowControl(
            "ImpactGlow",
            GameConstants.LaserImpactGlowRadius,
            GameConstants.LaserImpactGlowStroke);
        AddChild(_impactGlow);

        var chainGlowRoot = new Control();
        chainGlowRoot.Name = "ChainImpactGlows";
        chainGlowRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(chainGlowRoot);

        int maxChain = GameConstants.SkillMaxRanks * GameConstants.SkillLaserChainPerRank;
        _chainGlows = new Control[maxChain];
        float chainRadius = GameConstants.LaserImpactGlowRadius * GameConstants.LaserChainImpactGlowScale;
        for (int i = 0; i < maxChain; i++)
        {
            var glow = CreateGlowControl(
                $"ChainImpact_{i}",
                chainRadius,
                GameConstants.LaserImpactGlowStroke * GameConstants.LaserChainImpactGlowScale);
            chainGlowRoot.AddChild(glow);
            _chainGlows[i] = glow;
        }
    }

    /// <summary>
    /// Builds one glow control: a ring plus a bright core, sized to fit the ring with
    /// room for its stroke. Drawing runs only when the control first enters the tree,
    /// so a glow never repaints per frame.
    /// </summary>
    private static Control CreateGlowControl(string name, float radius, float stroke)
    {
        float size = (radius + stroke) * 2f + 4f;
        var glow = new Control();
        glow.Name = name;
        glow.Size = new Vector2(size, size);
        glow.MouseFilter = Control.MouseFilterEnum.Ignore;
        glow.ZIndex = 2;
        glow.Visible = false;
        glow.Draw += () =>
        {
            if (!IsInstanceValid(glow)) return;
            var center = new Vector2(size / 2f, size / 2f);
            glow.DrawCircle(center, radius, GameConstants.LaserImpactGlowColor with { A = 0.55f }, false, stroke);
            glow.DrawCircle(center, radius * 0.4f, GameConstants.LaserImpactGlowColor, true);
        };
        return glow;
    }

    protected override void OnArchetypeChanged()
    {
        _body?.QueueRedraw();
    }

    /// <summary>
    /// Paints the emitter silhouette for the current archetype:
    /// none = magenta circle with outline and core; Chain = circle with three linked
    /// rim nodes and a small core; Ramp-Up = circle with a rising wedge core.
    /// </summary>
    private void DrawBody()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4f;
        Vector2 center = new(cell / 2f, cell / 2f);
        Color fill = GameConstants.LaserTowerFillColor;
        Color outline = GameConstants.LaserTowerOutlineColor;

        switch (Archetype)
        {
            case TowerArchetype.Chain:
                DrawChainBody(center, radius, fill, outline);
                break;
            case TowerArchetype.RampUp:
                _body.DrawCircle(center, radius, fill);
                _body.DrawCircle(center, radius, outline, false, 2f);
                // Rising wedge core: a right triangle climbing from bottom-left to top-right.
                var wedge = new[]
                {
                    center + new Vector2(-radius * 0.5f, radius * 0.5f),
                    center + new Vector2(radius * 0.5f, radius * 0.5f),
                    center + new Vector2(radius * 0.5f, -radius * 0.5f)
                };
                _body.DrawColoredPolygon(wedge, new Color(0.55f, 0.18f, 0.9f));
                break;
            default:
                _body.DrawCircle(center, radius, fill);                             // Magenta fill
                _body.DrawCircle(center, radius, outline, false, 2f);               // Darker outline
                _body.DrawCircle(center, radius * 0.4f, new Color(0.5f, 0.15f, 0.8f)); // Inner core
                break;
        }

        DrawAimPointer(center, radius);
    }

    /// <summary>
    /// Small nib at the emitter's rim along +X. The body's Rotation (applied every
    /// frame in OnAimUpdated) aims it at the current target.
    /// </summary>
    private void DrawAimPointer(Vector2 center, float radius)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        var pointer = new[]
        {
            center + new Vector2(radius * 1.02f, 0f),
            center + new Vector2(radius * 0.5f, -radius * 0.22f),
            center + new Vector2(radius * 0.5f, radius * 0.22f)
        };
        _body.DrawColoredPolygon(pointer, new Color(0.95f, 0.75f, 1f));
    }

    /// <summary>
    /// Emitter aim: the body rotates so its +X pointer faces the target. Recoil does
    /// not apply to the continuous laser; its firing cue is the emitter pulse.
    /// </summary>
    protected override void OnAimUpdated()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        _body.Rotation = AimAngle - DefaultAimAngle;
    }

    /// <summary>
    /// Chain: circle body with three small rim nodes (top, lower-left, lower-right)
    /// joined by thin lines, plus a small central core.
    /// </summary>
    private void DrawChainBody(Vector2 center, float radius, Color fill, Color outline)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        _body.DrawCircle(center, radius, fill);
        _body.DrawCircle(center, radius, outline, false, 2f);

        float rim = radius * 0.78f;
        float nodeRadius = radius * 0.16f;
        var link = new Color(0.75f, 0.45f, 1f);
        var nodes = new Vector2[3];
        for (int i = 0; i < nodes.Length; i++)
        {
            float angle = Mathf.Tau * i / nodes.Length - Mathf.Pi / 2f;
            nodes[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rim;
        }

        _body.DrawLine(nodes[0], nodes[1], link, 1.5f);
        _body.DrawLine(nodes[1], nodes[2], link, 1.5f);
        _body.DrawLine(nodes[2], nodes[0], link, 1.5f);
        foreach (var node in nodes)
            _body.DrawCircle(node, nodeRadius, link);

        _body.DrawCircle(center, radius * 0.28f, new Color(0.5f, 0.15f, 0.8f));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_beam == null)
            return;

        var target = CurrentTarget;
        bool beamActive = target != null && !target.IsDead && IsTargetInRange(target);
        if (beamActive)
        {
            // Beam endpoints are in this node's local space; the tower is at its origin.
            _beam.SetPointPosition(0, Vector2.Zero);
            _beam.SetPointPosition(1, target!.Position - Position);
            ApplyBeamRamp();
            _beam.Visible = true;
        }
        else
        {
            _beam.Visible = false;
        }

        UpdateChainBeam();
        UpdateImpactGlows(beamActive ? target : null);
        UpdateEmitterPulse((float)delta, beamActive);
    }

    /// <summary>
    /// Positions the primary and chain impact glows at the live beam endpoints. A dead
    /// or out-of-range target hides them immediately (no stranded ring), and extra
    /// chain glows hide when the chain shrinks. Each frame writes transform/modulate
    /// only — the rings themselves are drawn once.
    /// </summary>
    private void UpdateImpactGlows(Enemy? target)
    {
        bool active = target != null;
        float ramp = RampProgress(_rampMultiplier, RampMaxMultiplier);
        float alpha = Mathf.Lerp(
            GameConstants.LaserImpactGlowAlphaMin,
            GameConstants.LaserImpactGlowAlphaMax,
            ramp);

        if (_impactGlow != null)
        {
            _impactGlow.Visible = active;
            if (active)
            {
                Vector2 local = target!.Position - Position;
                _impactGlow.Position = local - _impactGlow.Size / 2f;
                _impactGlow.Modulate = new Color(1f, 1f, 1f, alpha);
            }
        }

        int chainCount = active ? _chainTargets.Count : 0;
        for (int i = 0; i < _chainGlows.Length; i++)
        {
            var glow = _chainGlows[i];
            if (glow == null)
                continue;

            bool used = i < chainCount;
            glow.Visible = used;
            if (!used)
                continue;

            Vector2 local = _chainTargets[i] - Position;
            glow.Position = local - glow.Size / 2f;
            glow.Modulate = new Color(1f, 1f, 1f, alpha * 0.85f);
        }
    }

    /// <summary>
    /// Subtle emitter scale pulse while the beam is active (transform only). Stops and
    /// resets the moment the beam ends, so no residual pulse lingers.
    /// </summary>
    private void UpdateEmitterPulse(float delta, bool beamActive)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        if (!beamActive)
        {
            // Reset once on the active -> inactive transition; after that the idle
            // emitter writes nothing per frame.
            if (_emitterPulseActive || _body.Scale != Vector2.One)
            {
                _emitterPulseActive = false;
                _pulsePhase = 0f;
                _body.Scale = Vector2.One;
            }
            return;
        }

        _emitterPulseActive = true;
        _pulsePhase += delta;
        float scale = 1f + GameConstants.LaserEmitterPulseAmplitude
            * Mathf.Sin(_pulsePhase * GameConstants.LaserEmitterPulseSpeed);
        _body.Scale = new Vector2(scale, scale);
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
