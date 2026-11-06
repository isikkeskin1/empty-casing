using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Capri.Desktop.Windows;

/// <summary>
/// User-controlled annotation mode: draws only into temporary WPF strokes.
/// The transparent window intentionally captures mouse input while active.
/// Never reads game pixels, injects into games or captures audio/video.
/// </summary>
public partial class ScreenSketchWindow : Window
{
    private readonly DispatcherTimer _timeout = new()
    {
        Interval = TimeSpan.FromMinutes(3)
    };

    public ScreenSketchWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        SetColor(Color.FromRgb(42, 240, 232));
        _timeout.Tick += (_, _) => Close();
        Closed += (_, _) => _timeout.Stop();
        Loaded += (_, _) =>
        {
            Activate();
            Focus();
            Keyboard.Focus(this);
            _timeout.Start();
        };
    }

    private void SetColor(Color color)
    {
        SketchCanvas.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color = color,
            Width = 4,
            Height = 4,
            FitToCurve = true,
            StylusTip = StylusTip.Ellipse
        };
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string choice }) return;
        var color = choice switch
        {
            "yellow" => Color.FromRgb(242, 216, 61),
            "pink" => Color.FromRgb(255, 77, 169),
            _ => Color.FromRgb(42, 240, 232)
        };
        SetColor(color);
        SketchStatus.Text = "Drawing in " + choice.ToUpperInvariant() +
                            ". ESC exits and restores normal mouse input.";
        Focus();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        var strokes = SketchCanvas.Strokes;
        if (strokes.Count > 0)
            strokes.RemoveAt(strokes.Count - 1);
        Focus();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SketchCanvas.Strokes.Clear();
        SketchStatus.Text = "Canvas cleared. Nothing was written to your game.";
        Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (SketchCanvas.Strokes.Count == 0)
        {
            SketchStatus.Text = "Draw something before exporting.";
            return;
        }
        var width = (int)Math.Ceiling(SketchCanvas.ActualWidth);
        var height = (int)Math.Ceiling(SketchCanvas.ActualHeight);
        if (width <= 0 || height <= 0 || width > 6400 || height > 4000)
        {
            SketchStatus.Text = "This virtual desktop is too large for PNG export.";
            return;
        }
        var dialog = new SaveFileDialog
        {
            FileName = "Capri-Sketch.png",
            DefaultExt = ".png",
            Filter = "PNG image|*.png",
            AddExtension = true,
            Title = "Save only your drawn strokes (not the screen or game)"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            // Rendering the transparent InkCanvas saves only the owner's
            // strokes; it does not capture the underlying desktop image.
            var bitmap = new RenderTargetBitmap(width, height, 96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(SketchCanvas);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var output = System.IO.File.Create(dialog.FileName);
            encoder.Save(output);
            SketchStatus.Text = "Sketch saved. No game screenshot was included.";
        }
        catch (Exception ex) when (ex is System.IO.IOException or
            UnauthorizedAccessException or InvalidOperationException)
        {
            SketchStatus.Text = "Couldn't export PNG: " + ex.GetType().Name;
        }
        Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
