using System.Text;

namespace FCT.App;

/// <summary>
/// errors.log in the data folder: the full details of replays that could not be read and of crashes, meant to be
/// attached to a bug report. Personal paths are masked, and the file is trimmed so it never grows past about 1 MB.
/// </summary>
public static class ErrorLog
{
    private const int MaxBytes = 1_000_000, KeepChars = 500_000;
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Where to write; null (before the data folder is known) silently drops entries.</summary>
    public static string? FilePath { get; set; }

    public static void Write(string what, object error)
    {
        if (FilePath is not { } path)
        {
            return;
        }
        try
        {
            var entry = $"==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} · {AppInfo.ShortName} {AppInfo.VersionText} · {Environment.OSVersion.VersionString}"
                        + Environment.NewLine + what + Environment.NewLine + error + Environment.NewLine + Environment.NewLine;
            File.AppendAllText(path, MaskUser(entry), Utf8);
            if (new FileInfo(path).Length > MaxBytes)
            {
                var all = File.ReadAllText(path);
                File.WriteAllText(path, all[^KeepChars..], Utf8);
            }
        }
        catch
        {
            // a log that fails must never take the app down with it
        }
    }

    /// <summary>Replaces the profile folder and the Windows user name, which bug reports would otherwise make public.</summary>
    private static string MaskUser(string text)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (home.Length > 3)
        {
            text = text.Replace(home, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
        var name = Environment.UserName;
        return name.Length >= 3 ? text.Replace(name, "<user>", StringComparison.OrdinalIgnoreCase) : text;
    }
}
