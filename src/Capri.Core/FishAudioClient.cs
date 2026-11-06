using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Capri.Core;

/// <summary>
/// Sends approved TTS text to Fish Audio. No background recording and no voice cloning.
/// Credentials are read from a separate Windows-encrypted SecretStore.
/// </summary>
public sealed partial class FishAudioClient : IDisposable
{
    public const string FreeModel = "s2.1-pro-free";
    public const string ProModel = "s2.1-pro";
    public const string KeyId = "fish";
    private static readonly Uri Endpoint = new("https://api.fish.audio/v1/tts");
    private const int MaxAudioBytes = 12 * 1024 * 1024;
    private readonly SecretStore _secrets;
    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    [GeneratedRegex("^[A-Za-z0-9-]{1,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceIdPattern();

    public FishAudioClient(SecretStore secrets, HttpClient? client = null)
    {
        _secrets = secrets;
        _ownsClient = client is null;
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(80) };
    }

    public static bool IsSupportedModel(string? model) => model is FreeModel or ProModel;

    public static bool IsValidReference(string? referenceId) =>
        string.IsNullOrEmpty(referenceId) || ReferenceIdPattern().IsMatch(referenceId);

    public async Task<byte[]> SynthesizeAsync(string text, string model = FreeModel,
        string? referenceId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 700)
            throw new ArgumentException("TTS text must be between 1 and 700 characters.", nameof(text));
        if (!IsSupportedModel(model))
            throw new ArgumentException("Unsupported Fish Audio model.", nameof(model));
        if (!IsValidReference(referenceId))
            throw new ArgumentException("Invalid Fish Audio voice reference ID.", nameof(referenceId));

        var key = _secrets.Read(KeyId);
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Connect a new Fish Audio API key in Voice Setup first.");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.TryAddWithoutValidation("model", model);
        request.Content = string.IsNullOrEmpty(referenceId)
            ? JsonContent.Create(new { text, format = "mp3" })
            : JsonContent.Create(new { text, format = "mp3", reference_id = referenceId });

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(70));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            deadline.Token);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Fish Audio returned HTTP " +
                (int)response.StatusCode + ". Verify your key, voice ID, model access or quota.");

        if (response.Content.Headers.ContentLength is long length && length > MaxAudioBytes)
            throw new InvalidOperationException("Fish Audio returned audio that is too large.");

        await using var content = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var buffer = new MemoryStream();
        var chunk = new byte[16384];
        int read;
        while ((read = await content.ReadAsync(chunk.AsMemory(), deadline.Token)) != 0)
        {
            if (buffer.Length + read > MaxAudioBytes)
                throw new InvalidOperationException("Fish Audio audio exceeded the size limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), deadline.Token);
        }
        if (buffer.Length < 64)
            throw new InvalidOperationException("Fish Audio returned empty or invalid audio.");

        // MP3 frame sync or ID3 header: prevents treating JSON error pages as audio.
        var bytes = buffer.ToArray();
        var isMp3 = (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3') ||
                    (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0);
        if (!isMp3)
            throw new InvalidOperationException("Fish Audio did not return an MP3 file.");
        return bytes;
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
