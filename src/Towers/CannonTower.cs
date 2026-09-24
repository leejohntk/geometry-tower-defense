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
    public override int Damage => SkillStats.CannonDamage(SkillRank(SkillTreeCatalog.CannonPowderCharge));
    public override float FireRate => SkillStats.CannonFireRate(SkillRank(SkillTreeCatalog.CannonAttackSpeed));
    public override int Cost => GameConstants.CannonTowerCost;
    public override float ProjectileSpeedMultiplier => SkillStats.CannonProjectileSpeedMultiplier(SkillRank(SkillTreeCatalog.CannonPowderCharge));
    public override int ClusterCount => SkillMechanics.CannonClusterCount(SkillRank(SkillTreeCatalog.CannonCluster));
    public override float StunChance => SkillMechanics.CannonStunChance(SkillRank(SkillTreeCatalog.CannonStunChance));

    /// <summary>
    /// Final AoE radius (px) with the Splash Radius node applied.
    /// </summary>
    public override float SplashRadius => SkillStats.CannonSplashRadius(SkillRank(SkillTreeCatalog.CannonSplashRadius));
    protected override Color RangeColor => new Color(0.3f, 0.6f, 0.4f);

    // Cluster muzzle-flash state (presentation only). The flash lives on this node:
    // a Control that draws a ring + spikes, repainted only while the short timer runs.
    private Control? _muzzleFlash;
    private float _muzzleFlashRemaining = 0f;

    /// <summary>
    /// True while the cluster volley's muzzle flash is playing.
    /// </summary>
    public bool IsMuzzleFlashing => _muzzleFlashRemaining > 0f;

    public override void _Ready()
    {
        float cell = GameConstants.CellSize;
        float radius = cell / 2f - 4;

        var body = new Control();
        body.Name = "CannonBody";
        body.Size = new Vector2(cell, cell);
        body.Position = new Vector2(-cell / 2f, -cell / 2f);
        body.MouseFilter = Control.MouseFilterEnum.Ignore;
        body.Draw += () =>
        {
            if (!IsInstanceValid(body)) return;

            Vector2 center = new Vector2(cell / 2f, cell / 2f);

            // Filled circle (cannon barrel) with dark outline
            body.DrawCircle(center, radius, new Color(0.3f, 0.6f, 0.4f));
            body.DrawCircle(center, radius, new Color(0.1f, 0.25f, 0.15f), false, 2.0f);

            // Small inner barrel circle
            body.DrawCircle(center, radius * 0.4f, new Color(0.12f, 0.28f, 0.17f));
        };
        AddChild(body);

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
