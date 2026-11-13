using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Cofres de cada día de la semana con el gráfico de la página («Evolución de cofres»): curva dorada suave con su
/// relleno degradado, líneas guía con la escala a la izquierda, la media de los días jugados como línea discontinua,
/// un punto por día (hoy en blanco) y los días debajo («hoy» en oro). Los días que aún no han llegado no tienen punto.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DailyChart : Control
{
    private IReadOnlyList<DayChests> _days = Array.Empty<DayChests>();

    public DailyChart()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Card;
    }

    public void SetDays(IReadOnlyList<DayChests> days)
    {
        _days = days;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e) => Backdrop.Paint(this, e.Graphics);

    /// <summary>Tope «redondo» de la escala (como NiceMax de la página): 1, 2, 2,5 o 5 por una potencia de 10.</summary>
    private static int NiceMax(int value)
    {
        if (value <= 0)
        {
            return 10;
        }
        var power = Math.Pow(10, Math.Floor(Math.Log10(value)));
        foreach (var step in new[] { 1, 2, 2.5, 5, 10 })
        {
            if (step * power >= value)
            {
                return (int)Math.Ceiling(step * power);
            }
        }
        return value;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        if (_days.Count < 2)
        {
            return;
        }

        // Letras: la del control se escala con la ventana.
        using var fontAxis = new Font(Font.FontFamily, Font.Size * 0.88f, FontStyle.Regular);
        using var fontDay = new Font(Font.FontFamily, Font.Size, FontStyle.Regular);
        using var fontToday = new Font(Font.FontFamily, Font.Size, FontStyle.Bold);
        using var fontAvg = new Font(Font.FontFamily, Font.Size * 0.88f, FontStyle.Bold);
        var dayH = fontDay.GetHeight(g.DpiY);

        // Zona del gráfico: a la izquierda las cifras de la escala, abajo los días.
        var max = NiceMax(_days.Max(d => d.Chests));
        var axisW = g.MeasureString(L.N(max), fontAxis).Width + 4f;
        var plot = new RectangleF(axisW + 4f, dayH * 0.6f, Width - axisW - 4f - 8f, Height - dayH * 0.6f - dayH - 7f);
        if (plot.Width <= 10 || plot.Height <= 10)
        {
            return;
        }
        float X(int i) => plot.Left + i * plot.Width / (_days.Count - 1);
        float Y(double v) => plot.Bottom - (float)(v / max) * plot.Height;

        // Líneas guía (0, mitad, tope) con su cifra a la izquierda.
        using (var grid = new Pen(Theme.Line, 1f))
        using (var axisBrush = new SolidBrush(Theme.Faint))
        using (var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
        {
            foreach (var v in new[] { 0, max / 2, max })
            {
                var y = (float)Math.Round(Y(v)) + 0.5f;
                g.DrawLine(grid, plot.Left, y, plot.Right, y);
                g.DrawString(L.N(v), fontAxis, axisBrush, new RectangleF(0, y - dayH, axisW, 2 * dayH), right);
            }
        }

        // Puntos de los días que ya han llegado (los que faltan de la semana no se dibujan).
        var points = _days.Select((d, i) => (d, p: new PointF(X(i), Y(d.Chests)))).Where(x => !x.d.Future).Select(x => x.p).ToList();
        if (points.Count >= 2)
        {
            // Curva suave (Catmull-Rom → Bézier) sin bajar de cero, como en la página.
            using var curve = new GraphicsPath();
            for (var i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[Math.Max(0, i - 1)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Math.Min(points.Count - 1, i + 2)];
                var c1 = new PointF(p1.X + (p2.X - p0.X) / 6f, Math.Min(plot.Bottom, p1.Y + (p2.Y - p0.Y) / 6f));
                var c2 = new PointF(p2.X - (p3.X - p1.X) / 6f, Math.Min(plot.Bottom, p2.Y - (p3.Y - p1.Y) / 6f));
                curve.AddBezier(p1, c1, c2, p2);
            }

            // Relleno: degradado de oro que se apaga hacia abajo.
            using (var area = (GraphicsPath)curve.Clone())
            {
                area.AddLine(points[^1], new PointF(points[^1].X, plot.Bottom));
                area.AddLine(new PointF(points[^1].X, plot.Bottom), new PointF(points[0].X, plot.Bottom));
                area.CloseFigure();
                using var fill = new LinearGradientBrush(new PointF(0, plot.Top - 1), new PointF(0, plot.Bottom + 1), Color.FromArgb(90, Theme.GoldBase), Color.FromArgb(0, Theme.GoldBase));
                g.FillPath(fill, area);
            }
            // Línea con su brillo (drop-shadow dorado de la página).
            using (var glow = new Pen(Color.FromArgb(55, Theme.GoldBase), 6f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawPath(glow, curve);
            }
            using (var line = new Pen(Theme.GoldBase, 2.2f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawPath(line, curve);
            }
        }

        // Media de los días jugados: línea discontinua con su texto encima, a la izquierda.
        var played = _days.Where(d => d.Chests > 0).ToList();
        if (played.Count >= 2)
        {
            var avg = played.Average(d => d.Chests);
            var y = Y(avg);
            using var dash = new Pen(Color.FromArgb(115, Theme.GoldHi), 1f) { DashPattern = new[] { 3f, 3f } };
            g.DrawLine(dash, plot.Left, y, plot.Right, y);
            using var label = new SolidBrush(Theme.GoldHi);
            var text = L.F("media {0}", "avg {0}", L.N((int)Math.Round(avg)));
            var size = g.MeasureString(text, fontAvg);
            g.DrawString(text, fontAvg, label, plot.Left + 4f, Math.Max(0, y - size.Height));
        }

        // Puntos: oro claro con borde del color de la tarjeta; hoy en blanco.
        var r = Math.Max(3f, dayH * 0.28f);
        using (var edge = new Pen(Theme.Card, 2f))
        using (var dot = new SolidBrush(Theme.GoldHi))
        using (var now = new SolidBrush(Color.White))
        using (var valueBrush = new SolidBrush(Theme.Champ))
        {
            for (var i = 0; i < _days.Count; i++)
            {
                var d = _days[i];
                if (d.Future)
                {
                    continue;
                }
                var p = new PointF(X(i), Y(d.Chests));
                var isToday = d.Day == DateTime.Now.Date;
                g.FillEllipse(isToday ? now : dot, p.X - r, p.Y - r, 2 * r, 2 * r);
                g.DrawEllipse(edge, p.X - r, p.Y - r, 2 * r, 2 * r);
                if (d.Chests > 0)
                {
                    // La cifra encima del punto (en la página sale al pasar el ratón; aquí, siempre).
                    var value = L.N(d.Chests);
                    var vs = g.MeasureString(value, fontAvg);
                    var vx = Math.Clamp(p.X - vs.Width / 2f, plot.Left, Width - vs.Width);
                    g.DrawString(value, fontAvg, isToday ? now : valueBrush, vx, Math.Max(0, p.Y - r - vs.Height));
                }
            }
        }

        // Días debajo («hoy» en oro); el primero y el último, pegados a su borde para que no se salgan.
        using var dayBrush = new SolidBrush(Theme.Faint);
        using var todayBrush = new SolidBrush(Theme.GoldBase);
        for (var i = 0; i < _days.Count; i++)
        {
            var d = _days[i];
            var isToday = d.Day == DateTime.Now.Date;
            var name = isToday ? L.T("hoy", "today") : d.Day.ToString("ddd", L.Culture);
            if (!isToday && name.Length > 3)
            {
                name = name[..3];
            }
            var font = isToday ? fontToday : fontDay;
            var w = g.MeasureString(name, font).Width;
            var x = Math.Clamp(X(i) - w / 2f, 0f, Width - w);
            g.DrawString(name, font, isToday ? todayBrush : dayBrush, x, plot.Bottom + 4f);
        }
    }
}
