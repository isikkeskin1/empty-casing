using System.Diagnostics;
using System.Runtime.InteropServices;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// Executes explicitly confirmed allowlisted desktop actions. No arbitrary commands, keyboard
/// typing, clicks, files, message sending, purchases, shell scripts or powershell execution.
/// </summary>
public static class DesktopActionExecutor
{
    private static readonly Dictionary<string, string> Apps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["notepad"] = "notepad.exe",
        ["calculator"] = "calc.exe",
        ["explorer"] = "explorer.exe",
        ["edge"] = "msedge.exe",
        ["chrome"] = "chrome.exe",
        ["vscode"] = "code",
        ["spotify"] = "spotify.exe",
        ["discord"] = "discord.exe",
        ["steam"] = "steam.exe"
    };

    private static readonly Dictionary<string, byte> MediaKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["play_pause"] = 0xB3,
        ["next"] = 0xB0,
        ["previous"] = 0xB1,
        ["volume_up"] = 0xAF,
        ["volume_down"] = 0xAE,
        ["mute"] = 0xAD
    };

    private const uint KeyUp = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extraInfo);

    public static string Execute(AssistantAction action)
    {
        if (!ActionPolicy.TryValidate(action, out var reason))
            throw new InvalidOperationException(reason);

        switch (action.Kind)
        {
            case "open_app":
                Process.Start(new ProcessStartInfo
                {
                    FileName = Apps[action.Argument], UseShellExecute = true
                }) ?? throw new InvalidOperationException("Windows could not start that app.");
                return "Windows launched " + action.Argument + ".";
            case "open_url":
                Process.Start(new ProcessStartInfo
                {
                    FileName = action.Argument, UseShellExecute = true
                }) ?? throw new InvalidOperationException("Windows could not open the browser.");
                return "Opened website in your default browser.";
            case "media":
                var key = MediaKeys[action.Argument];
                keybd_event(key, 0, 0, UIntPtr.Zero);
                keybd_event(key, 0, KeyUp, UIntPtr.Zero);
                return "Sent media command: " + action.Argument + ".";
            default:
                throw new InvalidOperationException("Action not supported.");
        }
    }
}
