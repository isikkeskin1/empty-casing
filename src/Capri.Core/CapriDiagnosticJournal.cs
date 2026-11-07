using System.Runtime.InteropServices;
using System.Text;

namespace Capri.Core;

/// <summary>
/// Opt-in-to-share, local-only diagnostic journal. It records event *categories*
/// and exception *types* only. Never exception messages, stack traces, source
/// paths, saved memories, chat, passwords, microphone audio or screen contents.
/// </summary>
public sealed class CapriDiagnosticJournal
{
    public const int MaxFileBytes = 48 * 1024;
    public const int MaxEntryCount = 60;
    private readonly string _path;
    private readonly object _gate = new();
    private static readonly HashSet<string> AllowedEvents =
        new(StringComparer.Ordinal)
        { "startup", "shutdown", "ui-unhandled", "app-unhandled",
          "task-unobserved", "update-check", "unknown" };

    public CapriDiagnosticJournal(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "diagnostics.log");
    }

    public string PathToJournal => _path;

    public void Record(string category, Exception? error = null)
    {
        var safeCategory = AllowedEvents.Contains(category) ? category : "unknown";
        var type = error?.GetType().Name ?? "none";
        // Only the type name is retained; a custom exception type may have an
        // unusual name, so limit it to an ASCII identifier of at most 80 chars.
        var safeType = new string(type.Take(80).Where(c =>
            (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
            (c >= '0' && c <= '9') || c == '_').ToArray());
        if (safeType.Length == 0) safeType = "Exception";
        var line = DateTimeOffset.UtcNow.ToString("O") + "  " +
                   safeCategory + "  " + safeType;
        lock (_gate)
        {
            try
            {
                // Bound storage and remove oldest lines; support bundle remains
                // useful offline even after repeated faults.
                var history = File.Exists(_path)
                    ? File.ReadLines(_path).TakeLast(MaxEntryCount - 1).ToList()
                    : new List<string>();
                history.Add(line);
                var all = string.Join(Environment.NewLine, history) +
                          Environment.NewLine;
                while (Encoding.UTF8.GetByteCount(all) > MaxFileBytes &&
                       history.Count > 1)
                {
                    history.RemoveAt(0);
                    all = string.Join(Environment.NewLine, history) +
                          Environment.NewLine;
                }
                File.WriteAllText(_path, all, new UTF8Encoding(false));
            }
            catch (IOException) { /* Never prevent shutdown because of diagnostics. */ }
            catch (UnauthorizedAccessException) { }
        }
    }

    public string ReadSupportReport(Version version)
    {
        var safeEvents = Array.Empty<string>();
        lock (_gate)
        {
            try
            {
                if (File.Exists(_path))
                    safeEvents = File.ReadLines(_path).TakeLast(20)
                        .Where(line => line.Length is > 15 and < 200 &&
                            !line.Contains('\r') && !line.Contains('\t'))
                        .ToArray();
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return "CAPRI DESKTOP — LOCAL SUPPORT REPORT\n" +
               "App version: " + version.ToString(3) + "\n" +
               "Windows version: " + Environment.OSVersion.Version + "\n" +
               "System architecture: " + RuntimeInformation.OSArchitecture + "\n" +
               ".NET runtime: " + Environment.Version + "\n" +
               "Generated (UTC): " + DateTimeOffset.UtcNow.ToString("O") + "\n" +
               "Private data: chat history, memory, credentials, file names, " +
               "window names and screen content excluded.\n" +
               "\nRECENT EVENT TYPES (newest last):\n" +
               (safeEvents.Length == 0
                    ? "No local errors logged."
                    : string.Join("\n", safeEvents));
    }
}
