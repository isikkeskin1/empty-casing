using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Capri.Core;
using Capri.Desktop.Windows;

namespace Capri.Desktop.Services;

/// <summary>
/// User-requested visual clones with global hold-Esc cleanup. They do NOT
/// steal input, move external windows, modify games or generate model actions.
/// Timer and windows exist only while owner explicitly arms Chaos Mode.
/// </summary>
public sealed class ChaosController : IDisposable
{
    private readonly List<ChaosCloneWindow> _clones = new();
    private readonly Action _emergencyStop;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(85) };
    private readonly DispatcherTimer _chatter = new() { Interval = TimeSpan.FromSeconds(5) };
    private long _escDownAt;
    private bool _disposed;
    private int _turn;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    private static bool IsDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    public int CloneCount => _clones.Count;

    public ChaosController(Action emergencyStop)
    {
        _emergencyStop = emergencyStop;
        _tick.Tick += (_, _) => Poll();
        _chatter.Tick += (_, _) => Converse();
    }

    // Conservative gating: do not cover a game or a broadcasting session.
    // Owner can end clones from any foreground app by holding Esc.
    public static string? GuardReason()
    {
        foreach (var name in new[] { "obs64", "Streamlabs Desktop", "Streamlabs OBS" })
        {
            if (Process.GetProcessesByName(name).Any())
                return "Chaos Mode is disabled while recording/streaming software is running.";
        }

        var hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero && GetWindowThreadProcessId(hwnd, out var pid) != 0)
        {
            try
            {
                using var p = Process.GetProcessById((int)pid);
                var name = p.ProcessName.ToLowerInvariant();
                if (new[] { "robloxplayerbeta", "valorant", "cs2", "fortniteclient",
                             "league of legends", "eldenring", "overwatch", "r5apex" }
                    .Any(game => name.Contains(game, StringComparison.Ordinal)))
                    return "Chaos Mode stays off while a game is in the foreground.";
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or
                System.ComponentModel.Win32Exception) { }
        }
        return null;
    }

    public string Start(int count, string artFolder)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count < 1 || count > ChaosModePolicy.MaxClones)
            return "Choose between one and five clones.";
        var reason = GuardReason();
        if (reason is not null) return reason;
        Stop();

        var pointer = Cursor.Position;
        var x = Math.Clamp((double)pointer.X - 100,
            SystemParameters.VirtualScreenLeft,
            Math.Max(SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 165));
        var y = Math.Clamp((double)pointer.Y - 240,
            SystemParameters.VirtualScreenTop,
            Math.Max(SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 254));
        try
        {
            for (var i = 0; i < count; i++)
            {
                var clone = new ChaosCloneWindow(i, artFolder)
                {
                    Left = Math.Clamp(x + i * 95, SystemParameters.VirtualScreenLeft,
                        Math.Max(SystemParameters.VirtualScreenLeft,
                            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 165)),
                    Top = Math.Clamp(y + (i % 2 == 0 ? 0 : 80), SystemParameters.VirtualScreenTop,
                        Math.Max(SystemParameters.VirtualScreenTop,
                            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 254))
                };
                _clones.Add(clone);
                clone.Show();
            }
            _escDownAt = 0;
            _tick.Start();
            _chatter.Start();
            Converse();
            return "Chaos Mode: " + count + " visual clone(s). Hold Esc for one second or say stop clones. Clones cannot control other windows.";
        }
        catch
        {
            Stop();
            throw;
        }
    }

    private void Converse()
    {
        if (_clones.Count == 0) return;
        var lines = new[]
        {
            "We're multiplying.", "Who's the real Capri?", "Choom. CHOOM.",
            "This desktop's ours.", "Psst. Another me.", "Glitchy day, huh?"
        };
        _turn++;
        for (var i = 0; i < _clones.Count; i++)
        {
            var clone = _clones[i];
            if (i == _turn % _clones.Count)
                clone.SpeakBubble(lines[(_turn + i) % lines.Length]);
            else clone.SetIdle();
        }
    }

    private void Poll()
    {
        // Freeze and clean up immediately when a game/streaming app becomes
        // active, rather than compete for the owner's screen attention.
        if (GuardReason() is not null)
        {
            Stop();
            return;
        }
        var now = Environment.TickCount64;
        if (IsDown(0x1B))
        {
            if (_escDownAt == 0) _escDownAt = now;
            else if (ChaosModePolicy.HeldEscapeLongEnough(true, _escDownAt, now))
            {
                Stop();
                _emergencyStop();
            }
        }
        else _escDownAt = 0;
    }

    public void Stop()
    {
        _tick.Stop();
        _chatter.Stop();
        _escDownAt = 0;
        foreach (var clone in _clones) clone.Close();
        _clones.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }
}
