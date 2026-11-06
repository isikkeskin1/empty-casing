namespace Capri.Core;

/// <summary>
/// Keyboard shortcuts are an owner-configured input channel, never model-generated actions.
/// Every binding requires Ctrl plus Alt/Shift and is validated before any Win32 registration.
/// </summary>
public sealed class InputShortcuts
{
    public string OpenChat { get; set; } = "Ctrl+Shift+Space";
    public string ToggleMic { get; set; } = "Ctrl+Alt+V";
    public string MuteMic { get; set; } = "Ctrl+Alt+N";
    public string FocusTyping { get; set; } = "Ctrl+Alt+T";
    public string ToggleCompanion { get; set; } = "Ctrl+Alt+H";
    public string BringHere { get; set; } = "Ctrl+Alt+C";
    public string StopVoice { get; set; } = "Ctrl+Alt+X";

    public IEnumerable<(string Action, string Gesture)> Bindings()
    {
        yield return (nameof(OpenChat), OpenChat);
        yield return (nameof(ToggleMic), ToggleMic);
        yield return (nameof(MuteMic), MuteMic);
        yield return (nameof(FocusTyping), FocusTyping);
        yield return (nameof(ToggleCompanion), ToggleCompanion);
        yield return (nameof(BringHere), BringHere);
        yield return (nameof(StopVoice), StopVoice);
    }

    public static bool TryValidate(InputShortcuts bindings, out string reason)
    {
        var used = new HashSet<(uint Mods, uint Key)>();
        foreach (var (action, gesture) in bindings.Bindings())
        {
            if (!GlobalShortcut.TryParse(gesture, out var parsed))
            {
                reason = "Invalid shortcut for " + action + ": " + gesture +
                    ". Use Ctrl+Alt or Ctrl+Shift plus a letter, function key, or Space.";
                return false;
            }
            if (!used.Add((parsed.Modifiers, parsed.VirtualKey)))
            {
                reason = "Duplicate shortcut assigned to " + action + ": " + gesture;
                return false;
            }
        }
        reason = "";
        return true;
    }
}

public readonly record struct GlobalShortcut(uint Modifiers, uint VirtualKey)
{
    public const uint Alt = 0x1;
    public const uint Ctrl = 0x2;
    public const uint Shift = 0x4;
    public const uint NoRepeat = 0x4000;

    public static bool TryParse(string? text, out GlobalShortcut gesture)
    {
        gesture = default;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 40) return false;
        var tokens = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is < 3 or > 4) return false;
        uint modifiers = 0;
        uint key = 0;
        foreach (var token in tokens)
        {
            if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                if ((modifiers & Ctrl) != 0) return false;
                modifiers |= Ctrl;
            }
            else if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                if ((modifiers & Alt) != 0) return false;
                modifiers |= Alt;
            }
            else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                if ((modifiers & Shift) != 0) return false;
                modifiers |= Shift;
            }
            else
            {
                if (key != 0) return false;
                var upper = token.ToUpperInvariant();
                if (upper.Length == 1 && upper[0] is >= 'A' and <= 'Z')
                    key = upper[0];
                else if (upper == "SPACE") key = 0x20;
                else if (upper.Length is 2 or 3 && upper[0] == 'F' &&
                    int.TryParse(upper[1..], out int index) && index is >= 1 and <= 12)
                    key = (uint)(0x70 + index - 1);
                else return false;
            }
        }
        // Requiring Ctrl and Alt or Shift avoids taking unmodified OS/game input.
        if (key == 0 || (modifiers & Ctrl) == 0 || (modifiers & (Alt | Shift)) == 0)
            return false;
        gesture = new GlobalShortcut(modifiers, key);
        return true;
    }
}
