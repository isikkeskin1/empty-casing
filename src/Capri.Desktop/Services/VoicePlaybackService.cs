using System.IO;
using System.Windows.Threading;
using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Fish MP3 output stays in RAM. A late NAudio completion or a missing speaker
/// must not crash the WPF dispatcher or complete a newer voice session.
/// </summary>
public sealed class VoicePlaybackService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private WaveOutEvent? _output;
    private Mp3FileReader? _reader;
    private MemoryStream? _stream;
    private bool _disposed;
    public int SpeakerDeviceIndex { get; set; } = -1;
    public bool IsPlaying => !_disposed && _output?.PlaybackState == PlaybackState.Playing;
    public event Action? PlaybackFinished;
    public event Action<string>? PlaybackFailed;

    public VoicePlaybackService(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public void PlayMp3(byte[] mp3)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(mp3);
        if (mp3.Length < 64) throw new ArgumentException("Missing MP3 data.", nameof(mp3));

        // Playback has only one owner, the Capri UI thread. Stopping an old
        // output clears its identity before NAudio sends PlaybackStopped.
        Stop();
        var stream = new MemoryStream(mp3, writable: false);
        Mp3FileReader? reader = null;
        WaveOutEvent? output = null;
        try
        {
            reader = new Mp3FileReader(stream);
            if (!AudioInputDevices.SpeakerExists(SpeakerDeviceIndex))
                throw new InvalidOperationException(
                    "Selected output speaker disappeared. Select an available speaker in INPUTS.");
            output = new WaveOutEvent { DeviceNumber = SpeakerDeviceIndex };
            output.Init(reader);
            var thisOutput = output;
            output.PlaybackStopped += (_, args) =>
            {
                // NAudio may emit this from an audio worker after Stop/Dispose,
                // after a replacement track starts, or during app shutdown.
                if (_disposed || _dispatcher.HasShutdownStarted ||
                    _dispatcher.HasShutdownFinished) return;
                try
                {
                    _dispatcher.BeginInvoke(() =>
                    {
                        if (_disposed || !ReferenceEquals(_output, thisOutput))
                            return;
                        Stop(); // Ignore nested PlaybackStopped: _output is null.
                        if (args.Exception is not null)
                            NotifyFailure("Windows audio output stopped unexpectedly.");
                        NotifyFinished();
                    });
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                    TaskCanceledException) { /* WPF shutdown race */ }
            };
            _stream = stream;
            _reader = reader;
            _output = output;
            output.Play();
        }
        catch
        {
            // Set fields to null BEFORE native resource teardown so any
            // reentrant playback callback is harmless.
            _output = null;
            _reader = null;
            _stream = null;
            SafelyStopAndDispose(output);
            SafelyDispose(reader);
            SafelyDispose(stream);
            throw;
        }
    }

    private void NotifyFinished()
    {
        try { PlaybackFinished?.Invoke(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    private void NotifyFailure(string message)
    {
        try { PlaybackFailed?.Invoke(message); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    private static void SafelyStopAndDispose(WaveOutEvent? output)
    {
        if (output is null) return;
        try { output.Stop(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { output.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    private static void SafelyDispose(IDisposable? value)
    {
        if (value is null) return;
        try { value.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    public void Stop()
    {
        var output = _output;
        _output = null;
        var reader = _reader;
        _reader = null;
        var stream = _stream;
        _stream = null;
        SafelyStopAndDispose(output);
        SafelyDispose(reader);
        SafelyDispose(stream);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
