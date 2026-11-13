namespace FCT.Reporting;

/// <summary>Turns raw match values into the short texts the window, the overlay and the stats page show.</summary>
public static class Pretty
{
    public static string Likely => L.T("probable", "likely");

    /// <summary>"Clasificatoria · Cero construcción · Dúo" from a playlist id; unknown ids lose their "Playlist_" prefix.</summary>
    public static string Playlist(string? playlist)
    {
        if (string.IsNullOrEmpty(playlist))
        {
            return L.T("modo desconocido", "unknown mode");
        }
        var id = playlist.ToLowerInvariant();
        var tags = new List<string>();
        if (id.Contains("habanero") || id.Contains("ranked"))
        {
            tags.Add(L.T("Clasificatoria", "Ranked"));
        }
        if (id.Contains("nobuild"))
        {
            tags.Add(L.T("Cero construcción", "Zero Build"));
        }
        if (TeamSize(id) is { } size)
        {
            tags.Add(size);
        }
        return tags.Count == 0 ? playlist.Replace("Playlist_", "") : string.Join(" · ", tags);
    }

    private static string? TeamSize(string id)
    {
        foreach (var (fragment, es, en) in new[] { ("solo", "Solo", "Solo"), ("duo", "Dúo", "Duos"), ("trio", "Trío", "Trios"), ("squad", "Escuadrón", "Squads") })
        {
            if (id.Contains(fragment))
            {
                return L.T(es, en);
            }
        }
        return null;
    }

    /// <summary>"Victoria", "3.º de 100", "3.º", or unknown.</summary>
    public static string Placement(MatchRecord match)
    {
        var place = match.Placement ?? 0;
        if (place <= 0)
        {
            return L.T("puesto desconocido", "unknown placement");
        }
        if (place == 1)
        {
            return L.T("Victoria", "Victory");
        }
        var ordinal = L.Ordinal(place);
        return match.TotalPlayers > 0 ? L.F("{0} de {1}", "{0} of {1}", ordinal, match.TotalPlayers) : ordinal;
    }

    /// <summary>Replay seconds as m:ss.</summary>
    public static string Clock(float seconds)
    {
        var whole = (int)seconds;
        return $"{whole / 60}:{whole % 60:00}";
    }

    /// <summary>When and from where a treasure came: "a las 7:16 · Cofre raro", or "hacia las 5:04 · Cofre raro (probable)".</summary>
    public static string Obtained(TreasureFind find)
    {
        if (find.ObtainedAtSeconds is not { } at)
        {
            return L.T("momento y origen desconocidos", "time and source unknown");
        }
        var prefix = find.TimeExact ? L.T("a las", "at") : L.T("hacia las", "around");
        var source = find.SourceCertain ? find.SourceLabel : $"{find.SourceLabel} ({Likely})";
        return $"{prefix} {Clock(at)} · {source}";
    }

    /// <summary>A readable item name from its blueprint: game prefixes removed, underscores as spaces.</summary>
    public static string ItemName(string item)
    {
        var name = item;
        foreach (var prefix in new[] { "WID_", "Athena", "AmmoData" })
        {
            name = name.Replace(prefix, "");
        }
        return name.Replace('_', ' ').Trim();
    }
}

/// <summary>Replays read so far out of those to read (the loading card of the stats page).</summary>
public sealed record ScanProgress(int Done, int Total);
