using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Lists real Windows capture/playback devices using the WinMM endpoints supported by NAudio.
/// Device indices are local machine configuration, never sent to the model or a cloud endpoint.
/// </summary>
public static class AudioInputDevices
{
    public sealed record Device(int Index, string Name);

    public static IReadOnlyList<Device> Microphones()
    {
        var devices = new List<Device> { new(-1, "Windows default microphone") };
        for (var i = 0; i < WaveIn.DeviceCount; i++)
            devices.Add(new Device(i, WaveIn.GetCapabilities(i).ProductName));
        return devices;
    }

    public static IReadOnlyList<Device> Speakers()
    {
        var devices = new List<Device> { new(-1, "Windows default speakers") };
        for (var i = 0; i < WaveOut.DeviceCount; i++)
            devices.Add(new Device(i, WaveOut.GetCapabilities(i).ProductName));
        return devices;
    }

    public static bool MicrophoneExists(int index) =>
        index == -1 || index >= 0 && index < WaveIn.DeviceCount;

    public static bool SpeakerExists(int index) =>
        index == -1 || index >= 0 && index < WaveOut.DeviceCount;

    public static double PeakLevel(byte[] bytes, int count)
    {
        if (bytes.Length == 0 || count < 2) return 0;
        var peak = 0;
        var size = Math.Min(count, bytes.Length) & ~1;
        for (var n = 0; n < size; n += 2)
        {
            var sample = (short)(bytes[n] | bytes[n + 1] << 8);
            var absolute = sample == short.MinValue ? 32768 : Math.Abs(sample);
            if (absolute > peak) peak = absolute;
        }
        return Math.Clamp(peak / 32768.0, 0.0, 1.0);
    }

    public static async Task<double> TestMicrophoneAsync(
        int index, TimeSpan duration, Action<double> report, CancellationToken cancellationToken = default)
    {
        if (!MicrophoneExists(index))
            throw new ArgumentOutOfRangeException(nameof(index), "Microphone is no longer available.");

        var peak = 0.0;
        using var mic = CreateCapture(index);
        mic.DataAvailable += (_, e) =>
        {
            var level = PeakLevel(e.Buffer, e.BytesRecorded);
            peak = Math.Max(peak, level);
            report(level);
        };
        mic.StartRecording();
        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            mic.StopRecording();
        }
        return peak;
    }

    public static WaveInEvent CreateCapture(int index)
    {
        if (!MicrophoneExists(index))
            throw new ArgumentOutOfRangeException(nameof(index), "Microphone no longer available.");
        return new WaveInEvent
        {
            DeviceNumber = index,
            WaveFormat = new WaveFormat(16000, 16, 1),
            BufferMilliseconds = 100,
            NumberOfBuffers = 4
        };
    }
}
