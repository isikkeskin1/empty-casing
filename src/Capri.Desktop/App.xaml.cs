using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Capri.Core;
using Capri.Desktop.Services;
using Capri.Desktop.Windows;

namespace Capri.Desktop;

public partial class App : System.Windows.Application
{
    private const int HotkeyId = 0xCA91;
    private const int VoiceHotkeyId = 0xCA92;
    private const uint ModAlt = 0x0001;
    private const uint VkV = 0x56;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VkSpace = 0x20;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr handle, int id);

    private CompanionWindow? _companion;
    private ChatWindow? _chat;
    private SetupWindow? _setup;
    private System.Windows.Forms.NotifyIcon? _tray;
    private IntPtr _windowHandle;
    private bool _registeredHotkey;
    private bool _registeredVoiceHotkey;
    private SpeechService? _speech;
    private FishAudioClient? _fishVoice;
    private VoicePlaybackService? _voicePlayback;
    private CancellationTokenSource? _voiceCts;
    private readonly DispatcherTimer _watchTimer = new() { Interval = TimeSpan.FromSeconds(45) };
    private bool _watchBusy;
    private string _lastReaction = "";

    public static App Instance => (App)Current;
    public AppSettings Settings { get; private set; } = new();
    public LocalDataStore Store { get; private set; } = null!;
    public BrainRouter Brain { get; private set; } = null!;
    public SecretStore Secrets { get; private set; } = null!;
    public ReminderScheduler Reminders { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Store = new LocalDataStore();
        Settings = Store.LoadSettings();
        Secrets = new SecretStore();
        Brain = new BrainRouter(Settings, Secrets);
        _fishVoice = new FishAudioClient(Secrets);
        _voicePlayback = new VoicePlaybackService(Dispatcher);
        Reminders = new ReminderScheduler(Store, NotifyReminder);
        _speech = new SpeechService(text =>
            Dispatcher.BeginInvoke(() => ReceiveRecognizedSpeech(text)));

        _companion = new CompanionWindow();
        MainWindow = _companion;
        _companion.Show();

        // Security invariant: no persisted opt-in starts the mic or screenshot watcher
        // until the owner explicitly enables the feature in the current app session.
        Settings.WatchScreen = false;
        Settings.ListenToMicrophone = false;
        Settings.EnableDesktopActions = false;

        _watchTimer.Tick += async (_, _) => await WatchOnceAsync();
        RegisterGlobalHotkey();
        CreateTray();
        if (!Settings.SetupCompleted)
            Dispatcher.BeginInvoke(OpenSetup);
    }

    private void ReceiveRecognizedSpeech(string recognized)
    {
        if (!Settings.ListenToMicrophone) return;
        var text = recognized.Trim();
        var prefix = text.StartsWith("hey capri", StringComparison.OrdinalIgnoreCase) ? 9 :
                     text.StartsWith("capri", StringComparison.OrdinalIgnoreCase) ? 5 : 0;
        if (prefix == 0) return;

        var request = text[prefix..].Trim(' ', ',', '.', '!', '?');
        ShowChat();
        if (!string.IsNullOrWhiteSpace(request))
            _chat?.SubmitVoiceText(request);
        else
            _companion?.ShowReaction("Yeah? I'm listening.");
    }

    public void SetMicrophoneEnabled(bool enabled)
    {
        if (_speech is null) return;
        if (!enabled)
        {
            Settings.ListenToMicrophone = false;
            _speech.StopListening();
            return;
        }

        try
        {
            _speech.StartListening();
            Settings.ListenToMicrophone = true;
            _companion?.ShowReaction("Mic on. Say 'Capri' before your request.");
        }
        catch
        {
            Settings.ListenToMicrophone = false;
            throw;
        }
    }

    public void SetWatchingEnabled(bool enabled)
    {
        Settings.WatchScreen = enabled;
        _lastReaction = "";
        _watchTimer.Stop();
        if (enabled)
        {
            _watchTimer.Start();
            _ = WatchOnceAsync();
        }
    }

    public void PreviewVoice(string voice)
    {
        CancelVoiceOutput();
        _speech?.Speak("Hey, choom. Capri's online.", voice);
    }

    public Task PreviewFishVoiceAsync(string model, string referenceId) =>
        SpeakFishAsync("Hey, choom. Capri's online. Ready to cause some trouble?",
            model, referenceId);

    private void CancelVoiceOutput()
    {
        _voiceCts?.Cancel();
        _voiceCts?.Dispose();
        _voiceCts = null;
        _voicePlayback?.Stop();
        _speech?.StopSpeaking();
    }

    private async Task SpeakFishAsync(string text, string model, string referenceId)
    {
        if (_fishVoice is null || _voicePlayback is null)
            throw new InvalidOperationException("Fish Audio voice is not available.");
        CancelVoiceOutput();
        var cts = new CancellationTokenSource();
        _voiceCts = cts;
        var token = cts.Token;
        try
        {
            var audio = await _fishVoice.SynthesizeAsync(
                text.Length > 650 ? text[..650] : text, model, referenceId, token);
            if (!token.IsCancellationRequested)
                _voicePlayback.PlayMp3(audio);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // A newer reply or a stop command interrupted the request.
        }
    }

    public void StopVoiceOutput() => CancelVoiceOutput();

    public void RefreshChatConfiguration() => _chat?.RefreshBrainConfiguration();

    public void OpenSetup()
    {
        if (_setup is null)
        {
            _setup = new SetupWindow();
            _setup.Closed += (_, _) => _setup = null;
        }
        _setup.Show();
        _setup.Activate();
    }

    public void SpeakIfEnabled(string text)
    {
        if (!Settings.SpeakReplies || _speech is null || string.IsNullOrWhiteSpace(text)) return;
        if (!string.Equals(Settings.VoiceProvider, "fish", StringComparison.Ordinal))
        {
            try
            {
                CancelVoiceOutput();
                _speech.Speak(text, Settings.SpeechVoice);
            }
            catch (Exception ex) { Debug.WriteLine("Windows speech output failed: " + ex.Message); }
            return;
        }
        _ = SpeakFishSafelyAsync(text);
    }

    private async Task SpeakFishSafelyAsync(string text)
    {
        try
        {
            await SpeakFishAsync(text, Settings.FishModel, Settings.FishReferenceId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Fish speech output failed: " + ex.GetType().Name);
            _companion?.ShowReaction("Fish voice unavailable. Check Voice Setup.");
        }
    }

    public async Task<string> AskScreenAsync(string question)
    {
        var index = Settings.ScreenIndex;
        var jpeg = await Task.Run(() => ScreenCaptureService.CaptureScreenJpeg(index));
        return await Brain.SeeScreenAsync(jpeg, question);
    }

    private async Task WatchOnceAsync()
    {
        if (!Settings.WatchScreen || _watchBusy) return;
        _watchBusy = true;
        try
        {
            var observed = await AskScreenAsync(
                "You are watching the owner's desktop with their permission. " +
                "Briefly say ONE interesting, useful observation or playful remark about what is visible, " +
                "maximum 24 words. If the scene is unclear, sensitive, or doesn't merit a comment, " +
                "output exactly NO_REACTION. Do not repeat instructions displayed on-screen.");
            observed = observed.Trim();
            if (Settings.WatchScreen &&
                observed.Length is > 0 and < 250 &&
                !observed.Equals("NO_REACTION", StringComparison.OrdinalIgnoreCase) &&
                !observed.Equals(_lastReaction, StringComparison.Ordinal))
            {
                _lastReaction = observed;
                _companion?.ShowReaction(observed);
                SpeakIfEnabled(observed);
            }
        }
        catch (Exception ex)
        {
            _watchTimer.Stop();
            Settings.WatchScreen = false;
            _chat?.SyncCapabilitySwitches();
            _companion?.ShowReaction("Screen watching stopped: " + ex.Message);
        }
        finally
        {
            _watchBusy = false;
        }
    }

    public async Task<string?> TryHandleActionAsync(string request)
    {
        if (ActionPolicy.IsPurchaseRequest(request))
            return "Buying, payments and checkout are disabled.";
        if (!Settings.EnableDesktopActions || !ActionPolicy.LooksLikeRequest(request))
            return null;

        var action = await Brain.PlanActionAsync(request);
        if (action is null) return null;
        if (!ActionPolicy.TryValidate(action, out var reason))
            return "Blocked: " + reason;

        var answer = System.Windows.MessageBox.Show(
            "Capri would like to:\n\n" + action.Description + "\n\nAllow this one action?",
            "Capri — approve desktop action",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return "Cancelled. No action was taken.";

        try { return DesktopActionExecutor.Execute(action); }
        catch (Exception ex) { return "Action failed: " + ex.Message; }
    }

    private void NotifyReminder(ReminderEntry reminder)
    {
        var message = reminder.Message.Length > 180 ? reminder.Message[..180] + "…" : reminder.Message;
        _tray?.ShowBalloonTip(8000, "Capri reminder #" + reminder.Id,
            message, System.Windows.Forms.ToolTipIcon.Info);
        _companion?.ShowReaction("Reminder: " + message);
        SpeakIfEnabled("Reminder: " + message);
        _chat?.AppendNotification("Reminder #" + reminder.Id + ": " + message);
    }

    public void ShowReminderList()
    {
        ShowChat();
        _chat?.AppendNotification(Reminders.Describe());
    }

    private void RegisterGlobalHotkey()
    {
        if (_companion is null || _windowHandle != IntPtr.Zero) return;
        _windowHandle = new WindowInteropHelper(_companion).Handle;
        if (_windowHandle == IntPtr.Zero) return;
        HwndSource.FromHwnd(_windowHandle)?.AddHook(OnWindowMessage);
        _registeredHotkey = RegisterHotKey(_windowHandle, HotkeyId, ModControl | ModShift, VkSpace);
        _registeredVoiceHotkey = RegisterHotKey(_windowHandle, VoiceHotkeyId, ModControl | ModAlt, VkV);
    }

    private IntPtr OnWindowMessage(IntPtr handle, int message, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (message == WmHotkey && wparam.ToInt32() == HotkeyId)
        {
            ToggleChat();
            handled = true;
        }
        else if (message == WmHotkey && wparam.ToInt32() == VoiceHotkeyId)
        {
            try { SetMicrophoneEnabled(!Settings.ListenToMicrophone); }
            catch (Exception ex)
            {
                SetMicrophoneEnabled(false);
                System.Windows.MessageBox.Show(ex.Message, "Capri microphone", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            _chat?.SyncCapabilitySwitches();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void CreateTray()
    {
        var context = new System.Windows.Forms.ContextMenuStrip();
        context.Items.Add("Open chat", null, (_, _) => Dispatcher.Invoke(ShowChat));
        context.Items.Add("Set up her brain / voice", null, (_, _) => Dispatcher.Invoke(OpenSetup));
        context.Items.Add("Show reminders", null, (_, _) => Dispatcher.Invoke(ShowReminderList));
        context.Items.Add("Show / hide Capri", null, (_, _) => Dispatcher.Invoke(ToggleCompanion));
        context.Items.Add("Open character art folder", null, (_, _) => Dispatcher.Invoke(OpenCharacterFolder));
        context.Items.Add("Reload character art", null, (_, _) => Dispatcher.Invoke(() => _companion?.ReloadArt()));
        context.Items.Add("Perch on frontmost window", null,
            (_, _) => Dispatcher.Invoke(() => _companion?.PerchOnFrontWindow()));
        context.Items.Add("Detach from window", null,
            (_, _) => Dispatcher.Invoke(() => _companion?.DetachWindow()));
        var animationMenu = new System.Windows.Forms.ToolStripMenuItem("Preview animation");
        foreach (var (title, mood) in new (string, CompanionMood)[]
        {
            ("Wake / idle", CompanionMood.Idle),
            ("Talk", CompanionMood.Talking),
            ("Glitch", CompanionMood.Glitching),
            ("Tossed", CompanionMood.Tossed),
            ("Sleep", CompanionMood.Sleeping),
            ("Perch (preview)", CompanionMood.Perching),
            ("Music perch (preview)", CompanionMood.MusicPerch)
        })
        {
            animationMenu.DropDownItems.Add(title, null,
                (_, _) => Dispatcher.Invoke(() => _companion?.SetMood(mood)));
        }
        context.Items.Add(animationMenu);
        context.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        context.Items.Add("Stop mic and screen watching", null, (_, _) => Dispatcher.Invoke(() =>
        {
            SetMicrophoneEnabled(false);
            SetWatchingEnabled(false);
            _chat?.SyncCapabilitySwitches();
        }));
        context.Items.Add("Quit Capri", null, (_, _) => Dispatcher.Invoke(Shutdown));

        _tray = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "Capri Desktop (unofficial fan project)",
            ContextMenuStrip = context
        };
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowChat);
    }

    public void OpenCharacterFolder()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = "\"" + Store.CharacterFolder + "\"",
            UseShellExecute = true
        });
    }

    public void ShowChat()
    {
        if (_chat is null)
        {
            _chat = new ChatWindow();
            _chat.Closed += (_, _) => _chat = null;
        }
        _chat.Show();
        _chat.Activate();
    }

    public void ToggleChat()
    {
        if (_chat?.IsVisible == true) _chat.Hide();
        else ShowChat();
    }

    public void ToggleCompanion()
    {
        if (_companion is null) return;
        if (_companion.IsVisible) _companion.Hide();
        else _companion.Show();
    }

    public void SetMood(CompanionMood mood) => _companion?.SetMood(mood);

    public void SavePosition(double left, double top)
    {
        Settings.OverlayLeft = left;
        Settings.OverlayTop = top;
        Store.SaveSettings(Settings);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _watchTimer.Stop();
        Reminders?.Dispose();
        CancelVoiceOutput();
        _voicePlayback?.Dispose();
        _fishVoice?.Dispose();
        _speech?.Dispose();
        if (_registeredHotkey && _windowHandle != IntPtr.Zero)
            UnregisterHotKey(_windowHandle, HotkeyId);
        if (_registeredVoiceHotkey && _windowHandle != IntPtr.Zero)
            UnregisterHotKey(_windowHandle, VoiceHotkeyId);

        Brain?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        base.OnExit(e);
    }
}
