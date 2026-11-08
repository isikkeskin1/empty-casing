using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using NAudio.Wave;
using Capri.Core;

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
    private readonly Action<string, long> _heard;
    private readonly AudioCaptureSessionGate _sessions = new();
    private long _failedSession;
    private volatile bool _listening;
    private volatile bool _speaking;
    private bool _disposed;

    public event Action? SpeechFinished;
    /// <summary>
    /// Raised for unexpected device/recognizer errors, not normal Stop/Cancel.
    /// Listeners must marshal this callback to the WPF dispatcher.
    /// </summary>
    public event Action<string, long>? CaptureFailed;
    public bool IsListening => _listening;
    public bool IsSpeaking => _speaking;

    public SpeechService(Action<string, long> heard) => _heard = heard;

    public bool IsSessionCurrent(long session) => _sessions.IsCurrent(session);

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

        var session = _sessions.Begin();
        Interlocked.Exchange(ref _failedSession, 0);
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
                if (!_listening || !_sessions.IsCurrent(session) ||
                    args.Result.Confidence < 0.68 ||
                    string.IsNullOrWhiteSpace(args.Result.Text) ||
                    (_speaking && !Capri.Core.VoiceTurnRouter.IsInterruptPhrase(args.Result.Text)))
                    return;
                // System.Speech invokes callbacks on an audio worker thread.
                // A closed WPF Dispatcher or cancelled session must never
                // terminate the entire desktop app from this callback.
                try
                {
                    if (_sessions.IsCurrent(session))
                        _heard(args.Result.Text, session);
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    RaiseCaptureFailed("Voice UI is unavailable. Open Capri again.", session);
                }
            };
            _recognizer.RecognizeCompleted += (_, args) =>
            {
                if (!_listening || !_sessions.IsCurrent(session)) return;
                RaiseCaptureFailed(args.Error is not null
                    ? "Windows speech recognition stopped unexpectedly. Check INPUTS."
                    : "The speech recognizer ended the session. Try Speak Now again.", session);
            };

            if (deviceIndex != -1)
            {
                _capture = AudioInputDevices.CreateCapture(deviceIndex);
                _capture.DataAvailable += (_, args) =>
                {
                    try
                    {
                        if (_listening && _sessions.IsCurrent(session))
                            _pcmStream?.Push(args.Buffer, args.BytesRecorded);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or
                        ObjectDisposedException)
                    {
                        // Late audio callback during StopListening: safe to drop.
                    }
                };
                _capture.RecordingStopped += (_, args) =>
                {
                    if (_listening && _sessions.IsCurrent(session))
                        RaiseCaptureFailed(args.Exception is null
                            ? "Windows ended this microphone session unexpectedly."
                            : "The microphone disconnected or Windows stopped capture.",
                            session);
                };
            }

            _listening = true;
            _capture?.StartRecording();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch
        {
            // Never let a second native teardown exception obscure the
            // original microphone/recognizer setup failure.
            try { StopListening(); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { }
            throw;
        }
    }

    private void RaiseCaptureFailed(string message, long session)
    {
        if (!_listening || _disposed || !_sessions.IsCurrent(session))
            return;
        // A stopped recognizer and a disconnected WaveIn can report the SAME
        // failure from different worker threads. Deliver it once per session.
        if (Interlocked.CompareExchange(ref _failedSession, session, 0) != 0)
            return;
        _listening = false;
        try { CaptureFailed?.Invoke(message, session); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    public void StopListening()
    {
        // Invalidate queued events BEFORE StopRecording/RecognizeAsyncCancel:
        // both may synchronously dispatch completion from the old session.
        _sessions.End();
        _listening = false;
        var capture = _capture;
        _capture = null;
        try { capture?.StopRecording(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { capture?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }

        var stream = _pcmStream;
        _pcmStream = null;
        stream?.Complete();

        var recognizer = _recognizer;
        _recognizer = null;
        try { recognizer?.RecognizeAsyncCancel(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { recognizer?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
        try { stream?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
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
            if (!_disposed)
            {
                try { SpeechFinished?.Invoke(); }
                catch (Exception ex) when (ex is not OutOfMemoryException) { }
            }
        };
        return speaker;
    }

    public void StopSpeaking()
    {
        _speaking = false;
        try { _speaker?.SpeakAsyncCancelAll(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopListening();
        try { _speaker?.Dispose(); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { }
    }
}
