using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace FCT.Reporting;

/// <summary>
/// The app icon (Assets/app.ico, embedded in the exe) as a PNG data URI, for the report header and the browser tab.
/// Built once; null when it cannot be read (the report then shows a drawn chest instead).
/// </summary>
internal static class AppLogo
{
    private static readonly Lazy<string?> Uri = new(Build);

    public static string? DataUri => Uri.Value;

    private static string? Build()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FCT.app.ico");
            if (stream == null)
            {
                return null;
            }
            using var icon = new Icon(stream, new Size(256, 256));
            using var source = icon.ToBitmap();
            // 96 px: sharp at 48 px on high-DPI screens, small enough to inline in a page that refreshes itself.
            using var logo = new Bitmap(96, 96);
            using (var g = Graphics.FromImage(logo))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, 96, 96));
            }
            using var png = new MemoryStream();
            logo.Save(png, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(png.ToArray());
        }
        catch
        {
            return null;
        }
    }
}
