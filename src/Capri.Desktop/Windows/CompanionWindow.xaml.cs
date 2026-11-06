using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CompanionWindow : Window
{
    private FrameAnimator? _frames;
    private CompanionMood _mood = CompanionMood.Idle;
    private readonly DispatcherTimer _returnToIdle = new();

    public CompanionWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) =>
        {
            _returnToIdle.Stop();
            _frames?.Dispose();
        };
        _returnToIdle.Tick += (_, _) =>
        {
            _returnToIdle.Stop();
            SetMood(CompanionMood.Idle);
        };
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

    private void Character_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
        {
            App.Instance.ShowChat();
            e.Handled = true;
            return;
        }

        try
        {
            DragMove();
            App.Instance.SavePosition(Left, Top);
        }
        catch (InvalidOperationException)
        {
            // A click can finish before Windows initiates a drag.
        }
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
            CompanionMood.Perching => ("PERCH PREVIEW", "#71DDF1", "perch"),
            CompanionMood.MusicPerch => ("MUSIC PREVIEW", "#71DDF1", "perch-music"),
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
    private void Idle_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Idle);
    private void Talk_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Talking);
    private void Glitch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Glitching);
    private void Toss_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Tossed);
    private void Sleep_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Sleeping);
    private void Perch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.Perching);
    private void MusicPerch_Click(object sender, RoutedEventArgs e) => SetMood(CompanionMood.MusicPerch);
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Quit_Click(object sender, RoutedEventArgs e) => App.Instance.Shutdown();
}
