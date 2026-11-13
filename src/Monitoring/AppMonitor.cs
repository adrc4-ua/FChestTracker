using System.Runtime.Versioning;

namespace FCT.Monitoring;

/// <summary>
/// Vigila los replays en segundo plano: escaneo, partida en curso, overlay, actualizaciones y cierre automático.
/// Solo el hilo de vigilancia toca los datos (el escaneo puede tardar minutos); la ventana pide cambios y
/// recibe el estado publicado, así nunca se congela mientras se analizan replays.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AppMonitor
{
    /// <summary>Protege solo la configuración (escrituras cortas desde la ventana, el overlay y el hilo de vigilancia).</summary>
    private readonly object _cfgGate = new();
    private readonly Store _store;
    private readonly Config _config;
    private readonly TrackerData _data;
    private readonly Tracker _tracker;
    private readonly string _dataDir;
    private readonly AutoResetEvent _wake = new(false);
    private FileSystemWatcher? _watcher;
    private volatile bool _stop;
    private volatile bool _publishRequested;
    private volatile bool _reportRequested;
    private volatile string? _shownKey;
    private volatile string? _shownName;
    private DateTime _nextScan = DateTime.UtcNow;
    private volatile UpdateInfo? _update;
    private bool _updateAnnounced;
    private volatile bool _installing;
    private volatile string? _notice;
    private volatile bool _keepOpenRequested;
    private DateTime? _fortniteClosedSince;
    private DateTime? _autoCloseAt;
    private bool _closeRequested;

    public AppMonitor(Store store, Config config, TrackerData data, Tracker tracker, string dataDir)
    {
        _store = store;
        _config = config;
        _data = data;
        _tracker = tracker;
        _dataDir = dataDir;
        // Tanda de replays (primer uso, o muchos a la vez): aviso de carga en la ventana, como en la página de estadísticas.
        _tracker.Progressed = p =>
        {
            _progress = p;
            Publish();
        };
    }

    private volatile ScanProgress? _progress;

    /// <summary>Ajustes actuales (leer; los cambios se hacen con <see cref="Change"/>).</summary>
    public Config Config => _config;

    /// <summary>Abierto por el proceso en espera de Fortnite (arranca en segundo plano, con el overlay).</summary>
    public bool AutoMode { get; set; }

    /// <summary>
    /// La ventana está oculta (en el área de notificación) con Fortnite abierto. Así, al cerrarse Fortnite, el contador
    /// se cierra solo en cuanto cuenta la última partida. Con la ventana a la vista no se cierra solo.
    /// </summary>
    public volatile bool InBackground;

    /// <summary>Se pone a true cuando hay que reiniciar la nueva versión.</summary>
    public bool RestartAfterUpdate { get; private set; }

    /// <summary>Reinicio pedido desde Ajustes (cambio de idioma): la ventana se cierra y el exe vuelve a arrancar.</summary>
    public bool RestartRequested { get; private set; }

    /// <summary>Cierra el contador para volver a abrirlo (los textos se leen al arrancar).</summary>
    public void Restart()
    {
        RestartRequested = true;
        CloseRequested?.Invoke();
    }

    /// <summary>Estado nuevo. Se llama desde el hilo de vigilancia: la ventana debe usar BeginInvoke.</summary>
    public event Action<MonitorState>? Changed;

    /// <summary>Pide cerrar la ventana (Fortnite cerrado, o nueva versión instalada). Desde otro hilo.</summary>
    public event Action? CloseRequested;

    public void Start()
    {
        if (OperatingSystem.IsWindows())
        {
            StartOverlay();
        }
        RestartWatcher();
        ObsServer.ReportFile = _store.ReportPath;
        ObsServer.ReportLang = L.Code;
        ObsServer.Settings = ApplyFromReport;
        ObsServer.Start(_config.ObsPort > 0 ? _config.ObsPort : ObsServer.DefaultPort);
        _gameLog = new GameLogWatcher(OnTreasureScreen);

        new Thread(Loop) { IsBackground = true, Name = "Vigilancia" }.Start();

    }

    // Nueva versión en GitHub: única conexión de red de la herramienta (se puede desactivar en Configuración). Se mira
    // al arrancar y luego cada 30 minutos mientras siga abierta, así una release nueva llega aunque no se cierre nunca.
    private DateTime _nextUpdateCheck = DateTime.UtcNow;
    private volatile bool _checkingUpdate;

    private void PollUpdates()
    {
        if (_config.UpdateMode == "off" || _update != null || _checkingUpdate || DateTime.UtcNow < _nextUpdateCheck)
        {
            return;
        }
        _checkingUpdate = true;
        _nextUpdateCheck = DateTime.UtcNow.AddMinutes(30);
        _ = Task.Run(async () =>
        {
            try
            {
                if (await Updater.CheckAsync() is { } found)
                {
                    _update = found;
                    RequestPublish();
                }
            }
            finally
            {
                _checkingUpdate = false;
            }
        });
    }

    public void Stop()
    {
        ObsServer.Stop();
        _stop = true;
        _wake.Set();
        _watcher?.Dispose();
        _gameLog?.Dispose();
    }

    // ---- Pantalla de tesoros de fin de partida --------------------------------------------------------------

    private GameLogWatcher? _gameLog;
    private readonly System.Collections.Concurrent.ConcurrentQueue<TreasureScreen> _screens = new();
    private const string GameProcess = "FortniteClient-Win64-Shipping";

    /// <summary>
    /// El juego acaba de mostrar la pantalla de tesoros: se captura solo su ventana (aunque esté detrás de otras) unas
    /// veces mientras las fichas terminan de aparecer, y se queda la lectura con más fichas. La imagen solo vive en
    /// memoria mientras se lee. Si la ventana está minimizada o no se puede capturar, la partida queda como hasta ahora.
    /// </summary>
    private void OnTreasureScreen(DateTime shownUtc)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                List<string>? best = null;
                foreach (var wait in new[] { 1200, 1200, 1200 })
                {
                    await Task.Delay(wait);
                    var hwnd = GameWindow();
                    var frame = await WindowCapture.CaptureAsync(hwnd, TimeSpan.FromSeconds(2));
                    if (frame == null)
                    {
                        continue;
                    }
                    var cards = TreasureScreenReader.Read(frame).Select(c => c.Item).ToList();
                    frame = null; // the image is dropped right after reading it
                    if (best == null || cards.Count > best.Count)
                    {
                        best = cards;
                    }
                }
                if (best is { Count: > 0 })
                {
                    _screens.Enqueue(new TreasureScreen { ShownUtc = shownUtc, Cards = best });
                    _wake.Set();
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Pantalla de tesoros", ex);
            }
        });
    }

    /// <summary>Ventana del juego. FCT_TREASURE_WINDOW (título exacto de otra ventana) sirve para probar sin Fortnite.</summary>
    private static IntPtr GameWindow() => Environment.GetEnvironmentVariable("FCT_TREASURE_WINDOW") is { Length: > 0 } title
        ? PostMatchTools.FindWindowByTitle(title)
        : System.Diagnostics.Process.GetProcessesByName(GameProcess).Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);

    /// <summary>Guarda las pantallas leídas y las aplica a su partida (si ya está analizada). True si cambió algo.</summary>
    private bool ApplyTreasureScreens()
    {
        var added = false;
        while (_screens.TryDequeue(out var screen))
        {
            if (_data.TreasureScreens.All(s => Math.Abs((s.ShownUtc - screen.ShownUtc).TotalSeconds) > 30))
            {
                _data.TreasureScreens.Add(screen);
                added = true;
            }
        }
        if (!added)
        {
            return false;
        }
        foreach (var m in _data.Matches)
        {
            TreasureScreenMerge.Apply(m, _data.TreasureScreens);
        }
        try { _store.SaveData(_data); } catch (Exception ex) { ErrorLog.Write("Guardar pantalla de tesoros", ex); }
        _reportRequested = true;
        return true;
    }

    /// <summary>
    /// Mensaje de estado de la ventana (por ejemplo, el reanálisis tras una actualización). Ocupa el sitio de los
    /// botones, así que se quita solo pasados unos segundos.
    /// </summary>
    public void SetNotice(string? text)
    {
        _notice = text;
        _noticeUntil = DateTime.UtcNow.AddSeconds(NoticeSeconds);
        RequestPublish();
    }

    private const int NoticeSeconds = 20;
    private DateTime _noticeUntil;

    /// <summary>Quita el aviso cuando caduca (los botones vuelven a verse). True si cambió algo.</summary>
    private bool ExpireNotice()
    {
        if (_notice == null || DateTime.UtcNow < _noticeUntil)
        {
            return false;
        }
        _notice = null;
        return true;
    }

    /// <summary>Cambia un ajuste y lo guarda. La ventana se actualiza enseguida.</summary>
    public void Change(Action<Config> change)
    {
        lock (_cfgGate)
        {
            change(_config);
            SaveQuietly();
        }
        RequestPublish();
    }

    public void SetProfile(string key) => Change(c => c.Profile = key);

    /// <summary>Sumar o no al total las cajas de munición, de comida o los cañones de botín. Rehace las estadísticas enseguida.</summary>
    public void SetCountExtra(Action<Config> change)
    {
        _reportRequested = true;
        Change(change);
    }

    /// <summary>Sumar o no a tu total los cofres abiertos de tus compañeros. Rehace las estadísticas enseguida.</summary>
    public void SetCountTeammates(bool count)
    {
        _reportRequested = true;
        Change(c => c.CountTeammateChests = count);
    }

    /// <summary>Contar solo desde una fecha (null = todas las partidas). Rehace las estadísticas enseguida.</summary>
    public void SetCountFrom(DateTime? from)
    {
        _reportRequested = true;
        Change(c => c.CountFrom = from?.Date);
    }

    /// <summary>
    /// Días con partidas del perfil mostrado (todas, sin el filtro de fecha), del más reciente al más antiguo, con
    /// cuántas partidas hubo: es lo que ofrece el selector «Contar desde».
    /// </summary>
    public IReadOnlyList<(DateTime Day, int Matches)> MatchDays()
    {
        try
        {
            var key = Profiles.Current(_data, _config)?.Key;
            return _data.Matches.ToList()
                .Where(m => (key == null || Profiles.Key(m) == key) && Profiles.IsSupportedVersion(m))
                .GroupBy(Profiles.Day)
                .OrderByDescending(g => g.Key)
                .Select(g => (g.Key, g.Count()))
                .ToList();
        }
        catch (InvalidOperationException)
        {
            return Array.Empty<(DateTime, int)>(); // la lista cambió mientras se leía: sin días esta vez
        }
    }

    /// <summary>Dónde abrir el Resumen: servido por la app (su Configuración funciona) o, si no, el archivo.</summary>
    public string ReportTarget => ObsServer.ReportUrl ?? _store.ReportPath;

    /// <summary>Cambia el idioma compartido (app y estadísticas) y rehace las estadísticas enseguida.</summary>
    public void SetLanguage(string? code)
    {
        _reportRequested = true;
        Change(c => c.Language = code);
    }

    /// <summary>
    /// Configuración cambiada en la página de Estadísticas (desde el hilo del servidor local). El idioma es
    /// bidireccional (cambia también la app y la reinicia); el resto solo toca los ajustes de la página.
    /// </summary>
    private bool ApplyFromReport(string key, string value)
    {
        if (key == "lang")
        {
            if (value.Length > 0 && !L.Languages.Any(l => l.Code == value))
            {
                return false;
            }
            var next = value.Length == 0 ? null : value;
            var changed = _config.Language != next;
            SetLanguage(next);
            if (changed && AppInfo.ExePath != null)
            {
                // Deja responder al servidor HTTP y reescribir stats.html antes de cerrar y relanzar la app.
                _ = Task.Delay(250).ContinueWith(_ => Restart());
            }
            return true;
        }
        var on = value == "1";
        Action<ReportSettings>? change = key switch
        {
            "teammates" => r => r.ShowTeammates = on,
            "countMates" => r => r.CountTeammateChests = on,
            "ammo" => r => r.CountAmmoBoxes = on,
            "food" => r => r.CountFoodBoxes = on,
            "cannons" => r => r.CountLootCannons = on,
            _ => null,
        };
        if (change == null)
        {
            return false;
        }
        _reportRequested = true;
        Change(c => change(c.ReportView));
        return true;
    }

    /// <summary>Mostrar u ocultar los compañeros en la ventana.</summary>
    public void SetShowTeammates(bool show)
    {
        _reportRequested = true;
        Change(c => c.ShowTeammates = show);
    }

    private volatile string? _resetBackup;

    /// <summary>
    /// Restablece las estadísticas: borra todo el historial (y los cofres de antes del contador) y empieza de cero.
    /// Los replays que ya están en la carpeta no se vuelven a contar. Antes se guarda una copia de data.json.
    /// Devuelve la ruta de esa copia. Se hace en el hilo de vigilancia, que es el dueño de los datos.
    /// </summary>
    public string ResetStats()
    {
        var backup = Path.Combine(_store.Folder, $"data-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        _resetBackup = backup;
        _wake.Set();
        return backup;
    }

    // ---- Estimado de los cofres de antes de FCT ---------------------------------------------------------

    private volatile IReadOnlyList<MatchRecord> _profileMatches = Array.Empty<MatchRecord>();
    private volatile bool _scanned;

    /// <summary>Id de Epic del perfil mostrado (la clave del perfil cuando el replay lo trae), o null.</summary>
    public string? ShownAccountId => _shownKey is { Length: 32 } key && key.All(Uri.IsHexDigit) ? key : null;

    /// <summary>Lo que daría el estimado con este total de partidas, tal como están ahora las partidas contadas.</summary>
    public HistoryEstimator.Result PreviewEstimate(int totalMatches, int sample, double? manualAverage) =>
        HistoryEstimator.Compute(_profileMatches, totalMatches, DateTime.UtcNow, sample, manualAverage);

    /// <summary>Estimado guardado (null = solo las repeticiones).</summary>
    public HistoryEstimate? Estimate
    {
        get
        {
            lock (_cfgGate)
            {
                return _config.Estimate;
            }
        }
    }

    /// <summary>
    /// Elige cómo contar lo de antes: <paramref name="totalMatches"/> null = solo las repeticiones guardadas (se quita
    /// el estimado); si no, repeticiones + estimado con ese total de partidas, tomado ahora. Se calcula en cuanto haya
    /// al menos <paramref name="sample"/> partidas contadas para la media (si faltan, tras las próximas).
    /// </summary>
    public void SetEstimate(int? totalMatches, string source, int sample, double? manualAverage)
    {
        var key = _shownKey ?? "";
        Change(c =>
        {
            if (totalMatches is not int total)
            {
                c.Estimate = null;
                c.InitialChests.Remove(key);
                return;
            }
            c.Estimate = new HistoryEstimate { TotalMatches = total, SnapshotUtc = DateTime.UtcNow, Source = source, ProfileKey = _shownKey, SampleSize = sample, ManualAverage = manualAverage };
        });
        _reportRequested = true;
        _wake.Set();
    }

    /// <summary>Calcula el estimado pendiente cuando ya se puede (repeticiones contadas o 5 partidas jugadas).</summary>
    private bool ApplyEstimate()
    {
        HistoryEstimate? e;
        lock (_cfgGate)
        {
            e = _config.Estimate;
        }
        if (e == null || e.Done || !_scanned || _tracker.HasPending || _tracker.IsLive || _tracker.IsFinishing)
        {
            return false;
        }
        var key = e.ProfileKey ?? Profiles.Current(_data, _config)?.Key;
        if (key == null)
        {
            return false; // aún no hay ninguna partida: no se sabe de qué perfil es
        }
        var matches = Profiles.View(_data, _config).Matches.Where(m => Profiles.Key(m) == key).ToList();
        if (HistoryEstimator.Compute(matches, e.TotalMatches, e.SnapshotUtc, e.SampleSize, e.ManualAverage).Chests is not int chests)
        {
            return false;
        }
        lock (_cfgGate)
        {
            if (chests > 0)
            {
                _config.InitialChests[key] = chests;
            }
            else
            {
                _config.InitialChests.Remove(key);
            }
            e.ProfileKey = key;
            e.Done = true;
            SaveQuietly();
        }
        _reportRequested = true;
        SetNotice(chests > 0
            ? L.F("Partidas de antes: ≈ {0} cofres estimados sumados a tu total.", "Earlier matches: ≈ {0} estimated chests added to your total.", chests.ToString("#,0", L.Culture))
            : L.T("Partidas de antes: tus repeticiones ya las cubren todas, no hace falta estimar.", "Earlier matches: your replays already cover them all, no estimate needed."));
        return true;
    }

    /// <summary>Nombre del perfil mostrado, o null si aún no hay partidas.</summary>
    public string? ShownProfileName => _shownName;

    /// <summary>Cofres estimados de antes de usar el contador, del perfil mostrado.</summary>
    public int InitialChests
    {
        get
        {
            lock (_cfgGate)
            {
                return Profiles.InitialChests(_config, _shownKey);
            }
        }
    }

    /// <summary>Guarda el estimado de cofres de antes para el perfil mostrado y rehace las estadísticas.</summary>
    public void SetInitialChests(int count)
    {
        var key = _shownKey ?? "";
        Change(c =>
        {
            if (count > 0)
            {
                c.InitialChests[key] = count;
            }
            else
            {
                c.InitialChests.Remove(key);
            }
        });
        _reportRequested = true;
        _wake.Set();
    }

    /// <summary>
    /// Al arrancar, antes de leer replays: rehace las estadísticas (si no, tras actualizar seguirían las de la versión anterior
    /// hasta la próxima partida) y las abre si está activado «Abrir estadísticas al iniciar». No se abre cuando el contador
    /// arranca solo con Fortnite, para no tapar el juego.
    /// </summary>
    private void WriteReportAtStart()
    {
        ReportWriter.WriteAll(_store, _config, _data);
        if (_config.OpenReportOnStart && !AutoMode && File.Exists(_store.ReportPath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ReportTarget) { UseShellExecute = true })?.Dispose();
            }
            catch
            {
                // sin navegador: el botón «Estadísticas» de la ventana sigue funcionando
            }
        }
    }

    public void ChangeDemosFolder(string folder)
    {
        Change(c => c.DemosFolder = folder);
        RestartWatcher();
    }

    /// <summary>Abrir con Fortnite: crea o quita la entrada de inicio. Devuelve false si no se puede (no se ejecuta desde su .exe).</summary>
    public bool SetStartWithFortnite(bool on)
    {
        lock (_cfgGate)
        {
            // Otra carpeta de datos (--data, pruebas): solo se guarda; el inicio de Windows es de la instalación normal.
            var defaultFolder = string.Equals(Path.GetFullPath(_dataDir).TrimEnd('\\', '/'), Path.GetFullPath(Store.DefaultFolder()).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
            if (defaultFolder && (!OperatingSystem.IsWindows() || !FortniteWatch.Apply(on, _dataDir)))
            {
                return false;
            }
            _config.StartWithFortnite = on;
            SaveQuietly();
        }
        RequestPublish();
        return true;
    }

    /// <summary>El jugador decide mantener el contador abierto tras cerrar Fortnite.</summary>
    public void KeepOpen()
    {
        AutoMode = false;
        _keepOpenRequested = true;
        RequestPublish();
    }

    /// <summary>El overlay siempre está disponible: el botón y Ctrl+Mayús+O lo muestran y ocultan.</summary>
    public bool OverlayEnabled => OperatingSystem.IsWindows();

    /// <summary>True cuando el overlay está en pantalla (desactivado por defecto).</summary>
    public bool OverlayVisible => OperatingSystem.IsWindows() && NativeOverlay.IsVisible;

    public void ToggleOverlay()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        if (NativeOverlay.IsRunning)
        {
            NativeOverlay.Toggle();
            return;
        }
        lock (_cfgGate)
        {
            _config.OverlayVisible = true;
            SaveQuietly();
        }
        StartOverlay();
        RequestPublish();
    }

    /// <summary>Descarga e instala la nueva versión en segundo plano; luego pide reiniciar.</summary>
    public void InstallUpdate()
    {
        var u = _update;
        if (u == null || _installing)
        {
            return;
        }
        _installing = true;
        RequestPublish();
        _ = Task.Run(() =>
        {
            var error = Updater.Install(u);
            if (error == null)
            {
                _update = null;
                _notice = L.T("Reiniciando con la nueva versión…", "Restarting with the new version…");
                _installing = false;
                RestartAfterUpdate = true;
                RequestPublish();
                CloseRequested?.Invoke();
                return;
            }
            _installing = false;
            ErrorLog.Write("Actualización", error);
            SetNotice(L.F("No se pudo actualizar: {0}", "Update failed: {0}", error));
        });
    }

    /// <summary>Pide al hilo de vigilancia que publique el estado (se hace en cuanto puede).</summary>
    public void RequestPublish()
    {
        _publishRequested = true;
        _wake.Set();
    }

    // ---- Hilo de vigilancia ----------------------------------------------------------------------

    private void Loop()
    {
        WriteReportAtStart();
        var first = true;
        while (!_stop)
        {
            var changed = first || _publishRequested;
            first = false;
            _publishRequested = false;
            changed |= ApplyTreasureScreens();

            if (_resetBackup is { } backup)
            {
                _resetBackup = null;
                try
                {
                    _tracker.Reset(backup);
                    lock (_cfgGate)
                    {
                        _config.InitialChests.Clear();
                        _config.Profile = null;
                        SaveQuietly();
                    }
                    _reportRequested = true;
                    changed = true;
                }
                catch (Exception ex)
                {
                    ErrorLog.Write("Restablecer estadísticas", ex);
                    SetNotice(L.F("No se pudo restablecer: {0}", "Could not reset: {0}", ex.Message));
                }
            }

            try
            {
                if (DateTime.UtcNow >= _nextScan)
                {
                    var added = _tracker.Scan();
                    _scanned = true;
                    var liveChanged = _tracker.UpdateLive();
                    changed |= added > 0 || liveChanged;

                    // Partida en curso: se relee cada 15 s. Replay terminado: enseguida. Si no, un escaneo de respaldo.
                    _nextScan = DateTime.UtcNow.AddSeconds(_tracker.IsFinishing ? 2 : _tracker.HasPending ? 15 : (_watcher != null && !AutoMode ? 60 : 10));
                }
                changed |= CheckAutoClose();
                // El overlay sigue a Fortnite (abierto o no) también cuando se cierra sin cambiar de ventana activa.
                if (OperatingSystem.IsWindows() && DateTime.UtcNow >= _nextOverlayCheck)
                {
                    _nextOverlayCheck = DateTime.UtcNow.AddSeconds(3);
                    NativeOverlay.Recheck();
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Vigilancia", ex);
            }

            changed |= ApplyEstimate();
            PollUpdates();
            changed |= CheckUpdate();
            changed |= ExpireNotice();
            if (_reportRequested)
            {
                _reportRequested = false;
                ReportWriter.WriteAll(_store, _config, _data);
            }
            if (changed || _publishRequested)
            {
                _publishRequested = false;
                Publish();
            }

            // Espera a que pase algo (cambio de archivo, ajuste) o un segundo.
            _wake.WaitOne(TimeSpan.FromSeconds(1));
        }
    }

    private void Publish()
    {
        var shown = Profiles.Current(_data, _config);
        _shownKey = shown?.Key;
        _shownName = shown?.Name;
        _profileMatches = Profiles.View(_data, _config).Matches;
        var liveAdjusted = (_tracker.LiveMatch != null && Profiles.Counted(_tracker.LiveMatch, _config)) ? Profiles.Adjust(_tracker.LiveMatch, _config) : null;
        var view = OverlayModel.Build(Profiles.View(_data, _config), liveAdjusted, _tracker.IsLive && liveAdjusted != null, Profiles.InitialChests(_data, _config));
        ReportWriter.WriteOverlay(_store, view);
        ObsServer.Update(view);
        if (OperatingSystem.IsWindows() && NativeOverlay.IsRunning)
        {
            NativeOverlay.Update(view);
        }

        var notice = _installing
            ? L.T("Descargando la actualización…", "Downloading the update…")
            : _progress is { } p
                ? L.F("Leyendo tus replays… {0} / {1}. Las cifras suben a medida que se leen.", "Reading your replays… {0} / {1}. The numbers grow as they are read.", p.Done, p.Total)
            : _notice ?? (_autoCloseAt != null ? L.F("Fortnite está cerrado: {0} se cierra en 30 s.", "Fortnite is closed: {0} closes in 30 s.", AppInfo.ShortName) : null);
        var state = new MonitorState(view, Profiles.List(_data, _config), Profiles.Current(_data, _config)?.Key, _update, notice, _autoCloseAt != null, BuildStats())
        {
            Team = BuildTeam(),
        };
        Changed?.Invoke(state);
    }

    /// <summary>
    /// Estadísticas del perfil mostrado sobre todo su historial guardado (no solo los replays que siguen en disco),
    /// con la partida en curso incluida. Son las mismas cifras que la página de estadísticas.
    /// </summary>
    private ChestStats BuildStats()
    {
        var matches = Profiles.View(_data, _config).Matches;
        var live = _tracker.IsLive && _tracker.LiveMatch != null && Profiles.Counted(_tracker.LiveMatch, _config) ? Profiles.Adjust(_tracker.LiveMatch, _config) : null;
        var today = DateTime.Now.Date;

        var perDay = new Dictionary<DateTime, int>();
        foreach (var m in matches)
        {
            var day = (m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime().Date;
            perDay[day] = perDay.GetValueOrDefault(day) + m.ChestsOpened;
        }
        if (live != null)
        {
            perDay[today] = perDay.GetValueOrDefault(today) + live.ChestsOpened;
        }

        var all = live != null ? matches.Append(live).ToList() : matches;
        var average = matches.Count > 0 ? (double)matches.Sum(m => m.ChestsOpened) / matches.Count : 0;
        var best = perDay.Count > 0 ? perDay.MaxBy(kv => kv.Value) : (KeyValuePair<DateTime, int>?)null;
        return new ChestStats(
            Matches: matches.Count,
            Average: average,
            BestMatch: all.Count > 0 ? all.Max(m => m.ChestsOpened) : 0,
            Treasures: all.Sum(m => m.TreasureFinds.Count),
            Today: perDay.GetValueOrDefault(today),
            BestDay: best?.Key,
            BestDayChests: best?.Value ?? 0)
        {
            LastDays = BuildLastDays(today, live),
            HistoryMatches = Profiles.View(_data, _config).Matches.Count,
            LastMatch = BuildLastMatch(),
            HistorySince = Profiles.View(_data, _config).Matches
                .Select(m => (DateTime?)(m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime().Date)
                .Min(),
        };
    }

    /// <summary>
    /// Equipo de la partida en curso (si se puede leer) o de la última: tus cofres y los abiertos de cada compañero.
    /// Null si «Compañeros» está desactivado o aún no hay partidas.
    /// </summary>
    private TeamSummary? BuildTeam()
    {
        if (!_config.ShowTeammates)
        {
            return null;
        }
        var kinds = _config.ExtraKinds();
        var live = _tracker.IsLive && _tracker.LiveMatch != null && Profiles.Counted(_tracker.LiveMatch, _config) ? Profiles.Adjust(_tracker.LiveMatch, _config) : null;
        var m = live ?? Profiles.View(_data, _config).Matches.OrderByDescending(x => x.MatchStartUtc ?? x.AnalyzedAtUtc).FirstOrDefault();
        if (m == null)
        {
            return null;
        }
        var mates = (m.Teammates ?? new List<TeammateRecord>())
            .Select(t => new TeamMember(t.Name, MatchRecord.Seen(t, kinds)))
            .OrderByDescending(t => t.Chests)
            .ToList();
        return new TeamSummary(live != null, m.OwnChests, mates, m.Teammates == null);
    }

    /// <summary>Última partida terminada del perfil mostrado.</summary>
    private LastMatchSummary? BuildLastMatch()
    {
        var m = Profiles.View(_data, _config).Matches
            .OrderByDescending(x => x.MatchStartUtc ?? x.AnalyzedAtUtc)
            .FirstOrDefault();
        if (m == null)
        {
            return null;
        }
        return new LastMatchSummary(m.ChestsOpened, m.Chests.Count(c => c.Destroyed), m.TreasureFinds.Count,
            m.Placement, m.TotalPlayers, m.Eliminations);
    }

    /// <summary>
    /// Cofres de cada día de esta semana, de lunes a domingo, del perfil mostrado (todas sus partidas, no solo las
    /// 100 últimas). Los días que aún no han llegado van marcados como futuros.
    /// </summary>
    private List<DayChests> BuildLastDays(DateTime today, MatchRecord? live)
    {
        const int days = 7;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var sunday = monday.AddDays(days - 1);
        var sums = new Dictionary<DateTime, int>();
        var all = Profiles.View(_data, _config).Matches.Append(live).OfType<MatchRecord>();
        foreach (var m in all)
        {
            var day = (m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime().Date;
            if (day >= monday && day <= sunday)
            {
                sums[day] = sums.GetValueOrDefault(day) + m.ChestsOpened;
            }
        }
        return Enumerable.Range(0, days)
            .Select(i => monday.AddDays(i))
            .Select(d => new DayChests(d, sums.GetValueOrDefault(d), Future: d > today))
            .ToList();
    }

    /// <summary>Anuncia una versión nueva; en modo automático la instala cuando no hay partidas en curso. Devuelve true si hay que publicar.</summary>
    private bool CheckUpdate()
    {
        var u = _update;
        if (u == null || _updateAnnounced || _installing)
        {
            return false;
        }

        if (_config.UpdateMode == "auto")
        {
            var idle = !_tracker.IsLive && !_tracker.HasPending && !_tracker.IsFinishing;
            if (!idle || AppInfo.ExePath == null)
            {
                return false;
            }
            _updateAnnounced = true;
            InstallUpdate();
            return true;
        }

        _updateAnnounced = true;
        return true;
    }

    /// <summary>
    /// En segundo plano (ventana oculta): al cerrarse Fortnite, el contador se cierra solo en cuanto cuenta la última
    /// partida (5 min como mucho). Con la ventana a la vista, o con Fortnite abierto, no se cierra.
    /// </summary>
    private bool CheckAutoClose()
    {
        _keepOpenRequested = false;
        var now = DateTime.UtcNow;
        if (!InBackground || _closeRequested)
        {
            _fortniteClosedSince = null;
            return false;
        }
        // Mirar los procesos tiene un coste: cada 3 s basta.
        if (now < _nextGameCheck)
        {
            return false;
        }
        _nextGameCheck = now.AddSeconds(3);
        if (FortniteWatch.IsFortniteRunning())
        {
            _fortniteClosedSince = null;
            return false;
        }

        _fortniteClosedSince ??= now;
        var lastMatchDone = !_tracker.IsLive && !_tracker.HasPending && !_tracker.IsFinishing;
        if (!lastMatchDone && now - _fortniteClosedSince.Value < TimeSpan.FromMinutes(5))
        {
            return false;
        }
        _closeRequested = true;
        CloseRequested?.Invoke();
        return false;
    }

    private DateTime _nextGameCheck, _nextOverlayCheck;

    // ---- Piezas internas -------------------------------------------------------------------------

    private void StartOverlay()
    {
        NativeOverlay.Start(_config.OverlayX, _config.OverlayY, _config.OverlayLocked,
            (x, y) =>
            {
                lock (_cfgGate)
                {
                    _config.OverlayX = x;
                    _config.OverlayY = y;
                    SaveQuietly();
                }
            },
            locked =>
            {
                lock (_cfgGate)
                {
                    _config.OverlayLocked = locked;
                    SaveQuietly();
                }
            },
            _config.OverlayVisible,
            visible =>
            {
                lock (_cfgGate)
                {
                    _config.OverlayVisible = visible;
                    SaveQuietly();
                }
                RequestPublish(); // el botón de la ventana cambia de color
            },
            // Botón de cerrar del overlay: cierra la aplicación entera (y con ella el overlay).
            () => CloseRequested?.Invoke(),
            // Solo en pantalla con Fortnite delante.
            FortniteWatch.IsGameProcess);
    }

    /// <summary>Windows avisa cuando cambia un replay: no hace falta releer la carpeta todo el tiempo.</summary>
    private void RestartWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;
        try
        {
            if (Directory.Exists(_config.DemosFolder))
            {
                _watcher = new FileSystemWatcher(_config.DemosFolder, "*.replay")
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                _watcher.Created += (_, _) => Wake();
                _watcher.Changed += (_, _) => Wake();
                _watcher.Renamed += (_, _) => Wake();
            }
        }
        catch
        {
            _watcher = null; // escaneo periódico de respaldo
        }
    }

    private void Wake()
    {
        var soon = DateTime.UtcNow.AddSeconds(2);
        if (soon < _nextScan)
        {
            _nextScan = soon;
        }
        _wake.Set();
    }

    private void SaveQuietly()
    {
        try { _store.SaveConfig(_config); } catch { /* ignore */ }
    }
}
