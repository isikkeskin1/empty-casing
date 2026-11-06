namespace Capri.Core;

public sealed class AppSettings
{
    public string OllamaModel { get; set; } = "qwen3:4b";
    public string VisionModel { get; set; } = "qwen3-vl:4b";
    public string BrainProvider { get; set; } = "local";
    public string CloudTextModel { get; set; } = "meta/llama-3.1-70b-instruct";
    public string CloudVisionModel { get; set; } = "";
    public bool SetupCompleted { get; set; }
    public string SpeechVoice { get; set; } = "";
    public bool WatchScreen { get; set; }
    public bool ListenToMicrophone { get; set; }
    public bool SpeakReplies { get; set; }
    public bool EnableDesktopActions { get; set; }
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
}
