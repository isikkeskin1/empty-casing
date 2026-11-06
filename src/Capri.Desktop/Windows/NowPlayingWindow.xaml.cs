using System.Windows;
using System.Windows.Threading;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class NowPlayingWindow : Window
{
    private readonly DispatcherTimer _refresh = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };
    private bool _busy;

    public NowPlayingWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        _refresh.Tick += async (_, _) => await RefreshAsync();
        Loaded += async (_, _) =>
        {
            _refresh.Start();
            await RefreshAsync();
        };
        Closed += (_, _) => _refresh.Stop();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var playing = await App.Instance.NowPlaying.GetCurrentAsync();
            SongText.Text = playing is null
                ? "No active media session" :
                string.IsNullOrWhiteSpace(playing.Title) ? "Unnamed media" : playing.Title;
            ArtistText.Text = playing?.Artist ?? "";
            SourceText.Text = playing?.Source ?? "";
            PlaybackText.Text = playing is null ? "No compatible media app detected" :
                playing.IsPlaying ? "● PLAYING" : "Ⅱ PAUSED / STOPPED";
        }
        catch (Exception ex)
        {
            SongText.Text = "Media session unavailable";
            ArtistText.Text = "";
            SourceText.Text = "";
            PlaybackText.Text = ex.GetType().Name;
        }
        finally { _busy = false; }
    }

    private async Task ControlAsync(MediaIntent intent)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            FeedbackText.Text = await App.Instance.TryControlMediaAsync(intent);
        }
        catch (Exception ex)
        {
            FeedbackText.Text = "Media control failed: " + ex.GetType().Name;
        }
        finally { _busy = false; }
        await RefreshAsync();
    }

    private void Previous_Click(object sender, RoutedEventArgs e) =>
        _ = ControlAsync(MediaIntent.PreviousTrack);
    private void Pause_Click(object sender, RoutedEventArgs e) =>
        _ = ControlAsync(MediaIntent.Pause);
    private void Play_Click(object sender, RoutedEventArgs e) =>
        _ = ControlAsync(MediaIntent.Play);
    private void Next_Click(object sender, RoutedEventArgs e) =>
        _ = ControlAsync(MediaIntent.NextTrack);
}
