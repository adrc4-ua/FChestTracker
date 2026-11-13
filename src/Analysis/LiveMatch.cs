namespace FCT.Analysis;

/// <summary>
/// The match being played: which replay is being recorded and, when it can be read, its figures so far (never saved).
/// Fortnite only writes the replay's real encryption key at the end, so for most of a match FCT can only tell that one
/// is in progress.
/// </summary>
internal sealed class LiveMatch
{
    /// <summary>A "live" replay untouched for this long was left by a crash, not a match being played.</summary>
    private static readonly TimeSpan Abandoned = TimeSpan.FromMinutes(3);

    /// <summary>After the match, the estimate stays on screen until the final reading, unless it goes this stale.</summary>
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(5);

    private readonly Config _config;
    private long _readSize = -1;
    private DateTime _readAtUtc = DateTime.MinValue;

    public LiveMatch(Config config) => _config = config;

    public string? File { get; private set; }
    public MatchRecord? Figures { get; set; }

    public void Clear()
    {
        File = null;
        _readSize = -1;
        Figures = null;
    }

    /// <summary>
    /// Looks at the newest replay being recorded and re-reads it when it grew (Fortnite appends about every 25 s), at
    /// most every <see cref="Config.LiveIntervalSeconds"/> (10 s minimum). True when what is shown should change.
    /// </summary>
    public bool Refresh(IEnumerable<FileInfo> candidates)
    {
        var recording = candidates
            .Where(f => DateTime.UtcNow - f.LastWriteTimeUtc < Abandoned)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault(f => ReplayHeaderProbe.IsLive(f.FullName) == true);

        if (recording == null)
        {
            if (File != null && (!System.IO.File.Exists(File) || DateTime.UtcNow - System.IO.File.GetLastWriteTimeUtc(File) > Stale))
            {
                Clear();
                return true;
            }
            return false;
        }

        var startedNow = recording.FullName != File;
        if (startedNow)
        {
            Clear();
            File = recording.FullName;
            Log(L.F("[{0:HH:mm:ss}] Partida en curso… (sus cofres se contarán al final)",
                "[{0:HH:mm:ss}] Match in progress… (its chests will be counted at the end)", DateTime.Now));
        }

        var dueAgain = recording.Length != _readSize && DateTime.UtcNow - _readAtUtc >= TimeSpan.FromSeconds(Math.Max(10, _config.LiveIntervalSeconds));
        if (!_config.LiveUpdate || ReplayHeaderProbe.KeyMissing(recording.FullName) || !dueAgain)
        {
            return startedNow;
        }

        MatchRecord latest;
        try
        {
            latest = ReplayReading.Read(recording, TimeSpan.FromSeconds(60));
        }
        catch (Exception ex)
        {
            _readAtUtc = DateTime.UtcNow; // next interval
            if (Environment.GetEnvironmentVariable("FCT_DEBUG") is { Length: > 0 })
            {
                Console.Error.WriteLine($"DBG live parse failed: {ex}");
            }
            return startedNow;
        }
        _readSize = recording.Length;
        _readAtUtc = DateTime.UtcNow;

        var previous = Figures;
        Figures = latest;
        var changed = previous == null || previous.ChestsOpened != latest.ChestsOpened || previous.TreasureFinds.Count != latest.TreasureFinds.Count;
        if (changed)
        {
            var treasures = latest.TreasureFinds.Count > 0 ? ", " + L.Count(latest.TreasureFinds.Count, "tesoro", "tesoros", "treasure", "treasures") : "";
            Log($"[{DateTime.Now:HH:mm:ss}] {L.T("Partida en curso", "Match in progress")}: {L.Count(latest.ChestsOpened, "cofre", "cofres", "chest", "chests")}{treasures}");
        }
        return changed || startedNow;
    }

    private static void Log(string line)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(line);
        Console.ResetColor();
    }
}
