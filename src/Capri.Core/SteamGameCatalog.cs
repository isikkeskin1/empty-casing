using System.Text.RegularExpressions;

namespace Capri.Core;

public sealed record SteamGame(uint AppId, string Name);

/// <summary>
/// Reads only Steam's local libraryfolders.vdf and appmanifest_*.acf metadata.
/// Never opens game executables, modifies Steam files, or downloads titles.
/// </summary>
public static partial class SteamGameCatalog
{
    private const int MaxManifestBytes = 256 * 1024;
    private const int MaxLibraries = 24;
    private const int MaxManifestsPerLibrary = 3000;
    private const int MaxCatalog = 5000;

    [GeneratedRegex(@"""(?<key>[^""]+)""\s+""(?<value>(?:\\.|[^""\\])*)""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueRegex();

    public static string? ReadValue(string? vdf, string key)
    {
        if (string.IsNullOrWhiteSpace(vdf)) return null;
        foreach (Match pair in KeyValueRegex().Matches(vdf))
        {
            if (pair.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
                return Unescape(pair.Groups["value"].Value);
        }
        return null;
    }

    public static IReadOnlyList<string> ParseLibraryPaths(string? vdf)
    {
        if (string.IsNullOrWhiteSpace(vdf)) return Array.Empty<string>();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in KeyValueRegex().Matches(vdf))
        {
            if (!match.Groups["key"].Value.Equals("path", StringComparison.OrdinalIgnoreCase))
                continue;
            var path = Unescape(match.Groups["value"].Value).Trim();
            // Local rooted paths only. No UNC, device paths or relative traversal.
            if (path.Length < 3 || path.Length > 245 ||
                path.StartsWith(@"\\", StringComparison.Ordinal) ||
                path.StartsWith("//", StringComparison.Ordinal) ||
                !Path.IsPathFullyQualified(path) ||
                path.Contains('\0'))
                continue;
            found.Add(Path.GetFullPath(path));
            if (found.Count >= MaxLibraries) break;
        }
        return found.ToArray();
    }

    public static SteamGame? ParseManifest(string? vdf)
    {
        if (string.IsNullOrWhiteSpace(vdf) || vdf.Length > MaxManifestBytes) return null;
        var appId = ReadValue(vdf, "appid");
        var name = ReadValue(vdf, "name")?.Trim();
        if (!uint.TryParse(appId, out var id) || id is 0 ||
            string.IsNullOrWhiteSpace(name) || name.Length > 130 ||
            name.Any(char.IsControl))
            return null;
        return new SteamGame(id, name);
    }

    public static IReadOnlyList<SteamGame> Discover(string steamRoot)
    {
        if (string.IsNullOrWhiteSpace(steamRoot) || !Path.IsPathFullyQualified(steamRoot))
            return Array.Empty<SteamGame>();
        try
        {
            steamRoot = Path.GetFullPath(steamRoot);
            if (!Directory.Exists(steamRoot)) return Array.Empty<SteamGame>();
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { steamRoot };
            var libraryFile = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryFile) &&
                new FileInfo(libraryFile).Length <= MaxManifestBytes)
            {
                foreach (var library in ParseLibraryPaths(File.ReadAllText(libraryFile)))
                    libraries.Add(library);
            }

            var games = new Dictionary<uint, SteamGame>();
            foreach (var library in libraries.Take(MaxLibraries))
            {
                var dir = Path.Combine(library, "steamapps");
                if (!Directory.Exists(dir)) continue;
                foreach (var manifest in Directory.EnumerateFiles(dir, "appmanifest_*.acf")
                             .Take(MaxManifestsPerLibrary))
                {
                    try
                    {
                        var file = new FileInfo(manifest);
                        if (file.Length <= 0 || file.Length > MaxManifestBytes ||
                            (file.Attributes & FileAttributes.ReparsePoint) != 0)
                            continue;
                        var game = ParseManifest(File.ReadAllText(manifest));
                        // Verify the filename ID matches the manifest ID.
                        if (game is null ||
                            !string.Equals(Path.GetFileNameWithoutExtension(manifest),
                                "appmanifest_" + game.AppId, StringComparison.OrdinalIgnoreCase))
                            continue;
                        games.TryAdd(game.AppId, game);
                        if (games.Count >= MaxCatalog) break;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                if (games.Count >= MaxCatalog) break;
            }
            return games.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (IOException) { return Array.Empty<SteamGame>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<SteamGame>(); }
    }

    public static SteamGame? FindUnique(IReadOnlyList<SteamGame> games, string? requestedName)
    {
        if (string.IsNullOrWhiteSpace(requestedName) || requestedName.Length > 130)
            return null;
        var query = requestedName.Trim();
        var exact = games.Where(g => g.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (exact.Length == 1) return exact[0];
        if (exact.Length > 1) return null;
        var matches = games.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    public static string? ParseLaunchRequest(string? request)
    {
        if (string.IsNullOrWhiteSpace(request) || request.Length > 220) return null;
        var text = request.Trim().TrimEnd('.', '!', '?');
        foreach (var prefix in new[]
        {
            "launch game ", "open game ", "start game ",
            "play game ", "launch ", "play ", "start "
        })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = text[prefix.Length..].Trim();
                return value.Length is > 0 and <= 130 ? value : null;
            }
        }
        return null;
    }

    public static string LaunchUri(SteamGame game) => "steam://rungameid/" + game.AppId;

    private static string Unescape(string value) =>
        value.Replace(@"\\", @"\", StringComparison.Ordinal)
             .Replace("\\\"", "\"", StringComparison.Ordinal);
}
