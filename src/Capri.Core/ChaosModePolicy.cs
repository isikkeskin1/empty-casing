using System.Text.RegularExpressions;

namespace Capri.Core;

/// <summary>
/// Strict user-authored clone commands and an explicit, timed hold-Escape stop.
/// No clone may independently issue automation or Windows input events.
/// </summary>
public static class ChaosModePolicy
{
    public const int MaxClones = 5;
    public static readonly TimeSpan StopHoldDuration = TimeSpan.FromMilliseconds(850);

    public static int CloneCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 120) return 0;
        var normalized = text.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();
        if (normalized.StartsWith("capri, ")) normalized = normalized[7..];
        if (normalized.StartsWith("hey capri, ")) normalized = normalized[11..];
        if (normalized is "clone yourself" or "make a clone" or "make one clone")
            return 1;
        if (normalized is "chaos mode" or "start chaos mode" or "let there be chaos")
            return 3;
        var match = Regex.Match(normalized, @"^(?:make|spawn|create) ([1-5]) clones?$",
            RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var count)
            ? count : 0;
    }

    // Unlike ordinary decorative clones, AI chatter starts ONLY on an explicit
    // "AI/talking" request, with a locally configured Ollama model. No cloud
    // requests or private user context are ever forwarded to clone personas.
    public static int AiCloneCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 120) return 0;
        var normalized = text.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();
        if (normalized.StartsWith("capri, ")) normalized = normalized[7..];
        if (normalized.StartsWith("hey capri, ")) normalized = normalized[11..];
        if (normalized is "make ai clones" or "let the clones talk" or
            "make talking clones" or "ai chaos mode")
            return 3;
        var match = Regex.Match(normalized,
            @"^(?:make|spawn|create) ([1-5]) (?:ai|talking) clones?$",
            RegexOptions.CultureInvariant);
        return match.Success && int.TryParse(match.Groups[1].Value, out var count)
            ? count : 0;
    }

    public static bool StopRequested(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Trim().TrimEnd('.', '!', '?').ToLowerInvariant() is
            "stop clones" or "hide clones" or "no more clones" or
            "stop chaos" or "chaos off" or "end chaos mode";
    }

    public static bool HeldEscapeLongEnough(bool isDown, long firstDownMs, long nowMs) =>
        isDown && firstDownMs > 0 && nowMs >= firstDownMs &&
        nowMs - firstDownMs >= (long)StopHoldDuration.TotalMilliseconds;
}
