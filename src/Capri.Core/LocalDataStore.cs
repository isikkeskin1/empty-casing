using System.Text.Json;

namespace Capri.Core;

public sealed class LocalDataStore
{
    private readonly string _directory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public LocalDataStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapriDesktop");
        Directory.CreateDirectory(_directory);
    }

    public string CharacterFolder
    {
        get
        {
            var path = Path.Combine(_directory, "character");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public AppSettings LoadSettings() => Read<AppSettings>("settings.json") ?? new AppSettings();
    public IReadOnlyList<ChatTurn> LoadHistory() => Read<List<ChatTurn>>("history.json") ?? new List<ChatTurn>();

    public IReadOnlyList<PersonalMemory> LoadPersonalMemories() =>
        Read<List<PersonalMemory>>("personal-memories.json") ?? new List<PersonalMemory>();

    public void SavePersonalMemories(IEnumerable<PersonalMemory> notes) =>
        Write("personal-memories.json", notes.Take(PersonalMemoryPolicy.MaxItems).ToArray());

    public IReadOnlyList<ReminderEntry> LoadReminders() =>
        Read<List<ReminderEntry>>("reminders.json") ?? new List<ReminderEntry>();

    public void SaveReminders(IEnumerable<ReminderEntry> reminders) =>
        Write("reminders.json", reminders.TakeLast(300).ToArray());

    public void SaveSettings(AppSettings settings) => Write("settings.json", settings);
    public void SaveHistory(IEnumerable<ChatTurn> turns) =>
        Write("history.json", turns.TakeLast(120).ToArray());

    private T? Read<T>(string fileName)
    {
        var path = Path.Combine(_directory, fileName);
        if (!File.Exists(path)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch (JsonException) { return default; }
        catch (IOException) { return default; }
    }

    private void Write<T>(string fileName, T content)
    {
        var path = Path.Combine(_directory, fileName);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(content, JsonOptions));
        File.Move(temporary, path, true);
    }
}
