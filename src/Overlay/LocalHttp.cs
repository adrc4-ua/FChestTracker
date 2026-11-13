using System.Net.Sockets;
using System.Text;

namespace FCT.Overlay;

/// <summary>A request as the local server needs it: method, path without the query, headers and a small body.</summary>
internal sealed record LocalRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers, string Body)
{
    public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
}

/// <summary>An answer of the local server.</summary>
internal readonly record struct LocalResponse(string Status, string ContentType, string Body)
{
    public static LocalResponse Html(string body) => new("200 OK", "text/html", body);
    public static LocalResponse Json(string body, string status = "200 OK") => new(status, "application/json", body);
    public static LocalResponse Text(string status, string body) => new(status, "text/plain", body);
}

/// <summary>
/// The bare minimum of HTTP/1.1 for a server that only talks to OBS and the browser on the same PC: one request per
/// connection, bounded sizes, no keep-alive. Anything unusual simply gets no answer.
/// </summary>
internal static class LocalHttp
{
    private const int HeaderLimit = 16 * 1024, BodyLimit = 4 * 1024;
    private static readonly byte[] EndOfHeaders = "\r\n\r\n"u8.ToArray();

    /// <summary>Reads one request (it may arrive in pieces); null for anything malformed or too large.</summary>
    public static LocalRequest? Read(Stream stream)
    {
        var received = new MemoryStream();
        var chunk = new byte[4096];
        int headersEnd;
        while ((headersEnd = IndexOf(received, EndOfHeaders)) < 0)
        {
            var count = stream.Read(chunk, 0, chunk.Length);
            if (count <= 0 || received.Length + count > HeaderLimit + BodyLimit)
            {
                return null;
            }
            received.Write(chunk, 0, count);
            if (IndexOf(received, EndOfHeaders) < 0 && received.Length > HeaderLimit)
            {
                return null;
            }
        }

        var all = received.ToArray();
        var lines = Encoding.ASCII.GetString(all, 0, headersEnd).Split("\r\n");
        var requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2)
        {
            return null;
        }
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < lines.Length; i++)
        {
            var colon = lines[i].IndexOf(':');
            if (colon > 0)
            {
                headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
            }
        }

        var declared = headers.TryGetValue("Content-Length", out var text) && int.TryParse(text, out var length) ? length : 0;
        if (declared is < 0 or > BodyLimit)
        {
            return null;
        }
        var body = new MemoryStream();
        body.Write(all, headersEnd + EndOfHeaders.Length, all.Length - headersEnd - EndOfHeaders.Length);
        while (body.Length < declared)
        {
            var count = stream.Read(chunk, 0, (int)Math.Min(chunk.Length, declared - body.Length));
            if (count <= 0)
            {
                return null;
            }
            body.Write(chunk, 0, count);
        }

        var path = requestLine[1];
        var query = path.IndexOf('?');
        return new LocalRequest(
            requestLine[0].ToUpperInvariant(),
            query < 0 ? path : path[..query],
            headers,
            Encoding.UTF8.GetString(body.GetBuffer(), 0, (int)Math.Min(body.Length, declared)));
    }

    public static void Write(Stream stream, LocalResponse response)
    {
        var body = Encoding.UTF8.GetBytes(response.Body);
        var head = new StringBuilder()
            .Append("HTTP/1.1 ").Append(response.Status).Append("\r\n")
            .Append("Content-Type: ").Append(response.ContentType).Append("; charset=utf-8\r\n")
            .Append("Content-Length: ").Append(body.Length).Append("\r\n")
            .Append("Cache-Control: no-store\r\n")
            .Append("X-Content-Type-Options: nosniff\r\n")
            .Append("Referrer-Policy: no-referrer\r\n")
            .Append("Connection: close\r\n\r\n");
        stream.Write(Encoding.ASCII.GetBytes(head.ToString()));
        stream.Write(body);
    }

    /// <summary>Position of <paramref name="needle"/> in what was received so far, or -1.</summary>
    private static int IndexOf(MemoryStream haystack, byte[] needle) =>
        haystack.GetBuffer().AsSpan(0, (int)haystack.Length).IndexOf(needle);

    /// <summary>Serves one connection with short timeouts; a client that hangs up is not an error.</summary>
    public static void Serve(TcpClient client, Func<LocalRequest, LocalResponse> answer)
    {
        using (client)
        {
            try
            {
                client.ReceiveTimeout = client.SendTimeout = 3000;
                var stream = client.GetStream();
                if (Read(stream) is { } request)
                {
                    Write(stream, answer(request));
                }
            }
            catch
            {
                // OBS or the browser went away, or the request failed: only this connection ends, never FCT (this
                // runs on a thread-pool thread, where an escaping exception would close the app)
            }
        }
    }
}
