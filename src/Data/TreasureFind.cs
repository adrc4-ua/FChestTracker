using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

public sealed class TreasureFind
{
    /// <summary>Name to display (re-resolved from treasures.json so renaming updates old matches).</summary>
    public string DisplayName => Prizes.Describe(this).Name;

    /// <summary>Featured treasure (cosmetic, vehicle, shop access…: the server names it) rather than a treasure gizmo.</summary>
    public bool Featured => FromPrizeChest || (PrizeId != null && !TreasureNames.IsGizmoKey(PrizeId));

    public const string FinderSelf = "self", FinderTeammate = "teammate", FinderUnknown = "unknown";

    /// <summary>
    /// Who opened the chest that paid out: the tracked player ("self", also every treasure saved before analyzer
    /// version 15, which only kept the player's own), a teammate ("teammate", see <see cref="FinderName"/>) or
    /// "unknown" (received by the squad, but the chest is not in the replay: a teammate too far away).
    /// </summary>
    public string Finder { get; set; } = FinderSelf;
    /// <summary>Name of the teammate who found it (null for the player's own and for unknown).</summary>
    public string? FinderName { get; set; }

    /// <summary>Name shown in the "obtained by" column of the report.</summary>
    public string FinderLabel(MatchRecord match) => Finder switch
    {
        FinderTeammate => string.IsNullOrWhiteSpace(FinderName) ? L.T("Compañero", "Teammate") : FinderName,
        FinderUnknown => L.T("Desconocido", "Unknown"),
        _ => string.IsNullOrWhiteSpace(match.PlayerName) ? L.T("Tú", "You") : match.PlayerName,
    };

    public string Item { get; set; } = "";
    public string RawItem { get; set; } = "";
    /// <summary>Prize id sent by the server with a featured treasure, e.g. "DelilahPrize:dpid_item4" (gizmos: an internal "Unknown:…" key).</summary>
    public string? PrizeId { get; set; }
    /// <summary>True when detected from the treasure hunt chest itself (it carries the prize id).</summary>
    public bool FromPrizeChest { get; set; }
    /// <summary>True when detected only from the prize handler (exact time, item unknown unless sent at that moment).</summary>
    public bool FromPrizeHandler { get; set; }
    /// <summary>True when the time is exact (featured treasure chest or prize handler), false when estimated.</summary>
    public bool TimeExact { get; set; }
    /// <summary>True when the source chest was destroyed rather than opened.</summary>
    public bool SourceDestroyed { get; set; }
    public string SourceLabel => SourceDestroyed ? $"{L.Name(SourceKind)} ({L.T("destruido", "destroyed")})" : L.Name(SourceKind);
    /// <summary>When it was obtained (replay seconds).</summary>
    public float? ObtainedAtSeconds { get; set; }
    /// <summary>Kind of container / interaction it came from (Chest, Loot cannon...).</summary>
    public string SourceKind { get; set; } = "";
    public string SourceBlueprint { get; set; } = "";
    public bool SourceIsChest { get; set; } = true;
    /// <summary>True when the source chest is known for sure (same moment as the prize), false when it is only close in time.</summary>
    public bool SourceCertain { get; set; }
    /// <summary>For estimated sources: other (non-chest) interactions between the source and the first use.</summary>
    public List<string> AlsoPossible { get; set; } = new();
}
