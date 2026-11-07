using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Capri.Desktop.Services;

/// <summary>
/// Checks only time since last local input and foreground process identity.
/// No keystrokes, window titles, screen pixels or filenames are collected.
/// Unknown foreground processes are treated as busy (e.g. games and IDEs).
/// </summary>
internal static class AutoUpdateIdleProbe
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Tick;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(
        IntPtr window, out uint processId);

    public static TimeSpan TimeSinceInput()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };
        if (!GetLastInputInfo(ref info)) return TimeSpan.Zero;
        var ticks = unchecked((uint)Environment.TickCount - info.Tick);
        return TimeSpan.FromMilliseconds(ticks);
    }

    public static bool SafeForeground()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;
        GetWindowThreadProcessId(foreground, out var processId);
        if (processId == Environment.ProcessId) return true;
        if (processId == 0) return false;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            // Windows shell/desktop is okay after the user has been inactive.
            // Never restart while a game, browser, IDE or unknown app is active.
            return process.ProcessName.Equals("explorer",
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or
            InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
