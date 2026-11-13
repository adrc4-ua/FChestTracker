using System.Diagnostics;
using System.Runtime.Versioning;

namespace FCT.App;

/// <summary>
/// "Open with Fortnite", with nothing running in between matches. Fortnite's anti-cheat installs and loads its driver
/// every time the game starts, and Windows logs it; a scheduled task of the user (<see cref="StartupEntries"/>, no admin
/// rights) starts a short launcher on that event (<see cref="RunOnGameStart"/>), which opens FCT once the game is up
/// and ends. FCT closes itself after Fortnite closes. Where the task cannot be created, the older way remains: a
/// tiny invisible "standby" started at logon (<see cref="RunStandby"/>). Neither reads anything from the game.
/// </summary>
[SupportedOSPlatform("windows")]
public static class FortniteWatch
{
    /// <summary>The driver comes ~10 s before the game; slow disks or a game update can stretch that.</summary>
    private static readonly TimeSpan LauncherPatience = TimeSpan.FromMinutes(3);

    public static bool IsGameProcess(int pid) => GameProcess.IsGame(pid);
    public static bool IsFortniteRunning() => GameProcess.IsRunning();
    public static bool TaskExists() => StartupEntries.TaskExists();

    /// <summary>
    /// Turns the option on or off. Also called at every start, so the task follows the exe when it moves or updates
    /// and older startup entries disappear. False when it cannot be set up (FCT not running from its exe).
    /// </summary>
    public static bool Apply(bool enabled, string dataDir)
    {
        if (!enabled)
        {
            StartupEntries.DeleteTask();
            StartupEntries.RemoveRunEntries(StartupEntries.AllRunEntries);
            StopStandby();
            return true;
        }
        if (AppInfo.ExePath is not { } exe)
        {
            return false;
        }
        if (StartupEntries.EnsureTask(exe, "--on-game " + DataArg(dataDir)))
        {
            StartupEntries.RemoveRunEntries(StartupEntries.AllRunEntries); // nothing left at logon
            StopStandby();
            return true;
        }

        // Fallback: the standby at logon (the entry is rewritten each start, so it follows the exe too).
        if (!StartupEntries.SetStandbyEntry($"\"{exe}\" --standby {DataArg(dataDir)}"))
        {
            return false;
        }
        StartupEntries.RemoveRunEntries(StartupEntries.AllRunEntries.Where(name => name != StartupEntries.StandbyEntry));
        try
        {
            StartStandby(exe, dataDir);
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static string DataArg(string dataDir) => $"--data \"{Path.GetFullPath(dataDir).TrimEnd('\\', '/')}\"";

    /// <summary>Opens FCT in background mode, unless it is already open or the option was turned off meanwhile.</summary>
    private static void OpenFct(string exe, string dataDir)
    {
        if (new Store(dataDir).LoadConfig() is { StartWithFortnite: false })
        {
            return;
        }
        if (Mutex.TryOpenExisting(AppInfo.InstanceMutexFor(dataDir), out var running))
        {
            running.Dispose(); // already open: nothing to say
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(exe, $"--auto {DataArg(dataDir)}") { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    // ---- The launcher (scheduled task) ------------------------------------------------------------------------------------

    /// <summary>
    /// "--on-game": waits for the game and opens FCT. With none of Fortnite's start-up programs around, the event came
    /// from another game using the same anti-cheat and this ends at once; it also gives up when they all go away.
    /// </summary>
    public static int RunOnGameStart(string dataDir)
    {
        if (AppInfo.ExePath is not { } exe)
        {
            return 1;
        }
        var waiting = Stopwatch.StartNew();
        while (!GameProcess.IsRunning())
        {
            if (!GameProcess.IsStarting() || waiting.Elapsed >= LauncherPatience)
            {
                return 0;
            }
            Thread.Sleep(1000);
        }
        if (new Store(dataDir).LoadConfig() is { StartWithFortnite: false })
        {
            return 0;
        }
        OpenFct(exe, dataDir);
        return 0;
    }

    // ---- The standby (fallback) --------------------------------------------------------------------------------------------

    public static void StartStandby(string exe, string dataDir)
    {
        if (Mutex.TryOpenExisting(AppInfo.StandbyMutex, out var alreadyWaiting))
        {
            alreadyWaiting.Dispose();
            return;
        }
        Process.Start(new ProcessStartInfo(exe, $"--standby {DataArg(dataDir)}") { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
    }

    public static void StopStandby()
    {
        if (EventWaitHandle.TryOpenExisting(AppInfo.StandbyStopEvent, out var stop))
        {
            using (stop)
            {
                stop.Set();
            }
        }
    }

    /// <summary>
    /// "--standby": idle-priority loop that checks every 5 s for the game, opens FCT, then waits for the game to close
    /// before arming again (the player may have closed FCT on purpose). Ends when signalled, or when the scheduled
    /// task exists (an entry left by an earlier version).
    /// </summary>
    public static int RunStandby(string dataDir)
    {
        using var single = new Mutex(false, AppInfo.StandbyMutex);
        try
        {
            if (!single.WaitOne(0))
            {
                return 0; // another standby is waiting
            }
        }
        catch (AbandonedMutexException)
        {
            // the previous standby was killed: this one takes over
        }
        if (StartupEntries.TaskExists())
        {
            StartupEntries.RemoveRunEntries(StartupEntries.AllRunEntries);
            return 0;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, AppInfo.StandbyStopEvent);
        try
        {
            using var self = Process.GetCurrentProcess();
            self.PriorityClass = ProcessPriorityClass.Idle;
            EfficiencyMode.Enable();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
        GC.Collect();
        if (AppInfo.ExePath is not { } exe)
        {
            return 1;
        }

        for (;;)
        {
            if (stop.WaitOne(TimeSpan.FromSeconds(5)))
            {
                return 0;
            }
            if (!GameProcess.IsRunning())
            {
                continue;
            }
            if (new Store(dataDir).LoadConfig() is { StartWithFortnite: false })
            {
                return 0;
            }
            OpenFct(exe, dataDir);
            while (GameProcess.IsRunning())
            {
                if (stop.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    return 0;
                }
            }
            GC.Collect();
        }
    }
}
