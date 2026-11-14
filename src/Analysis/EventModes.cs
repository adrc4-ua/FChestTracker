using System.Text.RegularExpressions;

namespace FCT.Analysis;

/// <summary>
/// The modes whose matches count for the treasure hunt: Battle Royale with building and Zero Build, whatever the team
/// size (solo, duos, trios, squads). Every other mode (Ranked, Reload, Blitz, limited-time modes, Creative…) is still read
/// and kept in the history, but left out of every figure. Another mode only needs to be added here.
/// </summary>
public static class EventModes
{
    /// <summary>"Playlist_DefaultSolo", "Playlist_Trios", "Playlist_NoBuildBR_Squad"… (the ids of the replays).</summary>
    private static readonly Regex BattleRoyale = new(@"^playlist_(default|nobuildbr_)?(solo|duos?|trios?|squads?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Whether a match of this playlist counts for the treasure hunt (an unknown playlist does not).</summary>
    public static bool Counts(string? playlist) => playlist != null && BattleRoyale.IsMatch(playlist);
}
