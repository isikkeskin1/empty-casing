using System.Runtime.InteropServices;
using System.Windows.Automation;
using Capri.Core;

namespace Capri.Desktop.Services;

public sealed record UiTabInfo(string Name, string AutomationId, string RuntimeKey,
    bool Enabled, bool Offscreen, bool Selected = false);

public sealed record UiWindowSnapshot(IntPtr Handle, int ProcessId, string Title,
    DateTimeOffset CapturedAt, IReadOnlyList<UiTabInfo> Tabs);

/// <summary>
/// Strictly bounded, user-triggered Windows UI Automation for tab navigation only.
/// No text values, passwords, arbitrary buttons, mouse clicks or keyboard injection.
/// </summary>
public sealed class UiAutomationNavigator
{
    private static readonly SemaphoreSlim ScanGate = new(1, 1);

    public async Task<UiWindowSnapshot> InspectFrontAsync()
    {
        var target = new WindowDockingService().FrontmostWindow();
        if (target is null)
            throw new InvalidOperationException("No usable external window found. Open an application first.");
        return await WithScanTimeoutAsync(() => InspectWindow(target.Value.Handle));
    }

    public async Task<UiWindowSnapshot> InspectPinnedAsync(
        UiWindowSnapshot original, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var current = await WithScanTimeoutAsync(
            () => InspectWindow(original.Handle), token);
        if (current.Handle != original.Handle ||
            current.ProcessId != original.ProcessId ||
            !string.Equals(current.Title, original.Title, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "The approved application changed or closed. Automatic navigation stopped.");
        return current;
    }

    public async Task<string> SelectTabAsync(
        UiWindowSnapshot snapshot, string requestedLabel, CancellationToken token = default)
    {
        if (DateTimeOffset.UtcNow - snapshot.CapturedAt > TimeSpan.FromSeconds(20))
            return "The window snapshot expired. Inspect it again before acting.";

        var matches = snapshot.Tabs.Where(t =>
            string.Equals(t.Name, requestedLabel, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1)
            return "No unique matching tab was found in the inspected window.";

        var target = matches[0];
        if (!UiNavigationPolicy.CanSelectTab(requestedLabel, target.Name, "TabItem",
            target.Enabled, target.Offscreen, snapshot.CapturedAt, DateTimeOffset.UtcNow,
            true, out var reason))
            return "Blocked: " + reason;

        return await WithScanTimeoutAsync(() => SelectTabCore(snapshot, target, token), token);
    }

    private static UiWindowSnapshot InspectWindow(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !IsWindow(handle))
            throw new InvalidOperationException("The target window has closed.");

        var root = AutomationElement.FromHandle(handle)
            ?? throw new InvalidOperationException("Windows accessibility could not access this window.");
        var title = SafeRead(() => root.Current.Name) ?? "";
        var processId = SafeRead(() => root.Current.ProcessId);
        if (processId <= 0 || processId == Environment.ProcessId)
            throw new InvalidOperationException("Cannot inspect Capri or a protected window.");

        var tabs = new List<UiTabInfo>();
        foreach (var item in ScanTabs(root))
        {
            var name = SafeRead(() => item.Current.Name)?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 72) continue;
            var id = SafeRead(() => item.Current.AutomationId) ?? "";
            var runtime = SafeRead(() => string.Join(".", item.GetRuntimeId())) ?? "";
            if (runtime.Length == 0) continue;
            var enabled = SafeBool(() => item.Current.IsEnabled, false);
            var offscreen = SafeBool(() => item.Current.IsOffscreen, true);
            var selected = SafeBool(() =>
                item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern) &&
                pattern is SelectionItemPattern chosen && chosen.Current.IsSelected, false);
            tabs.Add(new UiTabInfo(name, id, runtime, enabled, offscreen, selected));
            if (tabs.Count >= 50) break;
        }

