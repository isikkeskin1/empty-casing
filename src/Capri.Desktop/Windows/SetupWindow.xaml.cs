using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class SetupWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SecretStore _secrets;
    private CancellationTokenSource? _pullCancellation;
    private bool _loading;
    private bool _closed;

    private void PostStatus(string message)
    {
        if (_closed || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            return;
        try
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!_closed) StatusText.Text = message;
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or
            TaskCanceledException) { /* Window was closed during download. */ }
    }

    public SetupWindow()
    {
        InitializeComponent();
        _settings = App.Instance.Settings;
        _secrets = App.Instance.Secrets;

        _loading = true;
        ProviderBox.Items.Add(new ComboBoxItem { Content = "LOCAL — Ollama / no cloud key", Tag = "local" });
        foreach (var provider in ProviderCatalog.Cloud)
            ProviderBox.Items.Add(new ComboBoxItem { Content = provider.Name, Tag = provider.Id });

        var selectedIndex = Enumerable.Range(0, ProviderBox.Items.Count).FirstOrDefault(i =>
            (string)((ComboBoxItem)ProviderBox.Items[i]).Tag == _settings.BrainProvider);
        ProviderBox.SelectedIndex = selectedIndex;
        LocalModelBox.Text = _settings.OllamaModel;
        LocalVisionBox.Text = _settings.VisionModel;
        CloudModelBox.Text = _settings.CloudTextModel;
        CloudVisionBox.Text = _settings.CloudVisionModel;
        SpeakOnCheck.IsChecked = _settings.SpeakReplies;

        VoiceBox.Items.Add(new ComboBoxItem { Content = "Windows default voice", Tag = "" });
        try
        {
            foreach (var voice in SpeechService.AvailableVoices())
                VoiceBox.Items.Add(new ComboBoxItem { Content = voice, Tag = voice });
        }
        catch (Exception ex)
        {
            StatusText.Text = "Voice list unavailable: " + ex.Message;
        }
        VoiceBox.SelectedIndex = Enumerable.Range(0, VoiceBox.Items.Count).FirstOrDefault(i =>
            (string)((ComboBoxItem)VoiceBox.Items[i]).Tag == _settings.SpeechVoice);

        VoiceProviderBox.Items.Add(new ComboBoxItem { Content = "LOCAL — Windows built-in voice", Tag = "windows" });
        VoiceProviderBox.Items.Add(new ComboBoxItem { Content = "CLOUD — Fish Audio", Tag = "fish" });
        VoiceProviderBox.SelectedIndex = _settings.VoiceProvider == "fish" ? 1 : 0;
        FishModelBox.Items.Add(new ComboBoxItem { Content = "S2.1 Pro Free — developer tier", Tag = FishAudioClient.FreeModel });
        FishModelBox.Items.Add(new ComboBoxItem { Content = "S2.1 Pro — paid tier", Tag = FishAudioClient.ProModel });
        FishModelBox.SelectedIndex = _settings.FishModel == FishAudioClient.ProModel ? 1 : 0;
        FishReferenceBox.Text = _settings.FishReferenceId;
        UpdateVoiceProvider();

        _loading = false;
        UpdateProvider();
        Closed += (_, _) =>
        {
            _closed = true;
            // Pull_Click owns the CTS and disposes it in finally. Disposing
            // it here races the in-flight Ollama request and its callbacks.
            try { _pullCancellation?.Cancel(); }
            catch (ObjectDisposedException) { }
        };
    }

    private string SelectedProvider =>
        (ProviderBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "local";

    private string SelectedVoiceProvider =>
        (VoiceProviderBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "windows";

    private string SelectedFishModel =>
        (FishModelBox.SelectedItem as ComboBoxItem)?.Tag as string ?? FishAudioClient.FreeModel;

    private string SelectedFishReference => FishReferenceBox.Text.Trim();

    private void UpdateVoiceProvider()
    {
        var fish = SelectedVoiceProvider == "fish";
        WindowsVoicePanel.Visibility = fish ? Visibility.Collapsed : Visibility.Visible;
        FishVoicePanel.Visibility = fish ? Visibility.Visible : Visibility.Collapsed;
        FishSavedKeyText.Text = _secrets.HasKey(FishAudioClient.KeyId)
            ? "Encrypted key saved for this Windows account. Leave blank to keep it."
            : "No Fish Audio key saved. Generate a NEW key and enter it here.";
    }

    private void VoiceProvider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || FishVoicePanel is null || WindowsVoicePanel is null) return;
        UpdateVoiceProvider();
    }

    private void SaveFishKeyIfEntered()
    {
        if (string.IsNullOrWhiteSpace(FishKeyBox.Password)) return;
        _secrets.Save(FishAudioClient.KeyId, FishKeyBox.Password);
        FishKeyBox.Clear();
        UpdateVoiceProvider();
    }

    private void GetFishKey_Click(object sender, RoutedEventArgs e) =>
        OpenOfficialLink("https://fish.audio/app/api-keys");

    private void RemoveFishKey_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(
                "Delete the locally saved Fish Audio key? This does not revoke it on Fish Audio.",
                "Capri — remove Fish key", MessageBoxButton.YesNo,
                MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        _secrets.Delete(FishAudioClient.KeyId);
        FishKeyBox.Clear();
        UpdateVoiceProvider();
        StatusText.Text = "Locally stored Fish Audio key deleted. Revoke it separately on Fish Audio if needed.";
    }

    private void UpdateProvider()
    {
        var local = ProviderCatalog.IsLocal(SelectedProvider);
        LocalPanel.Visibility = local ? Visibility.Visible : Visibility.Collapsed;
        CloudPanel.Visibility = local ? Visibility.Collapsed : Visibility.Visible;
        if (!local)
        {
            var provider = ProviderCatalog.Get(SelectedProvider);
            SavedKeyText.Text = _secrets.HasKey(provider.Id)
                ? "An encrypted key is saved. Leave blank to keep it."
                : "No key saved yet.";
            if (string.IsNullOrWhiteSpace(CloudModelBox.Text))
                CloudModelBox.Text = provider.SuggestedModel;
        }
    }

    private void Provider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LocalPanel is null) return;
        if (!ProviderCatalog.IsLocal(SelectedProvider))
            CloudModelBox.Text = ProviderCatalog.Get(SelectedProvider).SuggestedModel;
        UpdateProvider();
    }

    private void SaveSelection(bool completed)
    {
        // Validate the entire selection before mutating live settings.
        // Previously a failed model/voice/API-key check left the app with a
        // half-updated brain or voice provider despite "Setup incomplete".
        var provider = SelectedProvider;
        var localModel = LocalModelBox.Text.Trim();
        var visionModel = LocalVisionBox.Text.Trim();
        var cloudModel = CloudModelBox.Text.Trim();
        var cloudVision = CloudVisionBox.Text.Trim();
        var speechVoice = (VoiceBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        var voiceProvider = SelectedVoiceProvider;
        var fishModel = SelectedFishModel;
        var fishReference = SelectedFishReference;
        var speakReplies = SpeakOnCheck.IsChecked == true;

        if (string.IsNullOrWhiteSpace(localModel) ||
            string.IsNullOrWhiteSpace(visionModel) ||
            (!ProviderCatalog.IsLocal(provider) && string.IsNullOrWhiteSpace(cloudModel)))
            throw new InvalidOperationException("Choose valid model names first.");
        if (voiceProvider == "fish" &&
            (!FishAudioClient.IsSupportedModel(fishModel) ||
             !FishAudioClient.IsValidReference(fishReference)))
            throw new InvalidOperationException("Invalid Fish voice model or reference ID.");

        // Credentials use Windows-encrypted SecretStore, never the JSON
        // settings file. Keys are applied only after ordinary validation.
        if (!ProviderCatalog.IsLocal(provider) &&
            !string.IsNullOrWhiteSpace(CloudKeyBox.Password))
        {
            _secrets.Save(provider, CloudKeyBox.Password);
            CloudKeyBox.Clear();
        }
        if (!ProviderCatalog.IsLocal(provider) && !_secrets.HasKey(provider))
            throw new InvalidOperationException("Enter a cloud API key or select LOCAL.");

        if (voiceProvider == "fish")
        {
            SaveFishKeyIfEntered();
            if (!_secrets.HasKey(FishAudioClient.KeyId))
                throw new InvalidOperationException(
                    "Enter a Fish Audio key before selecting cloud speech.");
        }

        _settings.BrainProvider = provider;
        _settings.OllamaModel = localModel;
        _settings.VisionModel = visionModel;
        _settings.CloudTextModel = cloudModel;
        _settings.CloudVisionModel = cloudVision;
        _settings.SpeechVoice = speechVoice;
        _settings.VoiceProvider = voiceProvider;
        _settings.FishModel = fishModel;
        _settings.FishReferenceId = fishReference;
        _settings.SpeakReplies = speakReplies;
        _settings.SetupCompleted |= completed;
        App.Instance.Store.SaveSettings(_settings);
        App.Instance.RefreshChatConfiguration();
        UpdateProvider();
    }

    private static void OpenOfficialLink(string url)
    {
        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    }

    private void DownloadOllama_Click(object sender, RoutedEventArgs e) =>
        OpenOfficialLink("https://ollama.com/download/windows");

    private void GetKey_Click(object sender, RoutedEventArgs e) =>
        OpenOfficialLink(ProviderCatalog.Get(SelectedProvider).KeyPage);

    private async void Pull_Click(object sender, RoutedEventArgs e)
    {
        if (_pullCancellation is not null) return;
        var pull = new CancellationTokenSource();
        _pullCancellation = pull;
        PullButton.IsEnabled = false;
        CancelPullButton.IsEnabled = true;
        try
        {
            StatusText.Text = "Downloading chat model...";
            await LocalModelSetup.PullAsync(LocalModelBox.Text.Trim(), text =>
                PostStatus("CHAT: " + text), pull.Token);
            if (_closed || pull.IsCancellationRequested) return;

            if (LocalVisionBox.Text.Trim() != LocalModelBox.Text.Trim())
            {
                StatusText.Text = "Downloading vision model...";
                await LocalModelSetup.PullAsync(LocalVisionBox.Text.Trim(), text =>
                    PostStatus("VISION: " + text), pull.Token);
            }
            if (!_closed && !pull.IsCancellationRequested)
                StatusText.Text = "Both local models are ready.";
        }
        catch (OperationCanceledException)
        {
            if (!_closed) StatusText.Text = "Model download cancelled.";
        }
        catch (Exception ex)
        {
            if (!_closed) StatusText.Text = "Download failed: " + ex.GetType().Name +
                ". Check Ollama connection and available disk space.";
        }
        finally
        {
            if (ReferenceEquals(_pullCancellation, pull))
                _pullCancellation = null;
            pull.Dispose();
            if (!_closed)
            {
                PullButton.IsEnabled = true;
                CancelPullButton.IsEnabled = false;
            }
        }
    }

    private void CancelPull_Click(object sender, RoutedEventArgs e)
    {
        try { _pullCancellation?.Cancel(); }
        catch (ObjectDisposedException) { } // Concurrent completion.
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        try
        {
            SaveSelection(completed: false);
            StatusText.Text = "Contacting selected brain (cloud test may consume API credits)...";
            if (ProviderCatalog.IsLocal(SelectedProvider))
            {
                if (!await App.Instance.Brain.IsAvailableAsync())
                    throw new InvalidOperationException("Ollama is not running. Start it first.");
            }
            var answer = ProviderCatalog.IsLocal(SelectedProvider)
                ? await App.Instance.Brain.ReplyAsync(new[]
                    { new ChatTurn { Role = "user", Text = "Say just one word: ready." } })
                : await App.Instance.Brain.TestCloudAsync();
            if (!_closed)
                StatusText.Text = "Brain replied: " + answer[..Math.Min(140, answer.Length)];
        }
        catch (Exception ex)
        {
            if (!_closed) StatusText.Text = "Test failed: " + ex.GetType().Name;
        }
        finally { if (!_closed) TestButton.IsEnabled = true; }
    }

    private async void PreviewVoice_Click(object sender, RoutedEventArgs e)
    {
        PreviewVoiceButton.IsEnabled = false;
        try
        {
            if (SelectedVoiceProvider == "fish")
            {
                if (!FishAudioClient.IsSupportedModel(SelectedFishModel) ||
                    !FishAudioClient.IsValidReference(SelectedFishReference))
                    throw new InvalidOperationException("Select a supported Fish model and a valid voice reference ID.");
                SaveFishKeyIfEntered();
                if (!_secrets.HasKey(FishAudioClient.KeyId))
                    throw new InvalidOperationException("Enter a new Fish Audio API key.");
                StatusText.Text = "Requesting Fish voice preview (text sent to Fish Audio)...";
                await App.Instance.PreviewFishVoiceAsync(SelectedFishModel, SelectedFishReference);
                if (!_closed)
                    StatusText.Text = "Fish voice preview playing. The speech request may count toward your quota.";
            }
            else
            {
                var voice = (VoiceBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
                App.Instance.PreviewVoice(voice);
                StatusText.Text = "Playing local Windows voice preview.";
            }
        }
        catch (Exception ex)
        {
            if (!_closed) StatusText.Text = "Voice preview failed: " + ex.GetType().Name;
        }
        finally { if (!_closed) PreviewVoiceButton.IsEnabled = true; }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSelection(completed: true);
            if (!ProviderCatalog.IsLocal(SelectedProvider) && !_secrets.HasKey(SelectedProvider))
                throw new InvalidOperationException("Paste a cloud API key, or select LOCAL.");
            App.Instance.ShowChat();
            Close();
        }
        catch (Exception ex) { StatusText.Text = "Setup incomplete: " + ex.Message; }
    }

    private void OpenInputSettings_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenInputSettings();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
