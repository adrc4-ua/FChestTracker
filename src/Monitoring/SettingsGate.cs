namespace FCT.Monitoring;

/// <summary>
/// The settings shared by the window, the overlay and the monitor thread: every change is short, done under one lock and
/// saved right away (a failed save is not worth interrupting anyone for).
/// </summary>
internal sealed class SettingsGate
{
    private readonly object _lock = new();
    private readonly Store _store;

    public SettingsGate(Store store, Config config)
    {
        _store = store;
        Config = config;
    }

    public Config Config { get; }

    public void Edit(Action<Config> change)
    {
        lock (_lock)
        {
            change(Config);
            Save();
        }
    }

    public T Read<T>(Func<Config, T> read)
    {
        lock (_lock)
        {
            return read(Config);
        }
    }

    private void Save()
    {
        try
        {
            _store.SaveConfig(Config);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
