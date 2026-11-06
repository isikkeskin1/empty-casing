using System.Windows.Threading;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Maintains local reminders while the application is running. Overdue reminders fire after restart.
/// No external calendar access, notification service, or telemetry.
/// </summary>
public sealed class ReminderScheduler : IDisposable
{
    private readonly LocalDataStore _store;
    private readonly Action<ReminderEntry> _notify;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly List<ReminderEntry> _entries;

    public ReminderScheduler(LocalDataStore store, Action<ReminderEntry> notify)
    {
        _store = store;
        _notify = notify;
        _entries = store.LoadReminders().ToList();
        _timer.Tick += (_, _) => Evaluate();
        _timer.Start();
    }

    public string Execute(ReminderCommand command)
    {
        switch (command.Kind)
        {
            case ReminderKind.List:
                return Describe();
            case ReminderKind.Cancel:
                var target = _entries.FirstOrDefault(x => x.Id == command.CancelId && !x.Completed);
                if (target is null) return "No pending reminder #" + command.CancelId + ".";
                target.Completed = true;
                Save();
                return "Cancelled reminder #" + target.Id + ".";
            case ReminderKind.Timer:
            case ReminderKind.Reminder:
                if (_entries.Count(x => !x.Completed) >= 100)
                    return "Too many pending reminders (maximum 100).";
                var next = _entries.Count == 0 ? 1 : _entries.Max(x => x.Id) + 1;
                var due = DateTimeOffset.Now + command.Delay;
                _entries.Add(new ReminderEntry { Id = next, Message = command.Message, DueAt = due });
                Save();
                return (command.Kind == ReminderKind.Timer ? "Timer" : "Reminder") +
                    " #" + next + " set for " + due.ToLocalTime().ToString("ddd HH:mm:ss") + ".";
            default:
                return "That reminder command is not supported.";
        }
    }

    public string Describe()
    {
        var pending = _entries.Where(x => !x.Completed).OrderBy(x => x.DueAt).Take(20).ToArray();
        if (pending.Length == 0) return "No pending timers or reminders.";
        return string.Join("\n", pending.Select(x => "#" + x.Id + "  " +
            x.DueAt.ToLocalTime().ToString("ddd HH:mm") + "  " + x.Message));
    }

    private void Evaluate()
    {
        var due = _entries.Where(x => !x.Completed && x.DueAt <= DateTimeOffset.Now).ToArray();
        if (due.Length == 0) return;
        foreach (var reminder in due)
            reminder.Completed = true;
        Save();
        foreach (var reminder in due)
            _notify(reminder);
    }

    private void Save() => _store.SaveReminders(_entries);

    public void Dispose() => _timer.Stop();
}
