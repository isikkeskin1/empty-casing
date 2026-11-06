namespace Capri.Core;

/// <summary>
/// Lightweight change detection for consented screen watching. Retains only a tiny grayscale
/// signature in RAM; no screenshot and no on-screen text are persisted by this service.
/// </summary>
public sealed class ScreenChangeGate
{
    private byte[]? _accepted;
    private int _screenId = int.MinValue;

    public void Reset()
    {
        _accepted = null;
        _screenId = int.MinValue;
    }

    /// <summary>Return true only for a visibly changed, nonblank screen.</summary>
    public bool ShouldObserve(byte[] luminance, int screenId)
    {
        if (luminance.Length is < 16 or > 4096)
            throw new ArgumentException("Invalid thumbnail signature length.", nameof(luminance));
        var average = luminance.Average(x => x);
        if (average < 12)
        {
            _accepted = null;
            _screenId = screenId;
            return false;
        }

        if (_accepted is null || _accepted.Length != luminance.Length || _screenId != screenId)
        {
            _accepted = (byte[])luminance.Clone();
            _screenId = screenId;
            return true;
        }

        var changed = 0;
        var combinedDifference = 0;
        for (var i = 0; i < luminance.Length; i++)
        {
            var diff = Math.Abs(luminance[i] - _accepted[i]);
            combinedDifference += diff;
            if (diff > 18) changed++;
        }

        // Small cursor movement, blinking indicators and JPEG noise are not meaningful.
        if (changed < Math.Ceiling(luminance.Length * 0.10) &&
            combinedDifference < luminance.Length * 9)
            return false;

        _accepted = (byte[])luminance.Clone();
        return true;
    }
}
