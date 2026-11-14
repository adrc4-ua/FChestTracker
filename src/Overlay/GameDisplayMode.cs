namespace FCT.Overlay;

/// <summary>
/// Fortnite's window mode, read from its own settings file (never written). In exclusive fullscreen the game owns the
/// screen and Windows draws no other window over it, so the overlay cannot be seen while playing.
/// </summary>
public static class GameDisplayMode
{
    /// <summary>FCT_GAME_SETTINGS points to another file in tests.</summary>
    public static string SettingsPath =>
        Environment.GetEnvironmentVariable("FCT_GAME_SETTINGS") is { Length: > 0 } test
            ? test
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FortniteGame", "Saved", "Config", "WindowsClient", "GameUserSettings.ini");

    /// <summary>
    /// True in exclusive fullscreen, false in windowed fullscreen or a window, null when it cannot be told. The game
    /// saves the mode the player confirmed (0 fullscreen, 1 windowed fullscreen, 2 window); older files only have
    /// "FullscreenMode".
    /// </summary>
    public static bool? IsExclusiveFullscreen()
    {
        try
        {
            string? confirmed = null, plain = null;
            foreach (var line in File.ReadLines(SettingsPath))
            {
                if (line.StartsWith("LastConfirmedFullscreenMode=", StringComparison.OrdinalIgnoreCase))
                {
                    confirmed = line[(line.IndexOf('=') + 1)..].Trim();
                }
                else if (line.StartsWith("FullscreenMode=", StringComparison.OrdinalIgnoreCase))
                {
                    plain = line[(line.IndexOf('=') + 1)..].Trim();
                }
            }
            return (confirmed ?? plain) switch
            {
                "0" => true,
                "1" or "2" => false,
                _ => null,
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null; // no file (game never started here) or being written: try again later
        }
    }
}
