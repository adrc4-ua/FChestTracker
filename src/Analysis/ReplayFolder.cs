namespace FCT.Analysis;

/// <summary>
/// The replays folder: which .replay files there are, which ones the history has not handled yet, and whether a file
/// is finished and stable enough to be read.
/// </summary>
public sealed class ReplayFolder
{
    /// <summary>Unchanged for this long, a finished replay saved earlier is surely complete.</summary>
    private static readonly TimeSpan OldEnough = TimeSpan.FromMinutes(1);

    /// <summary>A replay Fortnite just finished (live flag off, real key written) only needs a few still seconds.</summary>
    private static readonly TimeSpan JustFinishedQuiet = TimeSpan.FromSeconds(4);

    /// <summary>Quiet time for any other replay; doubled care (2 min) when its header cannot even be read.</summary>
    private static readonly TimeSpan DefaultQuiet = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan UnreadableHeaderQuiet = TimeSpan.FromMinutes(2);

    private readonly Func<string> _path;
    private readonly IReadOnlyDictionary<string, string> _handled;
    private readonly Dictionary<string, long> _sizeAtLastLook = new();

    /// <param name="path">The folder (read each time: it can change in Settings).</param>
    /// <param name="handled">The history's processed-files table, keyed by <see cref="Key"/>.</param>
    public ReplayFolder(Func<string> path, IReadOnlyDictionary<string, string> handled)
    {
        _path = path;
        _handled = handled;
    }

    /// <summary>Identifies one version of a file: name, size and last write.</summary>
    public static string Key(FileInfo file) => $"{file.Name}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";

    /// <summary>Every replay, oldest first.</summary>
    public IEnumerable<FileInfo> All()
    {
        var folder = new DirectoryInfo(_path());
        return folder.Exists
            ? folder.EnumerateFiles("*.replay", SearchOption.TopDirectoryOnly).OrderBy(f => f.LastWriteTimeUtc)
            : Enumerable.Empty<FileInfo>();
    }

    public IEnumerable<FileInfo> NotHandled() => All().Where(f => !_handled.ContainsKey(Key(f)));

    /// <summary>Replays not being recorded right now.</summary>
    public static bool NotRecording(FileInfo file) => ReplayHeaderProbe.IsLive(file.FullName) != true;

    /// <summary>
    /// Whether the replay can be read now. A file being recorded never is. Otherwise its size must not have moved since
    /// the previous look and it must have been still for a while; how long depends on what its header says.
    /// <paramref name="justFinished"/> tells a match that has just ended (worth checking again within seconds).
    /// </summary>
    public bool IsReady(FileInfo file, bool skipQuietTime, out bool justFinished)
    {
        justFinished = false;
        var live = ReplayHeaderProbe.IsLive(file.FullName);
        if (live == true)
        {
            return false;
        }
        var stillFor = DateTime.UtcNow - file.LastWriteTimeUtc;
        if (live == false && (skipQuietTime || stillFor > OldEnough))
        {
            return true;
        }

        var sameSize = _sizeAtLastLook.TryGetValue(file.FullName, out var before) && before == file.Length;
        _sizeAtLastLook[file.FullName] = file.Length;
        if (live == null)
        {
            return sameSize && stillFor > UnreadableHeaderQuiet;
        }
        if (!ReplayHeaderProbe.KeyMissing(file.FullName))
        {
            justFinished = true;
            return sameSize && stillFor >= JustFinishedQuiet;
        }
        return sameSize && stillFor > DefaultQuiet;
    }
}
