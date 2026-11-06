using System.Windows;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class ConnectionsWindow : Window
{
    private bool _working;
    private CancellationTokenSource? _loginCts;

    public ConnectionsWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        SpotifyIdBox.Text = App.Instance.Settings.SpotifyClientId;
        RobloxIdBox.Text = App.Instance.Settings.RobloxClientId;
        RefreshConnectionLabels();
        Closed += (_, _) => _loginCts?.Cancel();
    }

    public void SetSpotifySearch(string query, IReadOnlyList<SpotifyTrackChoice> results, bool queue = false)
    {
        TrackQuery.Text = query;
        TrackResults.ItemsSource = results;
        StatusText.Text = queue
            ? "Choose the correct recording, then press QUEUE SELECTED."
            : "Choose the correct recording, then press PLAY SELECTED.";
    }

    private void RefreshConnectionLabels()
    {
        SpotifyAccountText.Text = App.Instance.LinkedAccounts.IsConnected(LinkedService.Spotify)
            ? "Spotify authorized for this Windows account."
            : "Spotify not linked. Enter a client ID and use CONNECT.";
        RobloxAccountText.Text = App.Instance.LinkedAccounts.IsConnected(LinkedService.Roblox)
            ? "Roblox authorized for this Windows account."
            : "Roblox not linked. Enter a client ID and use CONNECT.";
    }

    private void SaveIds_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            App.Instance.SaveLinkedClientIds(SpotifyIdBox.Text.Trim(), RobloxIdBox.Text.Trim());
            RefreshConnectionLabels();
            StatusText.Text = "Client IDs saved. Link or relink via the official login page.";
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }

    private async Task ConnectAsync(LinkedService provider)
    {
        if (_working) return;
        SaveIds_Click(this, new RoutedEventArgs());
        if (!LinkedAccountPolicy.IsClientIdValid(
            App.Instance.LinkedAccounts.ClientId(provider)))
        {
            StatusText.Text = "Enter a valid client ID for " +
                              LinkedAccountPolicy.ProviderName(provider) + ".";
            return;
        }

        _working = true;
        _loginCts = new CancellationTokenSource();
        CancelConnectButton.IsEnabled = true;
        StatusText.Text = "Opening " + LinkedAccountPolicy.ProviderName(provider) +
            " official browser authorization. No passwords entered into Capri.";
        try
        {
            StatusText.Text = await App.Instance.LinkedAccounts.ConnectAsync(
                provider, _loginCts.Token);
            RefreshConnectionLabels();
        }
        catch (OperationCanceledException)
        { StatusText.Text = "Account connection cancelled."; }
        catch (Exception ex)
        { StatusText.Text = "Connection failed: " + ex.Message; }
        finally
        {
            _loginCts.Dispose();
            _loginCts = null;
            CancelConnectButton.IsEnabled = false;
            _working = false;
        }
    }

    private void ConnectSpotify_Click(object sender, RoutedEventArgs e) =>
        _ = ConnectAsync(LinkedService.Spotify);
    private void ConnectRoblox_Click(object sender, RoutedEventArgs e) =>
        _ = ConnectAsync(LinkedService.Roblox);

    private void CancelConnect_Click(object sender, RoutedEventArgs e) =>
        _loginCts?.Cancel();

    private void UnlinkSpotify_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.LinkedAccounts.Disconnect(LinkedService.Spotify);
        RefreshConnectionLabels();
        StatusText.Text = "Spotify tokens erased from Capri. Revoke Capri from Spotify's account-app settings for server-side revocation.";
    }

    private void UnlinkRoblox_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.LinkedAccounts.Disconnect(LinkedService.Roblox);
        RefreshConnectionLabels();
        StatusText.Text = "Roblox tokens erased from Capri. Revoke app access in Roblox settings for server-side revocation.";
    }

    private async Task RunAsync(Func<Task<string>> action)
    {
        if (_working) return;
        _working = true;
        try { StatusText.Text = await action(); }
        catch (Exception ex) { StatusText.Text = "Provider request failed: " + ex.Message; }
        finally { _working = false; }
    }

    private void SpotifyStatus_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(async () =>
        {
            var profile = await App.Instance.LinkedAccounts.ProfileAsync(LinkedService.Spotify);
            var playback = await App.Instance.LinkedAccounts.SpotifyPlaybackAsync();
            SpotifyAccountText.Text = "Connected: " + profile.Name;
            return playback?.Summary ?? "No active Spotify Connect device/playback.";
        });

    private void RobloxProfile_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(async () =>
        {
            var profile = await App.Instance.LinkedAccounts.ProfileAsync(LinkedService.Roblox);
            RobloxAccountText.Text = "Connected: " + profile.Name + " (" + profile.Detail + ")";
            return "Roblox profile retrieved from official OAuth userinfo.";
        });

    private void SpotifyPlay_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(() => App.Instance.TrySpotifyActionAsync(MediaIntent.Play));
    private void SpotifyPause_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(() => App.Instance.TrySpotifyActionAsync(MediaIntent.Pause));
    private void SpotifyNext_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(() => App.Instance.TrySpotifyActionAsync(MediaIntent.NextTrack));
    private void SpotifyPrevious_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(() => App.Instance.TrySpotifyActionAsync(MediaIntent.PreviousTrack));

    private void SpotifySearch_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(async () =>
        {
            var result = await App.Instance.LinkedAccounts.SpotifySearchAsync(
                TrackQuery.Text.Trim());
            TrackResults.ItemsSource = result;
            return "Found " + result.Count + " Spotify track(s). Pick one to play.";
        });

    private void SpotifyPlayTrack_Click(object sender, RoutedEventArgs e)
    {
        if (TrackResults.SelectedItem is not SpotifyTrackChoice selected)
        {
            StatusText.Text = "Search and select a Spotify track first.";
            return;
        }
        _ = RunAsync(() => App.Instance.TrySpotifyTrackAsync(selected));
    }

    private void SpotifyQueueTrack_Click(object sender, RoutedEventArgs e)
    {
        if (TrackResults.SelectedItem is not SpotifyTrackChoice selected)
        {
            StatusText.Text = "Search and select a Spotify track first.";
            return;
        }
        _ = RunAsync(() => App.Instance.TryQueueSpotifyTrackAsync(selected));
    }

    private void OpenRobloxExperience_Click(object sender, RoutedEventArgs e) =>
        StatusText.Text = App.Instance.TryOpenRobloxExperience(RobloxPlaceBox.Text.Trim());
}
