using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Where the pointer is for the overlay. The overlay never takes the mouse: it is always transparent to it, so the
/// cursor on screen is always the game's (or the window's below), whatever the game does with it. So whether a cursor
/// is showing tells whether the pointer is free (a menu, Esc, the map, the desktop) or the game hides it (in play), and
/// the overlay only listens to clicks over it while it is free.
/// <para>A single reading is not trusted to end that: games set their cursor again and again (for an instant there may
/// be none), so the pointer only stops being free after the cursor has stayed hidden for a moment, or when it leaves the
/// overlay.</para>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MouseGate
{
    /// <summary>Distance (pixels) within which the pointer is watched closely.</summary>
    private const int NearMargin = 300;

    /// <summary>How long the cursor must stay hidden for a free pointer over the overlay to count as taken by the game.</summary>
    private const int HiddenMs = 250;

    private bool _free;
    private long _hiddenSince = -1;

    /// <summary>The pointer is close to the overlay: worth looking more often.</summary>
    public bool Near { get; private set; }

    /// <summary>The last reading of the cursor (for the overlay's diagnostic log).</summary>
    public CURSORINFO LastCursor { get; private set; }

    /// <summary>The pointer is over the overlay and free; <paramref name="at"/> is where it is.</summary>
    public bool FreeOver(RECT overlay, out POINT at)
    {
        if (!GetCursorPos(out at))
        {
            Near = false;
            return _free = false;
        }
        Near = at.X > overlay.Left - NearMargin && at.X < overlay.Right + NearMargin && at.Y > overlay.Top - NearMargin && at.Y < overlay.Bottom + NearMargin;
        if (!overlay.Contains(at.X, at.Y))
        {
            _hiddenSince = -1;
            return _free = false;
        }
        if (CursorShown())
        {
            _hiddenSince = -1;
            return _free = true;
        }
        var now = Environment.TickCount64;
        if (_hiddenSince < 0)
        {
            _hiddenSince = now;
        }
        if (now - _hiddenSince >= HiddenMs)
        {
            _free = false;
        }
        return _free;
    }

    /// <summary>A cursor is on screen: the window under the pointer shows one (the game hides it while you play).</summary>
    private bool CursorShown()
    {
        var cursor = new CURSORINFO { cbSize = (uint)Marshal.SizeOf<CURSORINFO>() };
        var read = GetCursorInfo(ref cursor);
        LastCursor = cursor;
        return read && (cursor.flags & CURSOR_SHOWING) != 0 && cursor.hCursor != IntPtr.Zero;
    }
}
