using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;

namespace Capri.Desktop.Services;

/// <summary>
/// Discord Rich Presence over Discord's documented local named-pipe RPC.
/// No Discord account token, browser cookie, bot credential or API scraping.
/// The application ID is PUBLIC and supplied once by the Capri publisher.
/// Opt-in only. Does NOT support user-account messages or voice auto-joining.
/// </summary>
public sealed class DiscordPresenceService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private NamedPipeClientStream? _pipe;
    private bool _disposed;

    public static string PublisherAppId =>
        typeof(DiscordPresenceService).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "CapriDiscordClientId")?.Value?.Trim() ?? "";

    public static bool HasPublisherAppId =>
        PublisherAppId.Length is >= 17 and <= 22 &&
        PublisherAppId.All(char.IsAsciiDigit);

    public async Task SetActivityAsync(string details, string state,
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DiscordPresenceService));
        if (!HasPublisherAppId)
            throw new InvalidOperationException(
                "Discord Rich Presence needs Capri's publisher-registered public Discord app ID.");
        if (details.Length > 128 || state.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(details));

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var token = timeout.Token;
        await _gate.WaitAsync(token);
        try
        {
            await EnsureConnectedAsync(token);
            var nonce = Guid.NewGuid().ToString("N");
            await WriteFrameAsync(_pipe!, 1, new
            {
                cmd = "SET_ACTIVITY", nonce,
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = new
                    {
                        type = 0,
                        details,
                        state
                    }
                }
            }, token);
            using var reply = await ReadFrameAsync(_pipe!, token);
            if (reply.RootElement.TryGetProperty("evt", out var evt) &&
                evt.ValueKind == JsonValueKind.String &&
                evt.GetString() == "ERROR")
                throw new InvalidOperationException(
                    "Discord rejected the Rich Presence update. Confirm the Capri app registration.");
        }
        catch
        {
            ClosePipe();
            throw;
        }
        finally { _gate.Release(); }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_pipe?.IsConnected == true) return;
        ClosePipe();
        for (var port = 0; port < 10; port++)
        {
            var pipe = new NamedPipeClientStream(".", "discord-ipc-" + port,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            try
            {
                await pipe.ConnectAsync(180, cancellationToken);
                await WriteFrameAsync(pipe, 0,
                    new { v = 1, client_id = PublisherAppId }, cancellationToken);
                using var ready = await ReadFrameAsync(pipe, cancellationToken);
                if (!ready.RootElement.TryGetProperty("evt", out var evt) ||
                    evt.GetString() != "READY")
                    throw new InvalidOperationException("Discord RPC handshake failed.");
                _pipe = pipe;
                return;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or
                InvalidOperationException)
            {
                pipe.Dispose();
            }
        }
        throw new InvalidOperationException(
            "Discord desktop RPC is unavailable. Open Discord and enable activity sharing.");
    }

    private static async Task WriteFrameAsync(Stream pipe, int opcode,
        object payload, CancellationToken token)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        if (json.Length > 64_000)
            throw new InvalidDataException("Discord RPC frame is too large.");
        var frame = new byte[8 + json.Length];
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(0, 4), opcode);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(4, 4), json.Length);
        json.CopyTo(frame.AsSpan(8));
        // Discord's named pipe expects one whole write for a frame.
        await pipe.WriteAsync(frame, token);
        await pipe.FlushAsync(token);
    }

    private static async Task<JsonDocument> ReadFrameAsync(
        Stream pipe, CancellationToken token)
    {
        var header = new byte[8];
        await pipe.ReadExactlyAsync(header, token);
        var opcode = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4));
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4));
        if (opcode != 1 || length <= 0 || length > 64_000)
            throw new InvalidDataException("Unexpected Discord RPC frame.");
        var body = new byte[length];
        await pipe.ReadExactlyAsync(body, token);
        return JsonDocument.Parse(body);
    }

    private void ClosePipe()
    {
        _pipe?.Dispose();
        _pipe = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClosePipe();
        _gate.Dispose();
    }
}
