using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Capri.Core;

namespace Capri.Desktop.Services;

public sealed record ObsReplayResult(string Message, bool? Running = null, string? FilePath = null);

/// <summary>
/// Localhost-only OBS WebSocket v5 connector. Opens a short-lived authenticated
/// connection for each explicit action; never changes stream/scene settings.
/// </summary>
public sealed class ObsReplayService
{
    public async Task<ObsReplayResult> ExecuteAsync(
        string requestType, int port, string password, CancellationToken cancellationToken = default)
    {
        if (!ObsReplayProtocol.IsAuthorizedRequest(requestType))
            throw new ArgumentException("Unsupported OBS operation.", nameof(requestType));
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                "Set your OBS WebSocket password under Apps & Tools → Game Clips first.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(9));
        using var socket = new ClientWebSocket();
        // Host is hard-coded to loopback. Never connect to a user- or model-given server.
        await socket.ConnectAsync(ObsReplayProtocol.Endpoint(port), timeout.Token);
        using (var hello = JsonDocument.Parse(await ReadAsync(socket, timeout.Token)))
        {
            var root = hello.RootElement;
            if (root.GetProperty("op").GetInt32() != 0)
                throw new InvalidOperationException("OBS did not send WebSocket v5 Hello.");
            var data = root.GetProperty("d");
            if (!data.TryGetProperty("rpcVersion", out var rpc) ||
                rpc.GetInt32() < 1 ||
                !data.TryGetProperty("authentication", out var auth))
                throw new InvalidOperationException(
                    "OBS WebSocket 5 with password authentication is required. " +
                    "Enable authentication in OBS → Tools → WebSocket Server Settings.");
            var challenge = auth.GetProperty("challenge").GetString() ?? "";
            var salt = auth.GetProperty("salt").GetString() ?? "";
            await SendAsync(socket, new
            {
                op = 1,
                d = new
                {
                    rpcVersion = 1,
                    eventSubscriptions = 0,
                    authentication = ObsReplayProtocol.Authenticate(password, salt, challenge)
                }
            }, timeout.Token);
        }
        using (var identified = JsonDocument.Parse(await ReadAsync(socket, timeout.Token)))
        {
            if (identified.RootElement.GetProperty("op").GetInt32() != 2)
                throw new InvalidOperationException(
                    "OBS rejected the connection. Check the WebSocket password.");
        }

        // A save is valid only when the replay buffer is already active.
        if (requestType == ObsReplayProtocol.Save)
        {
            var active = await RequestAsync(socket, ObsReplayProtocol.Status, timeout.Token);
            if (ObsReplayProtocol.ReadReplayRunning(active) != true)
                return new ObsReplayResult("OBS replay buffer is not running. " +
                    "Enable it in OBS Output settings, then press START BUFFER.");
        }

        var dataResult = await RequestAsync(socket, requestType, timeout.Token);
        if (requestType == ObsReplayProtocol.Status)
        {
            var running = ObsReplayProtocol.ReadReplayRunning(dataResult);
            return new ObsReplayResult(running == true
                ? "OBS replay buffer is running. Capri can save the previous moments."
                : "OBS connected, but replay buffering is off. " +
                  "Enable Replay Buffer in OBS Output settings, then press START BUFFER.",
                running);
        }
        if (requestType == ObsReplayProtocol.Start)
            return new ObsReplayResult("OBS replay buffer started. " +
                "It will retain recent video according to your OBS settings.", true);
        if (requestType == ObsReplayProtocol.Save)
            return new ObsReplayResult("OBS accepted your save request. " +
                "Check its configured replay folder for the clip.", true);
        var file = ObsReplayProtocol.ReadReplayPath(dataResult);
        return file is null
            ? new ObsReplayResult("OBS has no recent replay file to show yet.")
            : new ObsReplayResult("Most recent OBS replay: " + Path.GetFileName(file), FilePath: file);
    }

    private static async Task<JsonElement> RequestAsync(
        ClientWebSocket socket, string type, CancellationToken token)
    {
        if (!ObsReplayProtocol.IsAuthorizedRequest(type))
            throw new InvalidOperationException("Blocked OBS operation.");
        var id = Guid.NewGuid().ToString("N");
        await SendAsync(socket, new
        {
            op = 6,
            d = new { requestType = type, requestId = id }
        }, token);
        using var response = JsonDocument.Parse(await ReadAsync(socket, token));
        var root = response.RootElement;
        if (root.GetProperty("op").GetInt32() != 7)
            throw new InvalidOperationException("OBS sent an unexpected response.");
        var data = root.GetProperty("d");
        if (data.GetProperty("requestId").GetString() != id ||
            data.GetProperty("requestType").GetString() != type)
            throw new InvalidOperationException("OBS response does not match the request.");
        var status = data.GetProperty("requestStatus");
        if (status.GetProperty("result").ValueKind != JsonValueKind.True)
        {
            var code = status.TryGetProperty("code", out var statusCode)
                ? statusCode.GetInt32() : 0;
            throw new InvalidOperationException("OBS rejected " + type +
                " (status " + code + "). Check Replay Buffer setup and OBS logs.");
        }
        return data.TryGetProperty("responseData", out var result)
            ? result.Clone() : JsonDocument.Parse("{}").RootElement.Clone();
    }

    private static async Task SendAsync(
        ClientWebSocket socket, object payload, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, token);
    }

    private static async Task<string> ReadAsync(ClientWebSocket socket, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        WebSocketReceiveResult read;
        do
        {
            read = await socket.ReceiveAsync(new ArraySegment<byte>(chunk), token);
            if (read.MessageType != WebSocketMessageType.Text)
                throw new InvalidOperationException("OBS WebSocket returned non-text data.");
            if (buffer.Length + read.Count > 65536)
                throw new InvalidOperationException("OBS reply exceeded the size limit.");
            buffer.Write(chunk, 0, read.Count);
        } while (!read.EndOfMessage);
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
