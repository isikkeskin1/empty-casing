using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Displays local owner-supplied PNG sequences when present, otherwise the
/// separately licensed default character animation shipped in the installer.
/// User art and third-party default art never share the app's MIT code licence.
/// </summary>
public sealed class FrameAnimator : IDisposable
{
    private sealed record Frame(BitmapSource Source, int DelayMs);

    private readonly string _baseFolder;
    public static string DefaultArtFolder =>
        Path.Combine(AppContext.BaseDirectory, "character-default");
    private readonly Action<BitmapSource?> _onFrame;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, IReadOnlyList<Frame>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Frame> _frames = Array.Empty<Frame>();
    private string _state = "idle";
    private int _index;

    public int CurrentDurationMs => _frames.Sum(frame => frame.DelayMs);

    public FrameAnimator(string baseFolder, Action<BitmapSource?> onFrame)
    {
        _baseFolder = baseFolder;
        _onFrame = onFrame;
        _timer = new DispatcherTimer(DispatcherPriority.Background);
        _timer.Tick += OnTick;
        SetState("idle");
    }

    public void SetState(string state)
    {
        _timer.Stop();
        _state = state;
        _index = 0;
        _frames = GetFrames(state);
        if (_frames.Count == 0 && state != "idle")
            _frames = GetFrames("idle");

        Publish();
        if (_frames.Count > 1)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_frames[0].DelayMs);
            _timer.Start();
        }
    }

    public void Reload()
    {
        _cache.Clear();
        SetState(_state);
    }

    private IReadOnlyList<Frame> GetFrames(string state)
    {
        if (_cache.TryGetValue(state, out var cached)) return cached;

        var path = Path.Combine(_baseFolder, state);
        // Owner-supplied/imported art always takes priority. Without it,
        // immediately render licensed animation frames shipped with the app,
        // rather than showing an empty "ART SLOT" and requiring ZIP import.
        if (!Directory.Exists(path) ||
            !Directory.EnumerateFiles(path, "*.png").Any())
            path = Path.Combine(DefaultArtFolder, state);
        var frames = new List<Frame>();
        if (Directory.Exists(path))
        {
            var timing = AnimationTiming.Load(Path.Combine(path, "timing.json"));
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.png")
                             .OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(Path.GetFullPath(file));
                        bitmap.EndInit();
                        bitmap.Freeze();
                        frames.Add(new Frame(bitmap, timing.DurationAt(frames.Count)));
                    }
                    catch (Exception ex) when (ex is IOException or InvalidOperationException
                                                     or NotSupportedException or UnauthorizedAccessException)
                    {
                        // Ignore damaged user-provided frames.
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A folder may be changed while the user is reloading assets.
            }
        }

        _cache[state] = frames;
        return frames;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_frames.Count <= 1) return;
        _index = (_index + 1) % _frames.Count;
        Publish();
        _timer.Interval = TimeSpan.FromMilliseconds(_frames[_index].DelayMs);
    }

    private void Publish() => _onFrame(_frames.Count == 0 ? null : _frames[_index].Source);

    public void Dispose() => _timer.Stop();
}
