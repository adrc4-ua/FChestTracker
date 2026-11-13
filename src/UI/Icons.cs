using FCT.Reporting;

namespace FCT.UI;

/// <summary>
/// Iconos de la app: los de la página de estadísticas (mismo dibujo) y unos pocos propios con el mismo trazo
/// (24 × 24, línea de 1,7 redondeada, relleno suave con class="f").
/// </summary>
internal static class Icons
{
    private const string Svg = """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">""";

    public static string Chest => ReportWriter.ChestIcon;
    public static string Summary => ReportWriter.TabIcons["resumen"];
    public static string Treasure => ReportWriter.GemIconBig;
    public static string Settings => ReportWriter.TabIcons["ajustes"];
    public static string Teammates => ReportWriter.TabIcons["companeros"];
    public static string Matches => ReportWriter.KpiIcons["matches"];
    public static string Average => ReportWriter.KpiIcons["avg"];
    public static string Record => ReportWriter.KpiIcons["best"];
    public static string Day => ReportWriter.KpiIcons["day"];
    public static string Wins => ReportWriter.KpiIcons["wins"];

    /// <summary>Overlay: una pantalla con el recuadro del contador en una esquina.</summary>
    public const string Overlay = Svg + """<rect class="f" x="3" y="4.5" width="18" height="12" rx="2.5"/><path d="M8.5 20h7M12 16.5V20"/><rect x="12.5" y="7" width="5.5" height="3.6" rx="1" fill="currentColor" stroke="none"/></svg>""";

    /// <summary>Perfil (cuenta de Epic).</summary>
    public const string Person = Svg + """<circle class="f" cx="12" cy="8" r="3.6"/><path class="f" d="M5 20a7 7 0 0 1 14 0z"/></svg>""";

    /// <summary>Hoy: un rayo de actividad.</summary>
    public const string Today = Svg + """<path class="f" d="M13.5 2.5 4.5 14h7l-1 7.5 9-11.5h-7z"/></svg>""";

    /// <summary>Gráfico semanal.</summary>
    public const string Week = Svg + """<rect class="f" x="4" y="12" width="4" height="8" rx="1"/><rect class="f" x="10" y="5" width="4" height="15" rx="1"/><rect class="f" x="16" y="9" width="4" height="11" rx="1"/></svg>""";

    /// <summary>Aviso o información.</summary>
    public const string Info = Svg + """<circle class="f" cx="12" cy="12" r="9"/><path d="M12 11v5.5M12 7.6v.1"/></svg>""";

    /// <summary>Descarga (actualización).</summary>
    public const string Download = Svg + """<path d="M12 3.5v11M7.5 10.5 12 15l4.5-4.5"/><path class="f" d="M4 15.5v2A2.5 2.5 0 0 0 6.5 20h11a2.5 2.5 0 0 0 2.5-2.5v-2"/></svg>""";

    /// <summary>Abrir fuera (página en el navegador).</summary>
    public const string External = Svg + """<path d="M14 4h6v6M20 4l-8.5 8.5M18 14v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4"/></svg>""";
}
