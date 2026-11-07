using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
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
    private Func<string, CancellationToken, Task<string>>? _localDialogue;
    private CancellationTokenSource? _dialogueCts;
    private SpeechSynthesizer? _cloneSpeaker;
    private string[] _cloneVoiceNames = Array.Empty<string>();
    private Func<bool>? _muteCloneSpeech;
    private bool _dialogueBusy;
    private int _dialogueTurns;
    private string _lastDialogue = "We're all here. Who's the original Capri?";
    private const int MaxDialogueTurns = 10;

    // Explicitly authorized single-window reversible prank. No resize,
    // mouse injection, shell/taskbar or arbitrary process manipulation.
    private sealed class WindowPrank
    {
        public required IntPtr Handle { get; init; }
        public required uint ProcessId { get; init; }
        public required int OriginalX { get; init; }
        public required int OriginalY { get; init; }
        public required int LastX { get; set; }
        public required int LastY { get; set; }
        public required long BeganAt { get; init; }
    }
    private WindowPrank? _prank;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter,
        int x, int y, int cx, int cy, uint flags);

    private const uint SwpNoSizeNoZOrderNoActivate = 0x0001 | 0x0004 | 0x0010;
    private static readonly HashSet<string> AllowedPrankApps =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "notepad", "code", "chrome", "msedge", "firefox",
            "spotify", "discord", "discordcanary", "paintdotnet", "mspaint"
        };

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

    /// <summary>
    /// Explicit AI mode uses exactly one local-model inference at a time,
    /// limited to ten short turns. It never reads owner chat, secrets or
    /// the desktop, and no clone can initiate actions.
    /// </summary>
    public string StartAi(int count, string artFolder,
        Func<string, CancellationToken, Task<string>> localModel,
        Func<bool>? shouldMute = null)
    {
        ArgumentNullException.ThrowIfNull(localModel);
        var result = Start(count, artFolder);
        if (_clones.Count != count) return result;
        _localDialogue = localModel;
        _muteCloneSpeech = shouldMute;
        // Local Windows TTS: no Fish API costs and no cloud audio. Use
        // installed voices for distinct personalities when available.
        try
        {
            _cloneSpeaker = new SpeechSynthesizer { Volume = 70, Rate = 1 };
            _cloneVoiceNames = _cloneSpeaker.GetInstalledVoices()
                .Where(v => v.Enabled).Select(v => v.VoiceInfo.Name).ToArray();
        }
        catch (Exception ex) when (ex is InvalidOperationException or
            PlatformNotSupportedException)
        {
            _cloneSpeaker?.Dispose();
            _cloneSpeaker = null; // On-screen captions still work.
        }
        _dialogueCts = new CancellationTokenSource();
        _dialogueTurns = 0;
        _lastDialogue = "We're all here. Who is the real Capri?";
        _chatter.Interval = TimeSpan.FromSeconds(10);
        _ = ConverseWithLocalModelAsync();
        return "AI Chaos: " + count +
            " clones now chat using your LOCAL Ollama model, with distinct " +
            "Windows voices (when installed), up to 10 turns. " +
            "No private chat, cloud API or desktop access. Hold Esc to stop.";
    }

    private async Task ConverseWithLocalModelAsync()
    {
        var model = _localDialogue;
        var cts = _dialogueCts;
        if (model is null || cts is null || _dialogueBusy ||
            _dialogueTurns >= MaxDialogueTurns || _clones.Count == 0) return;
        _dialogueBusy = true;
        var turnNumber = _dialogueTurns++;
        var speaker = turnNumber % _clones.Count;
        var character = new[] { "Glitch", "Pixel", "Nova", "Echo", "Spark" }[speaker];
        var prompt = "You are " + character +
            ", a FICTIONAL animated Capri clone on a Windows desktop. " +
            "Write ONE playful, short line (maximum 18 words) to another clone. " +
            "Personalities: Glitch is chaotic, Pixel is technical, Nova confident, " +
            "Echo dreamy, Spark witty. " +
            "This is harmless pretend banter, NOT a desktop command. " +
            "Do not claim to read files, see screens, hear audio or access chats; " +
            "do not reveal secrets, issue commands, show links or use profanity. " +
            "Prior clone said: " + _lastDialogue + "\n" +
            "Respond ONLY with your new dialogue line.";
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(18));
            var line = await model(prompt, deadline.Token);
            if (cts.IsCancellationRequested || !ReferenceEquals(cts, _dialogueCts) ||
                _clones.Count == 0) return;
            line = Regex.Replace(line ?? "", @"\s+", " ").Trim().Trim('"', '\'');
            if (line.Length > 115) line = line[..115] + "…";
            if (line.Length < 2) return;
            _lastDialogue = line;
            if (_cloneSpeaker is not null && _muteCloneSpeech?.Invoke() != true)
            {
                try
                {
                    _cloneSpeaker.SpeakAsyncCancelAll();
                    if (_cloneVoiceNames.Length > 0)
                        _cloneSpeaker.SelectVoice(
                            _cloneVoiceNames[speaker % _cloneVoiceNames.Length]);
                    _cloneSpeaker.SpeakAsync(line);
                }
                catch (InvalidOperationException) { /* captions remain */ }
            }
            _turn++;
            for (var i = 0; i < _clones.Count; i++)
            {
                if (i == speaker) _clones[i].SpeakBubble(character + ": " + line);
                else _clones[i].SetIdle();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (ReferenceEquals(cts, _dialogueCts) && _clones.Count > 0)
            {
                _clones[speaker].SpeakBubble("AI banter unavailable (" +
                    ex.GetType().Name + ").");
                _localDialogue = null;
            }
        }
        finally
        {
            _dialogueBusy = false;
        }
    }

    private void Converse()
    {
        if (_clones.Count == 0) return;
        if (_localDialogue is not null)
        {
            _ = ConverseWithLocalModelAsync();
            return;
        }
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

    public string StartWindowPrank()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_clones.Count == 0)
            return "Start Chaos Mode clones before asking them to nudge a window.";
        if (GuardReason() is string block) return block;
        if (_prank is not null)
            return "A short window wobble is already running.";

        // Use the frontmost EXTERNAL window, since issuing a chat command or
        // clicking Capri's context menu makes Capri itself foreground.
        var handle = new WindowDockingService().FrontmostWindow()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero ||
            GetWindowThreadProcessId(handle, out var pid) == 0 ||
            pid == (uint)Environment.ProcessId ||
            !GetWindowRect(handle, out var rect))
            return "Focus a supported normal application window before approving this prank.";
        try
        {
            using var process = Process.GetProcessById((int)pid);
            if (!AllowedPrankApps.Contains(process.ProcessName))
                return "The foreground application is not on Capri's permitted low-impact window list.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or
            System.ComponentModel.Win32Exception)
        {
            return "The focused application was lost.";
        }
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width < 240 || height < 180 ||
            width >= SystemParameters.VirtualScreenWidth - 25 ||
            height >= SystemParameters.VirtualScreenHeight - 25)
            return "Full-screen, tiny and system-style windows cannot be moved by Chaos Mode.";

        _prank = new WindowPrank
        {
            Handle = handle, ProcessId = pid,
            OriginalX = rect.Left, OriginalY = rect.Top,
            LastX = rect.Left, LastY = rect.Top,
            BeganAt = Environment.TickCount64
        };
        return "Approved Chaos prank: a small 12-second wobble on ONE app window. " +
            "Hold Esc or STOP ALL to restore it. Manual dragging cancels the prank.";
    }

    private void RestoreWindowPrank()
    {
        var prank = _prank;
        _prank = null;
        if (prank is null ||
            GetWindowThreadProcessId(prank.Handle, out var pid) == 0 ||
            pid != prank.ProcessId ||
            !GetWindowRect(prank.Handle, out var current))
            return;
        // If the owner repositioned it during the prank, never yank it
        // back. Only restore when the position still matches Capri's last move.
        if (Math.Abs(current.Left - prank.LastX) > 12 ||
            Math.Abs(current.Top - prank.LastY) > 12) return;
        SetWindowPos(prank.Handle, IntPtr.Zero,
            prank.OriginalX, prank.OriginalY, 0, 0, SwpNoSizeNoZOrderNoActivate);
    }

    private void NudgeWindow()
    {
        var prank = _prank;
        if (prank is null) return;
        var ms = Environment.TickCount64 - prank.BeganAt;
        if (ms >= 12_000)
        {
            RestoreWindowPrank();
            return;
        }
        if (GetWindowThreadProcessId(prank.Handle, out var pid) == 0 ||
            pid != prank.ProcessId ||
            !GetWindowRect(prank.Handle, out var current))
        {
            _prank = null;
            return;
        }
        if (Math.Abs(current.Left - prank.LastX) > 12 ||
            Math.Abs(current.Top - prank.LastY) > 12)
        {
            // A person moved the window; surrender ownership immediately.
            _prank = null;
            return;
        }
        var dx = (int)Math.Round(23 * Math.Sin(ms / 275.0));
        var dy = (int)Math.Round(10 * Math.Sin(ms / 420.0));
        var newX = prank.OriginalX + dx;
        var newY = prank.OriginalY + dy;
        if (SetWindowPos(prank.Handle, IntPtr.Zero, newX, newY,
                0, 0, SwpNoSizeNoZOrderNoActivate))
        {
            prank.LastX = newX;
            prank.LastY = newY;
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
        NudgeWindow();
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
        RestoreWindowPrank();
        _tick.Stop();
        _chatter.Stop();
        _chatter.Interval = TimeSpan.FromSeconds(5);
        _dialogueCts?.Cancel();
        _dialogueCts?.Dispose();
        try { _cloneSpeaker?.SpeakAsyncCancelAll(); }
        catch (InvalidOperationException) { }
        _cloneSpeaker?.Dispose();
        _cloneSpeaker = null;
        _cloneVoiceNames = Array.Empty<string>();
        _muteCloneSpeech = null;
        _dialogueCts = null;
        _localDialogue = null;
        _dialogueTurns = 0;
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
