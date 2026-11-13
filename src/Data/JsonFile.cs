using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCT.Data;

/// <summary>
/// Reading and writing of FCT's JSON files. Writes go to a temporary file that then replaces the real one, so a crash
/// or a power cut never leaves half a file; a file that cannot be read is copied aside instead of being lost.
/// </summary>
public static class JsonFile
{
    /// <summary>Indented, readable accents, and null values left out (the shape every data file has).</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>The file's content, or null when it does not exist or cannot be read (then kept as *.corrupt-…).</summary>
    public static T? Read<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
        }
        catch (Exception ex)
        {
            File.Copy(path, $"{path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}", overwrite: true);
            ErrorLog.Write($"Archivo ilegible: {path}", ex);
            return null;
        }
    }

    public static void Write<T>(string path, T value)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
        File.Move(temporary, path, overwrite: true);
    }
}
