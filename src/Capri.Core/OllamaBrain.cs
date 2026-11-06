using System.Net.Http.Json;
using System.Text.Json;

namespace Capri.Core;

public sealed class OllamaBrain : IBrainProvider, IDisposable
{
    private const string Endpoint = "http://127.0.0.1:11434";
    private readonly AppSettings _settings;
    private readonly HttpClient _client;
    private readonly bool _ownsClient;

    public OllamaBrain(AppSettings settings, HttpClient? client = null)
    {
        _settings = settings;
        _ownsClient = client is null;
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));
            using var response = await _client.GetAsync(Endpoint + "/api/tags", deadline.Token);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    public async Task<string> ReplyAsync(IReadOnlyList<ChatTurn> turns, CancellationToken cancellationToken = default)
    {
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = "You are a fictional, fan-made interpretation of Capri, an expressive and sharp-witted " +
                          "desktop companion. Keep a playful, slightly mischievous tone, but be useful and concise. " +
                          "You are running in a text-only prototype. You cannot see the screen, hear the microphone, " +
                          "control desktop applications or perform actions. Never pretend you have done any of those. " +
                          "Never claim to be an official character or a human."
            }
        };
        messages.AddRange(turns.Where(t => t.Role is "user" or "assistant")
            .TakeLast(20)
            .Select(t => (object)new { role = t.Role, content = t.Text }));

        using var response = await _client.PostAsJsonAsync(Endpoint + "/api/chat",
            new { model = _settings.OllamaModel, messages, stream = false }, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                "Ollama HTTP " + (int)response.StatusCode + ": " + body[..Math.Min(body.Length, 200)]);

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Ollama returned an unexpected response.");

        return content.GetString()?.Trim() is { Length: > 0 } reply
            ? reply : "(The local model returned an empty reply.)";
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }
}
