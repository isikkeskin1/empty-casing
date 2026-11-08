namespace Capri.Core;

/// <summary>
/// The compact voice interaction state shown consistently across Chat and the
/// floating Capri overlay. Voice capture is always opt-in and cancellable.
/// </summary>
public enum VoiceCapturePhase
{
    Ready,
    Starting,
    Listening,
    Transcribing,
    Error
}

public static class VoiceCaptureUiState
{
    public static bool IsActive(VoiceCapturePhase phase) =>
        phase is VoiceCapturePhase.Starting or VoiceCapturePhase.Listening or
            VoiceCapturePhase.Transcribing;

    public static string ButtonLabel(VoiceCapturePhase phase) =>
        IsActive(phase) ? "■  STOP LISTENING" : "◉  SPEAK NOW";

    public static string StatusLabel(VoiceCapturePhase phase) => phase switch
    {
        VoiceCapturePhase.Starting => "CONNECTING",
        VoiceCapturePhase.Listening => "LISTENING",
        VoiceCapturePhase.Transcribing => "TRANSCRIBING",
        VoiceCapturePhase.Error => "MIC NEEDS ATTENTION",
        _ => "MIC STANDBY"
    };

    public static string ShortHelp(VoiceCapturePhase phase, bool whisper) => phase switch
    {
        VoiceCapturePhase.Starting =>
            "Opening your selected microphone. Nothing has been sent yet.",
        VoiceCapturePhase.Listening when whisper =>
            "Recording locally for six seconds. Click Stop to cancel.",
        VoiceCapturePhase.Listening =>
            "Speak one request; no wake word needed. Click Stop to cancel.",
        VoiceCapturePhase.Transcribing =>
            "Converting your voice to text on this PC. Click Stop to cancel.",
        VoiceCapturePhase.Error =>
            "Microphone unavailable. Check device, language and Windows permissions.",
        _ when whisper =>
            "Whisper · local multilingual • 6-second manual recording.",
        _ =>
            "Windows speech • one request, up to 20 seconds."
    };
}
