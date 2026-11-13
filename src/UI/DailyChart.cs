using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Gráfico de barras de los cofres de cada día de la semana, con el estilo de la página: barras de oro (hoy más clara
/// y con brillo), la media de los días jugados como línea discontinua y los días debajo (hoy en oro).
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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        if (_days.Count == 0)
        {
            return;
        }

        // La fuente del control se escala con la ventana; la negrita es solo para las cifras.
        using var fontValue = new Font(Font, FontStyle.Bold);
        using var fontDay = new Font(Font, FontStyle.Regular);
        using var fontAvg = new Font(Font.FontFamily, Font.Size * 0.85f, FontStyle.Bold);

        // Las franjas de texto y las barras salen del alto de la fuente: con medidas fijas, al maximizar las cifras se cortaban.
        var lineH = fontDay.GetHeight(g.DpiY);
        var valueH = fontValue.GetHeight(g.DpiY) + 2f;
        var dayH = lineH;
        var max = _days.Max(d => d.Chests);
        var slot = (float)Width / _days.Count;
        var barWidth = Math.Min(slot - 8f, lineH * 1.9f);
        var radius = barWidth / 4.5f;
        var baseline = Height - dayH - 5f;                 // línea base de las barras
        var maxBar = Math.Max(0f, baseline - valueH - 4f); // altura máxima de una barra
        using var brushEmpty = new SolidBrush(Theme.Line);
        using var brushValue = new SolidBrush(Theme.Ink);
        using var brushMuted = new SolidBrush(Theme.Faint);
        using var brushToday = new SolidBrush(Theme.GoldBase);
        using var centerFar = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };
        using var centerNear = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };

        // Línea base de toda la semana (como las líneas guía de la página).
        using (var guide = new Pen(Theme.LineSoft, 1f))
        {
            g.DrawLine(guide, 0, baseline + 0.5f, Width, baseline + 0.5f);
        }

        // Semana de lunes a domingo: hoy en oro claro; los días que aún no han llegado, solo la línea base y sin cifra.
        for (var i = 0; i < _days.Count; i++)
        {
            var d = _days[i];
            var isToday = d.Day == DateTime.Now.Date;
            var x = i * slot;
            var cx = x + slot / 2f;
            var h = max > 0 ? d.Chests / (float)max * maxBar : 0f;

            if (d.Chests > 0)
            {
                h = Math.Max(h, 5f);
                var bar = new RectangleF(cx - barWidth / 2f, baseline - h, barWidth, h);
                using var path = Shape.Rounded(bar, Math.Min(radius, h / 2f));
                using var fill = new LinearGradientBrush(new PointF(0, bar.Top - 1), new PointF(0, bar.Bottom + 1),
                    isToday ? Theme.GoldHi : Theme.GoldBase, isToday ? Theme.GoldBase : Theme.GoldLo);
                if (isToday)
                {
                    // Brillo como el de la línea del gráfico de la página.
                    using var glowPath = Shape.Rounded(RectangleF.Inflate(bar, 3f, 3f), radius + 3f);
                    using var glow = new SolidBrush(Color.FromArgb(34, Theme.GoldBase));
                    g.FillPath(glow, glowPath);
                }
                g.FillPath(fill, path);
            }
            else
            {
                g.FillRectangle(brushEmpty, cx - barWidth / 2f, baseline - 2f, barWidth, 2f);
            }

            // Cifra encima de cada barra (no en los días que aún no han llegado).
            if (!d.Future)
            {
                g.DrawString(L.N(d.Chests), fontValue, d.Chests > 0 ? brushValue : brushMuted,
                    new RectangleF(x, baseline - h - valueH - 2f, slot, valueH), centerFar);
            }

            // Día abreviado debajo (lun, mar…): hoy en oro.
            var name = d.Day.ToString("ddd", L.Culture);
            if (name.Length > 3)
            {
                name = name[..3];
            }
            using var dayBrush = new SolidBrush(isToday ? Theme.GoldBase : Theme.Muted);
            using var dayFont = isToday ? new Font(fontDay, FontStyle.Bold) : (Font)fontDay.Clone();
            g.DrawString(name, dayFont, dayBrush, new RectangleF(x, baseline + 4f, slot, dayH), centerNear);
        }

        // Media de los días jugados: línea discontinua de oro claro con su cifra (como en la página).
        var played = _days.Where(d => d.Chests > 0).ToList();
        if (played.Count >= 2 && max > 0)
        {
            var avg = played.Average(d => d.Chests);
            var y = baseline - (float)(avg / max) * maxBar;
            using var dash = new Pen(Color.FromArgb(115, Theme.GoldHi), 1f) { DashPattern = new[] { 3f, 3f } };
            g.DrawLine(dash, 0, y, Width, y);
            var label = L.F("media {0}", "avg {0}", L.N((int)Math.Round(avg)));
            var size = g.MeasureString(label, fontAvg);
            using var labelBrush = new SolidBrush(Theme.GoldHi);
            g.DrawString(label, fontAvg, labelBrush, Width - size.Width, y - size.Height);
        }
    }
}
