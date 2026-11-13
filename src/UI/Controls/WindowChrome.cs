using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FCT.UI;

/// <summary>
/// The window frame in the app's colours: dark title bar and Windows 11's round corners, and where Windows allows it
/// (Windows 11) a black caption, a gold border and champagne title text. Older Windows ignores what it does not know.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WindowChrome
{
    private const int DarkModeOld = 19, DarkMode = 20, CornerPreference = 33, BorderColor = 34, CaptionColor = 35, TextColor = 36;
    private const int RoundCorners = 2;

    public static void Apply(IntPtr window)
    {
        try
        {
            Set(window, DarkMode, 1);
            Set(window, DarkModeOld, 1);
            Set(window, CornerPreference, RoundCorners);
            Set(window, CaptionColor, ColorRef(Color.FromArgb(11, 11, 13)));
            Set(window, BorderColor, ColorRef(Theme.GoldLine));
            Set(window, TextColor, ColorRef(Theme.Champ));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // no desktop composition: the standard frame stays
        }
    }

    private static void Set(IntPtr window, int attribute, int value) => DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));

    private static int ColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
