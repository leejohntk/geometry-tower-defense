using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Identifies an enemy variant. Basic is a red circle; Swarm is a smaller orange circle;
/// Armored is a grey square (diamond) with flat damage reduction.
/// </summary>
public enum EnemyKind
{
    Basic,
    Swarm,
    Armored
}

/// <summary>
/// Enemy that follows a path of waypoints from left to right.
/// Movement and damage logic are shared across kinds; stats and visuals are set via Configure.
/// </summary>
public partial class Enemy : Node2D
{
    [Signal]
    public delegate void ReachedEndEventHandler(Enemy enemy);

    [Signal]
    public delegate void DestroyedEventHandler(Enemy enemy);

    private List<Vector2> _waypoints = new();
    private int _currentWaypointIndex = 0;
    // The path anchor is the point on the waypoint path that this enemy follows.
    // Position is always anchor + FormationOffset, so a formation offset survives
    // waypoint arrival snaps and stays constant for the entire path.
    private Vector2 _anchorPosition = Vector2.Zero;
    private Vector2 _formationOffset = Vector2.Zero;
    private float _formationAngularSpeed = 0f;
    private float _currentHP;
    private float _maxHP;
    private float _speed;
    private int _armor;
    private int _diameter;
    private Color _fillColor;
    private Color _borderColor;
    private bool _isDead = false;
    private Control? _circleContainer;

    // Status state (Part 2). Stun pauses movement; burn is a damage-over-time that
    // ignores armor; hit flash is a brief crit brightening. Timers tick in the
    // centralized status pass (GameManager) — never in _Process — and are reset on
    // pool reuse.
    private float _stunRemaining = 0f;
    private float _burnRemaining = 0f;
    private float _burnDps = 0f;
    private float _hitFlashRemaining = 0f;

    // Burn presentation: a hot flash on proc, plus a quantized pulse phase/step for the
    // tint while the DoT drains. The step (not the phase) is what the draw reads, so the
    // burning tint only redraws when the quantized level changes — see TickStatuses.
    private float _burnFlashRemaining = 0f;
    private float _burnPulsePhase = 0f;
    private int _burnPulseStep = 0;

    // Cached armored geometry + fill-color array so the per-frame redraw allocates
    // nothing. The diamond/inset points depend on _diameter and are rebuilt in
    // UpdateVisual (which runs on Configure); the color array is reused and its
    // single element reassigned on every draw.
    private Vector2[] _diamondPoints = System.Array.Empty<Vector2>();
    private Vector2[] _insetPoints = System.Array.Empty<Vector2>();
    private readonly Color[] _fillColorArray = new Color[1];

    /// <summary>
    /// The kind this enemy is currently configured as.
    /// </summary>
    public EnemyKind Kind { get; private set; } = EnemyKind.Basic;

    public bool IsDead => _isDead;

    /// <summary>
    /// Monotonically increasing identity generation, incremented on every pool reset.
    /// Long-lived consumers (e.g. a laser's ramp/ignite state) use it to detect that a
    /// pooled object was reissued as a brand-new enemy even though the reference is
    /// identical (the pool is LIFO).
    /// </summary>
    public int Generation { get; private set; }

    /// <summary>
    /// Flat damage reduction applied to each incoming hit unless the attack ignores armor.
    /// </summary>
    public int Armor => _armor;

    /// <summary>
    /// Current HP (read-only). Useful for tests that verify exact damage application.
    /// </summary>
    public float CurrentHP => _currentHP;

    /// <summary>
    /// Coin value awarded when this enemy is destroyed.
    /// </summary>
    public int CoinDrop { get; private set; } = GameConstants.CoinDropPerKill;

    /// <summary>
    /// Collision radius in pixels (half of the rendered diameter).
    /// </summary>
    public float CollisionRadius => _diameter / 2f;

    /// <summary>
    /// Rendered diameter in pixels.
    /// </summary>
    public float Diameter => _diameter;

    /// <summary>
    /// True while this enemy is stunned (movement paused).
    /// </summary>
    public bool IsStunned => _stunRemaining > 0f;

    /// <summary>
    /// Seconds of stun remaining.
    /// </summary>
    public float StunRemaining => _stunRemaining;

    /// <summary>
    /// True while this enemy is burning (a laser ignite DoT is active).
    /// </summary>
    public bool IsBurning => _burnRemaining > 0f;

    /// <summary>
    /// Seconds of burn remaining.
    /// </summary>
    public float BurnRemaining => _burnRemaining;

