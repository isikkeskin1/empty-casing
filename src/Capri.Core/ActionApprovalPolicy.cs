namespace Capri.Core;

/// <summary>
/// Separates the owner's consent choice from what the app is technically authorized
/// to execute. Hands-free mode only suppresses confirmations for already-validated
/// supported actions; it is never a way around action policies.
/// </summary>
public enum ActionApproval
{
    Blocked,
    ConfirmEachAction,
    HandsFree
}

public static class ActionApprovalPolicy
{
    public static ActionApproval Decide(
        bool desktopActionsEnabled,
        bool skipConfirmations,
        bool locallyValidated,
        bool prohibited)
    {
        if (!desktopActionsEnabled || !locallyValidated || prohibited)
            return ActionApproval.Blocked;
        return skipConfirmations
            ? ActionApproval.HandsFree
            : ActionApproval.ConfirmEachAction;
    }
}
