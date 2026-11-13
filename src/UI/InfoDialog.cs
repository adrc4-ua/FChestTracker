using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>One line of a dialog: an icon (an <see cref="Icons"/> SVG, or a short symbol), its colour, a title and a text.</summary>
public sealed record InfoPoint(string Glyph, Color Color, string Title, string Text);

/// <summary>
/// FCT's question/notice dialog, in the app's black and gold instead of the grey Windows box: a gold icon tile and a
/// title, an optional card of points, and a gold main button with an optional second one.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class InfoDialog : ThemedForm
{
    private const int Width_ = 440, Margin_ = 20, TextLeft = 74, HeaderIcon = 42;
    private const int PointIcon = 32, PointText = 58, ButtonH = 36, MainW = 130, SecondW = 150;

    /// <summary>True for the main button, false for the second one, null when the window was closed.</summary>
    public static bool? Ask(IWin32Window? owner, string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel, string? icon = null)
    {
        using var dialog = new InfoDialog(title, intro, points, ok, cancel, owner != null, icon);
        var result = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        return result == DialogResult.OK ? true : result == DialogResult.No ? false : null;
    }

    /// <summary>True only for the main button.</summary>
    /// <param name="owner">Window to centre on; null (no window yet) centres on the screen and shows in the taskbar.</param>
    /// <param name="cancel">Text of the second button, or null for a single button.</param>
    /// <param name="icon">Header icon (one of <see cref="Icons"/>), the info one by default.</param>
    public static bool Confirm(IWin32Window? owner, string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel, string? icon = null) =>
        Ask(owner, title, intro, points, ok, cancel, icon) == true;

    private InfoDialog(string title, string intro, IReadOnlyList<InfoPoint> points, string ok, string? cancel, bool owned, string? icon)
    {
        Text = title;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = !owned;
        StartPosition = owned ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        Icon = AppIcon.Load();

        var bottom = AddHeader(title, intro, icon ?? Icons.Info);
        if (points.Count > 0)
        {
            bottom = AddPoints(points, bottom) + 18;
        }
        else
        {
            AddPoints(points, bottom); // an invisible, empty card keeps the controls' order the same in every dialog
            bottom += 6;
        }
        AddButtons(ok, cancel, bottom);
        ClientSize = new Size(Width_, bottom + ButtonH + Margin_);
    }

    /// <summary>Gold tile, title and introduction; returns where the next block starts.</summary>
    private int AddHeader(string title, string intro, string icon)
    {
        Controls.Add(new IconTile { IconSvg = icon, Solid = true, Location = new Point(Margin_, Margin_), Size = new Size(HeaderIcon, HeaderIcon) });
        var textWidth = Width_ - TextLeft - Margin_;
        var y = Paragraph(this, title, Theme.Text(12f, FontStyle.Bold), Theme.GoldHi, TextLeft, Margin_ - 1, textWidth) + 4;
        y = Paragraph(this, intro, Theme.Text(9f), Theme.Muted, TextLeft, y, textWidth);
        return Math.Max(y, Margin_ + HeaderIcon) + 16;
    }

    /// <summary>The points inside one card; returns the card's bottom.</summary>
    private int AddPoints(IReadOnlyList<InfoPoint> points, int top)
    {
        var card = new Card { Location = new Point(Margin_, top), Radius = 16, BackColor = Theme.Card, Visible = points.Count > 0 };
        var width = Width_ - 2 * Margin_;
        var y = 14;
        foreach (var point in points)
        {
            card.Controls.Add(PointBadge(point, new Point(14, y)));
            var textWidth = width - PointText - 14;
            var below = Paragraph(card, point.Title, Theme.Text(9.25f, FontStyle.Bold), point.Color, PointText, y, textWidth);
            below = Paragraph(card, point.Text, Theme.Text(8.75f), Theme.Muted, PointText, below, textWidth);
            y = Math.Max(below, y + PointIcon) + 12;
        }
        card.Size = new Size(width, y + 2);
        Controls.Add(card);
        return top + card.Height;
    }

    /// <summary>The point's icon in a small gold tile; a short symbol when it is not an SVG.</summary>
    private static Control PointBadge(InfoPoint point, Point at)
    {
        var size = new Size(PointIcon, PointIcon);
        if (point.Glyph.StartsWith("<svg", StringComparison.Ordinal))
        {
            return new IconTile { IconSvg = point.Glyph, Location = at, Size = size };
        }
        var tile = new Card { Location = at, Size = size, Radius = 10, BackColor = Theme.GoldSoft, Edge = Theme.GoldLine };
        tile.Controls.Add(new Label
        {
            Text = point.Glyph,
            Font = Theme.Text(11f, FontStyle.Bold),
            ForeColor = point.Color,
            BackColor = Color.Transparent,
            Location = Point.Empty,
            Size = size,
            TextAlign = ContentAlignment.MiddleCenter,
        });
        return tile;
    }

    /// <summary>Main (gold) button on the right, second one to its left. The second returns No, distinct from closing.</summary>
    private void AddButtons(string ok, string? cancel, int top)
    {
        var main = new PillButton
        {
            Text = ok,
            Size = new Size(MainW, ButtonH),
            Location = new Point(Width_ - Margin_ - MainW, top),
            Radius = 11,
            BackColor = Theme.Accent,
            HoverColor = Theme.AccentHover,
            ForeColor = Theme.AccentInk,
            DialogResult = DialogResult.OK,
        };
        var second = new PillButton
        {
            Text = cancel ?? "",
            Size = new Size(SecondW, ButtonH),
            Location = new Point(main.Left - 8 - SecondW, top),
            Radius = 11,
            DialogResult = DialogResult.No,
            Visible = cancel != null,
        };
        Controls.Add(main);
        Controls.Add(second);
        AcceptButton = main;
        CancelButton = cancel == null ? main : second;
        ActiveControl = main;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    /// <summary>A wrapping label exactly as tall as its text; returns the y right below it.</summary>
    private static int Paragraph(Control parent, string text, Font font, Color color, int x, int y, int width)
    {
        var label = new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Color.Transparent,
            UseMnemonic = false,
            AutoSize = true,
            MaximumSize = new Size(width, 0), // measured at the full width: the last line or word is never cut
            Location = new Point(x, y),
        };
        parent.Controls.Add(label);
        return y + label.PreferredHeight + 2;
    }
}
