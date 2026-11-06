using System.Text.RegularExpressions;

namespace Capri.Core;

public enum ReminderKind { Timer, Reminder, List, Cancel }

public sealed record ReminderCommand(ReminderKind Kind, TimeSpan Delay, string Message, int CancelId = 0);

/// <summary>
/// Deliberately narrow natural-language parser for local timers and reminders.
/// Do not dispatch arbitrary prompts to a task runner.
/// </summary>
public static partial class ReminderParser
{
    [GeneratedRegex(@"^remind me in (?<count>\d{1,5}) (?<unit>seconds?|minutes?|hours?|days?) to (?<message>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RemindRegex();

    [GeneratedRegex(@"^(?:set )?(?:a )?timer (?:for )?(?<count>\d{1,5}) (?<unit>seconds?|minutes?|hours?)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TimerRegex();

    [GeneratedRegex(@"^cancel reminder #?(?<id>\d{1,8})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CancelRegex();

    public static ReminderCommand? Parse(string text)
    {
        var input = text.Trim();
        if (input.Equals("show reminders", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("list reminders", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("show timers", StringComparison.OrdinalIgnoreCase))
            return new ReminderCommand(ReminderKind.List, TimeSpan.Zero, "");

        var cancel = CancelRegex().Match(input);
        if (cancel.Success && int.TryParse(cancel.Groups["id"].Value, out var cancelId) && cancelId > 0)
            return new ReminderCommand(ReminderKind.Cancel, TimeSpan.Zero, "", cancelId);

        var reminder = RemindRegex().Match(input);
        var kind = ReminderKind.Reminder;
        var match = reminder;
        if (!match.Success)
        {
            match = TimerRegex().Match(input);
            kind = ReminderKind.Timer;
        }
        if (!match.Success || !int.TryParse(match.Groups["count"].Value, out var count) || count < 1)
            return null;

        var seconds = match.Groups["unit"].Value.ToLowerInvariant() switch
        {
            "second" or "seconds" => (double)count,
            "minute" or "minutes" => count * 60d,
            "hour" or "hours" => count * 3600d,
            "day" or "days" => count * 86400d,
            _ => 0d
        };
        if (seconds < 1 || seconds > TimeSpan.FromDays(90).TotalSeconds) return null;

        var message = kind == ReminderKind.Timer
            ? "Your timer is finished."
            : match.Groups["message"].Value.Trim();
        if (message.Length is 0 or > 250) return null;

        return new ReminderCommand(kind, TimeSpan.FromSeconds(seconds), message);
    }
}
