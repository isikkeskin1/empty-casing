using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Capri.Core;

/// <summary>
/// Integrity check for locally cached ML model weights. A matching file size
/// alone is NOT proof that bytes are authentic or safe to load natively.
/// </summary>
public static class VerifiedLocalModelPolicy
{
    public static bool IsVerified(string path, long expectedLength, string expectedSha256)
    {
        if (string.IsNullOrWhiteSpace(path) || expectedLength <= 0 ||
            string.IsNullOrWhiteSpace(expectedSha256) ||
            !Regex.IsMatch(expectedSha256, "^[0-9a-fA-F]{64}$",
                RegexOptions.CultureInvariant))
            return false;
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.Read, 128 * 1024, FileOptions.SequentialScan);
            if (file.Length != expectedLength) return false;
            var digest = SHA256.HashData(file);
            var expected = Convert.FromHexString(expectedSha256);
            return CryptographicOperations.FixedTimeEquals(digest, expected);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or
                                  ArgumentException or NotSupportedException)
        {
            return false;
        }
    }
}
