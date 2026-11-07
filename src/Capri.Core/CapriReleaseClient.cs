using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Capri.Core;

public sealed record CapriRelease(
    Version Version, string Tag, Uri ReleasePage,
    DateTimeOffset? PublishedAt, bool HasWindowsInstaller);

/// <summary>
/// A deliberately read-only GitHub release check. Never follows asset URLs,
/// downloads installers, executes update binaries or accepts arbitrary hosts.
/// </summary>
public static partial class CapriReleasePolicy
{
    public const string Repository = "isikkeskin1/empty-casing";
    public const string ReleasesPage = "https://github.com/isikkeskin1/empty-casing/releases";
    public const string BuildsPage =
        "https://github.com/isikkeskin1/empty-casing/actions/workflows/windows.yml";
    public const string LatestApi =
        "https://api.github.com/repos/isikkeskin1/empty-casing/releases/latest";
    public const int MaxResponseBytes = 512 * 1024;

    [GeneratedRegex(@"^v?(?<major>0|[1-9][0-9]{0,3})\.(?<minor>0|[1-9][0-9]{0,3})\.(?<patch>0|[1-9][0-9]{0,3})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex VersionTagPattern();

    public static bool TryParseVersionTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag) || tag.Length > 19) return false;
        var match = VersionTagPattern().Match(tag);
        if (!match.Success ||
            !int.TryParse(match.Groups["major"].Value, out var major) ||
            !int.TryParse(match.Groups["minor"].Value, out var minor) ||
            !int.TryParse(match.Groups["patch"].Value, out var patch))
            return false;
        version = new Version(major, minor, patch);
        return true;
    }

    public static bool IsNewer(Version current, CapriRelease available) =>
        available.Version.CompareTo(new Version(
            Math.Max(0, current.Major), Math.Max(0, current.Minor),
            Math.Max(0, current.Build))) > 0;

    public static bool IsOfficialReleasePage(Uri? url, string tag)
    {
        if (url is null || !url.IsAbsoluteUri ||
            url.Scheme != Uri.UriSchemeHttps ||
            url.Host != "github.com" || url.Port != 443 ||
            url.UserInfo.Length != 0 ||
            url.Query.Length != 0 || url.Fragment.Length != 0 ||
            !TryParseVersionTag(tag, out _))
            return false;
        return url.AbsolutePath == "/isikkeskin1/empty-casing/releases/tag/" + tag;
    }

    public static CapriRelease? ParseLatestRelease(string json)
    {
        if (string.IsNullOrWhiteSpace(json) ||
            json.Length > MaxResponseBytes)
            throw new InvalidDataException("Release metadata is too large.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("tag_name", out var tagNode) ||
            tagNode.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("html_url", out var pageNode) ||
            pageNode.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("GitHub release metadata is incomplete.");
        if (root.TryGetProperty("draft", out var draft) &&
            draft.ValueKind == JsonValueKind.True ||
            root.TryGetProperty("prerelease", out var prerelease) &&
            prerelease.ValueKind == JsonValueKind.True)
            return null;
        var tag = tagNode.GetString() ?? "";
        if (!TryParseVersionTag(tag, out var version) ||
            !Uri.TryCreate(pageNode.GetString(), UriKind.Absolute, out var url) ||
            !IsOfficialReleasePage(url, tag))
            throw new InvalidDataException("The release tag or link was not accepted.");
        DateTimeOffset? published = null;
        if (root.TryGetProperty("published_at", out var timestamp) &&
            timestamp.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(timestamp.GetString(), out var parsed))
            published = parsed;
        var installer = false;
        if (root.TryGetProperty("assets", out var assets) &&
            assets.ValueKind == JsonValueKind.Array)
            installer = assets.EnumerateArray().Any(a =>
                a.ValueKind == JsonValueKind.Object &&
                a.TryGetProperty("name", out var name) &&
                name.ValueKind == JsonValueKind.String &&
                name.GetString() == "Capri-Setup.exe");
        return new CapriRelease(version, tag, url!, published, installer);
    }
}

public sealed class CapriReleaseClient
{
    private readonly HttpClient _client;

    public CapriReleaseClient(HttpClient? client = null) =>
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

    public async Task<CapriRelease?> CheckLatestAsync(CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, CapriReleasePolicy.LatestApi);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CapriDesktop", "0.6"));
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await _client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("GitHub rate limited this update check. Try again later.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("GitHub releases returned HTTP " +
                (int)response.StatusCode + ".");
        if (response.Content.Headers.ContentLength >
            CapriReleasePolicy.MaxResponseBytes)
            throw new InvalidDataException("Release metadata is too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var bounded = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(chunk, token)) != 0)
        {
            if (bounded.Length + count > CapriReleasePolicy.MaxResponseBytes)
                throw new InvalidDataException("Release metadata exceeded the limit.");
            bounded.Write(chunk, 0, count);
        }
        return CapriReleasePolicy.ParseLatestRelease(
            System.Text.Encoding.UTF8.GetString(bounded.ToArray()));
    }
}
