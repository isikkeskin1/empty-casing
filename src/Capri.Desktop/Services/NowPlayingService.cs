using Capri.Core;
using Windows.Media.Control;

namespace Capri.Desktop.Services;

/// <summary>
/// Windows SMTC integration. Reads only current song/video metadata and controls
/// the same currently selected media session; never reads audio or browsing history.
/// </summary>
public sealed class NowPlayingService
{
    private static async Task<GlobalSystemMediaTransportControlsSession?> CurrentAsync(
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        token.ThrowIfCancellationRequested();
        return manager.GetCurrentSession();
    }

    public async Task<NowPlayingInfo?> GetCurrentAsync(CancellationToken token = default)
    {
        var session = await CurrentAsync(token);
        if (session is null) return null;
        token.ThrowIfCancellationRequested();

        var media = await session.TryGetMediaPropertiesAsync();
        token.ThrowIfCancellationRequested();
        var isPlaying = session.GetPlaybackInfo()?.PlaybackStatus ==
                        GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        return new NowPlayingInfo(
            MediaMetadataPolicy.Clean(media?.Title),
            MediaMetadataPolicy.Clean(media?.Artist),
            MediaMetadataPolicy.Clean(session.SourceAppUserModelId),
            isPlaying);
    }

    public async Task<string> ExecuteAsync(MediaIntent intent, CancellationToken token = default)
    {
        if (intent is not (MediaIntent.Pause or MediaIntent.Play or
            MediaIntent.TogglePlayback or MediaIntent.NextTrack or MediaIntent.PreviousTrack))
            return "Unsupported media command.";

        var session = await CurrentAsync(token);
        if (session is null)
            return "No Windows media session is available; open Spotify or a video first.";
        token.ThrowIfCancellationRequested();
        var ok = intent switch
        {
            MediaIntent.Pause => await session.TryPauseAsync(),
            MediaIntent.Play => await session.TryPlayAsync(),
            MediaIntent.TogglePlayback => await session.TryTogglePlayPauseAsync(),
            MediaIntent.NextTrack => await session.TrySkipNextAsync(),
            MediaIntent.PreviousTrack => await session.TrySkipPreviousAsync(),
            _ => false
        };
        token.ThrowIfCancellationRequested();
        return ok ? intent switch
        {
            MediaIntent.Pause => "Paused the active Windows media session.",
            MediaIntent.Play => "Resumed the active Windows media session.",
            MediaIntent.TogglePlayback => "Toggled playback of the active media session.",
            MediaIntent.NextTrack => "Requested the next track.",
            MediaIntent.PreviousTrack => "Requested the previous track.",
            _ => "Media control sent."
        } : "The active media app did not accept that control.";
    }
}
