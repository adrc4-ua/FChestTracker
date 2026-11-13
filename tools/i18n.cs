// Creates or updates a language file for FCT: src/Localization/Languages/<code>.json
//
//   dotnet run tools/i18n.cs -- <code> [--name "<language name>"] [--culture <culture>]
//   e.g.  dotnet run tools/i18n.cs -- pt --name "Português (Brasil)" --culture pt-BR
//
// It reads every text in the code (L.T, L.F, L.Count, L.Big and the chest names in Lang.cs) and writes them all to the
// file: the key is the Spanish text and the value starts as the English one. Translate the values and rebuild: the
// language shows up in Settings by itself. Running it again keeps your translations, adds the new texts and drops the
// ones the app no longer uses.
#:property PublishAot=false
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

if (args.Length == 0 || args[0].StartsWith('-') || args[0].Length < 2)
{
    Console.WriteLine("Usage: dotnet run tools/i18n.cs -- <code> [--name \"<language name>\"] [--culture <culture>]");
    return 1;
}
var code = args[0].ToLowerInvariant();
if (code is "es" or "en")
{
    Console.WriteLine("Spanish and English are written in the code itself (L.T(\"español\", \"English\")).");
    return 1;
}
string? Arg(string name) => Array.IndexOf(args, name) is var i and >= 0 && i + 1 < args.Length ? args[i + 1] : null;

var root = FindRoot();
var src = Path.Combine(root, "src");
var file = Path.Combine(src, "Localization", "Languages", code + ".json");

// 1. Every text in the code, in order of appearance: Spanish key -> English text.
var texts = new List<(string Key, string En)>();
var seen = new HashSet<string>(StringComparer.Ordinal);
void Add(string key, string en)
{
    if (seen.Add(key))
    {
        texts.Add((key, en));
    }
}
foreach (var path in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
             .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
             .OrderBy(p => p, StringComparer.Ordinal))
{
    var code_ = File.ReadAllText(path);
    foreach (Match m in Regex.Matches(code_, @"\bL\.(T|F|Count|Big)\("))
    {
        var p = new Literals(code_, m.Index + m.Length);
        switch (m.Groups[1].Value)
        {
            case "T" or "F":
                if (p.Next() is { } es && p.Next() is { } en) Add(es, en);
                break;
            case "Count":
                // L.Count(n, esOne, esMany, enOne, enMany): skip the number, then four texts.
                if (p.SkipToString() && p.Next() is { } esOne && p.Next() is { } esMany && p.Next() is { } enOne && p.Next() is { } enMany)
                {
                    Add(esOne, enOne);
                    Add(esMany, enMany);
                }
                break;
            case "Big":
                if (p.Next() is { } id && p.Next() is { } _ && p.Next() is { } enBig) Add("#" + id, enBig);
                break;
        }
    }
    if (Path.GetFileName(path) == "Lang.cs")
    {
        // Chest and item names: ["Key"] = ("Spanish", "English"),
        foreach (Match m in Regex.Matches(code_, @"\[""[^""]+""\]\s*=\s*\("))
        {
            var p = new Literals(code_, m.Index + m.Length);
            if (p.Next() is { } es && p.Next() is { } en) Add(es, en);
        }
    }
}

// 2. Merge with the existing file (keeps the translations already made).
JsonObject old = File.Exists(file) ? JsonNode.Parse(File.ReadAllText(file), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject ?? new JsonObject() : new JsonObject();
var oldTexts = old["texts"] as JsonObject ?? new JsonObject();
var outTexts = new JsonObject();
int kept = 0, added = 0;
foreach (var (key, en) in texts)
{
    if (oldTexts[key]?.GetValue<string>() is { } done)
    {
        outTexts[key] = done;
        kept++;
    }
    else
    {
        outTexts[key] = en;
        added++;
    }
}
var dropped = oldTexts.Count(kv => !seen.Contains(kv.Key));
var untranslated = texts.Count(t => outTexts[t.Key]!.GetValue<string>() == t.En && t.Key != t.En);

var result = new JsonObject
{
    ["name"] = Arg("--name") ?? old["name"]?.GetValue<string>() ?? code,
    ["culture"] = Arg("--culture") ?? old["culture"]?.GetValue<string>() ?? code,
    ["ordinal"] = old["ordinal"]?.GetValue<string>() ?? "{0}.",
    ["texts"] = outTexts,
};
Directory.CreateDirectory(Path.GetDirectoryName(file)!);
var json = result.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
File.WriteAllText(file, json + "\n", new UTF8Encoding(false));
Console.WriteLine($"{Path.GetRelativePath(root, file)}: {texts.Count} texts ({kept} kept, {added} new, {dropped} no longer used). Still in English: {untranslated}.");
return 0;

static string FindRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "src", "FCT.csproj")))
        {
            return dir.FullName;
        }
    }
    throw new InvalidOperationException("Run it from the FCT repository.");
}

