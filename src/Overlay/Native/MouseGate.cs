using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Whether the overlay may take the mouse. Most of the time it must not: whatever window is under the pointer decides
/// the cursor, and the overlay would show its own (an arrow or a hand) over the game while the game hides it, for
/// instance when the hidden pointer rests on the overlay or when the overlay redraws under it. So it lets the mouse
/// through (the game keeps its cursor) and only takes it while the pointer is really free over it:
/// <list type="bullet">
/// <item>Letting it through, the pointer over the overlay is over the window below: if that window shows a cursor (a
/// menu, Esc, the desktop) the mouse is free, and the overlay takes it (padlock, ✕, dragging).</item>
/// <item>Taking it, it gives the mouse back when the pointer leaves, or as soon as the mouse itself moves (raw input,
/// read only while taking it) and the pointer does not: a game in play keeps its hidden pointer pinned. Keys and clicks
/// never count.</item>
/// </list>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MouseGate
{
    /// <summary>Mouse travel (in raw counts) with the pointer still that means the pointer is pinned.</summary>
    private const int PinnedTravel = 24;

    private POINT _last;
    private int _travel;

    /// <summary>The overlay takes the mouse (clickable); otherwise the mouse goes through it to the window below.</summary>
    public bool Taking { get; private set; }

    /// <summary>Looks again (every moment while on screen); true when <see cref="Taking"/> changed.</summary>
    /// <param name="overlay">The overlay's place on screen.</param>
    public bool Update(RECT overlay)
    {
        if (!GetCursorPos(out var at))
        {
            return Set(false);
        }
        var over = overlay.Contains(at.X, at.Y);
        if (Taking)
        {
            return Set(over);
        }
        // the cursor is the one of the window below the overlay: shown means free
        var cursor = new CURSORINFO { cbSize = (uint)Marshal.SizeOf<CURSORINFO>() };
        var shown = GetCursorInfo(ref cursor) && (cursor.flags & CURSOR_SHOWING) != 0 && cursor.hCursor != IntPtr.Zero;
        return Set(over && shown);
    }

    /// <summary>
    /// The mouse moved by (<paramref name="dx"/>, <paramref name="dy"/>) raw counts while the overlay takes it. True when
    /// it gives the mouse back: the mouse keeps moving and the pointer does not.
    /// </summary>
    public bool Moved(int dx, int dy)
    {
        if (!Taking || !GetCursorPos(out var at))
        {
            return false;
        }
        if (at.X != _last.X || at.Y != _last.Y)
        {
            _last = at;
            _travel = 0;
            return false;
        }
        _travel += Math.Abs(dx) + Math.Abs(dy);
        // never in the middle of a click or a drag
        return _travel >= PinnedTravel && !ButtonDown() && Set(false);
    }

    /// <summary>Gives the mouse back (the overlay hides).</summary>
    public bool Release() => Set(false);

    private static bool ButtonDown() => (GetAsyncKeyState(0x01) & 0x8000) != 0 || (GetAsyncKeyState(0x02) & 0x8000) != 0;

    private bool Set(bool taking)
    {
        if (taking == Taking)
        {
            return false;
        }
        Taking = taking;
        _travel = 0;
        if (taking && GetCursorPos(out var at))
        {
            _last = at;
        }
        return true;
    }
}
