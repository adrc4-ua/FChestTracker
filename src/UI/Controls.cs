using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>Figuras redondeadas para los controles dibujados a mano.</summary>
internal static class Shape
{
    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        if (d <= 0.5f)
        {
            p.AddRectangle(r);
            return p;
        }
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

/// <summary>Ventana con fondo propio (el resplandor dorado de la página): sus controles lo pintan detrás de sus esquinas.</summary>
internal interface IBackdrop
{
    /// <summary>Pinta el fondo de la ventana en <paramref name="g"/>, ya trasladado a la posición de <paramref name="child"/>.</summary>
    void PaintBackdrop(Graphics g, Control child);
}

[SupportedOSPlatform("windows")]
internal static class Backdrop
{
    /// <summary>
    /// Fondo detrás de un control redondeado: el de la tarjeta que lo contiene o, directamente sobre la ventana, el
    /// resplandor de la ventana (así las esquinas no dejan un recuadro de otro color).
    /// </summary>
    public static void Paint(Control c, Graphics g)
    {
        // Paneles transparentes intermedios (grupos de controles dentro de una tarjeta): se mira a través de ellos.
        var offset = c.Location;
        var parent = c.Parent;
        while (parent is { } p && p is not Card && p is not IBackdrop && p.BackColor.A < 255 && p.Parent != null)
        {
            offset.Offset(p.Location);
            parent = p.Parent;
        }
        switch (parent)
        {
            case Card card:
            {
                // La tarjeta tal cual (con su velo dorado), desplazada a la posición del control.
                g.Clear(card.BackColor);
                var state = g.Save();
                g.TranslateTransform(-offset.X, -offset.Y);
                Theme.PaintCard(g, new RectangleF(0.5f, 0.5f, card.Width - 1.5f, card.Height - 1.5f), card.Radius, card.BackColor, card.Hero, card.Edge);
                g.Restore(state);
                break;
            }
            case IBackdrop form:
                form.PaintBackdrop(g, c);
                break;
            case { } other:
                g.Clear(other.BackColor.A == 255 ? other.BackColor : Theme.Bg);
                break;
            default:
                g.Clear(Theme.Bg);
                break;
        }
    }

    /// <summary>
    /// El fondo de la página: negro con un resplandor de oro arriba en el centro y otro, más suave, abajo a la derecha
    /// (los dos radial-gradient del body).
    /// </summary>
    public static void PaintGlow(Graphics g, Size size, Point offset)
    {
        g.Clear(Theme.Bg);
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }
        var state = g.Save();
        g.TranslateTransform(-offset.X, -offset.Y);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Glow(g, new RectangleF(size.Width * 0.5f - size.Width * 0.85f, -size.Height * 0.62f, size.Width * 1.7f, size.Height * 1.05f), 40);
        Glow(g, new RectangleF(size.Width * 0.62f, size.Height * 0.55f, size.Width * 0.9f, size.Height * 0.95f), 18);
        g.Restore(state);
    }

    private static void Glow(Graphics g, RectangleF ellipse, int alpha)
    {
        using var path = new GraphicsPath();
        path.AddEllipse(ellipse);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(alpha, Theme.GoldBase),
            SurroundColors = new[] { Color.FromArgb(0, Theme.GoldBase) },
        };
        brush.Blend = new Blend { Factors = new[] { 0f, 0.35f, 1f }, Positions = new[] { 0f, 0.45f, 1f } };
        g.FillPath(brush, path);
    }
}

/// <summary>
/// Ventana con el estilo de la página: fondo negro con resplandor dorado, barra de título oscura (en Windows 11, del
/// mismo negro y con borde dorado) y doble búfer para que no parpadee.
/// </summary>
[SupportedOSPlatform("windows")]
public class ThemedForm : Form, IBackdrop
{
    public ThemedForm()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.PaintGlow(e.Graphics, ClientSize, Point.Empty);

    void IBackdrop.PaintBackdrop(Graphics g, Control child)
    {
        var at = PointToClient(child.Parent!.PointToScreen(child.Location));
        Backdrop.PaintGlow(g, ClientSize, at);
    }
}

