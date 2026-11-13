using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace FCT.Localization;

/// <summary>A language added as a file: its own name, culture for numbers and dates, ordinal pattern and texts.</summary>
internal sealed record LanguagePack(string Code, string Name, CultureInfo Culture, string Ordinal, IReadOnlyDictionary<string, string> Texts);

/// <summary>
/// Languages other than Spanish and English, shipped inside the exe as Localization/Languages/&lt;code&gt;.json (see the
/// README in that folder). A broken file is skipped so the app still starts in Spanish or English.
/// </summary>
internal static class LanguagePacks
{
    private const string ResourcePrefix = "FCT.lang.", ResourceSuffix = ".json";
    private static readonly CultureInfo Fallback = CultureInfo.GetCultureInfo("en-US");

    public static IReadOnlyDictionary<string, LanguagePack> All { get; } = Load();

    public static LanguagePack? Find(string code) => All.TryGetValue(code, out var pack) ? pack : null;

    private static Dictionary<string, LanguagePack> Load()
    {
        var packs = new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);
        var exe = Assembly.GetExecutingAssembly();
        foreach (var resource in exe.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !resource.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var code = resource[ResourcePrefix.Length..^ResourceSuffix.Length].ToLowerInvariant();
            if (code.Length < 2 || code is "es" or "en") // the two built-in languages cannot be replaced by a file
            {
                continue;
            }
            try
            {
                using var stream = exe.GetManifestResourceStream(resource)!;
                packs[code] = Parse(code, stream);
            }
            catch (JsonException)
            {
                // ignored: see the class summary
            }
        }
        return packs;
    }

    private static LanguagePack Parse(string code, Stream stream)
    {
        using var json = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var root = json.RootElement;
        string? Text(string property) =>
            root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } s ? s : null;

        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.TryGetProperty("texts", out var entries) && entries.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in entries.EnumerateObject())
            {
                if (entry.Value.ValueKind == JsonValueKind.String)
                {
                    texts[entry.Name] = entry.Value.GetString()!;
                }
            }
        }
        return new LanguagePack(code, Text("name") ?? code, CultureOrFallback(Text("culture") ?? code), Text("ordinal") ?? "{0}.", texts);
    }

    private static CultureInfo CultureOrFallback(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return Fallback;
        }
    }
}
