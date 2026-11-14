using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FCT.Overlay.Native;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay;

/// <summary>
/// The in-game overlay: a small always-on-top window on its own thread, drawn with GDI so it stays sharp and cheap. It
/// has no timer for the figures (it repaints when told to) and only shows while a window of the game is open. Its
/// padlock locks it in place. The mouse always goes through it, so the cursor is always the game's and the overlay can
/// never show one the game hides; while the pointer is free over it (a menu, Esc, the map) it reads the mouse itself to
/// work its padlock and ✕, to be dragged when unlocked and hidden with a right click (<see cref="MouseGate"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public static class NativeOverlay
{
    private const uint WM_APP_TOGGLE = 0x8001, WM_APP_RELAYOUT = 0x8002, WM_APP_LOCK = 0x8003, WM_APP_RECHECK = 0x8004;
    private const int GameTimer = 3, GameTimerMs = 2000, MouseTimer = 4;

    /// <summary>How often the pointer is looked at: often near the overlay (so it answers the click at once), seldom far.</summary>
    private const uint MouseNearMs = 40, MouseFarMs = 160;

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
    private static readonly MouseGate _mouse = new();
    private static bool _listening, _dragging;
    private static OverlayButton _hover, _pressed;
    private static POINT _grab; // where the pointer took the overlay to drag it, relative to it
    private static uint _mouseEvery;
    private static WndProc? _procedure; // kept alive while the window lives
    private static WinEventProc? _eventProcedure;
    private static readonly List<IntPtr> _eventHooks = new();

    public static bool IsRunning => _window != IntPtr.Zero;

    /// <summary>Locked: it cannot be moved (the mouse always goes through it).</summary>
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
            hCursor = LoadCursorW(IntPtr.Zero, IDC_ARROW), // never shown: the mouse always goes through the overlay
            hbrBackground = _ink.Card,
            lpszClassName = "FCTOverlay",
        };
        if (RegisterClassExW(ref windowClass) == 0)
        {
            throw new InvalidOperationException("RegisterClassEx failed");
        }
        // layered + transparent: the mouse goes through it to the game (an off-screen preview needs neither)
        var passThrough = _offscreen ? 0 : WS_EX_LAYERED | WS_EX_TRANSPARENT;
        _window = CreateWindowExW(WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | passThrough, windowClass.lpszClassName, L.T("Cofres", "Chests"), WS_POPUP,
            x ?? _ink.Px(24), y ?? _ink.Px(24), _ink.Px(OverlayLayout.MinWidth), _ink.Px(90), IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
        if (_window == IntPtr.Zero)
        {
            throw new InvalidOperationException("CreateWindowEx failed");
        }
        if (!_offscreen)
        {
            SetLayeredWindowAttributes(_window, 0, 255, LWA_ALPHA); // fully opaque: layered only for the pass-through
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
            Show(_window, true);
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
            case WM_APP_LOCK:
                SetLocked(window, !_locked, notify: true);
                return 0;
            case WM_APP_TOGGLE:
                SetVisible(window, !_visible);
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
            case WM_INPUT:
                if (ReadMouse(lParam) is var (buttons, moved))
                {
                    OnMouse(window, buttons, moved);
                }
                return DefWindowProcW(window, message, wParam, lParam);
            case WM_TIMER when (long)wParam == MouseTimer:
                LookAtPointer(window);
                return 0;
            case WM_TIMER when (long)wParam == GameTimer:
            case WM_APP_RECHECK:
                GameChanged(window);
                return 0;
            case WM_APP_RELAYOUT:
                Relayout(window);
                return 0;
            case WM_ERASEBKGND:
                return 1; // painted whole below, in one go: never a bare background first
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

    private static OverlayButton ButtonAt(int x, int y) => _layout.ButtonAt(x, y, _close != null);

    /// <summary>A place for the window on the screen it is on, whole (not even past one edge).</summary>
    private static RECT OnItsScreen(RECT box)
    {
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfoW(MonitorFromRect(ref box, MONITOR_DEFAULTTONEAREST), ref info))
        {
            return box;
        }
        var screen = info.rcMonitor;
        int width = box.Width, height = box.Height;
        var left = Math.Max(screen.Left, Math.Min(box.Left, screen.Right - width));
        var top = Math.Max(screen.Top, Math.Min(box.Top, screen.Bottom - height));
        return new RECT(left, top, left + width, top + height);
    }

    // ---- the mouse ----------------------------------------------------------------------------------------------------

    /// <summary>
    /// Every moment while on screen (often near it, seldom far): listens to the mouse only while the pointer is free
    /// over the overlay, and lights the button under it.
    /// </summary>
    private static void LookAtPointer(IntPtr window)
    {
        if (_dragging)
        {
            if ((GetAsyncKeyState(0x01) & 0x8000) == 0)
            {
                EndDrag(window); // the release was missed
            }
            return;
        }
        if (!GetWindowRect(window, out var place))
        {
            return;
        }
        var free = _mouse.FreeOver(place, out var at);
        var clicking = _pressed != OverlayButton.None && (GetAsyncKeyState(0x01) & 0x8000) != 0; // a click on a button, not yet released
        Listen(free || clicking);
        Hover(window, free ? ButtonAt(at.X - place.Left, at.Y - place.Top) : OverlayButton.None);
        WatchPointer(window, free || _mouse.Near ? MouseNearMs : MouseFarMs);
        if (OverlayLog.On && (_mouse.Near || _listening))
        {
            OverlayLog.Write("look", at, place, _mouse.LastCursor, MouseState(free));
        }
    }

    private static long _loggedMove;

    /// <summary>The overlay's mouse state, for the diagnostic log.</summary>
    private static string MouseState(bool free) =>
        $"free {free} listen {_listening} hover {_hover} pressed {_pressed} drag {_dragging} locked {_locked}";

    /// <summary>A raw mouse report while listening: a click on a button, a drag, a right click, or just a move.</summary>
    private static void OnMouse(IntPtr window, ushort buttons, bool moved)
    {
        if (!GetWindowRect(window, out var place))
        {
            return;
        }
        var free = _mouse.FreeOver(place, out var at); // the same steady reading as the timer's: never acts in play
        if (OverlayLog.On && (buttons != 0 || Environment.TickCount64 - _loggedMove >= 50))
        {
            _loggedMove = Environment.TickCount64;
            OverlayLog.Write(buttons != 0 ? $"btn {buttons:X}" : "move", at, place, _mouse.LastCursor, MouseState(free));
        }
        if (_dragging)
        {
            if (moved)
            {
                var left = at.X - _grab.X;
                var top = at.Y - _grab.Y;
                var box = OnItsScreen(new RECT(left, top, left + place.Width, top + place.Height));
                SetWindowPos(window, IntPtr.Zero, box.Left, box.Top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
            if ((buttons & RI_MOUSE_LEFT_BUTTON_UP) != 0)
            {
                EndDrag(window);
            }
            return;
        }

        var (x, y) = (at.X - place.Left, at.Y - place.Top);
        var under = free ? ButtonAt(x, y) : OverlayButton.None;
        if (moved)
        {
            Hover(window, under);
        }
        if ((buttons & RI_MOUSE_LEFT_BUTTON_DOWN) != 0 && free)
        {
            _pressed = under;
            if (under == OverlayButton.None && !_locked)
            {
                _dragging = true;
                _grab = new POINT { X = x, Y = y };
                WatchPointer(window, MouseNearMs);
            }
        }
        if ((buttons & RI_MOUSE_LEFT_BUTTON_UP) != 0)
        {
            // a click that began on a button while free ends on it, even if the game hid the cursor meanwhile
            var released = place.Contains(at.X, at.Y) ? ButtonAt(x, y) : OverlayButton.None;
            var clicked = _pressed != OverlayButton.None && _pressed == released ? _pressed : OverlayButton.None;
            _pressed = OverlayButton.None;
            if (clicked == OverlayButton.Close)
            {
                _close?.Invoke(); // closes the app, and the overlay with it
            }
            else if (clicked == OverlayButton.Lock)
            {
                SetLocked(window, !_locked, notify: true);
            }
        }
        if ((buttons & RI_MOUSE_RIGHT_BUTTON_UP) != 0 && free && under == OverlayButton.None && !_locked)
        {
            SetVisible(window, false); // right click on an unlocked overlay hides it
        }
        if (!free && !_dragging && _pressed == OverlayButton.None)
        {
            Listen(false); // left it, or the game hid the pointer
            Hover(window, OverlayButton.None);
        }
    }

    private static void EndDrag(IntPtr window)
    {
        _dragging = false;
        _pressed = OverlayButton.None;
        if (GetWindowRect(window, out var moved))
        {
            _moved?.Invoke(moved.Left, moved.Top);
        }
        LookAtPointer(window);
    }

    /// <summary>Reads the mouse itself (raw input, also while the game is in front) or stops.</summary>
    private static void Listen(bool on)
    {
        if (on == _listening)
        {
            return;
        }
        _listening = on;
        if (!on)
        {
            _pressed = OverlayButton.None;
        }
        var mouse = new[] { new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = on ? RIDEV_INPUTSINK : RIDEV_REMOVE, hwndTarget = on ? _window : IntPtr.Zero } };
        RegisterRawInputDevices(mouse, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    /// <summary>Lights a button (or none), repainting only the buttons' strip, and only when it changes.</summary>
    private static void Hover(IntPtr window, OverlayButton button)
    {
        if (button == _hover)
        {
            return;
        }
        _hover = button;
        var strip = new RECT(Math.Min(_layout.Lock.Left, _layout.Close.Left), Math.Min(_layout.Lock.Top, _layout.Close.Top),
            Math.Max(_layout.Lock.Right, _layout.Close.Right), Math.Max(_layout.Lock.Bottom, _layout.Close.Bottom));
        var handle = Marshal.AllocHGlobal(Marshal.SizeOf<RECT>());
        try
        {
            Marshal.StructureToPtr(strip, handle, false);
            InvalidateRect(window, handle, false);
        }
        finally
        {
            Marshal.FreeHGlobal(handle);
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
            ? L.T("Overlay bloqueado: ya no se mueve. Haz clic en el candado para desbloquearlo.",
                "Overlay locked: it no longer moves. Click the padlock to unlock it.")
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
        if (_offscreen)
        {
            return;
        }
        // the pointer is only watched while it is on screen; hidden, it gives the mouse back
        if (show)
        {
            WatchPointer(window, MouseFarMs);
        }
        else
        {
            KillTimer(window, (UIntPtr)MouseTimer);
            _mouseEvery = 0;
            _dragging = false;
            Listen(false);
            Hover(window, OverlayButton.None);
        }
    }

    /// <summary>Looks at the pointer every <paramref name="ms"/> (the timer is only reset when that changes).</summary>
    private static void WatchPointer(IntPtr window, uint ms)
    {
        if (ms != _mouseEvery)
        {
            _mouseEvery = ms;
            SetTimer(window, (UIntPtr)MouseTimer, ms, IntPtr.Zero);
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
        InvalidateRect(window, IntPtr.Zero, false);
        UpdateWindow(window); // one repaint per change
    }

    /// <summary>A raw mouse report: its button changes, and whether it moves (an absolute device, a pen… always counts).</summary>
    private static (ushort Buttons, bool Moved)? ReadMouse(IntPtr report)
    {
        var size = (uint)Marshal.SizeOf<RAWMOUSEINPUT>();
        if (GetRawInputData(report, RID_INPUT, out var input, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) == uint.MaxValue
            || input.header.dwType != RIM_TYPEMOUSE)
        {
            return null;
        }
        var moved = (input.usFlags & MOUSE_MOVE_ABSOLUTE) != 0 || input.lLastX != 0 || input.lLastY != 0;
        return ((ushort)(input.ulButtons & 0xFFFF), moved);
    }

    /// <summary>Paints the whole overlay in memory and copies it in one go: no flash of a half-drawn card on screen.</summary>
    private static void PaintInto(IntPtr window, IntPtr hdc)
    {
        GetClientRect(window, out var client);
        int width = Math.Max(1, client.Width), height = Math.Max(1, client.Height);
        var memory = CreateCompatibleDC(hdc);
        var bitmap = CreateCompatibleBitmap(hdc, width, height);
        if (memory == IntPtr.Zero || bitmap == IntPtr.Zero)
        {
            DeleteObject(bitmap);
            DeleteDC(memory);
            OverlayPainter.Paint(hdc, client, _ink, _layout, _view, _locked, _close != null, _hover);
            return;
        }
        var previous = SelectObject(memory, bitmap);
        try
        {
            OverlayPainter.Paint(memory, client, _ink, _layout, _view, _locked, _close != null, _hover);
            BitBlt(hdc, 0, 0, width, height, memory, 0, 0, SRCCOPY);
        }
        finally
        {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
        }
    }
}
