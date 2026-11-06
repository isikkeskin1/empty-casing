using System.Diagnostics;
using System.Runtime.InteropServices;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// On-demand app/media context for a single explicitly opted-in conversation.
/// Reads process name, never titles, window text, chat, files or screenshots.
/// </summary>
public sealed class AmbientContextService
{
    private readonly WindowDockingService _windows = new();
    private readonly NowPlayingService _media;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(
        IntPtr windowHandle, out uint processId);

    public AmbientContextService(NowPlayingService media) => _media = media;

    public async Task<AmbientContext> ReadOnceAsync(CancellationToken token = default)
    {
        var application = "";
        try
        {
            var front = _windows.FrontmostWindow();
            if (front is { } target &&
                GetWindowThreadProcessId(target.Handle, out var processId) != 0 &&
                processId > 0 && processId <= int.MaxValue)
            {
                using var process = Process.GetProcessById((int)processId);
                application = process.ProcessName;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
            InvalidOperationException or System.ComponentModel.Win32Exception) { }

        var title = "";
        var artist = "";
        try
        {
            var playing = await _media.GetCurrentAsync(token)
                .WaitAsync(TimeSpan.FromSeconds(2), token);
            if (playing?.IsPlaying == true)
            {
                title = playing.Title;
                artist = playing.Artist;
            }
        }
        catch (Exception ex) when (ex is TimeoutException or
            InvalidOperationException or System.Runtime.InteropServices.COMException)
        { /* Unsupported media apps simply produce no media hint. */ }

        return new AmbientContext(application, title, artist);
    }
}
