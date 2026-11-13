using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace FCT.App;

/// <summary>
/// One-time move from v1.0.0, published under the old name: data folder, Windows startup entry, standby process and
/// the default names it wrote in treasures.json. It is the only place that knows the old name; it does nothing when
/// there is nothing to move, and can be deleted once nobody runs v1.0.0 any more.
/// </summary>
internal static class Migration
{
    private const string OldName = "ChestCounter";

    /// <summary>
    /// Moves %APPDATA%\ChestCounter to <paramref name="folder"/> (only the default folder, only if it does not exist
    /// yet). Waits up to <paramref name="wait"/> for v1.0.0 to close (after an update it is still closing). Returns
    /// false when the old folder is still in use: the caller asks the player to close the old version.
    /// </summary>
    public static bool MoveDataFolder(string folder, TimeSpan wait)
    {
        var old = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), OldName);
        if (Directory.Exists(folder) || !File.Exists(Path.Combine(old, "data.json")))
        {
            return true;
        }
        StopOldStandby();
        var until = Stopwatch.StartNew();
        // v1.0.0 still open (it does not keep its files open, so the move alone would not notice): it would go on
        // writing to the old folder. Wait for it to close (after an update it is closing), else ask.
        while (OldInstanceRunning(old))
        {
            if (until.Elapsed >= wait)
            {
                return false;
            }
            Thread.Sleep(500);
        }
        while (true)
        {
            try
            {
                Directory.Move(old, folder);
                CleanTreasureNames(Path.Combine(folder, "treasures.json"));
                RemoveOldStartupEntry();
                return true;
            }
            catch (IOException) when (until.Elapsed < wait)
            {
                Thread.Sleep(500);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    /// <summary>The single-instance mutex of v1.0.0 for its data folder (same naming as <see cref="AppInfo.InstanceMutexFor"/>).</summary>
    private static bool OldInstanceRunning(string oldFolder)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(oldFolder).TrimEnd('\\', '/').ToLowerInvariant()));
        if (Mutex.TryOpenExisting($@"Local\{OldName}.Instance." + Convert.ToHexString(hash, 0, 8), out var mutex))
        {
            mutex.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>v1.0.0 wrote its default prize names (with "?" when unverified) and an empty name per gizmo.</summary>
    private static void CleanTreasureNames(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }
            var names = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
            string[] defaults =
            {
                "Plunder in Paradise (pantalla de carga)", "Plunder in Paradise (loading screen)",
                "NO TRESPASSING (pista musical)", "NO TRESPASSING (jam track)", "No Trespassing (pista musical)",
            };
            var kept = names.Where(kv => !string.IsNullOrWhiteSpace(kv.Value) && !kv.Value.TrimEnd().EndsWith('?')
                                         && !defaults.Contains(kv.Value.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            File.WriteAllText(path, JsonSerializer.Serialize(kept, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // not critical: the names are only shown instead of the catalogue ones
        }
    }

    /// <summary>"Open with Fortnite" of v1.0.0: its standby process and its entry at Windows logon.</summary>
    private static void StopOldStandby()
    {
        if (OperatingSystem.IsWindows() && EventWaitHandle.TryOpenExisting($@"Local\{OldName}.StandbyStop", out var stop))
        {
            using (stop)
            {
                stop.Set();
            }
        }
    }

    private static void RemoveOldStartupEntry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            key?.DeleteValue(OldName, throwOnMissingValue: false);
        }
        catch
        {
            // not critical: the new entry is written separately when "Open with Fortnite" is on
        }
    }
}
