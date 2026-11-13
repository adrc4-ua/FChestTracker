namespace FCT.Analysis;

/// <summary>
/// Estimate of the chests opened in the matches FCT has no replay of ("Cofres antes de FCT"):
/// <c>average × (total matches − matches FCT already counts)</c>.
/// <list type="bullet">
/// <item>The average needs at least the number of matches the player chose (5 by default, 10 recommended: the more,
/// the closer). The matches FCT already counts from before the total was taken are all used; if there are fewer, it
/// waits for the next matches until there are enough. Those later matches are not in the total (taken before them),
/// so they are not subtracted.</item>
/// <item>The average is rounded down when there was no replay at all, or <see cref="ReplayCap"/> or more (Fortnite
/// keeps no more: the history is cut); otherwise it is used as is.</item>
/// <item>Or the player types the average: it is used as is, with no waiting and no rounding.</item>
/// </list>
/// </summary>
public static class HistoryEstimator
{
    /// <summary>Replays Fortnite keeps at most on PC (not documented by Epic: checked by hand).</summary>
    public const int ReplayCap = 100;

    /// <summary>Matches the average needs by default, and the recommended number.</summary>
    public const int DefaultSample = 5, RecommendedSample = 10;

    /// <summary>Choices offered for the number of matches.</summary>
    public static readonly int[] SampleChoices = { 5, 10, 15, 20 };

    /// <param name="Chests">Estimated chests, or null while waiting for matches.</param>
    /// <param name="Counted">Matches the average comes from.</param>
    /// <param name="Average">Chests per match used.</param>
    /// <param name="RoundedDown">The average was rounded down (no replays, or at the replay limit).</param>
    /// <param name="Waiting">Matches still to play before it can be calculated.</param>
    public sealed record Result(int? Chests, int Counted, double? Average, bool RoundedDown, int Waiting);

    /// <param name="matches">Matches of the profile, as counted (own chests, with the chosen extra kinds).</param>
    /// <param name="totalMatches">The player's total matches according to the stats.</param>
    /// <param name="snapshotUtc">When that total was taken.</param>
    /// <param name="sample">Matches the average needs at least.</param>
    /// <param name="manualAverage">Average typed by the player (chests per match), or null to calculate it.</param>
    public static Result Compute(IReadOnlyList<MatchRecord> matches, int totalMatches, DateTime snapshotUtc, int sample = DefaultSample, double? manualAverage = null)
    {
        static DateTime Start(MatchRecord m) => m.MatchStartUtc ?? m.AnalyzedAtUtc;
        sample = Math.Max(1, sample);
        var before = matches.Where(m => Start(m) <= snapshotUtc).ToList();
        if (manualAverage is double typed && typed >= 0)
        {
            var rest = Math.Max(0, totalMatches - before.Count);
            return new Result((int)Math.Round(typed * rest, MidpointRounding.AwayFromZero), before.Count, typed, false, 0);
        }
        var need = Math.Max(0, sample - before.Count);
        var after = matches.Where(m => Start(m) > snapshotUtc).OrderBy(Start).Take(need).ToList();
        if (after.Count < need)
        {
            return new Result(null, before.Count + after.Count, null, before.Count == 0, need - after.Count);
        }
        var used = before.Concat(after).ToList();
        var average = (double)used.Sum(m => m.OwnChests) / used.Count;
        var rounded = before.Count == 0 || before.Count >= ReplayCap;
        if (rounded)
        {
            average = Math.Floor(average);
        }
        var missing = Math.Max(0, totalMatches - before.Count);
        return new Result((int)Math.Round(average * missing, MidpointRounding.AwayFromZero), used.Count, average, rounded, 0);
    }
}
