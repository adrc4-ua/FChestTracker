using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

/// <summary>
/// A teammate of the tracked player in one match. Only what happens near the recording player is in the replay
/// (the game does not send what happens far away), so <see cref="ChestsSeen"/> is a minimum, never a total.
/// </summary>
public sealed class TeammateRecord
{
    public string Name { get; set; } = "";
    public string? EpicId { get; set; }
    /// <summary>Chests this teammate was seen opening or destroying.</summary>
    public int ChestsSeen { get; set; }
    /// <summary>Everything they were seen searching, by kind (chests, ammo boxes, food boxes, loot cannons).</summary>
    public Dictionary<string, int> Kinds { get; set; } = new();
}
