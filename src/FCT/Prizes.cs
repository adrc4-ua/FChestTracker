using System.Text.RegularExpressions;

namespace FCT;

/// <summary>
/// Rewards of the treasure hunt ("Treasure Hunters", Chapter 7, 1 Nov – 5 Dec 2026): every chest has a small chance
/// to pay one out to the whole squad. Two families:
/// <list type="bullet">
/// <item>Featured treasures: the weekly cosmetics (outfit, pickaxe, vehicle…) and the Pirate Shop Access. The server
/// sends their prize id ("DelilahPrize:dpid_item3") with an animated chest, so they are named here.</item>
/// <item>Treasure gizmos: the five gizmos, in every week. The replay does not say which one was received.</item>
/// </list>
/// Names are the official English ones (also used by the Spanish game for these items). Weeks 3 and 4 only list the
/// vehicle announced so far; week 5 is not announced yet.
/// </summary>
public static class Prizes
{
    public enum Kind { Outfit, Pickaxe, BackBling, Vehicle, Decal, JamTrack, LoadingScreen, ShopAccess, Gizmo }

    /// <param name="Week">1–5, or 0 for every week.</param>
    /// <param name="Id">Prize id seen in replays (only for the ones already received).</param>
    /// <param name="Announced">False when only reported by the community, not yet in the game.</param>
    public sealed record Prize(string Name, Kind Type, int Week, string? Id = null, bool Announced = true);

    /// <summary>Start (local date) of each week of the event; it ends on <see cref="End"/>.</summary>
    public static readonly DateTime[] WeekStarts =
    {
        new(2026, 11, 1), new(2026, 11, 7), new(2026, 11, 14), new(2026, 11, 21), new(2026, 11, 28),
    };
    public static readonly DateTime End = new(2026, 12, 5);

    public static readonly Prize[] All =
    {
        // Every week
        new("Pirate Shop Access", Kind.ShopAccess, 0),
        new("Golden Firework", Kind.Gizmo, 0),
        new("Portable Extractor", Kind.Gizmo, 0),
        new("Sprite Locator", Kind.Gizmo, 0),
        new("Lucky Locator", Kind.Gizmo, 0),
        new("Extraction Accelerator", Kind.Gizmo, 0),
        // Week 1
        new("Solid Gold A$AP Rocky", Kind.Outfit, 1),
        new("Solid Gold A$AP Rocky Gauntlets", Kind.Pickaxe, 1, "DelilahPrize:dpid_item1"),
        new("Plunder in Paradise", Kind.LoadingScreen, 1, "DelilahPrize:dpid_item2"),
        new("Mercedes-AMG G 63 'G-Wagon'", Kind.Vehicle, 1, "DelilahPrize:dpid_item3"),
        new("No Trespassing", Kind.JamTrack, 1, "DelilahPrize:dpid_item4"),
        new("Golden Llama Crew", Kind.Decal, 1),
        // Week 2. The prize ids are slots, the same every week (item1 pickaxe / back bling, item2 loading screen,
        // item3 vehicle, item4 jam track): the item behind each one changes with the week.
        new("Solid Gold Lil Uzi Vert", Kind.Outfit, 2),
        new("Diamond and Gold", Kind.BackBling, 2, "DelilahPrize:dpid_item1"),
        new("So Kawaii", Kind.LoadingScreen, 2, "DelilahPrize:dpid_item2"),
        new("Cadillac CT5-V Blackwing", Kind.Vehicle, 2, "DelilahPrize:dpid_item3"),
        new("London", Kind.JamTrack, 2, "DelilahPrize:dpid_item4"),
        // Weeks 3 and 4: the gold car of the week, as reported (the rest of the week is not known yet)
        new("Chevrolet Corvette ZR1", Kind.Vehicle, 3, "DelilahPrize:dpid_item3", Announced: false),
        new("Lamborghini Urus SE", Kind.Vehicle, 4, "DelilahPrize:dpid_item3", Announced: false),
    };

    /// <summary>Week of the event (1–5) of a local date, or 0 outside the event.</summary>
    public static int WeekOf(DateTime local)
    {
        var d = local.Date;
        if (d < WeekStarts[0] || d >= End)
        {
            return 0;
        }
        var week = 0;
        for (var i = 0; i < WeekStarts.Length; i++)
        {
            if (d >= WeekStarts[i]) week = i + 1;
        }
        return week;
    }

    /// <summary>Prize behind an id in a week of the event (0: the week is not known, the first one that matches).</summary>
    public static Prize? ById(string? id, int week = 0) =>
        id == null ? null : All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase) && (week == 0 || p.Week == 0 || p.Week == week));
    public static Prize? ByName(string? name) =>
        name == null ? null : All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public static string TypeName(Kind kind) => kind switch
    {
        Kind.Outfit => L.T("Traje", "Outfit"),
        Kind.Pickaxe => L.T("Pico", "Pickaxe"),
        Kind.BackBling => L.T("Mochila", "Back Bling"),
        Kind.Vehicle => L.T("Vehículo", "Vehicle"),
        Kind.Decal => L.T("Adhesivo de vehículo", "Vehicle decal"),
        Kind.JamTrack => L.T("Pista de improvisación", "Jam Track"),
        Kind.LoadingScreen => L.T("Pantalla de carga", "Loading Screen"),
        Kind.ShopAccess => L.T("Acceso a la tienda", "Item Shop access"),
        _ => L.T("Artilugio", "Gizmo"),
    };

    public static string Featured => L.T("Tesoro destacado", "Featured treasure");

    // A name from treasures.json may carry its type in brackets: "Name (type)".
    private static readonly Regex NameAndType = new(@"^\s*(?<name>.*?)\s*(\((?<type>[^()]*)\))?\s*\??\s*$", RegexOptions.Compiled);

    /// <summary>Display of one treasure: clean name, type (or null) and whether it is a featured treasure.</summary>
    public static (string Name, string? Type, bool Featured) Describe(TreasureFind f, DateTime? matchLocal = null)
    {
        var week = matchLocal is DateTime d ? WeekOf(d) : 0;
        var featured = f.PrizeId != null && !TreasureNames.IsGizmoKey(f.PrizeId);
        if (featured)
        {
            // The player's own name in treasures.json wins; then the catalogue; else a generic name.
            if (TreasureNames.PrizeNames.TryGetValue(f.PrizeId!, out var custom) && !string.IsNullOrWhiteSpace(custom))
            {
                var m = NameAndType.Match(custom);
                var name = m.Groups["name"].Value;
                var known = ByName(name);
                return (name.Length > 0 ? name : custom.Trim(), known != null ? TypeName(known.Type) : NullIfEmpty(m.Groups["type"].Value), true);
            }
            return ById(f.PrizeId, week) is { } p ? (p.Name, TypeName(p.Type), true) : (Featured, null, true);
        }

        // Gizmo: named when the item is known (older analyses could see it), else the generic name.
        var custom2 = f.PrizeId != null && TreasureNames.PrizeNames.TryGetValue(f.PrizeId, out var n2) && !string.IsNullOrWhiteSpace(n2) ? n2.Trim() : null;
        if (custom2 != null)
        {
            return (custom2, TypeName(Kind.Gizmo), false);
        }
        var item = TreasureNames.GizmoName(f.Item);
        return ByName(item) is { Type: Kind.Gizmo } g ? (g.Name, TypeName(Kind.Gizmo), false) : (L.Name(TreasureNames.UnknownGizmo), null, false);
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