/// <summary>Reads the C# string literals of a call one by one: "regular", @"verbatim" and """raw""".</summary>
sealed class Literals(string code, int pos)
{
    private int _pos = pos;

    private void SkipSpace()
    {
        while (_pos < code.Length && (char.IsWhiteSpace(code[_pos]) || code[_pos] == ','))
        {
            _pos++;
        }
    }

    /// <summary>Moves to the next string literal of this call (skips a first argument that is not text).</summary>
    public bool SkipToString()
    {
        var depth = 0;
        for (; _pos < code.Length; _pos++)
        {
            var c = code[_pos];
            if (c == '"' || (c == '@' && _pos + 1 < code.Length && code[_pos + 1] == '"')) return depth == 0;
            if (c == '(') depth++;
            if (c == ')' && --depth < 0) return false;
        }
        return false;
    }

    /// <summary>The next argument when it is a plain string literal; null otherwise (interpolated, a variable…).</summary>
    public string? Next()
    {
        SkipSpace();
        if (_pos >= code.Length) return null;
        if (code[_pos] == '@' && _pos + 1 < code.Length && code[_pos + 1] == '"')
        {
            var sb = new StringBuilder();
            for (_pos += 2; _pos < code.Length; _pos++)
            {
                if (code[_pos] == '"')
                {
                    if (_pos + 1 < code.Length && code[_pos + 1] == '"') { sb.Append('"'); _pos++; continue; }
                    _pos++;
                    return sb.ToString();
                }
                sb.Append(code[_pos]);
            }
            return null;
        }
        if (code[_pos] != '"') return null;
        if (code.AsSpan(_pos).StartsWith("\"\"\""))
        {
            // Raw string: content between the lines of the delimiters, minus the indentation of the closing one.
            var quotes = 0;
            while (_pos + quotes < code.Length && code[_pos + quotes] == '"') quotes++;
            var delimiter = new string('"', quotes);
            var start = _pos + quotes;
            var end = code.IndexOf(delimiter, start, StringComparison.Ordinal);
            if (end < 0) return null;
            _pos = end + quotes;
            var body = code[start..end].Replace("\r\n", "\n");
            if (!body.Contains('\n')) return body;
            var lines = body.Split('\n').Skip(1).ToList();
            var indent = lines[^1];
            lines.RemoveAt(lines.Count - 1);
            return string.Join("\n", lines.Select(l => l.StartsWith(indent, StringComparison.Ordinal) ? l[indent.Length..] : l.TrimStart()));
        }
        var text = new StringBuilder();
        for (_pos++; _pos < code.Length; _pos++)
        {
            var c = code[_pos];
            if (c == '"')
            {
                _pos++;
                return text.ToString();
            }
            if (c != '\\')
            {
                text.Append(c);
                continue;
            }
            var e = code[++_pos];
            switch (e)
            {
                case 'n': text.Append('\n'); break;
                case 't': text.Append('\t'); break;
                case 'r': text.Append('\r'); break;
                case '0': text.Append('\0'); break;
                case 'u':
                    text.Append((char)Convert.ToInt32(code.Substring(_pos + 1, 4), 16));
                    _pos += 4;
                    break;
                default: text.Append(e); break;
            }
        }
        return null;
    }
}
