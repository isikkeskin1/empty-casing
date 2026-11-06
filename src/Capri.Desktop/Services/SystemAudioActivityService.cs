using System.Windows.Threading;
using Capri.Core;
using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Optional Windows WASAPI loopback energy monitor. Frames are reduced immediately
/// to an RMS value; neither audio samples nor recordings are stored or uploaded.
/// </summary>
public sealed class SystemAudioActivityService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Action<bool> _onActive;
    private readonly Action _onPulse;
    private readonly Func<bool> _isCapriSpeaking;
    private readonly AudioPulseDetector _detector = new();
    private readonly object _lock = new();
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private WasapiLoopbackCapture? _capture;
    private bool _reportedActive;
    private bool _disposed;

    public bool IsEnabled => _capture is not null;

    public SystemAudioActivityService(
        Dispatcher dispatcher, Action<bool> onActive, Action onPulse,
        Func<bool> isCapriSpeaking)
    {
        _dispatcher = dispatcher;
        _onActive = onActive;
        _onPulse = onPulse;
        _isCapriSpeaking = isCapriSpeaking;
        _tick.Tick += (_, _) => PollActivity();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_capture is not null) return;
        lock (_lock) _detector.Reset();

        WasapiLoopbackCapture? capture = null;
        try
        {
            capture = new WasapiLoopbackCapture();
            var current = capture;
            capture.DataAvailable += (_, args) =>
            {
                if (!ReferenceEquals(_capture, current)) return;
                if (_isCapriSpeaking()) return;
                var rms = ComputeRms(args.Buffer, args.BytesRecorded, current.WaveFormat.BitsPerSample);
                AudioPulseDetector.Reading result;
                lock (_lock)
                    result = _detector.Observe(rms, DateTimeOffset.UtcNow);
                if (result.Pulse)
                    _dispatcher.BeginInvoke(_onPulse);
            };
            capture.RecordingStopped += (_, _) => _dispatcher.BeginInvoke(() =>
            {
                if (ReferenceEquals(_capture, current)) Stop();
            });
            _capture = capture;
            capture.StartRecording();
            _tick.Start();
        }
        catch
        {
            _capture = null;
            capture?.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        var capture = _capture;
        _capture = null;
        _tick.Stop();
        try { capture?.StopRecording(); }
        catch (InvalidOperationException) { }
        capture?.Dispose();
        lock (_lock) _detector.Reset();

        if (_reportedActive)
        {
            _reportedActive = false;
            _onActive(false);
        }
    }

    private void PollActivity()
    {
        if (_capture is null) return;
        bool active;
        lock (_lock) active = _detector.IsActive(DateTimeOffset.UtcNow);
        if (_isCapriSpeaking()) active = false;
        if (active == _reportedActive) return;
        _reportedActive = active;
        _onActive(active);
    }

    /// <summary>
    /// WASAPI loopback generally returns 32-bit IEEE float. Also accept 16-bit
    /// PCM endpoints; unsupported encodings simply produce no dance pulses.
    /// Samples are examined but never retained.
    /// </summary>
    public static double ComputeRms(byte[] buffer, int count, int bitsPerSample)
    {
        if (count <= 0 || buffer.Length == 0) return 0;
        var length = Math.Min(count, buffer.Length);
        double energy = 0;
        int samples = 0;
        if (bitsPerSample == 32)
        {
            for (var i = 0; i + 3 < length; i += 4)
            {
                var sample = BitConverter.ToSingle(buffer, i);
                if (!float.IsFinite(sample) || Math.Abs(sample) > 1.1) continue;
                energy += sample * sample;
                samples++;
            }
        }
        else if (bitsPerSample == 16)
        {
            for (var i = 0; i + 1 < length; i += 2)
            {
                var sample = BitConverter.ToInt16(buffer, i) / 32768.0;
                energy += sample * sample;
                samples++;
            }
        }
        return samples == 0 ? 0 : Math.Clamp(Math.Sqrt(energy / samples), 0, 1);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
