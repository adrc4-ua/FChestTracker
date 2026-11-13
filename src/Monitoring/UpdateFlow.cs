namespace FCT.Monitoring;

/// <summary>
/// New versions while FCT runs: GitHub is asked at start and every 30 minutes (so a long session still gets them);
/// "auto" installs between matches, "prompt" shows the offer, "off" never asks.
/// </summary>
internal sealed class UpdateFlow
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(30);

    private readonly Func<Config> _config;
    private readonly Action _publish;
    private DateTime _nextCheckUtc = DateTime.UtcNow;
    private volatile bool _checking;
    private bool _handled;

    public UpdateFlow(Func<Config> config, Action publish)
    {
        _config = config;
        _publish = publish;
    }

    public volatile UpdateInfo? Available;
    public volatile bool Installing;

    /// <summary>Installed: FCT must restart into the new exe.</summary>
    public bool Installed { get; private set; }

    /// <summary>Starts a check in the background when one is due.</summary>
    public void Poll()
    {
        if (_config().UpdateMode == "off" || Available != null || _checking || DateTime.UtcNow < _nextCheckUtc)
        {
            return;
        }
        _checking = true;
        _nextCheckUtc = DateTime.UtcNow + Every;
        _ = Task.Run(async () =>
        {
            try
            {
                if (await Updater.CheckAsync() is { } found)
                {
                    Available = found;
                    _publish();
                }
            }
            finally
            {
                _checking = false;
            }
        });
    }

    /// <summary>
    /// Deals once with a version found: "auto" installs it as soon as no match is being played or read; otherwise the
    /// window shows the offer. True when the window should refresh.
    /// </summary>
    public bool Handle(bool idle, Action install)
    {
        if (Available == null || _handled || Installing)
        {
            return false;
        }
        if (_config().UpdateMode == "auto")
        {
            if (!idle || AppInfo.ExePath == null)
            {
                return false;
            }
            _handled = true;
            install();
            return true;
        }
        _handled = true;
        return true;
    }

    /// <summary>Downloads and swaps in the new exe in the background, then asks to restart (or reports why not).</summary>
    public void Install(Action<string> notice, Action<string> failed, Action restart)
    {
        var update = Available;
        if (update == null || Installing)
        {
            return;
        }
        Installing = true;
        _publish();
        _ = Task.Run(() =>
        {
            var problem = Updater.Install(update);
            if (problem != null)
            {
                Installing = false;
                ErrorLog.Write("Actualización", problem);
                failed(L.F("No se pudo actualizar: {0}", "Update failed: {0}", problem));
                return;
            }
            Available = null;
            notice(L.T("Reiniciando con la nueva versión…", "Restarting with the new version…"));
            Installing = false;
            Installed = true;
            _publish();
            restart();
        });
    }
}
