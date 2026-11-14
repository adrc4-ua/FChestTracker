using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static FCT.Overlay.Native.Win32;

namespace FCT.Overlay.Native;

/// <summary>
/// Where the pointer is for the overlay. The overlay never takes the mouse: it is always transparent to it, so the
/// cursor on screen is always the game's (or the window's below), whatever the game does with it. So the cursor shown
/// right now is a true reading of whether the pointer is free (a menu, Esc, the map, the desktop) or the game hides it
/// (in play), and the overlay only listens to clicks over it while it is free.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class MouseGate
{
    /// <summary>Distance (pixels) within which the pointer is watched closely.</summary>
    private const int NearMargin = 300;

    /// <summary>The pointer is close to the overlay: worth looking more often.</summary>
    public bool Near { get; private set; }

    /// <summary>The pointer is over the overlay and free (a cursor is showing); <paramref name="at"/> is where it is.</summary>
    public bool FreeOver(RECT overlay, out POINT at)
    {
        if (!GetCursorPos(out at))
        {
            Near = false;
            return false;
        }
        Near = at.X > overlay.Left - NearMargin && at.X < overlay.Right + NearMargin && at.Y > overlay.Top - NearMargin && at.Y < overlay.Bottom + NearMargin;
        return overlay.Contains(at.X, at.Y) && CursorShown();
    }

    /// <summary>A cursor is on screen: the window under the pointer shows one (the game hides it while you play).</summary>
    public static bool CursorShown()
    {
        var cursor = new CURSORINFO { cbSize = (uint)Marshal.SizeOf<CURSORINFO>() };
        return GetCursorInfo(ref cursor) && (cursor.flags & CURSOR_SHOWING) != 0 && cursor.hCursor != IntPtr.Zero;
    }
}
