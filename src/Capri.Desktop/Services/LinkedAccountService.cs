using System.Net.Http;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Capri.Core;

namespace Capri.Desktop.Services;

public sealed record LinkedAccountStatus(string Service, string Name, string Detail);
public sealed record SpotifyPlayback(string Title, string Artist, string Device, bool Playing)
{
    public string Summary => (Playing ? "Playing" : "Paused") + ": " + Title +
        (Artist.Length > 0 ? " — " + Artist : "") +
        (Device.Length > 0 ? " on " + Device : "");
}
public sealed record SpotifyTrackChoice(string Title, string Artist, string Uri)
{
    public string Display => Title + " — " + Artist;
}

internal sealed record StoredOAuthTokens(
    string ClientId, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

/// <summary>
/// Native provider APIs with OAuth PKCE and DPAPI-protected access/refresh tokens.
/// No model can supply network paths or read credentials. All API URLs are fixed.
/// </summary>
public sealed class LinkedAccountService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly SecretStore _secrets;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public LinkedAccountService(AppSettings settings, SecretStore secrets)
    {
        _settings = settings;
        _secrets = secrets;
        _http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        })
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CapriDesktop/0.1");
    }

    private static string Key(LinkedService service) =>
        service == LinkedService.Spotify ? "spotify" : "roblox";

    public string ClientId(LinkedService service)
    {
        // A consumer install uses Capri's registered app, not a developer app
        // created by each end-user. A local custom ID is a developer fallback.
        var packaged = PackagedOAuthClients.Get(service);
        return packaged.Length > 0 ? packaged :
            service == LinkedService.Spotify
                ? _settings.SpotifyClientId.Trim()
                : _settings.RobloxClientId.Trim();
    }

    public bool IsConnected(LinkedService service)
    {
        if (!_secrets.HasKey(Key(service))) return false;
        try
        {
            var token = Load(service);
            return token is not null &&
                   token.ClientId.Equals(ClientId(service), StringComparison.Ordinal) &&
                   token.RefreshToken.Length > 0;
        }
        catch { return false; }
    }

    public void Disconnect(LinkedService service) => _secrets.Delete(Key(service));

    public async Task<string> ConnectAsync(LinkedService service, CancellationToken cancellationToken)
    {
        var clientId = ClientId(service);
        if (!LinkedAccountPolicy.IsClientIdValid(clientId))
            return "Enter a valid " + LinkedAccountPolicy.ProviderName(service) +
                " OAuth client ID first.";

        var verifier = LinkedAccountPolicy.NewRandomValue();
        var state = LinkedAccountPolicy.NewRandomValue();
        var url = LinkedAccountPolicy.BuildAuthorizationUri(
            service, clientId, state, LinkedAccountPolicy.CodeChallenge(verifier));
        using var receiver = new OAuthLoopbackReceiver(service);
        try { receiver.Start(); }
        catch (System.Net.Sockets.SocketException)
        {
            return "Capri couldn't reserve the local OAuth callback port. Close other login attempts.";
        }

        Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
        var code = await receiver.WaitForCodeAsync(state, cancellationToken);

        using var request = new HttpRequestMessage(
            HttpMethod.Post, LinkedAccountPolicy.TokenEndpoint(service));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["redirect_uri"] = LinkedAccountPolicy.Redirect(service),
            ["code"] = code,
            ["code_verifier"] = verifier
        });
        var tokens = await ExchangeAsync(service, request, clientId, null, cancellationToken);
        Save(service, tokens);
        return LinkedAccountPolicy.ProviderName(service) +
            " linked successfully. Tokens are encrypted for this Windows user.";
    }

    private StoredOAuthTokens? Load(LinkedService service)
    {
        var raw = _secrets.Read(Key(service));
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JsonSerializer.Deserialize<StoredOAuthTokens>(raw); }
        catch (JsonException) { return null; }
    }

    private void Save(LinkedService service, StoredOAuthTokens token) =>
        _secrets.Save(Key(service), JsonSerializer.Serialize(token));

    private async Task<StoredOAuthTokens> ExchangeAsync(
        LinkedService service, HttpRequestMessage request, string clientId,
        string? previousRefresh, CancellationToken cancellationToken)
    {
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                LinkedAccountPolicy.ProviderName(service) + " OAuth rejected credentials (" +
                (int)response.StatusCode + "). Confirm redirect URI/client ID and account access.");
        using var payload = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var json = payload.RootElement;
        string Read(string key) =>
            json.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? "" : "";
        var access = Read("access_token");
        var refresh = Read("refresh_token");
        if (string.IsNullOrEmpty(refresh)) refresh = previousRefresh ?? "";
        var expires = json.TryGetProperty("expires_in", out var exp) &&
                      exp.TryGetInt32(out var seconds)
            ? Math.Clamp(seconds, 60, 86400) : 900;
        if (access.Length is < 8 or > 8192 || refresh.Length is < 8 or > 8192)
            throw new InvalidOperationException("Provider returned incomplete OAuth tokens.");
        return new StoredOAuthTokens(clientId, access, refresh,
            DateTimeOffset.UtcNow.AddSeconds(expires));
    }

    private async Task<string> AccessTokenAsync(
        LinkedService service, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            var token = Load(service);
            if (token is null || token.ClientId != ClientId(service))
                throw new InvalidOperationException(
                    "Account not connected. Open CONNECTIONS and link " +
                    LinkedAccountPolicy.ProviderName(service) + ".");
            if (token.ExpiresAt > DateTimeOffset.UtcNow.AddSeconds(55))
                return token.AccessToken;

            using var request = new HttpRequestMessage(
                HttpMethod.Post, LinkedAccountPolicy.TokenEndpoint(service));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = token.RefreshToken,
                ["client_id"] = token.ClientId
            });
            var renewed = await ExchangeAsync(service, request, token.ClientId,
                token.RefreshToken, cancellationToken);
            Save(service, renewed);
            return renewed.AccessToken;
        }
        finally { _refreshGate.Release(); }
    }

    private async Task<HttpResponseMessage> RequestAsync(
        LinkedService service, HttpMethod method, string relativePath,
        object? jsonBody, CancellationToken cancellationToken)
    {
        var url = service == LinkedService.Spotify
            ? "https://api.spotify.com/v1/" + relativePath
            : "https://apis.roblox.com/oauth/v1/" + relativePath;
        // Callers provide only hard-coded relative paths or properly encoded
        // Spotify query values, never an arbitrary model-supplied URL.
        using var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            await AccessTokenAsync(service, cancellationToken));
        if (jsonBody is not null)
            message.Content = new StringContent(
                JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");
        var result = await _http.SendAsync(message, cancellationToken);
        if (result.IsSuccessStatusCode ||
            result.StatusCode == HttpStatusCode.NoContent)
            return result;

        var status = (int)result.StatusCode;
        result.Dispose();
        throw new InvalidOperationException(
            LinkedAccountPolicy.ProviderName(service) + " API returned " + status +
            (status == 403 ? " (Premium, scope or developer-mode permission may be required)." :
             status == 429 ? " (rate limit; retry later)." :
             status == 401 ? " (reconnect this account)." : "."));
    }

    private static string JsonString(JsonElement obj, string key)
    {
        if (obj.ValueKind != JsonValueKind.Object ||
            !obj.TryGetProperty(key, out var prop) ||
            prop.ValueKind != JsonValueKind.String)
            return "";
        return MediaMetadataPolicy.Clean(prop.GetString(), 120);
    }

    public async Task<LinkedAccountStatus> ProfileAsync(
        LinkedService service, CancellationToken cancellationToken = default)
    {
        var path = service == LinkedService.Spotify ? "me" : "userinfo";
        using var response = await RequestAsync(
            service, HttpMethod.Get, path, null, cancellationToken);
        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        if (service == LinkedService.Spotify)
            return new LinkedAccountStatus("Spotify",
                JsonString(root, "display_name"),
                JsonString(root, "id"));
        return new LinkedAccountStatus("Roblox",
            JsonString(root, "name") is var display && display.Length > 0
                ? display : JsonString(root, "preferred_username"),
            "@" + JsonString(root, "preferred_username") + " · " +
            JsonString(root, "sub"));
    }

    public async Task<SpotifyPlayback?> SpotifyPlaybackAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await RequestAsync(LinkedService.Spotify,
            HttpMethod.Get, "me/player", null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var isPlaying = root.TryGetProperty("is_playing", out var play) &&
                        play.ValueKind == JsonValueKind.True;
        var item = root.TryGetProperty("item", out var current)
            ? current : default;
        var title = JsonString(item, "name");
        var artist = "";
        if (item.ValueKind == JsonValueKind.Object &&
            item.TryGetProperty("artists", out var artists) &&
            artists.ValueKind == JsonValueKind.Array &&
            artists.GetArrayLength() > 0)
            artist = JsonString(artists[0], "name");
        var device = root.TryGetProperty("device", out var d)
            ? JsonString(d, "name") : "";
        return new SpotifyPlayback(title, artist, device, isPlaying);
    }

    public async Task<string> SpotifyControlAsync(
        MediaIntent intent, CancellationToken cancellationToken = default)
    {
        var (method, path) = intent switch
        {
            MediaIntent.Play => (HttpMethod.Put, "me/player/play"),
            MediaIntent.Pause => (HttpMethod.Put, "me/player/pause"),
            MediaIntent.NextTrack => (HttpMethod.Post, "me/player/next"),
            MediaIntent.PreviousTrack => (HttpMethod.Post, "me/player/previous"),
            _ => throw new InvalidOperationException("Unsupported Spotify playback command.")
        };
        using var response = await RequestAsync(
            LinkedService.Spotify, method, path, null, cancellationToken);
        return "Spotify received " + intent + " on the active Spotify Connect device.";
    }

    public async Task<IReadOnlyList<SpotifyTrackChoice>> SpotifySearchAsync(
        string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length is > 120)
            throw new ArgumentException("Search term must be 1–120 characters.");
        using var response = await RequestAsync(
            LinkedService.Spotify, HttpMethod.Get,
            "search?q=" + Uri.EscapeDataString(query.Trim()) + "&type=track&limit=8",
            null, cancellationToken);
        using var doc = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        if (!root.TryGetProperty("tracks", out var tracks) ||
            !tracks.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
            return Array.Empty<SpotifyTrackChoice>();

        var results = new List<SpotifyTrackChoice>();
        foreach (var item in items.EnumerateArray().Take(8))
        {
            var title = JsonString(item, "name");
            var uri = JsonString(item, "uri");
            if (string.IsNullOrEmpty(title) ||
                !System.Text.RegularExpressions.Regex.IsMatch(uri,
                    @"^spotify:track:[A-Za-z0-9]{22}$"))
                continue;
            var artist = "";
            if (item.TryGetProperty("artists", out var artists) &&
                artists.ValueKind == JsonValueKind.Array &&
                artists.GetArrayLength() > 0)
                artist = JsonString(artists[0], "name");
            results.Add(new SpotifyTrackChoice(title, artist, uri));
        }
        return results;
    }

    public async Task<string> SpotifyQueueTrackAsync(
        SpotifyTrackChoice track, CancellationToken cancellationToken = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(track.Uri,
                @"^spotify:track:[A-Za-z0-9]{22}$"))
            return "Invalid Spotify track URI.";
        using var response = await RequestAsync(LinkedService.Spotify,
            HttpMethod.Post, "me/player/queue?uri=" + Uri.EscapeDataString(track.Uri),
            null, cancellationToken);
        return "Added to Spotify queue: " + track.Display;
    }

    public async Task<string> SpotifyPlayTrackAsync(
        SpotifyTrackChoice track, CancellationToken cancellationToken = default)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(track.Uri,
                @"^spotify:track:[A-Za-z0-9]{22}$"))
            return "Invalid Spotify track URI.";
        using var response = await RequestAsync(
            LinkedService.Spotify, HttpMethod.Put, "me/player/play",
            new { uris = new[] { track.Uri } }, cancellationToken);
        return "Spotify was asked to play " + track.Display + ".";
    }

    public void Dispose()
    {
        _http.Dispose();
        _refreshGate.Dispose();
    }
}
