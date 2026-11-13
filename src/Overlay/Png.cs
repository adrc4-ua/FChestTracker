using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FCT.Overlay;

/// <summary>Saves a raw pixel buffer (overlay previews) as an opaque PNG, with the PNG encoder Windows already has.</summary>
[SupportedOSPlatform("windows")]
public static class Png
{
    /// <param name="bgra">Top-down rows of 32-bit BGRA pixels; the alpha byte is ignored.</param>
    public static void Write(string path, int width, int height, byte[] bgra)
    {
        using var image = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        var target = image.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var row = new byte[target.Stride];
            for (var y = 0; y < height; y++)
            {
                for (int x = 0, from = y * width * 4; x < width; x++, from += 4)
                {
                    // 24-bit GDI+ pixels are B, G, R: the source already is in that order.
                    row[x * 3] = bgra[from];
                    row[x * 3 + 1] = bgra[from + 1];
                    row[x * 3 + 2] = bgra[from + 2];
                }
                Marshal.Copy(row, 0, target.Scan0 + y * target.Stride, row.Length);
            }
        }
        finally
        {
            image.UnlockBits(target);
        }
        image.Save(path, ImageFormat.Png);
    }
}