    /// <summary>
    /// Seconds of burn-proc flash remaining. Non-zero for a brief moment after a burn
    /// is applied, so the proc itself is visible.
    /// </summary>
    public float BurnFlashRemaining => _burnFlashRemaining;

    /// <summary>
    /// Current quantized burn-pulse step (0..BurnPulseSteps-1). The burning tint is a
    /// pure function of this step, so it only changes when the step does.
    /// </summary>
    public int BurnPulseStep => _burnPulseStep;

    /// <summary>
    /// Constant offset from the path anchor applied to Position every tick.
    /// Zero for basic enemies; non-zero for swarm cluster members.
    /// </summary>
    public Vector2 FormationOffset => _formationOffset;

    /// <summary>
    /// Angular speed (radians/sec) at which FormationOffset orbits the anchor.
    /// Zero for basic enemies; non-zero for swarm cluster members.
    /// </summary>
    public float FormationAngularSpeed => _formationAngularSpeed;

    public override void _Ready()
    {
        ApplyConfig(EnemyKind.Basic);
        CreateVisual();
    }

    private void ApplyConfig(EnemyKind kind)
    {
        Kind = kind;

        switch (kind)
        {
            case EnemyKind.Basic:
                _maxHP = GameConstants.EnemyHP;
                _armor = 0;
                _speed = GameConstants.EnemySpeed;
                _diameter = GameConstants.EnemyDiameter;
                _fillColor = new Color(0.9f, 0.1f, 0.1f);   // Red fill
                _borderColor = new Color(0.8f, 0.05f, 0.05f); // Darker red border
                CoinDrop = GameConstants.CoinDropPerKill;
                break;

            case EnemyKind.Swarm:
                _maxHP = GameConstants.SwarmEnemyHP;
                _armor = 0;
                _speed = GameConstants.SwarmEnemySpeed;
                _diameter = GameConstants.SwarmEnemyDiameter;
                _fillColor = new Color(1f, 0.55f, 0.1f);     // Orange fill
                _borderColor = new Color(0.85f, 0.4f, 0.05f); // Darker orange border
                CoinDrop = GameConstants.SwarmCoinDropPerKill;
                break;

            case EnemyKind.Armored:
                _maxHP = GameConstants.ArmoredEnemyHP;
                _armor = GameConstants.ArmoredEnemyArmor;
                _speed = GameConstants.ArmoredEnemySpeed;
                _diameter = GameConstants.ArmoredEnemyDiameter;
                _fillColor = new Color(0.6f, 0.6f, 0.65f);    // Grey fill
                _borderColor = new Color(0.3f, 0.3f, 0.35f);  // Darker grey border
                CoinDrop = GameConstants.ArmoredCoinDropPerKill;
                break;
        }

        _currentHP = _maxHP;
        _isDead = false;
    }

    /// <summary>
    /// Configures this enemy for a specific kind. Idempotent and safe to call on pooled reuse.
    /// </summary>
    public void Configure(EnemyKind kind)
    {
        ApplyConfig(kind);
        UpdateVisual();
    }

    private void CreateVisual()
    {
        _circleContainer = new Control();
        _circleContainer.Name = "CircleContainer";
        _circleContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_circleContainer);
        _circleContainer.Draw += () => DrawEnemyShape(_circleContainer);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Geometry is diameter-dependent; rebuild on configure (not per draw).
        RebuildArmoredGeometry();

        if (_circleContainer == null) return;

