using System.Windows;
using System.Windows.Controls;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class GameCenterWindow : Window
{
    private IReadOnlyList<SteamGame> _games = Array.Empty<SteamGame>();
    private bool _busy;

    public GameCenterWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        Loaded += async (_, _) => await ReloadAsync(false);
    }

    private void Game_Changed(object sender, SelectionChangedEventArgs e) =>
        LaunchButton.IsEnabled = !_busy && GamesList.SelectedItem is SteamGame;

    private void Search_Changed(object sender, TextChangedEventArgs e) => Filter();

    private void Filter()
    {
        var search = SearchBox.Text.Trim();
        GamesList.ItemsSource = string.IsNullOrEmpty(search)
            ? _games : _games.Where(g =>
                g.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray();
        LaunchButton.IsEnabled = false;
        StatusText.Text = _games.Count + " installed game(s) discovered.";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = ReloadAsync(true);

    private async Task ReloadAsync(bool refresh)
    {
        if (_busy) return;
        _busy = true;
        LaunchButton.IsEnabled = false;
        StatusText.Text = "Reading local Steam manifests...";
        try
        {
            _games = await Task.Run(() => App.Instance.SteamGames.GetInstalledGames(refresh));
            Filter();
            if (_games.Count == 0)
                StatusText.Text = "No installed Steam games discovered. Check Steam's library folders.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Could not read Steam library: " + ex.GetType().Name;
        }
        finally { _busy = false; }
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || GamesList.SelectedItem is not SteamGame game) return;
        StatusText.Text = App.Instance.TryLaunchSteamGame(game);
    }

    private void GameHelp_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.ShowChat();
        App.Instance.RequestGameHelpFromChat();
        StatusText.Text = "Opened Mainframe to analyze one current screen frame.";
    }
}