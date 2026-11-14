using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;

namespace FCT.App;

/// <summary>
/// Where Windows is told to start FCT: the scheduled task "Fortnite Chest Tracker" (current way, see
/// <see cref="FortniteWatch"/>) and the HKCU Run entries (fallback, and those of earlier versions to clean up).
/// FCT_TASK_NAME gives the task another name in tests, so the real one is never touched.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StartupEntries
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <summary>Run entry of the fallback standby.</summary>
    public const string StandbyEntry = AppInfo.ShortName;

    /// <summary>Run entries FCT ever wrote ("FortniteChestTracker" was a development name, "ChestCounter" the first one).</summary>
    public static readonly string[] AllRunEntries = { StandbyEntry, "FortniteChestTracker", "ChestCounter" };

    private static string TaskName => Environment.GetEnvironmentVariable("FCT_TASK_NAME") is { Length: > 0 } test ? test : AppInfo.Name;

    /// <summary>
    /// The System-log events of Fortnite's anti-cheat driver: 7045 "service installed" and FilterManager 6 "filter
    /// loaded", for EasyAntiCheat_EOSSys (and BEDaisy, BattlEye's). Both come once per game start, ~10 s before the game.
    /// </summary>
    internal const string GameStartQuery =
        "<QueryList><Query Id=\"0\" Path=\"System\">" +
        "<Select Path=\"System\">*[System[Provider[@Name='Service Control Manager'] and EventID=7045]] and *[EventData[Data[@Name='ServiceName']='EasyAntiCheat_EOSSys' or Data[@Name='ServiceName']='BEDaisy']]</Select>" +
        "<Select Path=\"System\">*[System[Provider[@Name='Microsoft-Windows-FilterManager'] and EventID=6]] and *[EventData[Data[@Name='DeviceName']='EasyAntiCheat_EOSSys' or Data[@Name='DeviceName']='BEDaisy']]</Select>" +
        "</Query></QueryList>";

    // ---- Scheduled task ----------------------------------------------------------------------------------------------------

    public static bool TaskExists() => Schtasks($"/Query /TN \"{TaskName}\"").ExitCode == 0;

    /// <summary>Registers the task, unless it already runs <paramref name="arguments"/> of this exe on those events.</summary>
    public static bool EnsureTask(string exe, string arguments)
    {
        var (code, registered) = Schtasks($"/Query /TN \"{TaskName}\" /XML");
        var upToDate = code == 0
            && registered.Contains(SecurityElement.Escape(exe), StringComparison.OrdinalIgnoreCase)
            && registered.Contains(SecurityElement.Escape(arguments), StringComparison.OrdinalIgnoreCase)
            && registered.Contains("EasyAntiCheat_EOSSys", StringComparison.Ordinal);
        if (upToDate)
        {
            return true;
        }
        var definition = Path.Combine(Path.GetTempPath(), $"fct-task-{Environment.ProcessId}.xml");
        try
        {
            File.WriteAllText(definition, TaskXml(exe, arguments), System.Text.Encoding.Unicode);
            return Schtasks($"/Create /TN \"{TaskName}\" /XML \"{definition}\" /F").ExitCode == 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            try { File.Delete(definition); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>The exe the registered task starts; null without a task.</summary>
    public static string? TaskExe()
    {
        var (code, registered) = Schtasks($"/Query /TN \"{TaskName}\" /XML");
        var command = code == 0 ? System.Text.RegularExpressions.Regex.Match(registered, "<Command>([^<]+)</Command>") : null;
        return command is { Success: true } ? System.Net.WebUtility.HtmlDecode(command.Groups[1].Value).Trim('"') : null;
    }

    public static void DeleteTask()
    {
        if (TaskExists())
        {
            Schtasks($"/Delete /TN \"{TaskName}\" /F");
        }
    }

    /// <summary>
    /// The task: the user's own (no admin rights), normal priority, one run at a time (the two events arrive together),
    /// also on battery, at most 10 minutes (the launcher ends long before).
    /// </summary>
    internal static string TaskXml(string exe, string arguments)
    {
        static string Esc(string text) => SecurityElement.Escape(text);
        var account = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        var about = L.T(
            $"Abre {AppInfo.Name} cuando arranca Fortnite («Abrir con Fortnite»). Se quita desde Configuración.",
            $"Opens {AppInfo.Name} when Fortnite starts (\"Open with Fortnite\"). Turned off in Settings.");
        return $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <Author>{Esc(AppInfo.Name)}</Author>
                <Description>{Esc(about)}</Description>
              </RegistrationInfo>
              <Triggers>
                <EventTrigger>
                  <Enabled>true</Enabled>
                  <Subscription>{Esc(GameStartQuery)}</Subscription>
                </EventTrigger>
              </Triggers>
              <Principals>
                <Principal id="Author">
                  <UserId>{Esc(account)}</UserId>
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
                  <Command>{Esc(exe)}</Command>
                  <Arguments>{Esc(arguments)}</Arguments>
                </Exec>
              </Actions>
            </Task>
            """;
    }

    /// <summary>Runs schtasks.exe hidden; (-1, "") when it cannot run or takes over 15 s.</summary>
    private static (int ExitCode, string Output) Schtasks(string arguments)
    {
        try
        {
            using var tool = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"), arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (tool == null)
            {
                return (-1, "");
            }
            var output = tool.StandardOutput.ReadToEndAsync();
            _ = tool.StandardError.ReadToEndAsync(); // drained so a full pipe can never block schtasks
            if (tool.WaitForExit(15_000))
            {
                return (tool.ExitCode, output.Result);
            }
            try { tool.Kill(); } catch (InvalidOperationException) { }
            return (-1, "");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return (-1, "");
        }
    }

    // ---- Run entries (logon) --------------------------------------------------------------------------------------------------

    /// <summary>Writes the fallback standby entry; false when the registry refuses.</summary>
    public static bool SetStandbyEntry(string command)
    {
        try
        {
            using var run = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            run.SetValue(StandbyEntry, command);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return false;
        }
    }

    /// <summary>The exes FCT's Run entries start (each command begins with the quoted exe).</summary>
    public static List<string> RunEntryExes()
    {
        var exes = new List<string>();
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            foreach (var name in AllRunEntries)
            {
                if (run?.GetValue(name) is string command && command.Contains("--standby", StringComparison.OrdinalIgnoreCase)
                    && command.StartsWith('"') && command.IndexOf('"', 1) is var end and > 1)
                {
                    exes.Add(command[1..end]);
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
        }
        return exes;
    }

    /// <summary>
    /// Removes these Run entries (and their Task Manager on/off state), but only those that start a "--standby":
    /// another program that happens to use one of the names is left alone.
    /// </summary>
    public static void RemoveRunEntries(IEnumerable<string> names)
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
            foreach (var name in names)
            {
                var ours = run?.GetValue(name) is not string command || command.Contains("--standby", StringComparison.OrdinalIgnoreCase);
                if (!ours)
                {
                    continue;
                }
                run?.DeleteValue(name, throwOnMissingValue: false);
                approved?.DeleteValue(name, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            // at worst an old entry starts a standby, which sees the task and exits
        }
    }
}
