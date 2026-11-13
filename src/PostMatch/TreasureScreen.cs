namespace FCT.PostMatch;

/// <summary>
/// What the post-match "Treasure found" screen showed: one entry per card, left to right (the order the treasures
/// were won). A gizmo name, or <see cref="TreasureScreenReader.Unknown"/> for a card that is not a gizmo (a featured
/// treasure) or could not be read. Saved in data.json so that a re-analysis of the replay applies it again.
/// </summary>
public sealed class TreasureScreen
{
    /// <summary>When the game showed the screen (UTC, from the game log).</summary>
    public DateTime ShownUtc { get; set; }

    public List<string> Cards { get; set; } = new();
}

/// <summary>
/// Adds what the treasure screen showed to a match analyzed from its replay. The replay stays the reference: the
/// screen only names its treasure gizmos and adds the ones the replay could not see (after a final elimination).
/// </summary>
public static class TreasureScreenMerge
{
    /// <summary>The screen shows up a few seconds after the end of the match (measured: 4–20 s).</summary>
    private static readonly TimeSpan Before = TimeSpan.FromMinutes(2), After = TimeSpan.FromMinutes(15);

    /// <summary>The screen of this match, if one was read: the closest one shown just after the match ended.</summary>
    public static TreasureScreen? ScreenOf(MatchRecord m, IEnumerable<TreasureScreen> screens)
    {
        if (m.MatchStartUtc is not DateTime start)
        {
            return null;
        }
        var end = DateTime.SpecifyKind(start, DateTimeKind.Utc).AddSeconds(m.DurationSeconds ?? 0);
        return screens
            .Where(s => s.ShownUtc >= end - Before && s.ShownUtc <= end + After)
            .OrderBy(s => Math.Abs((s.ShownUtc - end).TotalSeconds))
            .FirstOrDefault();
    }

    /// <summary>Applies the screen of this match (if any) after removing what an earlier screen added. True if changed.</summary>
    public static bool Apply(MatchRecord m, IEnumerable<TreasureScreen> screens)
    {
        var before = Signature(m);
        Reset(m);
        if (ScreenOf(m, screens) is { } screen)
        {
            ApplyScreen(m, screen);
        }
        return Signature(m) != before;
    }

    private static void Reset(MatchRecord m)
    {
        m.TreasureFinds.RemoveAll(f => f.FromScreen);
        foreach (var f in m.TreasureFinds)
        {
            f.ScreenName = null;
        }
        m.TreasureScreenRead = false;
    }

    private static void ApplyScreen(MatchRecord m, TreasureScreen screen)
    {
        var gizmos = screen.Cards.Where(c => c != TreasureScreenReader.Unknown).ToList();
        var unknownCards = screen.Cards.Count - gizmos.Count;
        var replayGizmos = m.TreasureFinds.Where(f => !f.Featured).OrderBy(f => f.ObtainedAtSeconds ?? float.MaxValue).ToList();
        var replayFeatured = m.TreasureFinds.Count(f => f.Featured);

        // Every card that is not a gizmo must be a featured treasure the replay knows; otherwise one gizmo card could
        // not be read and the order of the others no longer matches the replay: nothing is used.
        // The screen must also show at least the gizmos the replay saw (all cards fully drawn).
        if (unknownCards > replayFeatured || gizmos.Count < replayGizmos.Count)
        {
            return;
        }

        // In the order they were won: the replay sees the first ones (until a final elimination), the screen all of them.
        for (var i = 0; i < replayGizmos.Count; i++)
        {
            replayGizmos[i].ScreenName = gizmos[i];
        }
        var localStart = m.MatchStartUtc is DateTime st ? DateTime.SpecifyKind(st, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        for (var i = replayGizmos.Count; i < gizmos.Count; i++)
        {
            m.TreasureFinds.Add(new TreasureFind
            {
                Item = gizmos[i],
                RawItem = "TreasureScreen",
                PrizeId = $"{TreasureNames.UnknownPrefix}{localStart:yyyy-MM-dd_HH.mm}@screen{i + 1}",
                ScreenName = gizmos[i],
                FromScreen = true,
                SourceKind = "unknown",
                SourceIsChest = true,
                Finder = TreasureFind.FinderUnknown,
            });
        }
        m.TreasureScreenRead = true;
    }

    private static string Signature(MatchRecord m) =>
        string.Join("|", m.TreasureFinds.Select(f => $"{f.FromScreen}:{f.ScreenName}")) + m.TreasureScreenRead;
}
