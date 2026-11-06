using System.Runtime.InteropServices;
using System.Windows.Interop;
using Capri.Core;

namespace Capri.Desktop.Services;

/// <summary>
/// RegisterHotKey-based global owner shortcuts; no keyboard hook or key logging.
/// Every hit dispatches only a named built-in action, never an arbitrary model command.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int BaseId = 0xCA90;
    private readonly IntPtr _windowHandle;
    private readonly HwndSource _source;
    private readonly Action<string> _onAction;
    private readonly Dictionary<int, string> _registered = new();
    private bool _disposed;

    public GlobalHotkeyService(IntPtr windowHandle, Action<string> onAction)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle)
            ?? throw new InvalidOperationException("Companion HWND was not available for shortcuts.");
        _onAction = onAction;
        _source.AddHook(OnMessage);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr handle, int id);

    /// <returns>List of shortcuts that Windows could not register (another app may own them).</returns>
    public string Apply(InputShortcuts shortcuts)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!InputShortcuts.TryValidate(shortcuts, out var reason))
            throw new ArgumentException(reason, nameof(shortcuts));

        UnregisterAll();
        var failures = new List<string>();
        var index = 0;
        foreach (var (action, binding) in shortcuts.Bindings())
        {
            var id = BaseId + index++;
            GlobalShortcut.TryParse(binding, out var gesture);
            if (RegisterHotKey(_windowHandle, id,
                gesture.Modifiers | GlobalShortcut.NoRepeat, gesture.VirtualKey))
                _registered.Add(id, action);
            else failures.Add(action + " (" + binding + ")");
        }
        return failures.Count == 0 ? "" :
            "Windows could not register: " + string.Join(", ", failures) +
            ". Choose a different binding or close the conflicting application.";
    }

    private IntPtr OnMessage(IntPtr window, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == WmHotkey && _registered.TryGetValue(wparam.ToInt32(), out var action))
        {
            _onAction(action);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Suspend()
    {
        if (!_disposed) UnregisterAll();
    }

    private void UnregisterAll()
    {
        foreach (var id in _registered.Keys)
            UnregisterHotKey(_windowHandle, id);
        _registered.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAll();
        _source.RemoveHook(OnMessage);
    }
}
