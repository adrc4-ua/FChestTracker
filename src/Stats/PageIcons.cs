namespace FCT.Stats;

/// <summary>
/// FCT's icon drawings (24 × 24, 1.7 gold stroke, soft fill on the "f" class), shared by the stats page and the app.
/// </summary>
internal static class PageIcons
{
    private const string IconSvg = """<svg class="ti" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private const string ChestShape = """<path class="f" d="M4 11V9.5A5.5 5.5 0 0 1 9.5 4h5A5.5 5.5 0 0 1 20 9.5V11z"/><rect x="3" y="11" width="18" height="9" rx="1.6"/><path d="M8.5 4.4V11M15.5 4.4V11M7 11v9M17 11v9"/><rect class="f" x="10" y="9.5" width="4" height="5" rx="1"/>""";
    public const string Chest = """<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""" + ChestShape + "</svg>";
    public static readonly Dictionary<string, string> Tabs = new()
    {
        ["resumen"] = IconSvg + """<rect class="f" x="3.5" y="3.5" width="7" height="9" rx="2"/><rect x="13.5" y="3.5" width="7" height="5" rx="2"/><rect x="13.5" y="11.5" width="7" height="9" rx="2"/><rect x="3.5" y="15.5" width="7" height="5" rx="2"/></svg>""",
        ["cofres"] = IconSvg + ChestShape + "</svg>",
        ["tesoros"] = IconSvg + """<path class="f" d="M3 9.5 6.5 4h11L21 9.5z"/><path d="M3 9.5 12 21l9-11.5M6.5 4 9 9.5 12 4l3 5.5L17.5 4M9 9.5 12 21l3-11.5"/></svg>""",
        ["partidas"] = IconSvg + """<path class="f" d="M7.5 7h9a5 5 0 0 1 4.7 6.7l-.9 2.6a2.4 2.4 0 0 1-4 .9L14.5 15h-5l-1.8 2.2a2.4 2.4 0 0 1-4-.9l-.9-2.6A5 5 0 0 1 7.5 7z"/><path d="M8 10v3.4M6.3 11.7h3.4"/><circle cx="15.5" cy="10.6" r=".5" fill="currentColor"/><circle cx="17.3" cy="12.6" r=".5" fill="currentColor"/></svg>""",
        ["companeros"] = IconSvg + """<circle class="f" cx="9" cy="8" r="3.5"/><path class="f" d="M2.5 20a6.5 6.5 0 0 1 13 0z"/><path d="M15.5 4.7a3.5 3.5 0 0 1 0 6.6M18 14a6.5 6.5 0 0 1 3.5 6"/></svg>""",
        ["ajustes"] = IconSvg + """<path d="M4 6.5h9M19 6.5h1M4 12h3M13 12h7M4 17.5h11M21 17.5h-1"/><circle class="f" cx="16" cy="6.5" r="2.5"/><circle class="f" cx="10" cy="12" r="2.5"/><circle class="f" cx="17.5" cy="17.5" r="2.5"/></svg>""",
    };

    // Figures of the overview.
    private const string KpiSvg = """<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    public static readonly Dictionary<string, string> Figures = new()
    {
        ["wins"] = KpiSvg + """<path class="f" d="M7 4h10v5a5 5 0 0 1-10 0z"/><path d="M8.5 21h7M12 14v7M17 5.5h3V7a3.5 3.5 0 0 1-3.3 3.5M7 5.5H4V7a3.5 3.5 0 0 0 3.3 3.5"/></svg>""",
        ["matches"] = KpiSvg + """<path class="f" d="M7.5 7h9a5 5 0 0 1 4.7 6.7l-.9 2.6a2.4 2.4 0 0 1-4 .9L14.5 15h-5l-1.8 2.2a2.4 2.4 0 0 1-4-.9l-.9-2.6A5 5 0 0 1 7.5 7z"/><path d="M8 10v3.4M6.3 11.7h3.4"/></svg>""",
        ["avg"] = KpiSvg + """<rect class="f" x="4" y="12" width="4" height="8" rx="1"/><rect class="f" x="10" y="5" width="4" height="15" rx="1"/><rect class="f" x="16" y="9" width="4" height="11" rx="1"/></svg>""",
        ["best"] = KpiSvg + """<path class="f" d="m12 3 2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/></svg>""",
        ["day"] = KpiSvg + """<rect x="3.5" y="5" width="17" height="15.5" rx="2.5"/><path class="f" d="M3.5 7.5A2.5 2.5 0 0 1 6 5h12a2.5 2.5 0 0 1 2.5 2.5V10h-17z"/><path d="M8 3v4M16 3v4"/><rect x="7" y="13" width="3" height="3" rx=".6" fill="currentColor" stroke="none"/></svg>""",
    };

