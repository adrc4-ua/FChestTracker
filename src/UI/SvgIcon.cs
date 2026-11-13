using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace FCT.UI;

/// <summary>
/// Dibuja los iconos SVG de la página de estadísticas (24 × 24, trazo redondeado, relleno suave en los elementos con
/// class="f") con GDI+, para que la app use exactamente los mismos dibujos. Entiende lo que usan esos iconos:
/// path (M L H V C S Q A Z, absolutos y relativos), rect (con rx) y circle.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class SvgIcon
{
    private sealed record Shape(GraphicsPath Path, bool Soft, Color? Fill, bool Stroke);
    private sealed record Parsed(IReadOnlyList<Shape> Shapes, float StrokeWidth);

    private static readonly ConcurrentDictionary<string, Parsed> Cache = new();

    /// <summary>Dibuja el icono en <paramref name="box"/> con el color dado (el «currentColor» del SVG).</summary>
    /// <param name="softAlpha">Opacidad del relleno suave (class="f"): .22 en la página, .35 sobre oro.</param>
    public static void Draw(Graphics g, string svg, RectangleF box, Color color, float softAlpha = 0.22f, float strokeScale = 1f)
    {
        var icon = Cache.GetOrAdd(svg, Parse);
        var scale = Math.Min(box.Width, box.Height) / 24f;
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TranslateTransform(box.X + (box.Width - 24 * scale) / 2, box.Y + (box.Height - 24 * scale) / 2);
        g.ScaleTransform(scale, scale);
        using var pen = new Pen(color, icon.StrokeWidth * strokeScale) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var soft = new SolidBrush(Color.FromArgb((int)(255 * softAlpha), color));
        using var solid = new SolidBrush(color);
        foreach (var s in icon.Shapes)
        {
            if (s.Fill is { } fill)
            {
                if (fill == Color.Empty)
                {
                    g.FillPath(solid, s.Path);
                }
                else
                {
                    using var own = new SolidBrush(fill);
                    g.FillPath(own, s.Path);
                }
            }
            else if (s.Soft)
            {
                g.FillPath(soft, s.Path);
            }
            if (s.Stroke)
            {
                g.DrawPath(pen, s.Path);
            }
        }
        g.Restore(state);
    }

    [GeneratedRegex(@"<(path|rect|circle)\b([^>]*?)/?>", RegexOptions.IgnoreCase)]
    private static partial Regex ElementRx();

    [GeneratedRegex(@"([\w-]+)=""([^""]*)""")]
    private static partial Regex AttrRx();

    [GeneratedRegex(@"stroke-width=""([\d.]+)""")]
    private static partial Regex StrokeWidthRx();

    private static Parsed Parse(string svg)
    {
        var width = StrokeWidthRx().Match(svg) is { Success: true } w ? Num(w.Groups[1].Value) : 1.7f;
        var shapes = new List<Shape>();
        foreach (Match m in ElementRx().Matches(svg))
        {
            var a = AttrRx().Matches(m.Groups[2].Value).ToDictionary(x => x.Groups[1].Value, x => x.Groups[2].Value);
            var path = new GraphicsPath();
            switch (m.Groups[1].Value.ToLowerInvariant())
            {
                case "path":
                    AddPath(path, a.GetValueOrDefault("d", ""));
                    break;
                case "rect":
                    var r = new RectangleF(Num(a.GetValueOrDefault("x", "0")), Num(a.GetValueOrDefault("y", "0")), Num(a.GetValueOrDefault("width", "0")), Num(a.GetValueOrDefault("height", "0")));
                    var rx = a.TryGetValue("rx", out var rxs) ? Num(rxs) : 0f;
                    if (rx > 0)
                    {
                        path.AddPath(FCT.UI.Shape.Rounded(r, rx), false);
                    }
                    else
                    {
                        path.AddRectangle(r);
                    }
                    break;
                case "circle":
                    var cx = Num(a.GetValueOrDefault("cx", "0"));
                    var cy = Num(a.GetValueOrDefault("cy", "0"));
                    var rr = Num(a.GetValueOrDefault("r", "0"));
                    path.AddEllipse(cx - rr, cy - rr, 2 * rr, 2 * rr);
                    break;
            }
            Color? fill = a.TryGetValue("fill", out var f) && f != "none"
                ? f == "currentColor" ? Color.Empty : ColorTranslator.FromHtml(f)
                : null;
            var stroke = !(a.TryGetValue("stroke", out var st) && st == "none");
            shapes.Add(new Shape(path, a.GetValueOrDefault("class") == "f", fill, stroke));
        }
        return new Parsed(shapes, width);
    }

    private static float Num(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"[MmLlHhVvCcSsQqTtAaZz]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")]
    private static partial Regex TokenRx();

    /// <summary>Convierte el atributo «d» de un path SVG en figuras de GDI+.</summary>
    private static void AddPath(GraphicsPath path, string d)
    {
        var tokens = TokenRx().Matches(d).Select(t => t.Value).ToList();
        var i = 0;
        var cmd = 'M';
        PointF cur = default, start = default, lastCtrl = default;
        var lastWasCurve = false;
        bool IsNum() => i < tokens.Count && !char.IsLetter(tokens[i][0]);
        float Next() => Num(tokens[i++]);

        while (i < tokens.Count)
        {
            if (char.IsLetter(tokens[i][0]))
            {
                cmd = tokens[i++][0];
            }
            var rel = char.IsLower(cmd);
            var ox = rel ? cur.X : 0;
            var oy = rel ? cur.Y : 0;
            switch (char.ToUpperInvariant(cmd))
            {
                case 'M':
                    cur = new PointF(ox + Next(), oy + Next());
                    start = cur;
                    path.StartFigure();
                    cmd = rel ? 'l' : 'L'; // los pares siguientes son líneas
                    lastWasCurve = false;
                    break;
                case 'L':
                {
                    var p = new PointF(ox + Next(), oy + Next());
                    path.AddLine(cur, p);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'H':
                {
                    var p = new PointF((rel ? cur.X : 0) + Next(), cur.Y);
                    path.AddLine(cur, p);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'V':
                {
                    var p = new PointF(cur.X, (rel ? cur.Y : 0) + Next());
                    path.AddLine(cur, p);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'C':
                {
                    var c1 = new PointF(ox + Next(), oy + Next());
                    var c2 = new PointF(ox + Next(), oy + Next());
                    var p = new PointF(ox + Next(), oy + Next());
                    path.AddBezier(cur, c1, c2, p);
                    lastCtrl = c2;
                    cur = p;
                    lastWasCurve = true;
                    break;
                }
                case 'S':
                {
                    var c1 = lastWasCurve ? new PointF(2 * cur.X - lastCtrl.X, 2 * cur.Y - lastCtrl.Y) : cur;
                    var c2 = new PointF(ox + Next(), oy + Next());
                    var p = new PointF(ox + Next(), oy + Next());
                    path.AddBezier(cur, c1, c2, p);
                    lastCtrl = c2;
                    cur = p;
                    lastWasCurve = true;
                    break;
                }
                case 'Q':
                {
                    var q = new PointF(ox + Next(), oy + Next());
                    var p = new PointF(ox + Next(), oy + Next());
                    path.AddBezier(cur, Lerp(cur, q, 2f / 3), Lerp(p, q, 2f / 3), p);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'T':
                {
                    var p = new PointF(ox + Next(), oy + Next());
                    path.AddLine(cur, p);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'A':
                {
                    var rx = Next();
                    var ry = Next();
                    var rot = Next();
                    var large = Next() != 0;
                    var sweep = Next() != 0;
                    var p = new PointF(ox + Next(), oy + Next());
                    AddArc(path, cur, p, rx, ry, rot, large, sweep);
                    cur = p;
                    lastWasCurve = false;
                    break;
                }
                case 'Z':
                    path.CloseFigure();
                    cur = start;
                    lastWasCurve = false;
                    break;
                default:
                    i++;
                    break;
            }
            // Números repetidos tras Z o un comando sin números: se saltan para no quedarse en bucle.
            if (char.ToUpperInvariant(cmd) == 'Z' && IsNum())
            {
                i++;
            }
        }
    }

    private static PointF Lerp(PointF a, PointF b, float t) => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    /// <summary>Arco elíptico de SVG (forma de extremos) como curvas de Bézier de 90° como mucho.</summary>
    private static void AddArc(GraphicsPath path, PointF p0, PointF p1, float rx, float ry, float rotDeg, bool large, bool sweep)
    {
        if (rx == 0 || ry == 0 || p0 == p1)
        {
            path.AddLine(p0, p1);
            return;
        }
        double phi = rotDeg * Math.PI / 180, cos = Math.Cos(phi), sin = Math.Sin(phi);
        double dx = (p0.X - p1.X) / 2.0, dy = (p0.Y - p1.Y) / 2.0;
        double x1 = cos * dx + sin * dy, y1 = -sin * dx + cos * dy;
        double rxa = Math.Abs(rx), rya = Math.Abs(ry);
        var lambda = x1 * x1 / (rxa * rxa) + y1 * y1 / (rya * rya);
        if (lambda > 1)
        {
            rxa *= Math.Sqrt(lambda);
            rya *= Math.Sqrt(lambda);
        }
        var num = rxa * rxa * rya * rya - rxa * rxa * y1 * y1 - rya * rya * x1 * x1;
        var den = rxa * rxa * y1 * y1 + rya * rya * x1 * x1;
        var coef = (large == sweep ? -1 : 1) * Math.Sqrt(Math.Max(0, num / den));
        double cxp = coef * rxa * y1 / rya, cyp = -coef * rya * x1 / rxa;
        double cx = cos * cxp - sin * cyp + (p0.X + p1.X) / 2.0, cy = sin * cxp + cos * cyp + (p0.Y + p1.Y) / 2.0;
        static double Angle(double ux, double uy, double vx, double vy)
        {
            var a = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
            return a;
        }
        var t1 = Angle(1, 0, (x1 - cxp) / rxa, (y1 - cyp) / rya);
        var dt = Angle((x1 - cxp) / rxa, (y1 - cyp) / rya, (-x1 - cxp) / rxa, (-y1 - cyp) / rya);
        if (!sweep && dt > 0) dt -= 2 * Math.PI;
        if (sweep && dt < 0) dt += 2 * Math.PI;
        var segments = (int)Math.Ceiling(Math.Abs(dt) / (Math.PI / 2));
        var step = dt / segments;
        var k = 4.0 / 3 * Math.Tan(step / 4);
        PointF Pt(double t) => new((float)(cx + rxa * Math.Cos(t) * cos - rya * Math.Sin(t) * sin), (float)(cy + rxa * Math.Cos(t) * sin + rya * Math.Sin(t) * cos));
        PointF D(double t) => new((float)(-rxa * Math.Sin(t) * cos - rya * Math.Cos(t) * sin), (float)(-rxa * Math.Sin(t) * sin + rya * Math.Cos(t) * cos));
        var t0 = t1;
        var from = p0;
        for (var s = 0; s < segments; s++)
        {
            var ta = t0 + step;
            var to = s == segments - 1 ? p1 : Pt(ta);
            var d0 = D(t0);
            var d1 = D(ta);
            path.AddBezier(from, new PointF((float)(from.X + k * d0.X), (float)(from.Y + k * d0.Y)), new PointF((float)(to.X - k * d1.X), (float)(to.Y - k * d1.Y)), to);
            from = to;
            t0 = ta;
        }
    }
}
