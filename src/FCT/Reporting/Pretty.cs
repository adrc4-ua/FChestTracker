using System.Globalization;
using System.Net;
using System.Text;

namespace FCT.Reporting;

public static class Pretty
{
    public static string Playlist(string? playlist)
    {
        if (string.IsNullOrEmpty(playlist))
        {
            return L.T("modo desconocido", "unknown mode");
        }

        var p = playlist.ToLowerInvariant();
        var parts = new List<string>();
        if (p.Contains("habanero") || p.Contains("ranked")) parts.Add(L.T("Clasificatoria", "Ranked"));
        if (p.Contains("nobuild")) parts.Add(L.T("Cero construcción", "Zero Build"));
        if (p.Contains("solo")) parts.Add("Solo");
        else if (p.Contains("duo")) parts.Add(L.T("Dúo", "Duos"));
        else if (p.Contains("trio")) parts.Add(L.T("Trío", "Trios"));
        else if (p.Contains("squad")) parts.Add(L.T("Escuadrón", "Squads"));
        return parts.Count > 0 ? string.Join(" · ", parts) : playlist.Replace("Playlist_", "");
    }

    public static string Placement(MatchRecord m)
    {
        if (m.Placement is not int place || place <= 0)
        {
            return L.T("puesto desconocido", "unknown placement");
        }
        if (place == 1)
        {
            return L.T("¡Victoria Real!", "Victory Royale!");
        }
        var ordinal = L.Ordinal(place);
        return m.TotalPlayers is int total && total > 0
            ? L.F("{0} de {1}", "{0} of {1}", ordinal, total)
            : ordinal;
    }

    public static string Clock(float seconds)
    {
        var s = (int)seconds;
        return $"{s / 60}:{s % 60:00}";
    }

    /// <summary>"a las 7:16 · Cofre raro" (seguro) o "hacia las 5:04 · Cofre raro (probable)".</summary>
    public static string Obtained(TreasureFind f)
    {
        if (f.ObtainedAtSeconds is not float t)
        {
            return L.T("momento y origen desconocidos", "time and source unknown");
        }
        var when = f.TimeExact ? $"{L.T("a las", "at")} {Clock(t)}" : $"{L.T("hacia las", "around")} {Clock(t)}";
        return f.SourceCertain ? $"{when} · {f.SourceLabel}" : $"{when} · {f.SourceLabel} ({Likely})";
    }

    public static string Likely => L.T("probable", "likely");

    public static string ItemName(string item)
    {
        var name = item;
        foreach (var prefix in new[] { "WID_", "Athena", "AmmoData" })
        {
            name = name.Replace(prefix, "");
        }
        return name.Replace("_", " ").Trim();
    }
}

/// <summary>Replays analyzed so far / to analyze, shown as a loading card in the report.</summary>
public sealed record ScanProgress(int Done, int Total);
