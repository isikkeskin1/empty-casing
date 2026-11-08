using System.Runtime.InteropServices;
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
    /// <summary>
    /// Raised for unexpected device/recognizer errors, not normal Stop/Cancel.
    /// Listeners must marshal this callback to the WPF dispatcher.
    /// </summary>
    public event Action<string>? CaptureFailed;
    public bool IsListening => _listening;
    public bool IsSpeaking => _speaking;

    public SpeechService(Action<string> heard) => _heard = heard;

    public void StartListening(int deviceIndex = -1, string? recognitionLanguage = null)
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
        var selected = string.IsNullOrWhiteSpace(recognitionLanguage)
            ? installed[0]
            : installed.FirstOrDefault(r => r.Culture.Name.Equals(
                recognitionLanguage, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
            throw new InvalidOperationException(
                "The selected Windows speech recognition language is unavailable. Select an installed language in INPUTS.");

        try
        {
            _recognizer = new SpeechRecognitionEngine(selected);
            _recognizer.LoadGrammar(new DictationGrammar());

            // SAPI can capture the Windows default microphone natively.
            // Only non-default devices need the NAudio -> PCM stream bridge.
            // This also avoids the custom-stream COM interop on the common path.
            if (deviceIndex == -1)
            {
                _recognizer.SetInputToDefaultAudioDevice();
            }
            else
            {
                _pcmStream = new BufferedMicrophoneStream();
                _recognizer.SetInputToAudioStream(_pcmStream,
                    new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
            }
            _recognizer.SpeechRecognized += (_, args) =>
            {
                if (!_listening || args.Result.Confidence < 0.68 ||
                    string.IsNullOrWhiteSpace(args.Result.Text) ||
                    (_speaking && !Capri.Core.VoiceTurnRouter.IsInterruptPhrase(args.Result.Text)))
                    return;
                // System.Speech invokes callbacks on an audio worker thread.
                // A closed WPF Dispatcher or cancelled session must never
                // terminate the entire desktop app from this callback.
                try { _heard(args.Result.Text); }
                catch (Exception ex) when (ex is InvalidOperationException or
                    ObjectDisposedException or System.Threading.Tasks.TaskCanceledException)
                {
                    RaiseCaptureFailed("Voice UI is unavailable. Open Capri again.");
                }
            };
            _recognizer.RecognizeCompleted += (_, args) =>
            {
                if (!_listening) return; // Normal cancellation during STOP.
                RaiseCaptureFailed(args.Error is not null
                    ? "Windows speech recognition stopped unexpectedly. Check INPUTS."
                    : "The speech recognizer ended the session. Try Speak Now again.");
            };

            if (deviceIndex != -1)
            {
                _capture = AudioInputDevices.CreateCapture(deviceIndex);
                _capture.DataAvailable += (_, args) =>
                {
                    try
                    {
                        if (_listening) _pcmStream?.Push(args.Buffer, args.BytesRecorded);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or
                        ObjectDisposedException)
                    {
                        // Late audio callback during StopListening: safe to drop.
                    }
                };
                _capture.RecordingStopped += (_, args) =>
                {
                    if (_listening && args.Exception is not null)
                        RaiseCaptureFailed("The microphone disconnected or Windows stopped capture.");
                };
            }

            _listening = true;
            _capture?.StartRecording();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
            StopListening();
            throw;
        }
    }

    private void RaiseCaptureFailed(string message)
    {
        if (!_listening || _disposed) return;
        _listening = false;
        try { CaptureFailed?.Invoke(message); }
        catch (Exception ex) when (ex is InvalidOperationException or
            ObjectDisposedException or TaskCanceledException) { }
    }

    public void StopListening()
    {
        _listening = false;
        var capture = _capture;
        _capture = null;
        try { capture?.StopRecording(); }
        catch (Exception ex) when (ex is InvalidOperationException or
            ObjectDisposedException or MmException) { }
        try { capture?.Dispose(); }
        catch (ObjectDisposedException) { }

        var stream = _pcmStream;
        _pcmStream = null;
        stream?.Complete();

        var recognizer = _recognizer;
        _recognizer = null;
        try { recognizer?.RecognizeAsyncCancel(); }
        catch (Exception ex) when (ex is InvalidOperationException or
            ObjectDisposedException or COMException) { }
        try { recognizer?.Dispose(); }
        catch (Exception ex) when (ex is ObjectDisposedException or COMException) { }
        try { stream?.Dispose(); }
        catch (ObjectDisposedException) { }
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
            // A cancelled old utterance may finish after a newer one begins.
            // Never mark the new playback as idle from that cancellation.
            if (args.Cancelled) return;
            _speaking = false;
            if (!_disposed) SpeechFinished?.Invoke();
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
