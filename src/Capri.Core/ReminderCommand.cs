using System.Globalization;
using System.Text.RegularExpressions;

namespace Capri.Core;

public enum ReminderKind { Timer, Reminder, List, Cancel }

public sealed record ReminderCommand(
    ReminderKind Kind, TimeSpan Delay, string Message, int CancelId = 0,
    DateTimeOffset? DueAt = null);

/// <summary>
/// Local-only timer and reminder parser. Accepts bounded relative times and
/// unambiguous calendar dates without using an AI model to invent timestamps.
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

    [GeneratedRegex(@"^remind me (?:on )?(?<date>tomorrow|\d{4}-\d{2}-\d{2}|\d{1,2} [A-Za-z]{3,9}(?: \d{4})?) at (?<time>\d{1,2}(?::\d{2})?(?: ?[ap]m)?) to (?<message>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteRegex();

    public static ReminderCommand? Parse(string text, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 400) return null;
        var input = text.Trim();
        if (input.Equals("show reminders", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("list reminders", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("show timers", StringComparison.OrdinalIgnoreCase))
            return new ReminderCommand(ReminderKind.List, TimeSpan.Zero, "");

        var cancel = CancelRegex().Match(input);
        if (cancel.Success && int.TryParse(cancel.Groups["id"].Value, out var cancelId) && cancelId > 0)
            return new ReminderCommand(ReminderKind.Cancel, TimeSpan.Zero, "", cancelId);

        var current = now ?? DateTimeOffset.Now;
        var absolute = AbsoluteRegex().Match(input);
        if (absolute.Success)
        {
            var message = absolute.Groups["message"].Value.Trim();
            if (message.Length is < 1 or > 250 || message.Any(char.IsControl))
                return null;

            var timeText = absolute.Groups["time"].Value.Trim().ToUpperInvariant().Replace("AM", " AM").Replace("PM", " PM");
            timeText = string.Join(" ", timeText.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            if (!DateTime.TryParseExact(timeText,
                    new[] { "H:mm", "HH:mm", "h tt", "hh tt", "h:mm tt", "hh:mm tt" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                return null;

            var dateText = absolute.Groups["date"].Value;
            DateTime date;
            if (dateText.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
                date = TimeZoneInfo.ConvertTime(current, TimeZoneInfo.Local).Date.AddDays(1);
            else
            {
                var formats = new[] { "yyyy-MM-dd", "d MMMM yyyy", "dd MMMM yyyy",
                    "d MMM yyyy", "dd MMM yyyy" };
                if (DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var dated))
                    date = dated.Date;
                else if (DateTime.TryParseExact(dateText,
                        new[] { "d MMMM", "dd MMMM", "d MMM", "dd MMM" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var partial))
                {
                    var localNow = TimeZoneInfo.ConvertTime(current, TimeZoneInfo.Local);
                    date = new DateTime(localNow.Year, partial.Month, partial.Day);
                    if (date < localNow.Date ||
                        (date == localNow.Date && time.TimeOfDay <= localNow.TimeOfDay))
                        date = date.AddYears(1);
                }
                else return null;
            }

            var localDateTime = DateTime.SpecifyKind(date.Add(time.TimeOfDay), DateTimeKind.Unspecified);
            if (TimeZoneInfo.Local.IsInvalidTime(localDateTime) ||
                TimeZoneInfo.Local.IsAmbiguousTime(localDateTime))
                return null;
            var due = new DateTimeOffset(localDateTime,
                TimeZoneInfo.Local.GetUtcOffset(localDateTime));
            var delay = due - current;
            if (delay <= TimeSpan.Zero || delay > TimeSpan.FromDays(365))
                return null;
            return new ReminderCommand(ReminderKind.Reminder, delay, message, DueAt: due);
        }

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

        var note = kind == ReminderKind.Timer
            ? "Your timer is finished."
            : match.Groups["message"].Value.Trim();
        if (note.Length is 0 or > 250 || note.Any(char.IsControl)) return null;
        return new ReminderCommand(kind, TimeSpan.FromSeconds(seconds), note);
    }
}