        return new UiWindowSnapshot(handle, processId,
            title.Length > 110 ? title[..110] : title,
            DateTimeOffset.UtcNow, tabs);
    }

    private static string SelectTabCore(
        UiWindowSnapshot snapshot, UiTabInfo chosen, CancellationToken token)
    {
        if (!IsWindow(snapshot.Handle))
            return "Target window closed. No action taken.";

        var root = AutomationElement.FromHandle(snapshot.Handle);
        var currentTitle = root is null ? "" : SafeRead(() => root.Current.Name) ?? "";
        if (currentTitle.Length > 110) currentTitle = currentTitle[..110];
        if (root is null || SafeRead(() => root.Current.ProcessId) != snapshot.ProcessId ||
            !string.Equals(currentTitle, snapshot.Title, StringComparison.Ordinal))
            return "The target application changed. No action taken.";

        // Do not act on the stale automation element from the earlier snapshot.
        // Resolve again from the same window and assert its runtime ID and accessibility role.
        var reObserved = ScanTabs(root).Where(item =>
            string.Equals(SafeRead(() => item.Current.Name), chosen.Name,
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(SafeRead(() => item.Current.AutomationId) ?? "", chosen.AutomationId,
                StringComparison.Ordinal) &&
            string.Equals(SafeRead(() => string.Join(".", item.GetRuntimeId())) ?? "",
                chosen.RuntimeKey, StringComparison.Ordinal)).Take(2).ToArray();
        if (reObserved.Length != 1)
            return "The tab changed or is ambiguous. Refresh inspection.";

        var tab = reObserved[0];
        var enabled = SafeBool(() => tab.Current.IsEnabled, false);
        var offscreen = SafeBool(() => tab.Current.IsOffscreen, true);
        var role = SafeRead(() => tab.Current.ControlType?.ProgrammaticName) ?? "";
        if (!UiNavigationPolicy.CanSelectTab(chosen.Name, SafeRead(() => tab.Current.Name),
            role == "ControlType.TabItem" ? "TabItem" : role,
            enabled, offscreen, snapshot.CapturedAt, DateTimeOffset.UtcNow,
            true, out var reason))
            return "Blocked: " + reason;

        if (!tab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern) ||
            pattern is not SelectionItemPattern selectable)
            return "That tab does not support a safe Windows accessibility selection.";

        token.ThrowIfCancellationRequested();
        selectable.Select();
        var selected = SafeRead(() => selectable.Current.IsSelected);
        return selected == true
            ? "Selected tab \"" + chosen.Name + "\" in " + snapshot.Title + "."
            : "Windows did not confirm the tab selection. No further action taken.";
    }

    private static IEnumerable<AutomationElement> ScanTabs(AutomationElement root)
    {
        var walker = TreeWalker.ControlViewWalker;
        var pending = new Queue<(AutomationElement Element, int Depth)>();
        pending.Enqueue((root, 0));
        var visited = 0;
        while (pending.Count > 0 && visited < 320)
        {
            var (element, depth) = pending.Dequeue();
            visited++;
            if (depth > 0 && SafeRead(() => element.Current.ControlType) == ControlType.TabItem)
                yield return element;
            if (depth >= 12) continue;

            var child = SafeRead(() => walker.GetFirstChild(element));
            var siblings = 0;
            while (child is not null && visited + pending.Count < 320 && siblings++ < 80)
            {
                pending.Enqueue((child, depth + 1));
                var old = child;
                child = SafeRead(() => walker.GetNextSibling(old));
            }
        }
    }

    private static bool SafeBool(Func<bool> read, bool fallback)
    {
        try { return read(); }
        catch (Exception ex) when (ex is ElementNotAvailableException or
            InvalidOperationException or COMException or UnauthorizedAccessException)
        { return fallback; }
    }

    private static T? SafeRead<T>(Func<T> read)
    {
        try { return read(); }
        catch (Exception ex) when (ex is ElementNotAvailableException or
            InvalidOperationException or COMException or UnauthorizedAccessException)
        { return default; }
    }

    private static async Task<T> WithScanTimeoutAsync<T>(
        Func<T> work, CancellationToken token = default)
    {
        if (!await ScanGate.WaitAsync(TimeSpan.FromSeconds(1), token))
            throw new InvalidOperationException("Windows accessibility inspection is already busy.");

        // Windows UIA providers can block. Do not free the gate while an orphaned
        // provider call is still running after the UI timeout.
        Task<T>? operation = null;
        try
        {
            operation = Task.Run(work, token);
            return await operation.WaitAsync(TimeSpan.FromSeconds(5), token);
        }
        finally
        {
            if (operation is not null && !operation.IsCompleted)
                _ = operation.ContinueWith(_ => ScanGate.Release(), TaskScheduler.Default);
            else
                ScanGate.Release();
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(IntPtr handle);
}
