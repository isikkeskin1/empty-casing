using System.Windows;
using System.Speech.Recognition;
using System.Windows.Controls;
using System.Windows.Input;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class InputSettingsWindow : Window
{
    private CancellationTokenSource? _testCancellation;
    public InputSettingsWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        PopulateDevices();
        DisplayBindings(App.Instance.Settings.Shortcuts ?? new InputShortcuts());
        PettingCheck.IsChecked = App.Instance.Settings.EnablePetting;
        FollowUpsCheck.IsChecked = App.Instance.Settings.EnableVoiceFollowUps;
        VoiceInterruptCheck.IsChecked = App.Instance.Settings.AllowVoiceInterrupt;
        OpenChatForVoiceCheck.IsChecked = App.Instance.Settings.OpenChatForVoiceRequests;
        SpeakWhileGeneratingCheck.IsChecked = App.Instance.Settings.SpeakWhileGenerating;
        StatusText.Text = "Select microphone and output, then save. No device monitoring is active.";
        Closed += (_, _) => _testCancellation?.Cancel();
    }

    private void PopulateDevices()
    {
        foreach (var device in AudioInputDevices.Microphones())
            MicBox.Items.Add(new ComboBoxItem { Content = device.Name, Tag = device.Index });
        foreach (var device in AudioInputDevices.Speakers())
            SpeakerBox.Items.Add(new ComboBoxItem { Content = device.Name, Tag = device.Index });

        SpeechLanguageBox.Items.Add(new ComboBoxItem
        {
            Content = "Use first installed Windows speech recognizer", Tag = ""
        });
        try
        {
            foreach (var recognizer in SpeechRecognitionEngine.InstalledRecognizers())
                SpeechLanguageBox.Items.Add(new ComboBoxItem
                {
                    Content = recognizer.Culture.DisplayName +
                        " (" + recognizer.Culture.Name + ")",
                    Tag = recognizer.Culture.Name
                });
        }
        catch (Exception ex) when (ex is InvalidOperationException or
            PlatformNotSupportedException)
        {
            StatusText.Text = "Cannot enumerate Windows recognition languages.";
        }
        var language = App.Instance.Settings.SpeechRecognitionLanguage;
        SpeechLanguageBox.SelectedItem = SpeechLanguageBox.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag is string tag &&
                tag.Equals(language, StringComparison.OrdinalIgnoreCase))
            ?? SpeechLanguageBox.Items[0];

        PickIndex(MicBox, App.Instance.Settings.MicrophoneDeviceIndex);
        PickIndex(SpeakerBox, App.Instance.Settings.SpeakerDeviceIndex);
    }

    private static void PickIndex(ComboBox combo, int device)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (((ComboBoxItem)combo.Items[i]).Tag is int index && index == device)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private static int SelectedIndex(ComboBox box) =>
        (box.SelectedItem as ComboBoxItem)?.Tag is int value ? value : -1;

    private void DisplayBindings(InputShortcuts keys)
    {
        OpenChatKey.Text = keys.OpenChat;
        ToggleMicKey.Text = keys.ToggleMic;
        TalkOnceKey.Text = keys.TalkOnce;
        MuteMicKey.Text = keys.MuteMic;
        FocusTypingKey.Text = keys.FocusTyping;
        ToggleCompanionKey.Text = keys.ToggleCompanion;
        BringHereKey.Text = keys.BringHere;
        QuietModeKey.Text = keys.ToggleQuietMode;
        StopReplyKey.Text = keys.StopReply;
        StopVoiceKey.Text = keys.StopVoice;
    }

    private InputShortcuts ReadBindings() => new()
    {
        OpenChat = OpenChatKey.Text,
        ToggleMic = ToggleMicKey.Text,
        TalkOnce = TalkOnceKey.Text,
        MuteMic = MuteMicKey.Text,
        FocusTyping = FocusTypingKey.Text,
        ToggleCompanion = ToggleCompanionKey.Text,
        BringHere = BringHereKey.Text,
        ToggleQuietMode = QuietModeKey.Text,
        StopReply = StopReplyKey.Text,
        StopVoice = StopVoiceKey.Text
    };

    private void Shortcut_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (sender is not TextBox field) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.None) return;

        var modifier = Keyboard.Modifiers;
        if (!modifier.HasFlag(ModifierKeys.Control) ||
            !modifier.HasFlag(ModifierKeys.Alt) && !modifier.HasFlag(ModifierKeys.Shift))
        {
            StatusText.Text = "Use Ctrl+Alt or Ctrl+Shift plus a supported key.";
            return;
        }

        var keyText = key switch
        {
            >= Key.A and <= Key.Z => key.ToString().ToUpperInvariant(),
            >= Key.F1 and <= Key.F12 => key.ToString().ToUpperInvariant(),
            Key.Space => "Space",
            _ => ""
        };
        if (keyText == "") return;

        var label = "Ctrl+" +
            (modifier.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") +
            (modifier.HasFlag(ModifierKeys.Shift) ? "Shift+" : "") + keyText;
        if (GlobalShortcut.TryParse(label, out _))
            field.Text = label;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        DisplayBindings(new InputShortcuts());
        StatusText.Text = "Default keys restored in editor. Press SAVE to apply.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var bindings = ReadBindings();
            if (!InputShortcuts.TryValidate(bindings, out var reason))
                throw new ArgumentException(reason);
            App.Instance.Settings.SpeechRecognitionLanguage =
                (SpeechLanguageBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
            var result = App.Instance.ApplyInputSettings(bindings,
                SelectedIndex(MicBox), SelectedIndex(SpeakerBox));
            App.Instance.Settings.EnablePetting = PettingCheck.IsChecked == true;
            App.Instance.Settings.EnableVoiceFollowUps = FollowUpsCheck.IsChecked == true;
            App.Instance.Settings.AllowVoiceInterrupt = VoiceInterruptCheck.IsChecked == true;
            App.Instance.Settings.OpenChatForVoiceRequests = OpenChatForVoiceCheck.IsChecked == true;
            App.Instance.Settings.SpeakWhileGenerating = SpeakWhileGeneratingCheck.IsChecked == true;
            App.Instance.Store.SaveSettings(App.Instance.Settings);
            StatusText.Text = result.Length == 0
                ? "Input devices and all shortcuts saved and active."
                : "Saved, with conflicts: " + result;
        }
        catch (Exception ex) { StatusText.Text = "Save failed: " + ex.Message; }
    }

    private async void TestMic_Click(object sender, RoutedEventArgs e)
    {
        if (_testCancellation is not null) return;
        if (App.Instance.Settings.ListenToMicrophone)
        {
            StatusText.Text = "Turn off voice listening before testing a recording device.";
            return;
        }
        var tokenSource = new CancellationTokenSource();
        _testCancellation = tokenSource;
        TestMicButton.IsEnabled = false;
        MicLevel.Value = 0;
        StatusText.Text = "Listening for five seconds — speak into the chosen microphone.";
        try
        {
            var peak = await AudioInputDevices.TestMicrophoneAsync(
                SelectedIndex(MicBox), TimeSpan.FromSeconds(5),
                level => Dispatcher.BeginInvoke(() => MicLevel.Value = Math.Min(100, level * 250)),
                tokenSource.Token);
            StatusText.Text = peak < 0.01
                ? "No significant audio detected. Check device and Windows microphone permission."
                : "Microphone signal detected. Peak: " + (peak * 100).ToString("0") + "%.";
        }
        catch (OperationCanceledException) { StatusText.Text = "Microphone test stopped."; }
        catch (Exception ex) { StatusText.Text = "Cannot test microphone: " + ex.Message; }
        finally
        {
            TestMicButton.IsEnabled = true;
            tokenSource.Dispose();
            _testCancellation = null;
        }
    }

    private void StopInputs_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.StopAllInputs();
        StatusText.Text = "Stopped screen watching, mic capture, desktop actions and speech.";
    }
}
