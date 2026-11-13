using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace FCT.Localization;

/// <summary>
/// Idioma de la aplicación. Por defecto el de Windows si está disponible (si no, inglés); se puede elegir otro en
/// Ajustes. Español e inglés se escriben en el código: <c>L.T("español", "English")</c>. Cualquier otro idioma es un
/// archivo <c>Localization/Languages/&lt;código&gt;.json</c> que va dentro del exe y aparece solo en Ajustes; lo que no
/// esté traducido sale en inglés. Cómo añadir uno: <c>Localization/Languages/README.md</c>.
/// Los nombres guardados en data.json (tipos de cofre, objetos del tesoro) están en inglés y se traducen al mostrarlos (<see cref="Name"/>).
/// </summary>
public static class L
{
    /// <summary>Un idioma añadido como archivo: nombre propio, cultura (fechas y números), ordinal y textos.</summary>
    private sealed record Pack(string Code, string Name, CultureInfo Culture, string Ordinal, Dictionary<string, string> Texts);

    private const string PackPrefix = "FCT.lang.";

    // Va antes que todo lo demás: Code y Languages lo necesitan al inicializarse.
    private static readonly Dictionary<string, Pack> Packs = LoadPacks();

    /// <summary>Idiomas que se pueden elegir en Ajustes, con su nombre en ese mismo idioma.</summary>
    public static IReadOnlyList<(string Code, string Name)> Languages { get; } =
        new[] { ("es", "Español"), ("en", "English") }
            .Concat(Packs.Values.OrderBy(p => p.Name, StringComparer.InvariantCultureIgnoreCase).Select(p => (p.Code, p.Name)))
            .ToList();

    /// <summary>
    /// Código del idioma actual ("es", "en" o el de un archivo de idioma). Por defecto el de Windows; <see cref="Use"/>
    /// aplica el elegido en Ajustes. Para probar: la variable FCT_LANG fuerza el idioma por encima de todo.
    /// </summary>
    public static string Code => _scope?.Code ?? _code;

    /// <summary>True cuando la interfaz va en inglés.</summary>
    public static bool En => Code == "en";

    public static CultureInfo Culture => _scope?.Culture ?? _culture;

    private static Pack? Current => _scope != null ? _scope.Pack : _current;

    private static string _code = Detect();
    private static CultureInfo _culture = CultureOf(_code);
    private static Pack? _current = Packs.GetValueOrDefault(_code);

    /// <summary>Idioma solo para este hilo mientras dura <see cref="Scope"/> (el Resumen puede ir en otro idioma que la app).</summary>
    private sealed record Scoped(string Code, CultureInfo Culture, Pack? Pack);
    [ThreadStatic] private static Scoped? _scope;

    /// <summary>
    /// Escribe en otro idioma lo que se genere en este hilo hasta que se libere (null o desconocido = el de la app).
    /// Lo usa el Resumen, que tiene su propio idioma en su Configuración.
    /// </summary>
    public static IDisposable Scope(string? code)
    {
        var previous = _scope;
        _scope = code != null && IsAvailable(code) ? new Scoped(code, CultureOf(code), Packs.GetValueOrDefault(code)) : null;
        return new Restore(previous);
    }

    private sealed class Restore(Scoped? previous) : IDisposable
    {
        public void Dispose() => _scope = previous;
    }

    /// <summary>Idioma de Windows (o el forzado con FCT_LANG), sin tener en cuenta lo elegido en Ajustes.</summary>
    public static string SystemCode => Detect();

    /// <summary>
    /// Aplica el idioma elegido en Ajustes (null o desconocido = el de Windows). Se llama al arrancar, antes de crear
    /// las ventanas: los textos se leen al construirlas, así que un cambio necesita reiniciar el contador.
    /// </summary>
    public static void Use(string? code)
    {
        var chosen = Environment.GetEnvironmentVariable("FCT_LANG") is { Length: > 0 } || code == null || !IsAvailable(code)
            ? Detect()
            : code;
        _code = chosen;
        _culture = CultureOf(chosen);
        _current = Packs.GetValueOrDefault(chosen);
    }

    private static bool IsAvailable(string code) => code is "es" or "en" || Packs.ContainsKey(code);

    private static string Detect()
    {
        var code = Environment.GetEnvironmentVariable("FCT_LANG") is { Length: > 0 } forced
            ? forced
            : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        code = code.Length >= 2 ? code[..2].ToLowerInvariant() : "";
        return IsAvailable(code) ? code : "en";
    }

    private static CultureInfo CultureOf(string code) => code switch
    {
        "es" => CultureInfo.GetCultureInfo("es-ES"),
        "en" => CultureInfo.GetCultureInfo("en-US"),
        _ => Packs.TryGetValue(code, out var pack) ? pack.Culture : CultureInfo.GetCultureInfo("en-US"),
    };

    /// <summary>Texto en el idioma actual: <paramref name="es"/> en español, <paramref name="en"/> en inglés y la traducción en los demás.</summary>
    public static string T(string es, string en) => Pick(es, es, en);

