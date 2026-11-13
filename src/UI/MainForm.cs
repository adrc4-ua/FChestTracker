using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Ventana principal, con el estilo de la página de estadísticas (negro y oro):
/// <list type="bullet">
/// <item>cabecera con el logo y el perfil;</item>
/// <item>tarjeta principal: cofres abiertos (con los de hoy) y la partida actual o la última;</item>
/// <item>fila de cifras con su icono (partidas, media, récord, mejor día, tesoros);</item>
/// <item>gráfico de la semana y, a su lado, los botones como el menú de la página.</item>
/// </list>
/// Todo lo demás (escaneo, overlay, actualizaciones) lo hace <see cref="AppMonitor"/> en segundo plano.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MainForm : ThemedForm
{
    private readonly AppMonitor _monitor;
    private readonly Store _store;
    private readonly PillButton _badge;
    private readonly PillButton _profileName;
    private readonly ComboBox _profile;
    private readonly Label _total;
    private readonly Label _today;
    private readonly Label _current;
    private readonly Label _currentCap;
    private readonly Label _currentUnit;
    private readonly Label _lastDetail;
    private readonly Label _statMatches, _statAverage, _statBestMatch, _statBestDay, _statTreasures;
    private readonly Label _statMatchesSub, _statBestDaySub;
    private readonly Card _noticeCard;
    private readonly Label _notice;
    private readonly PillButton _update;
    private readonly PillButton _overlayButton, _reportButton, _settingsButton;
    private readonly DailyChart _daily;
    private readonly Label _weekMeta;
    private readonly Card _teamCard;
    private readonly SectionTitle _teamTitle;
    private readonly Label _teamTotal, _teamMessage;
    private readonly Label[] _teamNames = new Label[TeamColumns], _teamValues = new Label[TeamColumns];
    private const int TeamColumns = 4; // tú + hasta 3 compañeros (escuadrón)
    private MonitorState? _last;
    private string? _profileSig;
    private bool _ignoreProfileChange;
    private bool _autoCloseOffer;

    // Disposición base (592 de ancho): márgenes de 16 y una rejilla de 560.
    private const int X0 = 16, FullW = 560, Right = X0 + FullW;
    private const int HeroY = 68, HeroH = 106;
    private const int KpiY = 184, KpiH = 78, KpiW = 106, KpiGap = 7;
    private const int RowY = 272, RowH = 106, NavW = 180, NavH = 32, NavGap = 5;
    private const int WeekW = FullW - NavW - 10;

    public MainForm(AppMonitor monitor, Store store)
    {
        _monitor = monitor;
        _store = store;

        Text = AppInfo.Name;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(440, 300);
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(DesignW, DesignHBase);
        Icon = AppIcon.Load();

        // ---- Cabecera: logo y nombre (como la barra lateral de la página), distintivo y perfil -------------------
        Controls.Add(new LogoBox { Location = new Point(X0 - 2, 12), Size = new Size(40, 40), GlowAlpha = 60 });
        Lbl(this, "F O R T N I T E", 6.75f, FontStyle.Bold, Theme.GoldBase, new Point(X0 + 42, 15), new Size(120, 13));
        var brand = Lbl(this, "Chest Tracker", 12.5f, FontStyle.Bold, Theme.GoldBase, new Point(X0 + 41, 27), new Size(170, 24), display: true);
        ((FitLabel)brand).Gold = true;

        _profileName = new PillButton
        {
            Location = new Point(Right - 190, 17), Size = new Size(190, 30), Radius = 15, IconSvg = Icons.Person,
            Font = Theme.Text(8.75f, FontStyle.Bold), BackColor = Theme.Card, HoverColor = Theme.Card, Cursor = Cursors.Default, TabStop = false,
        };
        Controls.Add(_profileName);
        _profile = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Card2,
            ForeColor = Theme.Ink,
            Font = Theme.Text(9f),
            Location = new Point(Right - 190, 20),
            Size = new Size(190, 24),
            Visible = false,
        };
        _profile.DrawItem += DrawProfileItem;
        _profile.SelectedIndexChanged += (_, _) =>
        {
            if (!_ignoreProfileChange && _profile.SelectedItem is ProfileItem item)
            {
                _monitor.SetProfile(item.Key);
            }
        };
        Controls.Add(_profile);
        _badge = new PillButton
        {
            Location = new Point(Right - 190 - 8 - 112, 17), Size = new Size(112, 30), Radius = 15, Indicator = Theme.Mint,
            Font = Theme.Text(8f, FontStyle.Bold), ForeColor = Theme.Mint, BackColor = Theme.Card, HoverColor = Theme.Card,
            Cursor = Cursors.Default, TabStop = false, Visible = false,
        };
        Controls.Add(_badge);

        // Línea bajo la cabecera (la de .ph en la página).
        Controls.Add(new Panel { BackColor = Theme.Line, Location = new Point(X0, 58), Size = new Size(FullW, 1) });

        // ---- Tarjeta principal: cofres abiertos y la partida actual o la última -----------------------------------
        var hero = new Card { Location = new Point(X0, HeroY), Size = new Size(FullW, HeroH), Hero = true };
        Controls.Add(hero);
        hero.Controls.Add(new LogoBox { Location = new Point(14, 15), Size = new Size(76, 76), GlowAlpha = 80 });
        Lbl(hero, L.T("Cofres abiertos", "Chests opened"), 9.25f, FontStyle.Bold, Theme.Ink, new Point(100, 16), new Size(196, 18));
        _total = Lbl(hero, "0", 27f, FontStyle.Bold, Theme.GoldBase, new Point(97, 32), new Size(200, 48), display: true);
        ((FitLabel)_total).Gold = true;
        ((FitLabel)_total).Deep = true;
        _today = Lbl(hero, "", 8.25f, FontStyle.Regular, Theme.Muted, new Point(100, 80), new Size(196, 16));

        hero.Controls.Add(new Panel { BackColor = Theme.Line, Location = new Point(310, 18), Size = new Size(1, HeroH - 36) });
        _currentCap = Lbl(hero, L.T("Última partida", "Last match"), 9.25f, FontStyle.Bold, Theme.Ink, new Point(328, 16), new Size(170, 18));
        hero.Controls.Add(new IconTile { IconSvg = Icons.Matches, Location = new Point(FullW - 14 - 34, 14), Size = new Size(34, 34) });
        _current = Lbl(hero, "—", 27f, FontStyle.Bold, Theme.GoldBase, new Point(325, 32), new Size(92, 48), display: true);
        ((FitLabel)_current).Gold = true;
        _currentUnit = Lbl(hero, L.T("cofres", "chests"), 9f, FontStyle.Bold, Theme.Champ, new Point(418, 55), new Size(100, 18));
        _lastDetail = Lbl(hero, "", 8.25f, FontStyle.Regular, Theme.Muted, new Point(328, 80), new Size(FullW - 328 - 14, 16));

        // ---- Cifras (las de la página, cada una con su icono) ------------------------------------------------------
        _statMatches = Kpi(0, L.T("Partidas", "Matches"), Icons.Matches, out _statMatchesSub);
        _statAverage = Kpi(1, L.T("Media", "Average"), Icons.Average, out var averageSub);
        averageSub.Text = L.T("Cofres por partida", "Chests per match");
        _statBestMatch = Kpi(2, L.T("Récord", "Record"), Icons.Record, out var recordSub);
        recordSub.Text = L.T("Cofres en una partida", "Chests in one match");
        _statBestDay = Kpi(3, L.T("Mejor día", "Best day"), Icons.Day, out _statBestDaySub);
        _statTreasures = Kpi(4, L.T("Tesoros", "Treasures"), Icons.Treasure, out var treasuresSub);
        treasuresSub.Text = L.T("Conseguidos", "Obtained");

        // ---- Esta semana -------------------------------------------------------------------------------------------
        var week = new Card { Location = new Point(X0, RowY), Size = new Size(WeekW, RowH) };
        Controls.Add(week);
        week.Controls.Add(new SectionTitle { Text = L.T("Esta semana", "This week"), Location = new Point(12, 7), Size = new Size(170, 20) });
        _weekMeta = Lbl(week, "", 7.75f, FontStyle.Regular, Theme.Muted, new Point(WeekW - 14 - 170, 9), new Size(170, 16), align: ContentAlignment.MiddleRight);
        _daily = new DailyChart { Location = new Point(10, 29), Size = new Size(WeekW - 20, RowH - 34), Font = Theme.Text(8f) };
        week.Controls.Add(_daily);

        // ---- Botones: como el menú de la página (Estadísticas resaltado) ------------------------------------------
        var navX = Right - NavW;
        _reportButton = Nav(L.T("Estadísticas", "Stats"), Icons.Summary, navX, RowY);
        _reportButton.BackColor = Theme.Accent;
        _reportButton.HoverColor = Theme.AccentHover;
        _reportButton.Click += (_, _) => Open(_monitor.ReportTarget);
        _overlayButton = Nav(L.T("Overlay", "Overlay"), Icons.Overlay, navX, RowY + NavH + NavGap);
        _overlayButton.Click += (_, _) => _monitor.ToggleOverlay();
        _settingsButton = Nav(L.T("Configuración", "Settings"), Icons.Settings, navX, RowY + 2 * (NavH + NavGap));
        _settingsButton.Click += (_, _) => OpenSettings();

        // ---- Equipo (con «Compañeros» activado): tus cofres y los de cada compañero en la partida actual o la última.
        // Solo se ve con la opción activada; entonces la ventana crece para hacerle sitio.
        _teamCard = new Card { Location = new Point(X0, TeamY), Size = new Size(FullW, TeamH), Visible = false };
        _teamTitle = new SectionTitle { Text = L.T("Equipo", "Team"), Location = new Point(12, 7), Size = new Size(260, 20) };
        _teamCard.Controls.Add(_teamTitle);
        _teamTotal = Lbl(_teamCard, "", 7.75f, FontStyle.Regular, Theme.Muted, new Point(FullW - 14 - 300, 9), new Size(300, 16), align: ContentAlignment.MiddleRight);
        var colW = (FullW - 28) / TeamColumns;
        for (var i = 0; i < TeamColumns; i++)
        {
            _teamNames[i] = Lbl(_teamCard, "", 7.75f, FontStyle.Regular, Theme.Muted, new Point(14 + i * colW, 32), new Size(colW - 12, 14));
            _teamValues[i] = Lbl(_teamCard, "", 14f, FontStyle.Bold, Theme.Ink, new Point(12 + i * colW, 46), new Size(colW - 12, 26), display: true);
        }
        ((FitLabel)_teamValues[0]).Gold = true;
        _teamMessage = Lbl(_teamCard, "", 8.25f, FontStyle.Regular, Theme.Muted, new Point(14 + colW, 32), new Size(FullW - 28 - colW, 40), align: ContentAlignment.MiddleLeft);
        Controls.Add(_teamCard);

        // ---- Aviso (versión nueva, estimado, carga de replays) o botón de actualizar: franja propia debajo de todo,
        // a lo ancho de la ventana (que crece para hacerle sitio), así los botones siguen siempre a la vista.
        _noticeCard = new Card { Location = new Point(X0, BannerYBase), Size = new Size(FullW, BannerH), Radius = 14, Edge = Theme.GoldLine, Visible = false };
        _noticeCard.Controls.Add(new IconTile { IconSvg = Icons.Info, Location = new Point(8, 7), Size = new Size(26, 26) });
        _notice = Lbl(_noticeCard, "", 8.5f, FontStyle.Regular, Theme.GoldHi, new Point(42, 0), new Size(FullW - 42 - 14, BannerH), align: ContentAlignment.MiddleLeft);
        _noticeCard.Click += (_, _) => { if (_autoCloseOffer) _monitor.KeepOpen(); };
        _notice.Click += (_, _) => { if (_autoCloseOffer) _monitor.KeepOpen(); };
        Controls.Add(_noticeCard);

        _update = new PillButton
        {
            Location = new Point(X0, BannerYBase), Size = new Size(FullW, BannerH), Radius = 14, Visible = false, IconSvg = Icons.Download,
            BackColor = Theme.Accent, HoverColor = Theme.AccentHover, Font = Theme.Text(9.25f, FontStyle.Bold),
        };
        _update.Click += (_, _) => _monitor.InstallUpdate();
        Controls.Add(_update);

        // ---- Icono en el área de notificación (la flecha hacia arriba de la barra de tareas): el programa sigue ahí
        // aunque la ventana esté oculta. Clic: abre la ventana; clic derecho: menú.
        var trayMenu = new ContextMenuStrip { Renderer = new DarkMenuRenderer(), ForeColor = Theme.Ink, Font = Theme.Text(9f) };
        var openItem = new ToolStripMenuItem(L.F("Abrir {0}", "Open {0}", AppInfo.ShortName), null, (_, _) => ShowFromTray());
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        trayMenu.Items.Add(openItem);
        trayMenu.Items.Add(new ToolStripMenuItem(L.T("Overlay", "Overlay"), null, (_, _) => _monitor.ToggleOverlay()));
        trayMenu.Items.Add(new ToolStripMenuItem(L.T("Estadísticas", "Stats"), null, (_, _) => Open(_monitor.ReportTarget)));
        trayMenu.Items.Add(new ToolStripMenuItem(L.T("Configuración", "Settings"), null, (_, _) => { ShowFromTray(); OpenSettings(); }));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(new ToolStripMenuItem(L.T("Salir", "Exit"), null, (_, _) => ExitApp()));
        foreach (ToolStripItem item in trayMenu.Items)
        {
            item.ForeColor = Theme.Ink;
        }
        _tray = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = AppInfo.Name,
            ContextMenuStrip = trayMenu,
            Visible = true,
        };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowFromTray(); };

        // Diseño base: al redimensionar o maximizar, todos los controles y fuentes escalan proporcionalmente.
        CaptureLayout();
        _scaleTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _scaleTimer.Tick += (_, _) => { _scaleTimer.Stop(); ApplyLayoutScale(); };

        monitor.Changed += OnChanged;
        // Oculta (área de notificación) = en segundo plano: al cerrarse Fortnite, el contador se cierra solo.
        VisibleChanged += (_, _) => _monitor.InBackground = !Visible;
    }

    private bool _exiting;

    /// <summary>Cierra la app de verdad (menú «Salir», X del overlay, Fortnite cerrado, actualización).</summary>
    internal void ExitApp()
    {
        _exiting = true;
        Close();
    }

    /// <summary>
    /// X de la ventana: con Fortnite abierto, la app pasa a segundo plano (sigue en el área de notificación, como
    /// Spotify) y se cierra sola al cerrar Fortnite; sin Fortnite, se cierra con normalidad.
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exiting && e.CloseReason == CloseReason.UserClosing && OperatingSystem.IsWindows() && FortniteWatch.IsFortniteRunning())
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    private readonly NotifyIcon _tray;

    /// <summary>
    /// Arranque solo con overlay: la ventana no se muestra (ni delante ni en la barra de tareas); el programa sigue
    /// funcionando y se recupera desde su icono del área de notificación o abriéndolo otra vez. Lo decide Program:
    /// solo con Fortnite abierto y no en el primer uso.
    /// </summary>
    internal bool StartInBackground;
    private bool _startHandled;

    protected override void SetVisibleCore(bool value)
    {
        if (value && StartInBackground && !_startHandled)
        {
            // Application.Run la haría visible: se crea la ventana (para recibir los datos) pero queda oculta.
            _startHandled = true;
            if (!IsHandleCreated)
            {
                CreateHandle();
            }
            base.SetVisibleCore(false);
            _monitor.InBackground = true;
            return;
        }
        base.SetVisibleCore(value);
    }

    /// <summary>Muestra la ventana delante (desde el icono, el menú o al abrir el programa otra vez).</summary>
    internal void ShowFromTray()
    {
        _startHandled = true;
        if (!Visible)
        {
            Show();
        }
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        Activate();
        BringToFront();
    }

    private bool _settingsOpen;

    private void OpenSettings()
    {
        if (_settingsOpen)
        {
            return;
        }
        _settingsOpen = true;
        try
        {
            using var dialog = new SettingsForm(_monitor, _store);
            dialog.ShowDialog(this);
        }
        finally
        {
            _settingsOpen = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Sin esto el icono se queda en la bandeja hasta pasar el ratón por encima.
        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosed(e);
    }

    private const int DesignW = 592, DesignHBase = RowY + RowH + 16;
    // Tarjeta «Equipo» bajo la fila del gráfico; franja del aviso bajo el gráfico o bajo «Equipo» si se ve.
    private const int TeamY = RowY + RowH + 10, TeamH = 78, DesignHTeam = TeamY + TeamH + 16;
    private const int BannerH = 40, BannerYBase = RowY + RowH + 10, BannerYTeam = TeamY + TeamH + 10, BannerGap = 16;
    private int _designH = DesignHBase;
    private readonly Dictionary<Control, LayoutEntry> _layout = new();
    private System.Windows.Forms.Timer? _scaleTimer;
    private float _lastFx = 1, _lastFy = 1;

    private sealed class LayoutEntry
    {
        public Point Location;
        public Size Size;
        public Font? Font;
        public Font? Scaled;
        public int Radius; // esquinas de tarjetas y botones (0 = no tiene)
    }

    private void CaptureLayout()
    {
        Capture(this);
        void Capture(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _layout[c] = new LayoutEntry { Location = c.Location, Size = c.Size, Font = c.Font, Radius = c is Card card ? card.Radius : c is PillButton b ? b.Radius : 0 };
                Capture(c);
            }
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
        {
            return;
        }
        _scaleTimer?.Stop();
        _scaleTimer?.Start();
    }

    /// <summary>Escala posiciones, tamaños y fuentes desde el diseño base. Las fuentes van en puntos: se dividen por el DPI para no contarlas dos veces.</summary>
    private void ApplyLayoutScale()
    {
        if (_layout.Count == 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }
        var fx = ClientSize.Width / (float)DesignW;
        var fy = ClientSize.Height / (float)_designH;
        if (Math.Abs(fx - _lastFx) < 0.005 && Math.Abs(fy - _lastFy) < 0.005)
        {
            return;
        }
        _lastFx = fx;
        _lastFy = fy;
        // Durante restaurar/maximizar el tamaño puede ser transitorio: sin factor válido no se tocan las fuentes.
        var k = Math.Min(fx / (DeviceDpi / 96f), fy / (DeviceDpi / 96f));
        var scaleFonts = float.IsFinite(k) && k > 0.2 && k < 10;
        SuspendLayout();
        foreach (var (c, l) in _layout)
        {
            c.Location = new Point((int)Math.Round(l.Location.X * fx), (int)Math.Round(l.Location.Y * fy));
            c.Size = new Size((int)Math.Round(l.Size.Width * fx), (int)Math.Round(l.Size.Height * fy));
            // Las esquinas siguen la escala (si no, al agrandar la ventana quedarían más cuadradas).
            var radius = Math.Max(4, (int)Math.Round(l.Radius * Math.Min(fx, fy)));
            if (c is Card card)
            {
                card.Radius = radius;
            }
            else if (c is PillButton button)
            {
                button.Radius = radius;
            }
            if (scaleFonts && l.Font != null)
            {
                // Misma letra que ya tiene: nada. (WinForms no cambia una fuente por otra igual, así que liberar la
                // anterior dejaba al control con una fuente liberada: «Parameter is not valid» al pintar.)
                var nf = new Font(l.Font.FontFamily, l.Font.SizeInPoints * k, l.Font.Style);
                if (l.Scaled != null && l.Scaled.Equals(nf))
                {
                    nf.Dispose();
                }
                else
                {
                    var old = l.Scaled;
                    c.Font = nf;
                    l.Scaled = nf;
                    old?.Dispose();
                }
            }
        }
        ResumeLayout();
        Invalidate(true);
    }

    private void OnChanged(MonitorState state)
    {
        _last = state;
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }
        try
        {
            BeginInvoke(new Action(() => Render(state)));
        }
        catch (ObjectDisposedException)
        {
            // la ventana se está cerrando
        }
        catch (InvalidOperationException)
        {
            // todavía sin ventana
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_last != null)
        {
            Render(_last);
        }
    }

    private void Render(MonitorState s)
    {
        var v = s.View;
        var st = s.Stats;
        _total.Text = v.Total;

        // Partida actual (en directo) o la última: la cifra grande y, debajo, sus detalles.
        var live = v.CurrentLive && v.Current != "…";
        _current.Text = v.Current ?? "—";
        _currentCap.Text = live ? L.T("Partida actual", "Current match") : L.T("Última partida", "Last match");
        _currentCap.ForeColor = live ? Theme.Mint : Theme.Ink;
        _currentUnit.Text = v.Current == null ? "" : v.Current == "1" ? L.T("cofre", "chest") : L.T("cofres", "chests");
        PlaceUnit();
        _badge.Text = (v.Badge ?? "").TrimStart('●', ' ');
        _badge.Visible = !string.IsNullOrEmpty(v.Badge);

        // Línea de hoy: la primera fila del modelo (cofres y, detrás, partidas y media de hoy).
        var today = v.Rows.FirstOrDefault();
        var todayChests = today != null && int.TryParse(today.Value, System.Globalization.NumberStyles.None, L.Culture, out var n)
            ? L.Count(n, "cofre", "cofres", "chest", "chests")
            : today?.Value;
        _today.Text = today == null ? "" : $"{L.T("Hoy", "Today")}: {todayChests}" + (today.Detail != null ? $"  ·  {today.Detail}" : "");

        if (live)
        {
            _lastDetail.Text = L.T("En directo: se confirma al terminar la partida", "Live: confirmed when the match ends");
        }
        else if (st.LastMatch is { } lm)
        {
            var parts = new List<string>();
            if (lm.Placement is { } place) parts.Add(lm.Players is { } players ? $"{L.Ordinal(place)} {L.T("de", "of")} {players}" : L.Ordinal(place));
            if (lm.Eliminations is { } elim) parts.Add(L.F("{0} elim.", "{0} elim.", elim));
            if (lm.Treasures > 0) parts.Add(L.Count(lm.Treasures, "tesoro", "tesoros", "treasure", "treasures"));
            if (lm.Destroyed > 0) parts.Add(L.F("{0} destruido(s)", "{0} destroyed", lm.Destroyed));
            _lastDetail.Text = string.Join("  ·  ", parts);
        }
        else
        {
            _lastDetail.Text = "";
        }

        _statMatches.Text = L.N(st.HistoryMatches);
        _statMatchesSub.Text = st.HistorySince is { } since ? L.F("Desde {0:dd/MM/yy}", "Since {0:dd/MM/yy}", since) : L.T("Analizadas", "Analyzed");
        _statAverage.Text = st.Matches > 0 ? st.Average.ToString("0.0", L.Culture) : "—";
        _statBestMatch.Text = st.Matches > 0 ? L.N(st.BestMatch) : "—";
        _statBestDay.Text = st.BestDay != null ? L.N(st.BestDayChests) : "—";
        _statBestDaySub.Text = st.BestDay is { } bd ? bd.ToString("d MMM", L.Culture) : L.T("Sin partidas aún", "No matches yet");
        _statTreasures.Text = L.N(st.Treasures);

        _overlayButton.Enabled = _monitor.OverlayEnabled;
        _overlayButton.Chip = _monitor.OverlayVisible ? L.T("● Activado", "● On") : L.T("Apagado", "Off");
        _overlayButton.ChipOn = _monitor.OverlayVisible;

        _daily.SetDays(st.LastDays);
        var weekTotal = st.LastDays.Sum(d => d.Chests);
        _weekMeta.Text = weekTotal > 0 ? L.Count(weekTotal, "cofre", "cofres", "chest", "chests") : "";
        RenderTeam(s.Team);

        // Perfil: nombre fijo con una sola cuenta, selector con varias.
        var signature = string.Join("|", s.Profiles.Select(p => p.Key));
        var several = s.Profiles.Count >= 2;
        _profile.Visible = several;
        var currentProfile = s.Profiles.FirstOrDefault(p => p.Key == s.CurrentKey);
        _profileName.Visible = !several && currentProfile != null;
        _profileName.Text = currentProfile?.Name ?? "";
        if (signature != _profileSig)
        {
            _profileSig = signature;
            _ignoreProfileChange = true;
            _profile.Items.Clear();
            foreach (var p in s.Profiles)
            {
                _profile.Items.Add(new ProfileItem(p.Key, Profiles.Describe(p)));
            }
            _ignoreProfileChange = false;
        }
        _ignoreProfileChange = true;
        for (var i = 0; i < _profile.Items.Count; i++)
        {
            if (_profile.Items[i] is ProfileItem item && item.Key == s.CurrentKey)
            {
                _profile.SelectedIndex = i;
                break;
            }
        }
        _ignoreProfileChange = false;

        // Aviso o actualización: el aviso (descarga, error, carga de replays) tiene prioridad sobre el botón.
        _autoCloseOffer = s.AutoCloseOffer;
        var hasNotice = !string.IsNullOrEmpty(s.Notice);
        _update.Visible = !hasNotice && s.Update != null;
        if (s.Update != null)
        {
            _update.Text = L.T("Instalar la actualización", "Install the update");
        }
        _notice.Text = s.Notice ?? "";
        _notice.Cursor = s.AutoCloseOffer ? Cursors.Hand : Cursors.Default;
        _noticeCard.Visible = hasNotice;

        Relayout();
    }

    /// <summary>«cofres» justo detrás de la cifra grande de la partida (la cifra cambia de ancho).</summary>
    private void PlaceUnit()
    {
        using var g = _current.CreateGraphics();
        var w = TextRenderer.MeasureText(g, _current.Text, _current.Font, _current.Size, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        var x = _current.Left + Math.Min(w, _current.Width) + (int)Math.Round(8 * (_current.Width / 92f));
        _currentUnit.Left = x;
        if (_layout.TryGetValue(_currentUnit, out var entry) && _lastFx > 0)
        {
            entry.Location = new Point((int)Math.Round(x / _lastFx), entry.Location.Y);
        }
    }

    /// <summary>
    /// Tarjeta «Equipo»: tus cofres y los abiertos de cada compañero (un mínimo: el replay solo tiene lo que pasa cerca
    /// de ti). Sin datos de equipo (opción desactivada) la tarjeta se oculta y la ventana vuelve a su tamaño.
    /// </summary>
    private void RenderTeam(TeamSummary? team)
    {
        SetTeamVisible(team != null);
        if (team == null)
        {
            return;
        }
        _teamTitle.Text = team.Live ? L.T("Equipo · partida actual", "Team · current match") : L.T("Equipo · última partida", "Team · last match");
        _teamTitle.ForeColor = team.Live ? Theme.Mint : Theme.Ink;
        _teamNames[0].Text = L.T("Tú", "You");
        _teamValues[0].Text = L.N(team.Mine);

        var message = team.Unknown
            ? L.T("Partida analizada antes de guardar compañeros.", "Match analyzed before teammates were saved.")
            : team.Mates.Count == 0 ? L.T("Partida en solitario: sin compañeros.", "Solo match: no teammates.") : null;
        _teamMessage.Text = message ?? "";
        _teamMessage.Visible = message != null;
        _teamTotal.Text = team.Mates.Count == 0 ? ""
            : team.Total > 0
                ? L.F("Equipo ≥ {0} · compañeros ≥ {1} %", "Team ≥ {0} · teammates ≥ {1} %", L.N(team.Total), (100.0 * team.MatesTotal / team.Total).ToString("0", L.Culture))
                : L.F("Equipo ≥ {0}", "Team ≥ {0}", L.N(team.Total));
        for (var i = 1; i < TeamColumns; i++)
        {
            var mate = message == null && i - 1 < team.Mates.Count ? team.Mates[i - 1] : null;
            _teamNames[i].Text = mate?.Name ?? "";
            _teamValues[i].Text = mate != null ? "≥ " + L.N(mate.Chests) : "";
            _teamNames[i].Visible = _teamValues[i].Visible = message == null;
        }
    }

    /// <summary>Muestra u oculta la tarjeta «Equipo»; la ventana crece o encoge con ella, a la misma escala.</summary>
    private void SetTeamVisible(bool on)
    {
        if (_teamCard.Visible == on)
        {
            return;
        }
        _teamCard.Visible = on;
        Relayout();
    }

    /// <summary>Altura de la ventana según lo que se ve (tarjeta «Equipo», franja del aviso), a la misma escala.</summary>
    private void Relayout()
    {
        var team = _teamCard.Visible;
        var banner = _noticeCard.Visible || _update.Visible;
        var bannerY = team ? BannerYTeam : BannerYBase;
        var designH = banner ? bannerY + BannerH + BannerGap : team ? DesignHTeam : DesignHBase;
        foreach (var c in new Control[] { _noticeCard, _update })
        {
            if (_layout.TryGetValue(c, out var entry))
            {
                entry.Location = new Point(entry.Location.X, bannerY);
            }
        }
        if (designH == _designH)
        {
            return; // nada cambia de sitio (se llama en cada refresco)
        }
        var scale = ClientSize.Height / (float)_designH;
        _designH = designH;
        _lastFx = _lastFy = -1; // vuelve a colocar todo con la nueva altura de diseño
        if (WindowState == FormWindowState.Normal)
        {
            ClientSize = new Size(ClientSize.Width, (int)Math.Round(_designH * scale));
        }
        ApplyLayoutScale();
    }

    /// <summary>
    /// Cifra de la fila (como .kpi de la página): título arriba, cuadro del icono a la derecha, cifra en oro y una
    /// línea debajo. Devuelve la etiqueta de la cifra; <paramref name="sub"/>, la de la línea de debajo.
    /// </summary>
    private Label Kpi(int index, string caption, string icon, out Label sub)
    {
        var card = new Card { Location = new Point(X0 + index * (KpiW + KpiGap), KpiY), Size = new Size(KpiW, KpiH), Radius = 16 };
        Controls.Add(card);
        Lbl(card, caption, 8.25f, FontStyle.Bold, Theme.Muted, new Point(11, 11), new Size(KpiW - 11 - 36, 15));
        card.Controls.Add(new IconTile { IconSvg = icon, Location = new Point(KpiW - 9 - 26, 8), Size = new Size(26, 26) });
        // La cifra empieza bajo el cuadro del icono (si lo pisara, lo taparía al pintarse).
        var value = Lbl(card, "—", 15.5f, FontStyle.Bold, Theme.GoldBase, new Point(8, 35), new Size(KpiW - 16, 25), display: true);
        ((FitLabel)value).Gold = true;
        sub = Lbl(card, "", 7.25f, FontStyle.Regular, Theme.Muted, new Point(11, 59), new Size(KpiW - 18, 14));
        return value;
    }

    /// <summary>Botón como las entradas del menú de la página: icono y texto a la izquierda.</summary>
    private PillButton Nav(string text, string icon, int x, int y)
    {
        var b = new PillButton
        {
            Text = text, IconSvg = icon, LeftAligned = true, Location = new Point(x, y), Size = new Size(NavW, NavH), Radius = 11,
            Font = Theme.Text(9f, FontStyle.Bold), BackColor = Theme.Card, HoverColor = Theme.Card2,
        };
        Controls.Add(b);
        return b;
    }

    private static Label Lbl(Control parent, string text, float size, FontStyle style, Color color, Point at, Size sz,
        ContentAlignment align = ContentAlignment.TopLeft, bool display = false)
    {
        var l = new FitLabel
        {
            Text = text,
            Font = display ? Theme.Display(size, style) : Theme.Text(size, style),
            ForeColor = color,
            BackColor = Color.Transparent,
            Location = at,
            Size = sz,
            TextAlign = align,
            UseMnemonic = false,
        };
        parent.Controls.Add(l);
        return l;
    }

    private void DrawProfileItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }
        var selected = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
        using var back = new SolidBrush(selected ? Theme.GoldSoft : Theme.Card2);
        e.Graphics.FillRectangle(back, e.Bounds);
        TextRenderer.DrawText(e.Graphics, _profile.Items[e.Index]?.ToString(), _profile.Font, e.Bounds, selected ? Theme.GoldHi : Theme.Ink,
            TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private static void Open(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch
        {
            // las estadísticas aún no existen o no hay navegador: nada que hacer
        }
    }

    private sealed record ProfileItem(string Key, string Text)
    {
        public override string ToString() => Text;
    }
}
