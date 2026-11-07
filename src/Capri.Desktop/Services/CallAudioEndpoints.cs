using NAudio.CoreAudioApi;

namespace Capri.Desktop.Services;

/// <summary>
/// Enumerates active WASAPI speaker endpoints for an explicitly armed call
/// listener. Device IDs remain on this computer and are never sent to an AI.
/// </summary>
public static class CallAudioEndpoints
{
    public sealed record Output(string Id, string Name);

    public static IReadOnlyList<Output> Speakers()
    {
        using var enumerator = new MMDeviceEnumerator();
        var found = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        var items = new List<Output> { new("", "Windows default speaker output") };
        foreach (var endpoint in found)
            items.Add(new Output(endpoint.ID, endpoint.FriendlyName));
        return items;
    }

    public static bool IsAvailable(string? id) =>
        string.IsNullOrEmpty(id) ||
        Speakers().Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static string Description(string? id) =>
        Speakers().FirstOrDefault(x => x.Id.Equals(id ?? "", StringComparison.OrdinalIgnoreCase))
            ?.Name ?? "(speaker device unavailable)";
}
