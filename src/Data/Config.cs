using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

public sealed class Config
{
    /// <summary>
    /// Language chosen in Settings ("es", "en"…); null = the Windows language. Saved under a new name: the old key
    /// held "es" for everyone and would force Spanish.
    /// </summary>
    [JsonPropertyName("UiLanguage")]
    public string? Language { get; set; }

    /// <summary>Selected profile (Epic id or name of the account the player plays on); null = most recent one.</summary>
    public string? Profile { get; set; }
    public string DemosFolder { get; set; } = "";

    /// <summary>Open the tool automatically when Fortnite starts (invisible standby process started with Windows).</summary>
    public bool StartWithFortnite { get; set; }

    /// <summary>
    /// "auto" (install new versions between matches, default), "prompt" (offer them) or "off" (never check GitHub).
    /// </summary>
    [JsonPropertyName("Updates")]
    public string UpdateMode { get; set; } = "auto";
    /// <summary>
    /// Open the report when the tool starts (off by default).
    /// </summary>
    [JsonPropertyName("OpenReportAtStartup")]
    public bool OpenReportOnStart { get; set; }
    public int? OverlayX { get; set; }
    public int? OverlayY { get; set; }
    /// <summary>Locked overlay = click-through, cannot be moved (Ctrl+Shift+L to toggle).</summary>
    public bool OverlayLocked { get; set; }

    /// <summary>Overlay shown on screen. Off by default: the player turns it on with the Overlay button or Ctrl+Shift+O.</summary>
    public bool OverlayVisible { get; set; }

    /// <summary>Count the chests of the match in progress (re-reads the replay being recorded).</summary>
    public bool LiveUpdate { get; set; } = true;

    /// <summary>Minimum delay between two readings of the replay in progress (seconds, 10 minimum).</summary>
    public int LiveIntervalSeconds { get; set; } = 30;

    /// <summary>Port of the local OBS page (http://localhost:port/), served while the tool is open.</summary>
    public int ObsPort { get; set; } = ObsServer.DefaultPort;

    /// <summary>
    /// Estimate of the chests opened before using the tool, per profile key ("" = no profile yet).
    /// Added to the total only (not to averages, records or per-day figures).
    /// </summary>
    public Dictionary<string, int> InitialChests { get; set; } = new();

    /// <summary>Add the ammo boxes, food boxes and loot cannons to the chest total (off by default).</summary>
    public bool CountAmmoBoxes { get; set; }
    public bool CountFoodBoxes { get; set; }
    public bool CountLootCannons { get; set; }

    /// <summary>
    /// Show the teammates section in the report (off by default): their chests come from the player's own replay,
    /// which only holds what happens nearby, so they are a minimum.
    /// </summary>
    public bool ShowTeammates { get; set; }

    /// <summary>
    /// Count only the matches played on or after this local date (null = all the replays, the default). The older
    /// matches stay saved in data.json: they are only hidden from the figures, and come back when this is cleared.
    /// </summary>
    public DateTime? CountFrom { get; set; }

    /// <summary>
    /// Add the chests the teammates were seen opening to the player's own total (off by default). Like the extra
    /// kinds, only the figures shown change: the saved history keeps the player's own chests.
    /// </summary>
    public bool CountTeammateChests { get; set; }

    /// <summary>
    /// Settings of the Resumen page (its own Configuración tab): they only change what the page shows, never the
    /// window or the overlay. Null in configs saved before it existed: it then starts from the app's settings.
    /// </summary>
    public ReportSettings? Report { get; set; }

    /// <summary>The Resumen settings, created from the app's ones the first time.</summary>
    [JsonIgnore]
    public ReportSettings ReportView => Report ??= new ReportSettings
    {
        ShowTeammates = ShowTeammates,
        CountTeammateChests = CountTeammateChests,
        CountAmmoBoxes = CountAmmoBoxes,
        CountFoodBoxes = CountFoodBoxes,
        CountLootCannons = CountLootCannons,
    };

    /// <summary>A copy of these settings with the Resumen ones in place of the app's (to build the page).</summary>
    public Config ForReport()
    {
        var r = ReportView;
        var copy = (Config)MemberwiseClone();
        copy.ShowTeammates = r.ShowTeammates;
        copy.CountTeammateChests = r.CountTeammateChests;
        copy.CountAmmoBoxes = r.CountAmmoBoxes;
        copy.CountFoodBoxes = r.CountFoodBoxes;
        copy.CountLootCannons = r.CountLootCannons;
        return copy;
    }

    /// <summary>Non-chest kinds the player chose to add to the chest total.</summary>
    public IReadOnlyCollection<string> ExtraKinds()
    {
        var kinds = new List<string>(3);
        if (CountAmmoBoxes) kinds.Add(ChestKinds.AmmoBox);
        if (CountFoodBoxes) kinds.Add(ChestKinds.FoodBox);
        if (CountLootCannons) kinds.Add(ChestKinds.Cannon);
        return kinds;
    }
}

/// <summary>Settings that only affect the Resumen page.</summary>
public sealed class ReportSettings
{
    /// <summary>Language of the page; null = the same as the app.</summary>
    public string? Language { get; set; }
    public bool ShowTeammates { get; set; }
    public bool CountTeammateChests { get; set; }
    public bool CountAmmoBoxes { get; set; }
    public bool CountFoodBoxes { get; set; }
    public bool CountLootCannons { get; set; }
}
