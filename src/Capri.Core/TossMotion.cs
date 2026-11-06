namespace Capri.Core;

/// <summary>Bounded, deterministic desktop tossing physics in device-independent pixels.</summary>
public static class TossMotion
{
    public readonly record struct State(double Left, double Top, double Vx, double Vy, bool Stopped);

    public static State Step(State state, double deltaSeconds,
        double minLeft, double minTop, double maxLeft, double maxTop)
    {
        if (state.Stopped) return state;
        var dt = Math.Clamp(deltaSeconds, 0.001, 0.05);
        var vx = state.Vx * Math.Exp(-1.8 * dt);
        var vy = state.Vy + 1250 * dt;
        var x = state.Left + vx * dt;
        var y = state.Top + vy * dt;

        if (maxLeft < minLeft) maxLeft = minLeft;
        if (maxTop < minTop) maxTop = minTop;

        if (x < minLeft)
        {
            x = minLeft;
            vx = Math.Abs(vx) * 0.42;
        }
        if (x > maxLeft)
        {
            x = maxLeft;
            vx = -Math.Abs(vx) * 0.42;
        }
        if (y < minTop)
        {
            y = minTop;
            vy = Math.Abs(vy) * 0.45;
        }
        if (y > maxTop)
        {
            y = maxTop;
            vy = -Math.Abs(vy) * 0.32;
            if (Math.Abs(vy) < 135) vy = 0;
        }

        var stopped = Math.Abs(vx) < 15 && Math.Abs(vy) < 15 && y >= maxTop - 0.1;
        return new State(x, y, stopped ? 0 : vx, stopped ? 0 : vy, stopped);
    }
}
