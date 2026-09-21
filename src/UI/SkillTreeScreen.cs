using Godot;
using System.Collections.Generic;

namespace GeometryTowerDefense;

/// <summary>
/// Title-screen skill tree. Shows the persistent Skill Point balance and one panel per
/// tower listing its nodes. Enabled (stat) nodes show a buy button; disabled (mechanic)
/// nodes render greyed with a "coming soon" label. Built programmatically to match the
/// rest of the codebase's UI (no .tscn).
/// </summary>
public partial class SkillTreeScreen : Control
{
    [Signal]
    public delegate void BackPressedEventHandler();

    private readonly SkillTree _skillTree;
    private Label? _spLabel;
    private readonly Dictionary<string, (Label RankLabel, Control PipControl, Button BuyButton)> _rows = new();

    public SkillTreeScreen(SkillTree skillTree)
    {
        _skillTree = skillTree;
    }

    public override void _Ready()
    {
        BuildUi();
        Refresh();
    }

    /// <summary>
    /// Re-reads the skill-tree state and updates SP, ranks, pips, and button states.
    /// Only enabled (buyable) nodes are registered, so each row maps 1:1 to a buy button.
    /// </summary>
    public void Refresh()
    {
        if (_spLabel == null)
            return;

        _spLabel.Text = $"Skill Points: {_skillTree.State.SkillPoints}";

        foreach (var (nodeId, row) in _rows)
        {
            int rank = _skillTree.State.GetRank(nodeId);
            row.RankLabel.Text = $"{rank}/{GameConstants.SkillMaxRanks}";
            row.PipControl.QueueRedraw();
            row.BuyButton.Disabled = !_skillTree.State.CanBuyRank(nodeId);
            row.BuyButton.Text = rank >= GameConstants.SkillMaxRanks
                ? "MAX"
                : $"Buy ({GameConstants.SkillNodeRankCost} SP)";
        }
    }

    private void BuildUi()
    {
        var viewportSize = GetViewportRect().Size;

        // Dark background matching the title screen.
        var bg = new ColorRect();
        bg.Size = viewportSize;
        bg.Color = new Color(0.05f, 0.05f, 0.1f);
        AddChild(bg);

        // Header
        var title = new Label();
        title.Text = "SKILL TREE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.Position = new Vector2(0, 20);
        title.Size = new Vector2(viewportSize.X, 50);
        title.AddThemeFontSizeOverride("font_size", 40);
        title.AddThemeColorOverride("font_color", new Color(0.8f, 0.85f, 1.0f));
        AddChild(title);

        // Skill Point readout
        _spLabel = new Label();
        _spLabel.Text = "Skill Points: 0";
        _spLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _spLabel.Position = new Vector2(0, 78);
        _spLabel.Size = new Vector2(viewportSize.X, 30);
        _spLabel.AddThemeFontSizeOverride("font_size", 22);
        _spLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        AddChild(_spLabel);

        // Back button
        var backButton = new Button();
        backButton.Text = "Back";
        backButton.Position = new Vector2(20, 18);
        backButton.Size = new Vector2(120, 44);
        backButton.Pressed += () => EmitSignal(SignalName.BackPressed);
        backButton.AddThemeFontSizeOverride("font_size", 18);
        AddChild(backButton);

        // Three tower panels side by side.
        const float margin = 20f;
        float panelWidth = (viewportSize.X - margin * 4f) / 3f;
        float panelTop = 130f;
        float panelHeight = viewportSize.Y - panelTop - 40f;

        var towers = new (TowerType Type, string Header)[]
        {
            (TowerType.Arrow, "ARROW TOWER\nSharpshooter | Barrage"),
            (TowerType.Cannon, "CANNON TOWER\nBombardier | Concussive"),
            (TowerType.Laser, "LASER TOWER\nArc/Chain | Melter")
        };

        for (int i = 0; i < towers.Length; i++)
        {
            var (type, header) = towers[i];
            float x = margin * (i + 1) + panelWidth * i;
            BuildTowerPanel(type, header, TowerColor(type), x, panelTop, panelWidth, panelHeight);
        }
    }

    private void BuildTowerPanel(TowerType type, string header, Color color, float x, float y, float width, float height)
    {
        var panelBg = new ColorRect();
        panelBg.Position = new Vector2(x, y);
        panelBg.Size = new Vector2(width, height);
        panelBg.Color = new Color(0.08f, 0.08f, 0.16f, 0.9f);
        AddChild(panelBg);

        var headerLabel = new Label();
        headerLabel.Text = header;
        headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        headerLabel.Position = new Vector2(x, y + 10);
        headerLabel.Size = new Vector2(width, 44);
        headerLabel.AddThemeFontSizeOverride("font_size", 18);
        headerLabel.AddThemeColorOverride("font_color", color);
        AddChild(headerLabel);

        const float rowHeight = 46f;
        float rowY = y + 64f;
        foreach (var node in SkillTreeCatalog.ForTower(type))
        {
            BuildNodeRow(node, color, x + 12f, rowY, width - 24f);
            rowY += rowHeight;
        }
    }

