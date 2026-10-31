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
    private Control? _rankBadge;

    // Aim/recoil presentation. The aim angle is purely visual: it is derived from the
    // live target and never feeds back into targeting, range checks, or damage. It is
    // animated toward the desired angle at a capped angular speed (no snapping) and
    // eases back to DefaultAimAngle when there is no valid target.
    private float _aimAngle = 0f;
    private bool _aimSeeded = false;
    private float _recoilRemaining = 0f;

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

    /// <summary>
    /// Damage per hit before enemy armor. Kept as a float end to end: rank multipliers
    /// produce fractional values (rank 3 arrow = 10.9) and the exact value must reach
    /// <see cref="Enemy.TakeDamage"/> without integer truncation.
    /// </summary>
    public abstract float Damage { get; }
    public abstract float FireRate { get; }
    public abstract int Cost { get; }
    protected abstract Color RangeColor { get; }

    /// <summary>
    /// This tower's geometric theme color (arrow blue, cannon green, laser magenta).
    /// Used by the upgrade panel's chevrons and archetype emblems.
    /// </summary>
    public Color TowerColor => RangeColor;

    /// <summary>
    /// In-level upgrade rank (0..3). Raised by <see cref="GameManager"/> through
    /// <see cref="ApplyRankUpgrade"/>; rank 3 is only reachable by choosing an
    /// archetype. Per tower instance — never shared or persisted across levels.
    /// </summary>
    public int Rank { get; private set; }

    /// <summary>
    /// Hidden kill-XP accumulator for this tower. Fractional shares are kept; the
    /// value is never rounded per hit and only gates the next upgrade's visibility.
    /// </summary>
    public float Xp { get; private set; }

    /// <summary>
    /// Chosen archetype, or <see cref="TowerArchetype.None"/> before rank 3.
    /// Final for this tower instance (no respec, no refund).
    /// </summary>
    public TowerArchetype Archetype { get; private set; } = TowerArchetype.None;

    /// <summary>
    /// True once this tower has specialized into one of its seed branches.
    /// </summary>
    public bool HasArchetype => Archetype != TowerArchetype.None;

    /// <summary>
    /// Final range (cells): skill-tree range plus the additive per-rank bonus. Applies
    /// to every tower type, including the cannon (which has no range trunk node).
    /// </summary>
    public float RangeCells =>
        TowerUpgrades.RangeCells(SkillStats.RangeCells(Type, _skillState), Rank);

    /// <summary>
    /// Current visual aim angle in radians. Read-only so aim behavior is testable
    /// without reading pixels. Before the first placement it reports the tower's
    /// default orientation.
    /// </summary>
    public float AimAngle => _aimSeeded ? _aimAngle : DefaultAimAngle;

    /// <summary>
    /// The angle the aim is easing toward this frame: the current target's direction
    /// when one is alive and in range, otherwise the tower's default orientation.
    /// </summary>
    public float DesiredAimAngle
    {
        get
        {
            var target = CurrentTarget;
            if (target == null || target.IsDead || !IsTargetInRange(target))
                return DefaultAimAngle;

            Vector2 toTarget = target.Position - Position;
            if (toTarget.LengthSquared() < 0.0001f)
                return DefaultAimAngle;

            return Mathf.Atan2(toTarget.Y, toTarget.X);
        }
    }

    /// <summary>
    /// The orientation this tower rests at when it has no target. Arrow points up
    /// (its triangle is drawn tip-up); cannon and laser point right (+X).
    /// </summary>
    public virtual float DefaultAimAngle => 0f;

    /// <summary>True while the short per-shot recoil cue is playing.</summary>
    public bool IsRecoiling => _recoilRemaining > 0f;

    /// <summary>
    /// Recoil cue progress (1 at the shot, decaying to 0). Transform-only: variants
    /// offset their body, never this tower's Position.
    /// </summary>
    public float RecoilProgress => GameConstants.TowerShotRecoilDuration > 0f
        ? Mathf.Clamp(_recoilRemaining / GameConstants.TowerShotRecoilDuration, 0f, 1f)
        : 0f;

    /// <summary>
    /// Moves <paramref name="current"/> toward <paramref name="target"/> by at most
    /// <paramref name="maxStep"/> radians, taking the shortest way around the circle.
    /// Pure, so the capped-approach contract is testable without a tower.
    /// </summary>
    public static float ApproachAngle(float current, float target, float maxStep)
    {
        if (!float.IsFinite(current))
            return float.IsFinite(target) ? target : 0f;
        if (!float.IsFinite(target) || !(maxStep > 0f))
            return current;

        float delta = Mathf.Wrap(target - current, -Mathf.Pi, Mathf.Pi);
        if (Mathf.Abs(delta) <= maxStep)
            return target;

        return current + Mathf.Sign(delta) * maxStep;
    }

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
    /// Rank that applies to a seed mechanic for this tower's current life. Seed
    /// mechanics are inert until the tower reaches rank 3 and chooses an archetype:
    /// before that every branch reads 0, and afterwards only the chosen branch applies
    /// (at least at rank 1). Skill-tree seed ranks are not consumed — they simply
    /// become effective when the archetype is chosen. Trunk nodes never route here.
    /// </summary>
    protected int SeedRankFor(TowerArchetype branch, string nodeId) =>
        Archetype == branch
            ? TowerArchetypes.EffectiveSeedRank(SkillRank(nodeId))
            : 0;

    // === In-level upgrades ===

    /// <summary>
    /// Credits kill XP to this tower. Fractional shares are accumulated exactly (never
    /// rounded per hit); negative or non-finite amounts are ignored.
    /// </summary>
    public void AddXp(float amount)
    {
        if (!(amount > 0f) || !float.IsFinite(amount))
            return;

        Xp += amount;
    }

    /// <summary>
    /// Raises the rank by exactly one for a completed purchase. Ranks 0 -> 1 and
    /// 1 -> 2 are the only generic steps: rank 2 -> 3 is the archetype choice, so
    /// there is no third chevron. Callers (GameManager) own the coin/XP transaction.
    /// </summary>
    internal bool ApplyRankUpgrade()
    {
        if (Rank >= GameConstants.TowerMaxRank - 1)
            return false;

        Rank++;
        UpdateRankBadge();
        return true;
    }

    /// <summary>
    /// Installs the chosen archetype and sets the tower to the final rank. Rejected
    /// unless the tower is exactly at rank 2, has no archetype yet, and the archetype
    /// belongs to this tower type. The only rank 2 -> 3 path.
    /// </summary>
    internal bool ApplyArchetype(TowerArchetype archetype)
    {
        if (Rank != GameConstants.TowerMaxRank - 1)
            return false;
        if (Archetype != TowerArchetype.None)
            return false;
        if (!TowerArchetypes.IsValidFor(Type, archetype))
            return false;

        Archetype = archetype;
        Rank = GameConstants.TowerMaxRank;
        OnArchetypeChanged();
        UpdateRankBadge();
        return true;
    }

    /// <summary>
    /// Called once after this tower's archetype is chosen. Variants repaint their own
    /// procedural body so the rank-3 silhouette reflects the specialization; there is
    /// no separate accent overlay. Default is a no-op.
    /// </summary>
    protected virtual void OnArchetypeChanged()
    {
    }

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

        // A freshly placed tower starts at its resting orientation with no recoil.
        _aimAngle = DefaultAimAngle;
        _aimSeeded = true;
        _recoilRemaining = 0f;
        OnAimUpdated();
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

        // Every successful shot kicks a short recoil cue. It is separate from the
        // cannon's cluster-only MuzzleFlash, so a single shell still fires visually
        // without ever setting IsMuzzleFlashing.
        _recoilRemaining = GameConstants.TowerShotRecoilDuration;

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
    /// Called after the aim angle advances (and from Initialize). Variants sync their
    /// body transforms here; the default is a no-op.
    /// </summary>
    protected virtual void OnAimUpdated()
    {
    }

    /// <summary>
    /// Show a circular range indicator centered on this tower.
    /// No-op if already shown.
    /// </summary>
    public void ShowRange()
    {
        if (_rangeIndicator != null) return;

        _rangeIndicator = new Control();
        _rangeIndicator.Name = "RangeIndicator";
        // Visual-only overlay: it must never swallow a UI click (the upgrade panel can
        // overlap the range circle).
        _rangeIndicator.MouseFilter = Control.MouseFilterEnum.Ignore;
        LayoutRangeIndicator();

        _rangeIndicator.Draw += () =>
        {
            if (_rangeIndicator == null) return;

            // Read the live range so a rank purchase that widens the range can repaint
            // the same control instead of replacing it.
            float radius = RangePixels;
            Vector2 center = _rangeIndicator.Size / 2f;

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
    /// Resizes and repaints a visible range indicator after a stat change (a rank
    /// purchase widens the range). No-op when the indicator is hidden.
    /// </summary>
    public void RefreshRangeIndicator()
    {
        if (_rangeIndicator == null)
            return;

        LayoutRangeIndicator();
        _rangeIndicator.QueueRedraw();
    }

    /// <summary>
    /// Sizes and centers the range control from the tower's current range. Shared by
    /// the initial show and the rank-purchase refresh so both stay in sync.
    /// </summary>
    private void LayoutRangeIndicator()
    {
        if (_rangeIndicator == null)
            return;

        float rangePixels = RangePixels;
        _rangeIndicator.Size = new Vector2(rangePixels * 2, rangePixels * 2);
        _rangeIndicator.Position = new Vector2(-rangePixels, -rangePixels);
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

    /// <summary>
    /// Creates (once) and repaints the tiny current-rank badge at the top-right of the
    /// tower's cell. Driven by rank purchases only — earning XP never creates or
    /// changes the badge. The badge never consumes mouse input, so it cannot block
    /// tower selection or upgrade-panel clicks.
    /// </summary>
    private void UpdateRankBadge()
    {
        if (Rank < 1)
            return;

        if (_rankBadge == null)
        {
            float halfCell = GameConstants.CellSize / 2f;
            var badge = new Control();
            badge.Name = "RankBadge";
            badge.Size = new Vector2(
                GameConstants.TowerRankBadgeWidth,
                GameConstants.TowerRankBadgeHeight);
            // Tower origin is the cell centre; the badge sits at the cell's top-right.
            badge.Position = new Vector2(
                halfCell - GameConstants.TowerRankBadgeWidth - GameConstants.TowerRankBadgeMargin,
                -halfCell + GameConstants.TowerRankBadgeMargin);
            badge.MouseFilter = Control.MouseFilterEnum.Ignore;
            // Keep the badge above the tower body no matter which order the body nodes
            // were added in.
            badge.ZIndex = 1;
            badge.Draw += DrawRankBadge;
            AddChild(badge);
            _rankBadge = badge;
        }

        _rankBadge.QueueRedraw();
    }

    private void DrawRankBadge()
    {
        if (_rankBadge == null || !IsInstanceValid(_rankBadge))
            return;

        RankIcon.DrawBadge(
            _rankBadge,
            new Rect2(Vector2.Zero, _rankBadge.Size),
            Rank,
            Archetype,
            TowerColor);
    }

    public override void _Process(double delta)
    {
        // Cooldown behavior is untouched: this is the only place it is advanced.
        if (_fireCooldownTimer > 0f)
        {
            _fireCooldownTimer -= (float)delta;
            if (_fireCooldownTimer < 0f)
                _fireCooldownTimer = 0f;
        }

        UpdateAim((float)delta);
    }

    /// <summary>
    /// Eases the visual aim toward <see cref="DesiredAimAngle"/> at the capped angular
    /// speed and decays the recoil cue. Never writes Position or any stat.
    /// </summary>
    private void UpdateAim(float delta)
    {
        if (!_aimSeeded)
        {
            _aimAngle = DefaultAimAngle;
            _aimSeeded = true;
        }

        if (delta > 0f && _recoilRemaining > 0f)
            _recoilRemaining = Mathf.Max(0f, _recoilRemaining - delta);

        _aimAngle = ApproachAngle(
            _aimAngle,
            DesiredAimAngle,
            GameConstants.TowerAimAngularSpeed * Mathf.Max(0f, delta));

        OnAimUpdated();
    }
}
