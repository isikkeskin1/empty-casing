using System.Collections.ObjectModel;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
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
    private string _activePage = "CHAT";

    public ChatWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeScreenOptions();
        InitializeWatchIntervals();
        RefreshBrainConfiguration();
        SyncCapabilitySwitches();
        SelectPage("CHAT");

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
    private void OpenUiInspector_Click(object sender, RoutedEventArgs e) => App.Instance.OpenUiInspector();
    private void OpenGames_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenGameCenter();
    private void OpenNowPlaying_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenNowPlaying();
    private void OpenCallCompanion_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenCallCompanion();
    private void OpenConnections_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenConnections();

    private void ListRoutines_Click(object sender, RoutedEventArgs e)
    {
        SelectPage("CHAT");
        AppendNotification("Try: gaming mode (Discord + Steam + Spotify), " +
            "Roblox mode (Discord + Roblox home + Spotify), " +
            "Roblox dev mode (Creator Dashboard + VS Code + Spotify), " +
            "study mode (VS Code + Notepad + Spotify). " +
            "Enable DESKTOP ACTIONS or HANDS-FREE first. Say 'Capri stop all' " +
            "to cancel remaining steps.");
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string section)
            SelectPage(section);
    }

    private void SelectPage(string section)
    {
        if (section is not ("CHAT" or "MODES" or "CONTROLS" or "CONNECT"))
            section = "CHAT";
        _activePage = section;
        ChatPage.Visibility = section == "CHAT" ? Visibility.Visible : Visibility.Collapsed;
        ModesPage.Visibility = section == "MODES" ? Visibility.Visible : Visibility.Collapsed;
        ControlsPage.Visibility = section == "CONTROLS" ? Visibility.Visible : Visibility.Collapsed;
        ConnectionsPage.Visibility = section == "CONNECT" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = section switch
        {
            "MODES" => "Start a session",
            "CONTROLS" => "Senses & control",
            "CONNECT" => "Apps & tools",
            _ => "Talk to Capri"
        };
        PageSubtitle.Text = section switch
        {
            "MODES" => "One click can prepare your whole desktop.",
            "CONTROLS" => "Manage voice, vision, actions and your AI models.",
            "CONNECT" => "Keep music, games and linked services within reach.",
            _ => "Ask a question, launch a task or just talk. Capri is here."
        };
        var regular = (Style)FindResource("CapriNavButton");
        var selected = (Style)FindResource("CapriNavActiveButton");
        NavChat.Style = section == "CHAT" ? selected : regular;
        NavModes.Style = section == "MODES" ? selected : regular;
        NavControls.Style = section == "CONTROLS" ? selected : regular;
        NavConnect.Style = section == "CONNECT" ? selected : regular;
    }

    private void LaunchRoutine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string command }) return;
        SelectPage("CHAT");
        _ = SendAsync(command);
    }

    private void GameHelp_Click(object sender, RoutedEventArgs e) =>
        StartGameHelp();

    public void StartGameHelp()
    {
        if (_busy) return;
        var text = PromptBox.Text.Trim();
        _ = SendAsync(string.IsNullOrWhiteSpace(text)
            ? "game help: What should I do next?" : "game help: " + text);
    }

    private void OpenAutoTask_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.OpenUiInspector();
        App.Instance.SetAutonomousTaskGoal(PromptBox.Text.Trim());
    }
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
        SelectPage("CHAT");
        Show();
        Activate();
        PromptBox.Focus();
    }

    public void SyncCapabilitySwitches()
    {
        _syncing = true;
        WatchCheck.IsChecked = App.Instance.Settings.WatchScreen;
        ContextCheck.IsChecked = App.Instance.Settings.EnableSituationalContext;
        PresenceCheck.IsChecked = App.Instance.LivePresenceEnabled;
        MicCheck.IsChecked = App.Instance.Settings.ListenToMicrophone;
        MicCheck.Content = App.Instance.MicrophoneMuted ? "MIC MUTED" : "MIC ON";
        TalkOnceButton.Content = App.Instance.TalkOnceArmed ? "LISTENING..." : "TALK ONCE";
        SpeakCheck.IsChecked = App.Instance.Settings.SpeakReplies;
        DanceCheck.IsChecked = App.Instance.Settings.DanceToSystemAudio;
        ActionsCheck.IsChecked = App.Instance.Settings.EnableDesktopActions;
        HandsFreeCheck.IsChecked = App.Instance.Settings.SkipActionConfirmations;
        HandsFreeCheck.Content = App.Instance.Settings.SkipActionConfirmations &&
            !App.Instance.Settings.EnableDesktopActions
                ? "Hands-free (disarmed)" : "Hands-free · no popups";
        QuickMicState.Text = App.Instance.MicrophoneMuted ? "MIC  /  MUTED" :
            App.Instance.Settings.ListenToMicrophone ? "MIC  /  ON" : "MIC  /  OFF";
        QuickSenseState.Text = App.Instance.Settings.WatchScreen ? "SCREEN  /  WATCHING" :
            App.Instance.LivePresenceEnabled ? "PRESENCE  /  LOCAL" :
            App.Instance.Settings.EnableSituationalContext ? "CONTEXT  /  ON" : "SENSE  /  OFF";
        QuickActionsState.Text = !App.Instance.Settings.EnableDesktopActions ?
            "ACTIONS  /  DISARMED" : App.Instance.Settings.SkipActionConfirmations ?
            "ACTIONS  /  HANDS-FREE" : "ACTIONS  /  CONFIRM";
        _syncing = false;
    }

    public void AppendNotification(string text) => Add("system", text);

    private void ShowReminders_Click(object sender, RoutedEventArgs e)
    {
        SelectPage("CHAT");
        AppendNotification(App.Instance.Reminders.Describe());
    }

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
        // File-derived content is a temporary result: do not persist it or
        // include it as a future model conversation turn.
        App.Instance.Store.SaveHistory(Messages.Where(x => x.Role != "file_result"));
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

    private void Context_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetSituationalContextEnabled(ContextCheck.IsChecked == true);
    }

    private void Presence_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetLivePresenceEnabled(PresenceCheck.IsChecked == true);
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

    private void Dance_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        if (DanceCheck.IsChecked == true &&
            System.Windows.MessageBox.Show(
                "DANCE TO AUDIO reads output volume levels from your Windows sound device, including calls or videos, while enabled. It does not save or upload audio. Enable for this session?",
                "Capri — system audio permission", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            SyncCapabilitySwitches();
            return;
        }
        try { App.Instance.SetDanceToSystemAudio(DanceCheck.IsChecked == true); }
        catch (Exception ex)
        {
            App.Instance.SetDanceToSystemAudio(false);
            SyncCapabilitySwitches();
            UpdateStatus("AUDIO DEVICE ERROR", Colors.OrangeRed);
            System.Windows.MessageBox.Show(ex.Message, "Capri system audio",
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
        App.Instance.SetDesktopActionsEnabled(ActionsCheck.IsChecked == true);
    }

    private void HandsFree_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetHandsFreeMode(HandsFreeCheck.IsChecked == true);
        UpdateStatus(HandsFreeCheck.IsChecked == true
            ? "HANDS-FREE ARMED — STOP ALL TO DISARM"
            : "MANUAL APPROVALS", Colors.LightGreen);
    }

    private void StopInputs_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.StopAllInputs();
        UpdateStatus("ALL INPUTS STOPPED", Colors.OrangeRed);
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
        SelectPage("CHAT");

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
            else if (!lookNow && GameHelpPolicy.IsGameHelpRequest(prompt))
                answer = await App.Instance.AskGameHelpAsync(prompt);
            else if (lookNow || (App.Instance.Settings.WatchScreen && RefersToScreen(prompt)))
            {
                if (!App.Instance.Brain.IsLocal && !App.Instance.Settings.WatchScreen && !lookNow)
                    throw new InvalidOperationException("Enable WATCH SCREEN to use cloud vision.");
                answer = await App.Instance.AskScreenAsync(prompt);
            }
            else
                answer = await App.Instance.TryHandleActionAsync(prompt)
                         ?? await App.Instance.ReplyWithAmbientContextAsync(Messages.ToArray());

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

    private async void ReadFile_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        var picker = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose one text or code file for Capri to review",
            Filter = "Text and code files|*.txt;*.md;*.csv;*.json;*.log;*.py;*.cs;*.js;*.ts;*.tsx;*.jsx;*.java;*.c;*.cpp;*.h;*.hpp;*.html;*.css;*.xml;*.yaml;*.yml;*.toml;*.ini;*.sql;*.rs;*.go",
            Multiselect = false,
            CheckFileExists = true
        };
        if (picker.ShowDialog(this) != true) return;

        _busy = true;
        SendButton.IsEnabled = false;
        PromptBox.IsEnabled = false;
        UpdateStatus("READING SELECTED FILE", Colors.LightSkyBlue);
        App.Instance.SetMood(CompanionMood.Thinking);
        try
        {
            var excerpt = await Task.Run(() =>
                LocalFileContextReader.ReadExplicitFile(picker.FileName));

            var task = PromptBox.Text.Trim();
            var provider = App.Instance.Brain.IsLocal
                ? "local Ollama" : "your selected cloud AI provider";
            if (!App.Instance.Brain.IsLocal &&
                System.Windows.MessageBox.Show(
                    "Share content from " + excerpt.FileName + " with " + provider +
                    "?\n\n" + excerpt.CharactersRead + " characters will be sent. " +
                    (excerpt.Truncated ? "Only the beginning of the file is included.\n\n" : "\n\n") +
                    "The file may contain personal data not detected by the credential scanner. " +
                    "Capri will not save the raw excerpt to chat history. " +
                    "File-derived answers are saved locally and will not be spoken aloud.",
                    "Capri — share file with cloud brain",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning,
                    MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                UpdateStatus("FILE SHARING CANCELLED", Colors.LightSkyBlue);
                return;
            }

            // This instruction is sent only as a chat question. It is never treated
            // as an action plan, and the local file path or full content is not saved.
            var question = LocalFileContextReader.BuildAnalysisPrompt(excerpt, task);
            Add("user", "Review selected file: " + excerpt.FileName +
                (string.IsNullOrWhiteSpace(task) ? "" : "\nQuestion: " + task) +
                (excerpt.Truncated ? "\n(First 12,000 characters)" : ""));
            PromptBox.Clear();
            var answer = await App.Instance.Brain.ReplyAsync(
                new[] { new ChatTurn { Role = "user", Text = question } });
            Add("file_result", answer);
            App.Instance.SetMood(CompanionMood.Reply);
            // File-derived answers are not auto-spoken: cloud TTS would transmit
            // those excerpts to an additional third-party provider.
            UpdateStatus("FILE REVIEW READY", Colors.LightGreen);
        }
        catch (Exception ex)
        {
            App.Instance.SetMood(CompanionMood.Error);
            Add("system", "File review stopped: " + ex.Message);
            UpdateStatus("FILE NOT READ", Colors.OrangeRed);
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
