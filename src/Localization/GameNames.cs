namespace FCT.Localization;

/// <summary>
/// data.json stores game things under fixed English keys (container kinds, treasure gizmos); this is how each one is
/// shown. Spanish names follow the game: Epic's patch notes for "Extractor portátil" and "Localizador de la suerte",
/// and the game's "espíritu" for "Sprite" in the rest. Lookups ignore case; a key that is not here is shown as it is.
/// </summary>
internal static class GameNames
{
    private static readonly Dictionary<string, (string Es, string En)> Shown = new(StringComparer.OrdinalIgnoreCase)
    {
        // Containers (chapter 7 names). "Extraction / relic chest" is the Sprite chest: "relic" in the game files.
        ["Chest"] = ("Cofre", "Chest"),
        ["Rare chest"] = ("Cofre raro", "Rare Chest"),
        ["Buried chest"] = ("Cofre enterrado", "Buried Chest"),
        ["Extraction / relic chest"] = ("Cofre de espíritu", "Sprite Chest"),
        ["Spirit poster chest"] = ("Cofre del cartel de espíritu", "Sprite Poster Chest"),
        ["Other chest"] = ("Otro cofre", "Other chest"),
        ["Loot cannon"] = ("Cañón de botín", "Loot cannon"),
        ["Ammo box"] = ("Caja de munición", "Ammo box"),
        ["Food box"] = ("Caja de comida", "Food box"),
        ["Other container"] = ("Otro contenedor", "Other container"),

        // Treasures.
        ["Treasure"] = ("Tesoro", "Treasure"),
        ["Treasure (unknown type)"] = ("Tesoro (tipo desconocido)", "Treasure (unknown type)"),
        ["Treasure gizmo"] = ("Artilugio de tesoro", "Treasure gizmo"),
        ["Golden Firework"] = ("Fuegos artificiales dorados", "Golden Firework"),
        ["Portable Extractor"] = ("Extractor portátil", "Portable Extractor"),
        ["Sprite Locator"] = ("Localizador de espíritus", "Sprite Locator"),
        ["Lucky Locator"] = ("Localizador de la suerte", "Lucky Locator"),
        ["Extraction Accelerator"] = ("Acelerador de extracción", "Extraction Accelerator"),
        ["unknown"] = ("Desconocido", "Unknown"),
    };

    public static bool TryGet(string key, out (string Es, string En) names) => Shown.TryGetValue(key, out names);
}
