using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FCT.Reporting;

/// <summary>
/// FCT's icon as a PNG data URI for the stats page header and its browser tab. Rendered once at 96 px (crisp at 48 px
/// on high-DPI screens, yet small enough for a page that reloads itself). Null when unavailable: the page then
/// draws a chest instead.
/// </summary>
internal static class AppLogo
{
    private const int Pixels = 96;
    private static readonly Lazy<string?> Cached = new(Render);

    public static string? DataUri => Cached.Value;

    private static string? Render()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }
        using var embedded = typeof(AppLogo).Assembly.GetManifestResourceStream("FCT.app.ico");
        if (embedded == null)
        {
            return null;
        }
        try
        {
            using var icon = new Icon(embedded, new Size(256, 256));
            using var large = icon.ToBitmap();
            using var small = new Bitmap(Pixels, Pixels);
            using (var canvas = Graphics.FromImage(small))
            {
                canvas.PixelOffsetMode = PixelOffsetMode.HighQuality;
                canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                canvas.DrawImage(large, 0, 0, Pixels, Pixels);
            }
            using var png = new MemoryStream();
            small.Save(png, ImageFormat.Png);
            return $"data:image/png;base64,{Convert.ToBase64String(png.ToArray())}";
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or IOException)
        {
            return null;
        }
    }
}
