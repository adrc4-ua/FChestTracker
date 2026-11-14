using System.Text.RegularExpressions;

namespace FCT.Analysis;

/// <summary>
/// The modes whose matches count for the treasure hunt: Battle Royale with building and Zero Build, whatever the team
/// size (solo, duos, trios, squads), and Ranked Battle Royale when the player turns it on. Every other mode (Reload,
/// Blitz, limited-time modes, Creative…) is still read and kept in the history, but left out of every figure. Another
/// mode only needs to be added here.
/// </summary>
public static class EventModes
{
    /// <summary>"Playlist_DefaultSolo", "Playlist_Trios", "Playlist_NoBuildBR_Squad"… (the ids of the replays).</summary>
    private static readonly Regex BattleRoyale = new(@"^playlist_(default|nobuildbr_)?(solo|duos?|trios?|squads?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Ranked Battle Royale, with and without building ("Habanero" in the game's files; never its Reload or other
    /// variants): "Playlist_Habanero_Solo", "Playlist_ShowdownAlt_NoBuild_Duos"…
    /// </summary>
    private static readonly Regex Ranked = new(@"^playlist_(habanero|showdownalt|ranked)_?(nobuild(br)?_?)?(solo|duos?|trios?|squads?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Whether a match of this playlist counts (an unknown playlist does not); Ranked only with <paramref name="ranked"/>.</summary>
    public static bool Counts(string? playlist, bool ranked = false) =>
        playlist != null && (BattleRoyale.IsMatch(playlist) || (ranked && Ranked.IsMatch(playlist)));
}
