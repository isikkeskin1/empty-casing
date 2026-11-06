namespace Capri.Core;

/// <summary>
/// Recognizes a deliberate back-and-forth mouse stroke over Capri, without global mouse hooks.
/// All positions are local to her overlay and timestamps are monotonic milliseconds.
/// </summary>
public sealed class PettingGesture
{
    private double _lastX;
    private double _lastY;
    private long _previousMs;
    private long _startMs;
    private long _lastTriggeredMs = long.MinValue / 2;
    private int _direction;
    private int _reversals;
    private double _distance;

    public void Reset()
    {
        _previousMs = 0;
        _direction = 0;
        _reversals = 0;
        _distance = 0;
    }

    public bool Observe(double x, double y, long timestampMs)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) return false;
        if (_previousMs == 0 || timestampMs <= _previousMs ||
            timestampMs - _previousMs > 350 || timestampMs - _startMs > 1900)
        {
            _lastX = x;
            _lastY = y;
            _previousMs = timestampMs;
            _startMs = timestampMs;
            _direction = 0;
            _reversals = 0;
            _distance = 0;
            return false;
        }

        var deltaX = x - _lastX;
        var deltaY = y - _lastY;
        _lastX = x;
        _lastY = y;
        _previousMs = timestampMs;
        // A stroke is primarily vertical or horizontal, not random cursor jitter.
        var axisMovement = Math.Abs(deltaX) >= Math.Abs(deltaY) * 1.5
            ? deltaX : Math.Abs(deltaY) >= Math.Abs(deltaX) * 1.5 ? deltaY : 0;
        if (Math.Abs(axisMovement) < 13) return false;

        var nextDirection = Math.Sign(axisMovement);
        if (_direction != 0 && nextDirection != _direction) _reversals++;
        _direction = nextDirection;
        _distance += Math.Abs(axisMovement);
        if (_reversals < 3 || _distance < 130 ||
            timestampMs - _lastTriggeredMs < 10000) return false;

        _lastTriggeredMs = timestampMs;
        Reset();
        return true;
    }
}
