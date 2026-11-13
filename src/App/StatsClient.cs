using System.Reflection;
using System.Text.Json;

namespace FCT.App;

/// <summary>
/// Total Battle Royale matches of a player, from fortnite-api.com (public stats). Only called when the player asks
/// for it (estimate of the chests before FCT). It needs the project's key, put in the exe when it is built (GitHub
/// secret FORTNITE_API_KEY; for tests, the variable FCT_FORTNITE_API_KEY). Without a key the player types the number.
/// </summary>
public static class StatsClient
{
    public enum Outcome { Ok, Private, NotFound, NoKey, Failed }

    public sealed record Answer(Outcome Outcome, int Matches = 0, string? Name = null);

    private static string? Key =>
        Environment.GetEnvironmentVariable("FCT_FORTNITE_API_KEY") is { Length: > 0 } test ? test
        : typeof(StatsClient).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "FortniteApiKey")?.Value is { Length: > 0 } key ? key : null;

    /// <summary>True when this build can look the matches up by itself.</summary>
    public static bool Available => Key != null;

    /// <summary>Lifetime matches of the player: by account id when known, else by Epic name.</summary>
    public static async Task<Answer> TotalMatchesAsync(string? accountId, string? name)
    {
        if (Key is not { } key)
        {
            return new Answer(Outcome.NoKey);
        }
        var byId = accountId is { Length: 32 } && accountId.All(Uri.IsHexDigit);
        if (!byId && string.IsNullOrWhiteSpace(name))
        {
            return new Answer(Outcome.NotFound);
        }
        var url = byId
            ? $"https://fortnite-api.com/v2/stats/br/v2/{accountId}"
            : $"https://fortnite-api.com/v2/stats/br/v2?name={Uri.EscapeDataString(name!.Trim())}&accountType=epic";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"{AppInfo.ShortName}/{AppInfo.VersionText}");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", key);
            using var response = await http.GetAsync(url);
            switch ((int)response.StatusCode)
            {
                case 403: return new Answer(Outcome.Private);
                case 404: return new Answer(Outcome.NotFound);
            }
            if (!response.IsSuccessStatusCode)
            {
                return new Answer(Outcome.Failed);
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = doc.RootElement.GetProperty("data");
            var matches = data.GetProperty("stats").GetProperty("all").GetProperty("overall").GetProperty("matches").GetInt32();
            var shown = data.TryGetProperty("account", out var account) && account.TryGetProperty("name", out var n) ? n.GetString() : null;
            return new Answer(Outcome.Ok, matches, shown);
        }
        catch
        {
            return new Answer(Outcome.Failed);
        }
    }
}
