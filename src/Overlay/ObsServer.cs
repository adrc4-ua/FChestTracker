using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FCT.Overlay;

/// <summary>
/// Tiny local web server (127.0.0.1 only) while the tool is open.
/// <list type="bullet">
/// <item>"/" is the overlay page for OBS (it refreshes itself every 2 s without flicker), "/view" its content.</item>
/// <item>"/informe" is the report, so that its Settings page can change settings: "POST /api/ajuste".</item>
/// </list>
/// Only requests addressed to localhost / 127.0.0.1 are answered (protection against DNS rebinding). Settings changes
/// also need the "X-FCT" header, which another website cannot send here without a CORS preflight that this server
/// never accepts, and an Origin (when present) that is this same server.
/// </summary>
public static class ObsServer
{
    public const int DefaultPort = 7373;

    private const int MaxHeader = 16 * 1024;
    private const int MaxBody = 4 * 1024;

    private static TcpListener? _listener;
    private static volatile string _view = "";

    /// <summary>Address to paste in OBS, or null when the server could not start.</summary>
    public static string? Url { get; private set; }

    /// <summary>Address of the report served by the app (its Settings page works there), or null.</summary>
    public static string? ReportUrl => Url != null ? Url + "informe" : null;

    /// <summary>The report file served at "/informe".</summary>
    public static string? ReportFile { get; set; }

    /// <summary>Applies a setting changed on the Resumen page: (key, value) → accepted.</summary>
    public static Func<string, string, bool>? Settings { get; set; }

