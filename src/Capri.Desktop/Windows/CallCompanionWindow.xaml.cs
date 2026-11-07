using System.Windows;
using System.Windows.Controls;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CallCompanionWindow : Window
{
    public CallCompanionWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        foreach (var device in CallAudioEndpoints.Speakers())
            CallIncomingBox.Items.Add(new ComboBoxItem { Content = device.Name, Tag = device.Id });
        var chosen = App.Instance.Settings.CallIncomingSpeakerId;
        CallIncomingBox.SelectedItem = CallIncomingBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag is string id && id == chosen)
            ?? CallIncomingBox.Items.Cast<ComboBoxItem>().FirstOrDefault();
        var callOutput = App.Instance.CallVirtualSpeakerIndex;
        foreach (var device in AudioInputDevices.Speakers())
            CallOutgoingBox.Items.Add(new ComboBoxItem
            {
                Content = device.Name + (device.Index == -1 ? " (not valid for virtual mic)" : ""),
                Tag = device.Index
            });
        CallOutgoingBox.SelectedItem = CallOutgoingBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag is int index && index == callOutput)
            ?? CallOutgoingBox.Items.Cast<ComboBoxItem>().FirstOrDefault();
        IncludeRealMicCheck.IsChecked = App.Instance.Settings.CallIncludeRealMicrophone;
        Loaded += (_, _) => RefreshState();
        Closed += (_, _) => App.Instance.StopCallCompanionMode();
    }

    public void RefreshState()
    {
        var settings = App.Instance.Settings;
        var speakers = AudioInputDevices.Speakers();
        var selected = speakers.FirstOrDefault(x => x.Index == App.Instance.CallVirtualSpeakerIndex);
        var everyday = speakers.FirstOrDefault(x => x.Index == settings.SpeakerDeviceIndex);
        OutputDeviceText.Text = "CALL OUTGOING: " + (selected?.Name ?? "(not configured)") +
            "  •  NORMAL VOICE: " + (everyday?.Name ?? "Windows default");
        var active = App.Instance.CallCompanionActive;
        CallIncomingBox.IsEnabled = !active;
        CallOutgoingBox.IsEnabled = !active;
        IncludeRealMicCheck.IsEnabled = !active;
        StartButton.IsEnabled = !active;
        DiscordPresenceButton.Content = settings.EnableDiscordRichPresence
            ? "DISCORD STATUS OFF" : "DISCORD STATUS ON";
        StopButton.IsEnabled = active;
        if (active) StatusText.Text = "CALL MODE LIVE — Say 'Capri' before a question. STOP CALL MODE to revoke listening.";
    }

    public void SetStatus(string message)
    {
        RefreshState();
        StatusText.Text = message;
    }

    private void Inputs_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenInputSettings();

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (InformedCheck.IsChecked != true)
            {
                StatusText.Text = "Inform call participants and check the consent box before starting.";
                return;
            }
            App.Instance.ConfigureCallRouting(
                (CallIncomingBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "",
                (CallOutgoingBox.SelectedItem as ComboBoxItem)?.Tag is int output ? output : -1,
                IncludeRealMicCheck.IsChecked == true);
            StatusText.Text = App.Instance.StartCallCompanionMode(true);
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

    private async void DiscordPresence_Click(object sender, RoutedEventArgs e)
    {
        DiscordPresenceButton.IsEnabled = false;
        try
        {
            StatusText.Text = await App.Instance.SetDiscordPresenceEnabledAsync(
                !App.Instance.Settings.EnableDiscordRichPresence);
        }
        finally
        {
            DiscordPresenceButton.IsEnabled = true;
            DiscordPresenceButton.Content = App.Instance.Settings.EnableDiscordRichPresence
                ? "DISCORD STATUS OFF" : "DISCORD STATUS ON";
        }
    }

    private void DiscordMute_Click(object sender, RoutedEventArgs e) =>
        StatusText.Text = App.Instance.TryDiscordDesktopShortcut(
            DiscordDesktopShortcuts.Command.ToggleMute);

    private void DiscordDeafen_Click(object sender, RoutedEventArgs e) =>
        StatusText.Text = App.Instance.TryDiscordDesktopShortcut(
            DiscordDesktopShortcuts.Command.ToggleDeafen);

    private async void Discord_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = await App.Instance.TryHandleActionAsync("open discord") ??
            "Enable DESKTOP ACTIONS or HANDS-FREE to open Discord.";
    }
}
