using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// A one-line label that never clips: when the text does not fit its box (width or height) it is drawn with a font
/// just small enough. Its Font is the largest size. With <see cref="Gold"/> the text gets the page's gold gradient.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class FitLabel : Label
{
    private const TextFormatFlags OneLine = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;

    // The shrunk font for the current text and size (null: the control's own fits), and what it was computed for.
    private Font? _shrunk;
    private Font? _shrunkFrom;
    private string? _shrunkFor;

    public FitLabel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
    }

    /// <summary>The gold gradient of big figures; <see cref="Deep"/> adds the third shade of the main figure.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Gold { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Deep { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var text = Text ?? "";
        if (text.Length == 0)
        {
            return;
        }
        var flags = OneLine | Placement(TextAlign);
        var font = FittingFont(e.Graphics, text);
        if (!(Gold && Enabled && TryPaintGold(e.Graphics, text, font, flags)))
        {
            TextRenderer.DrawText(e.Graphics, text, font, ClientRectangle, Enabled ? ForeColor : SystemColors.GrayText, flags);
        }
    }

    /// <summary>The gold text; false when GDI+ cannot outline it (odd font, printing), so it is drawn plain.</summary>
    private bool TryPaintGold(Graphics g, string text, Font font, TextFormatFlags flags)
    {
        try
        {
            PaintGold(g, text, font, flags);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ExternalException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Fills the outline of the text with the gold gradient, placed where TextRenderer would put it: its small margin
    /// on the side, and on the same line height.
    /// </summary>
    private void PaintGold(Graphics g, string text, Font font, TextFormatFlags flags)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var outline = new GraphicsPath();
        var emPixels = font.SizeInPoints * g.DpiY / 72f;
        outline.AddString(text, font.FontFamily, (int)font.Style, emPixels, PointF.Empty, StringFormat.GenericTypographic);
        var ink = outline.GetBounds();
        var lineHeight = font.GetHeight(g.DpiY);
        var margin = Math.Max(1f, emPixels * 0.06f);

        var x = flags.HasFlag(TextFormatFlags.HorizontalCenter) ? (Width - ink.Width) / 2f - ink.Left
            : flags.HasFlag(TextFormatFlags.Right) ? Width - margin - ink.Right
            : margin - ink.Left;
        var y = flags.HasFlag(TextFormatFlags.VerticalCenter) ? (Height - lineHeight) / 2f
            : flags.HasFlag(TextFormatFlags.Bottom) ? Height - lineHeight
            : 0f;
        using (var shift = new Matrix())
        {
            shift.Translate(x, y);
            outline.Transform(shift);
        }
        using var gold = Theme.GoldBrush(outline.GetBounds(), Deep);
        g.FillPath(gold, outline);
    }

    /// <summary>The control's font, or a smaller one that fits; cached until the text, size or font change.</summary>
    private Font FittingFont(IDeviceContext dc, string text)
    {
        var key = $"{text}|{ClientSize}";
        if (key == _shrunkFor && ReferenceEquals(_shrunkFrom, Font))
        {
            return _shrunk ?? Font;
        }

        var box = ClientSize;
        var size = Font.Size;
        var font = Font;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var needed = TextRenderer.MeasureText(dc, text, font, new Size(int.MaxValue, int.MaxValue), OneLine);
            if (needed.Width <= box.Width && needed.Height <= box.Height || size <= 5f)
            {
                break;
            }
            // shrink by what is missing and measure again: GDI rounding may ask for one more step
            var ratio = Math.Min(box.Width / (float)Math.Max(1, needed.Width), box.Height / (float)Math.Max(1, needed.Height));
            size = Math.Max(5f, size * Math.Min(ratio, 0.97f));
            if (!ReferenceEquals(font, Font))
            {
                font.Dispose();
            }
            font = new Font(Font.FontFamily, size, Font.Style, Font.Unit);
        }

        _shrunk?.Dispose();
        _shrunk = ReferenceEquals(font, Font) ? null : font;
        _shrunkFrom = Font;
        _shrunkFor = key;
        return font;
    }

    private static TextFormatFlags Placement(ContentAlignment alignment)
    {
        var vertical = alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => TextFormatFlags.Top,
            ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter,
            _ => TextFormatFlags.Bottom,
        };
        var horizontal = alignment switch
        {
            ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => TextFormatFlags.Left,
            ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            _ => TextFormatFlags.Right,
        };
        return vertical | horizontal;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shrunk?.Dispose();
            _shrunk = null;
        }
        base.Dispose(disposing);
    }
}