/// <summary>
/// Tarjeta de la página: esquinas redondeadas, velo dorado arriba y borde de oro fino. <see cref="Hero"/> es la
/// tarjeta principal (resplandor desde la esquina, borde más marcado).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Card : Panel
{
    public Card()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
    }

    /// <summary>Radio de las esquinas, en píxeles.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 18;

    /// <summary>Tarjeta principal (la de «Cofres abiertos» de la página).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Hero { get; set; }

    /// <summary>Borde de otro color (null = el de oro fino).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? Edge { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Paint(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.PaintCard(e.Graphics, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Radius, BackColor, Hero, Edge);
    }
}

/// <summary>
/// Botón redondeado. Con <see cref="Theme.Accent"/> de fondo es el principal (degradado de oro, texto oscuro); si no,
/// tarjeta oscura con borde de oro fino que se ilumina al pasar el ratón. Puede llevar un icono, ir alineado a la
/// izquierda (como el menú de la página) y una etiqueta a la derecha (<see cref="Chip"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PillButton : Button
{
    private bool _hover, _down;

    public PillButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Theme.Panel;
        ForeColor = Theme.Ink;
        Font = Theme.Text(9f, FontStyle.Bold);
        Cursor = Cursors.Hand;
    }

    /// <summary>Color de fondo cuando el ratón está encima.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Theme.PanelHover;

    /// <summary>Radio de las esquinas, en píxeles.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = 12;

    /// <summary>Icono SVG delante del texto (los de <see cref="Icons"/>), o null.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? IconSvg { get; set; }

    /// <summary>Icono y texto a la izquierda, como las entradas del menú de la página.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LeftAligned { get; set; }

    private string? _chip;
    private bool _chipOn;

    /// <summary>Etiqueta pequeña a la derecha (por ejemplo «Activado»), o null.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? Chip
    {
        get => _chip;
        set
        {
            if (_chip != value)
            {
                _chip = value;
                Invalidate();
            }
        }
    }

    /// <summary>La etiqueta en oro (encendido) o apagada.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ChipOn
    {
        get => _chipOn;
        set
        {
            if (_chipOn != value)
            {
                _chipOn = value;
                Invalidate();
            }
        }
    }

    private Color? _indicator;

    /// <summary>Punto de estado delante del texto y borde del mismo color (null = sin indicador).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color? Indicator
    {
        get => _indicator;
        set
        {
            if (_indicator != value)
            {
                _indicator = value;
                Invalidate();
            }
        }
    }

    private bool Primary => BackColor.ToArgb() == Theme.Accent.ToArgb();

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = _down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Nada: OnPaint pinta el fondo redondeado completo.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Backdrop.Paint(this, g);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using var path = Shape.Rounded(rect, Radius);
        Color text, icon;
        var soft = 0.22f;
        if (Primary)
        {
            // Botón principal: el degradado de oro de los iconos de sección (.phi) y un borde claro.
            using var fill = new LinearGradientBrush(new PointF(0, -1), new PointF(0, Height + 1),
                _down ? Theme.GoldBase : _hover ? Theme.Blend(Theme.GoldHi, Color.White, 0.25f) : Theme.GoldHi,
                _down ? Theme.GoldDeep : Theme.GoldBase);
            g.FillPath(fill, path);
            using var edge = new Pen(Color.FromArgb(90, 255, 240, 200), 1f);
            g.DrawPath(edge, path);
            text = icon = Theme.GoldInk;
            soft = 0.35f;
        }
        else
        {
            var back = _down ? Theme.Blend(HoverColor, Theme.GoldBase, 0.06f) : _hover ? HoverColor : BackColor;
            using var fill = new SolidBrush(back);
            g.FillPath(fill, path);
            using var edge = new Pen(_indicator is { } dotEdge ? Color.FromArgb(150, dotEdge) : _hover ? Theme.GoldLine : Theme.CardEdge, 1f);
            g.DrawPath(edge, path);
            text = Enabled ? ForeColor : Theme.Faint;
            icon = Enabled ? Theme.GoldBase : Theme.Faint;
        }

        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
        var iconSize = IconSvg != null ? Math.Min(Height * 0.46f, Font.Height * 1.25f) : 0f;
        var dot = _indicator is { } ? Math.Max(6f, Font.Height * 0.45f) : 0f;
        var gap = Font.Height * 0.5f;
        var lead = iconSize > 0 ? iconSize + gap : dot > 0 ? dot + gap * 0.8f : 0f;

        // Etiqueta de la derecha.
        var chipW = 0f;
        if (_chip is { Length: > 0 } chip)
        {
            using var chipFont = new Font(Font.FontFamily, Font.Size * 0.82f, FontStyle.Bold);
            var size = TextRenderer.MeasureText(g, chip, chipFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            var ch = Math.Min(Height - 10f, chipFont.Height + 6f);
            chipW = size.Width + ch;
            var pad = Math.Max(8f, Height * 0.3f);
            var chipRect = new RectangleF(Width - pad - chipW, (Height - ch) / 2f, chipW, ch);
            using var chipPath = Shape.Rounded(chipRect, ch / 2f);
            using var chipFill = new SolidBrush(_chipOn ? Theme.GoldSoft : Theme.Bg2);
            using var chipEdge = new Pen(_chipOn ? Theme.GoldLine : Theme.Line, 1f);
            g.FillPath(chipFill, chipPath);
            g.DrawPath(chipEdge, chipPath);
            TextRenderer.DrawText(g, chip, chipFont, Rectangle.Round(chipRect), _chipOn ? Theme.GoldHi : Theme.Muted,
                TextFormatFlags.SingleLine | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            chipW += pad;
        }

        var textSize = TextRenderer.MeasureText(g, Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        float left;
        if (LeftAligned)
        {
            left = Math.Max(10f, Height * 0.32f);
        }
        else
        {
            left = Math.Max(4f, (Width - chipW - (lead + textSize.Width)) / 2f);
        }
        if (iconSize > 0)
        {
            SvgIcon.Draw(g, IconSvg!, new RectangleF(left, (Height - iconSize) / 2f, iconSize, iconSize), icon, soft);
        }
        else if (_indicator is { } c)
        {
            using var brush = new SolidBrush(c);
            g.FillEllipse(brush, left, (Height - dot) / 2f, dot, dot);
        }
        // Centrado: la caja justa del texto (con la de todo el ancho, el texto largo se cortaba con «…» antes de tiempo).
        var room = Math.Max(10f, Width - chipW - left - lead - 2f);
        var textRect = Rectangle.Round(new RectangleF(left + lead, 0, LeftAligned ? room : Math.Min(room, textSize.Width + 4f), Height));
        TextRenderer.DrawText(g, Text, Font, textRect, text, flags | TextFormatFlags.Left);
    }
}

/// <summary>
/// Cuadro de icono de la página (.ki de las cifras, .gem de los tesoros): oro translúcido con borde dorado y el icono
/// en oro claro. <see cref="Solid"/>: el cuadro de oro macizo de las secciones (.phi), con el icono oscuro.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class IconTile : Control
{
    public IconTile()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? IconSvg { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Solid { get; set; }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Paint(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        var radius = Width * 0.3f;
        using var path = Shape.Rounded(rect, radius);
        if (Solid)
        {
            using var fill = Theme.GoldBrush(rect);
            g.FillPath(fill, path);
        }
        else
        {
            var card = Parent?.BackColor ?? Theme.Card;
            using var fill = new LinearGradientBrush(new PointF(0, -1), new PointF(0, Height + 1),
                Theme.Blend(card, Theme.GoldBase, 0.22f), Theme.Blend(card, Theme.GoldBase, 0.07f));
            g.FillPath(fill, path);
            using var edge = new Pen(Theme.GoldLine, 1f);
            g.DrawPath(edge, path);
        }
        if (IconSvg != null)
        {
            var s = Width * 0.56f;
            SvgIcon.Draw(g, IconSvg, new RectangleF((Width - s) / 2f, (Height - s) / 2f, s, s), Solid ? Theme.GoldInk : Theme.GoldHi, Solid ? 0.35f : 0.22f);
        }
    }
}

/// <summary>El logo de la app (el cofre del icono) con un resplandor dorado detrás, como en la página.</summary>
[SupportedOSPlatform("windows")]
public sealed class LogoBox : Control
{
    private static readonly Lazy<Bitmap?> Logo = new(Load);

    public LogoBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    /// <summary>Intensidad del resplandor (0 = sin él).</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GlowAlpha { get; set; } = 70;

    private static Bitmap? Load()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FCT.app.ico");
            if (stream == null)
            {
                return null;
            }
            using var icon = new Icon(stream, new Size(256, 256));
            return icon.ToBitmap();
        }
        catch
        {
            return null;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Paint(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var side = Math.Min(Width, Height);
        var box = new RectangleF((Width - side) / 2f, (Height - side) / 2f, side, side);
        if (GlowAlpha > 0)
        {
            // filter: drop-shadow(0 6px 14px rgba(232,176,75,.35))
            using var glow = new GraphicsPath();
            var grow = side * 0.1f;
            glow.AddEllipse(box.X - grow, box.Y + side * 0.05f - grow, box.Width + 2 * grow, box.Height + 2 * grow);
            using var brush = new PathGradientBrush(glow) { CenterColor = Color.FromArgb(GlowAlpha, Theme.GoldBase), SurroundColors = new[] { Color.FromArgb(0, Theme.GoldBase) } };
            g.FillPath(brush, glow);
        }
        if (Logo.Value is { } logo)
        {
            g.DrawImage(logo, box);
        }
        else
        {
            SvgIcon.Draw(g, Icons.Chest, box, Theme.GoldBase);
        }
    }
}

/// <summary>Interruptor redondeado (sustituye a la casilla blanca de Windows): oro encendido, gris apagado.</summary>
[SupportedOSPlatform("windows")]
public sealed class ToggleSwitch : Control
{
    private bool _checked;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        Size = new Size(40, 22);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        Focus();
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            Checked = !Checked;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Paint(this, e.Graphics);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new RectangleF(1, 1, Width - 3, Height - 3);
        using (var path = Shape.Rounded(track, track.Height / 2))
        {
            if (_checked)
            {
                using var fill = Theme.GoldBrush(track);
                g.FillPath(fill, path);
            }
            else
            {
                using var fill = new SolidBrush(Theme.Bg2);
                using var edge = new Pen(Theme.Line, 1f);
                g.FillPath(fill, path);
                g.DrawPath(edge, path);
            }
            if (Focused)
            {
                using var ring = new Pen(Color.FromArgb(160, Theme.GoldHi), 1f);
                g.DrawPath(ring, path);
            }
        }
        var d = track.Height - 6;
        var x = _checked ? track.Right - d - 3 : track.Left + 3;
        using var knob = new SolidBrush(_checked ? Theme.GoldInk : Theme.Muted);
        g.FillEllipse(knob, x, track.Top + 3, d, d);
    }
}

/// <summary>
/// Etiqueta de una sola línea que nunca se corta: si el texto no cabe en su caja (ancho o alto),
/// se dibuja con la letra justa para que quepa. Su fuente es el tamaño máximo. Con <see cref="Gold"/>, el texto lleva
/// el degradado de oro de las cifras de la página.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class FitLabel : Label
{
    private Font? _fit;       // fuente reducida propia (null: se usa la del control)
    private Font? _fitBase;   // fuente del control con la que se calculó
    private string? _fitKey;

    public FitLabel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
    }

    /// <summary>Degradado de oro (cifras grandes); <see cref="Deep"/> le añade el tercer tono de la cifra principal.</summary>
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
        var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | Align(TextAlign);
        var font = FitFont(e.Graphics, text);
        if (!Gold || !Enabled || !TryPaintGold(e.Graphics, text, font, flags))
        {
            TextRenderer.DrawText(e.Graphics, text, font, ClientRectangle, Enabled ? ForeColor : SystemColors.GrayText, flags);
        }
    }

    /// <summary>Texto con el degradado de oro. Si GDI+ no puede (fuente rara, impresión), false y se dibuja liso.</summary>
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

    private void PaintGold(Graphics g, string text, Font font, TextFormatFlags flags)
    {

        // Oro: se dibuja la forma del texto con el degradado, en el mismo sitio en que lo pondría TextRenderer.
        g.SmoothingMode = SmoothingMode.AntiAlias;
        // La forma del texto, sin márgenes, colocada como la pondría TextRenderer: a la izquierda con su pequeño margen,
        // y en vertical en la misma línea (la altura de la fuente).
        using var path = new GraphicsPath();
        var em = font.SizeInPoints * g.DpiY / 72f;
        path.AddString(text, font.FontFamily, (int)font.Style, em, PointF.Empty, StringFormat.GenericTypographic);
        var ink = path.GetBounds();
        var lineH = font.GetHeight(g.DpiY);
        var pad = Math.Max(1f, em * 0.06f);
        var x = (flags & TextFormatFlags.HorizontalCenter) != 0 ? (Width - ink.Width) / 2f - ink.Left
            : (flags & TextFormatFlags.Right) != 0 ? Width - pad - ink.Right
            : pad - ink.Left;
        var y = (flags & TextFormatFlags.VerticalCenter) != 0 ? (Height - lineH) / 2f
            : (flags & TextFormatFlags.Bottom) != 0 ? Height - lineH : 0f;
        using (var move = new Matrix())
        {
            move.Translate(x, y);
            path.Transform(move);
        }
        using var brush = Theme.GoldBrush(path.GetBounds(), Deep);
        g.FillPath(brush, path);
    }

    private Font FitFont(IDeviceContext dc, string text)
    {
        var key = $"{text}|{ClientSize}";
        if (key == _fitKey && ReferenceEquals(_fitBase, Font))
        {
            return _fit ?? Font;
        }
        var size = Font.Size;
        var box = ClientSize;
        const TextFormatFlags measure = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        var fit = Font;
        for (var i = 0; i < 12; i++)
        {
            var m = TextRenderer.MeasureText(dc, text, fit, new Size(int.MaxValue, int.MaxValue), measure);
            if (m.Width <= box.Width && m.Height <= box.Height || size <= 5f)
            {
                break;
            }
            // Reduce lo que falte (en ancho o alto) y prueba otra vez: el redondeo de GDI puede pedir un paso más.
            var ratio = Math.Min(box.Width / (float)Math.Max(1, m.Width), box.Height / (float)Math.Max(1, m.Height));
            size = Math.Max(5f, size * Math.Min(ratio, 0.97f));
            if (!ReferenceEquals(fit, Font)) fit.Dispose();
            fit = new Font(Font.FontFamily, size, Font.Style, Font.Unit);
        }
        _fit?.Dispose();
        _fit = ReferenceEquals(fit, Font) ? null : fit;
        _fitBase = Font;
        _fitKey = key;
        return fit;
    }

    private static TextFormatFlags Align(ContentAlignment a) => a switch
    {
        ContentAlignment.TopLeft => TextFormatFlags.Top | TextFormatFlags.Left,
        ContentAlignment.TopCenter => TextFormatFlags.Top | TextFormatFlags.HorizontalCenter,
        ContentAlignment.TopRight => TextFormatFlags.Top | TextFormatFlags.Right,
        ContentAlignment.MiddleLeft => TextFormatFlags.VerticalCenter | TextFormatFlags.Left,
        ContentAlignment.MiddleCenter => TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter,
        ContentAlignment.MiddleRight => TextFormatFlags.VerticalCenter | TextFormatFlags.Right,
        ContentAlignment.BottomLeft => TextFormatFlags.Bottom | TextFormatFlags.Left,
        ContentAlignment.BottomCenter => TextFormatFlags.Bottom | TextFormatFlags.HorizontalCenter,
        _ => TextFormatFlags.Bottom | TextFormatFlags.Right,
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fit?.Dispose();
            _fit = null;
        }
        base.Dispose(disposing);
    }
}

