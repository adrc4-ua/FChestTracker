namespace FCT.Data;

/// <summary>Root of data.json: the history and the bookkeeping needed to read each replay only once.</summary>
public sealed class TrackerData
{
    public int Version { get; set; } = 1;
    public int AnalyzerVersion { get; set; } = 1;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<MatchRecord> Matches { get; set; } = new();

    /// <summary>
    /// Replays already handled. Key: "name|size|last-write ticks"; value: the session id it produced, "skipped", or
    /// "error: …" with the reason.
    /// </summary>
    public Dictionary<string, string> ProcessedFiles { get; set; } = new();

    /// <summary>End-of-match treasure screens read so far; kept so they can be matched again after a re-analysis.</summary>
    public List<TreasureScreen> TreasureScreens { get; set; } = new();
}
