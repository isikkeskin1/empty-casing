namespace Capri.Core;

/// <summary>
/// Computes simple music/activity pulses from RMS levels only. No audio frames or
/// speech content are retained. Timestamp progression is supplied by the caller.
/// </summary>
public sealed class AudioPulseDetector
{
    public readonly record struct Reading(bool Active, bool Pulse);

    private double _baseline = 0.02;
    private DateTimeOffset _lastAudible;
    private DateTimeOffset _lastPulse;
    private bool _receivedAudio;

    public Reading Observe(double rms, DateTimeOffset now)
    {
        if (!double.IsFinite(rms)) rms = 0;
        rms = Math.Clamp(rms, 0, 1);

        var baselineBefore = _baseline;
        _baseline = Math.Clamp(_baseline * 0.94 + rms * 0.06, 0.008, 0.6);
        var audible = rms >= 0.016;
        if (audible)
        {
            _receivedAudio = true;
            _lastAudible = now;
        }

        var pulse = audible && rms >= 0.045 &&
                    rms > baselineBefore * 1.55 &&
                    now - _lastPulse >= TimeSpan.FromMilliseconds(280);
        if (pulse) _lastPulse = now;
        return new Reading(IsActive(now), pulse);
    }

    public bool IsActive(DateTimeOffset now) =>
        _receivedAudio && now - _lastAudible < TimeSpan.FromSeconds(2);

    public void Reset()
    {
        _baseline = 0.02;
        _receivedAudio = false;
        _lastAudible = default;
        _lastPulse = default;
    }
}
