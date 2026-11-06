using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace Capri.Desktop.Services;

/// <summary>
/// Windows-installed speech recognition and speech synthesis. Microphone is off by default;
/// no raw microphone audio is saved or uploaded by Capri.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private SpeechRecognitionEngine? _recognizer;
    private SpeechSynthesizer? _speaker;
    private readonly Action<string> _heard;
    private bool _listening;
    private bool _disposed;

    public bool IsListening => _listening;

    public SpeechService(Action<string> heard) => _heard = heard;

    public void StartListening()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_listening) return;

        if (_recognizer is null)
        {
            var installed = SpeechRecognitionEngine.InstalledRecognizers();
            if (installed.Count == 0)
                throw new InvalidOperationException(
                    "No Windows speech recognition language pack is installed. Install a speech language in Windows Settings.");

            _recognizer = new SpeechRecognitionEngine(installed[0]);
            _recognizer.LoadGrammar(new DictationGrammar());
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += (_, args) =>
            {
                if (_listening && args.Result.Confidence >= 0.65 &&
                    !string.IsNullOrWhiteSpace(args.Result.Text))
                    _heard(args.Result.Text);
            };
        }

        _listening = true;
        try { _recognizer.RecognizeAsync(RecognizeMode.Multiple); }
        catch
        {
            _listening = false;
            throw;
        }
    }

    public void StopListening()
    {
        if (!_listening) return;
        _listening = false;
        _recognizer?.RecognizeAsyncCancel();
    }

    public void Speak(string text)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _speaker ??= new SpeechSynthesizer();
        _speaker.SpeakAsyncCancelAll();
        var line = text.Length > 450 ? text[..450] : text;
        _speaker.SpeakAsync(line);
    }

    public void StopSpeaking() => _speaker?.SpeakAsyncCancelAll();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopListening();
        _recognizer?.Dispose();
        _speaker?.Dispose();
    }
}
