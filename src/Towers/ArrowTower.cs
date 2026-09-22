using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Arrow Tower: upward-pointing blue triangle.
/// Targets nearest enemy within 4-cell range, fires every 1.5s, deals 10 damage.
/// </summary>
public partial class ArrowTower : Tower
{
    public override TowerType Type => TowerType.Arrow;
    public override int Damage => SkillStats.ArrowDamage(SkillRank(SkillTreeCatalog.ArrowDamage));
    public override float FireRate => SkillStats.ArrowFireRate(SkillRank(SkillTreeCatalog.ArrowAttackSpeed));
    public override int Cost => GameConstants.ArrowTowerCost;
    protected override Color RangeColor => new Color(0.2f, 0.5f, 1.0f);

    public override void _Ready()
    {
        // Draw the tower as a blue upward-pointing triangle
        var trianglePoints = new Vector2[]
        {
            new Vector2(0, -(GameConstants.CellSize / 2f - 4)),                            // Top center
            new Vector2(-(GameConstants.CellSize / 2f - 4), GameConstants.CellSize / 2f - 4), // Bottom left
            new Vector2(GameConstants.CellSize / 2f - 4, GameConstants.CellSize / 2f - 4)     // Bottom right
        };

        var triangle = new Polygon2D();
        triangle.Polygon = trianglePoints;
        triangle.Color = new Color(0.2f, 0.5f, 1.0f); // Blue fill
        AddChild(triangle);

        // Draw outline (slightly larger behind for outline effect)
        var outlineBg = new Polygon2D();
        outlineBg.Polygon = new Vector2[]
        {
            new Vector2(0, -(GameConstants.CellSize / 2f - 2)),                            // Top center
            new Vector2(-(GameConstants.CellSize / 2f - 2), GameConstants.CellSize / 2f - 2), // Bottom left
            new Vector2(GameConstants.CellSize / 2f - 2, GameConstants.CellSize / 2f - 2)     // Bottom right
        };
        outlineBg.Color = new Color(0.05f, 0.1f, 0.4f);
        triangle.AddChild(outlineBg);
    }
}
