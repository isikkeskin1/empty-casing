using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Capri.Core;

public enum LinkedService { Spotify, Roblox }

/// <summary>
/// Authorization Code + PKCE, using a loopback redirect registered by the
/// app owner with the corresponding provider. No client secret is embedded.
/// </summary>
public static partial class LinkedAccountPolicy
{
    public const int SpotifyPort = 18441;
    public const int RobloxPort = 18442;
    public static readonly TimeSpan OAuthTimeout = TimeSpan.FromMinutes(3);

    [GeneratedRegex("^[A-Za-z0-9_\\-]{8,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex ClientIdPattern();

    public static string ProviderName(LinkedService service) =>
        service == LinkedService.Spotify ? "Spotify" : "Roblox";

    public static bool IsClientIdValid(string? clientId) =>
        !string.IsNullOrWhiteSpace(clientId) && ClientIdPattern().IsMatch(clientId) &&
        !clientId.StartsWith("sk_", StringComparison.OrdinalIgnoreCase) &&
        !clientId.StartsWith("sk-", StringComparison.OrdinalIgnoreCase) &&
        !clientId.StartsWith("nvapi-", StringComparison.OrdinalIgnoreCase) &&
        !clientId.StartsWith("RBX-CR", StringComparison.OrdinalIgnoreCase);

    public static int Port(LinkedService service) =>
        service == LinkedService.Spotify ? SpotifyPort : RobloxPort;

    public static string Redirect(LinkedService service) =>
        $"http://127.0.0.1:{Port(service)}/oauth/{ProviderName(service).ToLowerInvariant()}/";

    public static string AuthorizationEndpoint(LinkedService service) =>
        service == LinkedService.Spotify ? "https://accounts.spotify.com/authorize" :
            "https://apis.roblox.com/oauth/v1/authorize";

    public static string TokenEndpoint(LinkedService service) =>
        service == LinkedService.Spotify ? "https://accounts.spotify.com/api/token" :
            "https://apis.roblox.com/oauth/v1/token";

    public static string Scopes(LinkedService service) =>
        service == LinkedService.Spotify
            ? "user-read-private user-read-playback-state user-modify-playback-state"
            : "openid profile";

    public static string NewRandomValue() =>
        Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string CodeChallenge(string codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) || codeVerifier.Length is < 43 or > 128)
            throw new ArgumentException("Invalid PKCE verifier.", nameof(codeVerifier));
        return Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static Uri BuildAuthorizationUri(
        LinkedService service, string clientId, string state,
        string codeChallenge)
    {
        if (!IsClientIdValid(clientId) || state.Length is < 32 or > 128 ||
            codeChallenge.Length is < 32 or > 128)
            throw new ArgumentException("OAuth application or PKCE values are invalid.");
        var parameters = new (string Name, string Value)[]
        {
            ("client_id", clientId),
            ("response_type", "code"),
            ("redirect_uri", Redirect(service)),
            ("scope", Scopes(service)),
            ("state", state),
            ("code_challenge_method", "S256"),
            ("code_challenge", codeChallenge)
        };
        return new Uri(AuthorizationEndpoint(service) + "?" + string.Join("&",
            parameters.Select(pair =>
                Uri.EscapeDataString(pair.Name) + "=" + Uri.EscapeDataString(pair.Value))));
    }

    public static string? ParseCallbackCode(
        LinkedService service, Uri? callbackUri, string expectedState)
    {
        if (callbackUri is null || callbackUri.Scheme != "http" ||
            callbackUri.Host != "127.0.0.1" || callbackUri.Port != Port(service) ||
            callbackUri.AbsolutePath != new Uri(Redirect(service)).AbsolutePath ||
            callbackUri.Fragment.Length != 0)
            return null;

        var parameters = callbackUri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .GroupBy(v => Uri.UnescapeDataString(v[0]), StringComparer.Ordinal)
            .ToArray();
        // Reject duplicate state/code, which can be interpreted differently
        // by HTTP libraries and potentially create a login CSRF confusion.
        if (parameters.Any(g => g.Count() > 1)) return null;
        string? Value(string name) =>
            parameters.FirstOrDefault(g => g.Key == name)?
                .Select(x => Uri.UnescapeDataString(x.Length > 1 ? x[1].Replace('+', ' ') : ""))
                .FirstOrDefault();

        var observedState = Value("state");
        if (observedState is null ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(observedState), Encoding.UTF8.GetBytes(expectedState)))
            return null;
        if (Value("error") is not null) return null;
        var code = Value("code");
        if (code is null || code.Length is < 8 or > 2000 ||
            !code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~'))
            return null;
        return code;
    }

    public static bool IsRobloxPlaceId(string? input) =>
        ulong.TryParse(input, out var id) && id > 0 &&
        input is { Length: <= 20 } && input.All(char.IsAsciiDigit);

    public static Uri RobloxExperienceUri(string placeId)
    {
        if (!IsRobloxPlaceId(placeId)) throw new ArgumentException("Invalid Roblox place ID.");
        // Legacy documented Roblox game-start link; Roblox now recommends
        // share links for creator campaigns, which can also be opened in-app.
        return new Uri("https://www.roblox.com/games/start?placeId=" + placeId);
    }

    public static bool IsAllowedRobloxShareUri(Uri? uri) =>
        uri is { Scheme: "https", Host: "www.roblox.com" or "roblox.com" } &&
        uri.AbsolutePath.StartsWith("/share", StringComparison.OrdinalIgnoreCase);
}
