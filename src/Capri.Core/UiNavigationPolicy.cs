using System.Text.RegularExpressions;

namespace Capri.Core;

public enum UiNavigationKind { InspectWindow, SelectTab }

public sealed record UiNavigationRequest(UiNavigationKind Kind, string TabName = "");

/// <summary>
/// Strict, deterministic owner-command parser. Model output or webpage text is not an authority
/// to operate an application. This first iteration can only inspect tabs or select one tab.
/// </summary>
public static partial class UiNavigationPolicy
{
    [GeneratedRegex(@"^(?:switch|go|navigate|change|move) to (?:the )?(?<name>[\w][\w\s\-]{0,70}) tab[.!?]?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectTabRegex();

    // Matching case-insensitively also blocks forms such as "Pay Now" and "Billing Settings".
    private static readonly string[] RestrictedTerms =
    {
        "pay", "payment", "purchase", "checkout", "check out", "cart", "credit",
        "debit", "bank", "billing", "invoice", "order", "subscribe", "transfer",
        "password", "credential", "security", "privacy", "account", "admin",
        "delete", "remove", "erase", "wipe", "reset", "send", "message",
        "email", "confirm", "authorize", "approve", "permission", "recovery",
        "sign in", "login", "log in", "logout", "log out", "access key", "token"
    };

    public static UiNavigationRequest? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 120) return null;
        var request = text.Trim();
        if (new[] { "inspect current window", "inspect active window", "show window tabs",
                    "list window tabs", "show current app tabs" }
            .Contains(request.TrimEnd('.', '!', '?'), StringComparer.OrdinalIgnoreCase))
            return new UiNavigationRequest(UiNavigationKind.InspectWindow);

        var match = SelectTabRegex().Match(request);
        if (!match.Success) return null;
        var label = match.Groups["name"].Value.Trim();
        return IsSafeTabLabel(label)
            ? new UiNavigationRequest(UiNavigationKind.SelectTab, label) : null;
    }

    private static readonly HashSet<string> SafeNavigationButtons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Back", "Home", "Help", "Menu", "Navigation",
            "Open navigation", "Show navigation", "More", "Show more",
            "Overview", "Sidebar", "Show sidebar"
        };

    public static bool IsSafeNavigationButton(string? label) =>
        !string.IsNullOrWhiteSpace(label) &&
        SafeNavigationButtons.Contains(label.Trim());

    public static bool IsSafeTabLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Length > 72 ||
            label.Any(char.IsControl))
            return false;
        var words = Regex.Split(label.ToLowerInvariant(), @"[\s_\-\W]+")
            .Where(x => x.Length > 0)
            .ToArray();
        if (words.Length == 0) return false;
        foreach (var token in words)
        {
            if (RestrictedTerms.Contains(token, StringComparer.Ordinal)) return false;
        }
        var phrase = " " + string.Join(" ", words) + " ";
        return !RestrictedTerms.Any(term =>
        {
            if (!term.Contains(' ')) return false;
            return phrase.Contains(" " + term + " ", StringComparison.Ordinal);
        });
    }

    public static bool CanOperateNavigation(
        string? expectedLabel, string? observedLabel, string action,
        bool enabled, bool offscreen, DateTimeOffset captured,
        DateTimeOffset now, bool sameWindow, out string reason)
    {
        var age = now - captured;
        if (!sameWindow || age < TimeSpan.Zero || age > TimeSpan.FromSeconds(20))
        {
            reason = "Approved window or observation expired.";
            return false;
        }
        if (!enabled || offscreen)
        {
            reason = "Control is not visible and enabled.";
            return false;
        }
        if (!string.Equals(expectedLabel, observedLabel,
                StringComparison.OrdinalIgnoreCase))
        {
            reason = "Observed control label changed.";
            return false;
        }
        if (action == "expand_tree" && IsSafeTabLabel(expectedLabel) ||
            action == "press_navigation" && IsSafeNavigationButton(expectedLabel))
        {
            reason = "";
            return true;
        }
        reason = "This action or label is not in the navigation allowlist.";
        return false;
    }

    public static bool CanSelectTab(
        string? requestedLabel, string? observedLabel, string? controlRole,
        bool enabled, bool offscreen, DateTimeOffset captured, DateTimeOffset now,
        bool sameWindow, out string reason)
    {
        var age = now - captured;
        if (!sameWindow || age < TimeSpan.Zero || age > TimeSpan.FromSeconds(20))
        {
            reason = "Window snapshot expired or no longer matches. Inspect it again.";
            return false;
        }
        if (!enabled || offscreen || controlRole != "TabItem")
        {
            reason = "Only enabled and visible tab controls can be selected.";
            return false;
        }
        if (!IsSafeTabLabel(requestedLabel) || !IsSafeTabLabel(observedLabel))
        {
            reason = "This tab is outside the navigation safety allowlist.";
            return false;
        }
        if (!string.Equals(requestedLabel, observedLabel, StringComparison.OrdinalIgnoreCase))
        {
            reason = "The tab name no longer matches the observed control.";
            return false;
        }
        reason = "";
        return true;
    }
}
