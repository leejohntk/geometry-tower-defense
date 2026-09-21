using Godot;

namespace GeometryTowerDefense;

/// <summary>
/// Serializes <see cref="SkillTreeState"/> to and from a Godot <see cref="ConfigFile"/>,
/// backed by <c>user://skilltree.cfg</c>. The flat save shape is one section with a
/// Skill Point key plus one key per node id (value = rank).
///
/// Writes are atomic: the config is written to a <c>.tmp</c> sibling and then renamed
/// over the target, so a crash or kill mid-write can never truncate the previous good
/// save. Missing files or missing keys load as defaults (0 SP, 0 ranks), so a first run
/// never needs a pre-seeded save.
/// </summary>
public static class SkillTreeSave
{
    public const string FilePath = "user://skilltree.cfg";

    private const string Section = "skilltree";
    private const string KeySkillPoints = "sp";
    private const string TempSuffix = ".tmp";

    /// <summary>
    /// Atomically writes the state to the default save file. Returns true on success.
    /// </summary>
    public static bool Save(SkillTreeState state) => Save(state, FilePath);

    /// <summary>
    /// Atomically writes the state to the given file path (exposed for testing).
    /// Returns true on success; the previous file (if any) stays intact on failure.
    /// </summary>
    public static bool Save(SkillTreeState state, string path)
    {
        var config = ToConfigFile(state);
        string tempPath = path + TempSuffix;

        if (config.Save(tempPath) != Error.Ok)
            return false;

        if (DirAccess.RenameAbsolute(tempPath, path) != Error.Ok)
        {
            // Orphaned temp file cleanup; the previous save (if any) is untouched.
            DirAccess.RemoveAbsolute(tempPath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads the state from the default save file (defaults on first run).
    /// </summary>
    public static SkillTreeState Load() => Load(FilePath);

    /// <summary>
    /// Loads the state from the given file path (defaults when missing or unreadable).
    /// A leftover <c>.tmp</c> from a failed atomic write is ignored — only the target
    /// path is ever read.
    /// </summary>
    public static SkillTreeState Load(string path)
    {
        var config = new ConfigFile();
        if (config.Load(path) != Error.Ok)
            return new SkillTreeState();
        return FromConfigFile(config);
    }

    /// <summary>
    /// Serializes a state into an in-memory ConfigFile (no file I/O — testable).
    /// </summary>
    public static ConfigFile ToConfigFile(SkillTreeState state)
    {
        var config = new ConfigFile();
        config.SetValue(Section, KeySkillPoints, state.SkillPoints);
        foreach (var node in SkillTreeCatalog.All)
            config.SetValue(Section, node.Id, state.GetRank(node.Id));
        return config;
    }

    /// <summary>
    /// Rebuilds a state from an in-memory ConfigFile. Unknown, missing, non-integer,
    /// or out-of-int32-range values fall back to 0; <see cref="SkillTreeState.SetRank"/>
    /// additionally rejects unknown/disabled node ids and clamps rank to the max.
    /// </summary>
    public static SkillTreeState FromConfigFile(ConfigFile config)
    {
        var state = new SkillTreeState();
        state.SetSkillPoints(ReadIntSafe(config, KeySkillPoints));
        foreach (var node in SkillTreeCatalog.All)
            state.SetRank(node.Id, ReadIntSafe(config, node.Id));
        return state;
    }

    /// <summary>
    /// Reads an int from the save section, returning 0 when the stored value is not
    /// a sane integer (wrong Variant type, or outside int32 range).
    /// </summary>
    private static int ReadIntSafe(ConfigFile config, string key)
    {
        var value = config.GetValue(Section, key, 0);
        if (value.VariantType != Variant.Type.Int)
            return 0;

        long raw = value.AsInt64();
        if (raw < int.MinValue || raw > int.MaxValue)
            return 0;

        return (int)raw;
    }
}
