using System.Text.RegularExpressions;
using FortniteReplayReader;
using FortniteReplayReader.Models;
using FortniteReplayReader.Models.NetFieldExports;
using FortniteReplayReader.Models.NetFieldExports.Items;
using FortniteReplayReader.Models.NetFieldExports.Weapons;
using Unreal.Core;
using Unreal.Core.Contracts;
using Unreal.Core.Models;
using Unreal.Core.Models.Enums;

namespace FCT.Analysis;

public static class TreasureNames
{
    /// <summary>Prize id -> display name, loaded from treasures.json (user editable).</summary>
    public static Dictionary<string, string> PrizeNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Key prefix of a treasure gizmo (its prize is not in the replay): an internal key, never shown.</summary>
    public const string UnknownPrefix = "Unknown:";
    public const string UnknownGizmo = "Treasure gizmo";

    public static bool IsGizmoKey(string id) => id.StartsWith(UnknownPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Official name of a treasure gizmo from its item blueprint (WID_RelicFinder → Sprite Locator).</summary>
    public static string GizmoName(string item)
    {
        var i = item.ToLowerInvariant();
        if (i.Contains("flameceleb")) return "Golden Firework";
        if (i.Contains("relicextractor")) return "Portable Extractor";
        if (i.Contains("relicfinder")) return "Sprite Locator";
        if (i.Contains("luckylocator")) return "Lucky Locator";
        if (i.Contains("smugglerextractionkey") || i.Contains("extractionaccelerat")) return "Extraction Accelerator";
        return item;
    }
}
