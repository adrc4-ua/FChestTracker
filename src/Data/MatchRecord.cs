using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

public sealed class MatchRecord
{
    public string SessionId { get; set; } = "";
    public string ReplayFile { get; set; } = "";
    public DateTime AnalyzedAtUtc { get; set; }
    public DateTime? MatchStartUtc { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Playlist { get; set; }
    public string? GameVersion { get; set; }
    public string? PlayerName { get; set; }
    public string? EpicId { get; set; }
    public bool TrackedByUsername { get; set; }
    /// <summary>Set when none of the configured names played this match (name of the replay owner).</summary>
    public string? NotTrackedOwner { get; set; }
    public bool IsReplayOwner { get; set; }
    public int? Placement { get; set; }
    public int? TotalPlayers { get; set; }
    public int? Eliminations { get; set; }
    public int ChestsOpened { get; set; }
    public int AmmoBoxesOpened { get; set; }
    public int OtherContainersOpened { get; set; }
    /// <summary>Non-chest interactions by kind (loot cannons, ammo boxes, food boxes...).</summary>
    public Dictionary<string, int> OtherInteractions { get; set; } = new();
    public int AnalyzerVersion { get; set; } = 1;
    public bool IsBattleRoyale { get; set; } = true;
    public List<ChestRecord> Chests { get; set; } = new();
    public List<TreasureFind> TreasureFinds { get; set; } = new();
    /// <summary>Teammates of the match (empty in solo). Null for matches analyzed before analyzer version 13.</summary>
    public List<TeammateRecord>? Teammates { get; set; }
    /// <summary>
    /// Moment of the final elimination of the player (no reboot afterwards), when the replay was recorded by them.
    /// After it, the treasures the squad gets leave no trace in the replay (the player's ability system stops
    /// replicating), so a treasure count lower than the post-match screen is expected. Null when they survived or
    /// got rebooted (version 16+).
    /// </summary>
    public float? EliminatedAtSeconds { get; set; }
    /// <summary>The post-match treasure screen of this match was read: its gizmos are named and none is missing.</summary>
    public bool TreasureScreenRead { get; set; }

    /// <summary>How many non-chest interactions of a kind (ammo box, food box, loot cannon) this match has.</summary>
    public int Interactions(string kind) => OtherInteractions.Count > 0
        ? OtherInteractions.GetValueOrDefault(kind)
        : kind == ChestKinds.AmmoBox ? AmmoBoxesOpened : 0; // old matches only kept the ammo boxes

    /// <summary>Chests a teammate was seen opening, plus the non-chest kinds the player chose to count.</summary>
    public static int Seen(TeammateRecord t, IReadOnlyCollection<string> kinds) =>
        t.ChestsSeen + kinds.Sum(k => t.Kinds.GetValueOrDefault(k));

    /// <summary>Chests the teammates of this match were seen opening (a minimum: only what happened nearby).</summary>
    public int TeammatesSeen(IReadOnlyCollection<string> kinds) => Teammates?.Sum(t => Seen(t, kinds)) ?? 0;

    /// <summary>Teammates' chests added to <see cref="ChestsOpened"/> by the setting (only on adjusted copies).</summary>
    [JsonIgnore]
    public int TeammatesAdded { get; private set; }

    /// <summary>The player's own chests (with the chosen extra kinds), without the teammates' ones.</summary>
    [JsonIgnore]
    public int OwnChests => ChestsOpened - TeammatesAdded;

    /// <summary>
    /// Copy whose chest total also includes the given non-chest kinds (settings "count ammo boxes"...) and, when
    /// asked, the chests the teammates were seen opening. The saved history is never changed. Returns the same match
    /// when nothing is added.
    /// </summary>
    public MatchRecord WithExtras(IReadOnlyCollection<string> kinds, bool addTeammates = false)
    {
        var extra = kinds.Sum(Interactions);
        var mates = addTeammates ? TeammatesSeen(kinds) : 0;
        if (extra == 0 && mates == 0)
        {
            return this;
        }
        var copy = (MatchRecord)MemberwiseClone();
        copy.ChestsOpened = ChestsOpened + extra + mates;
        copy.TeammatesAdded = mates;
        return copy;
    }
}