/// <summary>Título de sección de la página (h2): un rombo de oro delante del texto.</summary>
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
        var d = Math.Max(4f, Font.Height * 0.3f);
        var cy = Height / 2f;
        var state = g.Save();
        g.TranslateTransform(d * 0.9f, cy);
        g.RotateTransform(45);
        using (var gold = new SolidBrush(Theme.GoldBase))
        using (var diamond = Shape.Rounded(new RectangleF(-d / 2, -d / 2, d, d), d * 0.25f))
        {
            g.FillPath(gold, diamond);
        }
        g.Restore(state);
        var left = (int)Math.Round(d * 2.4f);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(left, 0, Width - left, Height), ForeColor,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }
}

/// <summary>Menús desplegables con los colores de la aplicación (fondo oscuro, resaltado dorado suave, texto claro).</summary>
[SupportedOSPlatform("windows")]
public sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new Colors())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Theme.GoldHi : e.Item.ForeColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            return;
        }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Shape.Rounded(new RectangleF(2, 1, e.Item.Width - 4, e.Item.Height - 2), 6);
        using var fill = new SolidBrush(Theme.GoldSoft);
        e.Graphics.FillPath(fill, path);
    }

    private sealed class Colors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Card;
        public override Color MenuBorder => Theme.GoldLine;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Theme.GoldSoft;
        public override Color ImageMarginGradientBegin => Theme.Card;
        public override Color ImageMarginGradientMiddle => Theme.Card;
        public override Color ImageMarginGradientEnd => Theme.Card;
        public override Color SeparatorDark => Theme.Line;
        public override Color SeparatorLight => Theme.Card;
    }
}
