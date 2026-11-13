namespace FCT.Overlay;

/// <summary>
/// Everything the overlay draws, already formatted (so the same view feeds the game overlay, the OBS page and the
/// previews).
/// </summary>
/// <param name="Total">The big number: chests in total.</param>
/// <param name="Current">The second number (match in progress or the last one); null draws a dash.</param>
/// <param name="CurrentCaption">Caption under <paramref name="Current"/>.</param>
/// <param name="CurrentLive">The second number is a match being played (drawn in mint).</param>
/// <param name="Badge">Small header label such as "● EN DIRECTO", or null.</param>
/// <param name="Rows">The figures under the numbers.</param>
public sealed record OverlayView(string Total, string? Current, string CurrentCaption, bool CurrentLive, string? Badge, IReadOnlyList<OverlayRow> Rows);

/// <summary>One figure of the overlay. <paramref name="Detail"/>: optional small grey text after the value.</summary>
public sealed record OverlayRow(string Label, string Value, bool Treasure = false, string? Detail = null);
