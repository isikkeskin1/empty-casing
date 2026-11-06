using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Capri.Core;

/// <summary>
/// Fixed OBS WebSocket 5 operations for local replay capture only. OBS controls
/// are not model-generated and will never call streaming, scene or file APIs.
/// </summary>
public static class ObsReplayProtocol
{
    public const int DefaultPort = 4455;
    public const string PasswordKey = "obs";
    public const string Status = "GetReplayBufferStatus";
    public const string Start = "StartReplayBuffer";
    public const string Save = "SaveReplayBuffer";
    public const string LastReplay = "GetLastReplayBufferReplay";

    public static bool IsValidPort(int port) => port is >= 1024 and <= 65535;
    public static Uri Endpoint(int port) => IsValidPort(port)
        ? new Uri("ws://127.0.0.1:" + port + "/")
        : throw new ArgumentOutOfRangeException(nameof(port));

    public static string? ClassifyIntent(string? input)
    {
        if (input is null || input.Length > 100 || input.Any(char.IsControl))
            return null;
        return input.Trim().TrimEnd('!', '.', '?').ToLowerInvariant() switch
        {
            "clip that" or "capri clip that" or "save that clip" or
            "save replay" or "save replay buffer" or
            "save the last clip" or "capture that clip" or
            "clip it" => Save,
            "start replay buffer" or "start clipping" or
            "enable replay buffer" => Start,
            "clip status" or "replay buffer status" or
            "is obs recording replays" => Status,
            "show last clip" or "last replay" => LastReplay,
            _ => null
        };
    }

    public static string Authenticate(string password, string salt, string challenge)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(salt) ||
            string.IsNullOrWhiteSpace(challenge))
            throw new ArgumentException("OBS authentication details are missing.");
        var secret = Convert.ToBase64String(SHA256.HashData(
            Encoding.UTF8.GetBytes(password + salt)));
        return Convert.ToBase64String(SHA256.HashData(
            Encoding.UTF8.GetBytes(secret + challenge)));
    }

    public static bool IsAuthorizedRequest(string name) => name is
        Status or Start or Save or LastReplay;

    public static bool? ReadReplayRunning(JsonElement responseData) =>
        responseData.ValueKind == JsonValueKind.Object &&
        responseData.TryGetProperty("outputActive", out var active) &&
        active.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? active.GetBoolean() : null;

    public static string? ReadReplayPath(JsonElement responseData)
    {
        if (responseData.ValueKind != JsonValueKind.Object ||
            !responseData.TryGetProperty("savedReplayPath", out var path) ||
            path.ValueKind != JsonValueKind.String)
            return null;
        var value = path.GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1000 ||
            value.Any(char.IsControl) || value.StartsWith(@"\\", StringComparison.Ordinal) ||
            !Path.IsPathFullyQualified(value) ||
            !new[] { ".mp4", ".mkv", ".mov", ".flv", ".ts" }
                .Contains(Path.GetExtension(value), StringComparer.OrdinalIgnoreCase))
            return null;
        return value;
    }
}
