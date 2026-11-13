
namespace FCT.App;

/// <summary>
/// One FCT at a time (two would mean two overlays and two counters). A second launch does not open another one: the
/// running FCT brings its window up and says it was already open.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _dataDir;
    private bool _owned;

    public SingleInstance(string dataDir)
    {
        _dataDir = dataDir;
        _mutex = new Mutex(false, AppInfo.InstanceMutexFor(dataDir));
    }

    /// <summary>Takes the instance, waiting up to <paramref name="wait"/> (after an update the old one is still closing).</summary>
    public bool TryTake(TimeSpan wait)
    {
        try
        {
            _owned = _mutex.WaitOne(wait);
        }
        catch (AbandonedMutexException)
        {
            _owned = true; // the previous FCT ended badly: the instance is ours now
        }
        return _owned;
    }

    /// <summary>Lets it go before starting another FCT (restart, update, uninstall).</summary>
    public void Release()
    {
        if (!_owned)
        {
            return;
        }
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }
        _owned = false;
    }

    /// <summary>
    /// Another FCT holds the instance. The running one shows its window if it can; if it cannot yet (still on the
    /// welcome, or starting), this launch says so itself.
    /// </summary>
    public void PointToRunning(bool terminal)
    {
        if (!terminal && EventWaitHandle.TryOpenExisting(AppInfo.ShowEventFor(_dataDir), out var show))
        {
            using (show)
            {
                show.Set();
            }
            return;
        }
        var message = L.F("{0} ya está abierto. Solo puede haber uno a la vez: búscalo en el área de notificación (la flecha ^ de la barra de tareas).",
            "{0} is already open. There can only be one at a time: look for it in the notification area (the ^ arrow on the taskbar).", AppInfo.ShortName);
        if (terminal)
        {
            Console.WriteLine("  " + message);
            return;
        }
        InfoDialog.Confirm(null, L.F("{0} ya está abierto", "{0} is already open", AppInfo.ShortName), message, Array.Empty<InfoPoint>(), L.T("Vale", "OK"), null, Icons.Info);
    }

    /// <summary>Calls <paramref name="show"/> on a pool thread each time a later launch asks for the window.</summary>
    public IDisposable OnShowRequested(Action show)
    {
        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, AppInfo.ShowEventFor(_dataDir));
        var registration = ThreadPool.RegisterWaitForSingleObject(signal, (_, _) => show(), null, Timeout.Infinite, executeOnlyOnce: false);
        return new Listening(signal, registration);
    }

    private sealed class Listening(EventWaitHandle signal, RegisteredWaitHandle registration) : IDisposable
    {
        public void Dispose()
        {
            registration.Unregister(null);
            signal.Dispose();
        }
    }

    public void Dispose() => _mutex.Dispose();
}
