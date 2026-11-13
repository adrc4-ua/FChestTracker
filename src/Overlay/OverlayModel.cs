namespace FCT.Overlay;

/// <summary>
/// Builds the <see cref="OverlayView"/> shared by the main window, the game overlay and the OBS page, from the profile's
/// matches and the match being played.
/// </summary>
public static class OverlayModel
{
    /// <summary>Caption of the second number between matches (also the width the overlay reserves for it).</summary>
    public static string LastMatchCaption => L.T("anterior", "previous");

    public const string TreasureIcon = "💎";

    /// <param name="data">The profile's counted matches (see <see cref="Profiles.View"/>).</param>
    /// <param name="live">Figures of the match in progress, when its replay could be read.</param>
    /// <param name="inProgress">A match is being recorded, readable or not.</param>
    /// <param name="initialChests">Chests before FCT: they only add to the total.</param>
    public static OverlayView Build(TrackerData data, MatchRecord? live, bool inProgress, int initialChests = 0)
    {
        var liveChests = live?.ChestsOpened ?? 0;
        var total = data.Matches.Sum(m => m.ChestsOpened) + liveChests + Math.Max(0, initialChests);
        var latest = data.Matches.OrderByDescending(m => m.MatchStartUtc ?? m.AnalyzedAtUtc).FirstOrDefault();

        var rows = new List<OverlayRow>();
        rows.AddRange(TodayRows(data, liveChests));
        if (latest != null && (live != null || inProgress))
        {
            // Between matches the second number already is the previous match; during one it gets its own row.
            rows.Add(new OverlayRow(L.T("Anterior", "Previous"), L.N(latest.ChestsOpened)));
        }
        var treasures = data.Matches.Sum(m => m.TreasureFinds.Count) + (live?.TreasureFinds.Count ?? 0);
        rows.Add(new OverlayRow(L.T("Tesoros", "Treasures"), $"{TreasureIcon} {L.N(treasures)}", Treasure: true));

        var shownTotal = L.N(total);
        if (live != null)
        {
            return new OverlayView(shownTotal, L.N(live.ChestsOpened), L.T("actual", "current"), true, L.T("● EN DIRECTO", "● LIVE"), rows);
        }
        if (inProgress)
        {
            return new OverlayView(shownTotal, "…", L.T("en curso", "in progress"), true, null, rows);
        }
        return new OverlayView(shownTotal, latest == null ? null : L.N(latest.ChestsOpened), LastMatchCaption, false, null, rows);
    }

    /// <summary>
    /// "Hoy" (today's chests, the match in progress included) and "Media" (per finished match today). Values are bare
    /// numbers so columns never collide; the window also shows "n partidas · media x" under today's figure.
    /// </summary>
    private static IEnumerable<OverlayRow> TodayRows(TrackerData data, int liveChests)
    {
        var today = DateTime.Now.Date;
        var finished = data.Matches.Where(m => (m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime().Date == today).ToList();
        var chests = finished.Sum(m => m.ChestsOpened);

        var average = "—";
        string? detail = null;
        if (finished.Count > 0)
        {
            average = Math.Round((double)chests / finished.Count, MidpointRounding.AwayFromZero).ToString("0", L.Culture);
            detail = $"{L.Count(finished.Count, "partida", "partidas", "match", "matches")} · {L.T("media", "avg.")} {average}";
        }
        yield return new OverlayRow(L.T("Hoy", "Today"), L.N(chests + liveChests), Detail: detail);
        yield return new OverlayRow(L.T("Media", "Avg."), average);
    }
}
