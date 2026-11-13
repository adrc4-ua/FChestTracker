using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Cómo contar las partidas de antes de FCT: solo las repeticiones guardadas, o repeticiones + un estimado
/// (media de cofres × partidas que FCT aún no tiene). En dos pasos: 1) partidas totales, buscadas en fortnite-api.com
/// (si esta versión lleva la clave) o escritas a mano mirándolas en fortnite.gg; 2) la media, calculada con las
/// partidas que elija el jugador (5 por defecto, 10 recomendado) o escrita a mano. Abajo, el resultado al momento.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class EstimateDialog : ThemedForm
{
    /// <summary>Lo elegido: <see cref="Estimate"/> false = solo repeticiones.</summary>
    public sealed record Choice(bool Estimate, int TotalMatches, string Source, int Sample, double? ManualAverage);

    private const int W = 500, Pad = 20;

    private readonly PillButton _onlyReplays;
    private readonly PillButton _withEstimate;
    private readonly Card _panel;
    private readonly TextBox _name;
    private readonly TextBox _total;
    private readonly Label _status;
    private readonly PillButton _calc;
    private readonly PillButton _typed;
    private readonly Panel _calcArea;
    private readonly Panel _typedArea;
    private readonly List<PillButton> _samples = new();
    private readonly TextBox _average;
    private readonly Label _preview;
    private readonly PillButton _ok;
    private readonly PillButton _cancel;
    private readonly Func<int, int, double?, HistoryEstimator.Result>? _previewOf;
    private readonly string? _accountId;
    private readonly string? _originalName;
    private bool _estimate;
    private bool _manualAverage;
    private int _sample;
    private string _source = "manual";

    private EstimateDialog(string? playerName, string? accountId, HistoryEstimate? current, Func<int, int, double?, HistoryEstimator.Result>? previewOf, bool owned)
    {
        _previewOf = previewOf;
        _accountId = accountId;
        _originalName = playerName;
        Text = L.F("Cofres antes de {0}", "Chests before {0}", AppInfo.ShortName);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = !owned;
        StartPosition = owned ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        Icon = AppIcon.Load();

        var y = Pad;
        y = AddText(this, L.T("¿Contar también tus partidas de antes?", "Count your earlier matches too?"), Theme.Text(12f, FontStyle.Bold), Theme.GoldHi, Pad, y, W - 2 * Pad) + 4;
        y = AddText(this, L.F("{0} cuenta exacto lo que hay en tus repeticiones. Puede sumar además un estimado de las partidas que no tiene: tu media de cofres × las partidas que le faltan. Va solo a tu total, marcado como estimado.",
                "{0} counts exactly what is in your replays. It can also add an estimate of the matches it does not have: your average chests × the matches it is missing. It only goes into your total, marked as an estimate.", AppInfo.ShortName),
            Theme.Text(9f), Theme.Muted, Pad, y, W - 2 * Pad) + 14;

        var half = (W - 2 * Pad - 8) / 2;
        _onlyReplays = Pill(this, L.T("Solo repeticiones guardadas", "Saved replays only"), new Point(Pad, y), new Size(half, 36), 9f);
        _withEstimate = Pill(this, L.T("Repeticiones + estimación", "Replays + estimate"), new Point(Pad + half + 8, y), new Size(half, 36), 9f);
        _onlyReplays.Click += (_, _) => SelectMode(false);
        _withEstimate.Click += (_, _) => SelectMode(true);
        y += 36 + 14;

        _panel = new Card { Location = new Point(Pad, y), Radius = 16, BackColor = Theme.Card };
        var pw = W - 2 * Pad;
        var inner = pw - 28;
        var py = 14;

        // 1 · Partidas de la temporada actual (la estimación solo cubre esta temporada).
        py = AddText(_panel, L.T("1 · Tus partidas de esta temporada", "1 · Your matches this season"), Theme.Text(9.5f, FontStyle.Bold), Theme.Ink, 14, py, inner) + 6;
        var buttons = StatsClient.Available ? 2 : 1;
        var fieldW = inner - buttons * (100 + 8);
        _name = Field(_panel, new Point(14, py), fieldW, playerName ?? "", Theme.Text(9.5f), Theme.Ink);
        SetCue(_name, L.T("Tu nombre de Epic", "Your Epic name"));
        var bx = 14 + fieldW + 8;
        if (StatsClient.Available)
        {
            var search = Pill(_panel, L.T("Buscar", "Look up"), new Point(bx, py), new Size(100, 32), 8.75f);
            search.Click += async (_, _) => await LookUp(search);
            bx += 100 + 8;
        }
        var site = Pill(_panel, "fortnite.gg ↗", new Point(bx, py), new Size(100, 32), 8.75f);
        site.Click += (_, _) => OpenSite();
        py += 32 + 8;
        _total = Field(_panel, new Point(14, py), 150, current is { TotalMatches: > 0 } c ? c.TotalMatches.ToString(CultureInfo.InvariantCulture) : "", Theme.Text(14f, FontStyle.Bold), Theme.Amber, 38);
        _total.MaxLength = 7;
        _total.KeyPress += (_, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
        _total.TextChanged += (_, _) => { _source = "manual"; UpdatePreview(); };
        _status = new Label { Location = new Point(14 + 150 + 12, py), Size = new Size(inner - 150 - 12, 38), Font = Theme.Text(8.5f), ForeColor = Theme.Muted, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft };
        _status.Text = StatsClient.Available
            ? L.T("Pulsa «Buscar» o escribe las partidas de la temporada actual que ves en fortnite.gg.", "Press «Look up» or type the current season's matches shown on fortnite.gg.")
            : L.T("Mira las partidas de la temporada actual en fortnite.gg (botón de arriba) y escríbelas aquí.", "Look up the current season's matches on fortnite.gg (button above) and type them here.");
        _panel.Controls.Add(_status);
        py += 38 + 16;

        // 2 · Media de cofres por partida: calculada con tus partidas, o escrita a mano.
        py = AddText(_panel, L.T("2 · Tu media de cofres por partida", "2 · Your average chests per match"), Theme.Text(9.5f, FontStyle.Bold), Theme.Ink, 14, py, inner) + 6;
        var halfInner = (inner - 8) / 2;
        _calc = Pill(_panel, L.T("Con mis partidas", "From my matches"), new Point(14, py), new Size(halfInner, 30), 8.75f);
        _typed = Pill(_panel, L.T("La escribo yo", "I type it"), new Point(14 + halfInner + 8, py), new Size(halfInner, 30), 8.75f);
        _calc.Click += (_, _) => SelectAverage(false);
        _typed.Click += (_, _) => SelectAverage(true);
        py += 30 + 10;

        _calcArea = new Panel { Location = new Point(14, py), Size = new Size(inner, 98), BackColor = Color.Transparent };
        var ay = AddText(_calcArea, L.T("Partidas nuevas:", "New matches:"), Theme.Text(8.75f), Theme.Muted, 0, 7, 150);
        var sx = 152;
        var sw = (inner - sx - 3 * 6) / HistoryEstimator.SampleChoices.Length;
        foreach (var n in HistoryEstimator.SampleChoices)
        {
            var label = n == HistoryEstimator.RecommendedSample ? L.F("{0} ★", "{0} ★", n) : n.ToString(CultureInfo.InvariantCulture);
            var pill = Pill(_calcArea, label, new Point(sx, 0), new Size(sw, 30), 9f);
            pill.Tag = n;
            pill.Click += (_, _) => SelectSample((int)pill.Tag!);
            _samples.Add(pill);
            sx += sw + 6;
        }
        AddText(_calcArea, L.F("Las que juegues a partir de ahora. Por defecto {0}; recomendado {1} (★): cuantas más, más acertada será la estimación. Tus repeticiones también entran en la media: cuantas más tengas, más peso tienen.",
                "The ones you play from now on. Default {0}; recommended {1} (★): the more, the more accurate the estimate. Your replays also go into the average: the more you have, the more they weigh.",
                HistoryEstimator.DefaultSample, HistoryEstimator.RecommendedSample),
            Theme.Text(8.25f), Theme.Muted, 0, 36, inner);
        _panel.Controls.Add(_calcArea);

        _typedArea = new Panel { Location = new Point(14, py), Size = new Size(inner, 98), BackColor = Color.Transparent, Visible = false };
        _average = Field(_typedArea, new Point(0, 0), 150, current?.ManualAverage is double typed ? typed.ToString("0.##", L.Culture) : "", Theme.Text(14f, FontStyle.Bold), Theme.Amber, 38);
        _average.MaxLength = 6;
        _average.KeyPress += (_, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.';
        _average.TextChanged += (_, _) => UpdatePreview();
        AddText(_typedArea, L.T("cofres por partida (por ejemplo 18,5). Se usa tal cual, sin esperar partidas.", "chests per match (for example 18.5). Used as is, with no waiting."),
            Theme.Text(8.5f), Theme.Muted, 162, 2, inner - 162);
        _panel.Controls.Add(_typedArea);
        py += 98 + 6;

        // 3 · Resultado, al momento.
        _preview = new Label { Location = new Point(14, py), Size = new Size(inner, 76), Font = Theme.Text(9.25f, FontStyle.Bold), ForeColor = Theme.Cyan, BackColor = Color.Transparent };
        _panel.Controls.Add(_preview);
        py += 76 + 10;
        _panel.Size = new Size(pw, py);
        Controls.Add(_panel);

        // Primer uso: «Continuar» y sin «Cancelar» (cerrar la ventana cierra la app, que vuelve a empezar la próxima vez).
        _ok = Pill(this, owned ? L.T("Guardar", "Save") : L.T("Continuar", "Continue"), Point.Empty, new Size(130, 36), 9f);
        _ok.BackColor = Theme.Accent;
        _ok.HoverColor = Theme.AccentHover;
        _ok.ForeColor = Theme.AccentInk;
        _ok.Click += (_, _) => Save();
        _cancel = Pill(this, L.T("Cancelar", "Cancel"), Point.Empty, new Size(130, 36), 9f);
        _cancel.DialogResult = DialogResult.Cancel;
        _cancel.Visible = owned;
        if (owned)
        {
            CancelButton = _cancel;
        }

        _sample = current?.SampleSize is int s && HistoryEstimator.SampleChoices.Contains(s) ? s : HistoryEstimator.DefaultSample;
        SelectSample(_sample);
        SelectAverage(current?.ManualAverage != null);
        SelectMode(current != null);
    }

    /// <summary>Muestra el diálogo. Devuelve lo elegido, o null si se cancela o se cierra la ventana.</summary>
    /// <param name="previewOf">Resultado con (partidas totales, partidas para la media, media escrita); null en el primer uso.</param>
    public static Choice? Ask(IWin32Window? owner, string? playerName, string? accountId, HistoryEstimate? current, Func<int, int, double?, HistoryEstimator.Result>? previewOf)
    {
        using var dialog = new EstimateDialog(playerName, accountId, current, previewOf, owner != null);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Result : null;
    }

    private Choice? Result { get; set; }

    private int Total => int.TryParse(_total.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private double? TypedAverage =>
        double.TryParse(_average.Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var a) && a > 0 && a < 1000 ? a : null;

    private void SelectMode(bool estimate)
    {
        _estimate = estimate;
        Mark(_onlyReplays, !estimate);
        Mark(_withEstimate, estimate);
        _panel.Visible = estimate;
        var bottom = (estimate ? _panel.Bottom : _onlyReplays.Bottom) + 18;
        _ok.Location = new Point(W - Pad - 130, bottom);
        _cancel.Location = new Point(W - Pad - 130 - 8 - 130, bottom);
        ClientSize = new Size(W, bottom + 36 + Pad);
        UpdatePreview();
    }

    private void SelectAverage(bool typed)
    {
        _manualAverage = typed;
        Mark(_calc, !typed);
        Mark(_typed, typed);
        _calcArea.Visible = !typed;
        _typedArea.Visible = typed;
        UpdatePreview();
    }

    private void SelectSample(int n)
    {
        _sample = n;
        foreach (var p in _samples)
        {
            Mark(p, (int)p.Tag! == n);
        }
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (!_estimate)
        {
            _ok.Enabled = true;
            return;
        }
        var total = Total;
        var typed = _manualAverage ? TypedAverage : null;
        _ok.Enabled = total > 0 && (!_manualAverage || typed != null);
        if (total <= 0)
        {
            _preview.Text = L.T("Escribe o busca tus partidas de esta temporada.", "Type or look up your matches this season.");
            return;
        }
        if (_manualAverage && typed == null)
        {
            _preview.Text = L.T("Escribe tu media de cofres por partida.", "Type your average chests per match.");
            return;
        }
        if (_previewOf == null)
        {
            _preview.Text = typed is double a
                ? L.F("Media {0} × las partidas que no tenga {1} → se calcula en cuanto se cuenten tus repeticiones.", "Average {0} × the matches {1} does not have → calculated as soon as your replays are counted.", a.ToString("0.##", L.Culture), AppInfo.ShortName)
                : L.F("Se calculará tras tus {0} próximas partidas, junto con tus repeticiones (cuantas más tengas, más pesan en la media).",
                    "It will be calculated after your next {0} matches, together with your replays (the more you have, the more they weigh).", _sample);
            return;
        }
        var r = _previewOf(total, _sample, typed);
        string Avg(double v) => v.ToString(r.RoundedDown ? "0" : "0.##", L.Culture);
        // Repeticiones de temporadas anteriores: cuentan en la media, pero no se restan de las partidas de esta temporada.
        var season = r.SeasonReplays < r.Replays
            ? " " + L.F("Se restan solo las {0} de esta temporada.", "Only this season's {0} are subtracted.", r.SeasonReplays)
            : "";
        if (r.Chests is int chests)
        {
            var from = typed != null
                ? L.T("escrita por ti", "typed by you")
                : L.F("{0} + {1}{2}", "{0} + {1}{2}", L.Count(r.Replays, "repetición", "repeticiones", "replay", "replays"),
                    L.Count(r.NewMatches, "partida nueva", "partidas nuevas", "new match", "new matches"), r.RoundedDown ? L.T(", redondeada hacia abajo", ", rounded down") : "");
            _preview.Text = L.F("Media de {0} cofres ({1}) → ≈ {2} cofres de antes.", "Average {0} chests ({1}) → ≈ {2} earlier chests.",
                Avg(r.Average ?? 0), from, chests.ToString("#,0", L.Culture)) + season;
            return;
        }
        _preview.Text = r.Provisional is int provisional
            ? L.F("Ahora: media de {0} en {1} → ≈ {2} (provisional). Se fija tras tus próximas {3}, que entran en la media junto a las repeticiones.",
                "Now: average {0} over {1} → ≈ {2} (provisional). It is fixed after your next {3}, which join the replays in the average.",
                Avg(r.Average ?? 0), L.Count(r.Replays, "repetición", "repeticiones", "replay", "replays"), provisional.ToString("#,0", L.Culture),
                L.Count(r.Waiting, "partida", "partidas", "match", "matches")) + season
            : L.F("Sin repeticiones: se calculará tras tus próximas {0}, con la media redondeada hacia abajo.",
                "No replays: it will be calculated after your next {0}, with the average rounded down.", L.Count(r.Waiting, "partida", "partidas", "match", "matches"));
    }

    private async Task LookUp(PillButton button)
    {
        button.Enabled = false;
        _status.ForeColor = Theme.Muted;
        _status.Text = L.T("Buscando…", "Looking up…");
        // Con el nombre sin tocar se busca por la cuenta (no depende de cómo se escriba el nombre).
        var id = string.Equals(_name.Text.Trim(), _originalName, StringComparison.OrdinalIgnoreCase) ? _accountId : null;
        var answer = await StatsClient.TotalMatchesAsync(id, _name.Text);
        button.Enabled = true;
        switch (answer.Outcome)
        {
            case StatsClient.Outcome.Ok:
                _total.Text = answer.Matches.ToString(CultureInfo.InvariantCulture);
                _source = "api";
                _status.ForeColor = Theme.Mint;
                _status.Text = L.F("{0}: {1} esta temporada.", "{0}: {1} this season.", answer.Name ?? _name.Text.Trim(), L.Count(answer.Matches, "partida", "partidas", "match", "matches"));
                break;
            case StatsClient.Outcome.Private:
                _status.ForeColor = Theme.Amber;
                _status.Text = L.T("Sus estadísticas son privadas: no se pueden consultar. Hazlas públicas en Fortnite o escribe el número.",
                    "Their stats are private: they cannot be looked up. Make them public in Fortnite or type the number.");
                break;
            case StatsClient.Outcome.NotFound:
                _status.ForeColor = Theme.Amber;
                _status.Text = L.T("No se ha encontrado ese jugador. Revisa el nombre de Epic.", "That player was not found. Check the Epic name.");
                break;
            default:
                _status.ForeColor = Theme.Amber;
                _status.Text = L.T("No se pudo consultar ahora. Escribe el número a mano.", "It could not be looked up now. Type the number by hand.");
                break;
        }
    }

    private void OpenSite()
    {
        var name = _name.Text.Trim();
        var url = name.Length > 0 ? "https://fortnite.gg/stats?player=" + Uri.EscapeDataString(name) : "https://fortnite.gg/stats";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        }
        catch
        {
            // sin navegador: se puede abrir a mano
        }
    }

    private void Save()
    {
        var typed = _manualAverage ? TypedAverage : null;
        if (_estimate && (Total <= 0 || (_manualAverage && typed == null)))
        {
            return;
        }
        Result = new Choice(_estimate, _estimate ? Total : 0, _source, _sample, typed);
        DialogResult = DialogResult.OK;
    }

    private static void Mark(PillButton b, bool on)
    {
        b.BackColor = on ? Theme.Accent : Theme.Panel;
        b.HoverColor = on ? Theme.AccentHover : Theme.PanelHover;
        b.ForeColor = on ? Theme.AccentInk : Theme.Ink;
        b.Invalidate();
    }

    private static PillButton Pill(Control parent, string text, Point at, Size size, float font)
    {
        var b = new PillButton { Text = text, Location = at, Size = size, Radius = 10, Font = Theme.Text(font, FontStyle.Bold) };
        parent.Controls.Add(b);
        return b;
    }

    private static TextBox Field(Control parent, Point at, int width, string text, Font font, Color color, int height = 32)
    {
        var holder = new Card { Location = at, Size = new Size(width, height), Radius = 10, BackColor = Theme.Bg2, Edge = Theme.Line };
        var box = new TextBox { BorderStyle = BorderStyle.None, BackColor = Theme.Bg2, ForeColor = color, Font = font, Text = text, Width = width - 20 };
        box.Location = new Point(10, (height - box.Height) / 2);
        holder.Controls.Add(box);
        parent.Controls.Add(holder);
        return box;
    }

    /// <summary>Texto gris de ayuda dentro de un campo vacío (EM_SETCUEBANNER).</summary>
    private static void SetCue(TextBox box, string text) =>
        box.HandleCreated += (_, _) => SendMessage(box.Handle, 0x1501, (IntPtr)1, text);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, string lParam);

    private static int AddText(Control parent, string text, Font font, Color color, int x, int y, int width)
    {
        // La etiqueta se mide sola (con el ancho máximo): así nunca corta la última línea.
        var label = new Label { Text = text, Font = font, ForeColor = color, BackColor = Color.Transparent, Location = new Point(x, y), AutoSize = true, MaximumSize = new Size(width, 0) };
        parent.Controls.Add(label);
        return y + label.PreferredHeight + 2;
    }
}
