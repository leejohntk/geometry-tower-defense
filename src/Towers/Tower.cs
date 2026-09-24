using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Base class for all tower variants. Owns targeting, cooldown, and range display.
/// Variants supply their stats (range, damage, fire rate, cost) and visuals.
/// </summary>
public abstract partial class Tower : Node2D
{
    private float _fireCooldownTimer = 0f;
    private Control? _rangeIndicator;

    /// <summary>
    /// Grid row position of this tower.
    /// </summary>
    public int GridRow { get; private set; }

    /// <summary>
    /// Grid column position of this tower.
    /// </summary>
    public int GridCol { get; private set; }

    /// <summary>
    /// The current target enemy, if any.
    /// </summary>
    public Enemy? CurrentTarget { get; private set; } = null;

    /// <summary>
    /// Current skill-tree state used to layer modifiers onto this tower's stats.
    /// Null until set (base stats apply) or across the tower's whole lifetime.
    /// </summary>
    private SkillTreeState? _skillState;

    public abstract TowerType Type { get; }
    public abstract int Damage { get; }
    public abstract float FireRate { get; }
    public abstract int Cost { get; }
    protected abstract Color RangeColor { get; }

    /// <summary>
    /// Final range (cells), base plus skill ranks. Concrete here so the placement
    /// preview (via <see cref="SkillStats.RangeCells"/>) and every placed tower
    /// share exactly one "type → range" mapping.
    /// </summary>
    public float RangeCells => SkillStats.RangeCells(Type, _skillState);

    /// <summary>
    /// Multiplier applied to projectile speed at launch. Base is 1.0; the cannon's
    /// Powder Charge node raises it. Arrow projectiles keep the default.
    /// </summary>
    public virtual float ProjectileSpeedMultiplier => 1f;

    /// <summary>
    /// Final explosion radius (px) for towers that explode on hit. Zero for towers
    /// without a splash; the cannon overrides it with its skill-modified radius.
    /// </summary>
    public virtual float SplashRadius => 0f;

    /// <summary>
    /// Continuous damage per second for drain-style towers. Zero for discrete-fire towers.
    /// </summary>
    public virtual float Dps => 0f;

    /// <summary>
    /// True if this tower damages continuously every frame (no projectile, no cooldown).
    /// </summary>
    public virtual bool IsContinuous => false;

    // Skill-mechanic values (Part 2). Base towers have no mechanics; variants override.
    /// <summary>Number of extra enemies an arrow passes through (0 = none).</summary>
    public virtual int PierceCount => 0;

    /// <summary>Per-hit chance an arrow crits for double damage.</summary>
    public virtual float CritChance => 0f;

    /// <summary>Number of shells fired per volley (cannon cluster).</summary>
    public virtual int ClusterCount => 1;

    /// <summary>Per-hit chance a cannon shell stuns its target.</summary>
    public virtual float StunChance => 0f;

    /// <summary>Number of extra enemies a laser beam chains to.</summary>
    public virtual int ChainJumps => 0;

    /// <summary>Per-second chance a laser applies its burn DoT while in contact.</summary>
    public virtual float IgniteChancePerSecond => 0f;

    /// <summary>Maximum dps multiplier a laser ramps to while holding one target.</summary>
    public virtual float RampMaxMultiplier => 1f;

    private SkillRandom _rolls = SkillRandom.Default;

    /// <summary>
    /// Injectable roll source for skill-mechanic random rolls (crit, stun, ignite).
    /// </summary>
    public SkillRandom Rolls => _rolls;

    /// <summary>
    /// Replaces this tower's roll source (tests inject a scripted source). Null falls
    /// back to the shared default.
    /// </summary>
    public void SetSkillRandom(SkillRandom? rolls)
    {
        _rolls = rolls ?? SkillRandom.Default;
    }

    /// <summary>
    /// Tower range in pixels.
    /// </summary>
    public float RangePixels => GameConstants.CellDistanceInPixels(RangeCells);

