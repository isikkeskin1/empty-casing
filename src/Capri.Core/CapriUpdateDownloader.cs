using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Capri.Core;

/// <summary>
/// Stages only the official versioned Capri installer when GitHub release
/// metadata supplies both a matching exact URL and a SHA256 asset digest.
/// The downloaded executable is never run by this class.
/// </summary>
public sealed class CapriUpdateDownloader
{
    private readonly HttpClient _client;
    public CapriUpdateDownloader(HttpClient? client = null) =>
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromMinutes(7) };

    public async Task<string> DownloadVerifiedAsync(
        CapriRelease release, string stagingRoot, CancellationToken cancellationToken = default)
    {
        if (release.InstallerAsset is not { } asset ||
            !CapriReleasePolicy.IsOfficialInstallerUrl(asset.DownloadUrl, release.Tag) ||
            asset.Size is < 1024 or > 200000000 ||
            asset.Sha256.Length != 64 || !asset.Sha256.All(Uri.IsHexDigit))
            throw new InvalidOperationException(
                "Release has no trusted installer digest, size and official download URL.");

        Directory.CreateDirectory(stagingRoot);
        var folder = Path.Combine(stagingRoot, "release-" +
            release.Tag.Replace(".", "-").Replace("v", ""));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "Capri-Setup.exe");
        var pending = Path.Combine(folder, "download-" +
            Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, asset.DownloadUrl);
            request.Headers.UserAgent.Add(
                new ProductInfoHeaderValue("CapriDesktop", "0.7"));
            using var response = await _client.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long len &&
                len != asset.Size)
                throw new InvalidDataException("Installer length did not match GitHub release metadata.");
            await using (var target = new FileStream(pending, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            using (var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[64 * 1024];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > asset.Size || total > 200000000)
                        throw new InvalidDataException("Installer exceeded the expected file size.");
                    sha.AppendData(buffer.AsSpan(0, read));
                    await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                if (total != asset.Size ||
                    !Convert.ToHexString(sha.GetHashAndReset())
                        .Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "Downloaded installer failed GitHub's size or SHA256 verification.");
            }
            File.Move(pending, finalPath, overwrite: true);
            return finalPath;
        }
        finally
        {
            if (File.Exists(pending)) File.Delete(pending);
        }
    }
}
