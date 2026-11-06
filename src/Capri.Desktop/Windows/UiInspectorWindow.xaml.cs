using System.Windows;
using System.Windows.Controls;
using Capri.Core;
using Capri.Desktop.Services;

namespace Capri.Desktop.Windows;

public partial class UiInspectorWindow : Window
{
    private UiWindowSnapshot? _snapshot;
    private bool _busy;
    private bool _runningTask;

    public UiInspectorWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        Closed += (_, _) => App.Instance.StopAutonomousTask();
        Loaded += async (_, _) =>
        {
            if (_snapshot is null)
                await RefreshAsync();
        };
    }

    public void SetGoal(string goal)
    {
        if (!string.IsNullOrWhiteSpace(goal))
            TaskGoalBox.Text = goal;
        TaskGoalBox.Focus();
        TaskGoalBox.SelectAll();
    }

    public void DisplaySnapshot(UiWindowSnapshot snapshot)
    {
        _snapshot = snapshot;
        WindowName.Text = string.IsNullOrWhiteSpace(snapshot.Title)
            ? "(Untitled application)" : snapshot.Title;
        WindowDetails.Text = "PID " + snapshot.ProcessId + " · " +
            snapshot.Tabs.Count + " tab(s), " +
            snapshot.NavigationControls.Count + " navigation control(s) · " +
            snapshot.CapturedAt.ToLocalTime().ToString("HH:mm:ss");
        TabsList.ItemsSource = snapshot.Tabs;
        SelectButton.IsEnabled = false;
        StartTaskButton.Content = App.Instance.Settings.SkipActionConfirmations &&
            App.Instance.Settings.EnableDesktopActions
            ? "RUN — NO PROMPTS" : "APPROVE TASK AND RUN";
        StatusText.Text = snapshot.Tabs.Count == 0 && snapshot.NavigationControls.Count == 0
            ? "This app exposes no supported navigation controls. No action."
            : "Manually select an accessible tab or run a scoped AI task below.";
    }

    private void Tab_Changed(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = !_runningTask && _snapshot is not null &&
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

    private async void StartTask_Click(object sender, RoutedEventArgs e)
    {
        if (_runningTask || _busy) return;
        if (_snapshot is null)
        {
            StatusText.Text = "Press REFRESH WINDOW to select a target application first.";
            return;
        }
        var goal = TaskGoalBox.Text.Trim();
        if (!AutonomousNavigationPolicy.IsSafeGoal(goal))
        {
            StatusText.Text = "Only safe navigation goals are supported. Try: Find the Display tab.";
            return;
        }

        _runningTask = true;
        StartTaskButton.IsEnabled = false;
        StopTaskButton.IsEnabled = true;
        SelectButton.IsEnabled = false;
        TaskLog.Clear();

        try
        {
            var result = await App.Instance.RunAutonomousNavigationAsync(
                _snapshot, goal, line => Dispatcher.BeginInvoke(() =>
                {
                    if (!IsVisible) return;
                    if (TaskLog.Text.Length > 6000)
                        TaskLog.Text = TaskLog.Text[^4000..];
                    TaskLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + "\n");
                    TaskLog.ScrollToEnd();
                }));
            if (IsVisible)
            {
                TaskLog.AppendText("RESULT: " + result + "\n");
                TaskLog.ScrollToEnd();
                StatusText.Text = result;
            }
        }
        catch (Exception ex)
        {
            if (IsVisible)
                StatusText.Text = "Task aborted: " + ex.GetType().Name;
        }
        finally
        {
            _runningTask = false;
            StartTaskButton.IsEnabled = true;
            StopTaskButton.IsEnabled = false;
        }
    }

    private void StopTask_Click(object sender, RoutedEventArgs e)
    {
        App.Instance.StopAutonomousTask();
        StatusText.Text = "Stop requested. Any current UIA call will finish or time out; no new step will be authorized.";
        StopTaskButton.IsEnabled = false;
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
