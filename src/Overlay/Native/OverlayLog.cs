using System.Runtime.Versioning;
using System.Text;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Diagnostic log of the overlay and the mouse, only when FCT_OVERLAY_LOG names a file: what the overlay sees of the
/// pointer and the game's cursor, and what it does with clicks. Written on the overlay's thread, a line per event.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class OverlayLog
{
    private static readonly StreamWriter? Writer = Open();
    private static readonly long Start = Environment.TickCount64;
    private static long _flushed;

    public static bool On => Writer != null;

    private static StreamWriter? Open()
    {
        try
        {
            return Environment.GetEnvironmentVariable("FCT_OVERLAY_LOG") is { Length: > 0 } path
                ? new StreamWriter(path, append: true, new UTF8Encoding(false))
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>One line: time, what happened, the pointer, the overlay, the cursor and the overlay's mouse state.</summary>
    public static void Write(string what, POINT at, RECT overlay, CURSORINFO cursor, string state)
    {
        if (Writer == null)
        {
            return;
        }
        var now = Environment.TickCount64;
        Writer.WriteLine($"{now - Start,7} {what,-9} ptr {at.X},{at.Y} ovl {overlay.Left},{overlay.Top},{overlay.Right},{overlay.Bottom} " +
                         $"cur flags {cursor.flags} h {cursor.hCursor:X} fg {Foreground()} | {state}");
        if (now - _flushed > 500)
        {
            _flushed = now;
            Writer.Flush();
        }
    }

    private static string Foreground()
    {
        var name = new StringBuilder(64);
        GetClassNameW(GetForegroundWindow(), name, name.Capacity);
        return name.ToString();
    }
}
