using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Whether the overlay may take the mouse. Most of the time it must not: whatever window is under the pointer decides
/// the cursor, so an overlay taking the mouse would show its own cursor over the game while the game hides it (the
/// hidden pointer resting on it, or the overlay redrawing under it). It lets the mouse through, and the game keeps its
/// cursor, except while the pointer is really free over the part of it that can be used (the padlock and ✕ when it is
/// locked, all of it when it is not):
/// <list type="bullet">
/// <item>Letting it through, the cursor on screen is the window's below. Shown means free (a menu, Esc, the map, the
/// desktop): the overlay takes the mouse.</item>
/// <item>Taking it, the overlay gives it back when the pointer leaves that part, and the moment the game pins the
/// pointer: the mouse itself moves (raw input, read only while taking it) and the pointer does not, or the game changes
/// the pointer's clipping. Keys never count, nor clicks other than on the overlay.</item>
/// <item>Given back that way, the cursor on screen is still the overlay's until the pointer moves again (the game keeps
/// it still), so it is hidden at once, and the overlay does not take the mouse again until the pointer has moved.</item>
/// </list>
/// Everything here runs on the overlay's thread.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MouseGate
{
    /// <summary>
    /// Mouse reports and travel (raw counts) with the pointer still that mean the game pinned it. Both, and generous: at
    /// Windows' slowest pointer speed a free pointer needs dozens of counts to move one pixel.
    /// </summary>
    private const int PinnedReports = 8, PinnedTravel = 96;

    /// <summary>Distance (pixels) within which the pointer is watched closely.</summary>
    private const int NearMargin = 300;

    private POINT _last;
    private int _reports, _travel;
    private RECT _clip;
    private POINT? _waitForMove;

    /// <summary>The overlay takes the mouse (clickable); otherwise the mouse goes through it to the window below.</summary>
    public bool Taking { get; private set; }

    /// <summary>The pointer is close to the overlay: worth looking more often.</summary>
    public bool Near { get; private set; }

    /// <summary>A button went down on the overlay itself (a click or a drag): the mouse stays while it is held.</summary>
    public bool PressedOnOverlay { private get; set; }

    /// <summary>
    /// Given back because the game pinned the pointer: the overlay's cursor must go (the pointer will not move for the
    /// window below to set its own). Read once.
    /// </summary>
    public bool HideCursor { get; private set; }

    /// <summary>Looks again; true when <see cref="Taking"/> changed.</summary>
    /// <param name="overlay">The overlay's place on screen.</param>
    /// <param name="usable">Whether a point of the overlay (relative to it) can be used with the mouse.</param>
    public bool Update(RECT overlay, Func<int, int, bool> usable)
    {
        if (!GetCursorPos(out var at))
        {
            return Set(false);
        }
        Near = at.X > overlay.Left - NearMargin && at.X < overlay.Right + NearMargin && at.Y > overlay.Top - NearMargin && at.Y < overlay.Bottom + NearMargin;
        if (_waitForMove is { } still && (still.X != at.X || still.Y != at.Y))
        {
            _waitForMove = null;
        }
        if (Holding())
        {
            return false;
        }
        var over = overlay.Contains(at.X, at.Y) && usable(at.X - overlay.Left, at.Y - overlay.Top);
        if (Taking)
        {
            return ClipChanged() ? Pinned(at) : Set(over);
        }
        if (!over || _waitForMove != null)
        {
            return false;
        }
        // the cursor on screen is the window's below the overlay: shown means the pointer is free
        var cursor = new CURSORINFO { cbSize = (uint)Marshal.SizeOf<CURSORINFO>() };
        var shown = GetCursorInfo(ref cursor) && (cursor.flags & CURSOR_SHOWING) != 0 && cursor.hCursor != IntPtr.Zero;
        return Set(shown);
    }

    /// <summary>The mouse moved (<paramref name="dx"/>, <paramref name="dy"/> raw counts) while taken; true when it gives it back.</summary>
    public bool Moved(int dx, int dy)
    {
        if (!Taking || !GetCursorPos(out var at))
        {
            return false;
        }
        if (at.X != _last.X || at.Y != _last.Y)
        {
            _last = at;
            _reports = _travel = 0;
            return false;
        }
        _reports++;
        _travel += Math.Abs(dx) + Math.Abs(dy);
        return _reports >= PinnedReports && _travel >= PinnedTravel && !Holding() && Pinned(at);
    }

    /// <summary>Gives the mouse back (the overlay hides).</summary>
    public bool Release()
    {
        PressedOnOverlay = false;
        return Set(false);
    }

    /// <summary>Reads <see cref="HideCursor"/> and clears it.</summary>
    public bool TakeHideCursor()
    {
        var hide = HideCursor;
        HideCursor = false;
        return hide;
    }

    /// <summary>A click or drag that started on the overlay and is still held.</summary>
    private bool Holding()
    {
        if (PressedOnOverlay && (GetAsyncKeyState(0x01) & 0x8000) == 0 && (GetAsyncKeyState(0x02) & 0x8000) == 0)
        {
            PressedOnOverlay = false;
        }
        return PressedOnOverlay;
    }

    /// <summary>The game pinned the pointer here: give the mouse back, hide the overlay's cursor, wait for a move.</summary>
    private bool Pinned(POINT at)
    {
        _waitForMove = at;
        HideCursor = true;
        return Set(false);
    }

    private bool ClipChanged() =>
        GetClipCursor(out var clip) && (clip.Left != _clip.Left || clip.Top != _clip.Top || clip.Right != _clip.Right || clip.Bottom != _clip.Bottom);

    private bool Set(bool taking)
    {
        if (taking == Taking)
        {
            return false;
        }
        Taking = taking;
        _reports = _travel = 0;
        if (taking)
        {
            GetCursorPos(out _last);
            GetClipCursor(out _clip);
        }
        return true;
    }
}
