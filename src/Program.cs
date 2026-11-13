using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

/// <summary>
/// Punto de entrada. Main lleva [STAThread]: Windows exige un hilo STA para el portapapeles y los diálogos
/// (con instrucciones de nivel superior el hilo principal arrancaba en MTA y "Copiar" fallaba en silencio).
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // --standby: el proceso invisible que espera a que arranque Fortnite ("abrir con Fortnite"). Sin consola ni ventana.
        if (args.Contains("--standby"))
        {
            var dataIndex = Array.IndexOf(args, "--data");
            return OperatingSystem.IsWindows()
                ? FortniteWatch.RunStandby(dataIndex >= 0 && dataIndex + 1 < args.Length ? args[dataIndex + 1] : Store.DefaultFolder())
                : 1;
        }

        // Estilo de Windows para los controles: debe fijarse antes de crear la primera ventana (también los mensajes).
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Las opciones de línea de comandos escriben en la terminal que lanzó el exe; si no, la aplicación es una ventana
        // y la consola (que el código de análisis usa para sus mensajes) se oculta.
        var cliMode = args.Any(a => a is "--once" or "--help" or "-h" or "--overlay-preview" or "--diagnose" or "--read-treasure-screen" or "--capture-window");
        if (OperatingSystem.IsWindows())
        {
            NativeConsole.Ensure(attachToParent: cliMode);
            if (!cliMode)
            {
                NativeConsole.Hide();
            }
        }
        Console.OutputEncoding = Encoding.UTF8;

        // Un fallo no debe cerrar la aplicación sin dejar rastro (el exe normalmente se abre con doble clic).
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            ErrorLog.Write("Unexpected error", e.ExceptionObject);
            if (cliMode)
            {
                Console.WriteLine(L.T("Error inesperado: ", "Unexpected error: ") + e.ExceptionObject);
            }
            else
            {
                MessageBox.Show(L.T("Error inesperado. Los detalles están en errors.log, en la carpeta de datos.",
                        "Unexpected error. The details are in errors.log, in the data folder.") + "\n\n" + e.ExceptionObject,
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // ---- Argumentos ----------------------------------------------------------------------------------
        string? dataDir = null, demosArg = null;
        var once = false;
        var reset = false;
        var auto = false;          // abierto por el proceso en espera cuando Fortnite arranca: se cierra solo al cerrar Fortnite
        var afterUpdate = false;   // reiniciado por el actualizador: espera a que la instancia anterior termine
        string? previewPath = null;
        string? diagnosePath = null; // diagnóstico (no aparece en la ayuda): contrasta las señales de cada replay
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--data": dataDir = args[++i]; break;
                case "--demos": demosArg = args[++i]; break;
                case "--lang": i++; break; // compatibilidad: el idioma lo elige Windows
                case "--once": once = true; break;
                case "--reset-config": reset = true; break;
                case "--overlay-preview": previewPath = args[++i]; break;
                case "--diagnose": diagnosePath = args[++i]; break;
                // Pruebas de la lectura de la pantalla de tesoros (no aparecen en la ayuda).
                case "--read-treasure-screen": return PostMatchTools.ReadImage(args[++i]);
                case "--capture-window": return PostMatchTools.CaptureWindow(args[++i], i + 1 < args.Length ? args[++i] : null);
                case "--auto": auto = true; break;
                case "--after-update": afterUpdate = true; break;
                case "-h":
                case "--help":
                    Console.WriteLine(L.Big("help", """
                        Fortnite Chest Tracker (FCT)

                        Opciones:
                          --demos <carpeta>   carpeta de los replays (por defecto %LOCALAPPDATA%\FortniteGame\Saved\Demos)
                          --data <carpeta>    carpeta de los datos (por defecto %APPDATA%\FCT)
                          --once              analiza los replays terminados, muestra el resumen y sale (sin ventana)
                          --reset-config      vuelve a mostrar las preguntas iniciales
                          --overlay-preview <archivo.png>   guarda vistas previas del overlay y sale
                        """, """
                        Fortnite Chest Tracker (FCT)

                        Options:
                          --demos <folder>    replays folder (default %LOCALAPPDATA%\FortniteGame\Saved\Demos)
                          --data <folder>     data folder (default %APPDATA%\FCT)
                          --once              analyze the finished replays, show the summary and quit (no window)
                          --reset-config      show the first-run questions again
                          --overlay-preview <file.png>   save previews of the overlay and quit
                        """));
                    return 0;
            }
        }

        // Carpeta de datos normal (no una de pruebas con --data): solo entonces se toca el inicio de Windows al empezar.
        var defaultFolder = dataDir == null;
        if (dataDir == null)
        {
            dataDir = Store.DefaultFolder();
            if (!Migration.MoveDataFolder(dataDir, TimeSpan.FromSeconds(afterUpdate ? 20 : 3)))
            {
                var closeOld = L.T("Cierra la versión anterior del contador para que FCT pueda traer tu historial, y vuelve a abrir FCT.",
                    "Close the previous version of the counter so that FCT can bring your history over, then open FCT again.");
                if (cliMode) Console.WriteLine("  " + closeOld);
                else MessageBox.Show(closeOld, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 0;
            }
        }
        var store = new Store(dataDir);
        ErrorLog.FilePath = store.ErrorLogPath;

        // Una sola instancia por carpeta de datos: dos contarían las mismas partidas.
        using var instance = new Mutex(false, AppInfo.InstanceMutexFor(dataDir));
        if (previewPath == null && diagnosePath == null && !AcquireInstance(instance, afterUpdate ? TimeSpan.FromSeconds(20) : TimeSpan.Zero))
        {
            if (auto)
            {
                return 0; // abierto con Fortnite cuando ya estaba abierto: nada que hacer
            }
            // Abierto otra vez a mano: el que ya está en marcha (quizá oculto en el área de notificación) muestra su ventana.
            if (!cliMode && EventWaitHandle.TryOpenExisting(AppInfo.ShowEventFor(dataDir), out var showRunning))
            {
                using (showRunning)
                {
                    showRunning.Set();
                }
                return 0;
            }
            var alreadyOpen = L.F("{0} ya está abierto: búscalo en el área de notificación (la flecha ^ de la barra de tareas).",
                "{0} is already open: look for it in the notification area (the ^ arrow on the taskbar).", AppInfo.ShortName);
            if (cliMode)
            {
                Console.WriteLine("  " + alreadyOpen);
            }
            else
            {
                MessageBox.Show(alreadyOpen, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return 0;
        }

        // ---- Configuración -------------------------------------------------------------------------------
        var config = reset ? null : store.LoadConfig();
        var firstRun = config == null;
        config ??= new Config { DefaultsVersion = Config.CurrentDefaults };
        // Opciones que ahora vienen activadas (antes no): se encienden una vez en las configuraciones anteriores.
        config.ApplyNewDefaults();
        L.Use(config.Language); // idioma elegido en Ajustes; sin elegir, el de Windows

        if (demosArg != null) config.DemosFolder = demosArg;
        var defaultDemos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FortniteGame", "Saved", "Demos");
        if (string.IsNullOrWhiteSpace(config.DemosFolder))
        {
            config.DemosFolder = defaultDemos;
        }

        if (previewPath != null)
        {
            // Desarrollo: dibuja el overlay fuera de pantalla y guarda PNG (en espera, partida en curso y en vivo), luego sale.
            store.LoadPrizeNames();
            var previewData = Profiles.View(store.LoadData(), config);
            // Solo en memoria: las 3 últimas partidas cuentan como "esta sesión", para una línea de sesión realista.
            foreach (var m in previewData.Matches.OrderByDescending(m => m.MatchStartUtc ?? m.AnalyzedAtUtc).Take(3))
            {
                m.MatchStartUtc = DateTime.UtcNow;
            }
            if (!OperatingSystem.IsWindows())
            {
                return 1;
            }
            NativeOverlay.Start(-20000, -20000, false, (_, _) => { }, _ => { }, onClose: () => { });
            var fakeLive = new MatchRecord { ChestsOpened = 12, TreasureFinds = { new TreasureFind { Item = "Treasure" } } };
            // "En vivo" primero: el overlay no encoge (ancho estable), así las tres vistas salen del mismo tamaño, como en el juego.
            foreach (var (suffix, live, inProgress) in new[] { ("-en-vivo", fakeLive, true), ("-en-curso", (MatchRecord?)null, true), ("-espera", null, false) })
            {
                NativeOverlay.Update(OverlayModel.Build(previewData, live, inProgress, Profiles.InitialChests(config, previewData.Matches.Select(Profiles.Key).FirstOrDefault())));
                Thread.Sleep(600);
                var file = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(previewPath)) ?? ".", Path.GetFileNameWithoutExtension(previewPath) + suffix + ".png");
                Console.WriteLine(NativeOverlay.Snapshot(file)
                    ? L.F("Vista previa: {0}", "Preview: {0}", file)
                    : L.F("Error al capturar: {0}", "Capture failed: {0}", file));
            }
            return 0;
        }

        if (diagnosePath != null)
        {
            // Diagnóstico: lee un replay (o los de una carpeta) sin guardar nada y muestra, partida a partida, las
            // señales independientes del replay (cofres, concesiones del premio, tesoros destacados) y lo que no cuadra.
            store.LoadPrizeNames();
            var files = Directory.Exists(diagnosePath)
                ? new DirectoryInfo(diagnosePath).GetFiles("*.replay").OrderBy(f => f.Name).ToList()
                : new List<FileInfo> { new(diagnosePath) };
            foreach (var f in files)
            {
                Console.WriteLine();
                Console.WriteLine(f.Name);
                if (!f.Exists || ReplayHeaderProbe.KeyMissing(f.FullName))
                {
                    Console.WriteLine(L.T("  No se puede leer (no existe o es una partida en curso).", "  Cannot be read (missing, or a match in progress)."));
                    continue;
                }
                try
                {
                    using var ms = new MemoryStream(File.ReadAllBytes(f.FullName), writable: false);
                    var analyzer = new ReplayAnalyzer { Diagnosis = new List<string>() };
                    var m = analyzer.Analyze(ms, f.Name, "");
                    Console.WriteLine($"  {L.DateTimeShort((m.MatchStartUtc ?? m.AnalyzedAtUtc).ToLocalTime())} · {Pretty.Playlist(m.Playlist)} · {m.PlayerName}");
                    foreach (var line in analyzer.Diagnosis)
                    {
                        Console.WriteLine("  " + line);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  " + L.T("Error: ", "Error: ") + ex.Message);
                }
            }
            return 0;
        }

        LowerPriority();
        store.LoadPrizeNames();
        var data = store.LoadData();
        var tracker = new Tracker(store, config, data);
        // Antes de cualquier escaneo: si no, los replays se analizarían dos veces.
        tracker.UpgradeIfNeeded();

        // ---- Modo de línea de comandos -------------------------------------------------------------------
        if (once)
        {
            tracker.Scan(ignoreQuietPeriod: true);
            ReportWriter.WriteAll(store, config, data);
            tracker.PrintSummary();
            Console.WriteLine(L.F("  Estadísticas: {0}", "  Stats: {0}", store.ReportPath));
            return 0;
        }

        // ---- Primer uso: aviso de las repeticiones y, si hay replays, si contarlos -------------------------------------------
        if (firstRun)
        {
            // Una versión que no es la última: se ofrece descargar la nueva antes de empezar. Si se instala, arranca la
            // nueva (que vuelve a dar la bienvenida); si no, las actualizaciones pasan a «Preguntar».
            if (config.UpdateMode != "off" && AppInfo.ExePath != null && Updater.CheckAsync().GetAwaiter().GetResult() is { } newer)
            {
                var download = InfoDialog.Ask(null, L.F("Hay una versión nueva de {0}", "There's a new version of {0}", AppInfo.ShortName),
                    L.T("Tarda unos segundos. ¿La descargamos antes de empezar?", "It only takes a few seconds. Shall we get it before you start?"),
                    Array.Empty<InfoPoint>(), L.T("Descargar", "Download"), L.T("Ahora no", "Not now"), Icons.Download);
                if (download == null)
                {
                    return 0; // ventana cerrada: no se guarda nada y la próxima vez vuelve a ser el primer uso
                }
                if (download == true)
                {
                    var error = Updater.Install(newer);
                    if (error == null && AppInfo.ExePath is { } updatedExe)
                    {
                        try { instance.ReleaseMutex(); } catch { }
                        Process.Start(new ProcessStartInfo(updatedExe, "--after-update") { UseShellExecute = true })?.Dispose();
                        return 0;
                    }
                    MessageBox.Show(L.F("No se pudo actualizar: {0}", "Update failed: {0}", error), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    config.UpdateMode = "prompt";
                }
            }

            // Bienvenida: qué hace, lo único que hay que activar en Fortnite y, si ya hay partidas guardadas, si contarlas.
            var existing = tracker.UnprocessedFiles().Where(f => ReplayHeaderProbe.IsLive(f.FullName) != true).ToList();
            var points = new List<InfoPoint>
            {
                new(Icons.RecordReplays, Theme.GoldHi, L.T("Activa las repeticiones en Fortnite", "Turn on replays in Fortnite"),
                    L.T("Ajustes → Juego → Repeticiones → «Grabar repeticiones». Sin esto no hay nada que contar.",
                        "Settings → Game → Replays → \"Record Replays\". Without it there's nothing to count.")),
                existing.Count > 0
                    ? new InfoPoint(Icons.Folder, Theme.GoldHi,
                        L.F("Tienes {0} guardadas", "You have {0} saved", L.Count(existing.Count, "partida", "partidas", "match", "matches")),
                        L.T("¿Las contamos ya? Si no, empezamos con las próximas.", "Shall we count them now? If not, we start with your next ones."))
                    : new InfoPoint(Icons.Matches, Theme.GoldHi, L.T("Y a jugar", "Then just play"),
                        L.T("Al acabar cada partida, tus cofres se suman solos.", "After each match, your chests are added up on their own.")),
            };
            var countThem = InfoDialog.Ask(null, L.F("Te damos la bienvenida a {0}", "Welcome to {0}", AppInfo.ShortName),
                L.T("Cuenta los cofres que abres en Fortnite, sin que tengas que hacer nada.", "It counts the chests you open in Fortnite, without you doing a thing."),
                points,
                existing.Count > 0 ? L.T("Contarlas", "Count them") : L.T("Empezar", "Start"),
                existing.Count > 0 ? L.T("Solo las nuevas", "Only new ones") : null,
                Icons.Sparkle);
            if (countThem == null)
            {
                return 0; // ventana cerrada
            }
            // Partidas de antes: solo las repeticiones, o repeticiones + un estimado (se calcula cuando estén contadas;
            // si no llegan a las partidas elegidas para la media, tras las próximas).
            // Sin partidas guardadas que contar (no hay, o «Solo las nuevas»), la primera opción es «Empezar de cero».
            if (EstimateDialog.Ask(null, null, null, null, null, fromScratch: existing.Count == 0 || countThem == false) is not { } earlier)
            {
                return 0; // ventana cerrada
            }
            // Solo ahora, con todos los pasos hechos, se guarda algo: cerrar antes deja la app como sin abrir.
            if (existing.Count > 0 && countThem == false)
            {
                tracker.MarkSkipped(existing);
            }
            if (earlier.Estimate)
            {
                config.Estimate = new HistoryEstimate { TotalMatches = earlier.TotalMatches, SnapshotUtc = DateTime.UtcNow, Source = earlier.Source, SampleSize = earlier.Sample, ManualAverage = earlier.ManualAverage };
            }
            // «Abrir con Fortnite» viene activado: se puede quitar en Configuración. Con la carpeta normal se registra ya
            // (solo desde el .exe); con otra (--data, pruebas) solo se guarda: el inicio de Windows es de la instalación normal.
            if (defaultFolder)
            {
                config.StartWithFortnite = OperatingSystem.IsWindows() && AppInfo.ExePath != null && FortniteWatch.Apply(true, dataDir);
            }
        }
        store.SaveConfig(config);

        if (OperatingSystem.IsWindows())
        {
            Updater.CleanupOld();
            if (config.StartWithFortnite && defaultFolder)
            {
                FortniteWatch.Apply(true, dataDir); // sigue la ruta del exe si se movió o se actualizó (y lo activa tras actualizar)
            }
        }

        // ---- Ventana -------------------------------------------------------------------------------------
        // Si el análisis cambió, los replays se vuelven a leer en silencio (sin duplicados): no hace falta avisar.
        // Con Fortnite abierto (o abierto por él): se ve el overlay y la app queda en segundo plano. Sin Fortnite, se
        // abre la ventana. El overlay solo aparece con Fortnite delante.
        var fortniteOpen = auto || (OperatingSystem.IsWindows() && FortniteWatch.IsFortniteRunning());
        if (fortniteOpen)
        {
            config.OverlayVisible = true;
        }
        var monitor = new AppMonitor(store, config, data, tracker, dataDir) { AutoMode = auto };

        var form = new MainForm(monitor, store);
        monitor.CloseRequested += () =>
        {
            try { form.BeginInvoke(new Action(form.ExitApp)); }
            catch (InvalidOperationException) { /* la ventana aún no existe */ }
        };

        // Otra apertura a mano mientras este está en marcha: se muestra la ventana.
        using var showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, AppInfo.ShowEventFor(dataDir));
        var showWait = ThreadPool.RegisterWaitForSingleObject(showRequest, (_, _) =>
        {
            try { form.BeginInvoke(new Action(form.ShowFromTray)); }
            catch (InvalidOperationException) { /* la ventana aún no existe o ya se cerró */ }
        }, null, Timeout.Infinite, executeOnlyOnce: false);

        monitor.Start();
        // Con Fortnite abierto solo se ve el overlay: la ventana queda oculta (fuera de la barra de tareas) y se recupera
        // desde el icono del área de notificación. Sin Fortnite, o en el primer uso (bienvenida), se muestra la ventana.
        form.StartInBackground = !firstRun && fortniteOpen && monitor.OverlayVisible;
        Application.Run(form);
        showWait.Unregister(null);
        monitor.Stop();

        // Nueva versión instalada: arranca el exe nuevo.
        // También tras cambiar el idioma en Ajustes (sin "--after-update": no hay versión nueva que anunciar).
        if ((monitor.RestartAfterUpdate || monitor.RestartRequested) && AppInfo.ExePath is { } newExe)
        {
            try { instance.ReleaseMutex(); } catch { }
            var kept = args.Where(a => a != "--after-update").Select(a => a.Contains(' ') ? $"\"{a}\"" : a);
            var restartArgs = string.Join(" ", monitor.RestartAfterUpdate ? kept.Append("--after-update") : kept);
            Process.Start(new ProcessStartInfo(newExe, restartArgs) { UseShellExecute = true })?.Dispose();
        }
        return 0;

        // ---- Ayudantes -----------------------------------------------------------------------------------

        static bool AcquireInstance(Mutex m, TimeSpan wait)
        {
            try
            {
                return m.WaitOne(wait);
            }
            catch (AbandonedMutexException)
            {
                return true; // la instancia anterior se cerró mal: ahora es nuestra
            }
        }

        // Prioridad baja + modo eficiencia de Windows 11 (EcoQoS): el juego siempre va primero.
        static void LowerPriority()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }
            try { EcoQos.Enable(); } catch { }
        }
    }
}
