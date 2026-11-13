namespace FCT.App;

/// <summary>What FCT.exe was asked to do, from its command line.</summary>
internal sealed class LaunchOptions
{
    /// <summary>Options that print to the terminal (and so attach to it) instead of opening the window.</summary>
    private static readonly HashSet<string> TerminalOptions = new()
    {
        "--once", "--help", "-h", "--overlay-preview", "--diagnose", "--read-treasure-screen", "--capture-window", "--licenses", "--uninstall",
    };

    public string? DataFolder { get; private set; }
    public string? DemosFolder { get; private set; }
    public bool Once { get; private set; }
    public bool ResetConfig { get; private set; }

    /// <summary>Opened by "Open with Fortnite": closes itself after the game.</summary>
    public bool Auto { get; private set; }

    /// <summary>Restarted by the updater: waits for the previous instance to finish closing.</summary>
    public bool AfterUpdate { get; private set; }

    public string? OverlayPreview { get; private set; }

    /// <summary>--diagnose (not in the help): the replay's signals side by side, for a file or a folder.</summary>
    public string? Diagnose { get; private set; }

    /// <summary>An option that runs on its own and ends the program (help, licenses, uninstall, screen tests).</summary>
    public Func<int>? RunAndExit { get; private set; }

    /// <summary>Output goes to the terminal that started the exe.</summary>
    public bool Terminal { get; private init; }

    public static LaunchOptions Parse(string[] args)
    {
        var options = new LaunchOptions { Terminal = args.Any(TerminalOptions.Contains) };
        var i = 0;
        string Value() => args[++i]; // a missing value fails like any other startup error
        for (; i < args.Length && options.RunAndExit == null; i++)
        {
            switch (args[i])
            {
                case "--data": options.DataFolder = Value(); break;
                case "--demos": options.DemosFolder = Value(); break;
                case "--lang": Value(); break; // older option: the language now follows Windows or Settings
                case "--once": options.Once = true; break;
                case "--reset-config": options.ResetConfig = true; break;
                case "--auto": options.Auto = true; break;
                case "--after-update": options.AfterUpdate = true; break;
                case "--overlay-preview": options.OverlayPreview = Value(); break;
                case "--diagnose": options.Diagnose = Value(); break;
                case "--licenses": options.RunAndExit = AppInfo.PrintLicenses; break;
                case "--uninstall": options.RunAndExit = () => OperatingSystem.IsWindows() ? Uninstaller.RunFromCommandLine() : 1; break;
                case "-h" or "--help": options.RunAndExit = PrintHelp; break;
                // Tests of the treasure screen reading (not in the help).
                case "--read-treasure-screen":
                    var image = Value();
                    options.RunAndExit = () => PostMatchTools.ReadImage(image);
                    break;
                case "--capture-window":
                    var title = Value();
                    var output = i + 1 < args.Length ? Value() : null;
                    options.RunAndExit = () => PostMatchTools.CaptureWindow(title, output);
                    break;
            }
        }
        return options;
    }

    private static int PrintHelp()
    {
        Console.WriteLine(L.Big("help", """
            Fortnite Chest Tracker (FCT)

            Opciones:
              --demos <carpeta>   carpeta de los replays (por defecto %LOCALAPPDATA%\FortniteGame\Saved\Demos)
              --data <carpeta>    carpeta de los datos (por defecto %APPDATA%\FCT)
              --once              analiza los replays terminados, muestra el resumen y sale (sin ventana)
              --reset-config      vuelve a mostrar las preguntas iniciales
              --overlay-preview <archivo.png>   guarda vistas previas del overlay y sale
              --licenses          muestra la licencia y los avisos de terceros
              --uninstall         quita todo rastro de FCT (tarea, inicio de Windows, el exe); tus datos se quedan
            """, """
            Fortnite Chest Tracker (FCT)

            Options:
              --demos <folder>    replays folder (default %LOCALAPPDATA%\FortniteGame\Saved\Demos)
              --data <folder>     data folder (default %APPDATA%\FCT)
              --once              analyze the finished replays, show the summary and quit (no window)
              --reset-config      show the first-run questions again
              --overlay-preview <file.png>   save previews of the overlay and quit
              --licenses          show the license and third-party notices
              --uninstall         remove every trace of FCT (task, Windows startup, the exe); your data stays
            """));
        return 0;
    }
}
