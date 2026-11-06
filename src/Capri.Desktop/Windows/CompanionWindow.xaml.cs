using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CompanionWindow : Window
{
    private FrameAnimator? _frames;
    private CompanionMood _mood = CompanionMood.Idle;

    public CompanionWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (_, _) => _frames?.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = App.Instance.Settings;
        var area = SystemParameters.WorkArea;
        Left = settings.OverlayLeft is double left && double.IsFinite(left) &&
               left >= area.Left && left < area.Right - 50 ? left : area.Right - Width - 20;
        Top = settings.OverlayTop is double top && double.IsFinite(top) &&
              top >= area.Top && top < area.Bottom - 50 ? top : area.Bottom - Height - 25;

        var bob = new DoubleAnimation(0, -8, TimeSpan.FromSeconds(1.9))
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
            // The drag can end before Windows starts moving the overlay.
        }
    }

    public void SetMood(CompanionMood mood)
    {
        _mood = mood;
        var (name, brush) = mood switch
        {
            CompanionMood.Thinking => ("THINKING", "#C994FA"),
            CompanionMood.Reply => ("REPLY READY", "#7DEBD1"),
            CompanionMood.Error => ("ERROR", "#FE7995"),
            _ => ("IDLE", "#71DDF1")
        };
        StatusText.Text = "CAPRI // " + name;
        StatusLight.Fill = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(brush)!);
        _frames?.SetState(mood switch
        {
            CompanionMood.Thinking => "think",
            CompanionMood.Reply => "reply",
            CompanionMood.Error => "error",
            _ => "idle"
        });
    }

    public void ReloadArt()
    {
        _frames?.Reload();
        SetMood(_mood);
    }

    private void OpenChat_Click(object sender, RoutedEventArgs e) => App.Instance.ShowChat();
    private void OpenArt_Click(object sender, RoutedEventArgs e) => App.Instance.OpenCharacterFolder();
    private void ReloadArt_Click(object sender, RoutedEventArgs e) => ReloadArt();
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void Quit_Click(object sender, RoutedEventArgs e) => App.Instance.Shutdown();
}
