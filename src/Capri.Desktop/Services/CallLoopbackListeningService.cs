using System.Speech.AudioFormat;
using System.Speech.Recognition;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Capri.Desktop.Services;

/// <summary>
/// Per-session, explicitly authorized, local speech recognition from the Windows
/// default playback endpoint (e.g. a voice call). No recordings, raw cloud audio,
/// replay, persistence, or hidden capture.
/// </summary>
public sealed class CallLoopbackListeningService : IDisposable
{
    private readonly Action<string> _heard;
    private readonly Func<bool> _capriIsSpeaking;
    private WasapiLoopbackCapture? _capture;
    private SpeechRecognitionEngine? _recognizer;
    private BufferedMicrophoneStream? _stream;
    private BufferedWaveProvider? _buffered;
    private IWaveProvider? _pcm;
    private System.Threading.Timer? _silencePump;
    private long _lastAudioAt;
    private volatile bool _active;
    private bool _disposed;

    public bool IsListening => _active;

    public CallLoopbackListeningService(Action<string> heard, Func<bool> capriIsSpeaking)
    {
        _heard = heard;
        _capriIsSpeaking = capriIsSpeaking;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_active) return;

        var languages = SpeechRecognitionEngine.InstalledRecognizers();
        if (languages.Count == 0)
            throw new InvalidOperationException(
                "Install a Windows Speech Recognition language pack before using Call Mode.");

        try
        {
            _capture = new WasapiLoopbackCapture();
            var format = _capture.WaveFormat;
            if (format.Channels is < 1 or > 2)
                throw new InvalidOperationException(
                    "Call Mode currently supports mono/stereo Windows playback devices.");

            _buffered = new BufferedWaveProvider(format)
            {
                BufferDuration = TimeSpan.FromSeconds(3),
                DiscardOnBufferOverflow = true,
                ReadFully = false
            };
            ISampleProvider sample = _buffered.ToSampleProvider();
            if (sample.WaveFormat.Channels == 2)
                sample = new StereoToMonoSampleProvider(sample)
                {
                    LeftVolume = 0.5f, RightVolume = 0.5f
                };

            sample = new WdlResamplingSampleProvider(sample, 16000);
            _pcm = sample.ToWaveProvider16();
            _stream = new BufferedMicrophoneStream();

            _recognizer = new SpeechRecognitionEngine(languages[0]);
            _recognizer.LoadGrammar(new DictationGrammar());
            _recognizer.SetInputToAudioStream(_stream,
                new SpeechAudioFormatInfo(
                    16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
            _recognizer.SpeechRecognized += (_, e) =>
            {
                if (_active && !_capriIsSpeaking() && e.Result.Confidence >= 0.75 &&
                    !string.IsNullOrWhiteSpace(e.Result.Text))
                    _heard(e.Result.Text);
            };

            _capture.DataAvailable += (_, args) =>
            {
                if (!_active || _capriIsSpeaking()) return;
                var buffer = _buffered;
                var pcm = _pcm;
                var stream = _stream;
                if (buffer is null || pcm is null || stream is null) return;
                try
                {
                    buffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
                    var durationSeconds = (double)args.BytesRecorded /
                                          Math.Max(1, format.AverageBytesPerSecond);
                    var estimate = Math.Clamp(
                        (int)(durationSeconds * 16000 * 2) + 640, 640, 16384);
                    var output = new byte[estimate];
                    var length = pcm.Read(output, 0, output.Length);
                    if (length > 0) stream.Push(output, length);
                    Interlocked.Exchange(ref _lastAudioAt, Environment.TickCount64);
                }
                catch (Exception ex) when (ex is InvalidOperationException or
                    ObjectDisposedException or ArgumentException) { }
            };
            _capture.RecordingStopped += (_, _) =>
            {
                // The session controls the lifetime; do not silently restart a
                // closed or disconnected output device.
                _active = false;
            };

            _active = true;
            Interlocked.Exchange(ref _lastAudioAt, Environment.TickCount64);
            _capture.StartRecording();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);

            // WASAPI loopback may stop reporting frames during silence.
            // Locally synthesize zero samples so dictation can finish phrases.
            _silencePump = new System.Threading.Timer(_ =>
            {
                if (!_active) return;
                if (Environment.TickCount64 -
                    Interlocked.Read(ref _lastAudioAt) < 160) return;
                try { _stream?.Push(new byte[3200], 3200); }
                catch (ObjectDisposedException) { }
            }, null, 100, 100);
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
        _silencePump?.Dispose();
        _silencePump = null;
        var capture = _capture;
        _capture = null;
        try { capture?.StopRecording(); }
        catch (InvalidOperationException) { }
        capture?.Dispose();

        _stream?.Complete();
        var recognizer = _recognizer;
        _recognizer = null;
        try { recognizer?.RecognizeAsyncCancel(); }
        catch (InvalidOperationException) { }
        recognizer?.Dispose();
        _stream?.Dispose();
        _stream = null;
        _pcm = null;
        _buffered = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
