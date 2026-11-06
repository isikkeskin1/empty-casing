using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Capri.Desktop.Windows;

/// <summary>
/// Short owner-requested coach tip drawn above borderless game windows.
/// The native click-through style keeps mouse/game inputs directed to the
/// underlying application. No permanent capture or game injection.
/// </summary>
public partial class GameCoachHudWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExNoactivate = 0x08000000;
    private const int WsExToolwindow = 0x80;
    private readonly DispatcherTimer _expires = new()
    {
        Interval = TimeSpan.FromSeconds(30)
    };

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(nint window, int index, int value);

    public GameCoachHudWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var original = GetWindowLong(hwnd, GwlExstyle);
            SetWindowLong(hwnd, GwlExstyle,
                original | WsExTransparent | WsExNoactivate | WsExToolwindow);
        };
        _expires.Tick += (_, _) => HideTip();
        Closed += (_, _) => _expires.Stop();
    }

    public void ShowTip(string text)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ShowTip(text));
            return;
        }
        if (string.IsNullOrWhiteSpace(text)) return;
        TipText.Text = text.Length > 650 ? text[..650] + "…" : text;
        var area = SystemParameters.WorkArea;
        Left = Math.Max(area.Left + 5, area.Right - Width - 18);
        Top = area.Top + 36;
        if (!IsVisible) Show();
        _expires.Stop();
        _expires.Start();
    }

    public void HideTip()
    {
        _expires.Stop();
        if (IsVisible) Hide();
    }
}
