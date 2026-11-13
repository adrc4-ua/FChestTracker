using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Paleta y estilo de la interfaz: los de la página de estadísticas (negro + oro, solo oscura). Los nombres antiguos
/// (Cyan, Amber, Accent…) siguen existiendo con los colores nuevos, así todas las ventanas comparten el estilo.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Theme
{
    // ---- Paleta de la página (--bg, --card, --gold…) ------------------------------------------------------------
    public static readonly Color Bg = Color.FromArgb(8, 8, 10);           // --bg
    public static readonly Color Bg2 = Color.FromArgb(17, 17, 20);        // --bg2 (fondo de iconos y campos)
    public static readonly Color Card = Color.FromArgb(18, 18, 21);       // --card
    public static readonly Color Card2 = Color.FromArgb(23, 23, 27);      // --card2
    public static readonly Color Ink = Color.FromArgb(243, 238, 227);     // --ink (marfil)
    public static readonly Color Muted = Color.FromArgb(150, 143, 128);   // --muted
    public static readonly Color Faint = Color.FromArgb(90, 85, 75);      // --faint
    public static readonly Color Line = Color.FromArgb(44, 42, 38);       // --line2
    public static readonly Color LineSoft = Color.FromArgb(31, 30, 29);   // --line
    public static readonly Color GoldBase = Color.FromArgb(232, 176, 75); // --gold
    public static readonly Color GoldHi = Color.FromArgb(248, 217, 143);  // --gold-hi
    public static readonly Color GoldLo = Color.FromArgb(138, 101, 36);   // --gold-lo
    public static readonly Color GoldDeep = Color.FromArgb(196, 138, 44); // tercer tono de las cifras grandes
    public static readonly Color Champ = Color.FromArgb(216, 194, 143);   // --champ
    public static readonly Color CardEdge = Color.FromArgb(48, 40, 29);   // borde de tarjeta: oro al 14 % sobre --card
    public static readonly Color GoldLine = Color.FromArgb(82, 65, 37);   // --gold-line: oro al 30 % sobre --card
    public static readonly Color GoldSoft = Color.FromArgb(39, 33, 25);   // --gold-soft: oro al 10 % sobre --card
    public static readonly Color GoldInk = Color.FromArgb(26, 18, 6);     // texto sobre oro (#1a1206)

    // ---- Nombres usados por las ventanas ---------------------------------------------------------------------------
    public static readonly Color Panel = Card2;                           // botones y campos
    public static readonly Color PanelHover = Color.FromArgb(33, 31, 28);
    public static readonly Color Cyan = GoldBase;                         // antes: cifras principales en cian
    public static readonly Color Amber = GoldBase;
    public static readonly Color Mint = Color.FromArgb(134, 214, 160);    // en directo (solo el punto y el distintivo)
    public static readonly Color Accent = GoldBase;                       // acciones principales (degradado oro)
    public static readonly Color AccentHover = GoldHi;
    public static readonly Color AccentInk = GoldInk;
    public static readonly Color AccentSoft = GoldHi;                     // avisos
    public static readonly Color Blue = GoldLo;                           // barras del gráfico (días pasados)
    public static readonly Color Rose = Champ;                            // tesoros
    public static readonly Color Coral = Color.FromArgb(226, 112, 92);    // apagado, borrar

    public static Color Gold => GoldBase;
    public static Color Green => Mint;
    public static Color Red => Coral;

    /// <summary>Mezcla dos colores (t = 0 → a, 1 → b).</summary>
    public static Color Blend(Color a, Color b, float t) => Color.FromArgb(
        (int)Math.Round(a.R + (b.R - a.R) * t), (int)Math.Round(a.G + (b.G - a.G) * t), (int)Math.Round(a.B + (b.B - a.B) * t));

    // ---- Tipografía ----------------------------------------------------------------------------------------------
    // Segoe UI: la de la página sin la variante «Variable» de Windows 11, que GDI+ (el texto en oro, el gráfico) no
    // dibuja bien y llegaba a colgar la ventana.
    public static Font Text(float size = 9f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);

    /// <summary>Letra de las cifras grandes (como .kpi .v de la página).</summary>
    public static Font Display(float size, FontStyle style = FontStyle.Bold) => new("Segoe UI", size, style);

    // ---- Pintura compartida ------------------------------------------------------------------------------------

    /// <summary>
    /// Fondo de tarjeta de la página: un velo de oro arriba que se apaga a la mitad, sobre --card, y un borde de oro fino.
    /// <paramref name="hero"/>: la tarjeta principal (resplandor desde la esquina y borde más marcado).
    /// </summary>
    /// <param name="flat">Sin velo ni brillo: un color liso (campos de texto, para que el cuadro de texto no se note).</param>
    public static void PaintCard(Graphics g, RectangleF rect, float radius, Color fill, bool hero = false, Color? edge = null, bool flat = false)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = Shape.Rounded(rect, radius);
        using (var solid = new SolidBrush(fill))
        {
            g.FillPath(solid, path);
        }
        if (flat)
        {
            using var line = new Pen(edge ?? CardEdge, 1f);
            g.DrawPath(line, path);
            return;
        }
        var state = g.Save();
        g.SetClip(path, CombineMode.Intersect);
        if (hero)
        {
            // radial-gradient(420px 220px at 0% 0%, oro .17, transparente 70%)
            using var glow = new GraphicsPath();
            var w = Math.Max(rect.Width * 1.25f, 60f);
            var h = Math.Max(rect.Height * 1.5f, 40f);
            glow.AddEllipse(rect.X - w / 2, rect.Y - h / 2, w, h);
            using var radial = new PathGradientBrush(glow)
            {
                CenterColor = Color.FromArgb(48, GoldBase),
                SurroundColors = new[] { Color.FromArgb(0, GoldBase) },
                FocusScales = new PointF(0.05f, 0.05f),
            };
            g.FillPath(radial, glow);
        }
        else
        {
            // linear-gradient(180deg, oro .05, transparente 55%)
            var veil = new RectangleF(rect.X, rect.Y, rect.Width, Math.Max(2f, rect.Height * 0.55f));
            using var linear = new LinearGradientBrush(new PointF(0, veil.Top - 1), new PointF(0, veil.Bottom), Color.FromArgb(14, GoldBase), Color.FromArgb(0, GoldBase));
            g.FillRectangle(linear, veil);
        }
        // Brillo de 1 px arriba (rgba(255,255,255,.03) inset).
        using (var shine = new Pen(Color.FromArgb(9, 255, 255, 255), 1f))
        {
            g.DrawLine(shine, rect.X + radius, rect.Y + 1.5f, rect.Right - radius, rect.Y + 1.5f);
        }
        g.Restore(state);
        using var pen = new Pen(edge ?? (hero ? GoldLine : CardEdge), 1f);
        g.DrawPath(pen, path);
    }

    /// <summary>Degradado vertical de oro de las cifras grandes y los botones principales.</summary>
    public static LinearGradientBrush GoldBrush(RectangleF r, bool deep = false)
    {
        var brush = new LinearGradientBrush(new PointF(0, r.Top - 1), new PointF(0, r.Bottom + 1), GoldHi, deep ? GoldDeep : GoldBase);
        if (deep)
        {
            brush.InterpolationColors = new ColorBlend
            {
                Colors = new[] { GoldHi, GoldBase, GoldDeep },
                Positions = new[] { 0f, 0.6f, 1f },
            };
        }
        return brush;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    /// <summary>
    /// Barra de título oscura, esquinas redondeadas de Windows 11 y, donde se puede (Windows 11), la barra y el borde
    /// en el negro y oro de la app. En versiones antiguas se ignora.
    /// </summary>
    public static void Chrome(IntPtr hwnd)
    {
        try
        {
            int on = 1;
            DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE
            DwmSetWindowAttribute(hwnd, 19, ref on, sizeof(int)); // nombre anterior de la misma opción
            int round = 2;                                        // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int)); // DWMWA_WINDOW_CORNER_PREFERENCE
            int caption = ColorRef(Color.FromArgb(11, 11, 13));
            DwmSetWindowAttribute(hwnd, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
            int border = ColorRef(GoldLine);
            DwmSetWindowAttribute(hwnd, 34, ref border, sizeof(int)); // DWMWA_BORDER_COLOR
            int text = ColorRef(Champ);
            DwmSetWindowAttribute(hwnd, 36, ref text, sizeof(int)); // DWMWA_TEXT_COLOR
        }
        catch
        {
            // sin soporte: la ventana queda con el estilo normal
        }
    }

    private static int ColorRef(Color c) => c.R | (c.G << 8) | (c.B << 16);

    /// <summary>Botón plano con el estilo de la aplicación (usado en ajustes).</summary>
    public static void Style(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Panel;
        b.ForeColor = Ink;
        b.Font = Text(9f);
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderColor = Line;
        b.FlatAppearance.MouseOverBackColor = PanelHover;
        b.FlatAppearance.MouseDownBackColor = Line;
    }
}
