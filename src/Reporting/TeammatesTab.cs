using System.Globalization;
using System.Text;
using static FCT.Reporting.PageParts;

namespace FCT.Reporting;

/// <summary>
/// The "Compañeros" tab (a setting of the page): who the player plays with, matches together, wins and the chests
/// they were seen opening. Those chests come from the player's replay, which only holds what happened near them, so
/// they are a minimum (≥).
/// </summary>
internal static class TeammatesTab
{
    private static readonly string[] Medals = { "🥇", "🥈", "🥉" };
    private static readonly string[] Tones = { "var(--t1)", "var(--t2)", "var(--t3)", "var(--t4)", "var(--t5)" };

    /// <summary>One teammate over every match played together.</summary>
    private sealed record Mate(string Key, string Name, int Matches, int Chests, int Best, int Wins, DateTime Last, int TeamChests);

    /// <summary>The tab's body, and how many teammates its table lists (the count on the tab button).</summary>
    public static (string Html, int Regulars) Build(List<MatchRecord> matches, IReadOnlyCollection<string> kinds)
    {
        var mates = Gather(matches, kinds);
        if (mates.Count == 0)
        {
            return ($"<div class=\"sheet\"><div class=\"void void-big\">{L.T("Aún no hay partidas en equipo analizadas. Aparecerán aquí tras tu próxima partida en dúos, tríos o escuadrones.", "No team matches analyzed yet. They will show up here after your next duos, trios or squads match.")}</div></div>", 0);
        }

        // The regulars (two matches or more); one-match fill-ins are summed up in a line under the table.
        var listed = mates.Where(m => m.Matches >= 2).ToList();
        var hidden = mates.Count - listed.Count;
        if (listed.Count == 0)
        {
            listed = mates.Take(20).ToList();
            hidden = Math.Max(0, mates.Count - 20);
        }

        var html = new StringBuilder(TeamFigures(matches, kinds));
        html.Append("<div class=\"pair pair-wide\">").Append(Table(mates, listed, hidden)).Append("<div class=\"pile\">");
        html.Append(Board(L.T("Más cofres abiertos", "Most chests opened"),
            mates.OrderByDescending(m => m.Chests).ThenByDescending(m => m.Matches).Take(5).Select(m => (m, (double)m.Chests)).ToList()));
        html.Append(Board(L.T("Récord en una partida", "Best single match"),
            mates.Where(m => m.Best > 0).OrderByDescending(m => m.Best).ThenByDescending(m => m.Matches).Take(5).Select(m => (m, (double)m.Best)).ToList()));
        return (html.Append("</div></div>").ToString(), listed.Count);
    }

