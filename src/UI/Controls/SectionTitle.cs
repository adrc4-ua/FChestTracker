using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>A section title of the page (its h2): a small gold diamond before the text.</summary>
[SupportedOSPlatform("windows")]
public sealed class SectionTitle : Control
{
    public SectionTitle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        ForeColor = Theme.Ink;
        Font = Theme.Text(9.5f, FontStyle.Bold);
    }

    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var side = Math.Max(4f, Font.Height * 0.3f);

        var saved = g.Save();
        g.TranslateTransform(side * 0.9f, Height / 2f);
        g.RotateTransform(45);
        using (var gold = new SolidBrush(Theme.GoldBase))
        using (var diamond = Rounded.Rect(new RectangleF(-side / 2, -side / 2, side, side), side * 0.25f))
        {
            g.FillPath(gold, diamond);
        }
        g.Restore(saved);

        var textLeft = (int)Math.Round(side * 2.4f);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(textLeft, 0, Width - textLeft, Height), ForeColor,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}
