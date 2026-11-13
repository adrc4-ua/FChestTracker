using System.Runtime.Versioning;

namespace FCT.Monitoring;

/// <summary>Lo que muestra la ventana.</summary>
public sealed record MonitorState(OverlayView View, IReadOnlyList<Profile> Profiles, string? CurrentKey, UpdateInfo? Update, string? Notice, bool AutoCloseOffer, ChestStats Stats)
{
    /// <summary>Equipo de la partida en curso o de la última (null si «Compañeros» está desactivado).</summary>
    public TeamSummary? Team { get; init; }
}

/// <summary>Un compañero y los cofres que se le vieron abrir (un mínimo).</summary>
public sealed record TeamMember(string Name, int Chests);

/// <summary>
/// Equipo de una partida para la ventana principal.
/// <paramref name="Unknown"/>: partida analizada antes de guardar compañeros (sin datos de equipo).
/// </summary>
public sealed record TeamSummary(bool Live, int Mine, IReadOnlyList<TeamMember> Mates, bool Unknown)
{
    public int MatesTotal => Mates.Sum(m => m.Chests);
    public int Total => Mine + MatesTotal;
}

/// <summary>Estadísticas del perfil mostrado (la partida en curso cuenta ya en hoy y en la mejor partida).</summary>
/// <param name="Today">Cofres de hoy.</param>
/// <param name="BestDay">Día con más cofres, o null si no hay partidas.</param>
/// <param name="BestDayChests">Cofres de ese día.</param>
/// <param name="Average">Media de cofres por partida terminada.</param>
/// <param name="BestMatch">Cofres de la mejor partida.</param>
public sealed record ChestStats(int Matches, double Average, int BestMatch, int Treasures, int Today, DateTime? BestDay, int BestDayChests)
{
    public static readonly ChestStats Empty = new(0, 0, 0, 0, 0, null, 0);

    /// <summary>Cofres de los últimos 7 días, del más antiguo al de hoy (incluye la partida en curso).</summary>
    public IReadOnlyList<DayChests> LastDays { get; init; } = Array.Empty<DayChests>();

    /// <summary>Partidas guardadas en el histórico (todas, no solo las 100 últimas; sobreviven aunque Fortnite borre el replay).</summary>
    public int HistoryMatches { get; init; }

    /// <summary>Fecha local de la primera partida del histórico, o null si no hay ninguna.</summary>
    public DateTime? HistorySince { get; init; }

    /// <summary>Resumen de la última partida terminada del perfil mostrado, o null si no hay ninguna.</summary>
    public LastMatchSummary? LastMatch { get; init; }
}

/// <summary>Cifras de la última partida: cofres abiertos (incluidos los destruidos), tesoros, puesto y eliminaciones.</summary>
public sealed record LastMatchSummary(int Chests, int Destroyed, int Treasures, int? Placement, int? Players, int? Eliminations);

/// <summary>Cofres de un día concreto. <paramref name="Future"/>: el día aún no ha llegado (resto de la semana).</summary>
public sealed record DayChests(DateTime Day, int Chests, bool Future = false);
