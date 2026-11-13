namespace FCT.Analysis;

/// <summary>
/// The kinds of searchable things FCT tells apart, and how a game blueprint maps to one. The values are saved in
/// data.json, so they never change; a new kind only needs a constant and a rule.
/// </summary>
public static class ChestKinds
{
    // Chests: they make the chest total.
    public const string Normal = "Chest";
    public const string Rare = "Rare chest";
    public const string Buried = "Buried chest";
    public const string Extraction = "Extraction / relic chest";
    public const string PosterChest = "Spirit poster chest";
    public const string Other = "Other chest";

    // Everything else that can be searched: kept apart, added to the total only if the player asks.
    public const string Cannon = "Loot cannon";
    public const string AmmoBox = "Ammo box";
    public const string FoodBox = "Food box";
    public const string OtherContainer = "Other container";

    private static readonly HashSet<string> Chests = new() { Normal, Rare, Buried, Extraction, PosterChest, Other };

    /// <summary>
    /// First rule whose fragments appear in the (lower-case) blueprint wins, so the order matters: cannons and ammo
    /// before anything named "chest", rare before the generic chest names.
    /// </summary>
    private static readonly (string Kind, string[] Fragments)[] Rules =
    {
        (Cannon, new[] { "lootcannon", "cannon" }),
        (AmmoBox, new[] { "ammo" }),
        (FoodBox, new[] { "foodbox", "food_box" }),
        (Rare, new[] { "rarechest", "legendary", "rare_chest" }),
        (Buried, new[] { "buried" }),
        (PosterChest, new[] { "spriteposter", "booty" }),
        (Extraction, new[] { "relic", "extraction" }),
        (Normal, new[] { "tiered_chest", "normalchest", "standardchest" }),
        (Other, new[] { "chest" }), // any other chest; coolers, barrels and the like fall through
    };

    public static bool IsChest(string kind) => Chests.Contains(kind);

    public static string Classify(string blueprint)
    {
        var name = blueprint.ToLowerInvariant();
        foreach (var (kind, fragments) in Rules)
        {
            if (Array.Exists(fragments, name.Contains))
            {
                return kind;
            }
        }
        return OtherContainer;
    }
}
