using System.Diagnostics;

namespace FCT.App;

/// <summary>
/// Recognises Fortnite's processes. Only the list of running programs is looked at; nothing of the game is read.
/// FCT_GAME_PROCESS replaces the game's name in tests ("x" is the game, "x_starting" a program of its start-up).
/// </summary>
public static class GameProcess
{
    private static readonly string? TestName = Environment.GetEnvironmentVariable("FCT_GAME_PROCESS") is { Length: > 0 } name ? name : null;

    /// <summary>The game itself.</summary>
    private static readonly string[] Game = { TestName ?? "FortniteClient-Win64-Shipping" };

    /// <summary>
    /// Every program of Fortnite's start-up: Epic's bootstrapper and launcher, the anti-cheat bootstrapper (the one that
    /// installs the driver) and the game.
    /// </summary>
    private static readonly string[] StartUp = TestName != null
        ? new[] { TestName, TestName + "_starting" }
        : new[] { "FortniteBootstrapper", "FortniteLauncher", "FortniteClient-Win64-Shipping_EAC_EOS", "FortniteClient-Win64-Shipping" };

    public static bool IsRunning() => AnyRunning(Game);

    /// <summary>Fortnite is starting or running (any of its start-up programs is alive).</summary>
    public static bool IsStarting() => AnyRunning(StartUp);

    /// <summary>The process with this id is the game (the overlay only shows over it).</summary>
    public static bool IsGame(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return Array.Exists(Game, name => name.Equals(process.ProcessName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false; // gone, or not ours to look at: not the game
        }
    }

    private static bool AnyRunning(string[] names)
    {
        foreach (var name in names)
        {
            var matches = Process.GetProcessesByName(name);
            foreach (var process in matches)
            {
                process.Dispose();
            }
            if (matches.Length > 0)
            {
                return true;
            }
        }
        return false;
    }
}