    private void BuildNodeRow(SkillNodeDefinition node, Color color, float x, float y, float width)
    {
        // Geometric shape icon: circle for trunk, diamond for seed.
        var icon = CreateShapeIcon(node.Kind, node.Enabled, color);
        icon.Position = new Vector2(x, y + 12f);
        AddChild(icon);

        var nameLabel = new Label();
        nameLabel.Text = node.DisplayName;
        nameLabel.Position = new Vector2(x + 30f, y + 13f);
        nameLabel.Size = new Vector2(150f, 20f);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color",
            node.Enabled ? new Color(0.9f, 0.9f, 1.0f) : new Color(0.45f, 0.45f, 0.5f));
        AddChild(nameLabel);

        // Rank pips (filled count = current rank).
        var pips = CreatePipControl(node.Id, color, node.Enabled);
        pips.Position = new Vector2(x + 190f, y + 12f);
        AddChild(pips);

        var rankLabel = new Label();
        rankLabel.Text = "0/5";
        rankLabel.Position = new Vector2(x + 272f, y + 13f);
        rankLabel.Size = new Vector2(44f, 20f);
        rankLabel.AddThemeFontSizeOverride("font_size", 14);
        rankLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 1.0f));
        AddChild(rankLabel);

        if (node.Enabled)
        {
            var buyButton = new Button();
            buyButton.Text = $"Buy ({GameConstants.SkillNodeRankCost} SP)";
            buyButton.Position = new Vector2(x + 318f, y + 8f);
            buyButton.Size = new Vector2(124f, 30f);
            buyButton.AddThemeFontSizeOverride("font_size", 12);
            buyButton.Pressed += () => OnBuyPressed(node.Id);
            AddChild(buyButton);
            _rows[node.Id] = (rankLabel, pips, buyButton);
        }
        else
        {
            // Disabled mechanic nodes are greyed with a "coming soon" label and are
            // deliberately NOT registered in _rows: they can never change rank.
            var comingSoon = new Label();
            comingSoon.Text = "coming soon";
            comingSoon.Position = new Vector2(x + 318f, y + 13f);
            comingSoon.Size = new Vector2(124f, 20f);
            comingSoon.AddThemeFontSizeOverride("font_size", 12);
            comingSoon.AddThemeColorOverride("font_color", new Color(0.45f, 0.45f, 0.5f));
            AddChild(comingSoon);
        }
    }

    private void OnBuyPressed(string nodeId)
    {
        _skillTree.BuyRank(nodeId);
        Refresh();
    }

    private Control CreateShapeIcon(SkillNodeKind kind, bool enabled, Color color)
    {
        var icon = new Control();
        icon.Size = new Vector2(22f, 22f);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Draw += () =>
        {
            if (!IsInstanceValid(icon)) return;

            var center = new Vector2(11f, 11f);
            Color fill = enabled ? color : new Color(0.3f, 0.3f, 0.35f);

            if (kind == SkillNodeKind.Trunk)
            {
                icon.DrawCircle(center, 8f, fill);
                icon.DrawCircle(center, 8f, new Color(0.05f, 0.05f, 0.1f), false, 1.5f);
            }
            else
            {
                var diamond = new Vector2[]
                {
                    new Vector2(center.X, 3f),
                    new Vector2(19f, center.Y),
                    new Vector2(center.X, 19f),
                    new Vector2(3f, center.Y)
                };
                icon.DrawPolygon(diamond, new[] { fill });
            }
        };
        return icon;
    }

    private Control CreatePipControl(string nodeId, Color color, bool enabled)
    {
        var pips = new Control();
        pips.Size = new Vector2(GameConstants.SkillMaxRanks * 14f, 22f);
        pips.MouseFilter = MouseFilterEnum.Ignore;
        pips.Draw += () =>
        {
            if (!IsInstanceValid(pips)) return;

            int rank = _skillTree.State.GetRank(nodeId);
            for (int i = 0; i < GameConstants.SkillMaxRanks; i++)
            {
                var center = new Vector2(i * 14f + 7f, 11f);

                if (!enabled)
                {
                    pips.DrawCircle(center, 4f, new Color(0.3f, 0.3f, 0.35f));
                }
                else if (i < rank)
                {
                    pips.DrawCircle(center, 4f, color);
                }
                else
                {
                    pips.DrawCircle(center, 4f, new Color(0.15f, 0.15f, 0.2f), false, 1.5f);
                }
            }
        };
        return pips;
    }

    private static Color TowerColor(TowerType type) => type switch
    {
        TowerType.Cannon => new Color(0.3f, 0.6f, 0.4f),
        TowerType.Laser => new Color(0.8f, 0.3f, 1.0f),
        _ => new Color(0.2f, 0.5f, 1.0f)
    };
}
