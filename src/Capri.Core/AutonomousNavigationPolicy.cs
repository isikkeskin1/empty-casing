using System.Text.Json;
using System.Text.RegularExpressions;

namespace Capri.Core;

/// <summary>
/// One approved, time-limited navigation job. The model can propose ONLY a numbered
/// accessible TabItem, never an arbitrary click, text entry, process or URL.
/// </summary>
public static class AutonomousNavigationPolicy
{
    public const int MaxSteps = 5;
    public static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(90);

    private static readonly Regex Forbidden = new(
        @"\b(buy|purchase|pay|checkout|order|bank|billing|payment|transfer|subscribe|" +
        @"password|passcode|credential|token|api.?key|security|permission|admin|account|" +
        @"delete|erase|remove|wipe|send|message|email|post|publish|submit|confirm|" +
        @"approve|sign.?in|log.?in|logout|install|download|upload|execute|run|" +
        @"script|terminal|powershell|cmd|edit|write|modify|change|fill|type|enter)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsSafeGoal(string? goal)
    {
        if (string.IsNullOrWhiteSpace(goal) || goal.Length > 220 ||
            goal.Any(char.IsControl)) return false;
        if (Forbidden.IsMatch(goal)) return false;
        // Ordinary navigation only. Broader tasks require a different approval and policy.
        return Regex.IsMatch(goal.Trim(),
            @"^(find|show|navigate|open|switch|go|look|locate|view|browse)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static bool IsLive(DateTimeOffset start, DateTimeOffset now, int steps, bool enabled) =>
        enabled && steps >= 0 && steps < MaxSteps &&
        now >= start && now - start <= MaxDuration;

    public sealed record Candidate(string Action, string Label, bool Enabled, bool Offscreen);

    public static bool CanExecuteNavigationChoice(
        UiNavigationStep? choice, IReadOnlyList<Candidate> candidates,
        int executedSteps, out string reason)
    {
        if (choice is null || choice.Action == "done" || choice.Index is null ||
            choice.Index.Value < 0 || choice.Index.Value >= candidates.Count)
        {
            reason = "No valid action index supplied by the model.";
            return false;
        }
        if (executedSteps >= MaxSteps)
        {
            reason = "The task step budget is exhausted.";
            return false;
        }
        var item = candidates[choice.Index.Value];
        if (!item.Enabled || item.Offscreen || item.Action != choice.Action)
        {
            reason = "Action or role does not match an eligible observed control.";
            return false;
        }
        var safe = choice.Action switch
        {
            "select_tab" or "expand_tree" => UiNavigationPolicy.IsSafeTabLabel(item.Label),
            "press_navigation" => UiNavigationPolicy.IsSafeNavigationButton(item.Label),
            _ => false
        };
        reason = safe ? "" : "The proposed target is not a safe navigation control.";
        return safe;
    }

    public static bool CanExecuteModelChoice(
        UiNavigationStep? choice, IReadOnlyList<string> eligibleTabNames, int executed,
        out string reason)
    {
        if (choice is null || choice.Action != "select_tab" || choice.Index is null)
        {
            reason = "Model did not propose a tab selection.";
            return false;
        }
        if (choice.Index.Value < 0 || choice.Index.Value >= eligibleTabNames.Count)
        {
            reason = "The suggested tab index is out of range.";
            return false;
        }
        if (executed >= MaxSteps ||
            !UiNavigationPolicy.IsSafeTabLabel(eligibleTabNames[choice.Index.Value]))
        {
            reason = "Step budget exhausted or tab is blocked.";
            return false;
        }
        reason = "";
        return true;
    }
}

public sealed record UiNavigationStep(string Action, int? Index, string Note)
{
    public static UiNavigationStep? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 1200) return null;
        try
        {
            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var action = root.GetProperty("action").GetString();
            if (action is not ("select_tab" or "expand_tree" or "press_navigation" or "done")) return null;
            int? index = null;
            if (root.TryGetProperty("index", out var value))
            {
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
                    return null;
                index = number;
            }
            if (action != "done" && index is null) return null;
            if (action == "done" && index is not null) return null;
            var note = root.TryGetProperty("note", out var text) &&
                text.ValueKind == JsonValueKind.String ? text.GetString() ?? "" : "";
            return new UiNavigationStep(action, index, note.Length > 160 ? note[..160] : note);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or
            KeyNotFoundException or FormatException)
        { return null; }
    }
}
