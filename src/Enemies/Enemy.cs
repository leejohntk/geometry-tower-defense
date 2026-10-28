using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Identifies an enemy variant. Basic is a red circle; Swarm is a smaller orange circle;
/// Armored is a grey square (diamond) with flat damage reduction; Progenitor is the
/// large magenta octagon boss that summons squads ahead of itself.
/// </summary>
public enum EnemyKind
{
    Basic,
    Swarm,
    Armored,
    Progenitor
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

    // Emitted by the Progenitor when a summon windup completes. The rolled squad stays
    // pending (PendingSummonCount/Kind) until the consumer clears it, so the sweep of
    // squad members spawns synchronously from one signal with no collection passed
    // through the signal system.
    [Signal]
    public delegate void SummonReadyEventHandler(Enemy boss);

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

    // Kill-XP attribution: credited (effective, non-overkill) damage per source tower
    // for this enemy's current life. Pre-sized, reused, and cleared on pool reset and
    // on every reconfigure so a pooled enemy never leaks contributions into reuse.
    // Dictionary lookup/assignment on an existing key allocates nothing, so steady
    // combat adds no per-hit garbage.
    private const int DamageCreditCapacity = 8;
    private readonly Dictionary<Tower, float> _damageCredit = new(DamageCreditCapacity);

    // The tower that applied the active burn, retained until the DoT ends so a burn
    // killing blow still credits the laser even after contact has stopped.
    private Tower? _burnSource;

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

    // Progenitor summon state machine. Timers are ticked in GameManager's centralized
    // boss pass (never in _Process), and every field is cleared by ApplyConfig /
    // ResetForPool so a pooled boss never leaks phase, cooldown, windup, or pending
    // composition into a normal-enemy reuse. The pending buffer is fixed-size and
    // reused, so a summon rolls its squad without allocating.
    private int _summonPhase = ProgenitorSummons.FirstPhase;
    private float _summonCountdown = 0f;
    private float _summonWindupRemaining = 0f;
    private int _pendingSummonCount = 0;
    private readonly EnemyKind[] _pendingSummonKinds = new EnemyKind[GameConstants.ProgenitorMaxSquadSize];
    private SkillRandom _summonRolls = SkillRandom.Default;

    // Cached octagon geometry for the Progenitor (fill ring + closed outline), rebuilt
    // on Configure like the armored diamond.
    private Vector2[] _octagonPoints = System.Array.Empty<Vector2>();
    private Vector2[] _octagonOutlinePoints = System.Array.Empty<Vector2>();

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
    /// Kill XP awarded to the towers that damaged this enemy, split in proportion to
    /// credited damage. Summons keep their rolled kind's value even though their coin
    /// drop is zero.
    /// </summary>
    public int XpValue { get; private set; } = GameConstants.EnemyXpBasic;

    /// <summary>
    /// Number of distinct towers currently holding damage credit on this enemy.
    /// </summary>
    public int CreditSourceCount => _damageCredit.Count;

    /// <summary>
    /// Credited (effective, non-overkill) damage this enemy has taken from the given
    /// tower during its current life. Zero when the tower never damaged it.
    /// </summary>
    public float CreditedDamage(Tower tower) =>
        _damageCredit.TryGetValue(tower, out float credited) ? credited : 0f;

    /// <summary>
    /// Drops every strong reference to the towers that damaged this enemy: the per-tower
    /// credit table and the retained burn source. Called by GameManager on every pool
    /// release path (kill, base arrival, level reset) so a released enemy never keeps a
    /// (possibly freed) tower alive. Idempotent and allocation-free once warm.
    /// </summary>
    public void ClearAttribution()
    {
        _damageCredit.Clear();
        _burnSource = null;
    }

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

    /// <summary>
    /// True while this enemy is configured as the Progenitor boss.
    /// </summary>
    public bool IsProgenitor => Kind == EnemyKind.Progenitor;

    /// <summary>
    /// Current summon phase (1..3). Always 1 for non-Progenitor enemies.
    /// </summary>
    public int SummonPhase => _summonPhase;

