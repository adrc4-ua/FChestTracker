using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FCT.Overlay.Native;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay;

/// <summary>
/// The in-game overlay: a small always-on-top window on its own thread, drawn with GDI so it stays sharp and cheap. It
/// has no timer for the figures (it repaints when told to) and only shows while a window of the game is open. Its
/// padlock locks it (locked = the mouse goes through it, except its buttons).
/// </summary>
[SupportedOSPlatform("windows")]
public static class NativeOverlay
{
    private const uint WM_APP_TOGGLE = 0x8001, WM_APP_RELAYOUT = 0x8002, WM_APP_LOCK = 0x8003, WM_APP_RECHECK = 0x8004;
    private const int GameTimer = 3, GameTimerMs = 2000;

    private static Thread? _thread;
    private static IntPtr _window;
    private static volatile OverlayView _view = new("0", null, OverlayModel.LastMatchCaption, false, null, Array.Empty<OverlayRow>());

    // callbacks into the app
    private static Action<int, int>? _moved;
    private static Action<bool>? _lockChanged, _visibleChanged;
    private static Action? _close;

    // state, used on the overlay's thread (the volatile ones are read from others too)
    private static volatile bool _visible, _locked, _gameOpen = true;
    private static bool _offscreen, _scaleForced;
    private static OverlayInk _ink = null!;
    private static OverlayLayout _layout = new();
    private static int _keptWidth;
    private static GameWindows _game = new(null);
    private static WndProc? _procedure; // kept alive while the window lives
    private static WinEventProc? _eventProcedure;
    private static readonly List<IntPtr> _eventHooks = new();

    public static bool IsRunning => _window != IntPtr.Zero;

    /// <summary>Locked: the mouse goes through the overlay and it cannot be moved.</summary>
    public static bool IsLocked => _locked;

    /// <summary>Turned on (it is on screen only while the game is open).</summary>
    public static bool IsVisible => _window != IntPtr.Zero && _visible;

