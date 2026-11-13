namespace FCT.Data;

/// <summary>One container the player searched or broke, with what came out of it (data.json format).</summary>
public sealed class ChestRecord
{
    /// <summary>Replay time of the search, in seconds from the start of the recording.</summary>
    public float TimeSeconds { get; set; }

    /// <summary>Kind shown to the player (chest, rare chest…; see <see cref="ChestKinds"/>).</summary>
    public string Kind { get; set; } = "";

    /// <summary>Game blueprint of the container, kept to tell kinds apart when the game adds new ones.</summary>
    public string Blueprint { get; set; } = "";

    /// <summary>Broken instead of searched (it still counts).</summary>
    public bool Destroyed { get; set; }

    public List<string> Items { get; set; } = new();

    /// <summary>Treasure-hunt items among <see cref="Items"/>.</summary>
    public List<string> TreasureItems { get; set; } = new();
}
