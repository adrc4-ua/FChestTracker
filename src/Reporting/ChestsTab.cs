using System.Text;
using static FCT.Reporting.PageParts;

namespace FCT.Reporting;

/// <summary>The "Cofres" tab: chests by kind, the other containers, and chests by game mode.</summary>
internal static class ChestsTab
{
    public static string Build(PageData page)
    {
        var html = new StringBuilder(Head("cofres", L.T("Cofres", "Chests"), L.T("Por tipo de cofre y modo de juego", "By chest type and game mode")));
        html.Append("<div class=\"cols2\">").Append(ByKind(page.Matches)).Append(OtherContainers(page.Matches, page.ExtraKinds)).Append("</div>");
        html.Append(ByMode(page.Matches));
        return html.ToString();
    }

    private static string ByKind(List<MatchRecord> matches)
    {
        var kinds = matches.SelectMany(m => m.Chests).GroupBy(c => c.Kind).Select(k => (Kind: k.Key, Count: k.Count())).OrderByDescending(k => k.Count).ToList();
        var total = kinds.Sum(k => k.Count);
        var largest = kinds.Count == 0 ? 1 : Math.Max(1, kinds.Max(k => k.Count));
        var rows = new StringBuilder();
        if (kinds.Count == 0)
        {
            rows.Append($"<div class=\"void\">{L.T("Aún no hay cofres registrados.", "No chests recorded yet.")}</div>");
        }
        foreach (var (kind, count) in kinds)
        {
            var share = (100.0 * count / Math.Max(1, total)).ToString("0", L.Culture);
            rows.Append(Bar(Esc(L.Name(kind)), $"{Num(count)}<small>{share} %</small>", 100.0 * count / largest));
        }
        var broken = matches.Sum(m => m.Chests.Count(c => c.Destroyed));
        if (broken > 0)
        {
            rows.Append($"<div class=\"bar bar-sub\"><span>{L.T("de ellos, destruidos en vez de abiertos", "of which destroyed instead of opened")}</span><span class=\"bar-n\">{Num(broken)}</span></div>");
        }
        return Section(L.T("Por tipo de cofre", "By chest type"), L.Count(total, "cofre", "cofres", "chest", "chests"),
            $"<div class=\"sheet sheet-pad\"><div class=\"bars\">{rows}</div></div>");
    }

    /// <summary>Ammo boxes, food boxes and loot cannons, each saying whether the settings add it to the total.</summary>
    private static string OtherContainers(List<MatchRecord> matches, IReadOnlyCollection<string> counted)
    {
        var kinds = matches
            .SelectMany(m => m.OtherInteractions.Count > 0
                ? m.OtherInteractions
                : new Dictionary<string, int> { [ChestKinds.AmmoBox] = m.AmmoBoxesOpened, [ChestKinds.OtherContainer] = m.OtherContainersOpened }) // early matches
            .GroupBy(e => e.Key)
            .Select(k => (Kind: k.Key, Count: k.Sum(e => e.Value)))
            .Where(k => k.Count > 0)
            .OrderByDescending(k => k.Count)
            .ToList();
        var largest = kinds.Count == 0 ? 1 : Math.Max(1, kinds.Max(k => k.Count));
        var rows = new StringBuilder();
        if (kinds.Count == 0)
        {
            rows.Append($"<div class=\"void\">{L.T("Aún no hay cajas ni cañones registrados.", "No boxes or cannons recorded yet.")}</div>");
        }
        foreach (var (kind, count) in kinds)
        {
            var added = counted.Contains(kind);
            var pill = added ? $"<span class=\"pill pill-top\">{L.T("suma al total", "added to the total")}</span>" : $"<span class=\"pill\">{L.T("fuera del total", "not in the total")}</span>";
            rows.Append(Bar($"{Esc(L.Name(kind))} {pill}", Num(count), 100.0 * count / largest, added ? null : "bar-off", labelHasPill: true));
        }
        return Section(L.T("Otros contenedores", "Other containers"), L.T("se activan en Configuración", "turned on in Settings"),
            $"<div class=\"sheet sheet-pad\"><div class=\"bars\">{rows}</div></div>");
    }

    private static string ByMode(List<MatchRecord> matches)
    {
        var modes = matches.GroupBy(m => Pretty.Playlist(m.Playlist))
            .Select(mode => (Name: mode.Key, Matches: mode.Count(), Chests: mode.Sum(m => m.ChestsOpened), Best: mode.Max(m => m.ChestsOpened), Treasures: mode.Sum(m => m.TreasureFinds.Count)))
            .OrderByDescending(mode => mode.Matches)
            .ToList();
        if (modes.Count == 0)
        {
            return "";
        }
        var bestAverage = Math.Max(0.1, modes.Max(mode => (double)mode.Chests / mode.Matches));
        var table = new StringBuilder("<div class=\"sheet scrollx\"><table><thead><tr>")
            .Append($"<th>{L.T("Modo", "Mode")}</th><th class=\"ac\">{L.T("Partidas", "Matches")}</th><th class=\"ac\">{L.T("Media", "Average")}</th>")
            .Append($"<th class=\"ac\">{L.T("Récord", "Best")}</th><th class=\"ac\">{L.T("Cofres", "Chests")}</th><th class=\"ac\">{L.T("Tesoros", "Treasures")}</th></tr></thead><tbody>");
        foreach (var mode in modes)
        {
            var average = (double)mode.Chests / mode.Matches;
            var gems = mode.Treasures > 0 ? $"<span class=\"gems\">{Num(mode.Treasures)}{PageIcons.SmallGem}</span>" : Dash;
            table.Append($"<tr><td><span class=\"pill pill-mode\">{Esc(mode.Name)}</span></td><td class=\"ac\">{Num(mode.Matches)}</td>")
                .Append($"<td><div class=\"count\"><span class=\"count-bar\"><i style=\"width:{Css(100.0 * average / bestAverage)}\"></i></span><b>{average.ToString("0.0", L.Culture)}</b></div></td>")
                .Append($"<td class=\"ac\">{Num(mode.Best)}</td><td class=\"ac\">{Num(mode.Chests)}</td><td class=\"ac\">{gems}</td></tr>");
        }
        table.Append("</tbody></table></div>");
        return Section(L.T("Por modo de juego", "By game mode"), L.Count(modes.Count, "modo", "modos", "mode", "modes"), table.ToString(), "fit");
    }
}
