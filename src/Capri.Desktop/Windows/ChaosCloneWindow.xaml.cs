using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

/// <summary>
/// One animated, non-interactive Capri copy. No input injection, desktop-window
/// movement, account access, microphone capture or independent AI permissions.
/// </summary>
public partial class ChaosCloneWindow : Window
{
    private readonly int _number;
    private readonly string _artFolder;
    private readonly DispatcherTimer _drift = new() { Interval = TimeSpan.FromMilliseconds(65) };
    private FrameAnimator? _frames;
    private double _angle;
    private double _originLeft;
    private double _originTop;

    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;
    private const long WsExNoActivate = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);

    public ChaosCloneWindow(int number, string artFolder)
    {
        _number = number;
        _artFolder = artFolder;
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var old = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(old | WsExTransparent | WsExNoActivate));
        };
        Loaded += (_, _) =>
        {
            _originLeft = Left;
            _originTop = Top;
            _frames = new FrameAnimator(_artFolder, image => CharacterImage.Source = image);
            _frames.SetState(number % 3 == 0 ? "glitch" : "idle");
            _drift.Start();
        };
        _drift.Tick += (_, _) => Drift();
        Closed += (_, _) =>
        {
            _drift.Stop();
            _frames?.Dispose();
            _frames = null;
        };
    }

    private void Drift()
    {
        _angle += 0.045 + _number * 0.005;
        var minX = SystemParameters.VirtualScreenLeft;
        var minY = SystemParameters.VirtualScreenTop;
        Left = Math.Clamp(_originLeft + Math.Sin(_angle) * 28, minX,
            Math.Max(minX, minX + SystemParameters.VirtualScreenWidth - Width));
        Top = Math.Clamp(_originTop + Math.Cos(_angle * 0.77) * 19, minY,
            Math.Max(minY, minY + SystemParameters.VirtualScreenHeight - Height));
    }

    public void SpeakBubble(string line)
    {
        Caption.Text = line;
        _frames?.SetState("talk");
    }

    public void SetIdle() => _frames?.SetState(_number % 3 == 0 ? "glitch" : "idle");
}
