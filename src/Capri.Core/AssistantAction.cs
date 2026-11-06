using System.Text.Json;

namespace Capri.Core;

public sealed record AssistantAction(string Kind, string Argument)
{
    public string Description => Kind switch
    {
        "open_app" => "Launch application: " + Argument,
        "open_url" => "Open website: " + Argument,
        "media" => "Media key: " + Argument,
        _ => "Unknown desktop action"
    };
}

public static class ActionPolicy
{
    private static readonly HashSet<string> Apps = new(StringComparer.OrdinalIgnoreCase)
    {
        "notepad", "calculator", "explorer", "edge", "chrome", "vscode", "spotify", "discord", "steam"
    };

    private static readonly HashSet<string> Media = new(StringComparer.OrdinalIgnoreCase)
    {
        "play_pause", "next", "previous", "volume_up", "volume_down", "mute"
    };

    public static bool LooksLikeRequest(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        return t.StartsWith("open ") || t.Contains(" open ") ||
               t.StartsWith("launch ") || t.Contains(" launch ") ||
               t.StartsWith("start ") || t.Contains(" start ") ||
               t.StartsWith("go to ") || t.Contains(" go to ") ||
               t.StartsWith("pause ") || t.Contains(" pause ") ||
               t.StartsWith("skip ") || t.Contains(" skip ") ||
               t.StartsWith("volume ") || t.Contains(" volume ") ||
               t.StartsWith("mute ") || t.Contains(" mute ");
    }

    public static bool IsPurchaseRequest(string text) =>
        new[] { "buy ", "purchase ", "checkout", "check out", "place order", "pay for ",
                 "subscribe to", "payment", "credit card", "bank transfer", "order now" }
        .Any(x => text.Contains(x, StringComparison.OrdinalIgnoreCase));

    public static bool TryValidate(AssistantAction? action, out string reason)
    {
        if (action is null)
        {
            reason = "No supported desktop action was requested.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(action.Argument) || action.Argument.Length > 400)
        {
            reason = "Action argument is missing or too long.";
            return false;
        }

        switch (action.Kind)
        {
            case "open_app" when Apps.Contains(action.Argument):
            case "media" when Media.Contains(action.Argument):
                reason = "";
                return true;
            case "open_url":
                if (Uri.TryCreate(action.Argument, UriKind.Absolute, out var url) &&
                    url.Scheme == Uri.UriSchemeHttps && url.UserInfo.Length == 0 &&
                    url.Host.Length > 3 && !url.IsLoopback &&
                    !url.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "";
                    return true;
                }
                reason = "Only normal HTTPS websites are allowed.";
                return false;
            default:
                reason = "This action is not in the supported allowlist.";
                return false;
        }
    }

    public static AssistantAction? ParsePlannerResponse(string raw)
    {
        try
        {
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            var kind = root.GetProperty("kind").GetString() ?? "";
            var argument = root.TryGetProperty("argument", out var value) ? value.GetString() ?? "" : "";
            if (kind == "none") return null;
            var action = new AssistantAction(kind, argument);
            return TryValidate(action, out _) ? action : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }
}
