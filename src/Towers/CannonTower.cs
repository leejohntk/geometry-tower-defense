using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Cannon Tower: dark green filled circle with dark outline.
/// Same range as Arrow (4 cells), slower fire rate (2.5s), projectile explodes
/// on contact dealing AoE damage. A cluster volley (more than one shell) also
/// flashes the barrel so a multi-shell shot is distinguishable from a single one.
/// </summary>
public partial class CannonTower : Tower
{
    public override TowerType Type => TowerType.Cannon;
    public override float Damage =>
        SkillStats.CannonDamage(SkillRank(SkillTreeCatalog.CannonPowderCharge))
        * TowerUpgrades.DamageMultiplier(Rank);
    public override float FireRate =>
        TowerUpgrades.FireInterval(
            SkillStats.CannonFireRate(SkillRank(SkillTreeCatalog.CannonAttackSpeed)), Rank);
    public override int Cost => GameConstants.CannonTowerCost;
    public override float ProjectileSpeedMultiplier => SkillStats.CannonProjectileSpeedMultiplier(SkillRank(SkillTreeCatalog.CannonPowderCharge));

    /// <summary>
    /// Shells per volley. Seed mechanics are inert before rank 3 (always one shell).
    /// After choosing Cluster it is 1 + max(1, skill rank); after choosing Stun,
    /// cluster contributes nothing (one shell).
    /// </summary>
    public override int ClusterCount =>
        SkillMechanics.CannonClusterCount(
            SeedRankFor(TowerArchetype.Cluster, SkillTreeCatalog.CannonCluster));

    /// <summary>
    /// Per-hit stun chance. Inert (zero) before rank 3. After choosing Stun it is
    /// max(1, skill rank) x 0.06; after choosing Cluster it contributes zero.
    /// </summary>
    public override float StunChance =>
        SkillMechanics.CannonStunChance(
            SeedRankFor(TowerArchetype.Stun, SkillTreeCatalog.CannonStunChance));

    /// <summary>
    /// Final AoE radius (px) with the Splash Radius trunk node applied. This is a
    /// trunk-only stat: the per-rank range bonus never touches the splash radius.
    /// </summary>
    public override float SplashRadius => SkillStats.CannonSplashRadius(SkillRank(SkillTreeCatalog.CannonSplashRadius));
    protected override Color RangeColor => GameConstants.CannonTowerFillColor;

    /// <summary>
    /// The cannon's barrel is drawn along +X, so its resting aim points right.
    /// </summary>
    public override float DefaultAimAngle => 0f;

    // Cluster muzzle-flash state (presentation only). The flash lives on this node:
    // a Control that draws a ring + spikes, repainted only while the short timer runs.
    private Control? _muzzleFlash;
    private float _muzzleFlashRemaining = 0f;

    // The tower's own procedural body. Its silhouette branches on the chosen
    // archetype (lobed cluster or shockwave rings); there is no separate overlay.
    private Control? _body;
    private Vector2 _bodyBasePosition;

    /// <summary>
    /// True while the cluster volley's muzzle flash is playing.
    /// </summary>
    public bool IsMuzzleFlashing => _muzzleFlashRemaining > 0f;

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;

        _bodyBasePosition = new Vector2(-cell / 2f, -cell / 2f);
        _body = new Control();
        _body.Name = "CannonBody";
        _body.Size = new Vector2(cell, cell);
        _body.Position = _bodyBasePosition;
        // Rotate around the cell centre so the barrel reflects the aim direction
        // without moving the tower's simulation position.
        _body.PivotOffset = new Vector2(cell / 2f, cell / 2f);
        _body.MouseFilter = Control.MouseFilterEnum.Ignore;
        _body.Draw += DrawBody;
        AddChild(_body);

        // Muzzle flash overlay, centered on the tower and twice a cell wide so the
        // expanding ring and its spikes stay inside the control's bounds. Hidden while
        // idle: it only costs draws during the ~0.2s after a cluster volley.
        float flashSize = cell * 2f;
        _muzzleFlash = new Control();
        _muzzleFlash.Name = "MuzzleFlash";
        _muzzleFlash.Size = new Vector2(flashSize, flashSize);
        _muzzleFlash.Position = new Vector2(-flashSize / 2f, -flashSize / 2f);
        _muzzleFlash.MouseFilter = Control.MouseFilterEnum.Ignore;
        _muzzleFlash.Visible = false;
        _muzzleFlash.Draw += DrawMuzzleFlash;
        AddChild(_muzzleFlash);

