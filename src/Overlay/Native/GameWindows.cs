using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Whether a window of the game is open and in sight on some screen, and which process it belongs to (a new process is
/// a new launch of Fortnite). In sight: not minimized, and not hidden behind the desktop or another big window on its
/// own screen (in windowed fullscreen Windows never reports the game as minimized: it only stops being in front).
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
        var gameWindow = IntPtr.Zero;
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
                gameWindow = window;
            }
            return !found;
        }, IntPtr.Zero);
        return found && !Covered(gameWindow, Process, own);
    }

    /// <summary>
    /// The game is out of sight: what has the focus on its screen is the desktop (Win + D) or a window over most of
    /// the game's window (another app brought up with Alt + Tab; the switcher itself does not count). Measured on the game's own window, so it
    /// holds in windowed fullscreen and in a plain window alike (a minimized game is already out: Windows reports it).
    /// A window on another screen, one beside the game or small (a notification, a chat in a corner), FCT itself and
    /// the taskbar or Start menu leave the game in sight.
    /// </summary>
    /// <summary>How much of the game's window the window in focus must cover for the game to count as hidden.</summary>
    private const int CoveredPercent = 80;

    private static bool Covered(IntPtr game, uint gameProcess, uint own)
    {
        var front = GetForegroundWindow();
        if (front == IntPtr.Zero || front == game || GetWindowThreadProcessId(front, out var pid) == 0 || pid == gameProcess || pid == own)
        {
            return false;
        }
        var screen = MonitorFromWindow(game, MONITOR_DEFAULTTONEAREST);
        if (MonitorFromWindow(front, MONITOR_DEFAULTTONEAREST) != screen || IsIconic(front))
        {
            return false;
        }
        var kind = new System.Text.StringBuilder(64);
        GetClassNameW(front, kind, kind.Capacity);
        switch (kind.ToString())
        {
            case "Progman" or "WorkerW":
                return true; // the desktop
            case "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "NotifyIconOverflowWindow" or "Windows.UI.Core.CoreWindow"
                or "XamlExplorerHostIslandWindow" or "MultitaskingViewFrame" or "TaskSwitcherWnd" or "ForegroundStaging":
                return false; // taskbar, tray, Start and notification panels, and the Alt + Tab / Win + Tab switchers
        }
        if (!GetWindowRect(front, out var box) || !GetWindowRect(game, out var area) || area.Width <= 0 || area.Height <= 0)
        {
            return false;
        }
        long width = Math.Max(0, Math.Min(box.Right, area.Right) - Math.Max(box.Left, area.Left));
        long height = Math.Max(0, Math.Min(box.Bottom, area.Bottom) - Math.Max(box.Top, area.Top));
        return width * height * 100 >= (long)area.Width * area.Height * CoveredPercent;
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
