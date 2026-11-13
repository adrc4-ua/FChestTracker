namespace FCT.App;

/// <summary>Version, GitHub repository and names shared by the tool's processes.</summary>
public static class AppInfo
{
    public static Version Version { get; } = typeof(AppInfo).Assembly.GetName().Version ?? new Version(1, 0, 0);

    public static string VersionText => $"{Version.Major}.{Version.Minor}.{Math.Max(0, Version.Build)}";

    public const string Repo = "adrc4-ua/Fortnite-Chest-Tracker";
    public static string RepoUrl => "https://github.com/" + Repo;
    public static string IssuesUrl => RepoUrl + "/issues";

    /// <summary>Name of the app where there is room for it (window title, welcome, report).</summary>
    public const string Name = "Fortnite Chest Tracker";
    /// <summary>Short name, used in sentences, menus and file names.</summary>
    public const string ShortName = "FCT";
    /// <summary>File name of the exe published in the releases.</summary>
    public const string ExeName = "FCT.exe";

    /// <summary>Path of the running exe when it is the published exe, whatever it is called (null under "dotnet FCT.dll").</summary>
    public static string? ExePath
    {
        get
        {
            var p = Environment.ProcessPath;
            return p != null && Path.GetExtension(p).Equals(".exe", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileNameWithoutExtension(p).Equals("dotnet", StringComparison.OrdinalIgnoreCase) ? p : null;
        }
    }

    // Names of the per-user kernel objects that let the processes find each other.
    /// <summary>
    /// One FCT at a time for the user, whatever its data folder: two would mean two overlays and two counters. Only the
    /// automatic tests set FCT_SEPARATE_INSTANCES=1 to run copies with their own data folder (one per folder).
    /// </summary>
    public static string InstanceMutexFor(string dataDir)
    {
        if (Environment.GetEnvironmentVariable("FCT_SEPARATE_INSTANCES") != "1")
        {
            return @"Local\FCT.Instance";
        }
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(dataDir).TrimEnd('\\', '/').ToLowerInvariant()));
        return @"Local\FCT.Instance." + Convert.ToHexString(hash, 0, 8);
    }
    /// <summary>Set by a second launch so that the running instance (maybe hidden in the tray) shows its window.</summary>
    public static string ShowEventFor(string dataDir) => InstanceMutexFor(dataDir).Replace(".Instance", ".Show");
    public const string StandbyMutex = @"Local\FCT.Standby";
    public const string StandbyStopEvent = @"Local\FCT.StandbyStop";
}
