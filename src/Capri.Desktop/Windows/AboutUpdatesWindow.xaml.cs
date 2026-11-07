using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class AboutUpdatesWindow : Window
{
    private readonly Version _current = typeof(App).Assembly.GetName().Version ??
        new Version(0, 6, 0);
    private CapriRelease? _latest;
    private bool _checking;
    private bool _syncing;

    public AboutUpdatesWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        VersionText.Text = "v" + _current.ToString(3) + "  /  Windows x64";
        RefreshReport();
        App.Instance.UpdateStateChanged += RefreshUpdateState;
        Closed += (_, _) => App.Instance.UpdateStateChanged -= RefreshUpdateState;
        RefreshUpdateState();
    }

    public void RefreshUpdateState()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshUpdateState);
            return;
        }
        _syncing = true;
        try
        {
            AutoUpdateCheck.IsChecked = App.Instance.Settings.AutoUpdateEnabled;
            AutoInstallCheck.IsChecked = App.Instance.Settings.AutoInstallUpdates;
            AutoInstallCheck.IsEnabled = App.Instance.CanAutomaticallyInstallUpdates;
            AutoInstallExplanation.Text = App.Instance.CanAutomaticallyInstallUpdates
                ? "Verified updates are installed after 3 minutes without input, " +
                  "only when Capri/desktop is in front and no chat, voice, gaming " +
                  "or automation task is active. Closing Capri also applies them."
                : "This is a portable or custom installation. Capri will still " +
                  "check and stage updates, but won't silently install over " +
                  "an unknown path. Use Restart to update or an official installer.";
            UpdateStatus.Text = App.Instance.UpdateStatus;
            _latest = App.Instance.LatestAvailableRelease;
            OpenReleaseButton.IsEnabled = _latest is not null;
            RestartUpdateButton.IsEnabled = App.Instance.UpdateReady;
            CheckButton.IsEnabled = !_checking && !App.Instance.UpdateChecking;
            ArtStatusText.Text = App.Instance.CharacterArtLoaded
                ? "ART READY · seven states detected"
                : "NO ART PACK · currently showing the fallback placeholder";
        }
        finally { _syncing = false; }
    }

    private void AutoUpdate_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetAutomaticUpdatesEnabled(AutoUpdateCheck.IsChecked == true);
        RefreshUpdateState();
    }

    private void AutoInstall_Changed(object sender, RoutedEventArgs e)
    {
        if (_syncing) return;
        App.Instance.SetAutomaticInstallationEnabled(AutoInstallCheck.IsChecked == true);
        RefreshUpdateState();
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (_checking) return;
        _checking = true;
        CheckButton.IsEnabled = false;
        try
        {
            await App.Instance.CheckAndStageUpdateAsync();
        }
        finally
        {
            _checking = false;
            RefreshUpdateState();
        }
    }

    private void RestartUpdate_Click(object sender, RoutedEventArgs e) =>
        App.Instance.InstallStagedUpdate();

    private void ImportPack_Click(object sender, RoutedEventArgs e) =>
        App.Instance.ImportCharacterPackInteractive();

    private void OpenArtFolder_Click(object sender, RoutedEventArgs e) =>
        App.Instance.OpenCharacterFolder();

    private void OpenRelease_Click(object sender, RoutedEventArgs e)
    {
        if (_latest is not { } release ||
            !CapriReleasePolicy.IsOfficialReleasePage(release.ReleasePage, release.Tag))
            return;
        OpenExternal(release.ReleasePage.AbsoluteUri);
    }

    private void OpenBuilds_Click(object sender, RoutedEventArgs e) =>
        OpenExternal(CapriReleasePolicy.BuildsPage);

    private void RefreshReport() =>
        ReportBox.Text = App.Instance.Diagnostics.ReadSupportReport(_current);

    private void RefreshReport_Click(object sender, RoutedEventArgs e) => RefreshReport();

    private void CopyReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Windows.Clipboard.SetText(ReportBox.Text);
            ReportFeedback.Text = "Report copied. Review it before sharing.";
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException
            or InvalidOperationException)
        {
            ReportFeedback.Text = "Clipboard unavailable. Select the text to copy it.";
        }
    }

    private void OpenData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var info = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            info.ArgumentList.Add(App.Instance.Store.DataFolder);
            Process.Start(info);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
            or InvalidOperationException)
        {
            ReportFeedback.Text = "Could not open local folder.";
        }
    }

    private void OpenExternal(string uri)
    {
        // URLs come only from hard-coded links or strict release URL validation.
        try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
            or InvalidOperationException)
        {
            UpdateStatus.Text = "Couldn't open the browser. Please visit the official GitHub repository.";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
