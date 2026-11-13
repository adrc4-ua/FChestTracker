namespace FCT.Analysis;

/// <summary>Names of treasure-hunt rewards: the player's own (treasures.json) and the official gizmo names.</summary>
public static class TreasureNames
{
    /// <summary>Prize id → name chosen by the player (treasures.json), case-insensitive.</summary>
    public static Dictionary<string, string> PrizeNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gizmos carry no prize id in the replay; FCT gives them an internal key with this prefix (never shown).</summary>
    public const string UnknownPrefix = "Unknown:";
    public const string UnknownGizmo = "Treasure gizmo";

    public static bool IsGizmoKey(string id) => id.StartsWith(UnknownPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Weapon blueprint fragments of each gizmo (lower case) and the name the game shows for it.</summary>
    private static readonly (string[] Fragments, string Name)[] Gizmos =
    {
        (new[] { "flameceleb" }, "Golden Firework"),
        (new[] { "relicextractor" }, "Portable Extractor"),
        (new[] { "relicfinder" }, "Sprite Locator"),
        (new[] { "luckylocator" }, "Lucky Locator"),
        (new[] { "smugglerextractionkey", "extractionaccelerat" }, "Extraction Accelerator"),
    };

    /// <summary>Official gizmo name for an item blueprint (WID_RelicFinder → Sprite Locator); other items as they are.</summary>
    public static string GizmoName(string item)
    {
        var lower = item.ToLowerInvariant();
        foreach (var (fragments, name) in Gizmos)
        {
            if (Array.Exists(fragments, lower.Contains))
            {
                return name;
            }
        }
        return item;
    }
}
