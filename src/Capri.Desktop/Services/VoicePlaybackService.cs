using System.IO;
using System.Windows.Threading;
using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>Plays Fish Audio MP3 entirely from memory; never writes a speech file to disk.</summary>
public sealed class VoicePlaybackService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private WaveOutEvent? _output;
    private Mp3FileReader? _reader;
    private MemoryStream? _stream;
    private bool _disposed;
    public int SpeakerDeviceIndex { get; set; } = -1;
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public event Action? PlaybackFinished;

    public VoicePlaybackService(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public void PlayMp3(byte[] mp3)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (mp3.Length < 64) throw new ArgumentException("Missing MP3 data.", nameof(mp3));
        Stop();
        var stream = new MemoryStream(mp3, writable: false);
        Mp3FileReader? reader = null;
        WaveOutEvent? output = null;
        try
        {
            reader = new Mp3FileReader(stream);
            if (!AudioInputDevices.SpeakerExists(SpeakerDeviceIndex))
                throw new InvalidOperationException("Selected speaker is unavailable.");
            output = new WaveOutEvent { DeviceNumber = SpeakerDeviceIndex };
            output.Init(reader);
            var currentOutput = output;
            output.PlaybackStopped += (_, _) => _dispatcher.BeginInvoke(() =>
            {
                if (!ReferenceEquals(_output, currentOutput)) return;
                Stop();
                PlaybackFinished?.Invoke();
            });
            _stream = stream;
            _reader = reader;
            _output = output;
            output.Play();
        }
        catch
        {
            output?.Dispose();
            reader?.Dispose();
            stream.Dispose();
            _stream = null;
            _reader = null;
            _output = null;
            throw;
        }
    }

    public void Stop()
    {
        var output = _output;
        _output = null;
        var reader = _reader;
        _reader = null;
        var stream = _stream;
        _stream = null;
        output?.Stop();
        output?.Dispose();
        reader?.Dispose();
        stream?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
