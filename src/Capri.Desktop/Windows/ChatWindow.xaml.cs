using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class ChatWindow : Window
{
    public ObservableCollection<ChatTurn> Messages { get; } = new();
    private bool _busy;
    private bool _syncing;

    public ChatWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeScreenOptions();
        InitializeWatchIntervals();
        RefreshBrainConfiguration();
        SyncCapabilitySwitches();

        foreach (var turn in App.Instance.Store.LoadHistory())
            Messages.Add(turn);

        if (Messages.Count == 0)
            Add("assistant", "Hey, choom. Want to actually see what I'm looking at? " +
                "Press LOOK NOW. Turn on the mic to talk to me, or opt into screen reactions. " +
                "Nothing buys, deletes or sends messages through my desktop controls.");

        Loaded += async (_, _) => await CheckBrainAsync();
    }

    private void InitializeScreenOptions()
    {
        _syncing = true;
        ScreenCombo.Items.Add(new ComboBoxItem { Content = "Primary display", Tag = -1 });
        ScreenCombo.Items.Add(new ComboBoxItem { Content = "Follow frontmost app", Tag = -2 });
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            var monitor = screens[i];
            ScreenCombo.Items.Add(new ComboBoxItem
            {
                Tag = i,
                Content = "Monitor " + (i + 1) + " — " + monitor.DeviceName +
                          " (" + monitor.Bounds.Width + "x" + monitor.Bounds.Height + ")"
            });
        }
        var wanted = App.Instance.Settings.ScreenIndex;
        ScreenCombo.SelectedIndex = wanted == -2 ? 1 :
            wanted >= 0 && wanted < screens.Length ? wanted + 2 : 0;
        _syncing = false;
    }

    private void InitializeWatchIntervals()
    {
        _syncing = true;
        foreach (var seconds in new[] { 10, 30, 45, 60, 120, 300, 600 })
            WatchIntervalBox.Items.Add(new ComboBoxItem
            {
                Content = seconds < 60 ? seconds + " seconds" : seconds / 60 + " minutes",
                Tag = seconds
            });
        for (var i = 0; i < WatchIntervalBox.Items.Count; i++)
            if (((ComboBoxItem)WatchIntervalBox.Items[i]).Tag is int value &&
                value == App.Instance.Settings.WatchIntervalSeconds)
                WatchIntervalBox.SelectedIndex = i;
        if (WatchIntervalBox.SelectedIndex < 0) WatchIntervalBox.SelectedIndex = 2;
        _syncing = false;
    }

    private void WatchInterval_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || (WatchIntervalBox.SelectedItem as ComboBoxItem)?.Tag is not int seconds)
            return;
        App.Instance.SetWatchIntervalSeconds(seconds);
    }

    private void Screen_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if ((ScreenCombo.SelectedItem as ComboBoxItem)?.Tag is not int index) return;
        App.Instance.Settings.ScreenIndex = index;
        App.Instance.Store.SaveSettings(App.Instance.Settings);
    }

    public void RefreshBrainConfiguration()
    {
        var settings = App.Instance.Settings;
        ModelBox.Text = App.Instance.Brain.IsLocal ? settings.OllamaModel : settings.CloudTextModel;
        VisionModelBox.Text = App.Instance.Brain.IsLocal ? settings.VisionModel : settings.CloudVisionModel;
        _ = CheckBrainAsync();
    }

    private void SetupBrain_Click(object sender, RoutedEventArgs e) => App.Instance.OpenSetup();
    private void OpenInputs_Click(object sender, RoutedEventArgs e) => App.Instance.OpenInputSettings();
    private void TalkOnce_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Instance.StartTalkOnce();
            UpdateStatus("LISTENING ONCE", Colors.LightGreen);
        }
        catch (Exception ex)
        {
            UpdateStatus("MIC UNAVAILABLE", Colors.OrangeRed);
            System.Windows.MessageBox.Show(ex.Message, "Talk Once", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private void StopVoice_Click(object sender, RoutedEventArgs e) => App.Instance.StopVoiceOutput();
    public void FocusPrompt()
    {
        Show();
        Activate();
        PromptBox.Focus();
    }

    public void SyncCapabilitySwitches()
    {
        _syncing = true;
        WatchCheck.IsChecked = App.Instance.Settings.WatchScreen;
        MicCheck.IsChecked = App.Instance.Settings.ListenToMicrophone;
        MicCheck.Content = App.Instance.MicrophoneMuted ? "MIC MUTED" : "MIC ON";
        TalkOnceButton.Content = App.Instance.TalkOnceArmed ? "LISTENING..." : "TALK ONCE";
        SpeakCheck.IsChecked = App.Instance.Settings.SpeakReplies;
        ActionsCheck.IsChecked = App.Instance.Settings.EnableDesktopActions;
        _syncing = false;
    }

    public void AppendNotification(string text) => Add("system", text);

    private void ShowReminders_Click(object sender, RoutedEventArgs e) =>
        AppendNotification(App.Instance.Reminders.Describe());

    public void SubmitVoiceText(string recognized)
    {
        if (_busy) return;
        _ = SendAsync(recognized);
    }

    private void Add(string role, string text)
    {
        Messages.Add(new ChatTurn
        {
            Role = role,
            Text = text,
            Timestamp = DateTimeOffset.Now
        });
        while (Messages.Count > 120) Messages.RemoveAt(0);
        App.Instance.Store.SaveHistory(Messages);
        Dispatcher.BeginInvoke(() => TranscriptScroll.ScrollToEnd());
    }

    private async Task CheckBrainAsync()
    {
        var available = await App.Instance.Brain.IsAvailableAsync();
        UpdateStatus(available
            ? (App.Instance.Brain.IsLocal ? "LOCAL BRAIN ONLINE" : "CLOUD KEY READY")
            : (App.Instance.Brain.IsLocal ? "OLLAMA OFFLINE" : "CLOUD KEY MISSING"),
            available ? Colors.LightGreen : Colors.Orange);
    }

    private void UpdateStatus(string text, System.Windows.Media.Color color)
    {
        StatusDisplay.Text = text;
        StatusDisplay.Foreground = new SolidColorBrush(color);
    }

    private void SaveModel_Click(object sender, RoutedEventArgs e)
    {
        var selected = ModelBox.Text.Trim();
        var vision = VisionModelBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(selected) ||
            (App.Instance.Brain.IsLocal && string.IsNullOrWhiteSpace(vision)))
        {
            UpdateStatus("SET REQUIRED MODEL NAMES", Colors.Orange);
            return;
        }
        if (App.Instance.Brain.IsLocal)
        {
            App.Instance.Settings.OllamaModel = selected;
            App.Instance.Settings.VisionModel = vision;
        }
        else
        {
            App.Instance.Settings.CloudTextModel = selected;
            App.Instance.Settings.CloudVisionModel = vision;
        }
        App.Instance.Store.SaveSettings(App.Instance.Settings);
        UpdateStatus("MODELS SAVED", Colors.LightSkyBlue);
    }

    private void Watch_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        if (WatchCheck.IsChecked == true && !App.Instance.Brain.IsLocal &&
            System.Windows.MessageBox.Show(
                "WATCH SCREEN will send repeated screenshots to your selected cloud AI provider. Allow this session?",
                "Capri — cloud screenshot permission", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            SyncCapabilitySwitches();
            return;
        }
        App.Instance.SetWatchingEnabled(WatchCheck.IsChecked == true);
    }

    private void Mic_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        try
        {
            App.Instance.SetMicrophoneEnabled(MicCheck.IsChecked == true);
            UpdateStatus(MicCheck.IsChecked == true ? "MIC ON: SAY CAPRI" : "MIC OFF",
                MicCheck.IsChecked == true ? Colors.LightGreen : Colors.LightSkyBlue);
        }
        catch (Exception exception)
        {
            App.Instance.SetMicrophoneEnabled(false);
            SyncCapabilitySwitches();
            UpdateStatus("MIC UNAVAILABLE", Colors.OrangeRed);
            System.Windows.MessageBox.Show(exception.Message,
                "Windows microphone recognition", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Speak_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.Settings.SpeakReplies = SpeakCheck.IsChecked == true;
        App.Instance.Store.SaveSettings(App.Instance.Settings);
    }

    private void Actions_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.Settings.EnableDesktopActions = ActionsCheck.IsChecked == true;
        App.Instance.Store.SaveSettings(App.Instance.Settings);
    }

    private void Prompt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;
            _ = SendAsync();
        }
    }

    private void Send_Click(object sender, RoutedEventArgs e) => _ = SendAsync();
    private void Look_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Instance.Brain.IsLocal &&
            System.Windows.MessageBox.Show(
                "Send one screenshot to your selected cloud AI provider?",
                "Capri — share screenshot", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        _ = SendAsync(
            string.IsNullOrWhiteSpace(PromptBox.Text) ? "What's happening on my screen?" : null, true);
    }

    private static bool RefersToScreen(string text)
    {
        return new[] { "screen", "screenshot", "what am i playing", "what is this",
                       "what's this", "look at", "can you see", "on my monitor" }
            .Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SendAsync(string? voiceOrDefaultPrompt = null, bool lookNow = false)
    {
        if (_busy) return;
        var prompt = (voiceOrDefaultPrompt ?? PromptBox.Text).Trim();
        if (prompt.Length == 0) return;

        _busy = true;
        SendButton.IsEnabled = false;
        PromptBox.IsEnabled = false;
        PromptBox.Clear();
        Add("user", prompt);
        App.Instance.SetMood(CompanionMood.Thinking);
        UpdateStatus(lookNow ? "LOOKING AT SCREEN" : "THINKING", Colors.Plum);

        try
        {
            string answer;
            var reminder = ReminderParser.Parse(prompt);
            if (!lookNow && (prompt.Equals("what did you see?", StringComparison.OrdinalIgnoreCase) ||
                prompt.Equals("what did you see", StringComparison.OrdinalIgnoreCase) ||
                prompt.Equals("what were you watching", StringComparison.OrdinalIgnoreCase)))
                answer = App.Instance.RecentScreenObservation ??
                    "I don't have a recent screen observation. Turn on WATCH SCREEN or press LOOK NOW.";
            else if (!lookNow && reminder is not null)
                answer = App.Instance.Reminders.Execute(reminder);
            else if (lookNow || (App.Instance.Settings.WatchScreen && RefersToScreen(prompt)))
            {
                if (!App.Instance.Brain.IsLocal && !App.Instance.Settings.WatchScreen && !lookNow)
                    throw new InvalidOperationException("Enable WATCH SCREEN to use cloud vision.");
                answer = await App.Instance.AskScreenAsync(prompt);
            }
            else
                answer = await App.Instance.TryHandleActionAsync(prompt)
                         ?? await App.Instance.Brain.ReplyAsync(Messages.ToArray());

            Add("assistant", answer);
            App.Instance.SetMood(CompanionMood.Reply);
            App.Instance.SpeakIfEnabled(answer);
            UpdateStatus("READY", Colors.LightGreen);
        }
        catch (Exception exception)
        {
            App.Instance.SetMood(CompanionMood.Error);
            Add("system", "Request failed: " + exception.Message +
                "\nCheck your selected model, local Ollama status, or cloud key and quota.");
            UpdateStatus("REQUEST ERROR", Colors.OrangeRed);
        }
        finally
        {
            _busy = false;
            SendButton.IsEnabled = true;
            PromptBox.IsEnabled = true;
            PromptBox.Focus();
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Delete your locally saved conversation?",
                "Capri Desktop", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        Messages.Clear();
        App.Instance.Store.SaveHistory(Messages);
        App.Instance.SetMood(CompanionMood.Idle);
    }
}
