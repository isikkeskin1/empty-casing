namespace Capri.Core;

public enum MediaIntent { None, Status, Pause, Play, TogglePlayback, NextTrack, PreviousTrack }

public sealed record NowPlayingInfo(
    string Title, string Artist, string Source, bool IsPlaying)
{
    public string FriendlyDescription
    {
        get
        {
            var title = MediaMetadataPolicy.Clean(Title);
            var artist = MediaMetadataPolicy.Clean(Artist);
            var source = MediaMetadataPolicy.Clean(Source);
            if (title.Length == 0)
                return "Windows has no supported media title available.";
            return (IsPlaying ? "Playing: " : "Paused: ") + title +
                   (artist.Length > 0 ? " — " + artist : "") +
                   (source.Length > 0 ? " (" + source + ")" : "") + ".";
        }
    }
}

public static class MediaMetadataPolicy
{
    public static string Clean(string? value, int max = 95)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var s = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }

    public static MediaIntent Parse(string? request)
    {
        if (string.IsNullOrWhiteSpace(request) || request.Length > 180) return MediaIntent.None;
        var text = request.Trim().TrimEnd('.', '!', '?').ToLowerInvariant();

        if (text is "what song is playing" or "what's playing" or "whats playing" or
            "what music is playing" or "what video is playing" or "what is playing" or
            "now playing" or "show now playing" or "what track is this" or
            "what am i listening to" or "what are we listening to")
            return MediaIntent.Status;

        if (text is "pause music" or "pause the song" or "pause song" or
            "pause video" or "pause the video")
            return MediaIntent.Pause;

        if (text is "resume music" or "resume video" or "resume the song" or
            "resume playback" or "play music" or "play the music" or
            "unpause music" or "unpause video")
            return MediaIntent.Play;

        if (text is "toggle playback" or "play pause")
            return MediaIntent.TogglePlayback;

        if (text is "next song" or "next track" or "skip song" or
            "skip track" or "skip the song" or "skip to next song")
            return MediaIntent.NextTrack;

        if (text is "previous song" or "previous track" or "last song" or
            "go back one song" or "replay previous song")
            return MediaIntent.PreviousTrack;

        return MediaIntent.None;
    }

    public static bool ShouldAnnounce(NowPlayingInfo? current, NowPlayingInfo? last)
    {
        if (current is null || !current.IsPlaying ||
            string.IsNullOrWhiteSpace(current.Title))
            return false;
        return last is null ||
            !string.Equals(current.Title, last.Title, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(current.Artist, last.Artist, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(current.Source, last.Source, StringComparison.OrdinalIgnoreCase);
    }
}