        OnAimUpdated();
    }

    protected override void OnArchetypeChanged()
    {
        _body?.QueueRedraw();
    }

    /// <summary>
    /// Applies the aim/recoil transforms: the whole body rotates to the aim angle and
    /// kicks back along the aim axis while the recoil cue runs. Every shot recoils,
    /// independent of the cluster-only MuzzleFlash. Never writes Position.
    /// </summary>
    protected override void OnAimUpdated()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        _body.Rotation = AimAngle - DefaultAimAngle;

        float recoil = GameConstants.TowerRecoilDistance * RecoilProgress;
        var aim = new Vector2(Mathf.Cos(AimAngle), Mathf.Sin(AimAngle));
        _body.Position = _bodyBasePosition - aim * recoil;
    }

    /// <summary>
    /// Paints the cannon silhouette for the current archetype:
    /// none = circle, outline, and inner barrel; Cluster = three overlapping lobe
    /// circles; Stun = circle with concentric shockwave rings (no barrel).
    /// </summary>
    private void DrawBody()
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4f;
        Vector2 center = new(cell / 2f, cell / 2f);
        Color fill = GameConstants.CannonTowerFillColor;
        Color outline = GameConstants.CannonTowerOutlineColor;

        switch (Archetype)
        {
            case TowerArchetype.Cluster:
                DrawClusterBody(center, radius, fill, outline);
                break;
            case TowerArchetype.Stun:
                _body.DrawCircle(center, radius, fill);
                _body.DrawCircle(center, radius, outline, false, 2f);
                _body.DrawCircle(center, radius * 0.68f, new Color(0.55f, 0.85f, 0.6f), false, 2f);
                _body.DrawCircle(center, radius * 0.36f, new Color(0.55f, 0.85f, 0.6f), false, 2f);
                break;
            default:
                _body.DrawCircle(center, radius, fill);
                _body.DrawCircle(center, radius, outline, false, 2f);
                // Directional barrel along +X: body rotation turns it toward the aim.
                float barrelLength = radius * 0.95f;
                float barrelHalfHeight = radius * 0.24f;
                var barrel = new Rect2(
                    center.X,
                    center.Y - barrelHalfHeight,
                    barrelLength,
                    barrelHalfHeight * 2f);
                _body.DrawRect(barrel, new Color(0.12f, 0.28f, 0.17f));
                _body.DrawRect(barrel, outline, filled: false, width: 2f);
                _body.DrawCircle(
                    new Vector2(center.X + barrelLength, center.Y),
                    barrelHalfHeight * 0.85f,
                    new Color(0.08f, 0.2f, 0.12f));
                break;
        }
    }

    /// <summary>
    /// Cluster: three overlapping filled circles arranged in a triangle around the
    /// centre, each with a dark outline — a lobed silhouette with no single barrel.
    /// </summary>
    private void DrawClusterBody(Vector2 center, float radius, Color fill, Color outline)
    {
        if (_body == null || !IsInstanceValid(_body))
            return;

        float lobeRadius = radius * 0.45f;
        float spread = radius * 0.38f;
        var lobes = new Vector2[3];
        for (int i = 0; i < lobes.Length; i++)
        {
            float angle = Mathf.Tau * i / lobes.Length - Mathf.Pi / 2f;
            lobes[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
        }

        foreach (var lobe in lobes)
            _body.DrawCircle(lobe, lobeRadius, fill);
        foreach (var lobe in lobes)
            _body.DrawCircle(lobe, lobeRadius, outline, false, 2f);
    }

    /// <summary>
    /// Starts the muzzle flash for a cluster volley. A single-shell shot stays silent so
    /// the cue reads specifically as "that was a cluster".
    /// </summary>
    protected override void OnFired(Vector2 targetPos)
    {
        if (ClusterCount <= 1)
            return;

        _muzzleFlashRemaining = GameConstants.CannonMuzzleFlashDuration;

        if (_muzzleFlash != null)
        {
            _muzzleFlash.Visible = true;
            _muzzleFlash.QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_muzzleFlashRemaining <= 0f)
            return;

        _muzzleFlashRemaining = Mathf.Max(0f, _muzzleFlashRemaining - (float)delta);

        if (_muzzleFlash == null)
            return;

        if (_muzzleFlashRemaining <= 0f)
        {
            _muzzleFlash.Visible = false;
            return;
        }

        _muzzleFlash.QueueRedraw();
    }

    /// <summary>
    /// Draws the flash: a ring expanding from the barrel edge outward, plus radial spikes
    /// that shrink as it spreads, all fading out. No cached buffers and no allocation —
    /// every point is struct math from the elapsed timer.
    /// </summary>
    private void DrawMuzzleFlash()
    {
        if (_muzzleFlash == null || !IsInstanceValid(_muzzleFlash))
            return;

        float cell = GameConstants.CellSize;
        float barrelRadius = cell / 2f - 4f;
        float progress = 1f - Mathf.Clamp(
            _muzzleFlashRemaining / GameConstants.CannonMuzzleFlashDuration, 0f, 1f);

        float ringRadius = barrelRadius * Mathf.Lerp(1f, GameConstants.CannonMuzzleFlashRingScale, progress);
        Color color = GameConstants.CannonMuzzleFlashColor with { A = 1f - progress };
        Vector2 center = new Vector2(cell, cell); // control is two cells wide, tower at its center

        _muzzleFlash.DrawCircle(
            center, ringRadius, color, false, GameConstants.CannonMuzzleFlashStrokeWidth);

        int spikes = GameConstants.CannonMuzzleFlashSpikes;
        float spikeLength = GameConstants.CannonMuzzleFlashSpikeLength * (1f - progress);
        for (int i = 0; i < spikes; i++)
        {
            float angle = i * (System.MathF.Tau / spikes);
            var direction = new Vector2(System.MathF.Cos(angle), System.MathF.Sin(angle));
            _muzzleFlash.DrawLine(
                center + direction * ringRadius,
                center + direction * (ringRadius + spikeLength),
                color,
                GameConstants.CannonMuzzleFlashStrokeWidth
            );
        }
    }
}
