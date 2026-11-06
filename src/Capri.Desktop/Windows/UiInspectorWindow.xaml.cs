using System.Windows;
using System.Windows.Controls;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class UiInspectorWindow : Window
{
    private UiWindowSnapshot? _snapshot;
    private bool _busy;

    public UiInspectorWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        Loaded += async (_, _) =>
        {
            if (_snapshot is null)
                await RefreshAsync();
        };
    }

    public void DisplaySnapshot(UiWindowSnapshot snapshot)
    {
        _snapshot = snapshot;
        WindowName.Text = string.IsNullOrWhiteSpace(snapshot.Title)
            ? "(Untitled application)" : snapshot.Title;
        WindowDetails.Text = "PID " + snapshot.ProcessId + " · " +
            snapshot.Tabs.Count + " tab(s) · " +
            snapshot.CapturedAt.ToLocalTime().ToString("HH:mm:ss");
        TabsList.ItemsSource = snapshot.Tabs;
        SelectButton.IsEnabled = false;
        StatusText.Text = snapshot.Tabs.Count == 0
            ? "This application exposes no tab controls. No action taken."
            : "Choose a tab. This inspection expires in 20 seconds.";
    }

    private void Tab_Changed(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = _snapshot is not null &&
            TabsList.SelectedItem is UiTabInfo tab &&
            tab.Enabled && !tab.Offscreen &&
            UiNavigationPolicy.IsSafeTabLabel(tab.Name);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_busy) return;
        _busy = true;
        SelectButton.IsEnabled = false;
        _snapshot = null;
        StatusText.Text = "Inspecting the frontmost external application...";
        try
        {
            var snap = await App.Instance.UiNavigator.InspectFrontAsync();
            DisplaySnapshot(snap);
        }
        catch (Exception ex)
        {
            TabsList.ItemsSource = null;
            WindowName.Text = "No accessible application";
            WindowDetails.Text = "";
            StatusText.Text = "Inspection failed: " + ex.GetType().Name +
                ". Open a normal visible application and refresh.";
        }
        finally { _busy = false; }
    }

    private async void Select_Click(object sender, RoutedEventArgs e)
    {
        if (_busy || _snapshot is null || TabsList.SelectedItem is not UiTabInfo target)
            return;
        _busy = true;
        SelectButton.IsEnabled = false;
        try
        {
            StatusText.Text = await App.Instance.NavigateInspectedTabAsync(_snapshot, target.Name);
        }
        finally { _busy = false; }
    }
}
