using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Capri.Desktop.Services;

/// <summary>
/// Loads only user-supplied frames from the app's local character folder.
/// No character artwork is included in the executable.
/// </summary>
public sealed class FrameAnimator : IDisposable
{
    private readonly string _baseFolder;
    private readonly Action<BitmapSource?> _onFrame;
    private readonly DispatcherTimer _timer;
    private IReadOnlyList<BitmapSource> _frames = Array.Empty<BitmapSource>();
    private string _state = "idle";
    private int _index;

    public FrameAnimator(string baseFolder, Action<BitmapSource?> onFrame)
    {
        _baseFolder = baseFolder;
        _onFrame = onFrame;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += OnTick;
        SetState("idle");
        _timer.Start();
    }

    public void SetState(string state)
    {
        _state = state;
        _index = 0;
        _frames = LoadFrames(Path.Combine(_baseFolder, state));
        if (_frames.Count == 0 && state != "idle")
            _frames = LoadFrames(Path.Combine(_baseFolder, "idle"));
        Publish();
    }

    public void Reload() => SetState(_state);

    private static IReadOnlyList<BitmapSource> LoadFrames(string path)
    {
        if (!Directory.Exists(path)) return Array.Empty<BitmapSource>();
        var frames = new List<BitmapSource>();
        foreach (var file in Directory.EnumerateFiles(path, "*.png").OrderBy(x => x, StringComparer.Ordinal))
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(Path.GetFullPath(file));
                image.EndInit();
                image.Freeze();
                frames.Add(image);
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or InvalidOperationException)
            {
                // Skip broken or unsupported image files without blocking startup.
            }
        }
        return frames;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_frames.Count == 0) return;
        _index = (_index + 1) % _frames.Count;
        Publish();
    }

    private void Publish() => _onFrame(_frames.Count == 0 ? null : _frames[_index]);

    public void Dispose() => _timer.Stop();
}
