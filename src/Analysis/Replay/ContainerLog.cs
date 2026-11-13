using FortniteReplayReader.Models.NetFieldExports.Items;

namespace FCT.Analysis;

/// <summary>
/// Turns the stream of container states into searches, once each: a container is searched when its "already searched"
/// flag turns on, broken when it is destroyed by someone, and can be searched again after a refill. Containers come in
/// and out of range, so their state is often re-sent: those repeats must not count twice.
/// </summary>
internal sealed class ContainerLog
{
    /// <summary>State received this soon after the channel opened was set before the container came into range.</summary>
    private const float StaleWindow = 0.5f;

    private readonly ReplaySignals _signals;

    // Per open channel.
    private readonly Dictionary<uint, string> _key = new();
    private readonly Dictionary<uint, string> _label = new();
    private readonly Dictionary<uint, float> _openedAt = new();
    private readonly Dictionary<uint, bool> _searchedNow = new();   // current flag; false again after a refill
    private readonly Dictionary<uint, uint> _lastSearcher = new();
    private readonly HashSet<uint> _payoutSeen = new();
    private readonly HashSet<uint> _equipSeen = new();

    // Per container, for the whole match.
    private readonly HashSet<string> _counted = new();
    private readonly Dictionary<string, uint?> _lastOpener = new();

    public ContainerLog(ReplaySignals signals) => _signals = signals;

    public void Opened(uint channel, string key, string label, float now)
    {
        _key[channel] = key;
        _label[channel] = label;
        _openedAt[channel] = now;
        _searchedNow.Remove(channel);
        _lastSearcher.Remove(channel);
        _equipSeen.Remove(channel);
        _payoutSeen.Remove(channel);
    }

    public string Label(uint channel, string fallback) => _label.GetValueOrDefault(channel, fallback);
    public bool FirstPayout(uint channel) => _payoutSeen.Add(channel);
    public bool FirstEquip(uint channel) => _equipSeen.Add(channel);

    public void State(uint channel, BaseContainer container, float now)
    {
        if (container.SearchingPawn is { } searcher && searcher != 0)
        {
            _lastSearcher[channel] = searcher;
        }
        var key = _key.GetValueOrDefault(channel, $"ch:{channel}");
        var label = Label(channel, "?");
        var stale = now - _openedAt.GetValueOrDefault(channel, float.MinValue) < StaleWindow;

        if (container.bAlreadySearched == false)
        {
            _searchedNow[channel] = false; // refilled (waves every few minutes): it can count again
            return;
        }
        if (container.bAlreadySearched == true && !_searchedNow.GetValueOrDefault(channel))
        {
            _searchedNow[channel] = true;
            Searched(key, label, _lastSearcher.TryGetValue(channel, out var by) ? by : null, now, stale);
            return;
        }
        // Already counted as searched, or no search flag in this update: it may still have just been broken.
        if (container.bDestroyed && container.SearchingPawn is { } breaker && breaker != 0
            && (_counted.Add(key) || (_searchedNow.TryGetValue(channel, out var wasSearched) && !wasSearched)))
        {
            // Broken (support destroyed, explosion…): the loot spills out and the server names whoever broke it.
            // Counted like a search: once, or once more after a refill.
            _searchedNow[channel] = true;
            _lastOpener[key] = breaker;
            _signals.Searches.Add(new ReplaySignals.Search(now, key, label, breaker, Destroyed: true, Stale: stale));
        }
    }

    /// <summary>
    /// A stale state by the same opener as the last search recorded for a stable container is that old search seen again;
    /// a container without a stable identity is recorded once. Anything else is a new search, refills included.
    /// </summary>
    private void Searched(string key, string label, uint? openedBy, float now, bool stale)
    {
        var repeat = stale && (ReplaySignals.StableKey(key)
            ? _lastOpener.TryGetValue(key, out var previous) && previous == openedBy
            : _counted.Contains(key));
        if (repeat)
        {
            return;
        }
        _counted.Add(key);
        _lastOpener[key] = openedBy;
        _signals.Searches.Add(new ReplaySignals.Search(now, key, label, openedBy, Stale: stale));
    }

    /// <summary>Each shot of a loot cannon is one use (the cannon itself has no "searched" state).</summary>
    public void CannonFired(uint channel, uint gunner, float now)
    {
        var shot = _key.GetValueOrDefault(channel, $"ch:{channel}") + $"#fire{now:F0}";
        if (_counted.Add(shot))
        {
            _signals.Searches.Add(new ReplaySignals.Search(now, shot, Label(channel, "LootCannon"), gunner));
        }
    }
}
