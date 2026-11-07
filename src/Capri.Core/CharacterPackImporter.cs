using System.IO.Compression;
using System.Text.Json;

namespace Capri.Core;

public sealed record AnimationPackPreview(int States, int Frames, long UncompressedBytes);

/// <summary>
/// Explicit local import of seven owner-provided animation sequences. A ZIP is
/// never trusted to write arbitrary paths, overwrite executable code or run
/// programs. Existing character art is restored if import is interrupted.
/// </summary>
public static class CharacterPackImporter
{
    public static readonly string[] States =
        ["idle", "talk", "glitch", "tossed", "sleep", "perch", "perch-music"];
    public const int MaxFrames = 500;
    public const long MaxZipBytes = 70L * 1024 * 1024;
    public const long MaxInflatedBytes = 120L * 1024 * 1024;
    public const long MaxFrameBytes = 3L * 1024 * 1024;
    public const int MinStates = 7;
    private static readonly HashSet<string> AllowedStates =
        new(States, StringComparer.Ordinal);

    public static AnimationPackPreview Validate(string zipPath)
    {
        if (!File.Exists(zipPath) || new FileInfo(zipPath).Length is <= 0 or > MaxZipBytes)
            throw new InvalidDataException("Animation ZIP is missing or too large.");
        using var archive = ZipFile.OpenRead(zipPath);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var frames = new Dictionary<string, int>(StringComparer.Ordinal);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue;
            if (!seen.Add(entry.FullName))
                throw new InvalidDataException("Duplicate animation file name.");
            if (entry.FullName.Contains('\\') || entry.FullName.Contains("..") ||
                entry.FullName.StartsWith('/') || entry.FullName.Contains(':') ||
                entry.FullName.Any(char.IsControl))
                throw new InvalidDataException("Unsafe animation filename.");
            var segments = entry.FullName.Split('/');
            if (segments.Length != 2 || !AllowedStates.Contains(segments[0]))
            {
                // Top-level notices are allowed, never extracted.
                if (segments.Length == 1 && entry.Name is
                    ("README.txt" or "THIRD_PARTY_NOTICES.txt"))
                    continue;
                throw new InvalidDataException("Unexpected ZIP entry: only seven state folders are accepted.");
            }
            var basename = segments[1];
            var isTiming = basename == "timing.json";
            var validFrame = basename.Length == 8 && basename.EndsWith(".png",
                StringComparison.Ordinal) && basename[..4].All(char.IsAsciiDigit);
            if (!isTiming && !validFrame)
                throw new InvalidDataException("Unexpected file in animation state.");
            if (entry.Length is < 1 || > MaxFrameBytes ||
                (isTiming && entry.Length > 4096))
                throw new InvalidDataException("Animation frame or timing manifest is invalid.");
            total += entry.Length;
            if (total > MaxInflatedBytes)
                throw new InvalidDataException("Animation pack is too large after extraction.");
            if (validFrame)
            {
                frames[segments[0]] = frames.GetValueOrDefault(segments[0]) + 1;
                if (frames.Values.Sum() > MaxFrames)
                    throw new InvalidDataException("Too many animation frames.");
                // Only PNG signatures are accepted: no renamed scripts or payloads.
                using var stream = entry.Open();
                Span<byte> signature = stackalloc byte[8];
                if (stream.Read(signature) != 8 ||
                    !signature.SequenceEqual(new byte[] { 137,80,78,71,13,10,26,10 }))
                    throw new InvalidDataException("Invalid PNG frame signature.");
            }
            else
            {
                using var stream = entry.Open();
                using var document = JsonDocument.Parse(stream);
                if (document.RootElement.ValueKind != JsonValueKind.Object ||
                    !document.RootElement.TryGetProperty("frameDurationsMs", out var delays) ||
                    delays.ValueKind != JsonValueKind.Array ||
                    delays.GetArrayLength() > MaxFrames ||
                    delays.EnumerateArray().Any(d =>
                        d.ValueKind != JsonValueKind.Number ||
                        !d.TryGetInt32(out var value) || value is < 20 or > 30000))
                    throw new InvalidDataException("Invalid frame timing data.");
            }
        }
        if (frames.Count != MinStates || States.Any(x => frames.GetValueOrDefault(x) == 0))
            throw new InvalidDataException("Pack must contain frames in all seven animation states.");
        return new AnimationPackPreview(frames.Count, frames.Values.Sum(), total);
    }

    public static AnimationPackPreview Import(string zipPath, string target)
    {
        var preview = Validate(zipPath);
        var absoluteTarget = Path.GetFullPath(target);
        var parent = Directory.GetParent(absoluteTarget)?.FullName ??
            throw new InvalidOperationException("Invalid character folder.");
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".character-import-" + Guid.NewGuid().ToString("N"));
        var backup = Path.Combine(parent, ".character-previous-" + Guid.NewGuid().ToString("N"));
        var swapped = false;
        try
        {
            Directory.CreateDirectory(staging);
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    var segments = entry.FullName.Split('/');
                    if (segments.Length != 2 || !AllowedStates.Contains(segments[0]))
                        continue;
                    var folder = Path.Combine(staging, segments[0]);
                    Directory.CreateDirectory(folder);
                    var destination = Path.Combine(folder, segments[1]);
                    using var input = entry.Open();
                    using var output = File.Create(destination);
                    input.CopyTo(output);
                }
            }
            // ZIP contents are fully staged before replacing existing artwork.
            if (Directory.Exists(absoluteTarget))
            {
                Directory.Move(absoluteTarget, backup);
                swapped = true;
            }
            Directory.Move(staging, absoluteTarget);
            if (swapped) Directory.Delete(backup, recursive: true);
            return preview;
        }
        catch
        {
            if (swapped)
            {
                if (Directory.Exists(absoluteTarget))
                    Directory.Delete(absoluteTarget, recursive: true);
                if (Directory.Exists(backup))
                    Directory.Move(backup, absoluteTarget);
            }
            throw;
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
        }
    }
}
