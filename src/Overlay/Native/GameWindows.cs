using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Whether a window of the game is open and not minimized on some screen, and which process it belongs to (a new
/// process is a new launch of Fortnite).
/// </summary>
internal sealed class GameWindows
{
    private readonly Func<int, bool>? _isGame;
    private readonly Dictionary<uint, bool> _known = new(); // process id → is it the game

    /// <param name="isGame">Given a process id, whether it is the game; null means "always open" (previews).</param>
    public GameWindows(Func<int, bool>? isGame) => _isGame = isGame;

    public bool Watching => _isGame != null;

    /// <summary>Process of the game window found by the last <see cref="Open"/> (0 for none).</summary>
    public uint Process { get; private set; }

    /// <summary>The process seen when the overlay last came on screen for a launch.</summary>
    public uint LastLaunch { get; set; }

    public bool Open()
    {
        Process = 0;
        if (_isGame == null)
        {
            return true;
        }
        if (_known.Count > 512)
        {
            _known.Clear();
        }
        var own = (uint)Environment.ProcessId;
        var found = false;
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window) || IsIconic(window) || GetWindowThreadProcessId(window, out var pid) == 0 || pid == own)
            {
                return true;
            }
            if (!_known.TryGetValue(pid, out var isGame))
            {
                _known[pid] = isGame = _isGame((int)pid);
            }
            found = isGame && OnScreen(window);
            if (found)
            {
                Process = pid;
            }
            return !found;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>
    /// Really on screen: not a tiny or helper window, inside some monitor (Windows parks minimized windows far away)
    /// and not hidden by Windows (cloaked).
    /// </summary>
    private static bool OnScreen(IntPtr window) =>
        GetWindowRect(window, out var box) && box.Width >= 200 && box.Height >= 150
        && MonitorFromWindow(window, MONITOR_DEFAULTTONULL) != IntPtr.Zero
        && !(DwmGetWindowAttribute(window, DWMWA_CLOAKED, out var cloaked, sizeof(int)) == 0 && cloaked != 0);
}
