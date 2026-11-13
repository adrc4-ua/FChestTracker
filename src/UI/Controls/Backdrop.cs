using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>A window that paints its own background, so rounded children can show it behind their corners.</summary>
internal interface IBackdrop
{
    /// <summary>Paints the window's background into <paramref name="g"/>, shifted to where <paramref name="child"/> sits.</summary>
    void PaintBackdrop(Graphics g, Control child);
}

/// <summary>What shows behind a rounded control: its card, its window's glow, or a plain colour.</summary>
[SupportedOSPlatform("windows")]
internal static class Backdrop
{
    /// <summary>
    /// Fills <paramref name="g"/> with whatever lies under <paramref name="control"/>. See-through panels in between
    /// (groups of controls inside a card) are looked through.
    /// </summary>
    public static void Fill(Control control, Graphics g)
    {
        var offset = control.Location;
        var host = control.Parent;
        while (host is { } panel && panel is not Card && panel is not IBackdrop && panel.BackColor.A < 255 && panel.Parent != null)
        {
            offset.Offset(panel.Location);
            host = panel.Parent;
        }

        if (host is Card card)
        {
            g.Clear(card.BackColor);
            var saved = g.Save();
            g.TranslateTransform(-offset.X, -offset.Y);
            card.PaintFace(g);
            g.Restore(saved);
        }
        else if (host is IBackdrop window)
        {
            window.PaintBackdrop(g, control);
        }
        else
        {
            g.Clear(host is { BackColor.A: 255 } ? host.BackColor : Theme.Bg);
        }
    }

    /// <summary>
    /// The page background: black, a wide gold glow at the top centre and a fainter one at the bottom right.
    /// <paramref name="offset"/> is where the painted area sits inside the window.
    /// </summary>
    public static void Glow(Graphics g, Size window, Point offset)
    {
        g.Clear(Theme.Bg);
        if (window.Width <= 0 || window.Height <= 0)
        {
            return;
        }
        var saved = g.Save();
        g.TranslateTransform(-offset.X, -offset.Y);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float w = window.Width, h = window.Height;
        Light(g, new RectangleF(w * 0.5f - w * 0.85f, -h * 0.62f, w * 1.7f, h * 1.05f), 40);
        Light(g, new RectangleF(w * 0.62f, h * 0.55f, w * 0.9f, h * 0.95f), 18);
        g.Restore(saved);
    }

    /// <summary>An elliptic gold light fading to nothing at its edge.</summary>
    private static void Light(Graphics g, RectangleF ellipse, int alpha)
    {
        using var shape = new GraphicsPath();
        shape.AddEllipse(ellipse);
        using var light = new PathGradientBrush(shape)
        {
            CenterColor = Color.FromArgb(alpha, Theme.GoldBase),
            SurroundColors = new[] { Color.FromArgb(0, Theme.GoldBase) },
            Blend = new Blend { Factors = new[] { 0f, 0.35f, 1f }, Positions = new[] { 0f, 0.45f, 1f } },
        };
        g.FillPath(light, shape);
    }
}
