namespace FCT.Data;

/// <summary>
/// A squad mate in one match (data.json format). The replay only carries what happens around the person recording,
/// so the counts here are what was seen, which can be less than what the teammate really did.
/// </summary>
public sealed class TeammateRecord
{
    public string Name { get; set; } = "";
    public string? EpicId { get; set; }

    /// <summary>Chests seen searched or broken by this teammate.</summary>
    public int ChestsSeen { get; set; }

    /// <summary>Every kind of container seen, chests included, keyed like <see cref="ChestKinds"/>.</summary>
    public Dictionary<string, int> Kinds { get; set; } = new();
}
