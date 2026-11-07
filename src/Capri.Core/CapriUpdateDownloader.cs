using System.Net;
using System.Security.Cryptography;

namespace Capri.Core;

/// <summary>
/// A GitHub-only, versioned update downloader. Updates never execute here:
/// the desktop app may offer "Restart to update" only after an exact SHA256
/// match against an official GitHub release asset digest.
/// </summary>
public sealed class CapriUpdateDownloader : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsClient;
    public const long MaxInstallerBytes = 250_000_000;

    public CapriUpdateDownloader(HttpClient? client = null)
    {
        _ownsClient = client is null;
        _http = client ?? new HttpClient(
            new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    private static bool IsAllowedDownloadUri(Uri uri) =>
        uri.IsAbsoluteUri && uri.Scheme == Uri.UriSchemeHttps &&
        uri.UserInfo.Length == 0 && uri.Port == 443 &&
        (uri.Host == "github.com" ||
         uri.Host == "release-assets.githubusercontent.com" ||
         uri.Host == "objects.githubusercontent.com");

    public static bool IsValidInstaller(CapriRelease release) =>
        release.VerifiedInstaller is { } asset &&
        CapriReleasePolicy.IsOfficialReleasePage(release.ReleasePage, release.Tag) &&
        CapriReleasePolicy.TryParseVersionTag(release.Tag, out _) &&
        asset.SizeBytes is >= 1000000 and <= MaxInstallerBytes &&
        System.Text.RegularExpressions.Regex.IsMatch(asset.Sha256Hex,
            "^[a-f0-9]{64}$") &&
        asset.DownloadUrl.AbsoluteUri ==
            "https://github.com/isikkeskin1/empty-casing/releases/download/" +
            release.Tag + "/Capri-Setup.exe";

    public static string? GetVerifiedStagedInstaller(
        CapriRelease release, string localDataFolder)
    {
        if (!IsValidInstaller(release)) return null;
        var path = Path.Combine(localDataFolder, "updates", release.Tag, "Capri-Setup.exe");
        return VerifyFile(path, release.VerifiedInstaller!) ? path : null;
    }

    public static bool VerifyFile(string path, CapriReleaseAsset asset)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length != asset.SizeBytes) return false;
            using var input = File.OpenRead(path);
            var sha = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(sha),
                System.Text.Encoding.ASCII.GetBytes(asset.Sha256Hex));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task<string> DownloadVerifiedAsync(
        CapriRelease release, string localDataFolder,
        Action<long, long>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidInstaller(release))
            throw new InvalidDataException("Release has no verified official installer digest.");
        var asset = release.VerifiedInstaller!;
        var existing = GetVerifiedStagedInstaller(release, localDataFolder);
        if (existing is not null) return existing;
        var outputDirectory = Path.Combine(localDataFolder, "updates", release.Tag);
        Directory.CreateDirectory(outputDirectory);
        var destination = Path.Combine(outputDirectory, "Capri-Setup.exe");
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            var uri = asset.DownloadUrl;
            HttpResponseMessage? response = null;
            for (var redirect = 0; redirect <= 4; ++redirect)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsAllowedDownloadUri(uri))
                    throw new InvalidDataException("Update redirected outside approved GitHub hosts.");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd("CapriDesktop-Updater/0.7");
                response = await _http.SendAsync(request,
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
                {
                    var location = response.Headers.Location;
                    response.Dispose();
                    response = null;
                    if (location is null || redirect == 4)
                        throw new InvalidDataException("Too many update redirects.");
                    uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                    continue;
                }
                break;
            }
            using (response)
            {
                if (response is null || !response.IsSuccessStatusCode)
                    throw new HttpRequestException("Installer download returned an HTTP error.");
                if (response.Content.Headers.ContentLength is long contentLength &&
                    contentLength != asset.SizeBytes)
                    throw new InvalidDataException("Installer size did not match the release metadata.");
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, 65536, useAsync: true);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[65536];
                long written = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    written += count;
                    if (written > asset.SizeBytes || written > MaxInstallerBytes)
                        throw new InvalidDataException("Installer exceeded the expected size.");
                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    reportProgress?.Invoke(written, asset.SizeBytes);
                }
                await output.FlushAsync(cancellationToken);
                var actualDigest = Convert.ToHexString(hash.GetHashAndReset())
                    .ToLowerInvariant();
                if (written != asset.SizeBytes ||
                    !CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.ASCII.GetBytes(actualDigest),
                        System.Text.Encoding.ASCII.GetBytes(asset.Sha256Hex)))
                    throw new InvalidDataException("Downloaded installer failed SHA-256 verification.");
            }
            File.Move(temp, destination, overwrite: true);
            return destination;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (IOException) { }
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _http.Dispose();
    }
}
