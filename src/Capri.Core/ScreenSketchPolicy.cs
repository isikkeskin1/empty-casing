namespace Capri.Core;

/// <summary>
/// Screen Sketch is an explicitly entered annotation mode, never a tool
/// instruction inferred from webpage/screenshot text or an arbitrary click.
/// </summary>
public static class ScreenSketchPolicy
{
    public static string? Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 80 ||
            input.Any(char.IsControl))
            return null;
        return input.Trim().TrimEnd('!', '.', '?').ToLowerInvariant() switch
        {
            "start screen drawing" or "start drawing mode" or
            "draw on screen" or "open screen sketch" or
            "open drawing mode" or "screen sketch mode" => "open",
            "stop screen drawing" or "exit drawing mode" or
            "close screen sketch" or "stop drawing" => "close",
            _ => null
        };
    }
}
