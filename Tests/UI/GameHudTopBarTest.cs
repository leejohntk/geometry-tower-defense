using System.Collections.Generic;
using GeometryTowerDefense;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// In-game top status bar: named stat groups with procedural (non-text) icons, uppercase
/// captions, the historical value labels and text formats, click-through decorations, a
/// tidy left-aligned layout inside the 40px bar, and unchanged signal updates.
/// </summary>
[TestSuite]
public class GameHudTopBarTest
{
    private static readonly (string Group, string Icon, string Caption, string ValueName, string ValueText)[] Stats =
    {
        ("Block_Health", "HpIcon", "HEALTH", "HpLabel", "3"),
        ("Block_Coins", "CoinIcon", "COINS", "CoinsLabel", "10"),
        ("Block_Wave", "WaveIcon", "WAVE", "WaveLabel", "0/5"),
    };

    private static GameHUD BuildHud()
    {
        NodeTestSupport.ApplyProjectViewportSize();
        return NodeTestSupport.Attach(new GameHUD());
    }

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - 0.01f
        && inner.Position.Y >= outer.Position.Y - 0.01f
        && inner.End.X <= outer.End.X + 0.01f
        && inner.End.Y <= outer.End.Y + 0.01f;

    private static void CollectLabelTexts(Node node, List<string> texts)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Label label)
                texts.Add(label.Text);
            CollectLabelTexts(child, texts);
        }
    }

    [TestCase]
    public void TopBar_KeepsItsRectAndHasThreeNamedStatGroups()
    {
        var hud = BuildHud();
        try
        {
            var bar = hud.GetNodeOrNull<Control>("Block_TopBar");
            AssertThat(bar).IsNotNull();
            var viewport = NodeTestSupport.ProjectViewportSize();
            AssertThat(bar!.Position).IsEqual(Vector2.Zero);
            AssertThat(bar.Size).IsEqual(new Vector2(viewport.X, GameConstants.TopBarHeight));
            AssertThat(bar.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

            foreach (var stat in Stats)
            {
                var group = bar.GetNodeOrNull<Control>(stat.Group);
                AssertThat(group).IsNotNull();
                AssertThat(group!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(group.Size).IsEqual(new Vector2(128f, GameConstants.TopBarHeight));

                var icon = group.GetNodeOrNull<Control>(stat.Icon);
                AssertThat(icon).IsNotNull();
                AssertThat(icon is Label).IsFalse();
                AssertThat(icon!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(icon.Size).IsEqual(new Vector2(28f, 28f));
                AssertThat(icon.GetChildCount()).IsEqual(0); // procedural draw only

                var caption = group.GetNodeOrNull<Label>("Caption");
                AssertThat(caption).IsNotNull();
                AssertThat(caption!.Text).IsEqual(stat.Caption);
                AssertThat(caption.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);

                var value = group.GetNodeOrNull<Label>(stat.ValueName);
                AssertThat(value).IsNotNull();
                AssertThat(value!.Text).IsEqual(stat.ValueText);
                AssertThat(value.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
                AssertThat(value.GetThemeFontSize("font_size")).IsGreaterEqual(18);
                AssertThat(value.GetThemeFontSize("font_size")).IsLessEqual(20);

                // All content inside the group, and the group inside the bar.
                Rect2 groupRect = group.GetGlobalRect();
                AssertThat(Contains(bar.GetGlobalRect(), groupRect)).IsTrue();
                AssertThat(Contains(groupRect, icon.GetGlobalRect())).IsTrue();
                AssertThat(Contains(groupRect, caption.GetGlobalRect())).IsTrue();
                AssertThat(Contains(groupRect, value.GetGlobalRect())).IsTrue();
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Icons_AreProceduralControls_AndTextPlaceholdersAreGone()
    {
        var hud = BuildHud();
        try
        {
            var bar = hud.GetNode<Control>("Block_TopBar");
            var texts = new List<string>();
            CollectLabelTexts(bar, texts);

            foreach (var text in texts)
            {
                AssertThat(text.Contains("[HP]")).IsFalse();
                AssertThat(text.Contains("[$]")).IsFalse();
                AssertThat(text == "Wave").IsFalse();
            }

            AssertThat(texts.Contains("HEALTH")).IsTrue();
            AssertThat(texts.Contains("COINS")).IsTrue();
            AssertThat(texts.Contains("WAVE")).IsTrue();

            foreach (var stat in Stats)
            {
                var icon = bar.GetNode<Control>($"{stat.Group}/{stat.Icon}");
                AssertThat(icon is Label).IsFalse();
                AssertThat(icon is TextureRect).IsFalse();
            }
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Decorations_AreClickThroughAndInsideTheBar()
    {
        var hud = BuildHud();
        try
        {
            var bar = hud.GetNode<Control>("Block_TopBar");
            Rect2 barRect = bar.GetGlobalRect();
            var viewport = NodeTestSupport.ProjectViewportSize();

            var background = bar.GetNodeOrNull<ColorRect>("Background");
            AssertThat(background).IsNotNull();
            AssertThat(background!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(Contains(barRect, background.GetGlobalRect())).IsTrue();

            var pattern = bar.GetNodeOrNull<Control>("Pattern");
            AssertThat(pattern).IsNotNull();
            AssertThat(pattern!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            AssertThat(Contains(barRect, pattern.GetGlobalRect())).IsTrue();
            AssertThat(pattern.Size).IsEqual(new Vector2(viewport.X, GameConstants.TopBarHeight));

            var border = bar.GetNodeOrNull<Control>("Border");
            AssertThat(border).IsNotNull();
            AssertThat(border!.MouseFilter).IsEqual(Control.MouseFilterEnum.Ignore);
            Rect2 borderRect = border.GetGlobalRect();
            AssertThat(Contains(barRect, borderRect)).IsTrue();
            AssertThat(borderRect.Size.Y).IsEqual(2f);
            AssertThat(borderRect.End.Y).IsEqual(barRect.End.Y);
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void Groups_AreLeftAlignedEvenlySpacedAndDoNotOverlap()
    {
        var hud = BuildHud();
        try
        {
            var bar = hud.GetNode<Control>("Block_TopBar");
            Rect2 barRect = bar.GetGlobalRect();

            Rect2 health = bar.GetNode<Control>("Block_Health").GetGlobalRect();
            Rect2 coins = bar.GetNode<Control>("Block_Coins").GetGlobalRect();
            Rect2 wave = bar.GetNode<Control>("Block_Wave").GetGlobalRect();

            AssertThat(health.Position.X).IsLess(coins.Position.X);
            AssertThat(coins.Position.X).IsLess(wave.Position.X);
            AssertThat(health.Intersects(coins)).IsFalse();
            AssertThat(coins.Intersects(wave)).IsFalse();
            AssertThat(health.Intersects(wave)).IsFalse();
            AssertThat(Contains(barRect, health)).IsTrue();
            AssertThat(Contains(barRect, coins)).IsTrue();
            AssertThat(Contains(barRect, wave)).IsTrue();

            // Uniform gap between neighbouring groups.
            AssertThat(coins.Position.X - health.End.X)
                .IsEqualApprox(wave.Position.X - coins.End.X, 0.01f);
        }
        finally
        {
            NodeTestSupport.Cleanup(hud);
        }
    }

    [TestCase]
    public void SignalUpdates_StillRefreshTheValueLabels()
    {
        var hud = BuildHud();
        var manager = new GameManager();
        try
        {
            manager.Initialize(Levels.Level1);
            hud.ConfigureForLevel(Levels.Level1);
            hud.ConnectToGameManager(manager);

            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Health/HpLabel").Text).IsEqual("3");
            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Coins/CoinsLabel").Text).IsEqual("10");
            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Wave/WaveLabel").Text).IsEqual("0/5");

            manager.EmitSignal(GameManager.SignalName.HpChanged, 2);
            manager.EmitSignal(GameManager.SignalName.CoinsChanged, 7);
            manager.EmitSignal(GameManager.SignalName.WaveChanged, 3);

            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Health/HpLabel").Text).IsEqual("2");
            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Coins/CoinsLabel").Text).IsEqual("7");
            AssertThat(hud.GetNode<Label>("Block_TopBar/Block_Wave/WaveLabel").Text).IsEqual("3/5");
        }
        finally
        {
            manager.QueueFree();
            NodeTestSupport.Cleanup(hud);
        }
    }
}
