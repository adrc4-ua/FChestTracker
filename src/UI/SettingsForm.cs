using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace FCT.UI;

/// <summary>
/// Ajustes con el mismo aspecto que la ventana principal: dos columnas de tarjetas, títulos pequeños en gris y
/// cifras grandes de color. Cada cambio se aplica y se guarda al instante.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SettingsForm : Form
{
    private static readonly string[] UpdateLabels = { L.T("Preguntar", "Ask"), L.T("Automáticas", "Automatic"), L.T("Nunca", "Never") };
    private static readonly string[] UpdateModes = { "prompt", "auto", "off" };

    // Misma rejilla que la ventana principal: columna izquierda y derecha.
    private const int LeftX = 15, LeftW = 267, RightX = 297, RightW = 280;
    private const int MaxInitial = 1_000_000;

    private readonly AppMonitor _monitor;
    private readonly ToggleSwitch _fortnite;
    private readonly ToggleSwitch _report;
    private readonly PillButton[] _updates = new PillButton[UpdateModes.Length];
    private readonly TextBox _folder;
    private readonly TextBox _initial;
    private bool _loading;

    public SettingsForm(AppMonitor monitor, Store store)
    {
        _monitor = monitor;
        var config = monitor.Config;

        Text = L.T("Configuración", "Settings");
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(592, 548);
        Icon = AppIcon.Load();

        // Cabecera, como en la ventana principal.
        Lbl(this, L.T("CONFIGURACIÓN", "SETTINGS"), 8.5f, FontStyle.Bold, Theme.Cyan, new Point(LeftX, 14), new Size(200, 18));
        AddLanguagePicker(config.Language, new Point(RightX, 9));

        // ---- Columna izquierda ---------------------------------------------------------------

        // Opciones: dos interruptores.
        var start = AddCard(LeftX, 40, LeftW, 170, 16);
        CardTitle(start, L.T("Opciones", "Options"));
        _fortnite = AddToggleRow(start, L.T("Abrir con Fortnite", "Open with Fortnite"),
            L.T("Cuenta tus partidas sin pensar en ello", "Counts your matches on its own"), 24, config.StartWithFortnite);
        _fortnite.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (!_monitor.SetStartWithFortnite(_fortnite.Checked))
            {
                _loading = true;
                _fortnite.Checked = !_fortnite.Checked;
                _loading = false;
                MessageBox.Show(this,
                    L.F("No se puede desde aquí: abre {0} desde su archivo .exe.", "Not possible from here: open {0} from its .exe file.", AppInfo.ShortName),
                    L.T("Configuración", "Settings"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
        _report = AddToggleRow(start, L.T("Abrir el resumen al iniciar", "Open the summary at startup"), null, 64, config.OpenReportOnStart);
        _report.CheckedChanged += (_, _) => { if (!_loading) _monitor.Change(c => c.OpenReportOnStart = _report.Checked); };
        var mates = AddToggleRow(start, L.T("Compañeros en la ventana", "Teammates in the window"),
            L.T("Sus cofres: hasta 1.087 m de ti", "Their chests: up to 1,087 m from you"), 94, config.ShowTeammates);
        mates.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (mates.Checked && !InfoDialog.Confirm(this, L.T("Compañeros en la ventana", "Teammates in the window"),
                    L.T("La ventana mostrará tu equipo y los cofres de cada compañero. El resumen tiene su propia configuración.",
                        "The window will show your team and each teammate's chests. The summary has its own settings."),
                    new[] { ExactPoint(), ApproxPoint() },
                    L.T("Activar", "Turn on"), L.T("Cancelar", "Cancel")))
            {
                Revert(mates);
                return;
            }
            _monitor.SetShowTeammates(mates.Checked);
        };
        var addMates = AddToggleRow(start, L.T("Sumar sus cofres a tu total", "Add their chests to your total"), null, 134, config.CountTeammateChests);
        addMates.CheckedChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }
            if (addMates.Checked && !InfoDialog.Confirm(this, L.T("Sumar sus cofres a tu total", "Add their chests to your total"),
                    L.T("Tu total, tu media, tus récords y el overlay incluirán también los cofres de tus compañeros.",
                        "Your total, your average, your records and the overlay will also include your teammates' chests."),
                    new[]
                    {
                        ApproxPoint(),
                        new InfoPoint("↺", Theme.Cyan, L.T("Tu historial no cambia", "Your history does not change"),
                            L.T("Al desactivarlo vuelves a ver solo tus cofres.", "Turning it off shows only your chests again.")),
                    },
                    L.T("Activar", "Turn on"), L.T("Cancelar", "Cancel")))
            {
                Revert(addMates);
                return;
            }
            _monitor.SetCountTeammates(addMates.Checked);
        };

        // Contar también como cofre: cajas de munición, de comida y cañones de botín (los cofres rotos cuentan siempre).
        var extras = AddCard(LeftX, 219, LeftW, 116, 16);
        CardTitle(extras, L.T("Contar también como cofre", "Also count as a chest"));
        var ammo = AddToggleRow(extras, L.T("Cajas de munición", "Ammo boxes"), null, 26, config.CountAmmoBoxes);
        ammo.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountAmmoBoxes = ammo.Checked); };
        var food = AddToggleRow(extras, L.T("Cajas de comida", "Food boxes"), null, 56, config.CountFoodBoxes);
        food.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountFoodBoxes = food.Checked); };
        var cannons = AddToggleRow(extras, L.T("Cañones de botín", "Loot cannons"), null, 86, config.CountLootCannons);
        cannons.CheckedChanged += (_, _) => { if (!_loading) _monitor.SetCountExtra(c => c.CountLootCannons = cannons.Checked); };

        // Actualizaciones: tres píldoras.
        var updates = AddCard(RightX, 169, RightW, 94, 14);
        CardTitle(updates, L.T("Actualizaciones", "Updates"));
        var pillW = (RightW - 26 - 2 * 6) / 3;
        for (var i = 0; i < UpdateModes.Length; i++)
        {
            var index = i;
            var pill = new PillButton { Text = UpdateLabels[i], Location = new Point(13 + i * (pillW + 6), 41), Size = new Size(pillW, 32), Radius = 10, Font = Theme.Text(8.5f, FontStyle.Bold) };
            pill.Click += (_, _) => SelectUpdateMode(index, save: true);
            updates.Controls.Add(pill);
            _updates[i] = pill;
        }
        var mode = Array.IndexOf(UpdateModes, config.UpdateMode);
        SelectUpdateMode(mode < 0 ? 0 : mode, save: false);

        // Carpeta de replays y datos.
        var folderCard = AddCard(LeftX, 344, LeftW, 94, 14);
        CardTitle(folderCard, L.T("Carpeta de replays", "Replays folder"));
        _folder = AddField(folderCard, new Point(13, 26), LeftW - 26 - 72, config.DemosFolder);
        var browse = new PillButton { Text = L.T("Cambiar", "Change"), Location = new Point(LeftW - 13 - 66, 26), Size = new Size(66, 30), Radius = 10, Font = Theme.Text(8.5f, FontStyle.Bold) };
        browse.Click += (_, _) => ChooseFolder();
        folderCard.Controls.Add(browse);
        Lbl(folderCard, L.T("Tus datos se guardan en ", "Your data is saved in ") + store.Folder,
            7.5f, FontStyle.Regular, Theme.Muted, new Point(13, 62), new Size(LeftW - 26, 28));

        // ---- Columna derecha -----------------------------------------------------------------

        // Cofres de antes del contador: cifra grande en ámbar, editable, con − / +.
        var initial = AddCard(RightX, 40, RightW, 120, 16);
        CardTitle(initial, L.F("Cofres antes de {0}", "Chests before {0}", AppInfo.ShortName));
        _initial = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Card,
            ForeColor = Theme.Amber,
            Font = Theme.Text(24f, FontStyle.Bold),
            Location = new Point(15, 36),
            Width = 160,
            MaxLength = 7,
            Text = Math.Min(MaxInitial, monitor.InitialChests).ToString(L.Culture),
        };
        initial.Controls.Add(_initial);
        var plus = new PillButton { Text = "+", Location = new Point(RightW - 13 - 34, 32), Size = new Size(34, 28), Radius = 9, Font = Theme.Text(11f, FontStyle.Bold) };
        var minus = new PillButton { Text = "−", Location = new Point(RightW - 13 - 34, 64), Size = new Size(34, 28), Radius = 9, Font = Theme.Text(11f, FontStyle.Bold) };
        plus.Click += (_, _) => StepInitial(10);
        minus.Click += (_, _) => StepInitial(-10);
        initial.Controls.Add(plus);
        initial.Controls.Add(minus);
        _initial.KeyPress += (_, e) => e.Handled = !char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar);
        _initial.TextChanged += (_, _) =>
        {
            if (!_loading)
            {
                _monitor.SetInitialChests(ParseInitial());
            }
        };
        var profileName = monitor.ShownProfileName;
        Lbl(initial, profileName != null
                ? L.F("estimado · se suma al total de {0}", "estimate · added to {0}'s total", profileName)
                : L.T("estimado · se suma a tu total", "estimate · added to your total"),
            8.0f, FontStyle.Regular, Theme.Muted, new Point(16, 88), new Size(RightW - 70, 16));

        // OBS: dirección y estado.
        var obsCard = AddCard(RightX, 272, RightW, 94, 14);
        CardTitle(obsCard, L.T("Overlay para OBS", "Overlay for OBS"));
        var obsUrl = ObsServer.Url;
        var stateCard = new Card { Location = new Point(RightW - 13 - 70, 6), Size = new Size(70, 18), Radius = 9, BackColor = Theme.Panel };
        Lbl(stateCard, obsUrl != null ? L.T("● ACTIVO", "● ON") : L.T("● INACTIVO", "● OFF"), 7.0f, FontStyle.Bold,
            obsUrl != null ? Theme.Mint : Theme.Red, new Point(0, 0), new Size(70, 18), ContentAlignment.MiddleCenter)
            .BackColor = Color.Transparent; // la píldora redondeada se ve por detrás
        obsCard.Controls.Add(stateCard);
        var obs = AddField(obsCard, new Point(13, 41), RightW - 26 - 72, obsUrl ?? L.T("Puerto ocupado", "Port in use"));
        var copy = new PillButton { Text = L.T("Copiar", "Copy"), Location = new Point(RightW - 13 - 66, 41), Size = new Size(66, 30), Radius = 10, Font = Theme.Text(8.5f, FontStyle.Bold), Enabled = obsUrl != null };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(obsUrl!);
                copy.Text = "✓";
            }
            catch
            {
                obs.Focus();
                obs.SelectAll();
            }
        };
        obsCard.Controls.Add(copy);

        // Atajos del overlay.
        var keys = AddCard(RightX, 375, RightW, 55, 12);
        Lbl(keys, L.T("Atajos del overlay", "Overlay shortcuts"), 7.5f, FontStyle.Regular, Theme.Muted, new Point(13, 7), new Size(RightW - 26, 17));
        Lbl(keys, L.T("Ctrl+Mayús+O mostrar  ·  Ctrl+Mayús+L fijar", "Ctrl+Shift+O show  ·  Ctrl+Shift+L lock"),
            8.0f, FontStyle.Bold, Theme.Ink, new Point(13, 27), new Size(RightW - 26, 18));

        // Restablecer estadísticas: borra el historial (con confirmación y copia de seguridad).
        var reset = new PillButton
        {
            Text = L.T("Restablecer estadísticas…", "Reset statistics…"),
            Location = new Point(LeftX, 447),
            Size = new Size(LeftW, 32),
            Radius = 10,
            Font = Theme.Text(8.5f, FontStyle.Bold),
            ForeColor = Theme.Red,
        };
        reset.Click += (_, _) => ResetStats();
        Controls.Add(reset);

        // Cerrar, en el sitio de los botones de la ventana principal.
        var close = new PillButton
        {
            Text = L.T("Cerrar", "Close"),
            Location = new Point(RightX, 439),
            Size = new Size(RightW, 40),
            Radius = 12,
            BackColor = Theme.Accent,
            HoverColor = Theme.AccentHover,
            ForeColor = Theme.AccentInk,
            DialogResult = DialogResult.OK,
        };
        Controls.Add(close);

        // Pie: enlace al repositorio del proyecto.
        var footer = AddCard(LeftX, 490, RightX + RightW - LeftX, 44, 12);
        AddSourceLink(footer, L.T("Código fuente", "Source code"), "GitHub", AppInfo.RepoUrl);

        FitTexts(this);
        AcceptButton = close;
        CancelButton = close;
        ActiveControl = close; // sin anillo de foco en el primer interruptor al abrir
    }

    /// <summary>
    /// Selector de idioma: «Sistema» (el de Windows, por defecto) o uno de los idiomas, con su nombre en ese idioma.
    /// El cambio se guarda al momento y se aplica al reiniciar el contador (se ofrece hacerlo ya).
    /// </summary>
    private void AddLanguagePicker(string? current, Point at)
    {
        var chosen = current != null && L.Languages.Any(l => l.Code == current) ? current : null;
        string NameOf(string? code) => code == null
            ? L.F("Sistema ({0})", "System ({0})", L.Languages.FirstOrDefault(l => l.Code == L.SystemCode).Name)
            : L.Languages.First(l => l.Code == code).Name;

        // Botón píldora como los de la ventana; al pulsarlo, un menú oscuro con los idiomas.
        var button = new PillButton
        {
            Text = NameOf(chosen) + "  ▾",
            Location = at,
            Size = new Size(172, 26),
            Radius = 9,
            Font = Theme.Text(8.5f, FontStyle.Bold),
        };
        var menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            BackColor = Theme.Panel,
            Font = Theme.Text(9f),
            Padding = new Padding(4),
        };
        void AddItem(string? code)
        {
            var item = new ToolStripMenuItem((code == chosen ? "✓   " : "      ") + NameOf(code))
            {
                ForeColor = code == chosen ? Theme.Cyan : Theme.Ink,
                Padding = new Padding(4, 3, 18, 3),
            };
            item.Click += (_, _) => Choose(code);
            menu.Items.Add(item);
        }
        AddItem(null);
        menu.Items.Add(new ToolStripSeparator());
        foreach (var (code, _) in L.Languages)
        {
            AddItem(code);
        }
        button.ContextMenuStrip = menu; // también con clic derecho
        button.Click += (_, _) => menu.Show(button, new Point(0, button.Height + 4));
        Controls.Add(button);

        void Choose(string? code)
        {
            if (code == chosen)
            {
                return;
            }
            chosen = code;
            button.Text = NameOf(code) + "  ▾";
            foreach (var item in menu.Items.OfType<ToolStripMenuItem>())
            {
                var mine = item.Text?.TrimStart('✓', ' ') == NameOf(code);
                item.Text = (mine ? "✓   " : "      ") + item.Text?.TrimStart('✓', ' ');
                item.ForeColor = mine ? Theme.Cyan : Theme.Ink;
            }
            _monitor.Change(c => c.Language = code);
            if (InfoDialog.Confirm(this, L.T("Idioma", "Language"),
                    L.F("El nuevo idioma se aplica al reiniciar {0}. Tu historial y tus ajustes se conservan.",
                        "The new language applies when {0} restarts. Your history and settings are kept.", AppInfo.ShortName),
                    Array.Empty<InfoPoint>(),
                    L.T("Reiniciar ahora", "Restart now"), L.T("Más tarde", "Later")))
            {
                Close();
                _monitor.Restart();
            }
        }
    }

    /// <summary>
    /// Textos que no caben en su sitio (idiomas largos): la letra se reduce un poco, hasta 6,5 pt, en vez de cortarse.
    /// Solo afecta a etiquetas de una línea y a botones.
    /// </summary>
    private static void FitTexts(Control parent)
    {
        foreach (Control c in parent.Controls)
        {
            FitTexts(c);
            if (c is not (Label { AutoEllipsis: true } or PillButton) || string.IsNullOrEmpty(c.Text))
            {
                continue;
            }
            var font = c.Font;
            if (c is Label && c.Height > font.Height * 1.8)
            {
                continue; // varias líneas: ya tiene sitio para partirse
            }
            var room = c.Width - (c is PillButton ? 12 : 2);
            while (font.Size > 6.5f && TextRenderer.MeasureText(c.Text, font).Width > room)
            {
                font = new Font(font.FontFamily, font.Size - 0.25f, font.Style);
            }
            if (!ReferenceEquals(font, c.Font))
            {
                c.Font = font;
            }
        }
    }

    /// <summary>Lo que sí es exacto de los compañeros.</summary>
    private static InfoPoint ExactPoint() => new("✓", Theme.Mint, L.T("Exacto", "Exact"),
        L.T("Con quién juegas, partidas juntos y victorias.", "Who you play with, matches together and wins."));

    /// <summary>Lo que no: sus cofres, con la distancia medida en replays reales.</summary>
    private static InfoPoint ApproxPoint() => new("≈", Theme.Amber, L.T("Aproximado: sus cofres", "Approximate: their chests"),
        L.Big("teammatesRange",
            "Fortnite solo graba lo que pasa a menos de 1.087 m de ti. Si un compañero se aleja más, sus cofres no se cuentan: son un mínimo, no su total.",
            "Fortnite only records what happens within 1,087 m of you. If a teammate goes further, their chests are not counted: they are a minimum, not their total."));

    /// <summary>Vuelve a poner el interruptor como estaba (se canceló el diálogo), sin guardar nada.</summary>
    private void Revert(ToggleSwitch toggle)
    {
        _loading = true;
        toggle.Checked = !toggle.Checked;
        _loading = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    private void SelectUpdateMode(int index, bool save)
    {
        for (var i = 0; i < _updates.Length; i++)
        {
            var on = i == index;
            _updates[i].BackColor = on ? Theme.Accent : Theme.Panel;
            _updates[i].HoverColor = on ? Theme.AccentHover : Theme.PanelHover;
            _updates[i].ForeColor = on ? Theme.AccentInk : Theme.Ink;
            _updates[i].Invalidate();
        }
        if (save)
        {
            var chosen = UpdateModes[index];
            _monitor.Change(c => c.UpdateMode = chosen);
        }
    }

    private int ParseInitial() =>
        int.TryParse(_initial.Text, System.Globalization.NumberStyles.None, L.Culture, out var n) ? Math.Clamp(n, 0, MaxInitial) : 0;

    private void StepInitial(int delta)
    {
        var value = Math.Clamp(ParseInitial() + delta, 0, MaxInitial);
        _initial.Text = value.ToString(L.Culture); // TextChanged guarda el valor
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = L.T("Carpeta de replays de Fortnite", "Fortnite replays folder"),
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_folder.Text) ? _folder.Text : "",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _monitor.ChangeDemosFolder(dialog.SelectedPath);
            _folder.Text = dialog.SelectedPath;
        }
    }

    // ---- Piezas de la ventana ----------------------------------------------------------------

    private Card AddCard(int x, int y, int w, int h, int radius)
    {
        var card = new Card { Location = new Point(x, y), Size = new Size(w, h), Radius = radius, BackColor = Theme.Card };
        Controls.Add(card);
        return card;
    }

    /// <summary>Pie de Ajustes: título gris y el enlace (abre la URL en el navegador).</summary>
    private static void AddSourceLink(Card footer, string title, string webText, string webUrl)
    {
        // Textos medidos y centrados en vertical: así no se tapan entre sí ni se cortan en ningún idioma.
        var titleFont = Theme.Text(8.0f);
        var titleW = TextRenderer.MeasureText(title, titleFont).Width;
        Lbl(footer, title, 8.0f, FontStyle.Regular, Theme.Muted, new Point(13, 2), new Size(titleW, footer.Height - 4), ContentAlignment.MiddleLeft);

        var webFont = Theme.Text(9.0f, FontStyle.Bold);
        var webLabel = webText + "  ↗";
        var web = new LinkLabel
        {
            Text = webLabel,
            Font = webFont,
            LinkColor = Theme.Cyan,
            ActiveLinkColor = Theme.AccentHover,
            VisitedLinkColor = Theme.Cyan,
            LinkBehavior = LinkBehavior.HoverUnderline,
            BackColor = footer.BackColor,
            Location = new Point(13 + titleW + 4, 2),
            Size = new Size(TextRenderer.MeasureText(webLabel, webFont).Width + 4, footer.Height - 4),
            TextAlign = ContentAlignment.MiddleLeft,
            LinkArea = new LinkArea(0, webLabel.Length),
            Cursor = Cursors.Hand,
        };
        web.LinkClicked += (_, _) => OpenUrl(webUrl);
        footer.Controls.Add(web);

        var tips = new ToolTip();
        tips.SetToolTip(web, webUrl);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        }
        catch
        {
            // Sin navegador disponible: no hacemos nada.
        }
    }

    /// <summary>Título pequeño en gris, como "Hoy" o "Mejor día" en la ventana principal.</summary>
    private static void CardTitle(Card card, string text) =>
        Lbl(card, text, 8.0f, FontStyle.Regular, Theme.Muted, new Point(13, 7), new Size(card.Width - 100, 15));

    /// <summary>Texto (y una línea gris opcional) a la izquierda y un interruptor a la derecha; clicar el texto también lo cambia.</summary>
    /// <summary>Pide confirmación y borra todo el historial (se guarda una copia de data.json antes).</summary>
    private void ResetStats()
    {
        var answer = MessageBox.Show(this,
            L.F("Se borrarán todas tus partidas, cofres y tesoros guardados, y los cofres de antes de {0}. {0} empezará de cero con tu próxima partida (los replays que ya tienes no se vuelven a contar).\n\nSe guarda una copia de seguridad en la carpeta de tus datos.\n\n¿Restablecer las estadísticas?",
                "All your saved matches, chests and treasures will be deleted, as well as the chests before {0}. {0} starts from zero with your next match (the replays you already have are not counted again).\n\nA backup copy is saved in your data folder.\n\nReset the statistics?", AppInfo.ShortName),
            L.T("Restablecer estadísticas", "Reset statistics"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
        {
            return;
        }
        var backup = _monitor.ResetStats();
        _loading = true;
        _initial.Text = "0";
        _loading = false;
        MessageBox.Show(this, L.F("Estadísticas restablecidas. Copia de seguridad: {0}", "Statistics reset. Backup copy: {0}", Path.GetFileName(backup)),
            L.T("Restablecer estadísticas", "Reset statistics"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static ToggleSwitch AddToggleRow(Card card, string text, string? hint, int top, bool value)
    {
        // Centrado con lo que tiene a la izquierda: solo el título (una línea) o título y línea gris (dos).
        var toggle = new ToggleSwitch { Location = new Point(card.Width - 13 - 40, hint != null ? top + 5 : top - 2), Checked = value };
        var label = Lbl(card, text, 9.5f, FontStyle.Bold, Theme.Ink, new Point(13, top), new Size(card.Width - 26 - 48, 18));
        label.Cursor = Cursors.Hand;
        label.Click += (_, _) => toggle.Checked = !toggle.Checked;
        if (hint != null)
        {
            var small = Lbl(card, hint, 7.5f, FontStyle.Regular, Theme.Muted, new Point(13, top + 18), new Size(card.Width - 26 - 48, 14));
            small.Cursor = Cursors.Hand;
            small.Click += (_, _) => toggle.Checked = !toggle.Checked;
        }
        card.Controls.Add(toggle);
        return toggle;
    }

    /// <summary>Campo de solo lectura sin borde dentro de un fondo redondeado.</summary>
    private static TextBox AddField(Card card, Point at, int width, string text)
    {
        var holder = new Card { Location = at, Size = new Size(width, 30), Radius = 10, BackColor = Theme.Panel };
        var box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Panel,
            ForeColor = Theme.Ink,
            Font = Theme.Text(9f),
            ReadOnly = true,
            Text = text,
            Width = width - 20,
        };
        box.Location = new Point(10, (holder.Height - box.Height) / 2);
        holder.Controls.Add(box);
        card.Controls.Add(holder);
        return box;
    }

    private static Label Lbl(Control parent, string text, float size, FontStyle style, Color color, Point at, Size sz, ContentAlignment align = ContentAlignment.TopLeft)
    {
        var label = new Label
        {
            Text = text,
            Font = Theme.Text(size, style),
            ForeColor = color,
            BackColor = parent.BackColor,
            Location = at,
            Size = sz,
            TextAlign = align,
            AutoEllipsis = true,
        };
        parent.Controls.Add(label);
        return label;
    }
}