    // Treasure-hunt rewards: one per kind, and one per gizmo.
    private const string ItemSvg = """<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">""";
    private static readonly Dictionary<Prizes.Kind, string> KindIcons = new()
    {
        [Prizes.Kind.Outfit] = ItemSvg + """<path class="f" d="M8.5 4 4 6.6l1.6 3.8 1.9-.8V20h9V9.6l1.9.8L20 6.6 15.5 4a3.5 3.5 0 0 1-7 0z"/></svg>""",
        [Prizes.Kind.Pickaxe] = ItemSvg + """<path class="f" d="M3.5 9C7 4.6 17 4.6 20.5 9 16.6 7.6 7.4 7.6 3.5 9z"/><path d="M12 7.2V21"/></svg>""",
        [Prizes.Kind.BackBling] = ItemSvg + """<rect class="f" x="5" y="7" width="14" height="13.5" rx="3.5"/><path d="M9 7V5.5a3 3 0 0 1 6 0V7M8.5 13.5h7v3.5h-7z"/></svg>""",
        [Prizes.Kind.Vehicle] = ItemSvg + """<path class="f" d="M3 16v-3l2.2-4.5a2 2 0 0 1 1.8-1.1h8.4a2 2 0 0 1 1.5.7L20 12l1 .5V16z"/><circle cx="7.5" cy="16.5" r="2" fill="#121215"/><circle cx="16.5" cy="16.5" r="2" fill="#121215"/><path d="M6 12h13"/></svg>""",
        [Prizes.Kind.Decal] = ItemSvg + """<path class="f" d="M5 4h14v10l-6 6H5z"/><path d="M13 20v-4a2 2 0 0 1 2-2h4"/></svg>""",
        [Prizes.Kind.JamTrack] = ItemSvg + """<path d="M9 17.5V5.5l11-2v12"/><circle class="f" cx="6.5" cy="17.5" r="2.5"/><circle class="f" cx="17.5" cy="15.5" r="2.5"/></svg>""",
        [Prizes.Kind.LoadingScreen] = ItemSvg + """<rect class="f" x="3" y="5" width="18" height="14" rx="2.5"/><path d="m3.5 16.5 5-5 4 4 3-3 5 5"/><circle cx="16" cy="9.5" r="1.4"/></svg>""",
        [Prizes.Kind.ShopAccess] = ItemSvg + """<path class="f" d="M4.5 8h15l-1.2 12.5H5.7z"/><path d="M9 10V6.5a3 3 0 0 1 6 0V10"/></svg>""",
    };
    private static readonly Dictionary<string, string> GizmoIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Golden Firework"] = ItemSvg + """<path d="M12 3v3.5M12 17.5V21M3 12h3.5M17.5 12H21M5.6 5.6l2.5 2.5M15.9 15.9l2.5 2.5M5.6 18.4l2.5-2.5M15.9 8.1l2.5-2.5"/><circle class="f" cx="12" cy="12" r="2.8"/></svg>""",
        ["Portable Extractor"] = ItemSvg + """<rect class="f" x="7" y="7" width="10" height="13.5" rx="2.5"/><path d="M10 7V3.5h4V7M7 12h10M12 15v2"/></svg>""",
        ["Sprite Locator"] = ItemSvg + """<circle cx="12" cy="12" r="8.5"/><path class="f" d="m15.6 8.4-2.1 5.1-5.1 2.1 2.1-5.1z"/></svg>""",
        ["Lucky Locator"] = ItemSvg + """<circle class="f" cx="9" cy="9" r="3"/><circle class="f" cx="15" cy="9" r="3"/><circle class="f" cx="9" cy="15" r="3"/><circle class="f" cx="15" cy="15" r="3"/><path d="m15.5 15.5 4 5"/></svg>""",
        ["Extraction Accelerator"] = ItemSvg + """<path class="f" d="M13.5 2.5 4.5 14h7l-1 7.5 9-11.5h-7z"/></svg>""",
    };
    public const string Gem = ItemSvg + """<path class="f" d="M3 9.5 6.5 4h11L21 9.5z"/><path d="M3 9.5 12 21l9-11.5M9 9.5 12 21l3-11.5"/></svg>""";

    /// <summary>A treasure's icon from its name: its gizmo's, or its kind's; the gem when unknown.</summary>
    public static string Item(string name) => Prizes.ByName(name) is { } prize ? Of(prize) : Gem;

    /// <summary>The icon of a catalogued prize.</summary>
    public static string Of(Prizes.Prize prize) =>
        prize.Type == Prizes.Kind.Gizmo ? GizmoIcons.GetValueOrDefault(prize.Name, Gem) : KindIcons.GetValueOrDefault(prize.Type, Gem);

    public const string SmallGem = """<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h12l3 6-9 12L3 9z"/><path d="M3 9h18"/></svg>""";

