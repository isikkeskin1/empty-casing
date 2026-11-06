namespace Capri.Core;

public sealed class AppSettings
{
    public string OllamaModel { get; set; } = "qwen3:4b";
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
}
