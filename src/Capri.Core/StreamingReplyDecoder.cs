using System.Text.Json;

namespace Capri.Core;

/// <summary>
/// Reads only incremental *assistant text* from Ollama NDJSON and OpenAI-style
/// chat completion SSE. Tool calls, reasoning fields, event IDs and other model
/// metadata are never fed back into Capri as instructions.
/// </summary>
public static class StreamingReplyDecoder
{
    public const int MaxReplyCharacters = 32_000;
    public const int MaxEventCharacters = 128_000;

    public static (string Text, bool Done) ReadOllamaLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return ("", false);
        if (line.Length > MaxEventCharacters)
            throw new InvalidOperationException("Ollama stream event is too large.");
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Unexpected Ollama stream event.");
        if (root.TryGetProperty("error", out _))
            throw new InvalidOperationException("The local model stopped with an error.");
        var done = root.TryGetProperty("done", out var complete) &&
                   complete.ValueKind == JsonValueKind.True;
        if (!root.TryGetProperty("message", out var message) ||
            message.ValueKind != JsonValueKind.Object ||
            !message.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.String)
            return ("", done);
        return (content.GetString() ?? "", done);
    }

    public static (string Text, bool Done) ReadCloudLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line) || line[0] == ':')
            return ("", false);
        if (line.Length > MaxEventCharacters)
            throw new InvalidOperationException("Cloud stream event is too large.");
        if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return ("", false);
        var data = line[5..].Trim();
        if (data.Equals("[DONE]", StringComparison.Ordinal))
            return ("", true);
        if (data.Length == 0) return ("", false);
        using var document = JsonDocument.Parse(data);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("Unexpected cloud stream event.");
        if (root.TryGetProperty("error", out _))
            throw new InvalidOperationException("Cloud stream stopped with a provider error.");
        if (!root.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            return ("", false);
        var first = choices[0];
        if (!first.TryGetProperty("delta", out var delta) ||
            delta.ValueKind != JsonValueKind.Object ||
            !delta.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.String)
            return ("", false);
        return (content.GetString() ?? "", false);
    }
}

/// <summary>
/// Conservative sentence segmentation: only speak complete clauses early,
/// never an individual token. Text is bounded to avoid runaway voice queues.
/// </summary>
public sealed class VoiceSentenceBuffer
{
    private string _pending = "";
    public const int MaximumSentence = 220;
    public string Pending => _pending;

    public IReadOnlyList<string> Push(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        // Brain stream is bounded at 32k; this buffer keeps only the unsaid tail.
        _pending += text;
        var result = new List<string>();
        while (_pending.Length >= 42)
        {
            var scan = Math.Min(_pending.Length, MaximumSentence);
            var stop = -1;
            for (var i = 0; i < scan; i++)
                if (_pending[i] is '.' or '!' or '?' or '\n' &&
                    (i + 1 == _pending.Length || char.IsWhiteSpace(_pending[i + 1])))
                    stop = i + 1;
            if (stop == -1)
            {
                if (_pending.Length < MaximumSentence) break;
                stop = _pending.LastIndexOf(' ', MaximumSentence - 1);
                if (stop < 42) stop = MaximumSentence;
            }
            if (stop < 42) break;
            var sentence = _pending[..stop].Trim();
            _pending = _pending[stop..].TrimStart();
            if (sentence.Length > 0) result.Add(sentence);
        }
        return result;
    }

    public string Finish()
    {
        var last = _pending.Trim();
        _pending = "";
        return last;
    }

    public void Clear() => _pending = "";
}
