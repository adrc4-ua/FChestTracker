using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;

namespace FCT.App;

/// <summary>
/// "Start with Fortnite", with nothing running in the meantime. Each time Fortnite starts, its anti-cheat installs and
/// loads its driver, and Windows writes that in the System log (event 7045 "service installed" and FilterManager 6
/// "filter loaded", both naming EasyAntiCheat_EOSSys; BEDaisy for BattlEye). A scheduled task of the user (no admin
/// rights needed) starts a short-lived launcher on those events ("--on-game"): it waits up to 3 minutes for the game
/// (the driver comes ~10 s before it; another game with the same anti-cheat never shows Fortnite, and the launcher
/// just ends) and opens the tool, which closes itself once Fortnite is closed and the last match counted.
/// If the task cannot be created (policies), the old way stays as fallback: a tiny invisible "standby" process
/// started at Windows logon that looks every few seconds at the list of running programs.
/// Nothing reads from the game: only the system log and the list of running programs.
/// </summary>
[SupportedOSPlatform("windows")]
public static class FortniteWatch
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string RunValue = AppInfo.ShortName;
    // Entries at Windows logon of earlier versions and names ("FortniteChestTracker" was a development name).
    private static readonly string[] LegacyRunValues = { RunValue, "FortniteChestTracker", "ChestCounter" };
    // How long the launcher waits for the game after the anti-cheat driver (it comes ~10 s before; slow disks, updates).
    private static readonly TimeSpan GameWait = TimeSpan.FromMinutes(3);
    // FCT_GAME_PROCESS: another process name, to test the standby without Fortnite.
    private static readonly string[] GameProcesses =
        { Environment.GetEnvironmentVariable("FCT_GAME_PROCESS") is { Length: > 0 } test ? test : "FortniteClient-Win64-Shipping" };

    // FCT_TASK_NAME: another task name, to test without touching the real one.
    private static string TaskName => Environment.GetEnvironmentVariable("FCT_TASK_NAME") is { Length: > 0 } test ? test : AppInfo.Name;

    /// <summary>The events of the anti-cheat driver that Fortnite starts (see the class summary).</summary>
    internal const string GameStartQuery =
        "<QueryList><Query Id=\"0\" Path=\"System\">" +
        "<Select Path=\"System\">*[System[Provider[@Name='Service Control Manager'] and EventID=7045]] and *[EventData[Data[@Name='ServiceName']='EasyAntiCheat_EOSSys' or Data[@Name='ServiceName']='BEDaisy']]</Select>" +
        "<Select Path=\"System\">*[System[Provider[@Name='Microsoft-Windows-FilterManager'] and EventID=6]] and *[EventData[Data[@Name='DeviceName']='EasyAntiCheat_EOSSys' or Data[@Name='DeviceName']='BEDaisy']]</Select>" +
        "</Query></QueryList>";

    /// <summary>True when the process with this id is the game (the overlay only shows while the game is in front).</summary>
    public static bool IsGameProcess(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return GameProcesses.Any(n => string.Equals(p.ProcessName, n, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false; // ya no existe o sin permiso: no es el juego
        }
    }

    public static bool IsFortniteRunning()
    {
        foreach (var name in GameProcesses)
        {
            var found = Process.GetProcessesByName(name);
            var any = found.Length > 0;
            foreach (var p in found)
            {
                p.Dispose();
            }
            if (any)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Turns "start with Fortnite" on or off: the scheduled task (or, if it cannot be created, the entry at Windows
    /// logon + the standby). Called at each launch too: the task follows the exe if it is moved or updated, and the
    /// entries of earlier versions are removed. Returns false when it cannot be done (tool not run from its exe).
    /// </summary>
    public static bool Apply(bool enabled, string dataDir)
    {
        if (!enabled)
        {
            DeleteTask();
            RemoveRunEntries(LegacyRunValues);
            StopStandby();
            return true;
        }
        var exe = AppInfo.ExePath;
        if (exe == null)
        {
            return false;
        }
        if (EnsureTask(exe, dataDir))
        {
            // Nothing left at Windows logon: no entry, no process waiting.
            RemoveRunEntries(LegacyRunValues);
            StopStandby();
            return true;
        }
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            key.SetValue(RunValue, $"\"{exe}\" --standby {DataArg(dataDir)}"); // refreshed at each launch: follows the exe
            RemoveRunEntries(LegacyRunValues.Where(v => v != RunValue));
            StartStandby(exe, dataDir);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DataArg(string dataDir) => $"--data \"{Path.GetFullPath(dataDir).TrimEnd('\\', '/')}\"";

    /// <summary>The task: the user's own, normal priority, one launcher at a time (both events come together).</summary>
    internal static string TaskXml(string exe, string dataDir)
    {
        static string X(string text) => SecurityElement.Escape(text);
        var user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        var description = L.T(
            $"Abre {AppInfo.Name} cuando arranca Fortnite («Abrir con Fortnite»). Se quita desde Configuración.",
            $"Opens {AppInfo.Name} when Fortnite starts (\"Open with Fortnite\"). Turned off in Settings.");
        return $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Author>{X(AppInfo.Name)}</Author>
                <Description>{X(description)}</Description>
              </RegistrationInfo>
              <Triggers>
                <EventTrigger>
                  <Enabled>true</Enabled>
                  <Subscription>{X(GameStartQuery)}</Subscription>
                </EventTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{X(user)}</UserId>
                  <LogonType>InteractiveToken</LogonType>
                  <RunLevel>LeastPrivilege</RunLevel>
                </Principal>
              </Principals>
              <Settings>
                <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                <StartWhenAvailable>false</StartWhenAvailable>
                <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                <IdleSettings>
                  <StopOnIdleEnd>false</StopOnIdleEnd>
                  <RestartOnIdle>false</RestartOnIdle>
                </IdleSettings>
                <AllowStartOnDemand>true</AllowStartOnDemand>
                <Enabled>true</Enabled>
                <Hidden>false</Hidden>
                <ExecutionTimeLimit>PT10M</ExecutionTimeLimit>
                <Priority>4</Priority>
              </Settings>
              <Actions Context="Author">
                <Exec>
                  <Command>{X(exe)}</Command>
                  <Arguments>{X("--on-game " + DataArg(dataDir))}</Arguments>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    /// <summary>Creates the task, or leaves it when it already starts this exe with this folder.</summary>
    private static bool EnsureTask(string exe, string dataDir)
    {
        var (code, current) = Schtasks($"/Query /TN \"{TaskName}\" /XML");
        if (code == 0
            && current.Contains(SecurityElement.Escape(exe), StringComparison.OrdinalIgnoreCase)
            && current.Contains(SecurityElement.Escape("--on-game " + DataArg(dataDir)), StringComparison.OrdinalIgnoreCase)
            && current.Contains("EasyAntiCheat_EOSSys", StringComparison.Ordinal))
        {
            return true;
        }
        var file = Path.Combine(Path.GetTempPath(), $"fct-task-{Environment.ProcessId}.xml");
        try
        {
            File.WriteAllText(file, TaskXml(exe, dataDir), System.Text.Encoding.Unicode);
            return Schtasks($"/Create /TN \"{TaskName}\" /XML \"{file}\" /F").Code == 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { File.Delete(file); } catch { }
        }
    }

    private static void DeleteTask()
    {
        if (TaskExists())
        {
            Schtasks($"/Delete /TN \"{TaskName}\" /F");
        }
    }

    /// <summary>True when "start with Fortnite" is set up as the scheduled task.</summary>
    public static bool TaskExists() => Schtasks($"/Query /TN \"{TaskName}\"").Code == 0;

    private static (int Code, string Output) Schtasks(string arguments)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p == null)
            {
                return (-1, "");
            }
            var output = p.StandardOutput.ReadToEndAsync();
            _ = p.StandardError.ReadToEndAsync(); // drained, so that schtasks never blocks on a full pipe
            if (!p.WaitForExit(15000))
            {
                try { p.Kill(); } catch { }
                return (-1, "");
            }
            return (p.ExitCode, output.Result);
        }
        catch
        {
            return (-1, "");
        }
    }

    /// <summary>Removes our entries at Windows logon (also from Task Manager's startup list).</summary>
    private static void RemoveRunEntries(IEnumerable<string> names)
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            using var approved = Registry.CurrentUser.OpenSubKey(StartupApprovedKey, writable: true);
            foreach (var name in names)
            {
                // Only ours: an entry with that name that starts something else is left alone.
                if (run?.GetValue(name) is string command && !command.Contains("--standby", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                run?.DeleteValue(name, throwOnMissingValue: false);
                approved?.DeleteValue(name, throwOnMissingValue: false);
            }
        }
        catch
        {
            // not critical: at worst an old entry starts a standby that finds the task and ends
        }
    }

    /// <summary>
    /// The launcher started by the task: waits for Fortnite, then opens the tool (unless it is already open or the
    /// option was turned off meanwhile) and ends. No window, nothing left running.
    /// </summary>
    public static int RunOnGameStart(string dataDir)
    {
        var exe = AppInfo.ExePath;
        if (exe == null)
        {
            return 1;
        }
        var waited = Stopwatch.StartNew();
        while (!IsFortniteRunning())
        {
            if (waited.Elapsed >= GameWait)
            {
                return 0; // another game with the same anti-cheat
            }
            Thread.Sleep(2000);
        }
        if (new Store(dataDir).LoadConfig() is { StartWithFortnite: false })
        {
            return 0;
        }
        if (Mutex.TryOpenExisting(AppInfo.InstanceMutexFor(dataDir), out var open))
        {
            open.Dispose(); // already open: nothing to say
            return 0;
        }
        try { Process.Start(new ProcessStartInfo(exe, $"--auto {DataArg(dataDir)}") { UseShellExecute = false })?.Dispose(); } catch { }
        return 0;
    }

    public static void StartStandby(string exe, string dataDir)
    {
        if (Mutex.TryOpenExisting(AppInfo.StandbyMutex, out var running))
        {
            running.Dispose();
            return;
        }
        Process.Start(new ProcessStartInfo(exe, $"--standby {DataArg(dataDir)}") { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
    }

    public static void StopStandby()
    {
        if (EventWaitHandle.TryOpenExisting(AppInfo.StandbyStopEvent, out var stop))
        {
            stop.Set();
            stop.Dispose();
        }
    }

    /// <summary>
    /// The standby process (fallback when the task cannot be created, or an entry at logon of an earlier version):
    /// no window, waits for Fortnite, opens the tool, waits for Fortnite to close.
    /// </summary>
    public static int RunStandby(string dataDir)
    {
        using var single = new Mutex(false, AppInfo.StandbyMutex);
        try
        {
            if (!single.WaitOne(0))
            {
                return 0; // already waiting
            }
        }
        catch (AbandonedMutexException)
        {
            // previous standby was killed: take over
        }

        // "Start with Fortnite" is the task now (entry left by an earlier version): nothing to wait for.
        if (TaskExists())
        {
            RemoveRunEntries(LegacyRunValues);
            return 0;
        }

        using var stop = new EventWaitHandle(false, EventResetMode.AutoReset, AppInfo.StandbyStopEvent);
        try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; } catch { }
        try { EcoQos.Enable(); } catch { }
        GC.Collect();

        var exe = AppInfo.ExePath;
        if (exe == null)
        {
            return 1;
        }

        while (true)
        {
            if (stop.WaitOne(TimeSpan.FromSeconds(5)))
            {
                return 0;
            }
            if (!IsFortniteRunning())
            {
                continue;
            }

            // Turned off in the settings meanwhile: stop waiting.
            if (new Store(dataDir).LoadConfig() is { StartWithFortnite: false })
            {
                return 0;
            }

            if (Mutex.TryOpenExisting(AppInfo.InstanceMutexFor(dataDir), out var open))
            {
                open.Dispose(); // the tool is already open
            }
            else
            {
                try { Process.Start(new ProcessStartInfo(exe, $"--auto {DataArg(dataDir)}") { UseShellExecute = false })?.Dispose(); } catch { }
            }

            // Arm again only for the next Fortnite launch (the player may have closed the tool on purpose).
            while (IsFortniteRunning())
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
