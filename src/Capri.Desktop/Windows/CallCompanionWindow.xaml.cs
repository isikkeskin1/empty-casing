using System.Windows;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CallCompanionWindow : Window
{
    public CallCompanionWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        Loaded += (_, _) => RefreshState();
        Closed += (_, _) => App.Instance.StopCallCompanionMode();
    }

    public void RefreshState()
    {
        var settings = App.Instance.Settings;
        var speakers = AudioInputDevices.Speakers();
        var selected = speakers.FirstOrDefault(x => x.Index == settings.SpeakerDeviceIndex);
        OutputDeviceText.Text = "OUTGOING: " + (selected?.Name ?? "(no configured output)") +
            " (selected Fish voice playback device)";
        var active = App.Instance.CallCompanionActive;
        StartButton.IsEnabled = !active;
        StopButton.IsEnabled = active;
        if (active) StatusText.Text = "CALL MODE LIVE — Say 'Capri' before a question. STOP CALL MODE to revoke listening.";
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
        RefreshState();
    }

    private void Inputs_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenInputSettings();

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = App.Instance.StartCallCompanionMode(InformedCheck.IsChecked == true);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Cannot start: " + ex.Message;
        }
        RefreshState();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.StopCallCompanionMode();
        StatusText.Text = "Call Mode OFF. All call audio capture stopped.";
        RefreshState();
    }

    private async void Discord_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = await App.Instance.TryHandleActionAsync("open discord") ??
            "Enable DESKTOP ACTIONS or HANDS-FREE to open Discord.";
    }
}
