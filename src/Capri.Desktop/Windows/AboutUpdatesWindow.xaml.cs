using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using Capri.Core;

namespace Capri.Desktop.Windows;

public partial class AboutUpdatesWindow : Window
{
    private readonly CapriReleaseClient _client = new();
    private readonly Version _current = typeof(App).Assembly.GetName().Version ??
        new Version(0, 6, 0);
    private CapriRelease? _latest;
    private CancellationTokenSource? _check;

    public AboutUpdatesWindow()
    {
        InitializeComponent();
        Owner = App.Instance.MainWindow;
        VersionText.Text = "v" + _current.ToString(3) + "  /  Windows x64";
        RefreshReport();
        Closed += (_, _) => _check?.Cancel();
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (_check is not null) return;
        _check = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        CheckButton.IsEnabled = false;
        OpenReleaseButton.IsEnabled = false;
        _latest = null;
        UpdateStatus.Text = "Checking official Capri GitHub Releases…";
        try
        {
            var release = await _client.CheckLatestAsync(_check.Token);
            _latest = release;
            if (release is null)
            {
                UpdateStatus.Text = "No public stable Capri release has been published yet. " +
                    "You can inspect recent CI preview builds through SEE PREVIEW BUILDS.";
            }
            else
            {
                OpenReleaseButton.IsEnabled = true;
                var compare = CapriReleasePolicy.IsNewer(_current, release);
                UpdateStatus.Text = (compare
                    ? "A newer release is available: "
                    : "No newer stable release found. Latest published: ") +
                    release.Tag + ". " +
                    (release.HasWindowsInstaller
                        ? "The release lists a Windows installer."
                        : "No Capri-Setup.exe is attached to that release.") +
                    " Press OPEN OFFICIAL RELEASE to inspect it. Nothing was downloaded.";
            }
        }
        catch (OperationCanceledException)
        {
            UpdateStatus.Text = "Update check timed out or was cancelled.";
        }
        catch (Exception ex) when (ex is HttpRequestException or
            InvalidDataException or InvalidOperationException or System.Text.Json.JsonException)
        {
            App.Instance.Diagnostics.Record("update-check", ex);
            UpdateStatus.Text = "Update check unavailable: " + ex.Message +
                " Use SEE PREVIEW BUILDS or try again later.";
        }
        finally
        {
            _check.Dispose();
            _check = null;
            CheckButton.IsEnabled = true;
        }
    }

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
            Clipboard.SetText(ReportBox.Text);
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
