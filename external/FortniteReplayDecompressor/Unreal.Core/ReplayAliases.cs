// Added for Fortnite Chest Tracker (see PATCHES.md). Not part of the original library.
namespace Unreal.Core;

/// <summary>
/// Lets the application read group paths the parser has no type for, by naming an equivalent group that it does
/// have (for example a new chest blueprint read as a generic container). Each lookup that finds nothing is
/// remembered per parser, so a path is only resolved once.
/// </summary>
public static class ReplayAliases
{
    /// <summary>Unknown export group path → path of a registered group to read it as, or null.</summary>
    public static Func<string, string?>? Group { get; set; }

    /// <summary>Unknown ClassNetCache path → path of a registered ClassNetCache to read it as, or null.</summary>
    public static Func<string, string?>? ClassNetCache { get; set; }
}
