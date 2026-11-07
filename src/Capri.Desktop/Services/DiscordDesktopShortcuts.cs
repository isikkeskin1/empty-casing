using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Capri.Desktop.Services;

/// <summary>
/// Sends only Discord's documented local mute/deafen keyboard shortcuts when
/// the OWNER explicitly invokes them. No user tokens, Discord API requests,
/// self-bot, message sending, call joining or attempts to read call state.
/// </summary>
public static class DiscordDesktopShortcuts
{
    public enum Command { ToggleMute, ToggleDeafen }

    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct InputEvent
    {
        public uint Type;
        public EventUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct EventUnion
    {
        [FieldOffset(0)] public KeyboardEvent Keyboard;
        [FieldOffset(0)] public MouseEvent Mouse; // ensure INPUT's native size
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardEvent
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseEvent
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] InputEvent[] events, int size);

    public static bool IsDiscordOpen()
    {
        foreach (var name in new[] { "Discord", "DiscordCanary", "DiscordPTB" })
        {
            using var probe = Process.GetProcessesByName(name).FirstOrDefault();
            if (probe is not null) return true;
        }
        return false;
    }

    private static InputEvent Key(ushort virtualKey, bool up) => new()
    {
        Type = InputKeyboard,
        Union = new EventUnion
        {
            Keyboard = new KeyboardEvent
            {
                VirtualKey = virtualKey,
                Flags = up ? KeyUp : 0
            }
        }
    };

    public static string Send(Command command)
    {
        if (!IsDiscordOpen())
            return "Discord desktop is not running. Launch Discord first.";
        // Discord's documented desktop global shortcuts:
        // Ctrl+Shift+M toggles mic; Ctrl+Shift+D toggles deafen.
        var key = command == Command.ToggleMute ? (ushort)0x4D : (ushort)0x44;
        var events = new[]
        {
            Key(0x11, false), Key(0x10, false), Key(key, false),
            Key(key, true), Key(0x10, true), Key(0x11, true)
        };
        var delivered = SendInput((uint)events.Length, events,
            Marshal.SizeOf<InputEvent>());
        return delivered == events.Length
            ? "Discord " + (command == Command.ToggleMute ? "mute" : "deafen") +
              " shortcut sent. Check its actual state in Discord; this is a toggle, not a state API."
            : "Windows did not deliver all Discord shortcut key events. Check desktop permissions.";
    }
}