    /// <summary>
    /// Attaches the player's skill-tree state so this tower's final stats layer in
    /// skill-node ranks. Pass null to fall back to base stats.
    /// </summary>
    public void SetSkillTree(SkillTreeState? state)
    {
        _skillState = state;
    }

    /// <summary>
    /// Current rank of a skill node for this tower's type (0 when no state attached).
    /// </summary>
    protected int SkillRank(string nodeId) => _skillState?.GetRank(nodeId) ?? 0;

    /// <summary>
    /// Initialize tower at a specific grid position.
    /// </summary>
    public void Initialize(int gridRow, int gridCol)
    {
        GridRow = gridRow;
        GridCol = gridCol;
        Position = new Vector2(
            GameConstants.CellCenterX(gridCol),
            GameConstants.CellCenterY(gridRow)
        );
    }

    /// <summary>
    /// Sets the current target for this tower. Called by GameManager during targeting phase.
    /// </summary>
    public void SetTarget(Enemy? enemy)
    {
        CurrentTarget = enemy;
    }

    /// <summary>
    /// Returns true if a target is within range, based on pixel distance (not grid-snapped).
    /// </summary>
    public bool IsTargetInRange(Enemy enemy)
    {
        float rangePixels = RangePixels;
        return Position.DistanceSquaredTo(enemy.Position) <= rangePixels * rangePixels;
    }

    /// <summary>
    /// Tries to fire at the current target. Returns true and the target position if firing.
    /// </summary>
    public bool TryFire(Enemy target, out Vector2 targetPos)
    {
        targetPos = Vector2.Zero;

        if (_fireCooldownTimer > 0f) return false;
        if (target == null || target.IsDead) return false;
        if (!IsTargetInRange(target)) return false;

        _fireCooldownTimer = FireRate;
        targetPos = target.Position;

        // Let variants layer fire-time presentation on a successful shot (the shot
        // itself is spawned by GameManager from the returned target position).
        OnFired(targetPos);

        return true;
    }

    /// <summary>
    /// Called once per successful <see cref="TryFire"/>, after the cooldown is armed.
    /// Default is a no-op; variants override it for fire-time feedback.
    /// </summary>
    protected virtual void OnFired(Vector2 targetPos)
    {
    }

    /// <summary>
    /// Show a circular range indicator centered on this tower.
    /// No-op if already shown.
    /// </summary>
    public void ShowRange()
    {
        if (_rangeIndicator != null) return;

        float rangePixels = RangePixels;

        _rangeIndicator = new Control();
        _rangeIndicator.Name = "RangeIndicator";
        _rangeIndicator.Size = new Vector2(rangePixels * 2, rangePixels * 2);
        _rangeIndicator.Position = new Vector2(-rangePixels, -rangePixels);

        _rangeIndicator.Draw += () =>
        {
            if (_rangeIndicator == null) return;

            float radius = rangePixels;
            Vector2 center = new Vector2(rangePixels, rangePixels);

            // Filled semi-transparent circle
            _rangeIndicator.DrawCircle(
                center,
                radius,
                RangeColor with { A = 0.1f }
            );

            // Border circle
            _rangeIndicator.DrawCircle(
                center,
                radius,
                RangeColor with { A = 0.4f },
                false,
                2.0f
            );
        };

        AddChild(_rangeIndicator);
    }

    /// <summary>
    /// Hide and remove the circular range indicator.
    /// No-op if already hidden.
    /// </summary>
    public void HideRange()
    {
        if (_rangeIndicator == null) return;
        _rangeIndicator.QueueFree();
        _rangeIndicator = null;
    }

    /// <summary>
    /// Toggle the circular range indicator on or off.
    /// </summary>
    public void ToggleRange()
    {
        if (_rangeIndicator != null)
            HideRange();
        else
            ShowRange();
    }

    public override void _Process(double delta)
    {
        if (_fireCooldownTimer > 0f)
        {
            _fireCooldownTimer -= (float)delta;
            if (_fireCooldownTimer < 0f)
                _fireCooldownTimer = 0f;
        }
    }
}
