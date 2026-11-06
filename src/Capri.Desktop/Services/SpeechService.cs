using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Local Windows speech recognition driven by the user's chosen recording device.
/// PCM is buffered only in memory and is never uploaded by this service.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private SpeechRecognitionEngine? _recognizer;
    private WaveInEvent? _capture;
    private BufferedMicrophoneStream? _pcmStream;
    private SpeechSynthesizer? _speaker;
    private readonly Action<string> _heard;
    private volatile bool _listening;
    private volatile bool _speaking;
    private bool _disposed;

    public event Action? SpeechFinished;
    public bool IsListening => _listening;
    public bool IsSpeaking => _speaking;

    public SpeechService(Action<string> heard) => _heard = heard;

    public void StartListening(int deviceIndex = -1)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listening) return;
        StopListening();

        if (!AudioInputDevices.MicrophoneExists(deviceIndex))
            throw new InvalidOperationException("The selected recording device is unavailable.");

        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        if (installed.Count == 0)
            throw new InvalidOperationException(
                "Windows Speech Recognition has no installed language pack. Add a speech language in Settings.");

        try
        {
            _recognizer = new SpeechRecognitionEngine(installed[0]);
            _recognizer.LoadGrammar(new DictationGrammar());
            _pcmStream = new BufferedMicrophoneStream();
            _recognizer.SetInputToAudioStream(_pcmStream,
                new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
            _recognizer.SpeechRecognized += (_, args) =>
            {
                if (_listening && args.Result.Confidence >= 0.68 &&
                    !string.IsNullOrWhiteSpace(args.Result.Text) &&
                    (!_speaking || Capri.Core.VoiceTurnRouter.IsInterruptPhrase(args.Result.Text)))
                    _heard(args.Result.Text);
            };

            _capture = AudioInputDevices.CreateCapture(deviceIndex);
            _capture.DataAvailable += (_, args) =>
            {
                if (_listening) _pcmStream?.Push(args.Buffer, args.BytesRecorded);
            };
            _listening = true;
            _capture.StartRecording();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
            StopListening();
            throw;
        }
    }

    public void StopListening()
    {
        _listening = false;
        var capture = _capture;
        _capture = null;
        try { capture?.StopRecording(); }
        catch (InvalidOperationException) { }
        capture?.Dispose();

        var stream = _pcmStream;
        _pcmStream = null;
        stream?.Complete();

        var recognizer = _recognizer;
        _recognizer = null;
        try { recognizer?.RecognizeAsyncCancel(); }
        catch (InvalidOperationException) { }
        recognizer?.Dispose();
        stream?.Dispose();
    }

    public static string[] AvailableVoices()
    {
        using var synth = new SpeechSynthesizer();
        return synth.GetInstalledVoices().Where(x => x.Enabled)
            .Select(x => x.VoiceInfo.Name).ToArray();
    }

    public void Speak(string text, string? voice = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _speaker ??= CreateSpeaker();
        _speaker.SpeakAsyncCancelAll();
        if (!string.IsNullOrWhiteSpace(voice))
            _speaker.SelectVoice(voice);
        var line = text.Length > 450 ? text[..450] : text;
        _speaking = true;
        _speaker.SpeakAsync(line);
    }

    private SpeechSynthesizer CreateSpeaker()
    {
        var speaker = new SpeechSynthesizer();
        speaker.SpeakCompleted += (_, args) =>
        {
            _speaking = false;
            if (!args.Cancelled && !_disposed) SpeechFinished?.Invoke();
        };
        return speaker;
    }

    public void StopSpeaking()
    {
        _speaking = false;
        _speaker?.SpeakAsyncCancelAll();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopListening();
        _speaker?.Dispose();
    }
}
