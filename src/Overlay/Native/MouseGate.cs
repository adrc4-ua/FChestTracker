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
/// <item>Taking it, it gives the mouse back when the pointer leaves, or when there is input (mouse or keys) while the
/// pointer stays still for a moment: a game in play keeps its hidden pointer pinned while the mouse moves.</item>
/// </list>
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MouseGate
{
    /// <summary>Input with the pointer still for this long: the pointer is pinned (the game has it).</summary>
    private const int PinnedMs = 1000;

    private POINT _last;
    private long _lastMove = Environment.TickCount64, _takenAt;

    /// <summary>The overlay takes the mouse (clickable); otherwise the mouse goes through it to the window below.</summary>
    public bool Taking { get; private set; }

    /// <summary>Looks again; true when <see cref="Taking"/> changed.</summary>
    /// <param name="overlay">The overlay's place on screen.</param>
    public bool Update(RECT overlay)
    {
        if (!GetCursorPos(out var at))
        {
            return Set(false);
        }
        var now = Environment.TickCount64;
        if (at.X != _last.X || at.Y != _last.Y)
        {
            _last = at;
            _lastMove = now;
        }
        var over = overlay.Contains(at.X, at.Y);
        if (!Taking)
        {
            // the cursor is the one of the window below the overlay: shown means free
            var cursor = new CURSORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<CURSORINFO>() };
            var shown = GetCursorInfo(ref cursor) && (cursor.flags & CURSOR_SHOWING) != 0 && cursor.hCursor != IntPtr.Zero;
            return Set(over && shown);
        }
        return Set(over && (ButtonDown() || !Pinned(now))); // never in the middle of a click or a drag
    }

    /// <summary>Gives the mouse back (the overlay hides, or the game takes over).</summary>
    public bool Release() => Set(false);

    /// <summary>
    /// Mouse or keyboard input after the pointer last moved (and after the overlay took the mouse, so a click on it does
    /// not count), with the pointer still since then.
    /// </summary>
    private bool Pinned(long now)
    {
        var input = new LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref input))
        {
            return false;
        }
        // dwTime is the 32-bit tick count of the last input; compared within the same 49-day window
        var sinceInput = unchecked((uint)now - input.dwTime);
        var still = now - Math.Max(_lastMove, _takenAt);
        return still >= PinnedMs && sinceInput < still - 50;
    }

    private static bool ButtonDown() => (GetAsyncKeyState(0x01) & 0x8000) != 0 || (GetAsyncKeyState(0x02) & 0x8000) != 0;

    private bool Set(bool taking)
    {
        if (taking == Taking)
        {
            return false;
        }
        Taking = taking;
        if (taking)
        {
            _takenAt = Environment.TickCount64;
        }
        return true;
    }
}
