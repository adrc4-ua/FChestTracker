namespace FCT.PostMatch;

/// <summary>
/// What the post-match "Treasure found" screen showed: one entry per card, left to right. The game groups equal
/// treasures into one card, so the cards are the kinds won, in the order each kind was first won. A gizmo name, or
/// <see cref="TreasureScreenReader.Unknown"/> for a card that is not a gizmo (a featured treasure) or could not be
/// read. Saved in data.json so that a re-analysis of the replay applies it again.
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
    /// <summary>
    /// The replay ends when the player leaves the match, so the screen shows up shortly before its end (measured on
    /// real matches: 11 to 63 s before). A wide margin after the end used to give a match whose own screen was not
    /// read the screen of the next match (12 minutes later): the next match's screen is always further away than this.
    /// </summary>
    private static readonly TimeSpan Before = TimeSpan.FromMinutes(3), After = TimeSpan.FromMinutes(2);

    /// <summary>The screen of this match, if one was read: the closest one shown just after the match ended.</summary>
    public static TreasureScreen? ScreenOf(MatchRecord m, IEnumerable<TreasureScreen> screens)
    {
        if (m.MatchStartUtc is not DateTime start)
        {
            return null;
        }
        var begin = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var end = begin.AddSeconds(m.DurationSeconds ?? 0);
        // Never a screen shown before this match started: a very short replay right after a match (joined and left)
        // took the previous match's screen, and its treasures were counted twice.
        return screens
            .Where(s => s.ShownUtc >= end - Before && s.ShownUtc <= end + After && s.ShownUtc > begin.AddSeconds(30))
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

    /// <summary>More cards than this beyond what the replay saw is not a believable reading (a misread screen).</summary>
    private const int MaxMissed = 8;

    /// <summary>
    /// The screen shows one card per kind of treasure, in the order each kind was first won (those won after a final
    /// elimination too); the replay sees every treasure until then, but not which gizmo each one was. So:
    /// <list type="number">
    /// <item>The first gizmo of the match is always the first gizmo card: it is always named.</item>
    /// <item>With as many cards as treasures, every kind appeared once: each gizmo gets its card's name, in order.</item>
    /// <item>Cards beyond the treasures the replay saw are treasures it missed (won after dying, or out of its
    /// reach): they are always added, so they count, with their name when it was read.</item>
    /// </list>
    /// Fewer cards than treasures (equal ones grouped) leaves the other gizmos unnamed: which kind each one was cannot
    /// be told. Nothing the replay saw is ever removed.
    /// </summary>
    private static void ApplyScreen(MatchRecord m, TreasureScreen screen)
    {
        const string unknown = TreasureScreenReader.Unknown;
        var cards = screen.Cards;
        var seen = m.TreasureFinds.Count; // the replay's (screen ones were removed by Reset)
        var replayGizmos = m.TreasureFinds.Where(f => !f.Featured).OrderBy(f => f.ObtainedAtSeconds ?? float.MaxValue).ToList();
        var replayFeatured = seen - replayGizmos.Count;
        var missed = cards.Count - seen;
        if (missed > MaxMissed)
        {
            return;
        }

        // 1 · Names of the gizmos the replay saw. With one card per treasure (as many cards as treasures, the unknown
        // ones being exactly its featured treasures), each gizmo gets its card in order.
        var head = cards.Take(seen).ToList();
        var headGizmos = head.Where(c => c != unknown).ToList();
        if (head.Count == seen && head.Count(c => c == unknown) == replayFeatured && headGizmos.Count == replayGizmos.Count)
        {
            for (var i = 0; i < replayGizmos.Count; i++)
            {
                replayGizmos[i].ScreenName = headGizmos[i];
            }
        }
        // Otherwise only the first gizmo is sure: the first gizmo card is the kind won first. (A card that could not
        // be read before it leaves even that open.)
        else if (replayGizmos.Count > 0 && cards.FirstOrDefault() is { } first && first != unknown && replayFeatured == 0)
        {
            replayGizmos[0].ScreenName = first;
        }

        // 2 · The ones the replay missed: the cards after those, always counted.
        var localStart = m.MatchStartUtc is DateTime st ? DateTime.SpecifyKind(st, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        for (var i = seen; i < cards.Count; i++)
        {
            var name = cards[i] != unknown ? cards[i] : null;
            m.TreasureFinds.Add(new TreasureFind
            {
                Item = name ?? TreasureNames.UnknownGizmo,
                RawItem = "TreasureScreen",
                PrizeId = $"{TreasureNames.UnknownPrefix}{localStart:yyyy-MM-dd_HH.mm}@screen{i + 1}",
                ScreenName = name,
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