    /// <summary>
    /// Seconds until the next summon windup begins. Zero for non-Progenitor enemies.
    /// </summary>
    public float SummonCountdown => _summonCountdown;

    /// <summary>
    /// True during the 0.5 s summon windup: movement is slowed and the pulse ring is
    /// visible.
    /// </summary>
    public bool IsSummoning => _summonWindupRemaining > 0f;

    /// <summary>
    /// Number of rolled squad members waiting to be spawned by the summon consumer.
    /// Zero when no windup has completed (or after the consumer clears it).
    /// </summary>
    public int PendingSummonCount => _pendingSummonCount;

    /// <summary>
    /// Base movement speed in cells/sec before the windup slow multiplier.
    /// </summary>
    public float Speed => _speed;

    /// <summary>
    /// Speed actually used for movement this tick: the base speed multiplied by
    /// ProgenitorSummonSlowFactor during a summon windup.
    /// </summary>
    public float CurrentSpeed => IsProgenitor && _summonWindupRemaining > 0f
        ? _speed * GameConstants.ProgenitorSummonSlowFactor
        : _speed;

    /// <summary>
    /// Player HP lost if this enemy reaches the base. The Progenitor uses its own
    /// constant; every other kind costs one HP, matching pre-boss behavior.
    /// </summary>
    public int BaseDamage => IsProgenitor
        ? GameConstants.ProgenitorBaseDamage
        : 1;

    /// <summary>
    /// The waypoint index this enemy is currently moving toward. Summons start with the
    /// boss's index so they continue along the same route.
    /// </summary>
    public int CurrentWaypointIndex => _currentWaypointIndex;

    /// <summary>
    /// The point on the route this enemy follows. Position is always
    /// AnchorPosition + FormationOffset.
    /// </summary>
    public Vector2 AnchorPosition => _anchorPosition;

    /// <summary>
    /// Border color for the current phase. Shifts white -> amber -> red as HP falls.
    /// </summary>
    public Color PhaseBorderColor => ProgenitorSummons.BorderColorForPhase(_summonPhase);

    /// <summary>
    /// True once this enemy has reached the base: the last real route cell is index
    /// Count-2, and GridManager appends one synthetic "one cell past the base" exit
    /// waypoint at Count-1. An enemy whose index has advanced to Count-1 is walking off
    /// that exit cell — it has already reached the base, so it must not be used as a
    /// summon anchor (adds spawned there would leak within a frame or two).
    /// </summary>
    public bool HasReachedBase =>
        _waypoints.Count > 0 && _currentWaypointIndex >= _waypoints.Count - 1;

    public override void _Ready()
    {
        ApplyConfig(EnemyKind.Basic);
        CreateVisual();
    }

    private void ApplyConfig(EnemyKind kind)
    {
        Kind = kind;

        // Every (re)configuration starts from a clean summon state; the Progenitor
        // branch below re-seeds its phase machine. This is what keeps a pooled boss
        // from leaking phase/windup/coin state when reused as a normal enemy.
        ResetSummonState();

        // A reconfigure starts a brand-new life: prior damage credit and any burn
        // source must never survive into the new kind (pooled reuse safety).
        ClearAttribution();
        XpValue = TowerUpgrades.EnemyXpValue(kind);

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

            case EnemyKind.Progenitor:
                _maxHP = GameConstants.ProgenitorHP;
                _armor = GameConstants.ProgenitorArmor;
                _speed = GameConstants.ProgenitorSpeed;
                _diameter = GameConstants.ProgenitorDiameter;
                _fillColor = GameConstants.ProgenitorFillColor;
                // The border is drawn from the live phase (PhaseBorderColor), not a
                // cached field, so it shifts white -> amber -> red as HP falls.
                CoinDrop = GameConstants.ProgenitorCoinDrop;
                // The timer starts at spawn and counts down to the first interval — the
                // boss never summons on the frame it appears.
                _summonCountdown = ProgenitorSummons.IntervalForPhase(_summonPhase);
                break;
        }

