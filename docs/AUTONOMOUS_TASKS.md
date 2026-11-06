# Task-based autonomy — first release

Capri's previous UI automation asked for confirmation for every tab selection.
The user should not have to micro-manage harmless navigation. This update
introduces a scoped, one-time authorization for AI-guided tab navigation.

**Workflow:** type a goal in Mainframe -> AUTO TASK -> inspect the target
window -> APPROVE TASK AND RUN -> confirm the app, goal, limits and any
cloud metadata sharing. Capri plans and executes safe tab selections
automatically, reporting progress. No per-click confirmation inside
the authorized task.

**Scope:** only visible, enabled, uniquely named, policy-approved Windows
UI Automation TabItem controls in a single initially inspected window.
The task is limited to five selections, 90 seconds, and the original
process/window identity. Any model output other than strict JSON
\`{"action":"select_tab","index":0}\` or \`{"action":"done"}\`
is rejected. Tabs whose names imply checkout, banking, security,
account controls, messages, deletion or authorization are refused.

The planner is a real model call (selected Ollama or cloud provider),
not hard-coded tab matching. It sees only the approved user goal and
eligible tab names with selected state, not arbitrary visible text or
form values. After model inference, Capri re-observes and revalidates
the intended tab, acts using SelectionItemPattern.Select, and verifies
Windows' selection receipt. Failures end the task; no blind retry loop.
Once the model marks done, Capri reports the steps without claiming
the user's full task was independently verified.

**STOP TASK** cancels the model task and prevents additional steps.
The same STOP INPUTS command/hotkey revokes the permission. Some
native accessibility calls cannot be canceled once already entered;
the system cannot undo a completed tab selection.

Purchases, subscriptions, checkout, payment, banking, scripts,
messages, security/credential controls, deletions, downloads and file
modifications cannot be performed by this agent. Full arbitrary
clicking is not enabled in this milestone; safe navigational patterns
will be expanded separately with audit logs and effect classification.

**Test on real Windows:** verify task starts without global action
permission, shows one consent prompt, can select one or two nested tabs
in a supported app, stops on stale or changed UI, rejects unsafe
goals/labels and invalid model decisions, and honors STOP INPUTS.
Test with both local Ollama and the chosen cloud model (only after
authorizing transmission of window/tab names).
