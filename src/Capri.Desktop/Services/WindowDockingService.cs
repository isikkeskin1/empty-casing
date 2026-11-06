using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Capri.Desktop.Services;

/// <summary>
/// Reads visible top-level Windows window bounds. Never controls or injects input into other apps.
/// All returned coordinates are Win32 screen pixels.
/// </summary>
internal sealed class WindowDockingService
{
    internal readonly record struct Bounds(int Left, int Top, int Right, int Bottom)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    internal readonly record struct Target(IntPtr Handle, Bounds Rect);

    private const int ExtendedFrameBounds = 9;
    private const int Cloaked = 14;
    private readonly uint _ownProcess = (uint)Environment.ProcessId;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    private delegate bool WindowEnumerator(IntPtr hwnd, IntPtr state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(WindowEnumerator callback, IntPtr state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowTextLengthW(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, int attribute, out NativeRect rect, int size);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetWindowAttribute(
        IntPtr hwnd, int attribute, out int value, int size);

    public Target? FrontmostWindow()
    {
        Target? result = null;
        EnumWindows((handle, _) =>
        {
            if (!TryRead(handle, out var candidate)) return true;
            result = candidate;
            return false;
        }, IntPtr.Zero);
        return result;
    }

    public Target? WindowNearEdge(System.Drawing.Point pointer, int tolerance = 90)
    {
        Target? result = null;
        var bestDistance = double.MaxValue;
        EnumWindows((handle, _) =>
        {
            if (!TryRead(handle, out var candidate)) return true;
            var bounds = candidate.Rect;
            if (pointer.X < bounds.Left || pointer.X > bounds.Right) return true;
            var distance = Math.Abs(pointer.Y - bounds.Top);
            if (distance > tolerance || distance >= bestDistance) return true;
            bestDistance = distance;
            result = candidate;
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public bool TryGetBounds(IntPtr hwnd, out Bounds bounds)
    {
        bounds = default;
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || !IsWindowVisible(hwnd) || IsIconic(hwnd))
            return false;

        if (DwmGetWindowAttribute(hwnd, Cloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
            return false;

        NativeRect rect;
        if (DwmGetWindowAttribute(hwnd, ExtendedFrameBounds, out rect, Marshal.SizeOf<NativeRect>()) != 0 &&
            !GetWindowRect(hwnd, out rect))
            return false;

        bounds = new Bounds(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width >= 210 && bounds.Height >= 140;
    }

    private bool TryRead(IntPtr hwnd, out Target target)
    {
        target = default;
        if (GetWindowThreadProcessId(hwnd, out var pid) == 0 || pid == _ownProcess)
            return false;
        // Exclude empty-title system/tool windows.
        if (GetWindowTextLengthW(hwnd) <= 0 || !TryGetBounds(hwnd, out var bounds))
            return false;
        target = new Target(hwnd, bounds);
        return true;
    }
}
