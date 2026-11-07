using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class ChatWindow : Window
{
    public ObservableCollection<ChatTurn> Messages { get; } = new();
    private bool _busy;
    private CancellationTokenSource? _activeReplyCts;
    private string? _queuedVoicePrompt;
    private bool _syncing;
    private string _activePage = "CHAT";
    private FrameAnimator? _heroAnimator;
    private CompanionMood _heroMood = CompanionMood.Idle;

    public ChatWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeScreenOptions();
        InitializeWatchIntervals();
        RefreshBrainConfiguration();
        ObsPortBox.Text = App.Instance.Settings.ObsWebSocketPort.ToString();
        SyncCapabilitySwitches();
        RefreshMemoryVault();
        SelectPage("CHAT");

        foreach (var turn in App.Instance.Store.LoadHistory())
            Messages.Add(turn);

        if (Messages.Count == 0)
            Add("assistant", "Hey, choom. I'm right here. Ask me anything, or try one of the ideas below.");
        Messages.CollectionChanged += (_, _) => RefreshStarterPanel();
        RefreshStarterPanel();

        Loaded += async (_, _) =>
        {
            EnsureHeroArt();
            await CheckBrainAsync();
        };
        Closed += (_, _) => _heroAnimator?.Dispose();
    }

    private void EnsureHeroArt()
    {
        if (_heroAnimator is not null) return;
        _heroAnimator = new FrameAnimator(App.Instance.Store.CharacterFolder, frame =>
        {
            HeroArtImage.Source = frame;
            HeroArtStatus.Visibility = frame is null
                ? Visibility.Visible : Visibility.Collapsed;
            HeroArtStatus.Text = frame is null
                ? "NO DEFAULT ANIMATION • IMPORT A PRIVATE PACK"
                : "DEFAULT CHARACTER PLAYING";
        });
        SetHeroMood(_heroMood);
    }

    private void RefreshStarterPanel()
    {
        StarterPanel.Visibility = Messages.Any(x => x.Role == "user")
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SuggestedPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string prompt })
            _ = SendAsync(prompt);
    }

    private void StarterGameHelp_Click(object sender, RoutedEventArgs e) =>
        _ = SendAsync("game help: What should I do next?");

    public void SetHeroMood(CompanionMood mood)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => SetHeroMood(mood));
            return;
        }
        if (_heroMood == mood && _heroAnimator is not null &&
            HeroMoodTitle.Text != "HERE WITH YOU")
            return;
        _heroMood = mood;
        var (label, caption, state, color) = mood switch
        {
            CompanionMood.Thinking => ("THINKING",
                "Let me work through that. I'm on it.", "idle", "#F8CF61"),
            CompanionMood.Talking => ("SPEAKING",
                "I'm talking. You can interrupt me anytime.", "talk", "#FF77AE"),
            CompanionMood.Reply => ("REPLY READY",
                "There you go. Want to keep going?", "talk", "#53E5DC"),
            CompanionMood.Error => ("NEEDS ATTENTION",
                "Something went wrong. Check the conversation for details.", "glitch", "#FF7296"),
            CompanionMood.Glitching => ("GLITCHING",
                "A little technical turbulence. I'm still here.", "glitch", "#CBA3FF"),
            CompanionMood.Tossed => ("ON THE MOVE",
                "Just moving around your desktop.", "tossed", "#F8CF61"),
            CompanionMood.Sleeping => ("RESTING",
                "Keeping quiet. I'll be here when you need me.", "sleep", "#91A7C2"),
            CompanionMood.Perching => ("PERCHED",
                "Hanging out at the edge of your window.", "perch", "#53E5DC"),
            CompanionMood.MusicPerch => ("VIBING",
                "Hanging out with your music.", "perch-music", "#A3FF7F"),
            _ => ("HERE WITH YOU",
                "Ready to hang out, help with a game or get something done.", "idle", "#A3FF7F")
        };
        HeroMoodTitle.Text = label;
        HeroMoodCaption.Text = caption;
        HeroMoodDot.Fill = new SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)!);
        _heroAnimator?.SetState(state);
    }

    public void ReloadHeroArt()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(ReloadHeroArt);
            return;
        }
        EnsureHeroArt();
        _heroAnimator?.Reload();
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
    private void OpenUpdates_Click(object sender, RoutedEventArgs e) => App.Instance.OpenUpdatesCenter();
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

    private void SketchMode_Click(object sender, RoutedEventArgs e)
    {
        var result = App.Instance.OpenScreenSketch();
        if (result.StartsWith("Screen Sketch active", StringComparison.Ordinal))
            Hide();
        else
            AppendNotification(result);
    }

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
        if (section is not ("CHAT" or "MODES" or "CONTROLS" or "CONNECT" or "MEMORY"))
            section = "CHAT";
        _activePage = section;
        ChatPage.Visibility = section == "CHAT" ? Visibility.Visible : Visibility.Collapsed;
        ModesPage.Visibility = section == "MODES" ? Visibility.Visible : Visibility.Collapsed;
        ControlsPage.Visibility = section == "CONTROLS" ? Visibility.Visible : Visibility.Collapsed;
        ConnectionsPage.Visibility = section == "CONNECT" ? Visibility.Visible : Visibility.Collapsed;
        MemoryPage.Visibility = section == "MEMORY" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = section switch
        {
            "MODES" => "Start a session",
            "CONTROLS" => "Senses & control",
            "CONNECT" => "Apps & tools",
            "MEMORY" => "My Memory Vault",
            _ => "At your side"
        };
        PageSubtitle.Text = section switch
        {
            "MODES" => "One click can prepare your whole desktop.",
            "CONTROLS" => "Manage voice, vision, actions and your AI models.",
            "CONNECT" => "Keep music, games and linked services within reach.",
            "MEMORY" => "Your notes, your control. Nothing is remembered automatically.",
            _ => "Your conversation, your projects, your pace."
        };
        var regular = (Style)FindResource("CapriNavButton");
        var selected = (Style)FindResource("CapriNavActiveButton");
        NavChat.Style = section == "CHAT" ? selected : regular;
        NavModes.Style = section == "MODES" ? selected : regular;
        NavControls.Style = section == "CONTROLS" ? selected : regular;
        NavConnect.Style = section == "CONNECT" ? selected : regular;
        NavMemory.Style = section == "MEMORY" ? selected : regular;
        if (section == "MEMORY") RefreshMemoryVault();
        if (section == "CHAT")
            Dispatcher.BeginInvoke(() => TranscriptScroll.ScrollToEnd());
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
        TalkOnceButton.Content = App.Instance.TalkOnceArmed ? "●  Listening…" : "◉  Talk once";
        SpeakCheck.IsChecked = App.Instance.Settings.SpeakReplies;
        QuietCheck.IsChecked = App.Instance.QuietModeEnabled;
        HuskWatchCheck.IsChecked = App.Instance.CodeHusk.Enabled;
        ObsArmedCheck.IsChecked = App.Instance.ObsClipsEnabled;
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
        UpdateLookoutStatus();
        RefreshHuskStatus();
        RefreshObsClipStatus();
    }

    public void RefreshMemoryVault()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshMemoryVault);
            return;
        }
        var selectedId = (MemoryList.SelectedItem as PersonalMemory)?.Id;
        var wasSyncing = _syncing;
        _syncing = true;
        try
        {
            MemoryList.ItemsSource = App.Instance.Memories.Items.ToArray();
            if (selectedId is Guid id)
                MemoryList.SelectedItem = App.Instance.Memories.Items
                    .FirstOrDefault(n => n.Id == id);
            UseMemoryCheck.IsChecked = App.Instance.Settings.UsePersonalMemoryInReplies;
            var hasConsent = App.Instance.Brain.IsLocal ||
                App.Instance.Settings.MemoryCloudProviderConsent ==
                App.Instance.Settings.BrainProvider;
            MemorySharingStatus.Text = !App.Instance.Settings.UsePersonalMemoryInReplies
                ? "Notes are LOCAL ONLY · context sharing off"
                : hasConsent
                    ? (App.Instance.Brain.IsLocal
                        ? "LOCAL BRAIN · selected notes included in chat requests"
                        : "CLOUD SHARING ON · " + App.Instance.Settings.BrainProvider)
                    : "PAUSED · approve sharing for this cloud model provider";
            MemoryCountText.Text = App.Instance.Memories.Items.Count +
                " / " + PersonalMemoryPolicy.MaxItems + " saved facts";
        }
        finally { _syncing = wasSyncing; }
    }

    private void MemorySelected_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing) return;
        if (MemoryList.SelectedItem is PersonalMemory memory)
            MemoryNoteBox.Text = memory.Text;
    }

    private void UseMemory_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        var enable = UseMemoryCheck.IsChecked == true;
        var approveCloud = false;
        if (enable && !App.Instance.Brain.IsLocal &&
            App.Instance.Settings.MemoryCloudProviderConsent !=
                App.Instance.Settings.BrainProvider)
        {
            var answer = System.Windows.MessageBox.Show(
                "Include your private saved notes in chat prompts sent to the selected " +
                "cloud AI provider (" + App.Instance.Settings.BrainProvider + ")?\n\n" +
                "Saved notes remain on your computer but copies of selected notes " +
                "leave your PC when used as model context. Changing AI providers " +
                "requires separate approval.\n\nEnable sharing?",
                "Capri — share memory with cloud AI?",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer != MessageBoxResult.Yes)
            {
                RefreshMemoryVault();
                return;
            }
            approveCloud = true;
        }
        try
        {
            App.Instance.SetMemoryContextEnabled(enable, approveCloud);
            MemoryFeedbackText.Text = enable
                ? "Memory context enabled. It is sent only with future normal AI replies."
                : "Memory context disabled. Your notes are still saved locally.";
        }
        catch (Exception ex)
        {
            MemoryFeedbackText.Text = "Memory sharing unchanged: " + ex.Message;
        }
        RefreshMemoryVault();
    }

    private void MemoryAdd_Click(object sender, RoutedEventArgs e)
    {
        MemoryFeedbackText.Text = App.Instance.Memories.Add(MemoryNoteBox.Text);
        RefreshMemoryVault();
        if (MemoryFeedbackText.Text.Contains("saved locally", StringComparison.Ordinal))
            MemoryNoteBox.Clear();
    }

    private void MemoryEdit_Click(object sender, RoutedEventArgs e)
    {
        if (MemoryList.SelectedItem is not PersonalMemory memory)
        {
            MemoryFeedbackText.Text = "Select a saved note first.";
            return;
        }
        MemoryFeedbackText.Text = App.Instance.Memories.Edit(memory.Id, MemoryNoteBox.Text);
        RefreshMemoryVault();
    }

    private void MemoryDelete_Click(object sender, RoutedEventArgs e)
    {
        if (MemoryList.SelectedItem is not PersonalMemory memory)
        {
            MemoryFeedbackText.Text = "Select a saved note first.";
            return;
        }
        MemoryFeedbackText.Text = App.Instance.Memories.Forget(memory.Id);
        MemoryNoteBox.Clear();
        RefreshMemoryVault();
    }

    private void MemoryClear_Click(object sender, RoutedEventArgs e)
    {
        if (App.Instance.Memories.Items.Count == 0) return;
        if (System.Windows.MessageBox.Show(
                "Permanently delete ALL saved notes from this Capri installation? " +
                "This does not delete conversation history or any copies previously " +
                "sent to a cloud AI provider.",
                "Capri — forget everything?",
                MessageBoxButton.YesNo, MessageBoxImage.Warning,
                MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        App.Instance.Memories.Clear();
        MemoryNoteBox.Clear();
        MemoryFeedbackText.Text = "All saved notes deleted from this PC.";
        RefreshMemoryVault();
    }

    private void MemoryExport_Click(object sender, RoutedEventArgs e)
    {
        if (App.Instance.Memories.Items.Count == 0)
        {
            MemoryFeedbackText.Text = "No notes to export.";
            return;
        }
        var chooser = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export your Capri personal notes",
            FileName = "Capri-Memories.txt",
            DefaultExt = ".txt",
            Filter = "Plain text|*.txt",
            AddExtension = true
        };
        if (chooser.ShowDialog(this) != true) return;
        try
        {
            System.IO.File.WriteAllText(chooser.FileName,
                App.Instance.Memories.ListSummary());
            MemoryFeedbackText.Text = "Notes exported to the file you selected.";
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            MemoryFeedbackText.Text = "Export failed: " + ex.GetType().Name;
        }
    }

    public void RefreshObsClipStatus()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshObsClipStatus);
            return;
        }
        ObsClipStatusText.Text = App.Instance.ObsClipStatus +
            (App.Instance.ObsPasswordSaved ? " · password saved" : " · no password");
    }

    private void ObsSaveCredentials_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ObsPortBox.Text.Trim(), out var port))
        {
            ObsClipFeedbackText.Text = "Enter the OBS WebSocket port (normally 4455).";
            return;
        }
        try
        {
            var password = ObsPasswordBox.Password;
            App.Instance.ConfigureObs(port, password.Length == 0 ? null : password);
            ObsPasswordBox.Clear();
            RefreshObsClipStatus();
            ObsClipFeedbackText.Text = App.Instance.ObsPasswordSaved
                ? "Connection settings saved locally. Enable clip controls for this session."
                : "Local port saved. Enter your OBS WebSocket password to enable clips.";
        }
        catch (Exception ex)
        {
            ObsClipFeedbackText.Text = "Could not save OBS settings: " + ex.Message;
        }
    }

    private void ObsForgetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(
                "Remove the saved OBS password and disarm clip controls?",
                "Capri — OBS clips", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        App.Instance.ForgetObsPassword();
        ObsPasswordBox.Clear();
        ObsClipFeedbackText.Text = "Saved OBS password removed from Capri.";
        SyncCapabilitySwitches();
    }

    private void ObsArmed_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        try
        {
            App.Instance.SetObsClipsEnabled(ObsArmedCheck.IsChecked == true);
            ObsClipFeedbackText.Text = ObsArmedCheck.IsChecked == true
                ? "Clip controls ready. Start OBS Replay Buffer, then say 'Capri, clip that'."
                : "OBS clip controls disarmed. OBS's own replay state is unchanged.";
        }
        catch (Exception ex)
        {
            _syncing = true;
            ObsArmedCheck.IsChecked = false;
            _syncing = false;
            ObsClipFeedbackText.Text = ex.Message;
        }
        RefreshObsClipStatus();
    }

    private async void ObsReplay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string command } btn) return;
        btn.IsEnabled = false;
        try
        {
            ObsClipFeedbackText.Text = "Contacting local OBS…";
            ObsClipFeedbackText.Text = await App.Instance.RunObsReplayAsync(command);
            RefreshObsClipStatus();
        }
        finally { btn.IsEnabled = true; }
    }

    private void ObsShowClip_Click(object sender, RoutedEventArgs e) =>
        ObsClipFeedbackText.Text = App.Instance.OpenLastObsClip();

    public void RefreshHuskStatus()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshHuskStatus);
            return;
        }
        HuskStatusText.Text = App.Instance.CodeHusk.Status;
        var recent = App.Instance.CodeHusk.Recent;
        HuskRecentText.Text = recent.Count == 0
            ? "No coding milestones in this session."
            : string.Join("\n", recent.TakeLast(8).Reverse().Select(evt =>
                evt.At.ToLocalTime().ToString("HH:mm:ss") + "   " + evt.FriendlyDescription));
    }

    private void HuskWatch_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        try
        {
            App.Instance.SetCodeHuskEnabled(HuskWatchCheck.IsChecked == true);
        }
        catch (Exception ex)
        {
            _syncing = true;
            HuskWatchCheck.IsChecked = false;
            _syncing = false;
            System.Windows.MessageBox.Show("Could not start Code/Husk: " + ex.GetType().Name,
                "Capri — Code/Husk", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        RefreshHuskStatus();
    }

    private void HuskInstall_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string agent }) return;
        var config = App.Instance.HuskHooks.ConfigPath(agent);
        var answer = System.Windows.MessageBox.Show(
            "Install Capri's read-only " + agent + " hooks?\n\n" +
            "This edits:\n" + config + "\n\n" +
            "A timestamped backup is created first. The hooks run a small, local " +
            "PowerShell relay when the coding assistant reaches milestones. " +
            "The relay discards prompts, code, outputs and paths. It writes " +
            "event names only while Code/Husk is enabled in Capri.\n\n" +
            "No approvals or commands are executed by Capri. You can remove " +
            "only Capri's hooks using the adjacent button.\n\nContinue?",
            "Capri — review coding hooks", MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        try
        {
            var result = App.Instance.HuskHooks.Install(agent);
            SelectPage("CHAT");
            AppendNotification(result);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Hook install failed: " + ex.Message +
                "\nNo existing config is intentionally deleted.",
                "Capri — Code/Husk", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void HuskRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string agent }) return;
        if (System.Windows.MessageBox.Show(
                "Remove only Capri's " + agent + " hook entries? " +
                "Other hooks and settings remain unchanged. A backup is saved.",
                "Capri — remove coding hooks", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        try
        {
            var result = App.Instance.HuskHooks.Uninstall(agent);
            SelectPage("CHAT");
            AppendNotification(result);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Hook removal failed: " + ex.Message,
                "Capri — Code/Husk", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void UpdateLookoutStatus()
    {
        LookoutStatusText.Text = App.Instance.LookoutStatus;
    }

    private void StartLookout_Click(object sender, RoutedEventArgs e)
    {
        var result = App.Instance.ArmScreenLookout(LookoutConditionBox.Text);
        UpdateLookoutStatus();
        UpdateStatus(result.StartsWith("Visual lookout armed", StringComparison.Ordinal)
            ? "LOOKOUT ARMED" : "WATCH SCREEN REQUIRED",
            result.StartsWith("Visual lookout armed", StringComparison.Ordinal)
                ? Colors.LightGreen : Colors.Orange);
        if (result.StartsWith("Visual lookout armed", StringComparison.Ordinal))
            App.Instance.SetMood(CompanionMood.Reply);
        else
            System.Windows.MessageBox.Show(result, "Capri — visual lookout",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CancelLookout_Click(object sender, RoutedEventArgs e)
    {
        var result = App.Instance.CancelScreenLookout();
        UpdateLookoutStatus();
        UpdateStatus("LOOKOUT STOPPED", Colors.LightSkyBlue);
        AppendNotification(result);
    }

    private void Quiet_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetQuietMode(QuietCheck.IsChecked == true);
        UpdateStatus(QuietCheck.IsChecked == true ? "QUIET MODE" : "READY",
            QuietCheck.IsChecked == true ? Colors.LightSkyBlue : Colors.LightGreen);
    }

    public void AppendNotification(string text) => Add("system", text);

    private void ShowReminders_Click(object sender, RoutedEventArgs e)
    {
        SelectPage("CHAT");
        AppendNotification(App.Instance.Reminders.Describe());
    }

    public void SubmitVoiceText(string recognized)
    {
        if (_busy)
        {
            // A fresh voice request replaces an in-flight AI reply. Keep only
            // the latest recognized utterance; never build an unbounded queue.
            if (_activeReplyCts is null)
            {
                AppendNotification("A separate operation is still running. Try again shortly.");
                return;
            }
            _queuedVoicePrompt = recognized;
            CancelActiveReply(discardQueuedPrompt: false);
            UpdateStatus("REPHRASING…", Colors.LightSkyBlue);
            return;
        }
        _ = SendAsync(recognized);
    }

    /// <summary>
    /// STOP REPLY, Talk Once, and STOP ALL can abandon an in-flight model
    /// response. External tool actions already started cannot be undone.
    /// </summary>
    public void CancelActiveReply(bool discardQueuedPrompt = true)
    {
        if (discardQueuedPrompt) _queuedVoicePrompt = null;
        _activeReplyCts?.Cancel();
    }

    private void CancelReply_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.CancelCurrentReply();
        UpdateStatus("REPLY INTERRUPTED", Colors.LightSkyBlue);
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
        UpdateLookoutStatus();
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

        using var replyCts = new CancellationTokenSource();
        _activeReplyCts = replyCts;
        var token = replyCts.Token;
        _busy = true;
        SendButton.IsEnabled = false;
        PromptBox.IsEnabled = false;
        PromptBox.Clear();
        Add("user", prompt);
        App.Instance.SetMood(CompanionMood.Thinking);
        UpdateStatus(lookNow ? "LOOKING AT SCREEN" : "THINKING", Colors.Plum);
        ChatTurn? livePreview = null;
        var streamed = false;
        var speakingSentences = false;
        var showCoachResult = false;
        var partialText = new StringBuilder();
        var lastPreview = DateTimeOffset.MinValue;

        async Task OnModelDeltaAsync(string delta)
        {
            token.ThrowIfCancellationRequested();
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(() => ApplyModelDelta(delta));
                return;
            }
            ApplyModelDelta(delta);
        }

        void ApplyModelDelta(string delta)
        {
            token.ThrowIfCancellationRequested();
            if (livePreview is null)
            {
                livePreview = new ChatTurn
                {
                    Role = "stream",
                    Text = "…",
                    Timestamp = DateTimeOffset.Now
                };
                Messages.Add(livePreview); // NEVER persist partial assistant text.
            }
            partialText.Append(delta);
            var now = DateTimeOffset.UtcNow;
            if (partialText.Length >= 100 &&
                now - lastPreview >= TimeSpan.FromMilliseconds(70) ||
                lastPreview == DateTimeOffset.MinValue)
            {
                var index = Messages.IndexOf(livePreview);
                if (index >= 0)
                {
                    livePreview = new ChatTurn
                    {
                        Role = "stream",
                        Text = partialText.ToString(),
                        Timestamp = livePreview.Timestamp
                    };
                    Messages[index] = livePreview;
                    if (IsVisible)
                        Dispatcher.BeginInvoke(() => TranscriptScroll.ScrollToEnd());
                }
                lastPreview = now;
            }
            if (speakingSentences)
                App.Instance.PushLiveSpeechDelta(delta);
        }

        void ClearLivePreview()
        {
            if (livePreview is null) return;
            Messages.Remove(livePreview);
            livePreview = null;
        }

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
            {
                if (!App.Instance.Brain.IsLocal &&
                    System.Windows.MessageBox.Show(
                        "Share one screenshot of your selected monitor with " +
                        "your cloud AI provider to get a game tip?\n\n" +
                        "The image may show personal information. Nothing is sent " +
                        "unless you approve this specific request.",
                        "Capri — send game screenshot?",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning,
                        MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    answer = "Game screenshot not shared. Select local Ollama or approve a " +
                             "one-time cloud screenshot to get visible game advice.";
                }
                else
                {
                    answer = await App.Instance.AskGameHelpAsync(prompt, token);
                    showCoachResult = true;
                }
            }
            else if (lookNow || (App.Instance.Settings.WatchScreen && RefersToScreen(prompt)))
            {
                if (!App.Instance.Brain.IsLocal && !App.Instance.Settings.WatchScreen && !lookNow)
                    throw new InvalidOperationException("Enable WATCH SCREEN to use cloud vision.");
                answer = await App.Instance.AskScreenAsync(prompt, token);
            }
            else
            {
                var action = await App.Instance.TryHandleActionAsync(prompt);
                if (action is not null)
                    answer = action;
                else
                {
                    streamed = true;
                    speakingSentences = App.Instance.BeginLiveSpeech();
                    answer = await App.Instance.ReplyWithAmbientContextAsync(
                        Messages.Where(x => x.Role != "stream").ToArray(),
                        token, OnModelDeltaAsync);
                }
            }

            // Model cancellation also prevents stale answers from being saved or
            // spoken. It cannot roll back an external tool action already sent.
            token.ThrowIfCancellationRequested();
            ClearLivePreview();
            Add("assistant", answer);
            if (showCoachResult) App.Instance.ShowGameCoachTip(answer);
            App.Instance.SetMood(CompanionMood.Reply);
            if (streamed && speakingSentences)
                App.Instance.CompleteLiveSpeech();
            else
                App.Instance.SpeakIfEnabled(answer);
            UpdateStatus("READY", Colors.LightGreen);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            ClearLivePreview();
            if (streamed) App.Instance.StopVoiceOutput();
            App.Instance.SetMood(CompanionMood.Idle);
            Add("system", "Reply interrupted. Any external action already initiated may still complete.");
            UpdateStatus("REPLY INTERRUPTED", Colors.LightSkyBlue);
        }
        catch (Exception exception) when (token.IsCancellationRequested)
        {
            // Providers can return a transport error while cancellation races.
            ClearLivePreview();
            if (streamed) App.Instance.StopVoiceOutput();
            App.Instance.SetMood(CompanionMood.Idle);
            UpdateStatus("REPLY INTERRUPTED", Colors.LightSkyBlue);
        }
        catch (Exception exception)
        {
            ClearLivePreview();
            if (streamed) App.Instance.StopVoiceOutput();
            App.Instance.SetMood(CompanionMood.Error);
            Add("system", "Request failed: " + exception.Message +
                "\nCheck your selected model, local Ollama status, or cloud key and quota.");
            UpdateStatus("REQUEST ERROR", Colors.OrangeRed);
        }
        finally
        {
            if (ReferenceEquals(_activeReplyCts, replyCts))
                _activeReplyCts = null;
            _busy = false;
            SendButton.IsEnabled = true;
            PromptBox.IsEnabled = true;
            if (IsVisible) PromptBox.Focus();
            if (_queuedVoicePrompt is { Length: > 0 } next)
            {
                _queuedVoicePrompt = null;
                _ = SendAsync(next);
            }
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
        SelectPage("CHAT");

        using var fileCts = new CancellationTokenSource();
        _activeReplyCts = fileCts;
        var fileToken = fileCts.Token;
        _busy = true;
        SendButton.IsEnabled = false;
        PromptBox.IsEnabled = false;
        UpdateStatus("READING SELECTED FILE", Colors.LightSkyBlue);
        App.Instance.SetMood(CompanionMood.Thinking);
        try
        {
            var excerpt = await Task.Run(() =>
                LocalFileContextReader.ReadExplicitFile(picker.FileName), fileToken);
            fileToken.ThrowIfCancellationRequested();

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
            fileToken.ThrowIfCancellationRequested();
            var question = LocalFileContextReader.BuildAnalysisPrompt(excerpt, task);
            Add("user", "Review selected file: " + excerpt.FileName +
                (string.IsNullOrWhiteSpace(task) ? "" : "\nQuestion: " + task) +
                (excerpt.Truncated ? "\n(First 12,000 characters)" : ""));
            PromptBox.Clear();
            var answer = await App.Instance.Brain.ReplyAsync(
                new[] { new ChatTurn { Role = "user", Text = question } }, fileToken);
            fileToken.ThrowIfCancellationRequested();
            Add("file_result", answer);
            App.Instance.SetMood(CompanionMood.Reply);
            // File-derived answers are not auto-spoken: cloud TTS would transmit
            // those excerpts to an additional third-party provider.
            UpdateStatus("FILE REVIEW READY", Colors.LightGreen);
        }
        catch (OperationCanceledException) when (fileToken.IsCancellationRequested)
        {
            App.Instance.SetMood(CompanionMood.Idle);
            Add("system", "File review interrupted before a reply was saved.");
            UpdateStatus("FILE REVIEW INTERRUPTED", Colors.LightSkyBlue);
        }
        catch (Exception ex) when (fileToken.IsCancellationRequested)
        {
            App.Instance.SetMood(CompanionMood.Idle);
            UpdateStatus("FILE REVIEW INTERRUPTED", Colors.LightSkyBlue);
        }
        catch (Exception ex)
        {
            App.Instance.SetMood(CompanionMood.Error);
            Add("system", "File review stopped: " + ex.Message);
            UpdateStatus("FILE NOT READ", Colors.OrangeRed);
        }
        finally
        {
            if (ReferenceEquals(_activeReplyCts, fileCts))
                _activeReplyCts = null;
            _busy = false;
            SendButton.IsEnabled = true;
            PromptBox.IsEnabled = true;
            PromptBox.Focus();
            if (_queuedVoicePrompt is { Length: > 0 } next)
            {
                _queuedVoicePrompt = null;
                _ = SendAsync(next);
            }
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
