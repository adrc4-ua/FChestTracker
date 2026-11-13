using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>The app's logo (the chest of its icon) over a soft gold glow, like on the page.</summary>
[SupportedOSPlatform("windows")]
public sealed class LogoBox : Control
{
    private static readonly Lazy<Bitmap?> Logo = new(LoadLogo);

    public LogoBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>Strength of the glow (0 for none).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GlowAlpha { get; set; } = 70;

    /// <summary>The 256 px image of the icon inside the exe; null if it cannot be read (the chest is drawn instead).</summary>
    private static Bitmap? LoadLogo()
    {
        try
        {
            using var stream = typeof(LogoBox).Assembly.GetManifestResourceStream("FCT.app.ico");
            if (stream == null)
            {
                return null;
            }
            using var icon = new Icon(stream, new Size(256, 256));
            return icon.ToBitmap();
        }
        catch (Exception)
        {
            return null; // a damaged resource only costs the image: never the window
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Fill(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var side = Math.Min(Width, Height);
        var square = new RectangleF((Width - side) / 2f, (Height - side) / 2f, side, side);
        if (GlowAlpha > 0)
        {
            // the page's drop-shadow(0 6px 14px gold): an ellipse a little larger and lower than the logo
            var spread = side * 0.1f;
            using var halo = new GraphicsPath();
            halo.AddEllipse(square.X - spread, square.Y + side * 0.05f - spread, square.Width + 2 * spread, square.Height + 2 * spread);
            using var glow = new PathGradientBrush(halo) { CenterColor = Color.FromArgb(GlowAlpha, Theme.GoldBase), SurroundColors = new[] { Color.FromArgb(0, Theme.GoldBase) } };
            g.FillPath(glow, halo);
        }
        if (Logo.Value is { } logo)
        {
            g.DrawImage(logo, square);
        }
        else
        {
            SvgIcon.Draw(g, Icons.Chest, square, Theme.GoldBase);
        }
    }
}
