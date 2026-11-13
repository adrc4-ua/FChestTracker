using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

public sealed class TrackerData
{
    public int Version { get; set; } = 1;
    public int AnalyzerVersion { get; set; } = 1;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<MatchRecord> Matches { get; set; } = new();

    /// <summary>Key = file name|size|last write ticks. Value = session id, "skipped" or "error: ...".</summary>
    public Dictionary<string, string> ProcessedFiles { get; set; } = new();
}