    private static List<Mate> Gather(List<MatchRecord> matches, IReadOnlyCollection<string> kinds) =>
        matches
            .Where(m => m.Teammates != null)
            .SelectMany(m => m.Teammates!.Select(t => (Match: m, Mate: t)))
            .GroupBy(r => r.Mate.EpicId ?? r.Mate.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var latest = group.OrderByDescending(r => LocalStart(r.Match)).First();
                var together = group.Select(r => r.Match).Distinct().ToList();
                return new Mate(
                    group.Key,
                    latest.Mate.Name,
                    together.Count,
                    group.Sum(r => MatchRecord.Seen(r.Mate, kinds)),
                    group.Max(r => MatchRecord.Seen(r.Mate, kinds)),
                    together.Count(m => m.Placement == 1),
                    LocalStart(latest.Match),
                    together.Sum(m => m.OwnChests + m.TeammatesSeen(kinds)));
            })
            .OrderByDescending(m => m.Matches).ThenByDescending(m => m.Chests)
            .ToList();

    /// <summary>Team chests: the player's own plus what the teammates were seen opening, over team matches.</summary>
    private static string TeamFigures(List<MatchRecord> matches, IReadOnlyCollection<string> kinds)
    {
        var team = matches.Where(m => m.Teammates is { Count: > 0 }).ToList();
        var own = team.Sum(m => m.OwnChests);
        var theirs = team.Sum(m => m.TeammatesSeen(kinds));
        var all = own + theirs;
        string PerMatch(int total) => team.Count > 0 ? ((double)total / team.Count).ToString("0.0", L.Culture) : "—";
        const string AtLeast = "<small>≥</small>";
        static string Mini(string value, string text, string valueClass = "") =>
            $"<div class=\"mini\"><b{(valueClass.Length > 0 ? $" class=\"{valueClass}\"" : "")}>{value}</b><span>{text}</span></div>";
        var body = "<div class=\"mini-figs\">"
                   + Mini(AtLeast + Num(all), L.T("cofres del equipo en total", "team chests in total"), "is-gold")
                   + Mini(AtLeast + PerMatch(all), L.T("del equipo por partida en equipo", "for the team per team match"))
                   + Mini(PerMatch(own), L.T("tuyos por partida en equipo", "yours per team match"))
                   + Mini($"{AtLeast}{(all > 0 ? (100.0 * theirs / all).ToString("0", L.Culture) : "0")} %", L.T("cofres del equipo abiertos por tus compañeros", "team chests opened by your teammates"))
                   + "</div>";
        return Section(L.T("Cofres del equipo", "Team chests"), null, body);
    }

    /// <summary>Every teammate in a table sortable by any column (matches, wins, average…).</summary>
    private static string Table(List<Mate> mates, List<Mate> listed, int hidden)
    {
        static string Raw(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        static string Th(string label, string type, bool centred, bool sorted = false) =>
            $"<th{(centred ? " class=\"ac\"" : "")} data-type=\"{type}\"{(sorted ? " aria-sort=\"descending\"" : "")}><button type=\"button\">{label}</button></th>";

        var meta = $"{L.Count(mates.Count, "compañero", "compañeros", "teammate", "teammates")} · {L.T("toca una columna para ordenar", "tap a column to sort")}";
        var html = new StringBuilder($"<section class=\"fit\"><div class=\"sec-head\"><h2>{L.T("Todos tus compañeros", "All your teammates")}</h2><span class=\"sec-meta\">{meta}</span></div>")
            .Append("<div class=\"sheet scrollx\"><table class=\"sort-table\"><thead><tr>")
            .Append(Th(L.T("Compañero", "Teammate"), "s", false))
            .Append(Th(L.T("Partidas", "Matches"), "n", true, true))
            .Append(Th(L.T("Victorias", "Wins"), "n", true))
            .Append(Th(L.T("Cofres abiertos", "Chests opened"), "n", true))
            .Append(Th(L.T("Media", "Average"), "n", true))
            .Append(Th(L.T("Aporta", "Share"), "n", true))
            .Append(Th(L.T("Última partida", "Last match"), "n", true))
            .Append("</tr></thead><tbody>");
        foreach (var mate in listed)
        {
            var average = (double)mate.Chests / mate.Matches;
            var share = mate.TeamChests > 0 ? (double)mate.Chests / mate.TeamChests : 0;
            var wins = mate.Wins > 0 ? $"<b class=\"accent\">{Num(mate.Wins)}</b> <span class=\"note\">· {Percent(mate.Wins, mate.Matches)}</span>" : Dash;
            html.Append($"<tr data-row><td data-v=\"{Esc(mate.Name.ToLowerInvariant())}\"><div class=\"person\">{Avatar(mate)}<span class=\"name\">{Esc(mate.Name)}</span></div></td>")
                .Append($"<td class=\"ac\" data-v=\"{mate.Matches}\">{Num(mate.Matches)}</td>")
                .Append($"<td class=\"ac nowrap\" data-v=\"{Raw(mate.Wins + mate.Wins / 1000.0 / mate.Matches)}\">{wins}</td>")
                .Append($"<td class=\"ac nowrap\" data-v=\"{mate.Chests}\"><span class=\"note\">≥</span> <b>{Num(mate.Chests)}</b></td>")
                .Append($"<td class=\"ac\" data-v=\"{Raw(average)}\"><b>{average.ToString("0.0", L.Culture)}</b></td>")
                .Append($"<td class=\"ac nowrap\" data-v=\"{Raw(share)}\"><span class=\"note\">≥</span> {Percent(mate.Chests, mate.TeamChests)}</td>")
                .Append($"<td class=\"ac nowrap note\" data-v=\"{mate.Last.Ticks}\">{L.DateTimeShort(mate.Last)}</td></tr>");
        }
        html.Append("</tbody>");
        if (hidden > 0)
        {
            var more = L.Count(hidden, "compañero", "compañeros", "teammate", "teammates");
            var line = mates.Any(m => m.Matches >= 2)
                ? L.F("Y {0} con una sola partida (relleno de equipo), no mostrados.", "And {0} with a single match (team fill), not shown.", more)
                : L.F("Y {0} más.", "And {0} more.", more);
            html.Append($"<tfoot><tr><td colspan=\"7\" class=\"note foot-note\">{line}</td></tr></tfoot>");
        }
        return html.Append("</table></div></section>").ToString();
    }

    /// <summary>A top-5 board with medals and a bar per teammate.</summary>
    private static string Board(string title, List<(Mate Mate, double Value)> entries)
    {
        if (entries.Count == 0)
        {
            return "";
        }
        var largest = Math.Max(0.0001, entries.Max(e => e.Value));
        var html = new StringBuilder($"<section class=\"stretch\"><div class=\"sec-head\"><h2>{title}</h2><span class=\"sec-meta\">Top 5</span></div><div class=\"sheet board board-one\"><ol>");
        for (var i = 0; i < entries.Count; i++)
        {
            var (mate, value) = entries[i];
            var position = i < Medals.Length ? $"<span class=\"pos pos-medal\" aria-label=\"{i + 1}\">{Medals[i]}</span>" : $"<span class=\"pos\">{i + 1}</span>";
            html.Append($"<li{(i == 0 ? " class=\"lead top\"" : "")}>{position}{Avatar(mate)}")
                .Append($"<span class=\"who-col\"><span class=\"name\" title=\"{Esc(mate.Name)}\">{Esc(mate.Name)}</span><span class=\"who-sub\">{L.Count(mate.Matches, "partida", "partidas", "match", "matches")}</span></span>")
                .Append($"<b><small>≥</small>{Num((int)value)}</b><span class=\"board-bar\"><i style=\"width:{Css(100.0 * value / largest)}\"></i></span></li>");
        }
        return html.Append("</ol></div></section>").ToString();
    }

    /// <summary>The initial on a colour picked from the teammate's id.</summary>
    private static string Avatar(Mate mate)
    {
        var tone = Tones[(int)((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(mate.Key) % Tones.Length)];
        var name = mate.Name.Trim();
        var initial = name.Length > 0 ? char.ToUpperInvariant(name[0]).ToString() : "?";
        return $"<span class=\"avatar avatar-s\" style=\"background:{tone}\">{Esc(initial)}</span>";
    }

    private static string Percent(int part, int whole) => whole > 0 ? (100.0 * part / whole).ToString("0", L.Culture) + " %" : "—";
}
