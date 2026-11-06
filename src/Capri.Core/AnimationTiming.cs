using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capri.Core;

/// <summary>Per-frame delays for a user-provided PNG animation sequence.</summary>
public sealed class AnimationTiming
{
    [JsonPropertyName("frameDurationsMs")]
    public int[] FrameDurationsMs { get; set; } = Array.Empty<int>();

    public int DurationAt(int index)
    {
        if (index < 0 || index >= FrameDurationsMs.Length) return 100;
        return Math.Clamp(FrameDurationsMs[index], 20, 2000);
    }

    public static AnimationTiming Load(string filePath)
    {
        try
        {
            return File.Exists(filePath)
                ? JsonSerializer.Deserialize<AnimationTiming>(File.ReadAllText(filePath)) ?? new()
                : new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }
}
