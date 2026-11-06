using System.Text.Json.Serialization;

namespace Capri.Core;

public sealed class ChatTurn
{
    public string Role { get; set; } = "assistant";
    public string Text { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    [JsonIgnore]
    public string DisplayRole => Role switch
    {
        "user" => "YOU",
        "system" => "SYSTEM",
        _ => "CAPRI"
    };

    [JsonIgnore]
    public string DisplayTime => Timestamp.ToLocalTime().ToString("HH:mm");
}
