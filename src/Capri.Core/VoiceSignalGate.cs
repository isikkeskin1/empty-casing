namespace Capri.Core;

/// <summary>
/// Low-cost local PCM16 activity check before running multilingual Whisper.
/// It does NOT classify speech or save samples; it only rejects unambiguously
/// silent recordings that can otherwise cause Whisper to hallucinate text.
/// </summary>
public sealed class VoiceSignalGate
{
    private long _totalSamples;
    private long _audibleSamples;

    // ~-52 dBFS: sensitive enough for a quiet microphone without routinely
    // classifying quantization noise as an audible user turn.
    private const double AudibleRms = 0.0025;
    private const int MinimumAudibleSamples = 3200; // 200ms at 16 kHz

    public void ObservePcm16(byte[]? buffer, int length)
    {
        if (buffer is null || length <= 1 || length > buffer.Length ||
            length > 128 * 1024) return;
        var count = length & ~1;
        if (count < 2) return;

        double sumSquares = 0;
        for (var i = 0; i < count; i += 2)
        {
            var amplitude = (short)(buffer[i] | buffer[i + 1] << 8);
            var normalized = amplitude / 32768.0;
            sumSquares += normalized * normalized;
        }
        var samples = count / 2;
        _totalSamples += samples;
        if (Math.Sqrt(sumSquares / samples) >= AudibleRms)
            _audibleSamples += samples;
    }

    public bool HasAudibleInput =>
        _totalSamples >= MinimumAudibleSamples &&
        _audibleSamples >= MinimumAudibleSamples;
}
