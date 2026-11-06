namespace Capri.Core;

public sealed class ReminderEntry
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset DueAt { get; set; }
    public bool Completed { get; set; }
}