    public static readonly Dictionary<string, string> Settings = new()
    {
        ["lang"] = KpiSvg + """<circle class="f" cx="12" cy="12" r="8.5"/><path d="M3.5 12h17M12 3.5c2.6 2.4 3.6 5.4 3.6 8.5s-1 6.1-3.6 8.5M12 3.5C9.4 5.9 8.4 8.9 8.4 12s1 6.1 3.6 8.5"/></svg>""",
        ["mates"] = KpiSvg + """<circle class="f" cx="9" cy="8" r="3.5"/><path class="f" d="M2.5 20a6.5 6.5 0 0 1 13 0z"/><path d="M15.5 4.7a3.5 3.5 0 0 1 0 6.6M18 14a6.5 6.5 0 0 1 3.5 6"/></svg>""",
        ["sum"] = KpiSvg + """<circle class="f" cx="12" cy="12" r="8.5"/><path d="M12 8v8M8 12h8"/></svg>""",
        ["ranked"] = KpiSvg + """<rect class="f" x="9" y="7" width="6" height="13.5" rx="1"/><rect x="3" y="12" width="6" height="8.5" rx="1"/><rect x="15" y="14.5" width="6" height="6" rx="1"/><path d="m12 2.5.9 1.8 2 .3-1.4 1.4.3 2-1.8-.9-1.8.9.3-2-1.4-1.4 2-.3z"/></svg>""",
        ["ammo"] = KpiSvg + """<path class="f" d="M5 10.5a2 2 0 0 1 4 0V20H5zM10 8.5a2 2 0 0 1 4 0V20h-4zM15 10.5a2 2 0 0 1 4 0V20h-4z"/><path d="M4 20h16"/></svg>""",
        ["food"] = KpiSvg + """<path class="f" d="M12 7.5c-2-1.6-7-1.2-7 4.8 0 4 2.9 8.2 5 8.2 1 0 1.4-.5 2-.5s1 .5 2 .5c2.1 0 5-4.2 5-8.2 0-6-5-6.4-7-4.8z"/><path d="M12 7.5c0-2 1-3.5 3-4.3"/></svg>""",
        ["cannons"] = KpiSvg + """<circle class="f" cx="8" cy="16" r="4"/><path d="m10.5 13 8-6.5 2.2 3-8.4 6"/><path d="M4 20h8"/></svg>""",
    };
}
