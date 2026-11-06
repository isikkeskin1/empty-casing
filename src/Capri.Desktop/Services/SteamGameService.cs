using System.Diagnostics;
using Microsoft.Win32;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Steam game discovery and launch through the registered Steam protocol.
/// There is no arbitrary executable path, argument concatenation or web search.
/// </summary>
public sealed class SteamGameService
{
    private IReadOnlyList<SteamGame>? _cached;
    private DateTimeOffset _cachedAt;

    public IReadOnlyList<SteamGame> GetInstalledGames(bool refresh = false)
    {
        if (!refresh && _cached is not null &&
            DateTimeOffset.UtcNow - _cachedAt < TimeSpan.FromMinutes(3))
            return _cached;

        _cached = SteamGameCatalog.Discover(FindSteamFolder() ?? "");
        _cachedAt = DateTimeOffset.UtcNow;
        return _cached;
    }

    public string Launch(SteamGame game)
    {
        // A game must come from the current installed manifest catalog.
        if (!GetInstalledGames().Any(g => g.AppId == game.AppId &&
                                         g.Name.Equals(game.Name, StringComparison.Ordinal)))
            return "That game is not in the installed Steam catalog. Refresh the list.";

        try
        {
            Process.Start(new ProcessStartInfo(SteamGameCatalog.LaunchUri(game))
            {
                UseShellExecute = true
            });
            return "Sent Steam a launch request for " + game.Name +
                   ". Steam may still need to update the game or show its launcher.";
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or
            InvalidOperationException or FileNotFoundException)
        {
            return "Steam launch failed (" + ex.GetType().Name +
                   "). Check Steam is installed and running.";
        }
    }

    private static string? FindSteamFolder()
    {
        try
        {
            using var user = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = user?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
                return steamPath;
            using var machine = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Valve\Steam");
            steamPath = machine?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(steamPath) && Directory.Exists(steamPath))
                return steamPath;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) { }

        var x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var fallback = Path.Combine(x86, "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }
}
