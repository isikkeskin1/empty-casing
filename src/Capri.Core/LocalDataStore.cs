using System.Text.Json;

namespace Capri.Core;

public sealed class LocalDataStore
{
    private readonly string _directory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private bool _settingsRecoveredFromBackup;

    /// <summary>
    /// True when settings.json was invalid/unreadable and a previous known-good
    /// settings.json.backup was loaded. No chat, personal memory or credentials
    /// are duplicated into recovery backups.
    /// </summary>
    public bool SettingsRecoveredFromBackup => _settingsRecoveredFromBackup;

    public LocalDataStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapriDesktop");
        Directory.CreateDirectory(_directory);
    }

    public string DataFolder => _directory;

    public string CharacterFolder
    {
        get
        {
            var path = Path.Combine(_directory, "character");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    public AppSettings LoadSettings()
    {
        _settingsRecoveredFromBackup = false;
        var primary = Read<AppSettings>("settings.json");
        if (primary is not null) return primary;
        var backup = Read<AppSettings>("settings.json.backup");
        if (backup is not null)
        {
            _settingsRecoveredFromBackup = true;
            return backup;
        }
        return new AppSettings();
    }
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
        catch (UnauthorizedAccessException) { return default; }
        catch (NotSupportedException) { return default; }
    }

    private void Write<T>(string fileName, T content)
    {
        var path = Path.Combine(_directory, fileName);
        // A same-directory temporary file makes replacement atomic on NTFS.
        // Only settings get a backup: retaining stale chat/memory after a
        // deliberate deletion would violate the owner's privacy expectations.
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(content, JsonOptions));
            if (fileName == "settings.json" && File.Exists(path) &&
                !_settingsRecoveredFromBackup)
            {
                File.Replace(temporary, path, path + ".backup");
            }
            else
            {
                // If we recovered from a corrupt primary, keep the good
                // backup intact rather than replacing it with broken bytes.
                File.Move(temporary, path, true);
            }
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
        }
    }
}
