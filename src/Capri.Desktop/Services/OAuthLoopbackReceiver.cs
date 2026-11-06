using System.Net;
using System.Net.Sockets;
using System.Text;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// A tiny one-shot OAuth callback receiver bound ONLY to 127.0.0.1.
/// No HttpListener URL ACL/admin rights or remote network binding.
/// </summary>
public sealed class OAuthLoopbackReceiver : IDisposable
{
    private readonly TcpListener _listener;
    private readonly LinkedService _provider;
    private bool _started;

    public OAuthLoopbackReceiver(LinkedService provider)
    {
        _provider = provider;
        _listener = new TcpListener(IPAddress.Loopback, LinkedAccountPolicy.Port(provider));
        _listener.Server.ExclusiveAddressUse = true;
    }

    public void Start()
    {
        _listener.Start(3);
        _started = true;
    }

    public async Task<string> WaitForCodeAsync(string expectedState, CancellationToken token)
    {
        if (!_started) throw new InvalidOperationException("OAuth receiver not started.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(LinkedAccountPolicy.OAuthTimeout);
        try
        {
            while (!deadline.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(deadline.Token);
                client.ReceiveTimeout = 4000;
                client.SendTimeout = 4000;
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, new UTF8Encoding(false),
                    detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

                string? firstLine;
                try { firstLine = await reader.ReadLineAsync(deadline.Token); }
                catch (IOException) { continue; }
                if (firstLine is null || firstLine.Length > 3000) continue;

                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var total = firstLine.Length;
                while (total < 8192)
                {
                    var line = await reader.ReadLineAsync(deadline.Token);
                    if (string.IsNullOrEmpty(line)) break;
                    total += line.Length;
                    var sep = line.IndexOf(':');
                    if (sep > 0 && !headers.ContainsKey(line[..sep]))
                        headers[line[..sep]] = line[(sep + 1)..].Trim();
                }

                var parts = firstLine.Split(' ', 3);
                var host = "127.0.0.1:" + LinkedAccountPolicy.Port(_provider);
                if (total >= 8192 || parts.Length != 3 || parts[0] != "GET" ||
                    !parts[2].StartsWith("HTTP/1.", StringComparison.Ordinal) ||
                    !headers.TryGetValue("Host", out var actualHost) ||
                    !string.Equals(actualHost, host, StringComparison.OrdinalIgnoreCase) ||
                    parts[1].Length > 2200 || !parts[1].StartsWith('/'))
                {
                    await ReplyAsync(stream, 400, "Invalid authorization callback.", deadline.Token);
                    continue;
                }

                var candidate = new Uri("http://" + host + parts[1]);
                var code = LinkedAccountPolicy.ParseCallbackCode(
                    _provider, candidate, expectedState);
                if (code is null)
                {
                    await ReplyAsync(stream, 400,
                        "This authorization request did not match Capri. You may close this tab.",
                        deadline.Token);
                    continue;
                }

                await ReplyAsync(stream, 200,
                    "Account authorized for Capri Desktop. You may return to the app.",
                    deadline.Token);
                return code;
            }
            throw new OperationCanceledException(deadline.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException("Account connection timed out after 3 minutes.");
        }
    }

    private static async Task ReplyAsync(
        NetworkStream stream, int statusCode, string message, CancellationToken token)
    {
        var html = "<!doctype html><html><head><meta charset=\"utf-8\">" +
            "<meta name=\"referrer\" content=\"no-referrer\"></head>" +
            "<body style=\"background:#101826;color:#eee;font:16px sans-serif;" +
            "max-width:550px;margin:15vh auto\"><h2>Capri Desktop</h2><p>" +
            message + "</p></body></html>";
        var body = Encoding.UTF8.GetBytes(html);
        var header = Encoding.ASCII.GetBytes("HTTP/1.1 " + statusCode +
            (statusCode == 200 ? " OK" : " Bad Request") +
            "\r\nContent-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\n" +
            "Pragma: no-cache\r\nX-Content-Type-Options: nosniff\r\n" +
            "Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'\r\n" +
            "Connection: close\r\nContent-Length: " + body.Length + "\r\n\r\n");
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(body, token);
        await stream.FlushAsync(token);
    }

    public void Dispose() => _listener.Stop();
}
