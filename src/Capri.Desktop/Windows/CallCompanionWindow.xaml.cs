using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class CallCompanionWindow : Window
{
    private CancellationTokenSource? _outputTestCancellation;
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
        Closed += (_, _) =>
        {
            _outputTestCancellation?.Cancel();
            App.Instance.StopCallCompanionMode();
        };
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
        TestVirtualButton.IsEnabled = !active && _outputTestCancellation is null;
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

    private void GetVirtualCable_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(
                "https://vb-audio.com/Cable/index.htm") { UseShellExecute = true });
            StatusText.Text = "Official VB-Audio driver page opened. " +
                "Install from the provider, reboot if requested, then reopen CALL COMPANION.";
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or
            InvalidOperationException)
        {
            StatusText.Text = "Could not open the VB-Audio website. " +
                "Visit vb-audio.com/Cable/ in your browser.";
        }
    }

    private async void TestVirtual_Click(object sender, RoutedEventArgs e)
    {
        if (_outputTestCancellation is not null) return;
        if (App.Instance.CallCompanionActive)
        {
            StatusText.Text = "Stop Call Mode before the virtual microphone routing test.";
            return;
        }
        if (InformedCheck.IsChecked != true)
        {
            StatusText.Text = "Tick CALL CONSENT before sending a test sound " +
                "into a device that Discord may transmit to other participants.";
            return;
        }
        if ((CallOutgoingBox.SelectedItem as ComboBoxItem)?.Tag is not int index || index < 0)
        {
            StatusText.Text = "Choose CABLE Input (a non-default virtual playback device) first.";
            return;
        }
        var test = new CancellationTokenSource();
        _outputTestCancellation = test;
        TestVirtualButton.IsEnabled = false;
        try
        {
            StatusText.Text = "Playing two quiet test tones to the selected cable. " +
                "Check Discord's microphone input meter (CABLE Output).";
            await CallAudioRoutingTester.TestVirtualOutputAsync(index, test.Token);
            if (!test.IsCancellationRequested)
                StatusText.Text = "Test tone finished. If Discord's microphone meter " +
                    "did not move, choose its CABLE Output input and check input sensitivity.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Output test cancelled.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Virtual cable test failed: " + ex.GetType().Name +
                ". Check your playback device and driver installation.";
        }
        finally
        {
            if (ReferenceEquals(_outputTestCancellation, test))
                _outputTestCancellation = null;
            test.Dispose();
            if (IsVisible) RefreshState();
        }
    }

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
