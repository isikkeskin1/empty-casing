namespace Capri.Core;

/// <summary>
/// A staged release is an untrusted executable until the exact release URL,
/// SHA-256 digest and local file have all been checked by the caller.
/// This policy only decides WHEN an already verified installed-app update
/// may be started. It never approves a download or skips verification.
/// </summary>
public static class CapriAutoInstallPolicy
{
    public const int CheckAfterLaunchSeconds = 8;
    public const int CheckIntervalMinutes = 10;
    public const int IdleSeconds = 180;
    public const int MinimumStagedSeconds = 60;

    public static bool ShouldApplyDuringIdle(
        bool automaticUpdates,
        bool autoInstallEnabled,
        bool installedCopy,
        bool verifiedUpdateReady,
        bool activeWork,
        bool safeForeground,
        TimeSpan timeSinceLastInput,
        TimeSpan timeSinceStaged)
    {
        return automaticUpdates && autoInstallEnabled &&
               installedCopy && verifiedUpdateReady &&
               !activeWork && safeForeground &&
               timeSinceLastInput >= TimeSpan.FromSeconds(IdleSeconds) &&
               timeSinceStaged >= TimeSpan.FromSeconds(MinimumStagedSeconds);
    }

    public static bool ShouldApplyOnExit(
        bool automaticUpdates, bool autoInstallEnabled,
        bool installedCopy, bool verifiedUpdateReady) =>
        automaticUpdates && autoInstallEnabled &&
        installedCopy && verifiedUpdateReady;

    // Only the standard per-user Inno Setup installation location is allowed
    // to self-replace. Portable, developer and unknown paths must use the
    // official GitHub release page or manually launched installer.
    public static bool IsManagedInstallation(string? processPath, string localAppData)
    {
        if (string.IsNullOrWhiteSpace(processPath) ||
            string.IsNullOrWhiteSpace(localAppData))
            return false;
        try
        {
            var expected = Path.GetFullPath(Path.Combine(
                localAppData, "Programs", "CapriDesktop", "Capri.Desktop.exe"));
            return Path.GetFullPath(processPath).Equals(
                expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or
            NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