    /// <summary>Texto largo: en los archivos de idioma su clave es "#" + <paramref name="id"/> en vez del texto entero.</summary>
    public static string Big(string id, string es, string en) => Pick("#" + id, es, en);

    /// <summary>Texto con huecos {0}, {1}… (formato de <c>string.Format</c>), traducido como <see cref="T"/>.</summary>
    public static string F(string es, string en, params object?[] args) => string.Format(Culture, T(es, en), args);

    private static string Pick(string key, string es, string en) => Code switch
    {
        "es" => es,
        "en" => en,
        _ => Current != null && Current.Texts.TryGetValue(key, out var text) && text.Length > 0 ? text : en,
    };

    public static string N(int n) => n.ToString("0", Culture);

    /// <summary>"3 cofres" / "1 chest" (plural según la cifra, en el idioma actual).</summary>
    public static string Count(int n, string esOne, string esMany, string enOne, string enMany) =>
        $"{N(n)} {(n == 1 ? T(esOne, enOne) : T(esMany, enMany))}";

    /// <summary>"12.º" / "12th"; en los demás idiomas, el formato "ordinal" de su archivo ("{0}." si no tiene).</summary>
    public static string Ordinal(int n) => Code switch
    {
        "es" => $"{n}.º",
        "en" => n + EnglishSuffix(n),
        _ => string.Format(Culture, Current?.Ordinal ?? "{0}.", n),
    };

    private static string EnglishSuffix(int n) =>
        (n % 100) is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

    /// <summary>Fecha corta + hora de una partida ("05/11 22:54" / "Nov 5 22:54").</summary>
    public static string DateTimeShort(DateTime d) => d.ToString(En ? "MMM d HH:mm" : "dd/MM HH:mm", Culture);

    /// <summary>
    /// Lee los archivos de idioma que van dentro del exe (Localization/Languages/*.json). Uno mal escrito se ignora:
    /// la app sigue en español o inglés en vez de no arrancar.
    /// </summary>
    private static Dictionary<string, Pack> LoadPacks()
    {
        var packs = new Dictionary<string, Pack>(StringComparer.OrdinalIgnoreCase);
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(PackPrefix, StringComparison.Ordinal) || !resource.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var code = resource[PackPrefix.Length..^".json".Length].ToLowerInvariant();
            if (code is "es" or "en" || code.Length < 2)
            {
                continue;
            }
            try
            {
                using var stream = assembly.GetManifestResourceStream(resource)!;
                using var json = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                var root = json.RootElement;
                string? Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var texts = new Dictionary<string, string>(StringComparer.Ordinal);
                if (root.TryGetProperty("texts", out var items) && items.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in items.EnumerateObject())
                    {
                        if (item.Value.ValueKind == JsonValueKind.String)
                        {
                            texts[item.Name] = item.Value.GetString()!;
                        }
                    }
                }
                CultureInfo culture;
                try
                {
                    culture = CultureInfo.GetCultureInfo(Str("culture") is { Length: > 0 } c ? c : code);
                }
                catch (CultureNotFoundException)
                {
                    culture = CultureInfo.GetCultureInfo("en-US");
                }
                packs[code] = new Pack(code, Str("name") is { Length: > 0 } name ? name : code, culture,
                    Str("ordinal") is { Length: > 0 } ordinal ? ordinal : "{0}.", texts);
            }
            catch (JsonException)
            {
            }
        }
        return packs;
    }

    // Nombres internos guardados en data.json (en inglés) -> nombre mostrado.
    private static readonly Dictionary<string, (string Es, string En)> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        // Nombres de los cofres en el juego (capítulo 7). Las claves son las internas de data.json y no cambian:
        // "Extraction / relic chest" es el cofre de los Sprites ("relic" en los archivos del juego).
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
        ["Treasure"] = ("Tesoro", "Treasure"),
        ["Treasure (unknown type)"] = ("Tesoro (tipo desconocido)", "Treasure (unknown type)"),
        ["Treasure gizmo"] = ("Artilugio de tesoro", "Treasure gizmo"),
        // Artilugios de la caza del tesoro. En data.json se guardan con su nombre inglés; en español, el del juego:
        // «Extractor portátil» y «Localizador de la suerte» salen así en las notas oficiales de Epic; los otros tres,
        // en guías en español y con el «espíritu» con que el juego traduce «Sprite». Las claves no distinguen mayúsculas.
        ["Golden Firework"] = ("Fuegos artificiales dorados", "Golden Firework"),
        ["Portable Extractor"] = ("Extractor portátil", "Portable Extractor"),
        ["Sprite Locator"] = ("Localizador de espíritus", "Sprite Locator"),
        ["Lucky Locator"] = ("Localizador de la suerte", "Lucky Locator"),
        ["Extraction Accelerator"] = ("Acelerador de extracción", "Extraction Accelerator"),
        ["unknown"] = ("Desconocido", "Unknown"),
    };

    /// <summary>Nombre mostrado de un tipo de cofre u objeto guardado en data.json.</summary>
    public static string Name(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return stored ?? "";
        }
        return Names.TryGetValue(stored, out var pair) ? T(pair.Es, pair.En) : stored;
    }
}
