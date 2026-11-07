using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Capri.Desktop.Windows;

/// <summary>
/// Visible task receipts and a click-through pink pointer, never a real mouse
/// cursor. Current tasks still use validated accessibility patterns only.
/// </summary>
public partial class AutonomousTaskHudWindow : Window
{
    private readonly Queue<string> _steps = new();
    private readonly Window _pointer;
    private bool _pointerVisible;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    public AutonomousTaskHudWindow()
    {
        InitializeComponent();
        Left = Math.Max(SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width - 22);
        Top = SystemParameters.VirtualScreenTop + 42;

        _pointer = new Window
        {
            Width = 70,
            Height = 70,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowActivated = false,
            ShowInTaskbar = false,
            Topmost = true,
            IsHitTestVisible = false,
            Content = new TextBlock
            {
                Text = "➤",
                FontSize = 43,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 81, 172)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 1,
                    BlurRadius = 6
                }
            }
        };
        _pointer.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(_pointer).Handle;
            var old = GetWindowLongPtr(hwnd, -20).ToInt64();
            SetWindowLongPtr(hwnd, -20, new IntPtr(old | 0x20L | 0x08000000L));
        };
        Closed += (_, _) =>
        {
            if (_pointerVisible) _pointer.Close();
        };
    }

    public void AddStep(string step)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => AddStep(step));
            return;
        }
        if (string.IsNullOrWhiteSpace(step)) return;
        _steps.Enqueue(step.Length > 180 ? step[..180] + "…" : step);
        while (_steps.Count > 9) _steps.Dequeue();
        StepsText.Text = string.Join("\n\n", _steps);
    }

    public void PointTo(string label, double pixelX, double pixelY)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PointTo(label, pixelX, pixelY));
            return;
        }
        if (!double.IsFinite(pixelX) || !double.IsFinite(pixelY)) return;
        var matrix = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice
            ?? Matrix.Identity;
        var point = matrix.Transform(new Point(pixelX, pixelY));
        _pointer.Left = Math.Clamp(point.X - 14, SystemParameters.VirtualScreenLeft,
            Math.Max(SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - _pointer.Width));
        _pointer.Top = Math.Clamp(point.Y - 18, SystemParameters.VirtualScreenTop,
            Math.Max(SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - _pointer.Height));
        if (!_pointerVisible)
        {
            _pointer.Show();
            _pointerVisible = true;
        }
        AddStep("TARGET  " + label);
    }

    private void Stop_Click(object sender, RoutedEventArgs e) =>
        App.Instance.StopAutonomousTask();
}
