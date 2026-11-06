using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Capri.Desktop.Services;

/// <summary>
/// Captures a single screenshot on demand. Nothing is written to disk.
/// All screen watching must be explicitly enabled in the UI.
/// </summary>
public static class ScreenCaptureService
{
    public static byte[] CapturePrimaryScreenJpeg() => CaptureScreenJpeg(-1);

    public static byte[] CaptureScreenJpeg(int index)
    {
        var screens = Screen.AllScreens;
        Screen screen;
        if (index == -2)
        {
            // Follow the frontmost external window, excluding Capri's own windows.
            var target = new WindowDockingService().FrontmostWindow();
            screen = target.HasValue
                ? Screen.FromHandle(target.Value.Handle)
                : Screen.PrimaryScreen ?? throw new InvalidOperationException("No primary display available.");
        }
        else
            screen = index >= 0 && index < screens.Length ? screens[index] :
                Screen.PrimaryScreen ?? throw new InvalidOperationException("No primary display available.");
        var bounds = screen.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("No valid display bounds.");

        using var source = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(source))
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);

        const int longestSide = 1280;
        var scale = Math.Min(1.0, (double)longestSide / Math.Max(bounds.Width, bounds.Height));
        var w = Math.Max(1, (int)(bounds.Width * scale));
        var h = Math.Max(1, (int)(bounds.Height * scale));
        using var scaled = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(source, 0, 0, w, h);
        }

        using var stream = new MemoryStream();
        var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.MimeType == "image/jpeg");
        if (encoder is null)
            scaled.Save(stream, ImageFormat.Jpeg);
        else
        {
            using var options = new EncoderParameters(1);
            options.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 75L);
            scaled.Save(stream, encoder, options);
        }
        return stream.ToArray();
    }

    /// <summary>Only 16x9 grayscale samples are retained between watch ticks.</summary>
    public static byte[] ThumbnailLuminance(byte[] jpeg)
    {
        using var memory = new MemoryStream(jpeg, writable: false);
        using var original = new Bitmap(memory);
        using var thumbnail = new Bitmap(16, 9, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(thumbnail))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.DrawImage(original, 0, 0, 16, 9);
        }
        var samples = new byte[16 * 9];
        for (var y = 0; y < 9; y++)
            for (var x = 0; x < 16; x++)
            {
                var pixel = thumbnail.GetPixel(x, y);
                samples[y * 16 + x] = (byte)((pixel.R * 77 +
                    pixel.G * 150 + pixel.B * 29) >> 8);
            }
        return samples;
    }
}
