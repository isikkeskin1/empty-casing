namespace Capri.Core;

public sealed class AppSettings
{
    public string OllamaModel { get; set; } = "qwen3:4b";
    public string VisionModel { get; set; } = "qwen3-vl:4b";
    public string BrainProvider { get; set; } = "local";
    // Public OAuth application identifiers only. Never put access/refresh tokens here.
    public string SpotifyClientId { get; set; } = "";
    public string RobloxClientId { get; set; } = "";
    // OBS is loopback-only; no hostname or password is saved in settings.json.
    public int ObsWebSocketPort { get; set; } = 4455;
    public string CloudTextModel { get; set; } = "meta/llama-3.1-70b-instruct";
    public string CloudVisionModel { get; set; } = "";
    // Automatically fetch validated stable updates in the background; for
    // installed copies, apply the staged update when Capri exits.
    public bool AutomaticUpdates { get; set; } = true;
    public bool SetupCompleted { get; set; }
    public int ScreenIndex { get; set; } = -1;
    public int WatchIntervalSeconds { get; set; } = 45;
    public bool EnablePetting { get; set; } = true;
    public string SpeechVoice { get; set; } = "";
    public string VoiceProvider { get; set; } = "windows";
    public int MicrophoneDeviceIndex { get; set; } = -1;
    public int SpeakerDeviceIndex { get; set; } = -1;
    public InputShortcuts Shortcuts { get; set; } = new();
    public string FishModel { get; set; } = "s2.1-pro-free";
    public string FishReferenceId { get; set; } = "";
    public bool WatchScreen { get; set; }
    // Explicit opt-in to include minimal foreground app process/media hints in
    // the next regular model reply (including cloud models). No background scan.
    public bool EnableSituationalContext { get; set; }
    // Private notes are never injected into cloud/local brain prompts until opt-in.
    public bool UsePersonalMemoryInReplies { get; set; }
    // Only the provider explicitly consented to may receive saved notes.
    public string MemoryCloudProviderConsent { get; set; } = "";
    // Show explicit game help as a no-focus HUD for ~30 seconds, never permanent watching.
    public bool ShowGameCoachHud { get; set; } = true;
    public bool ListenToMicrophone { get; set; }
    public bool SpeakReplies { get; set; }
    // Windows TTS can begin complete sentences during streamed model output.
    // Fish Audio continues its whole-MP3 path until verified streaming support.
    public bool SpeakWhileGenerating { get; set; } = true;
    public bool EnableDesktopActions { get; set; }
    // Saved preference, NOT a startup permission grant. Desktop actions are re-armed
    // explicitly each session; STOP INPUTS revokes all active execution permission.
    public bool SkipActionConfirmations { get; set; }
    public bool DanceToSystemAudio { get; set; }
    public bool EnableVoiceFollowUps { get; set; }
    public bool AllowVoiceInterrupt { get; set; }
    // Default off: gaming voice requests should not pull a full chat window
    // into the foreground. If speech is disabled, chat still opens for replies.
    public bool OpenChatForVoiceRequests { get; set; }
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
}