        _currentHP = _maxHP;
        _isDead = false;
    }

    /// <summary>
    /// Clears every Progenitor-only field to its default. Called on every (re)configure
    /// and on pool reset, so no boss state can survive into a reuse.
    /// </summary>
    private void ResetSummonState()
    {
        _summonPhase = ProgenitorSummons.FirstPhase;
        _summonCountdown = 0f;
        _summonWindupRemaining = 0f;
        _pendingSummonCount = 0;
    }

    /// <summary>
    /// Sets the injectable roll source used for summon composition. Tests pass a
    /// scripted source; production leaves the seeded default. A null source falls back
    /// to the shared seeded default rather than ever rolling from an unseeded source.
    /// </summary>
    public void SetSummonRandom(SkillRandom? rolls)
    {
        _summonRolls = rolls ?? SkillRandom.Default;
    }

    /// <summary>
    /// Zero-coin override used by summons so they cannot be farmed. Configure resets the
    /// coin value for the kind, so this is applied after Configure.
    /// </summary>
    public void SetCoinDrop(int coins)
    {
        CoinDrop = Mathf.Max(0, coins);
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
        RebuildProgenitorGeometry();

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

    /// <summary>
    /// Rebuilds the cached octagon fill/outline vertex arrays from the current
    /// diameter. No-op for non-Progenitor kinds.
    /// </summary>
    private void RebuildProgenitorGeometry()
    {
        if (!IsProgenitor)
            return;

        const int sides = 8;
        float diameter = _diameter;
        Vector2 center = new Vector2(diameter / 2f, diameter / 2f);
        float radius = diameter / 2f;

        _octagonPoints = new Vector2[sides];
        _octagonOutlinePoints = new Vector2[sides + 1];

        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.Tau * i / sides;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            _octagonPoints[i] = point;
            _octagonOutlinePoints[i] = point;
        }

        // Close the outline polyline back to the first vertex.
        _octagonOutlinePoints[sides] = _octagonOutlinePoints[0];
    }

    private void DrawEnemyShape(Control container)
    {
        if (!IsInstanceValid(container)) return;

        float radius = _diameter / 2f;
        Vector2 center = new Vector2(_diameter / 2f, _diameter / 2f);
        Color fill = EffectiveFillColor();

        if (IsProgenitor)
        {
            // Geometry is rebuilt on Configure; drawing before that (or after a
            // partial teardown) must not touch an empty vertex array.
            if (_octagonPoints.Length == 0)
                return;

            // Large octagon with a thick phase-colored border, plus the geometric HP
            // bar and (mid-windup) the telegraph pulse ring.
            _fillColorArray[0] = fill;
            container.DrawPolygon(_octagonPoints, _fillColorArray);
            container.DrawPolyline(
                _octagonOutlinePoints,
                PhaseBorderColor,
                GameConstants.ProgenitorBorderWidth);
            DrawProgenitorHealthBar(container, center);
            DrawSummonPulseRing(container, center, radius);
            return;
        }

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
    /// Draws the boss health bar above the octagon: dark track, phase-colored fill, a
    /// thin outline, and tick marks exactly at the 66% and 33% phase thresholds. All
    /// coordinates are container-local; the bar lives above the octagon's top edge.
    /// </summary>
    private void DrawProgenitorHealthBar(Control container, Vector2 center)
    {
        float barWidth = _diameter * GameConstants.ProgenitorHealthBarWidthRatio;
        float barHeight = GameConstants.ProgenitorHealthBarHeight;
        float left = center.X - barWidth / 2f;
        float top = -GameConstants.ProgenitorHealthBarGap - barHeight;

        var track = new Rect2(left, top, barWidth, barHeight);
        container.DrawRect(track, new Color(0.08f, 0.02f, 0.06f, 0.9f));

        float fraction = _maxHP > 0f ? Mathf.Clamp(_currentHP / _maxHP, 0f, 1f) : 0f;
        if (fraction > 0f)
        {
            var fillRect = new Rect2(left, top, barWidth * fraction, barHeight);
            container.DrawRect(fillRect, PhaseBorderColor);
        }

        container.DrawRect(track, new Color(0.95f, 0.9f, 0.95f, 0.9f), filled: false, width: 1f);

        // Tick marks at the phase thresholds (33% then 66%), drawn individually so the
        // draw path allocates nothing.
        var tickColor = new Color(0.95f, 0.9f, 0.95f, 0.85f);
        DrawHealthBarTick(container, left, top, barWidth, barHeight, ProgenitorSummons.Phase3ThresholdHP(_maxHP), tickColor);
        DrawHealthBarTick(container, left, top, barWidth, barHeight, ProgenitorSummons.Phase2ThresholdHP(_maxHP), tickColor);
    }

    private void DrawHealthBarTick(
        Control container,
        float left,
        float top,
        float barWidth,
        float barHeight,
        int thresholdHP,
        Color color)
    {
        if (!(_maxHP > 0f))
            return;

        float x = left + barWidth * (thresholdHP / _maxHP);
        container.DrawLine(new Vector2(x, top), new Vector2(x, top + barHeight), color, 1.0f);
    }

    /// <summary>
    /// Draws the telegraph ring that expands during the summon windup, so the squad
    /// spawn is always visible before it happens. Draws nothing outside a windup.
    /// </summary>
    private void DrawSummonPulseRing(Control container, Vector2 center, float radius)
    {
        if (_summonWindupRemaining <= 0f)
            return;

        float duration = GameConstants.ProgenitorSummonWindup;
        float progress = duration > 0f
            ? Mathf.Clamp(1f - _summonWindupRemaining / duration, 0f, 1f)
            : 1f;
        float pulseRadius = radius * Mathf.Lerp(
            GameConstants.ProgenitorPulseStartScale,
            GameConstants.ProgenitorPulseEndScale,
            progress);

        Color pulse = GameConstants.ProgenitorPulseColor with { A = 1f - 0.5f * progress };
        container.DrawArc(
            center,
            pulseRadius,
            0f,
            Mathf.Tau,
            GameConstants.ProgenitorPulseSegments,
            pulse,
            3.0f,
            antialiased: true);
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

    /// <summary>
    /// Starts this enemy partway along a path — at <paramref name="waypointIndex"/>
    /// with its anchor already at <paramref name="anchorPosition"/>. Used by boss
    /// summons so an add continues along the boss's route from the boss's current
    /// progress instead of restarting at the spawn cell. The formation offset is kept,
    /// so Position is anchor + offset.
    /// </summary>
    public void StartAtProgress(List<Vector2> waypoints, int waypointIndex, Vector2 anchorPosition)
    {
        // GridManager's waypoint list appends a synthetic one-cell-past-base exit
        // waypoint, so the last index (Count-1) is not a spawnable route position: an
        // enemy starting there would leak within a frame or two. Clamp to the last real
        // route cell (Count-2) so no caller can ever start an enemy on the exit.
        int maxIndex = waypoints.Count >= 2 ? waypoints.Count - 2 : 0;

        _waypoints = waypoints;
        _currentWaypointIndex = Mathf.Clamp(waypointIndex, 0, maxIndex);
        _anchorPosition = anchorPosition;
        _isDead = false;
        Position = _anchorPosition + _formationOffset;
    }

    /// <summary>
    /// The point on this enemy's route where a summon squad should appear: the boss's
    /// current anchor pushed ProgenitorSummonLeadDistance px toward the waypoint it is
    /// approaching, clamped to that waypoint so the lead never cuts a corner. Returns
    /// the anchor unchanged once the route is finished.
    /// </summary>
    public Vector2 GetSummonLeadPoint()
    {
        if (_currentWaypointIndex < 0 || _currentWaypointIndex >= _waypoints.Count)
            return _anchorPosition;

        return ProgenitorSummons.LeadPoint(
            _anchorPosition,
            _waypoints[_currentWaypointIndex],
            GameConstants.ProgenitorSummonLeadDistance);
    }

    /// <summary>
    /// Advances the Progenitor summon state machine by one frame. Called once per frame
    /// by GameManager's centralized boss pass — never from _Process — so the boss adds
    /// no per-node update loop and no enemy-list scan.
    /// Allocating nothing: composition rolls write into the fixed pending buffer.
    /// </summary>
    public void TickProgenitorSummon(float delta)
    {
        if (!IsProgenitor || _isDead || !(delta > 0f))
            return;

        if (_summonWindupRemaining > 0f)
        {
            _summonWindupRemaining = Mathf.Max(0f, _summonWindupRemaining - delta);

            if (_summonWindupRemaining > 0f)
            {
                // The pulse ring expands every frame for the (short) windup only.
                RedrawVisual();
                return;
            }

            // Reset the cooldown only once the squad actually spawns. A phase change
            // during the windup therefore applies to the next summon, not this one.
            _summonCountdown = ProgenitorSummons.IntervalForPhase(_summonPhase);
            RedrawVisual();
            EmitSignal(SignalName.SummonReady, this);
            return;
        }

        _summonCountdown -= delta;
        if (_summonCountdown <= 0f)
            BeginSummonWindup();
    }

    /// <summary>
    /// Rolls the squad for the current phase at windup start and freezes it: if a phase
    /// threshold is crossed during the windup, the already-rolled composition still
    /// spawns (the spec's edge case). One roll per squad slot.
    /// </summary>
    private void BeginSummonWindup()
    {
        _pendingSummonCount = ProgenitorSummons.SquadSizeForPhase(_summonPhase);
        for (int i = 0; i < _pendingSummonCount; i++)
            _pendingSummonKinds[i] = ProgenitorSummons.RollKind(_summonPhase, _summonRolls);

        _summonWindupRemaining = GameConstants.ProgenitorSummonWindup;
        RedrawVisual();
    }

    /// <summary>
    /// Kind of the pending squad member at <paramref name="index"/>. Out-of-range reads
    /// return Basic so a stale index can never throw.
    /// </summary>
    public EnemyKind PendingSummonKind(int index) =>
        index >= 0 && index < _pendingSummonCount ? _pendingSummonKinds[index] : EnemyKind.Basic;

    /// <summary>
    /// Marks the pending squad as consumed. Called by the summon consumer after every
    /// member has been released into the normal enemy lifecycle.
    /// </summary>
    public void ClearPendingSummon()
    {
        _pendingSummonCount = 0;
    }

    /// <summary>
    /// Recomputes the phase from current HP. Damage is the only way HP falls, so this
    /// runs on each damaging hit for the boss. Crossing a threshold immediately adopts
    /// the new cadence for the current countdown — clamped, never reset to the new full
    /// duration — so a threshold can neither add a sudden wait nor cause a double
    /// summon.
    /// </summary>
    private void UpdateProgenitorPhase()
    {
        if (!IsProgenitor)
            return;

        int phase = ProgenitorSummons.PhaseFor(_currentHP, _maxHP);
        if (phase == _summonPhase)
            return;

        _summonPhase = phase;
        _summonCountdown = Mathf.Min(_summonCountdown, ProgenitorSummons.IntervalForPhase(phase));
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
        // CurrentSpeed includes the Progenitor's summon-windup slow: the boss keeps
        // walking during the windup, just at a fraction of its speed.
        float moveDistance = CurrentSpeed * GameConstants.CellSize * delta;

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

        // Drop all kill-XP attribution: a pooled enemy must never carry one tower's
        // damage credit (or a boss's XP value) into its next life.
        ClearAttribution();
        XpValue = GameConstants.EnemyXpBasic;

        // Clear every Progenitor field: phase, summon timer, windup, pending
        // composition, roll source, and the zero-coin summon override. The next
        // Configure re-seeds what its kind needs, so a boss reused as a Basic enemy
        // has completely normal stats and behavior.
        ResetSummonState();
        _summonRolls = SkillRandom.Default;
        // Back to the default kind too, so a pooled boss is not still identified as a
        // Progenitor (which now drives both IsProgenitor and the draw path) between
        // release and the next Configure.
        Kind = EnemyKind.Basic;
        CoinDrop = GameConstants.CoinDropPerKill;
        RedrawVisual();
    }

    /// <summary>
    /// Apply damage to this enemy. Flat armor reduces the damage (clamped to >= 0)
    /// unless ignoreArmor is true. If HP reaches 0, enemy is destroyed.
    ///
    /// When <paramref name="source"/> is given, the hit credits that tower with the
    /// effective damage actually removed from HP — <c>min(applied, HP before hit)</c> —
    /// so overkill is never credited. On death the enemy's kill XP is split across
    /// every credited tower in proportion to its credit, before the Destroyed signal
    /// releases and resets the enemy. Omitting the source keeps the old behavior:
    /// damage applies normally but awards no XP.
    /// </summary>
    public void TakeDamage(float damage, bool ignoreArmor = false, Tower? source = null)
    {
        if (_isDead)
            return;

        float applied = Mathf.Max(ignoreArmor ? damage : damage - _armor, 0f);

        if (source != null && applied > 0f && _currentHP > 0f)
        {
            float credited = Mathf.Min(applied, _currentHP);
            CreditDamage(source, credited);
        }

        _currentHP -= applied;

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            _isDead = true;
            // A dying boss cancels its in-flight windup: no squad spawns, and the
            // telegraph ring is cleared before the enemy is hidden and pooled.
            _summonWindupRemaining = 0f;
            _pendingSummonCount = 0;
            AwardKillExperience();
            EmitSignal(SignalName.Destroyed, this);
            // GameManager.OnEnemyDestroyed handles release to pool
            return;
        }

        if (IsProgenitor && applied > 0f)
        {
            // Phase recompute on every damaging hit, and repaint the border/bar.
            UpdateProgenitorPhase();
            RedrawVisual();
        }
    }

    /// <summary>
    /// Adds effective damage to one tower's credit entry. The table is keyed by tower
    /// so repeated hits accumulate on a single entry (and allocate nothing once the
    /// entry exists).
    /// </summary>
    private void CreditDamage(Tower source, float amount)
    {
        _damageCredit.TryGetValue(source, out float existing);
        _damageCredit[source] = existing + amount;
    }

    /// <summary>
    /// Splits this enemy's kill XP across its credited towers by credited-damage
    /// share. Called exactly once, on death, before the Destroyed signal. A tower
    /// freed mid-level is skipped so no dangling node is dereferenced.
    /// </summary>
    private void AwardKillExperience()
    {
        if (XpValue <= 0 || _damageCredit.Count == 0)
            return;

        float total = 0f;
        foreach (var entry in _damageCredit)
            total += entry.Value;

        if (!(total > 0f))
            return;

        float enemyXp = XpValue;
        foreach (var entry in _damageCredit)
        {
            var tower = entry.Key;
            if (tower == null || !IsInstanceValid(tower))
                continue;

            tower.AddXp(enemyXp * entry.Value / total);
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
    /// The optional <paramref name="source"/> laser is retained until the burn ends so
    /// every tick — including a killing blow — credits its tower.
    /// </summary>
    public void ApplyBurn(float dps, float duration, Tower? source = null)
    {
        if (_isDead || dps <= 0f || duration <= 0f)
            return;

        _burnDps = dps;
        _burnRemaining = Mathf.Max(_burnRemaining, duration);
        _burnSource = source;
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
            // the timer. Total burn damage is dps * duration (2 dps x 2s = 4). The
            // credited source survives contact so the killing tick still awards XP.
            float burnTime = Mathf.Min(delta, _burnRemaining);
            TakeDamage(_burnDps * burnTime, ignoreArmor: true, _burnSource);
            if (_isDead)
                return;
            _burnRemaining = Mathf.Max(0f, _burnRemaining - delta);
            if (_burnRemaining <= 0f)
                _burnSource = null;

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
