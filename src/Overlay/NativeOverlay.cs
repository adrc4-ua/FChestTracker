using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FCT.Overlay;

[SupportedOSPlatform("windows")]
public static class NativeOverlay
{
    private static Thread? _thread;
    private static IntPtr _hwnd;
    private static volatile OverlayView _view = new("0", null, OverlayModel.LastMatchCaption, false, null, Array.Empty<OverlayRow>());

    // Layout (computed by Relayout, used by Paint; both on the overlay thread).
    private static int _headerH, _bigH, _captionH, _statH, _col1W, _chipW, _chipH, _mainH, _badgeW;
    private static int[] _statW = Array.Empty<int>();
    private static IntPtr _fontHeader, _fontCaption, _fontLabel;
    private static IntPtr _brushBorder, _brushLine, _brushChip, _brushGold, _brushGreen, _brushPill;
    private static IntPtr _fontStat;
    private static string Header => L.T("COFRES", "CHESTS");
    private const string CaptionTotal = "total";
    private static Action<int, int>? _onMoved;
    private static Action<bool>? _onLockChanged;
    private static Action<bool>? _onVisibleChanged;
    private static volatile bool _visible;
    private static volatile bool _locked;
    private static bool _offscreen;
    private static int _stableW;
    private static RECT _lockRect; // lock button, client coordinates
    private static RECT _closeRect; // close button (closes the whole app), right of the lock
    private static Action? _onClose;
    private static IntPtr _fontIcon;
    private static WndProcDelegate? _wndProc; // keep the delegate alive
    private static IntPtr _fontBig, _fontSmall, _brushBg;
    private static float _scale = 1f;
    private static bool _forcedScale;

    /// <summary>
    /// Letras del overlay a la escala actual. ClearType «natural» (calidad 6) y ningún texto por debajo de 9 px: los
    /// títulos pequeños (TOTAL, HOY, MEDIA…) a 8 px se veían borrosos.
    /// </summary>
    private static void CreateFonts()
    {
        foreach (var old in new[] { _fontBig, _fontSmall, _fontHeader, _fontCaption, _fontLabel, _fontStat, _fontIcon })
        {
            if (old != IntPtr.Zero)
            {
                DeleteObject(old);
            }
        }
        int S(int v) => (int)Math.Round(v * _scale);
        IntPtr Font(int px, int weight, string face = "Segoe UI") => CreateFontW(-S(px), 0, 0, 0, weight, 0, 0, 0, 1, 0, 0, ClearTypeNatural, 0, face);
        _fontBig = Font(26, 800);
        _fontSmall = Font(12, 500);
        _fontHeader = Font(11, 700);
        _fontCaption = Font(9, 600);
        _fontLabel = Font(9, 700);
        _fontStat = Font(13, 700);
        _fontIcon = Font(14, 400, "Segoe MDL2 Assets");
    }

    private const uint ClearTypeNatural = 6;

    // Solo con el juego abierto: _visible es lo que pide el jugador (botón, Ctrl+Mayús+O); _inGame, si Fortnite está
    // abierto y sin minimizar en alguna pantalla. En pantalla solo cuando las dos se cumplen.
    private static Func<int, bool>? _isGame; // id de proceso → ¿es el juego?
    private static readonly Dictionary<uint, bool> _gamePids = new();
    private static volatile bool _inGame = true;
    private static WinEventDelegate? _winEvent; // keep the delegate alive
    private static readonly List<IntPtr> _winHooks = new();

    private const int MinWidth = 170, Pad = 10, Stripe = 3, ColGap = 16, StatGap = 12;

    public static bool IsRunning => _hwnd != IntPtr.Zero;

    /// <summary>True when the global Ctrl+Shift+O shortcut could be registered.</summary>
    public static bool HotkeyRegistered { get; private set; }

    /// <summary>True when the global Ctrl+Shift+L shortcut (lock/unlock) could be registered.</summary>
    public static bool LockHotkeyRegistered { get; private set; }

    /// <summary>Locked = click-through: the mouse goes through the overlay and it cannot be moved.</summary>
    public static bool IsLocked => _locked;

    /// <summary>True when the overlay is turned on (it is on screen only while the game is open, see <c>isGame</c>).</summary>
    public static bool IsVisible => _hwnd != IntPtr.Zero && _visible;

