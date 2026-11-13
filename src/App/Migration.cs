using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace FCT.App;

/// <summary>
/// One-time takeover from v1.0.0, which was published under the name "ChestCounter": its data folder, its startup entry,
/// its background process and the placeholder prize names it wrote. Nothing happens when there is nothing to take
/// over. This is the only code that knows the old name; it can go once nobody runs v1.0.0.
/// </summary>
internal static class Migration
{
    private const string V1Name = "ChestCounter";

    /// <summary>Prize names v1.0.0 filled in by itself; the player never chose them.</summary>
    private static readonly string[] V1PlaceholderNames =
    {
        "Plunder in Paradise (pantalla de carga)", "Plunder in Paradise (loading screen)",
        "NO TRESPASSING (pista musical)", "NO TRESPASSING (jam track)", "No Trespassing (pista musical)",
    };

    /// <summary>
    /// Moves %APPDATA%\ChestCounter to <paramref name="folder"/> when that one does not exist yet. Waits up to
    /// <paramref name="wait"/> for v1.0.0 to exit (right after an update it is still closing). False when v1.0.0 keeps
    /// the folder: the caller then asks the player to close it.
    /// </summary>
    public static bool MoveDataFolder(string folder, TimeSpan wait)
    {
        var v1Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), V1Name);
        if (Directory.Exists(folder) || !File.Exists(Path.Combine(v1Folder, "data.json")))
        {
            return true; // already moved, or never installed
        }

        SignalV1Standby();
        var clock = Stopwatch.StartNew();
        // v1.0.0 does not keep its files open, so a move would "succeed" under its feet: wait for it to be gone.
        while (V1IsRunning(v1Folder))
        {
            if (clock.Elapsed >= wait)
            {
                return false;
            }
            Thread.Sleep(500);
        }

        for (;;)
        {
            try
            {
                Directory.Move(v1Folder, folder);
                break;
            }
            catch (IOException) when (clock.Elapsed < wait)
            {
                Thread.Sleep(500); // still locked for a moment
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
        DropPlaceholderNames(Path.Combine(folder, "treasures.json"));
        DeleteV1StartupEntry();
        return true;
    }

    /// <summary>v1.0.0 held one single-instance mutex per data folder, named after a hash of the folder.</summary>
    private static bool V1IsRunning(string v1Folder)
    {
        var normalized = Path.GetFullPath(v1Folder).TrimEnd('\\', '/').ToLowerInvariant();
        var name = $@"Local\{V1Name}.Instance.{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)), 0, 8)}";
        if (!Mutex.TryOpenExisting(name, out var held))
        {
            return false;
        }
        held.Dispose();
        return true;
    }

    /// <summary>Keeps only names a player typed: not blank, not v1's "…?" guesses, not its placeholders.</summary>
    private static void DropPlaceholderNames(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }
        try
        {
            var names = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new();
            var chosen = new Dictionary<string, string>();
            foreach (var (id, name) in names)
            {
                var placeholder = string.IsNullOrWhiteSpace(name) || name.TrimEnd().EndsWith('?')
                    || Array.Exists(V1PlaceholderNames, p => p.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (!placeholder)
                {
                    chosen[id] = name;
                }
            }
            File.WriteAllText(path, JsonSerializer.Serialize(chosen, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // only display names: the catalogue ones are shown instead
        }
    }

    /// <summary>Asks v1.0.0's background process ("open with Fortnite") to exit.</summary>
    private static void SignalV1Standby()
    {
        if (OperatingSystem.IsWindows() && EventWaitHandle.TryOpenExisting($@"Local\{V1Name}.StandbyStop", out var stop))
        {
            stop.Set();
            stop.Dispose();
        }
    }

    private static void DeleteV1StartupEntry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            run?.DeleteValue(V1Name, throwOnMissingValue: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            // the current startup setup is written separately
        }
    }
}
