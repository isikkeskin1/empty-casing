using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Optional direct local microphone monitor into the SAME selected virtual
/// playback device as Capri's Fish output. Windows mixes the two streams for
/// the virtual recording input selected in Discord/Zoom. Never records disk,
/// transmits mic PCM to a provider, or opens without explicit Call Mode opt-in.
/// </summary>
public sealed class CallMicrophoneBridge : IDisposable
{
    private WaveInEvent? _capture;
    private WaveOutEvent? _cableOutput;
    private BufferedWaveProvider? _buffer;
    private bool _disposed;
    private volatile bool _active;
    public bool Active => _active && _capture is not null && _cableOutput is not null;
    /// <summary>Raised if the mic/cable unexpectedly stops. The owner must rearm.</summary>
    public event Action? BridgeStopped;

    private void UnexpectedStop()
    {
        if (!_active) return;
        _active = false;
        try { BridgeStopped?.Invoke(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    public void Start(int microphoneIndex, int virtualSpeakerIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Active) return;
        if (!AudioInputDevices.MicrophoneExists(microphoneIndex) ||
            virtualSpeakerIndex < 0 || !AudioInputDevices.SpeakerExists(virtualSpeakerIndex))
            throw new InvalidOperationException(
                "Choose an available real microphone and a non-default virtual cable playback device.");

        // The virtual cable output is the only destination. This does not
        // change the Windows default devices or Discord's own configuration.
        var format = new WaveFormat(16000, 16, 1);
        _buffer = new BufferedWaveProvider(format)
        {
            BufferDuration = TimeSpan.FromMilliseconds(450),
            DiscardOnBufferOverflow = true,
            ReadFully = true
        };
        try
        {
            _cableOutput = new WaveOutEvent
            {
                DeviceNumber = virtualSpeakerIndex,
                DesiredLatency = 120,
                NumberOfBuffers = 3
            };
            _cableOutput.Init(_buffer);
            _capture = AudioInputDevices.CreateCapture(microphoneIndex);
            _capture.DataAvailable += (_, args) =>
            {
                if (!_active) return;
                try { _buffer?.AddSamples(args.Buffer, 0, args.BytesRecorded); }
                catch (Exception ex) when (ex is InvalidOperationException or
                    ArgumentException)
                {
                    UnexpectedStop();
                }
            };
            _capture.RecordingStopped += (_, _) => UnexpectedStop();
            _cableOutput.PlaybackStopped += (_, _) => UnexpectedStop();
            _active = true;
            _cableOutput.Play();
            _capture.StartRecording();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public void Stop()
    {
        _active = false;
        var capture = _capture;
        _capture = null;
        try { capture?.StopRecording(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { capture?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }

        var output = _cableOutput;
        _cableOutput = null;
        try { output?.Stop(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { output?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        _buffer = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
