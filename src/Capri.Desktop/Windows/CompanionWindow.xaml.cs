using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CompanionWindow : Window
{
    private readonly WindowDockingService _windows = new();
    private readonly DispatcherTimer _returnToIdle = new();
    private readonly DispatcherTimer _reactionTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly DispatcherTimer _follow = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly DispatcherTimer _flight = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly DispatcherTimer _cursorFollow = new() { Interval = TimeSpan.FromMilliseconds(35) };
    private bool _cursorRiding;
    public bool CursorRiding => _cursorRiding;
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    private FrameAnimator? _frames;
    private readonly PettingGesture _petting = new();
    private WindowDockingService.Target? _attached;
    private double _relativeX = 0.5;
    private TossMotion.State _motion;
    private long _lastMotionTick;
    private CompanionMood _mood = CompanionMood.Idle;

    public CompanionWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _returnToIdle.Stop();
            _reactionTimer.Stop();
            _follow.Stop();
            _flight.Stop();
            _cursorFollow.Stop();
            _frames?.Dispose();
        };
        _returnToIdle.Tick += (_, _) =>
        {
            _returnToIdle.Stop();
            SetMood(_attached.HasValue ? CompanionMood.Perching : CompanionMood.Idle);
        };
        _reactionTimer.Tick += (_, _) =>
        {
            _reactionTimer.Stop();
            ReactionBubble.Visibility = Visibility.Collapsed;
        };
        _follow.Tick += (_, _) => TrackAttachedWindow();
        _flight.Tick += (_, _) => AdvanceFlight();
        _cursorFollow.Tick += (_, _) => TrackCursor();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = App.Instance.Settings;
        var scale = double.IsFinite(settings.OverlayScale)
            ? Math.Clamp(settings.OverlayScale, 0.5, 1.6) : 1.0;
        Width = 270 * scale;
        Height = 480 * scale;
        var area = SystemParameters.WorkArea;
        Left = settings.OverlayLeft is double left && double.IsFinite(left) &&
               left >= area.Left && left < area.Right - 50 ? left : area.Right - Width - 20;
        Top = settings.OverlayTop is double top && double.IsFinite(top) &&
              top >= area.Top && top < area.Bottom - 50 ? top : area.Bottom - Height - 25;

        var bob = new DoubleAnimation(0, -6, TimeSpan.FromSeconds(1.9))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        FloatOffset.BeginAnimation(TranslateTransform.YProperty, bob);

        _frames ??= new FrameAnimator(App.Instance.Store.CharacterFolder, image =>
        {
            CharacterImage.Source = image;
            CharacterImage.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
            Placeholder.Visibility = image is null ? Visibility.Visible : Visibility.Collapsed;
        });
        SetMood(_mood);
    }

    private void Character_MouseMove(object sender, MouseEventArgs e)
    {
        if (!App.Instance.Settings.EnablePetting ||
            e.LeftButton == MouseButtonState.Pressed ||
            e.RightButton == MouseButtonState.Pressed) return;

        var local = e.GetPosition(CharacterSurface);
        var timestampMs = Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
        if (_petting.Observe(local.X, local.Y, timestampMs))
            ShowReaction("Hey, easy with the hair! Okay, that's actually pretty nice.");
    }

    private void Character_MouseLeave(object sender, MouseEventArgs e) => _petting.Reset();

    private void Character_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            ToggleSleep();
            e.Handled = true;
            return;
        }

        EndCursorRide();
        _flight.Stop();
        _follow.Stop();
        var originalLeft = Left;
        var originalTop = Top;
        var watch = Stopwatch.StartNew();

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        finally
        {
            watch.Stop();
        }

        var travel = Math.Sqrt(Math.Pow(Left - originalLeft, 2) + Math.Pow(Top - originalTop, 2));
        if (travel < 8)
        {
            // A click on a perched character should not unpin her from the window.
            if (_attached.HasValue) _follow.Start();
            return;
        }

        DetachWindow();
        var modifiers = Keyboard.Modifiers;
        // Shift-drop seats Capri at the owner's exact position rather than
        // trying to attach her to an unrelated top-level window.
        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            SetMood(CompanionMood.Perching);
            App.Instance.SavePosition(Left, Top);
            return;
        }

        // Ctrl-drop explicitly requests a persistent window-edge hang. The
        // seven-state art currently uses the perch frame for this pose.
        var cursor = System.Windows.Forms.Cursor.Position;
        var target = _windows.WindowNearEdge(cursor,
            modifiers.HasFlag(ModifierKeys.Control) ? 180 : 90);
        if (!target.HasValue && modifiers.HasFlag(ModifierKeys.Control))
            target = _windows.FrontmostWindow();
        if (target.HasValue)
        {
            Attach(target.Value, cursor.X);
            return;
        }

        var dt = Math.Max(0.13, watch.Elapsed.TotalSeconds);
        var dx = Left - originalLeft;
        var dy = Top - originalTop;
        var speed = Math.Sqrt(dx * dx + dy * dy) / dt;

        if (speed > 850 && Math.Sqrt(dx * dx + dy * dy) > 90)
        {
            // Velocity is clamped to avoid launching the companion far off-screen.
            _motion = new TossMotion.State(Left, Top,
                Math.Clamp(dx / dt * 0.55, -1600, 1600),
                Math.Clamp(dy / dt * 0.55, -1600, 1600), false);
            _lastMotionTick = Stopwatch.GetTimestamp();
            _flight.Start();
            SetMood(CompanionMood.Tossed);
        }
        else
        {
            SetMood(CompanionMood.Idle);
            App.Instance.SavePosition(Left, Top);
        }
    }

    private System.Windows.Point ToDip(int pixelX, int pixelY)
    {
        var source = PresentationSource.FromVisual(this);
        var matrix = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        return matrix.Transform(new System.Windows.Point(pixelX, pixelY));
    }

    private void Attach(WindowDockingService.Target target, double centerXInPixels)
    {
        _flight.Stop();
        var pLeft = ToDip(target.Rect.Left, target.Rect.Top);
        var pRight = ToDip(target.Rect.Right, target.Rect.Bottom);
        var preferredCenter = ToDip((int)centerXInPixels, target.Rect.Top).X;
        _relativeX = PerchLayout.RelativePosition(preferredCenter, pLeft.X, pRight.X);
        _attached = target;
        _follow.Start();
        TrackAttachedWindow();
        SetMood(CompanionMood.Perching);
    }

    public void PerchOnFrontWindow()
    {
        var target = _windows.FrontmostWindow();
        if (!target.HasValue)
        {
            SetMood(CompanionMood.Glitching);
            return;
        }
        Attach(target.Value, target.Value.Rect.Left + target.Value.Rect.Width * 0.72);
    }

    public void BringNearPointer()
    {
        if (!IsVisible) Show();
        EndCursorRide();
        _flight.Stop();
        DetachWindow();
        var cursor = System.Windows.Forms.Cursor.Position;
        var point = ToDip(cursor.X, cursor.Y);
        Left = Math.Clamp(point.X - Width / 2,
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
        Top = Math.Clamp(point.Y - Height / 2,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
        SetMood(CompanionMood.Glitching);
        App.Instance.SavePosition(Left, Top);
    }

    private void SetRideClickThrough(bool enabled)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var oldStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        var newStyle = enabled ? oldStyle | WsExTransparent : oldStyle & ~WsExTransparent;
        if (oldStyle != newStyle)
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(newStyle));
    }

    private void TrackCursor()
    {
        if (!_cursorRiding) return;
        var cursor = System.Windows.Forms.Cursor.Position;
        var point = ToDip(cursor.X, cursor.Y);
        var minX = SystemParameters.VirtualScreenLeft;
        var minY = SystemParameters.VirtualScreenTop;
        Left = Math.Clamp(point.X - Width * 0.5, minX,
            Math.Max(minX, minX + SystemParameters.VirtualScreenWidth - Width));
        // Capri floats *above* the pointer, never directly on its click target.
        Top = Math.Clamp(point.Y - Height - 16, minY,
            Math.Max(minY, minY + SystemParameters.VirtualScreenHeight - Height));
    }

    public void StartCursorRide()
    {
        if (!IsVisible) Show();
        if (_cursorRiding) return;
        _flight.Stop();
        DetachWindow();
        _cursorRiding = true;
        SetRideClickThrough(true);
        _cursorFollow.Start();
        TrackCursor();
        SetMood(CompanionMood.Perching);
    }

    public void EndCursorRide()
    {
        if (!_cursorRiding) return;
        _cursorRiding = false;
        _cursorFollow.Stop();
        SetRideClickThrough(false);
        SetMood(CompanionMood.Idle);
        App.Instance.SavePosition(Left, Top);
    }

    public void ToggleSleep()
    {
        if (_mood == CompanionMood.Sleeping)
            SetMood(CompanionMood.Idle);
        else
        {
            EndCursorRide();
            _flight.Stop();
            SetMood(CompanionMood.Sleeping);
        }
    }

    public void Wake() => SetMood(CompanionMood.Idle);
    public void GoToSleep()
    {
        EndCursorRide();
        _flight.Stop();
        SetMood(CompanionMood.Sleeping);
    }

    public void SetScale(double value)
    {
        if (!double.IsFinite(value) || value < 0.5 || value > 1.6) return;
        var centerX = Left + Width * 0.5;
        var centerY = Top + Height * 0.5;
        Width = 270 * value;
        Height = 480 * value;
        var minX = SystemParameters.VirtualScreenLeft;
        var minY = SystemParameters.VirtualScreenTop;
        Left = Math.Clamp(centerX - Width * 0.5, minX,
            Math.Max(minX, minX + SystemParameters.VirtualScreenWidth - Width));
        Top = Math.Clamp(centerY - Height * 0.5, minY,
            Math.Max(minY, minY + SystemParameters.VirtualScreenHeight - Height));
        App.Instance.Settings.OverlayScale = value;
        App.Instance.Store.SaveSettings(App.Instance.Settings);
        App.Instance.SavePosition(Left, Top);
        if (_cursorRiding) TrackCursor();
    }

    private void Scale_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: string tag } &&
            double.TryParse(tag, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var scale))
            SetScale(scale);
    }

    public void DetachWindow()
    {
        _attached = null;
        _follow.Stop();
        if (_mood is CompanionMood.Perching or CompanionMood.MusicPerch)
            SetMood(CompanionMood.Idle);
    }

    private void TrackAttachedWindow()
    {
        if (!_attached.HasValue) return;
        if (!_windows.TryGetBounds(_attached.Value.Handle, out var bounds))
        {
            DetachWindow();
            App.Instance.SavePosition(Left, Top);
            return;
        }

        var leftTop = ToDip(bounds.Left, bounds.Top);
        var rightBottom = ToDip(bounds.Right, bounds.Bottom);
        var target = PerchLayout.Place(leftTop.X, rightBottom.X, leftTop.Y,
            _relativeX, Width, SystemParameters.VirtualScreenTop + 6);

        Left = target.Left;
        Top = target.Top;
    }

    private void AdvanceFlight()
    {
        var now = Stopwatch.GetTimestamp();
        var dt = (double)(now - _lastMotionTick) / Stopwatch.Frequency;
        _lastMotionTick = now;

        var screenLeft = SystemParameters.VirtualScreenLeft;
        var screenTop = SystemParameters.VirtualScreenTop;
        var screenRight = screenLeft + SystemParameters.VirtualScreenWidth - Width;
        var screenBottom = screenTop + SystemParameters.VirtualScreenHeight - Height;
        _motion = TossMotion.Step(_motion, dt, screenLeft, screenTop, screenRight, screenBottom);

        Left = _motion.Left;
        Top = _motion.Top;
        if (!_motion.Stopped) return;

        _flight.Stop();
        App.Instance.SavePosition(Left, Top);
    }

    public void SetMusicActivity(bool active)
    {
        if (!active)
        {
            if (_mood == CompanionMood.MusicPerch)
                SetMood(_attached.HasValue ? CompanionMood.Perching : CompanionMood.Idle);
            return;
        }
        if (_mood is CompanionMood.Idle or CompanionMood.Perching)
            SetMood(CompanionMood.MusicPerch);
    }

    public void OnMusicPulse()
    {
        if (!App.Instance.Settings.DanceToSystemAudio) return;
        SetMusicActivity(true);
        if (_mood != CompanionMood.MusicPerch) return;
        var pulse = new DoubleAnimation(0.75, 1.0, TimeSpan.FromMilliseconds(180));
        CharacterImage.BeginAnimation(OpacityProperty, pulse);
    }

    public void ShowReaction(string reaction)
    {
        if (string.IsNullOrWhiteSpace(reaction)) return;
        ReactionText.Text = reaction.Length > 150 ? reaction[..150] + "…" : reaction;
        ReactionBubble.Visibility = Visibility.Visible;
        // A short entrance makes the card feel alive without endless flickering.
        ReactionBubble.BeginAnimation(OpacityProperty, new DoubleAnimation(
            0, 1, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
        _reactionTimer.Stop();
        _reactionTimer.Start();
        SetMood(CompanionMood.Talking);
    }

    public void SetMood(CompanionMood mood)
    {
        _mood = mood;
        _returnToIdle.Stop();
        var (label, color, state) = mood switch
        {
            CompanionMood.Thinking => ("THINKING", "#C994FA", "idle"),
            CompanionMood.Reply => ("REPLY READY", "#7DEBD1", "talk"),
            CompanionMood.Error => ("ERROR", "#FE7995", "glitch"),
            CompanionMood.Talking => ("TALKING", "#FA8EB3", "talk"),
            CompanionMood.Glitching => ("GLITCHING", "#C994FA", "glitch"),
            CompanionMood.Tossed => ("TOSSED", "#FE7995", "tossed"),
            CompanionMood.Sleeping => ("SLEEPING", "#8C9EC3", "sleep"),
            CompanionMood.Perching => ("PERCHED", "#71DDF1", "perch"),
            CompanionMood.MusicPerch => ("MUSIC PERCH", "#71DDF1", "perch-music"),
            _ => ("IDLE", "#71DDF1", "idle")
        };

        StatusText.Text = "CAPRI // " + label;
        StatusLight.Fill = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)!);
        _frames?.SetState(state);

        if (mood is CompanionMood.Talking or CompanionMood.Reply or CompanionMood.Glitching or CompanionMood.Tossed)
        {
            var duration = _frames?.CurrentDurationMs ?? 1500;
            if (duration < 400) duration = 1500;
            _returnToIdle.Interval = TimeSpan.FromMilliseconds(
                mood == CompanionMood.Reply ? Math.Max(duration, 2500) : duration);
            _returnToIdle.Start();
        }
    }

    public void ReloadArt()
    {
        _frames?.Reload();
        SetMood(_mood);
    }

    private void DismissReaction_Click(object sender, RoutedEventArgs e)
    {
        _reactionTimer.Stop();
        ReactionBubble.Visibility = Visibility.Collapsed;
    }

    private void ReactionTalk_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Instance.StartTalkOnce();
        }
        catch (Exception ex) when (ex is InvalidOperationException or
            System.ComponentModel.Win32Exception)
        {
            // The CTA always gives the owner an interaction route, even when
            // Windows Speech or a selected microphone isn't configured.
            App.Instance.ShowChat();
            ShowReaction("Microphone unavailable. Type to me in the Control Deck.");
        }
    }

    private void OpenChat_Click(object sender, RoutedEventArgs e) => App.Instance.ShowChat();
    private void ImportArt_Click(object sender, RoutedEventArgs e) =>
        App.Instance.ImportCharacterPackInteractive();
    private void OpenArt_Click(object sender, RoutedEventArgs e) => App.Instance.OpenCharacterFolder();
    private void ReloadArt_Click(object sender, RoutedEventArgs e) => ReloadArt();
    private void Idle_Click(object sender, RoutedEventArgs e)
    {
        EndCursorRide();
        DetachWindow();
        SetMood(CompanionMood.Idle);
    }
    private void Attach_Click(object sender, RoutedEventArgs e) => PerchOnFrontWindow();
    private void Detach_Click(object sender, RoutedEventArgs e) => DetachWindow();
    private void Talk_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Talking);
    private void Glitch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Glitching);
    private void Toss_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Tossed);
    private void Sleep_Click(object sender, RoutedEventArgs e) => GoToSleep();
    private void RideCursor_Click(object sender, RoutedEventArgs e) => StartCursorRide();
    private void HopOff_Click(object sender, RoutedEventArgs e) => EndCursorRide();
    private void Perch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Perching);
    private void MusicPerch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.MusicPerch);
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Quit_Click(object sender, RoutedEventArgs e) => App.Instance.Shutdown();
}