        float radius = _diameter / 2f;
        _circleContainer.Position = new Vector2(-radius, -radius);
        _circleContainer.Size = new Vector2(_diameter, _diameter);
        _circleContainer.QueueRedraw();
    }

    /// <summary>
    /// Rebuilds the cached armored diamond/inset vertex arrays from the current
    /// diameter. No-op for non-armored kinds.
    /// </summary>
    private void RebuildArmoredGeometry()
    {
        if (Kind != EnemyKind.Armored)
            return;

        float diameter = _diameter;
        Vector2 center = new Vector2(diameter / 2f, diameter / 2f);

        _diamondPoints = new Vector2[]
        {
            new Vector2(center.X, 0f),          // top
            new Vector2(diameter, center.Y),    // right
            new Vector2(center.X, diameter),    // bottom
            new Vector2(0f, center.Y)           // left
        };

        _insetPoints = new Vector2[]
        {
            new Vector2(center.X, 2f),
            new Vector2(diameter - 2f, center.Y),
            new Vector2(center.X, diameter - 2f),
            new Vector2(2f, center.Y),
            new Vector2(center.X, 2f)
        };
    }

    private void DrawEnemyShape(Control container)
    {
        if (!IsInstanceValid(container)) return;

        float radius = _diameter / 2f;
        Vector2 center = new Vector2(_diameter / 2f, _diameter / 2f);
        Color fill = EffectiveFillColor();

        if (Kind == EnemyKind.Armored)
        {
            // Grey square rendered as a diamond (rotated 45°), matching the geometric
            // theme. Vertices come from the cached arrays rebuilt on Configure.
            _fillColorArray[0] = fill;
            container.DrawPolygon(_diamondPoints, _fillColorArray);
            container.DrawPolyline(_insetPoints, _borderColor, 2.0f);
            return;
        }

        // Filled circle
        container.DrawCircle(center, radius, fill);

        // Dark border
        container.DrawCircle(center, radius - 2, _borderColor, false, 2.0f);
    }

    /// <summary>
    /// Fill color with the status overlays applied: stun desaturates toward grey,
    /// burn tints toward orange (pulsing while it drains), the burn proc flashes hot
    /// yellow/orange, and a crit flash brightens toward white.
    /// </summary>
    private Color EffectiveFillColor()
    {
        Color color = _fillColor;

        if (_stunRemaining > 0f)
        {
            float luminance = color.R * 0.299f + color.G * 0.587f + color.B * 0.114f;
            var grey = new Color(luminance, luminance, luminance, color.A);
            color = color.Lerp(grey, 0.6f);
        }

        if (_burnRemaining > 0f)
            color = BurnTint(color, _burnPulseStep);

        if (_burnFlashRemaining > 0f)
        {
            color = color.Lerp(
                GameConstants.BurnProcFlashColor with { A = color.A },
                GameConstants.BurnProcFlashStrength);
        }

        if (_hitFlashRemaining > 0f)
            color = color.Lerp(new Color(1f, 1f, 1f, color.A), 0.6f);

        return color;
    }

    /// <summary>
    /// Applies the burn tint for a quantized pulse step to a fill color. The tint follows
    /// the pulse step rather than a raw timer, so the draw stays a pure function of state
    /// (see <see cref="TickStatuses"/>): the color only changes when the step changes.
    /// Pure, so the pulse's visual contract is testable — step 0 (the trough) is exactly
    /// the static burn tint the enemy showed before the pulse existed, and later steps are
    /// hotter, which is what makes the drain read as active.
    /// </summary>
    public static Color BurnTint(Color fill, int pulseStep)
    {
        float pulse = BurnPulseIntensity(pulseStep);

        Color tint = GameConstants.BurnPulseCoolColor
            .Lerp(GameConstants.BurnPulseHotColor, pulse);
        float strength = Mathf.Lerp(
            GameConstants.BurnPulseTintStrengthMin,
            GameConstants.BurnPulseTintStrengthMax,
            pulse);

        return fill.Lerp(tint with { A = fill.A }, strength);
    }

    /// <summary>
    /// Sets the formation offset applied to this enemy relative to its path anchor,
    /// plus the angular speed at which that offset orbits the anchor.
    /// Set before SetPath so the initial anchor placement already includes the offset.
    /// </summary>
    public void SetFormationOffset(Vector2 offset, float angularSpeed)
    {
        _formationOffset = offset;
        _formationAngularSpeed = angularSpeed;

        // If a path is already active, keep Position consistent with the new offset.
        if (_waypoints.Count > 0)
            Position = _anchorPosition + _formationOffset;
    }

    /// <summary>
    /// Sets the path waypoints for this enemy to follow.
    /// The cached waypoint list is treated as read-only and shared across enemies;
    /// ResetForPool reassigns (never clears) so it can't mutate the shared cache.
    /// </summary>
    public void SetPath(List<Vector2> waypoints)
    {
        _waypoints = waypoints;
        _currentWaypointIndex = 0;

        if (_waypoints.Count > 0)
        {
            _anchorPosition = _waypoints[0];
            Position = _anchorPosition + _formationOffset;
        }
    }

    public override void _Process(double delta)
    {
        if (_isDead || _waypoints.Count == 0)
            return;

        // Stun pauses movement only (formation orbit included). Status timers tick
        // in GameManager's centralized status pass, not here.
        if (_stunRemaining > 0f)
            return;

        MoveAlongPath((float)delta);
    }

    private void MoveAlongPath(float delta)
    {
        if (_currentWaypointIndex >= _waypoints.Count)
        {
            _isDead = true;
            EmitSignal(SignalName.ReachedEnd, this);
            return;
        }

        Vector2 target = _waypoints[_currentWaypointIndex];

        // Rotate the formation offset so swarm members orbit their anchor like
        // electrons around an atom. Basic enemies have zero angular speed, so the
        // offset (always zero for them) never changes.
        if (_formationAngularSpeed != 0f)
            _formationOffset = _formationOffset.Rotated(_formationAngularSpeed * delta);

        Vector2 toTarget = target - _anchorPosition;
        float distanceToWaypoint = toTarget.Length();
        float moveDistance = _speed * GameConstants.CellSize * delta;

        if (moveDistance >= distanceToWaypoint)
        {
            // Anchor arrives at waypoint; the formation offset survives because
            // Position is recomputed from the anchor below, not snapped to target.
            _anchorPosition = target;
            _currentWaypointIndex++;
        }
        else
        {
            // Guard near-zero distance so Normalized() never produces NaN.
            Vector2 direction = distanceToWaypoint > 0.0001f
                ? toTarget / distanceToWaypoint
                : Vector2.Zero;
            _anchorPosition += direction * moveDistance;
        }

        Position = _anchorPosition + _formationOffset;
    }

    /// <summary>
    /// Reset this enemy's state for reuse from the object pool.
    /// Called after Acquire before repositioning. HP is restored from the current kind config
    /// (or the basic default if never configured).
    /// </summary>
    public void ResetForPool()
    {
        // Bump the identity generation so consumers holding this reference (e.g. a
        // laser's ramp state) can detect it was reissued as a new enemy.
        Generation++;

        // Reassign to a fresh empty list rather than Clear() — the previous list may
        // be a shared cached path reference owned by GridManager.
        _waypoints = new List<Vector2>();
        _currentWaypointIndex = 0;
        // Clear formation state so a pooled swarm member can't leak its offset
        // into a later basic-enemy reuse.
        _formationOffset = Vector2.Zero;
        _formationAngularSpeed = 0f;
        _anchorPosition = Vector2.Zero;
        _armor = 0;
        _currentHP = _maxHP > 0 ? _maxHP : GameConstants.EnemyHP;
        _isDead = false;
        Position = Vector2.Zero;

        // Clear stun/burn/flash so a pooled enemy can't leak a status into reuse.
        _stunRemaining = 0f;
        _burnRemaining = 0f;
        _burnDps = 0f;
        _hitFlashRemaining = 0f;
        _burnFlashRemaining = 0f;
        _burnPulsePhase = 0f;
        _burnPulseStep = 0;
    }

    /// <summary>
    /// Apply damage to this enemy. Flat armor reduces the damage (clamped to >= 0)
    /// unless ignoreArmor is true. If HP reaches 0, enemy is destroyed.
    /// The Destroyed signal is emitted; GameManager handles release to pool.
    /// </summary>
    public void TakeDamage(float damage, bool ignoreArmor = false)
    {
        if (_isDead)
            return;

        float applied = Mathf.Max(ignoreArmor ? damage : damage - _armor, 0f);
        _currentHP -= applied;

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            _isDead = true;
            EmitSignal(SignalName.Destroyed, this);
            // GameManager.OnEnemyDestroyed handles release to pool
        }
    }

    /// <summary>
    /// Applies stun for the given duration. Re-stun refreshes the timer (it never
    /// shortens an active stun) and does not stack.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (_isDead || duration <= 0f)
            return;

        _stunRemaining = Mathf.Max(_stunRemaining, duration);
        RedrawVisual();
    }

    /// <summary>
    /// Applies a burn DoT of the given dps for the given duration. Re-proc refreshes
    /// the duration and does not stack. A proc (first or refresh) also flashes the
    /// enemy briefly and restarts the pulse phase so the moment it lands is visible.
    /// </summary>
    public void ApplyBurn(float dps, float duration)
    {
        if (_isDead || dps <= 0f || duration <= 0f)
            return;

        _burnDps = dps;
        _burnRemaining = Mathf.Max(_burnRemaining, duration);
        _burnFlashRemaining = GameConstants.BurnProcFlashDuration;
        _burnPulsePhase = 0f;
        _burnPulseStep = 0;
        RedrawVisual();
    }

    /// <summary>
    /// Brief brighter flash after a crit lands (presentation only).
    /// </summary>
    public void ApplyHitFlash()
    {
        if (_isDead)
            return;

        _hitFlashRemaining = GameConstants.HitFlashDuration;
        RedrawVisual();
    }

    /// <summary>
    /// Advances stun/burn/flash timers and applies burn damage for the elapsed time.
    /// Called once per frame by GameManager's centralized status pass. Burn damage
    /// ignores armor. Allocation-free: pure arithmetic plus TakeDamage, which only
    /// emits a signal on death.
    /// </summary>
    public void TickStatuses(float delta)
    {
        if (_isDead)
            return;

        bool wasStunned = _stunRemaining > 0f;
        bool wasBurning = _burnRemaining > 0f;
        bool wasFlashing = _hitFlashRemaining > 0f;
        bool wasBurnFlashing = _burnFlashRemaining > 0f;
        if (!wasStunned && !wasBurning && !wasFlashing && !wasBurnFlashing)
            return;

        bool redraw = false;

        if (_burnRemaining > 0f)
        {
            // Burn the portion of this frame the DoT was still active, then advance
            // the timer. Total burn damage is dps * duration (2 dps x 2s = 4).
            float burnTime = Mathf.Min(delta, _burnRemaining);
            TakeDamage(_burnDps * burnTime, ignoreArmor: true);
            _burnRemaining = Mathf.Max(0f, _burnRemaining - delta);

            // Burn is the one status whose tint is not a pure function of "active or
            // not": it pulses so the drain reads as ongoing. Advance the phase and
            // redraw only when the quantized step changes — BurnPulseSteps redraws per
            // BurnPulsePeriod (~17 Hz while burning), never once per frame.
            _burnPulsePhase += delta;
            int step = BurnPulseStepAt(_burnPulsePhase);
            if (step != _burnPulseStep)
            {
                _burnPulseStep = step;
                redraw = true;
            }
        }

        _stunRemaining = Mathf.Max(0f, _stunRemaining - delta);
        _hitFlashRemaining = Mathf.Max(0f, _hitFlashRemaining - delta);
        _burnFlashRemaining = Mathf.Max(0f, _burnFlashRemaining - delta);

        // The tint is otherwise a pure function of *which* statuses are active, so
        // redraw only on a status-set transition (a status applied, or one expiring) —
        // never on every frame a status is active. The burn pulse above is the only
        // additional redraw source, and it is quantized to discrete steps.
        if ((_stunRemaining > 0f) != wasStunned ||
            (_burnRemaining > 0f) != wasBurning ||
            (_hitFlashRemaining > 0f) != wasFlashing ||
            (_burnFlashRemaining > 0f) != wasBurnFlashing)
        {
            redraw = true;
        }

        if (redraw)
            RedrawVisual();
    }

    /// <summary>
    /// Discrete pulse step (0..BurnPulseSteps-1) for a burn pulse phase in seconds.
    /// The phase is wrapped into one cycle here, so an unbounded phase still yields a
    /// bounded step. Pure and allocation-free so the burning tint can be a function of
    /// the step alone.
    /// </summary>
    public static int BurnPulseStepAt(float phase)
    {
        int steps = GameConstants.BurnPulseSteps;
        float period = GameConstants.BurnPulsePeriod;

        if (steps <= 1 || !(period > 0f) || !float.IsFinite(phase))
            return 0;

        float cycle = phase - period * System.MathF.Floor(phase / period);
        if (!(cycle >= 0f)) // NaN guard
            return 0;

        int step = (int)(cycle / period * steps);
        return step >= steps ? steps - 1 : step;
    }

    /// <summary>
    /// Burn-pulse intensity (0..1) for a discrete pulse step: a sine of the step's
    /// position in the cycle, quantized to BurnPulseSteps levels. Step 0 is the trough
    /// (intensity 0), which makes the pulse's resting tint the static burn tint.
    /// </summary>
    public static float BurnPulseIntensity(int step)
    {
        int steps = GameConstants.BurnPulseSteps;
        if (steps <= 1)
            return 0f;

        int clamped = Mathf.Clamp(step, 0, steps - 1);
        float progress = (float)clamped / steps;
        return 0.5f - 0.5f * System.MathF.Cos(System.MathF.Tau * progress);
    }

    private void RedrawVisual()
    {
        _circleContainer?.QueueRedraw();
    }
}
