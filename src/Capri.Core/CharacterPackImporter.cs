using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Capri.Core;

public sealed record CharacterPackResult(bool Installed, int FrameCount, string Message);

/// <summary>
/// User-owned animation pack importer. Existing art wins over bundled defaults.
/// ZIP data is never blindly extracted: only seven known frame directories,
/// numbered PNGs and their timing files can be written.
/// </summary>
public static partial class CharacterPackImporter
{
    public const string PackFileName = "Capri-Animation-Pack-v0.2.zip";
    public static readonly string[] States =
        ["idle", "talk", "glitch", "tossed", "sleep", "perch", "perch-music"];
    public const long MaxArchiveBytes = 40L * 1024 * 1024;
    public const long MaxExpandedBytes = 100L * 1024 * 1024;

    [GeneratedRegex(@"^(idle|talk|glitch|tossed|sleep|perch|perch-music)/(\d{4}\.png|timing\.json)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex AllowedEntry();

    public static bool IsComplete(string folder) =>
        States.All(state => Directory.Exists(Path.Combine(folder, state)) &&
            Directory.EnumerateFiles(Path.Combine(folder, state), "*.png").Any());

    public static string? FindBundledPack(string appDirectory)
    {
        var path = Path.Combine(appDirectory, PackFileName);
        return File.Exists(path) ? path : null;
    }

    public static CharacterPackResult InstallBundledIfNeeded(string appDirectory, string characterFolder)
    {
        var archive = FindBundledPack(appDirectory);
        if (archive is null)
            return new CharacterPackResult(false, 0,
                "No bundled character art. Import your private pack once under Character Art.");
        if (States.Any(state => Directory.Exists(Path.Combine(characterFolder, state)) &&
            Directory.EnumerateFiles(Path.Combine(characterFolder, state), "*.png").Any()))
            return new CharacterPackResult(false, 0,
                "Existing custom animation frames preserved.");
        return Import(archive, characterFolder, overwrite: false);
    }

    public static CharacterPackResult Import(string archivePath, string characterFolder, bool overwrite)
    {
        if (!File.Exists(archivePath))
            return new CharacterPackResult(false, 0, "Select an existing animation ZIP.");
        var length = new FileInfo(archivePath).Length;
        if (length is < 1024 or > MaxArchiveBytes)
            return new CharacterPackResult(false, 0,
                "Animation ZIP must be between 1 KB and 40 MB.");
        Directory.CreateDirectory(characterFolder);
        var staging = Path.Combine(characterFolder, ".stage-" + Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(characterFolder, ".backup-" + Guid.NewGuid().ToString("N"));
        var moved = new List<string>();
        var replaced = new List<string>();
        var frames = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        try
        {
            Directory.CreateDirectory(staging);
            using (var input = ZipFile.OpenRead(archivePath))
            {
                if (input.Entries.Count > 370)
                    throw new InvalidDataException("Animation ZIP has too many entries.");
                long expanded = 0;
                foreach (var entry in input.Entries)
                {
                    if (entry.FullName.EndsWith('/'))
                        continue;
                    var name = entry.FullName.Replace('\\', '/');
                    // Ignore legal notices in the archive, but never extract
                    // unknown files or paths, including traversal/symlinks.
                    if (name is "README.txt" or "THIRD_PARTY_NOTICES.txt")
                        continue;
                    var match = AllowedEntry().Match(name);
                    if (!match.Success)
                        throw new InvalidDataException("Unexpected file inside animation ZIP.");
                    var state = match.Groups[1].Value;
                    var file = match.Groups[2].Value;
                    if (entry.Length > 1024 * 1024)
                        throw new InvalidDataException("Animation frame is too large.");
                    expanded += entry.Length;
                    if (expanded > MaxExpandedBytes)
                        throw new InvalidDataException("Animation ZIP expands beyond 100 MB.");
                    if (file.EndsWith(".png", StringComparison.Ordinal))
                    {
                        if (!frames.TryGetValue(state, out var set))
                            frames[state] = set = new SortedSet<string>(StringComparer.Ordinal);
                        if (!set.Add(file) || set.Count > 100)
                            throw new InvalidDataException("Repeated or excessive frames.");
                    }
                    var destinationDir = Path.Combine(staging, state);
                    Directory.CreateDirectory(destinationDir);
                    var path = Path.Combine(destinationDir, file);
                    if (File.Exists(path))
                        throw new InvalidDataException("Repeated animation archive entry.");
                    entry.ExtractToFile(path);
                    if (file.EndsWith(".png", StringComparison.Ordinal))
                        VerifyPng(path);
                }
            }
            if (States.Any(state => !frames.TryGetValue(state, out var set) ||
                set.Count == 0 || !File.Exists(Path.Combine(staging, state, "timing.json"))))
                throw new InvalidDataException("Expected all seven animated states and timing files.");
            foreach (var state in States)
            {
                var paths = frames[state];
                if (!paths.SequenceEqual(Enumerable.Range(0, paths.Count)
                    .Select(i => i.ToString("D4") + ".png")))
                    throw new InvalidDataException("Animation frame numbers must be sequential.");
                using var document = JsonDocument.Parse(File.ReadAllText(
                    Path.Combine(staging, state, "timing.json")));
                if (!document.RootElement.TryGetProperty("frameDurationsMs", out var timing) ||
                    timing.ValueKind != JsonValueKind.Array ||
                    timing.GetArrayLength() != paths.Count ||
                    timing.EnumerateArray().Any(v => v.ValueKind != JsonValueKind.Number ||
                        !v.TryGetInt32(out var duration) || duration is < 20 or > 2000))
                    throw new InvalidDataException("Invalid animation timing metadata.");
            }
            foreach (var state in States)
            {
                var target = Path.Combine(characterFolder, state);
                if (!Directory.Exists(target))
                    continue;
                if (!overwrite)
                    return new CharacterPackResult(false, 0,
                        "Existing character frames are present. Select Replace in the import dialog.");
            }
            Directory.CreateDirectory(backup);
            foreach (var state in States)
            {
                var target = Path.Combine(characterFolder, state);
                if (Directory.Exists(target))
                {
                    Directory.Move(target, Path.Combine(backup, state));
                    replaced.Add(state);
                }
                Directory.Move(Path.Combine(staging, state), target);
                moved.Add(state);
            }
            var count = frames.Values.Sum(x => x.Count);
            return new CharacterPackResult(true, count,
                "Imported " + count + " frames across all seven character states.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            InvalidDataException or JsonException or System.ArgumentException)
        {
            // Restore preexisting character art if an update fails midway.
            foreach (var state in moved)
            {
                var path = Path.Combine(characterFolder, state);
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            foreach (var state in replaced)
            {
                var source = Path.Combine(backup, state);
                if (Directory.Exists(source))
                    Directory.Move(source, Path.Combine(characterFolder, state));
            }
            return new CharacterPackResult(false, 0,
                "Animation import failed: " + ex.Message);
        }
        finally
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (Directory.Exists(backup)) Directory.Delete(backup, true);
        }
    }

    private static void VerifyPng(string file)
    {
        using var input = File.OpenRead(file);
        Span<byte> header = stackalloc byte[24];
        if (input.Read(header) != 24 ||
            !header[..8].SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}) ||
            !header.Slice(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Invalid PNG frame.");
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
        var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));
        if (width is < 16 or > 1024 || height is < 16 or > 1024)
            throw new InvalidDataException("PNG frame dimensions are not supported.");
    }
}
