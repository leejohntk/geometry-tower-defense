using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace GeometryTowerDefense.Tests;

/// <summary>
/// Shared node-lifecycle and viewport setup for the UI/visual test suites: attaching
/// nodes to the real scene tree, detaching/queue-freeing them, and applying the project
/// viewport size so real rects are meaningful.
///
/// The viewport helper fails loudly (assertion failure, like MainTest) when the project
/// settings are missing or the assignment does not take, rather than silently measuring
/// at the headless 64x64 size.
/// </summary>
public static class NodeTestSupport
{
    private const string ViewportWidthSetting = "display/window/size/viewport_width";
    private const string ViewportHeightSetting = "display/window/size/viewport_height";

    /// <summary>Adds the node to the scene root (running _Ready) and returns it.</summary>
    public static T Attach<T>(T node) where T : Node
    {
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(node);
        return node;
    }

    /// <summary>Removes the node from its parent and queues it for freeing.</summary>
    public static void Detach(Node node)
    {
        node.GetParent()?.RemoveChild(node);
        node.QueueFree();
    }

    /// <summary>Alias for <see cref="Detach"/> used by suites that only add nodes once.</summary>
    public static void Cleanup(Node node) => Detach(node);

    /// <summary>
    /// The viewport size configured in project.godot. Fails loudly when either setting
    /// is missing instead of silently falling back to an arbitrary size.
    /// </summary>
    public static Vector2I ProjectViewportSize()
    {
        AssertThat(ProjectSettings.HasSetting(ViewportWidthSetting) &&
                   ProjectSettings.HasSetting(ViewportHeightSetting))
            .OverrideFailureMessage(
                $"ProjectSettings is missing {ViewportWidthSetting} / {ViewportHeightSetting}; " +
                "the test cannot establish the real game viewport size.")
            .IsTrue();

        return new Vector2I(
            ProjectSettings.GetSetting(ViewportWidthSetting).AsInt32(),
            ProjectSettings.GetSetting(ViewportHeightSetting).AsInt32());
    }

    /// <summary>
    /// Applies the project viewport size to the scene root and asserts the assignment
    /// took, so rect-based assertions never measure the headless 64x64 default.
    /// </summary>
    public static void ApplyProjectViewportSize()
    {
        Vector2I expected = ProjectViewportSize();
        var root = (Window)((SceneTree)Engine.GetMainLoop()).Root;
        root.Size = expected;

        AssertThat(root.Size)
            .OverrideFailureMessage(
                $"Root viewport size did not take: expected {expected} from " +
                $"{ViewportWidthSetting} / {ViewportHeightSetting}, got {root.Size}.")
            .IsEqual(expected);
    }
}
