using System.Reflection;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Public OAuth application identifiers configured once by the Capri publisher at build
/// time, not by every person who installs Capri. These are NOT passwords or tokens.
/// Never package provider client secrets, access tokens, or refresh tokens.
/// </summary>
public static class PackagedOAuthClients
{
    public static string Get(LinkedService provider)
    {
        var name = provider == LinkedService.Spotify
            ? "CapriSpotifyClientId" : "CapriRobloxClientId";
        var id = typeof(PackagedOAuthClients).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == name)?.Value ?? "";
        return LinkedAccountPolicy.IsClientIdValid(id) ? id : "";
    }
}
