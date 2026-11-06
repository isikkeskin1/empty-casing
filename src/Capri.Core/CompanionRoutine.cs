namespace Capri.Core;

/// <summary>
/// Predictable owner-invoked multi-action sessions. Unlike an unrestricted LLM
/// tool list, these plans are fixed and reviewed in code. Model output, screen
/// observations and media metadata cannot introduce extra actions.
/// </summary>
public sealed record CompanionRoutine(string Name, IReadOnlyList<AssistantAction> Steps)
{
    public const int MaximumSteps = 4;
    public string Preview => Name + ": " +
        string.Join(" → ", Steps.Select(step => step.Description));

    public static CompanionRoutine? Parse(string? request)
    {
        if (string.IsNullOrWhiteSpace(request) || request.Length > 120 ||
            request.Any(char.IsControl) || ActionPolicy.IsPurchaseRequest(request))
            return null;
        var phrase = string.Join(" ", request.Trim().TrimEnd('.', '!', '?')
            .ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return phrase switch
        {
            "gaming mode" or "start gaming mode" or "start my gaming session" or
            "set up my gaming session" or "set up gaming mode" or
            "get me ready to game" or "prepare my gaming session" =>
                new CompanionRoutine("Gaming session", new[]
                {
                    new AssistantAction("open_app", "discord"),
                    new AssistantAction("open_app", "steam"),
                    new AssistantAction("open_app", "spotify")
                }),
            "roblox mode" or "start roblox mode" or "set up roblox mode" or
            "get me ready for roblox" or "prepare my roblox session" =>
                new CompanionRoutine("Roblox session", new[]
                {
                    new AssistantAction("open_app", "discord"),
                    new AssistantAction("open_url", "https://www.roblox.com/home"),
                    new AssistantAction("open_app", "spotify")
                }),
            "roblox studio mode" or "roblox dev mode" or
            "start roblox dev mode" or "set up roblox studio" or
            "get me ready to build on roblox" =>
                new CompanionRoutine("Roblox creation session", new[]
                {
                    new AssistantAction("open_url", "https://create.roblox.com/dashboard/creations"),
                    new AssistantAction("open_app", "vscode"),
                    new AssistantAction("open_app", "spotify")
                }),
            "coding mode" or "start coding mode" or "set up coding mode" or
            "focus mode" or "start focus mode" or "study mode" or
            "set up study mode" or "get me ready to code" =>
                new CompanionRoutine("Focus session", new[]
                {
                    new AssistantAction("open_app", "vscode"),
                    new AssistantAction("open_app", "notepad"),
                    new AssistantAction("open_app", "spotify")
                }),
            _ => null
        };
    }

    public static bool TryValidate(CompanionRoutine? routine, out string reason)
    {
        if (routine is null || routine.Steps.Count is < 1 or > MaximumSteps)
        {
            reason = "Unknown or oversized routine.";
            return false;
        }
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in routine.Steps)
        {
            if (!ActionPolicy.TryValidate(action, out reason))
                return false;
            if (!unique.Add(action.Kind + ":" + action.Argument))
            {
                reason = "Duplicate action in routine.";
                return false;
            }
        }
        reason = "";
        return true;
    }
}