    /// <param name="x">Saved position; -10000 or less keeps it off screen for good (--overlay-preview).</param>
    /// <param name="isGame">Given a process id, whether it is the game: the overlay only shows while a window of the
    /// game is open and not minimized, on any screen. Null: always (previews).</param>
    public static void Start(int? x, int? y, bool locked, Action<int, int> onMoved, Action<bool> onLockChanged,
        bool visible = true, Action<bool>? onVisibleChanged = null, Action? onClose = null, Func<int, bool>? isGame = null)
    {
        if (_thread != null)
        {
            return;
        }
        _visible = visible;
        _locked = locked;
        _offscreen = x <= -10000;
        _game = new GameWindows(isGame);
        (_moved, _lockChanged, _visibleChanged, _close) = (onMoved, onLockChanged, onVisibleChanged, onClose);

        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            try
            {
                Run(x, y, ready);
            }
            catch (Exception ex)
            {
                Console.WriteLine(L.F("Overlay no disponible: {0}", "Overlay unavailable: {0}", ex.Message));
                ready.Set();
            }
        })
        {
            IsBackground = true,
            Name = "Overlay",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
    }

    /// <summary>Shows other figures; the window resizes to fit them.</summary>
    public static void Update(OverlayView view)
    {
        _view = view;
        Post(WM_APP_RELAYOUT);
    }

    public static void ToggleLock() => Post(WM_APP_LOCK);

    public static void Toggle() => Post(WM_APP_TOGGLE);

    /// <summary>Looks again whether the game is open (Fortnite closing without another window coming forward says nothing).</summary>
    public static void Recheck()
    {
        if (_game.Watching)
        {
            Post(WM_APP_RECHECK);
        }
    }

    /// <summary>Takes the overlay off screen and ends its thread.</summary>
    public static void Stop()
    {
        var thread = _thread;
        if (thread == null)
        {
            return;
        }
        Post(WM_CLOSE);
        thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _window = IntPtr.Zero;
        _visible = false;
    }

    /// <summary>Saves the overlay as drawn to a PNG (works off screen too). For --overlay-preview.</summary>
    public static bool Snapshot(string path)
    {
        if (_window == IntPtr.Zero || !GetWindowRect(_window, out var box))
        {
            return false;
        }
        int width = box.Width, height = box.Height;
        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, width, height);
        var previous = SelectObject(memory, bitmap);
        var printed = PrintWindow(_window, memory, 1); // PW_CLIENTONLY: comes back as WM_PRINTCLIENT
        SelectObject(memory, previous);
        var header = new BITMAPINFOHEADER { biSize = 40, biWidth = width, biHeight = -height, biPlanes = 1, biBitCount = 32 };
        var pixels = new byte[width * height * 4];
        GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref header, 0);
        DeleteObject(bitmap);
        DeleteDC(memory);
        ReleaseDC(IntPtr.Zero, screen);
        Png.Write(path, width, height, pixels);
        return printed;
    }

    private static void Post(uint message)
    {
        if (_window != IntPtr.Zero)
        {
            PostMessageW(_window, message, IntPtr.Zero, IntPtr.Zero);
        }
    }

    // ---- the overlay's thread -----------------------------------------------------------------------------------------

    private static void Run(int? x, int? y, ManualResetEventSlim ready)
    {
        try { SetProcessDPIAware(); } catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException) { /* very old Windows */ }

        _ink = new OverlayInk();
        _ink.SetScale(InitialScale());
        var instance = GetModuleHandleW(null);
        _procedure = Procedure;
        var windowClass = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_procedure),
            hInstance = instance,
            hCursor = LoadCursorW(IntPtr.Zero, IDC_SIZEALL),
            hbrBackground = _ink.Card,
            lpszClassName = "FCTOverlay",
        };
        if (RegisterClassExW(ref windowClass) == 0)
        {
            throw new InvalidOperationException("RegisterClassEx failed");
        }
        _window = CreateWindowExW(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, windowClass.lpszClassName, L.T("Cofres", "Chests"), WS_POPUP,
            x ?? _ink.Px(24), y ?? _ink.Px(24), _ink.Px(OverlayLayout.MinWidth), _ink.Px(90), IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("CreateWindowEx failed");
        }

        // the scale of the monitor it is on, not of the main one (with two screens the text took the other's size)
        if (!_offscreen && !_scaleForced && GetDpiForWindow(_window) is var dpi and > 0 && Math.Abs(dpi / 96f - _ink.Scale) > 0.01f)
        {
            _ink.SetScale(dpi / 96f);
        }
        SetLocked(_window, _locked, notify: false);
        Relayout(_window);
        if (_game.Watching && !_offscreen)
        {
            FollowTheGame();
        }
        if (_visible && _gameOpen)
        {
            ShowWindow(_window, SW_SHOWNOACTIVATE);
        }
        ready.Set();

        // no timer for the figures: the thread sleeps here until something changes
        while (GetMessageW(out var message, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref message);
            DispatchMessageW(ref message);
        }
    }

    /// <summary>The main screen's scale, or FCT_OVERLAY_SCALE when set (README captures with --overlay-preview).</summary>
    private static float InitialScale()
    {
        if (float.TryParse(Environment.GetEnvironmentVariable("FCT_OVERLAY_SCALE"), NumberStyles.Float, CultureInfo.InvariantCulture, out var forced)
            && forced is > 0.5f and < 5f)
        {
            _scaleForced = true;
            return forced;
        }
        var screen = GetDC(IntPtr.Zero);
        var scale = GetDeviceCaps(screen, LOGPIXELSY) / 96f;
        ReleaseDC(IntPtr.Zero, screen);
        return scale;
    }

    /// <summary>
    /// Windows says when the window in front changes or a window is minimized or restored, and then the game is looked
    /// for. Also every 2 s on this thread, as Fortnite closing or crashing without a new window in front says nothing.
    /// </summary>
    private static void FollowTheGame()
    {
        _eventProcedure = (_, _, _, _, _, _, _) => GameChanged(_window);
        foreach (var (first, last) in new[] { (EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND), (EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND) })
        {
            var hook = SetWinEventHook(first, last, IntPtr.Zero, _eventProcedure, 0, 0, WINEVENT_OUTOFCONTEXT);
            if (hook != IntPtr.Zero)
            {
                _eventHooks.Add(hook);
            }
        }
        SetTimer(_window, (UIntPtr)GameTimer, GameTimerMs, IntPtr.Zero);
        _gameOpen = _game.Open();
        _game.LastLaunch = _game.Process; // a Fortnite already open at start is not a new launch
    }

    private static IntPtr Procedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        switch (message)
        {
            case WM_NCHITTEST:
            {
                // the buttons are clickable; anywhere else drags the window, or lets the mouse through when locked
                GetWindowRect(window, out var box);
                var (x, y) = (LowWord(lParam) - box.Left, HighWord(lParam) - box.Top);
                return OnButton(x, y) ? HTCLIENT : _locked ? HTTRANSPARENT : HTCAPTION;
            }
            case WM_SETCURSOR when ((long)lParam & 0xFFFF) == HTCLIENT:
                SetCursor(LoadCursorW(IntPtr.Zero, IDC_HAND));
                return 1;
            case WM_LBUTTONUP:
                if (_close != null && _layout.Close.Contains(LowWord(lParam), HighWord(lParam)))
                {
                    _close(); // closes the app, and the overlay with it
                }
                else
                {
                    SetLocked(window, !_locked, notify: true);
                }
                return 0;
            case WM_NCRBUTTONUP:
                SetVisible(window, false);
                return 0;
            case WM_APP_LOCK:
                SetLocked(window, !_locked, notify: true);
                return 0;
            case WM_APP_TOGGLE:
                SetVisible(window, !_visible);
                return 0;
            case WM_MOVING when !_offscreen && lParam != IntPtr.Zero:
                KeepOnItsScreen(lParam);
                return 1;
            case WM_EXITSIZEMOVE:
                if (GetWindowRect(window, out var moved))
                {
                    _moved?.Invoke(moved.Left, moved.Top);
                }
                return 0;
            case WM_DPICHANGED:
                // moved to a monitor with another scale: new fonts and size (Relayout places it)
                if (!_scaleForced)
                {
                    _ink.SetScale((int)((long)wParam & 0xFFFF) / 96f);
                    _keptWidth = 0;
                    Relayout(window);
                }
                return 0;
            case WM_TIMER when (long)wParam == GameTimer:
            case WM_APP_RECHECK:
                GameChanged(window);
                return 0;
            case WM_APP_RELAYOUT:
                Relayout(window);
                return 0;
            case WM_PAINT:
                var hdc = BeginPaint(window, out var paint);
                try
                {
                    PaintInto(window, hdc);
                }
                finally
                {
                    EndPaint(window, ref paint);
                }
                return 0;
            case WM_PRINTCLIENT: // Snapshot: paint into the given DC
                PaintInto(window, wParam);
                return 0;
            case WM_DESTROY:
                _eventHooks.ForEach(hook => UnhookWinEvent(hook));
                _eventHooks.Clear();
                PostQuitMessage(0);
                return 0;
        }
        return DefWindowProcW(window, message, wParam, lParam);
    }

    private static bool OnButton(int x, int y) => _layout.Lock.Contains(x, y) || (_close != null && _layout.Close.Contains(x, y));

    /// <summary>While dragged, the window cannot leave its screen, not even past one edge.</summary>
    private static void KeepOnItsScreen(IntPtr proposed)
    {
        var box = Marshal.PtrToStructure<RECT>(proposed);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (GetMonitorInfoW(MonitorFromRect(ref box, MONITOR_DEFAULTTONEAREST), ref info))
        {
            var screen = info.rcMonitor;
            int width = box.Width, height = box.Height;
            var left = Math.Max(screen.Left, Math.Min(box.Left, screen.Right - width));
            var top = Math.Max(screen.Top, Math.Min(box.Top, screen.Bottom - height));
            Marshal.StructureToPtr(new RECT(left, top, left + width, top + height), proposed, false);
        }
    }

    /// <summary>Locked: click-through except the buttons. Unlocked: drag to move.</summary>
    private static void SetLocked(IntPtr window, bool locked, bool notify)
    {
        _locked = locked;
        SetWindowPos(window, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        InvalidateRect(window, IntPtr.Zero, true);
        if (!notify)
        {
            return;
        }
        _lockChanged?.Invoke(locked);
        Console.WriteLine(locked
            ? L.T("Overlay bloqueado: el ratón lo atraviesa. Haz clic en el candado para desbloquearlo.",
                "Overlay locked: the mouse goes through it. Click the padlock to unlock it.")
            : L.T("Overlay desbloqueado: arrastra con el botón izquierdo para moverlo; bloquéalo de nuevo con el candado.",
                "Overlay unlocked: drag it with the left mouse button; lock it again with the padlock."));
    }

    /// <summary>
    /// The game opened or closed. A new Fortnite process turns the overlay on; restoring the same one does not (turned
    /// off by hand, it stays off until Fortnite is launched again).
    /// </summary>
    private static void GameChanged(IntPtr window)
    {
        var open = _game.Open();
        if (open == _gameOpen)
        {
            return;
        }
        _gameOpen = open;
        if (open && _game.Process != _game.LastLaunch)
        {
            _game.LastLaunch = _game.Process;
            if (!_visible)
            {
                _visible = true;
                _visibleChanged?.Invoke(true);
            }
        }
        if (_visible)
        {
            Show(window, open);
        }
    }

    private static void SetVisible(IntPtr window, bool visible)
    {
        _visible = visible;
        _visibleChanged?.Invoke(visible);
        if (visible && _game.Watching && !_offscreen)
        {
            _gameOpen = _game.Open();
        }
        if (visible && !_gameOpen)
        {
            return; // on: it shows as soon as the game is open
        }
        Show(window, visible);
        if (!visible)
        {
            Console.WriteLine(L.T("Overlay oculto: usa el botón Overlay de la ventana para volver a mostrarlo.", "Overlay hidden: use the Overlay button in the window to show it again."));
        }
    }

    private static void Show(IntPtr window, bool show)
    {
        if (show)
        {
            ShowWindow(window, SW_SHOWNOACTIVATE);
            SetWindowPos(window, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            ShowWindow(window, SW_HIDE);
        }
    }

    /// <summary>
    /// Measures the current view and resizes the window and its round region. It stays whole on its monitor (the whole
    /// monitor: the game covers the taskbar): on the right half of a screen it grows to the left.
    /// </summary>
    private static void Relayout(IntPtr window)
    {
        var hdc = GetDC(window);
        try
        {
            var layout = OverlayLayout.Measure(hdc, _ink, _view, _close != null, _keptWidth);
            _layout = layout;
            _keptWidth = layout.Width;
            int width = layout.Width, height = layout.Height;

            GetWindowRect(window, out var now);
            int left = now.Left, top = now.Top;
            var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!_offscreen && GetMonitorInfoW(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), ref info))
            {
                var screen = info.rcMonitor;
                if (now.Width > 0 && (now.Left + now.Right) / 2 > (screen.Left + screen.Right) / 2)
                {
                    left = now.Right - width; // anchored by its right edge
                }
                left = Math.Max(screen.Left, Math.Min(left, screen.Right - width));
                top = Math.Max(screen.Top, Math.Min(top, screen.Bottom - height));
            }
            SetWindowPos(window, HWND_TOPMOST, left, top, width, height, SWP_NOACTIVATE);
            SetWindowRgn(window, CreateRoundRectRgn(0, 0, width + 1, height + 1, _ink.Px(16), _ink.Px(16)), true); // the window owns the region
            if (left != now.Left || top != now.Top)
            {
                _moved?.Invoke(left, top);
            }
        }
        finally
        {
            ReleaseDC(window, hdc);
        }
        InvalidateRect(window, IntPtr.Zero, true);
        UpdateWindow(window); // one repaint per change
    }

    private static void PaintInto(IntPtr window, IntPtr hdc)
    {
        GetClientRect(window, out var client);
        OverlayPainter.Paint(hdc, client, _ink, _layout, _view, _locked, _close != null);
    }
}
