namespace Capri.Core;

public sealed class AppSettings
{
    public string OllamaModel { get; set; } = "qwen3:4b";
    public string VisionModel { get; set; } = "qwen3-vl:4b";
    public bool WatchScreen { get; set; }
    public bool ListenToMicrophone { get; set; }
    public bool SpeakReplies { get; set; }
    public bool EnableDesktopActions { get; set; }
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
}
