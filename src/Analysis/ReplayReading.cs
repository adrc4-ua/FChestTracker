using System.Runtime;
using System.Runtime.ExceptionServices;

namespace FCT.Analysis;

/// <summary>
/// Turns one replay file into a <see cref="MatchRecord"/> without disturbing the game: the file is copied to memory
/// (shared with Fortnite, which may still be writing it), parsed on a lowest-priority thread with a time limit, and
/// the memory is handed back to Windows afterwards so FCT idles at a few MB.
/// </summary>
internal static class ReplayReading
{
    public static MatchRecord Read(FileInfo file, TimeSpan limit)
    {
        var content = Load(file.FullName);
        MatchRecord? match = null;
        ExceptionDispatchInfo? failure = null;
        var parser = new Thread(() =>
        {
            try
            {
                // Replays on a PC are its player's: the recording player is the one tracked (one profile per account).
                using var memory = new MemoryStream(content, writable: false);
                match = new ReplayAnalyzer().Analyze(memory, file.Name, "");
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        })
        {
            Name = "ReplayAnalysis",
            IsBackground = true,
            Priority = ThreadPriority.Lowest,
        };
        parser.Start();
        if (!parser.Join(limit))
        {
            throw new TimeoutException(L.T("análisis demasiado largo", "analysis took too long"));
        }
        content = Array.Empty<byte>();
        GiveMemoryBack();
        failure?.Throw(); // with the parser's own stack trace
        return match!;
    }

    private static byte[] Load(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var content = new byte[file.Length];
        file.ReadExactly(content);
        return content;
    }

    private static void GiveMemoryBack()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
    }
}
