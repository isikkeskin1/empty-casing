namespace Capri.Core;

public enum CompanionCommand
{
    None,
    RideCursor,
    HopOff,
    ComeHere,
    Sleep,
    Wake
}

/// <summary>
/// Owner-only exact conversational commands for companion position/state.
/// They never go to a model planner or move other applications' windows.
/// </summary>
public static class CompanionCommandPolicy
{
    public static CompanionCommand Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 100)
            return CompanionCommand.None;
        var request = text.Trim().TrimEnd('!', '.', '?').ToLowerInvariant();
        if (request.StartsWith("capri, ")) request = request[7..];
        else if (request.StartsWith("hey capri, ")) request = request[11..];
        return request switch
        {
            "ride my cursor" or "ride the cursor" or "hop on my cursor" =>
                CompanionCommand.RideCursor,
            "hop off" or "stop riding my cursor" or "get off my cursor" =>
                CompanionCommand.HopOff,
            "come here" or "come to my cursor" or "come to me" =>
                CompanionCommand.ComeHere,
            "go to sleep" or "take a nap" or "sleep" =>
                CompanionCommand.Sleep,
            "wake up" or "wake" or "good morning capri" =>
                CompanionCommand.Wake,
            _ => CompanionCommand.None
        };
    }
}
