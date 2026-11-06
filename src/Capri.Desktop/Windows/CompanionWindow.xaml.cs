using System.Diagnostics;
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
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = App.Instance.Settings;
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
            App.Instance.ShowChat();
            e.Handled = true;
            return;
        }

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
        // DragMove returns after mouse release; measure the drop against other apps' top edges.
        var cursor = System.Windows.Forms.Cursor.Position;
        var target = _windows.WindowNearEdge(cursor);
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
        ReactionText.Text = reaction.Length > 220 ? reaction[..220] + "…" : reaction;
        ReactionBubble.Visibility = Visibility.Visible;
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

    private void OpenChat_Click(object sender, RoutedEventArgs e) => App.Instance.ShowChat();
    private void OpenArt_Click(object sender, RoutedEventArgs e) => App.Instance.OpenCharacterFolder();
    private void ReloadArt_Click(object sender, RoutedEventArgs e) => ReloadArt();
    private void Idle_Click(object sender, RoutedEventArgs e)
    {
        DetachWindow();
        SetMood(CompanionMood.Idle);
    }
    private void Attach_Click(object sender, RoutedEventArgs e) => PerchOnFrontWindow();
    private void Detach_Click(object sender, RoutedEventArgs e) => DetachWindow();
    private void Talk_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Talking);
    private void Glitch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Glitching);
    private void Toss_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Tossed);
    private void Sleep_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Sleeping);
    private void Perch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Perching);
    private void MusicPerch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.MusicPerch);
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Quit_Click(object sender, RoutedEventArgs e) => App.Instance.Shutdown();
}
