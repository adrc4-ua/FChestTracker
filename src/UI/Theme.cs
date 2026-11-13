using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

namespace FCT.UI;

/// <summary>
/// The look of every window, shared with the stats page: black and gold, dark only. Colours carry the page's CSS
/// names in their comments so both stay in step.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Theme
{
    // ---- surfaces ------------------------------------------------------------------------------------------------------
    public static readonly Color Bg = Color.FromArgb(8, 8, 10);              // --bg
    public static readonly Color Bg2 = Color.FromArgb(17, 17, 20);           // --bg2: behind icons and fields
    public static readonly Color Card = Color.FromArgb(18, 18, 21);          // --card
    public static readonly Color Panel = Color.FromArgb(23, 23, 27);         // --card2: buttons and fields
    public static readonly Color PanelHover = Color.FromArgb(33, 31, 28);
    public static readonly Color Line = Color.FromArgb(44, 42, 38);          // --line2
    public static readonly Color CardEdge = Color.FromArgb(48, 40, 29);      // 14 % gold over --card

    // ---- text --------------------------------------------------------------------------------------------------------
    public static readonly Color Ink = Color.FromArgb(243, 238, 227);        // --ink, ivory
    public static readonly Color Muted = Color.FromArgb(150, 143, 128);      // --muted
    public static readonly Color Faint = Color.FromArgb(90, 85, 75);         // --faint
    public static readonly Color Champ = Color.FromArgb(216, 194, 143);      // --champ

    // ---- gold ----------------------------------------------------------------------------------------------------------
    public static readonly Color GoldBase = Color.FromArgb(232, 176, 75);    // --gold
    public static readonly Color GoldHi = Color.FromArgb(248, 217, 143);     // --gold-hi
    public static readonly Color GoldDeep = Color.FromArgb(196, 138, 44);    // third shade of the big figures
    public static readonly Color GoldLine = Color.FromArgb(82, 65, 37);      // --gold-line: 30 % gold over --card
    public static readonly Color GoldSoft = Color.FromArgb(39, 33, 25);      // --gold-soft: 10 % gold over --card
    public static readonly Color GoldInk = Color.FromArgb(26, 18, 6);        // text on gold

    // ---- roles ---------------------------------------------------------------------------------------------------------
    /// <summary>Primary actions: a <see cref="PillButton"/> with this colour is painted in the gold gradient.</summary>
    public static readonly Color Accent = GoldBase;
    public static readonly Color AccentHover = GoldHi;
    public static readonly Color AccentInk = GoldInk;
    /// <summary>Live: only the dot and the badge.</summary>
    public static readonly Color Mint = Color.FromArgb(134, 214, 160);
    /// <summary>Off, failing, deleting.</summary>
    public static readonly Color Danger = Color.FromArgb(226, 112, 92);

    /// <summary>Mixes two colours (0 gives <paramref name="from"/>, 1 gives <paramref name="to"/>).</summary>
    public static Color Blend(Color from, Color to, float amount) => Color.FromArgb(
        (int)Math.Round(from.R + (to.R - from.R) * amount),
        (int)Math.Round(from.G + (to.G - from.G) * amount),
        (int)Math.Round(from.B + (to.B - from.B) * amount));

    // ---- type ----------------------------------------------------------------------------------------------------------
    // Segoe UI by name: Windows 11's "Variable" version is not drawn well by GDI+ (gold text, the chart) and could hang.
    public static Font Text(float size = 9f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    /// <summary>The font of the big figures.</summary>
    public static Font Display(float size, FontStyle style = FontStyle.Bold) => new("Segoe UI", size, style);

    // ---- shared painting -----------------------------------------------------------------------------------------------

    /// <summary>
    /// A card of the page on <paramref name="fill"/>: a gold veil fading out halfway down, a 1 px shine on top and a thin
    /// gold edge. <paramref name="hero"/>: the main card (a glow from its corner, a stronger edge). <paramref name="flat"/>:
    /// one plain colour and the edge (behind text boxes, so the box itself does not show).
    /// </summary>
    public static void PaintCard(Graphics g, RectangleF bounds, float radius, Color fill, bool hero = false, Color? edge = null, bool flat = false)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = Rounded.Rect(bounds, radius);
        using (var plain = new SolidBrush(fill))
        {
            g.FillPath(plain, shape);
        }
        if (flat)
        {
            using var outline = new Pen(edge ?? CardEdge, 1f);
            g.DrawPath(outline, shape);
            return;
        }

        var saved = g.Save();
        g.SetClip(shape, CombineMode.Intersect);
        if (hero)
        {
            // radial-gradient(420px 220px at 0% 0%, gold .17, transparent 70%)
            var width = Math.Max(bounds.Width * 1.25f, 60f);
            var height = Math.Max(bounds.Height * 1.5f, 40f);
            using var glow = new GraphicsPath();
            glow.AddEllipse(bounds.X - width / 2, bounds.Y - height / 2, width, height);
            using var light = new PathGradientBrush(glow)
            {
                CenterColor = Color.FromArgb(48, GoldBase),
                SurroundColors = new[] { Color.FromArgb(0, GoldBase) },
                FocusScales = new PointF(0.05f, 0.05f),
            };
            g.FillPath(light, glow);
        }
        else
        {
            // linear-gradient(180deg, gold .05, transparent 55%); it runs past the area, or GDI+ restarts it on the last row
            var veil = new RectangleF(bounds.X, bounds.Y, bounds.Width, Math.Max(2f, bounds.Height * 0.55f));
            using var fade = new LinearGradientBrush(new PointF(0, veil.Top - 1), new PointF(0, veil.Bottom + 2), Color.FromArgb(14, GoldBase), Color.FromArgb(0, GoldBase));
            g.FillRectangle(fade, veil);
        }
        // inset 0 1px rgba(255,255,255,.03)
        using (var shine = new Pen(Color.FromArgb(9, 255, 255, 255), 1f))
        {
            g.DrawLine(shine, bounds.X + radius, bounds.Y + 1.5f, bounds.Right - radius, bounds.Y + 1.5f);
        }
        g.Restore(saved);

        using var border = new Pen(edge ?? (hero ? GoldLine : CardEdge), 1f);
        g.DrawPath(border, shape);
    }

    /// <summary>The vertical gold gradient of big figures and primary buttons; <paramref name="deep"/> adds a darker third shade.</summary>
    public static LinearGradientBrush GoldBrush(RectangleF bounds, bool deep = false)
    {
        var gold = new LinearGradientBrush(new PointF(0, bounds.Top - 1), new PointF(0, bounds.Bottom + 1), GoldHi, deep ? GoldDeep : GoldBase);
        if (deep)
        {
            gold.InterpolationColors = new ColorBlend { Colors = new[] { GoldHi, GoldBase, GoldDeep }, Positions = new[] { 0f, 0.6f, 1f } };
        }
        return gold;
    }
}