    /// <summary>Starts on the first free port from <paramref name="port"/> (a few tries). Safe to call twice.</summary>
    public static void Start(int port)
    {
        if (_listener != null)
        {
            return;
        }
        for (var p = port; p < port + 10; p++)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, p);
                listener.Start();
                _listener = listener;
                Url = $"http://localhost:{p}/";
                new Thread(() => AcceptLoop(listener)) { IsBackground = true, Name = "OBS" }.Start();
                return;
            }
            catch (SocketException)
            {
                // port in use: try the next one
            }
        }
        ErrorLog.Write($"OBS: no free port from {port}", new InvalidOperationException("no free port"));
    }

    public static void Stop()
    {
        try { _listener?.Stop(); } catch { /* closing */ }
        _listener = null;
        Url = null;
    }

    /// <summary>New figures for the page (called on every publish).</summary>
    public static void Update(OverlayView view) => _view = ReportWriter.OverlayBox(view);

    /// <summary>True when the Host header names this machine: "localhost:port" or "127.0.0.1:port".</summary>
    private static bool IsLocalHost(string host, int port) =>
        host.Equals($"localhost:{port}", StringComparison.OrdinalIgnoreCase) ||
        host.Equals($"127.0.0.1:{port}", StringComparison.Ordinal);

    private static void AcceptLoop(TcpListener listener)
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch
            {
                return; // stopped
            }
            ThreadPool.QueueUserWorkItem(_ => Serve(client));
        }
    }

    private sealed record Request(string Method, string Path, Dictionary<string, string> Headers, string Body);

    /// <summary>Reads the request line, the headers and a small body (Content-Length), which may come in several packets.</summary>
    private static Request? Read(NetworkStream stream)
    {
        var data = new List<byte>(1024);
        var buffer = new byte[4096];
        var headerEnd = -1;
        while (headerEnd < 0)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0 || data.Count + read > MaxHeader + MaxBody)
            {
                return null;
            }
            data.AddRange(buffer.AsSpan(0, read).ToArray());
            for (var i = Math.Max(0, data.Count - read - 3); i + 3 < data.Count; i++)
            {
                if (data[i] == '\r' && data[i + 1] == '\n' && data[i + 2] == '\r' && data[i + 3] == '\n')
                {
                    headerEnd = i;
                    break;
                }
            }
            if (headerEnd < 0 && data.Count > MaxHeader)
            {
                return null;
            }
        }
        var lines = Encoding.ASCII.GetString(data.GetRange(0, headerEnd).ToArray()).Split("\r\n");
        var first = lines[0].Split(' ');
        if (first.Length < 2)
        {
            return null;
        }
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon > 0)
            {
                headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
        }
        var length = headers.TryGetValue("Content-Length", out var cl) && int.TryParse(cl, out var n) ? n : 0;
        if (length < 0 || length > MaxBody)
        {
            return null;
        }
        var body = data.Skip(headerEnd + 4).ToList();
        while (body.Count < length)
        {
            var read = stream.Read(buffer, 0, Math.Min(buffer.Length, length - body.Count));
            if (read <= 0)
            {
                return null;
            }
            body.AddRange(buffer.AsSpan(0, read).ToArray());
        }
        return new Request(first[0].ToUpperInvariant(), first[1].Split('?')[0], headers,
            Encoding.UTF8.GetString(body.Take(length).ToArray()));
    }

    private static void Serve(TcpClient client)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = 3000;
                client.SendTimeout = 3000;
                var stream = client.GetStream();
                var request = Read(stream);
                if (request == null)
                {
                    return;
                }
                var port = ((IPEndPoint)client.Client.LocalEndPoint!).Port;
                var (status, type, body) = Answer(request, port);
                var bytes = Encoding.UTF8.GetBytes(body);
                var header = $"HTTP/1.1 {status}\r\nContent-Type: {type}; charset=utf-8\r\nContent-Length: {bytes.Length}\r\n" +
                             "Cache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\nConnection: close\r\n\r\n";
                stream.Write(Encoding.ASCII.GetBytes(header));
                stream.Write(bytes);
            }
            catch
            {
                // the browser or OBS closed the connection: nothing to do
            }
        }
    }

    private static (string Status, string Type, string Body) Answer(Request request, int port)
    {
        const string html = "text/html", json = "application/json";
        if (!IsLocalHost(request.Headers.GetValueOrDefault("Host") ?? "", port))
        {
            // Another site pointing its own name at 127.0.0.1 (DNS rebinding): never answer it.
            return ("403 Forbidden", "text/plain", "forbidden");
        }
        switch (request.Method, request.Path)
        {
            case ("GET", "/"):
            case ("GET", "/overlay"):
                return ("200 OK", html, ReportWriter.OverlayLivePage(_view));
            case ("GET", "/view"):
                return ("200 OK", html, _view);
            case ("GET", "/informe"):
                return ReportFile != null && File.Exists(ReportFile)
                    ? ("200 OK", html, File.ReadAllText(ReportFile))
                    : ("404 Not Found", "text/plain", "report not ready");
            case ("GET", "/api/ping"):
                return ("200 OK", json, "{\"ok\":true}");
            case ("POST", "/api/ajuste"):
                return ChangeSetting(request, port);
            default:
                // OPTIONS included: no CORS preflight is ever accepted.
                return ("404 Not Found", "text/plain", "not found");
        }
    }

    private static (string Status, string Type, string Body) ChangeSetting(Request request, int port)
    {
        const string json = "application/json";
        var origin = request.Headers.GetValueOrDefault("Origin");
        var sameOrigin = origin == null || origin.Equals($"http://localhost:{port}", StringComparison.OrdinalIgnoreCase)
                         || origin.Equals($"http://127.0.0.1:{port}", StringComparison.Ordinal);
        if (request.Headers.GetValueOrDefault("X-FCT") != "1" || !sameOrigin || Settings == null)
        {
            return ("403 Forbidden", json, "{\"ok\":false}");
        }
        // Body: "key=value", form-encoded.
        var pair = request.Body.Split('=', 2);
        var key = WebUtility.UrlDecode(pair[0]);
        var value = pair.Length > 1 ? WebUtility.UrlDecode(pair[1]) : "";
        return Settings(key, value)
            ? ("200 OK", json, "{\"ok\":true}")
            : ("400 Bad Request", json, "{\"ok\":false}");
    }
}
