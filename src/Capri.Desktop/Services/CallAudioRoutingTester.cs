using System.IO;
using NAudio.Wave;

namespace Capri.Desktop.Services;

/// <summary>
/// Makes a brief, low-volume, local-only two-tone test sound in the selected
/// virtual cable. Users can check the Discord microphone level meter without
/// purchasing a Fish API call or turning on a real microphone.
/// </summary>
public static class CallAudioRoutingTester
{
    public static async Task TestVirtualOutputAsync(
        int speakerIndex, CancellationToken cancellationToken = default)
    {
        if (speakerIndex < 0 || !AudioInputDevices.SpeakerExists(speakerIndex))
            throw new InvalidOperationException(
                "Choose an available non-default virtual cable output first.");

        const int rate = 16000;
        const int totalSamples = rate; // one second total
        var bytes = new byte[totalSamples * 2];
        for (var i = 0; i < totalSamples; ++i)
        {
            var t = i / (double)rate;
            // Two short chirps, with a smooth envelope. Conservative peak
            // amplitude avoids blasting someone's speakers or loopback.
            var active = t is > 0.08 and < 0.37 or > 0.52 and < 0.85;
            var envelope = active
                ? Math.Min(1, Math.Min((t % 0.44) / 0.025, (1 - t) / 0.025))
                : 0.0;
            var frequency = t < 0.45 ? 540.0 : 780.0;
            var sample = (short)(Math.Sin(2 * Math.PI * frequency * t) *
                                 0.13 * Math.Clamp(envelope, 0, 1) * short.MaxValue);
            bytes[i * 2] = (byte)sample;
            bytes[i * 2 + 1] = (byte)(sample >> 8);
        }

        using var stream = new MemoryStream(bytes, writable: false);
        using var audio = new RawSourceWaveStream(stream, new WaveFormat(rate, 16, 1));
        using var output = new WaveOutEvent
        {
            DeviceNumber = speakerIndex,
            DesiredLatency = 120
        };
        output.Init(audio);
        output.Play();
        try { await Task.Delay(TimeSpan.FromMilliseconds(1200), cancellationToken); }
        finally
        {
            output.Stop();
        }
    }
}