    /// <param name="isGame">Given a process id, whether it is the game: the overlay is only on screen while a window of
    /// the game is open and not minimized, on any screen. Null = always (previews).</param>
    public static void Start(int? x, int? y, bool locked, Action<int, int> onMoved, Action<bool> onLockChanged,
        bool visible = true, Action<bool>? onVisibleChanged = null, Action? onClose = null, Func<int, bool>? isGame = null)
    {
        if (_thread != null)
        {
            return;
        }

        _visible = visible;
        _isGame = isGame;
        _onVisibleChanged = onVisibleChanged;
        _onClose = onClose;

        _onMoved = onMoved;
        _offscreen = x <= -10000; // --overlay-preview: never brought back on screen
        _onLockChanged = onLockChanged;
        _locked = locked;
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

    /// <summary>Replaces what is shown. The window resizes itself to fit.</summary>
    public static void Update(OverlayView view)
    {
        _view = view;
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_RELAYOUT, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Saves the overlay as it is drawn to a PNG file (works off-screen). Used by --overlay-preview.</summary>
    public static bool Snapshot(string path)
    {
        if (_hwnd == IntPtr.Zero || !GetWindowRect(_hwnd, out var r))
        {
            return false;
        }
        int w = r.Right - r.Left, h = r.Bottom - r.Top;
        var screen = GetDC(IntPtr.Zero);
        var mem = CreateCompatibleDC(screen);
        var bmp = CreateCompatibleBitmap(screen, w, h);
        var old = SelectObject(mem, bmp);
        var ok = PrintWindow(_hwnd, mem, 1 /* PW_CLIENTONLY: WM_PRINT -> WM_PRINTCLIENT */);
        SelectObject(mem, old);
        var bi = new BITMAPINFOHEADER { biSize = 40, biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 };
        var bgra = new byte[w * h * 4];
        GetDIBits(mem, bmp, 0, (uint)h, bgra, ref bi, 0);
        DeleteObject(bmp);
        DeleteDC(mem);
        ReleaseDC(IntPtr.Zero, screen);
        Png.Write(path, w, h, bgra);
        return ok;
    }

    public static void ToggleLock()
    {
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_LOCK, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Vuelve a mirar si el juego está abierto (Fortnite cerrado sin cambiar de ventana activa no avisa).</summary>
    public static void Recheck()
    {
        if (_hwnd != IntPtr.Zero && _isGame != null)
        {
            PostMessageW(_hwnd, WM_APP_RECHECK, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public static void Toggle()
    {
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_APP_TOGGLE, IntPtr.Zero, IntPtr.Zero);
        }
    }

    /// <summary>Quita el overlay de la pantalla, termina su hilo y apaga sus atajos (Ctrl+Mayús+O/L).</summary>
    public static void Stop()
    {
        var thread = _thread;
        if (thread == null)
        {
            return;
        }
        if (_hwnd != IntPtr.Zero)
        {
            PostMessageW(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        thread.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _hwnd = IntPtr.Zero;
        _visible = false;
        HotkeyRegistered = false;
        LockHotkeyRegistered = false;
    }

    private static void Run(int? x, int? y, ManualResetEventSlim ready)
    {
        try { SetProcessDPIAware(); } catch { /* old Windows */ }

        var screenDc = GetDC(IntPtr.Zero);
        _scale = GetDeviceCaps(screenDc, LOGPIXELSY) / 96f;
        ReleaseDC(IntPtr.Zero, screenDc);
        // Desarrollo (capturas del README con --overlay-preview): FCT_OVERLAY_SCALE fuerza la escala.
        if (float.TryParse(Environment.GetEnvironmentVariable("FCT_OVERLAY_SCALE"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var forced) && forced is > 0.5f and < 5f)
        {
            _scale = forced;
            _forcedScale = true;
        }

        int S(int v) => (int)Math.Round(v * _scale);

        CreateFonts();
        _brushBg = CreateSolidBrush(Rgb(15, 19, 27));
        _brushBorder = CreateSolidBrush(Rgb(42, 51, 68));
        _brushLine = CreateSolidBrush(Rgb(34, 41, 56));
        _brushChip = CreateSolidBrush(Rgb(26, 32, 45));
        _brushGold = CreateSolidBrush(Rgb(255, 184, 77));
        _brushGreen = CreateSolidBrush(Rgb(61, 220, 151));
        _brushPill = CreateSolidBrush(Rgb(20, 58, 44));

        var hInstance = GetModuleHandleW(null);
        _wndProc = WndProc;
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            hCursor = LoadCursorW(IntPtr.Zero, IDC_SIZEALL),
            hbrBackground = _brushBg,
            lpszClassName = "FCTOverlay",
        };
        if (RegisterClassExW(ref wc) == 0)
        {
            throw new InvalidOperationException("RegisterClassEx failed");
        }

        int w = S(MinWidth), h = S(90);
        var posX = x ?? S(24);
        var posY = y ?? S(24);

        _hwnd = CreateWindowExW(
            WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
            wc.lpszClassName, L.T("Cofres", "Chests"), WS_POPUP,

            posX, posY, w, h, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("CreateWindowEx failed");
        }

        // La escala del monitor donde queda el overlay (no la de la pantalla principal): con dos pantallas de distinta
        // escala, la letra salía del tamaño del otro monitor.
        if (!_offscreen && !_forcedScale && GetDpiForWindow(_hwnd) is var dpi and > 0 && Math.Abs(dpi / 96f - _scale) > 0.01f)
        {
            _scale = dpi / 96f;
            CreateFonts();
        }
        ApplyLock(_hwnd, _locked, notify: false);
        Relayout(_hwnd);
        if (_isGame != null && !_offscreen)
        {
            // Windows avisa cuando cambia la ventana activa o se minimiza o restaura una ventana (sin temporizador):
            // entonces se mira si el juego sigue abierto.
            _winEvent = (_, _, _, _, _, _, _) => FollowGame(_hwnd);
            foreach (var (min, max) in new[] { (EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND), (EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND) })
            {
                var hook = SetWinEventHook(min, max, IntPtr.Zero, _winEvent, 0, 0, WINEVENT_OUTOFCONTEXT);
                if (hook != IntPtr.Zero)
                {
                    _winHooks.Add(hook);
                }
            }
            _inGame = GameOpen();
        }
        if (_visible && _inGame)
        {
            ShowWindow(_hwnd, SW_SHOWNOACTIVATE);
        }
        // No timer at all: the thread sleeps in GetMessage and only wakes up when the counter changes,
        // when you drag/hide the window or press the shortcut.
        // Global shortcut, works even when the game has the focus.
        HotkeyRegistered = RegisterHotKey(_hwnd, 1, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_O);
        LockHotkeyRegistered = RegisterHotKey(_hwnd, 2, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_L);
        ready.Set();

        while (GetMessageW(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_NCHITTEST:
            {
                // Lock and close buttons = normal client area (clickable); everywhere else = caption (drag to move).
                // When locked the rest of the window is click-through so the game still receives the mouse.
                GetWindowRect(hwnd, out var wr);
                var px = (short)((long)lParam & 0xFFFF) - wr.Left;
                var py = (short)(((long)lParam >> 16) & 0xFFFF) - wr.Top;
                if (Inside(_lockRect, px, py) || (_onClose != null && Inside(_closeRect, px, py)))
                {
                    return (IntPtr)HTCLIENT;
                }
                return (IntPtr)(_locked ? HTTRANSPARENT : HTCAPTION);
            }

            case WM_SETCURSOR:
                if (((long)lParam & 0xFFFF) == HTCLIENT)
                {
                    SetCursor(LoadCursorW(IntPtr.Zero, IDC_HAND));
                    return (IntPtr)1;
                }
                break;

            case WM_LBUTTONUP:
                if (_onClose != null && Inside(_closeRect, (short)((long)lParam & 0xFFFF), (short)(((long)lParam >> 16) & 0xFFFF)))
                {
                    _onClose(); // closes the app; the overlay goes with it
                }
                else
                {
                    ApplyLock(hwnd, !_locked, notify: true);
                }
                return IntPtr.Zero;

            case WM_APP_LOCK:
                ApplyLock(hwnd, !_locked, notify: true);
                return IntPtr.Zero;

            case WM_NCRBUTTONUP:
                SetVisible(hwnd, false);
                return IntPtr.Zero;

            case WM_HOTKEY:
                if ((long)wParam == 2)
                {
                    ApplyLock(hwnd, !_locked, notify: true);
                }
                else
                {
                    SetVisible(hwnd, !_visible);
                }
                return IntPtr.Zero;

            case WM_MOVING:
                // Arrastrándolo: no puede salir de la pantalla ni por un borde.
                if (!_offscreen && lParam != IntPtr.Zero)
                {
                    var rc = Marshal.PtrToStructure<RECT>(lParam);
                    var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfoW(MonitorFromRect(ref rc, MONITOR_DEFAULTTONEAREST), ref mi))
                    {
                        var m = mi.rcMonitor;
                        int w = rc.Right - rc.Left, h = rc.Bottom - rc.Top;
                        var nx = Math.Max(m.Left, Math.Min(rc.Left, m.Right - w));
                        var ny = Math.Max(m.Top, Math.Min(rc.Top, m.Bottom - h));
                        Marshal.StructureToPtr(new RECT { Left = nx, Top = ny, Right = nx + w, Bottom = ny + h }, lParam, false);
                    }
                    return (IntPtr)1;
                }
                break;

            case WM_EXITSIZEMOVE:
                if (GetWindowRect(hwnd, out var r))
                {
                    _onMoved?.Invoke(r.Left, r.Top);
                }
                return IntPtr.Zero;

            case WM_APP_TOGGLE:
                SetVisible(hwnd, !_visible);
                return IntPtr.Zero;

            case WM_DPICHANGED:
                // Movido a un monitor con otra escala: letras y tamaño a la nueva (el sitio lo pone Relayout).
                if (!_forcedScale)
                {
                    _scale = ((int)((long)wParam & 0xFFFF)) / 96f;
                    CreateFonts();
                    _stableW = 0;
                    Relayout(hwnd);
                }
                return IntPtr.Zero;

            case WM_APP_RECHECK:
                FollowGame(hwnd);
                return IntPtr.Zero;

            case WM_APP_RELAYOUT:
                Relayout(hwnd);
                return IntPtr.Zero;

            case WM_PAINT:
                Paint(hwnd);
                return IntPtr.Zero;

            case WM_PRINTCLIENT: // snapshot (--overlay-preview): draw into the given DC
                PaintTo(hwnd, wParam);
                return IntPtr.Zero;

            case WM_DESTROY:
                foreach (var hook in _winHooks)
                {
                    UnhookWinEvent(hook);
                }
                _winHooks.Clear();
                UnregisterHotKey(hwnd, 1);
                UnregisterHotKey(hwnd, 2);
                PostQuitMessage(0);
                return IntPtr.Zero;
        }
        return DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private static bool Inside(RECT r, int x, int y) => x >= r.Left && x < r.Right && y >= r.Top && y < r.Bottom;

    /// <summary>Locked: click-through except the lock button (HTTRANSPARENT in WM_NCHITTEST). Unlocked: drag to move.</summary>
    private static void ApplyLock(IntPtr hwnd, bool locked, bool notify)
    {
        _locked = locked;
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        InvalidateRect(hwnd, IntPtr.Zero, true);

        if (notify)
        {
            _onLockChanged?.Invoke(locked);
            Console.WriteLine(locked
                ? L.F("Overlay bloqueado: el ratón lo atraviesa. Haz clic en el candado{0} para desbloquearlo.",
                      "Overlay locked: the mouse goes through it. Click the padlock{0} to unlock it.",
                      LockHotkeyRegistered ? L.T(" o Ctrl+Mayús+L", " or press Ctrl+Shift+L") : "")
                : L.T("Overlay desbloqueado: arrastra con el botón izquierdo para moverlo; bloquéalo de nuevo con el candado.",
                      "Overlay unlocked: drag it with the left mouse button; lock it again with the padlock."));
        }
    }

    /// <summary>
    /// Fortnite está abierto y sin minimizar en alguna pantalla. Entonces el overlay se ve en la pantalla y el sitio
    /// donde el jugador lo haya puesto (también en otra pantalla), aunque esté usando otra ventana.
    /// </summary>
    private static bool GameOpen()
    {
        if (_isGame == null)
        {
            return true;
        }
        if (_gamePids.Count > 512)
        {
            _gamePids.Clear();
        }
        var own = (uint)Environment.ProcessId;
        var open = false;
        EnumWindows((h, _) =>
        {
            if (!IsWindowVisible(h) || IsIconic(h) || GetWindowThreadProcessId(h, out var pid) == 0 || pid == own)
            {
                return true;
            }
            if (!_gamePids.TryGetValue(pid, out var isGame))
            {
                _gamePids[pid] = isGame = _isGame((int)pid);
            }
            open = isGame;
            return !open;
        }, IntPtr.Zero);
        return open;
    }

    private static void FollowGame(IntPtr hwnd)
    {
        var inGame = GameOpen();
        if (inGame == _inGame)
        {
            return;
        }
        _inGame = inGame;
        if (inGame && !_visible)
        {
            // Fortnite se acaba de abrir: el overlay se enciende (apagado a mano, sigue así hasta la próxima vez).
            _visible = true;
            _onVisibleChanged?.Invoke(true);
        }
        if (!_visible)
        {
            return;
        }
        if (inGame)
        {
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            ShowWindow(hwnd, SW_HIDE);
        }
    }

    private static void SetVisible(IntPtr hwnd, bool visible)
    {
        _visible = visible;
        _onVisibleChanged?.Invoke(visible);
        if (visible && _isGame != null && !_offscreen)
        {
            _inGame = GameOpen();
        }
        if (visible && !_inGame)
        {
            return; // encendido: aparecerá en cuanto el juego esté abierto
        }
        if (visible)
        {
            ShowWindow(hwnd, SW_SHOWNOACTIVATE);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            ShowWindow(hwnd, SW_HIDE);
            Console.WriteLine(HotkeyRegistered
                ? L.T("Overlay oculto: Ctrl+Mayús+O (desde cualquier sitio) lo vuelve a mostrar.", "Overlay hidden: Ctrl+Shift+O (from anywhere) shows it again.")
                : L.T("Overlay oculto: usa el botón Overlay de la ventana para volver a mostrarlo.", "Overlay hidden: use the Overlay button in the window to show it again."));
        }
    }

    private static int TextWidth(IntPtr hdc, IntPtr font, string text, out int height)
    {
        SelectObject(hdc, font);
        var r = new RECT();
        DrawTextW(hdc, text.Length == 0 ? " " : text, -1, ref r, DT_CALCRECT | DT_SINGLELINE | DT_NOPREFIX);
        height = r.Bottom - r.Top;
        return r.Right - r.Left;
    }

    /// <summary>Badge text without its leading dot (the pill already shows the state).</summary>
    private static string BadgeText(string badge) => badge.TrimStart('●', ' ');

    /// <summary>Measures the text and resizes the window (and its rounded region) to fit it.</summary>
    private static void Relayout(IntPtr hwnd)
    {
        int S(int v) => (int)Math.Round(v * _scale);
        var v = _view;
        var hdc = GetDC(hwnd);
        try
        {
            // Header: "COFRES" ... [● EN DIRECTO] [lock]
            _badgeW = v.Badge != null ? TextWidth(hdc, _fontHeader, BadgeText(v.Badge), out _) + S(22) : 0;
            var buttons = _onClose != null ? S(44) : S(24); // lock (+ close)
            var headerW = TextWidth(hdc, _fontHeader, Header, out _headerH) + S(10) + _badgeW + buttons;

            // Big total on the left, current / last match in a rounded chip on the right.
            _col1W = Math.Max(TextWidth(hdc, _fontBig, v.Total, out _bigH), TextWidth(hdc, _fontCaption, CaptionTotal.ToUpperInvariant(), out _captionH));
            var currentW = Math.Max(TextWidth(hdc, _fontBig, v.Current ?? "—", out _),
                Math.Max(TextWidth(hdc, _fontCaption, v.CurrentCaption.ToUpperInvariant(), out _), TextWidth(hdc, _fontCaption, OverlayModel.LastMatchCaption.ToUpperInvariant(), out _)));
            _chipW = currentW + 2 * S(10);
            _chipH = _bigH + _captionH + S(8);
            _mainH = Math.Max(_bigH + _captionH, _chipH);
            var mainW = _col1W + S(ColGap) + _chipW;

            // Stats strip: one column per row (label / value / detail).
            _statW = new int[v.Rows.Count];
            _statH = 0;
            for (var i = 0; i < v.Rows.Count; i++)
            {
                var row = v.Rows[i];
                _statW[i] = Math.Max(TextWidth(hdc, _fontLabel, row.Label.ToUpperInvariant(), out _), TextWidth(hdc, _fontStat, row.Value, out var vh));
                _statH = Math.Max(_statH, vh);
            }
            var statsW = _statW.Sum() + Math.Max(0, _statW.Length - 1) * S(StatGap);

            // Never shrinks: the overlay keeps the same size between a match in progress and the end of the match.
            var contentW = Math.Max(headerW, Math.Max(mainW, statsW));
            var w0 = Math.Max(_stableW, Math.Max(S(MinWidth), contentW + 2 * S(Pad) + S(Stripe)));
            _stableW = w0;
            var h = S(9) + _headerH + S(6) + _mainH + S(8);
            if (v.Rows.Count > 0)
            {
                h += 1 + S(7) + _captionH + S(1) + _statH + S(9);
            }
            // Header buttons on the right: [lock] [close]. Without a close action the lock keeps the corner.
            var corner = new RECT { Left = w0 - S(Pad) - S(14), Top = S(4), Right = w0 - S(4), Bottom = S(9) + _headerH + S(4) };
            if (_onClose != null)
            {
                _closeRect = corner;
                _lockRect = new RECT { Left = corner.Left - S(22), Top = corner.Top, Right = corner.Left - S(2), Bottom = corner.Bottom };
            }
            else
            {
                _lockRect = corner;
            }
            // Keep it fully on its screen (the whole monitor: the game covers the taskbar): on the right half of the
            // screen it grows to the left, and it is pushed back inside when it would overflow.
            GetWindowRect(hwnd, out var cur);
            int nx = cur.Left, ny = cur.Top;
            var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!_offscreen && GetMonitorInfoW(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref mi))
            {
                var work = mi.rcMonitor;
                if (cur.Right - cur.Left > 0 && (cur.Left + cur.Right) / 2 > (work.Left + work.Right) / 2)
                {
                    nx = cur.Right - w0; // anchored on the right edge
                }
                nx = Math.Max(work.Left, Math.Min(nx, work.Right - w0));
                ny = Math.Max(work.Top, Math.Min(ny, work.Bottom - h));
            }
            SetWindowPos(hwnd, HWND_TOPMOST, nx, ny, w0, h, SWP_NOACTIVATE);
            SetWindowRgn(hwnd, CreateRoundRectRgn(0, 0, w0 + 1, h + 1, S(16), S(16)), true); // window owns the region
            if (nx != cur.Left || ny != cur.Top)
            {
                _onMoved?.Invoke(nx, ny);
            }
        }
        finally
        {
            ReleaseDC(hwnd, hdc);
        }
        InvalidateRect(hwnd, IntPtr.Zero, true);
        UpdateWindow(hwnd); // paint now (one repaint per update, no timer)
    }

    private static void Text(IntPtr hdc, IntPtr font, uint color, string text, int left, int top, int right, int bottom, uint align = DT_LEFT)
    {
        SelectObject(hdc, font);
        SetTextColor(hdc, color);
        var r = new RECT { Left = left, Top = top, Right = right, Bottom = bottom };
        DrawTextW(hdc, text, -1, ref r, DT_SINGLELINE | DT_NOPREFIX | align);
    }

    private static void RoundFill(IntPtr hdc, IntPtr brush, int left, int top, int right, int bottom, int radius)
    {
        var rgn = CreateRoundRectRgn(left, top, right + 1, bottom + 1, radius, radius);
        FillRgn(hdc, rgn, brush);
        DeleteObject(rgn);
    }

    private static void Paint(IntPtr hwnd)
    {
        var hdc = BeginPaint(hwnd, out var ps);
        try
        {
            PaintTo(hwnd, hdc);
        }
        finally
        {
            EndPaint(hwnd, ref ps);
        }
    }

    private static void PaintTo(IntPtr hwnd, IntPtr hdc)
    {
        var v = _view;
        GetClientRect(hwnd, out var rc);
        FillRect(hdc, ref rc, _brushBg);
        SetBkMode(hdc, TRANSPARENT);

        int S(int n) => (int)Math.Round(n * _scale);
        var muted = Rgb(122, 132, 150);
        var ink = Rgb(230, 234, 242);
        var gold = Rgb(255, 184, 77);
        var green = Rgb(61, 220, 151);

        // Accent stripe on the left edge (gold, green during a match), clipped by the rounded window region.
        var stripe = new RECT { Left = 0, Top = 0, Right = S(Stripe), Bottom = rc.Bottom };
        FillRect(hdc, ref stripe, v.CurrentLive ? _brushGreen : _brushGold);

        // Thin border following the rounded shape.
        var border = CreateRoundRectRgn(0, 0, rc.Right + 1, rc.Bottom + 1, S(16), S(16));
        FrameRgn(hdc, border, _brushBorder, 1, 1);
        DeleteObject(border);

        var x = S(Stripe) + S(Pad);
        var right = rc.Right - S(Pad);
        var y = S(9);

        // Header: title, state pill, lock button (Segoe MDL2 Assets: E785 = open padlock, E72E = closed padlock).
        Text(hdc, _fontHeader, muted, Header, x, y, right, y + _headerH);
        if (v.Badge != null)
        {
            var pr = _lockRect.Left - S(6);
            var pl = pr - _badgeW;
            RoundFill(hdc, _brushPill, pl, y - S(3), pr, y + _headerH + S(3), _headerH + S(6));
            var dot = S(7);
            var dy = y + (_headerH - dot) / 2;
            var dotRgn = CreateEllipticRgn(pl + S(7), dy, pl + S(7) + dot + 1, dy + dot + 1);
            FillRgn(hdc, dotRgn, _brushGreen);
            DeleteObject(dotRgn);
            Text(hdc, _fontHeader, green, BadgeText(v.Badge), pl + S(17), y, pr, y + _headerH);
        }
        SelectObject(hdc, _fontIcon);
        SetTextColor(hdc, _locked ? Rgb(86, 96, 114) : Rgb(200, 206, 218));
        var lr = _lockRect;
        DrawTextW(hdc, _locked ? "\uE72E" : "\uE785", -1, ref lr, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
        if (_onClose != null)
        {
            // Close (Segoe MDL2 Assets E8BB = ChromeClose): closes the app and the overlay.
            SetTextColor(hdc, Rgb(200, 206, 218));
            var cr = _closeRect;
            DrawTextW(hdc, "\uE8BB", -1, ref cr, DT_SINGLELINE | DT_CENTER | DT_VCENTER | DT_NOPREFIX);
        }
        y += _headerH + S(6);

        // Total (left) and current / last match chip (right).
        var ty = y + (_mainH - _bigH - _captionH) / 2;
        Text(hdc, _fontBig, gold, v.Total, x - S(1), ty, x + _col1W + S(4), ty + _bigH);
        Text(hdc, _fontCaption, muted, CaptionTotal.ToUpperInvariant(), x, ty + _bigH, x + _col1W + S(4), ty + _bigH + _captionH);

        var cl = right - _chipW;
        var ct = y + (_mainH - _chipH) / 2;
        RoundFill(hdc, _brushChip, cl, ct, right, ct + _chipH, S(10));
        var currentColor = v.Current == null ? Rgb(80, 88, 104) : v.CurrentLive ? green : ink;
        Text(hdc, _fontBig, currentColor, v.Current ?? "—", cl, ct + S(3), right, ct + S(3) + _bigH, DT_CENTER);
        Text(hdc, _fontCaption, v.CurrentLive ? green : muted, v.CurrentCaption.ToUpperInvariant(), cl, ct + S(3) + _bigH, right, ct + S(3) + _bigH + _captionH, DT_CENTER);
        y += _mainH + S(8);

        // Stats strip: columns spread over the whole width.
        if (v.Rows.Count > 0)
        {
            var line = new RECT { Left = x, Top = y, Right = right, Bottom = y + 1 };
            FillRect(hdc, ref line, _brushLine);
            y += 1 + S(7);
            var free = right - x - _statW.Sum();
            var gap = v.Rows.Count > 1 ? free / (v.Rows.Count - 1) : 0;
            var cx = x;
            for (var i = 0; i < v.Rows.Count; i++)
            {
                var row = v.Rows[i];
                var colRight = i == v.Rows.Count - 1 ? right + S(4) : cx + _statW[i] + S(4);
                Text(hdc, _fontLabel, muted, row.Label.ToUpperInvariant(), cx, y, colRight, y + _captionH);
                var vy = y + _captionH + S(1);
                Text(hdc, _fontStat, row.Treasure ? Rgb(255, 128, 170) : ink, row.Value, cx, vy, colRight, vy + _statH);
                cx += _statW[i] + gap;
            }
        }
    }

    private static uint Rgb(byte r, byte g, byte b) => (uint)(r | (g << 8) | (b << 16));

    // ---- Win32 ------------------------------------------------------------------------------
    private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const uint WS_POPUP = 0x80000000;
    private const uint WS_EX_TOPMOST = 0x00000008, WS_EX_TOOLWINDOW = 0x00000080, WS_EX_NOACTIVATE = 0x08000000;
    private const int HTCLIENT = 1, HTTRANSPARENT = -1;
    private const uint WM_SETCURSOR = 0x0020, WM_LBUTTONUP = 0x0202, WM_APP_LOCK = 0x8003, VK_L = 0x4C, SWP_FRAMECHANGED = 0x20;
    private static readonly IntPtr IDC_HAND = new(32649);
    private const int SW_HIDE = 0, SW_SHOWNOACTIVATE = 4;
    private const uint WM_CLOSE = 0x0010, WM_DESTROY = 0x0002, WM_PAINT = 0x000F, WM_NCHITTEST = 0x0084, WM_NCRBUTTONUP = 0x00A5,
        WM_TIMER = 0x0113, WM_EXITSIZEMOVE = 0x0232, WM_APP_TOGGLE = 0x8001, WM_PRINTCLIENT = 0x0318, WM_APP_RELAYOUT = 0x8002, WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000, VK_O = 0x4F;
    private const int HTCAPTION = 2;
    private const int TRANSPARENT = 1, LOGPIXELSY = 90;
    private const uint DT_LEFT = 0x0, DT_CENTER = 0x1, DT_RIGHT = 0x2, DT_VCENTER = 0x4, DT_BOTTOM = 0x8, DT_SINGLELINE = 0x20, DT_CALCRECT = 0x400, DT_NOPREFIX = 0x800;
    private const uint SWP_NOSIZE = 0x1, SWP_NOMOVE = 0x2, SWP_NOACTIVATE = 0x10;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr IDC_SIZEALL = new(32646);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public IntPtr hInstance, hIcon, hCursor, hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam, lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore, fIncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetMessageW(out MSG msg, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr hwnd, IntPtr rgn, bool redraw);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT ps);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hwnd, ref PAINTSTRUCT ps);
    [DllImport("user32.dll")] private static extern int FillRect(IntPtr hdc, ref RECT rect, IntPtr brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int DrawTextW(IntPtr hdc, string text, int count, ref RECT rect, uint format);
    [DllImport("user32.dll")] private static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint ms, IntPtr proc);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursorW(IntPtr instance, IntPtr name);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandleW(string? name);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(IntPtr hdc, int index);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateSolidBrush(uint color);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO { public uint cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref RECT rect, uint flags);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    private const uint WM_DPICHANGED = 0x02E0;
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    private delegate void WinEventDelegate(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);
    [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventDelegate proc, uint pid, uint thread, uint flags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003, EVENT_SYSTEM_MINIMIZESTART = 0x0016,
        EVENT_SYSTEM_MINIMIZEEND = 0x0017, WINEVENT_OUTOFCONTEXT = 0x0000, WM_MOVING = 0x0216, WM_APP_RECHECK = 0x8004;
    [DllImport("user32.dll")] private static extern bool GetMonitorInfoW(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr bmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER bi, uint usage);
    [DllImport("gdi32.dll")] private static extern bool FrameRgn(IntPtr hdc, IntPtr rgn, IntPtr brush, int w, int h);
    [DllImport("gdi32.dll")] private static extern bool FillRgn(IntPtr hdc, IntPtr rgn, IntPtr brush);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateEllipticRgn(int x1, int y1, int x2, int y2);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern int SetBkMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll")] private static extern uint SetTextColor(IntPtr hdc, uint color);
}
