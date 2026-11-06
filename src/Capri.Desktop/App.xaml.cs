using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Capri.Core;
using Capri.Desktop.Windows;

namespace Capri.Desktop;

public partial class App : System.Windows.Application
{
    private const int HotkeyId = 0xCA91;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr handle, int id);

    private CompanionWindow? _companion;
    private ChatWindow? _chat;
    private System.Windows.Forms.NotifyIcon? _tray;
    private IntPtr _windowHandle;
    private bool _registeredHotkey;

    public static App Instance => (App)Current;
    public AppSettings Settings { get; private set; } = new();
    public LocalDataStore Store { get; private set; } = null!;
    public OllamaBrain Brain { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Store = new LocalDataStore();
        Settings = Store.LoadSettings();
        Brain = new OllamaBrain(Settings);

        _companion = new CompanionWindow();
        MainWindow = _companion;
        _companion.Show();
        RegisterGlobalHotkey();
        CreateTray();
    }

    private void RegisterGlobalHotkey()
    {
        if (_companion is null || _windowHandle != IntPtr.Zero) return;
        _windowHandle = new WindowInteropHelper(_companion).Handle;
        if (_windowHandle == IntPtr.Zero) return;
        HwndSource.FromHwnd(_windowHandle)?.AddHook(OnWindowMessage);
        _registeredHotkey = RegisterHotKey(_windowHandle, HotkeyId, ModControl | ModShift, VkSpace);
    }

    private IntPtr OnWindowMessage(IntPtr handle, int message, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (message == WmHotkey && wparam.ToInt32() == HotkeyId)
        {
            ToggleChat();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void CreateTray()
    {
        var context = new System.Windows.Forms.ContextMenuStrip();
        context.Items.Add("Open chat", null, (_, _) => Dispatcher.Invoke(ShowChat));
        context.Items.Add("Show / hide Capri", null, (_, _) => Dispatcher.Invoke(ToggleCompanion));
        context.Items.Add("Open character art folder", null, (_, _) => Dispatcher.Invoke(OpenCharacterFolder));
        context.Items.Add("Reload character art", null, (_, _) => Dispatcher.Invoke(() => _companion?.ReloadArt()));
        context.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        context.Items.Add("Quit Capri", null, (_, _) => Dispatcher.Invoke(Shutdown));

        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Capri Desktop (unofficial fan project)",
            ContextMenuStrip = context
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowChat);
    }

    public void OpenCharacterFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "\"" + Store.CharacterFolder + "\"",
            UseShellExecute = true
        });
    }

    public void ShowChat()
    {
        if (_chat is null)
        {
            _chat = new ChatWindow();
            _chat.Closed += (_, _) => _chat = null;
        }

        _chat.Show();
        _chat.Activate();
    }

    public void ToggleChat()
    {
        if (_chat?.IsVisible == true) _chat.Hide();
        else ShowChat();
    }

    public void ToggleCompanion()
    {
        if (_companion is null) return;
        if (_companion.IsVisible) _companion.Hide();
        else _companion.Show();
    }

    public void SetMood(CompanionMood mood) => _companion?.SetMood(mood);

    public void SavePosition(double left, double top)
    {
        Settings.OverlayLeft = left;
        Settings.OverlayTop = top;
        Store.SaveSettings(Settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_registeredHotkey && _windowHandle != IntPtr.Zero)
            UnregisterHotKey(_windowHandle, HotkeyId);

        Brain?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        base.OnExit(e);
    }
}
