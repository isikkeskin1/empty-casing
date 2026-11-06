namespace Capri.Core;

/// <summary>
/// Layout math kept separate from Windows APIs so it can be smoke tested.
/// All coordinates use WPF device-independent pixels.
/// </summary>
public static class PerchLayout
{
    public const double SeatOffsetY = 256;

    public static double RelativePosition(double screenX, double windowLeft, double windowRight)
    {
        var width = windowRight - windowLeft;
        if (width <= 0 || !double.IsFinite(width)) return 0.5;
        return Math.Clamp((screenX - windowLeft) / width, 0.12, 0.88);
    }

    public static (double Left, double Top) Place(
        double windowLeft, double windowRight, double windowTop,
        double relativeX, double overlayWidth, double minScreenTop)
    {
        var windowWidth = windowRight - windowLeft;
        if (windowWidth <= 0 || overlayWidth <= 0) return (windowLeft, windowTop);

        var left = windowLeft + Math.Clamp(relativeX, 0.12, 0.88) * windowWidth - overlayWidth / 2;
        // For narrow windows, center the companion rather than producing an impossible clamp.
        left = windowWidth < overlayWidth
            ? windowLeft + (windowWidth - overlayWidth) / 2
            : Math.Clamp(left, windowLeft, windowRight - overlayWidth);

        // When the window is maximized, keep Capri visible instead of placing her above the monitor.
        var top = Math.Max(minScreenTop, windowTop - SeatOffsetY);
        return (left, top);
    }

    public static bool IsNearTopEdge(double pointerY, double windowTop, double threshold) =>
        double.IsFinite(pointerY) && double.IsFinite(windowTop) &&
        Math.Abs(pointerY - windowTop) <= threshold;
}
