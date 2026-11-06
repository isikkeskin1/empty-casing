using System.Text.Json;

namespace Capri.Core;

/// <summary>
/// Small read-only coding lifecycle signals. Never retain prompts, file names,
/// tool arguments, chat contents, or outputs from the coding assistants.
/// </summary>
public sealed record CodeHuskEvent(string Agent, string Kind, DateTimeOffset At)
{
    public string Label => FriendlyDescription;

    public string FormatStatus() => Kind switch
    {
        "prompt" => " began a turn",
        "working" => " is working",
        "approval" => " needs your approval in its own window",
        "complete" => " finished a turn",
        "error" => " reported a tool error",
        "subagent" => " started a subagent",
        "compact" => " compacted context",
        _ => " updated"
    };

    public string FriendlyDescription => (Agent == "codex" ? "Codex" : "Claude Code") + FormatStatus() + ".";

    public static CodeHuskEvent? Parse(string? line, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length > 1024) return null;
        try
        {
            using var document = JsonDocument.Parse(line, new JsonDocumentOptions
            {
                MaxDepth = 3, AllowTrailingCommas = false
            });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("agent", out var a) ||
                !root.TryGetProperty("event", out var e) ||
                !root.TryGetProperty("timestamp", out var t) ||
                a.ValueKind != JsonValueKind.String ||
                e.ValueKind != JsonValueKind.String ||
                t.ValueKind != JsonValueKind.Number ||
                !t.TryGetInt64(out var seconds))
                return null;
            var agent = a.GetString();
            var kind = e.GetString();
            if (agent is not ("claude" or "codex") ||
                kind is not ("prompt" or "working" or "approval" or "complete" or
                    "error" or "subagent" or "compact"))
                return null;
            var at = DateTimeOffset.FromUnixTimeSeconds(seconds);
            if (at < now.AddMinutes(-3) || at > now.AddSeconds(30)) return null;
            return new CodeHuskEvent(agent, kind, at);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
