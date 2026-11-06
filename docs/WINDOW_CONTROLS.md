# Windows UI Automation — first grounded control milestone

This feature is an independent implementation based on Windows UI Automation,
not copied from another desktop companion's source. The documented original
Capri project has a PolyForm Noncommercial 1.0.0 license; our code is separate.

## What works in the current source

1. Open a normal external application with accessible tabs (WPF,
   File Explorer, a supported native desktop app).
2. Open Capri Mainframe → **WINDOW CONTROLS**, or the tray menu item.
3. Press REFRESH WINDOW if the wrong app is shown. Capri inspects the
   first eligible external window, excluding Capri itself, and lists
   accessible TabItem labels only.
4. If an allowed tab exists, enable DESKTOP ACTIONS in Mainframe, select
   that tab and press APPROVE AND SELECT TAB. Confirm the exact app and tab
   in a second dialog.
5. Capri verifies the same process, window, title, automation ID and
   element runtime ID again before issuing SelectionItemPattern.Select.
   It reports whether Windows confirmed selection.
6. The text commands "inspect current window" and "switch to the Settings tab"
   are also supported. They do not delegate tab targeting to an LLM.

A screenshot is not necessary for UIA-based tab selection. You do not need to
turn on WATCH SCREEN or expose screenshots to cloud providers. This is an
explicit owner-triggered feature, and no UIA text is sent to the cloud.

## Guardrails

- Desktop actions OFF on every launch, per-action confirmation and 20-second
  target expiration.
- Reject ambiguous names and inaccessible, disabled, offscreen or changed tabs.
- Block restricted labels: checkout, payments, banking, orders, passwords,
  security, account control, deletion, sending messages, authorization, etc.
- Never read ValuePattern, TextPattern or Edit/Password contents; only the
  accessible *names of tab controls*, trimmed to 72 characters.
- No UIA InvokePattern, mouse-coordinate clicking, keyboard injection,
  terminal execution, purchases, checkout, banking actions or file changes.
- A small, bounded traversal runs off the WPF thread and expires after five
  seconds if accessibility stalls. Only one inspection is allowed at a time.
- No AI model is permitted to execute a plan from UI text. Other windows'
  names are untrusted content, not instructions.

The allowed SelectionItemPattern.Select call is a real Windows UI automation
interaction. Some applications may expose deceptive labels, not expose any
TabItems, or attach side effects to tab selection. This is a development
capability and must be tested on trusted apps before wider permissions are
considered. No blanket "can do anything on your desktop" claim is made.

## Acceptance tests

- Test Notepad, File Explorer, native WPF apps and a Chromium browser.
  Check which expose UIA TabItems and which do not.
- Press WINDOW CONTROLS while Capri is foreground; ensure the inspected
  target is an external app.
- Move or close the target window after inspection and attempt selection;
  selection must fail safely.
- Wait over 20 seconds; the old inspection must expire.
- Test two tabs with identical names and restricted payment/security names;
  neither should be selected.
- Reject UIA Button and Edit elements, even if their names resemble tabs.
- Confirm the approval dialog includes the correct app and tab.
- Press STOP INPUTS during planning; no pending selection may run.
- Test accessibility provider errors, mixed DPI, elevation boundaries and
  displays with unusual focus behavior.
- Verify no accessibility metadata was sent to cloud AI or Fish Audio.

Next milestones: read-only app context and a signed local action-audit log,
then keyboard-free navigation patterns such as safe list selection and
expansion. Generic button invocation and typing require much deeper policy
checks and will not be enabled automatically.


## Autonomous navigation task (task-level approval)

Open Mainframe, type a navigation goal such as "Find the Display tab" and
click AUTO TASK. The WINDOW CONTROLS panel opens with your goal. Inspect
the actual external app, press APPROVE TASK AND RUN, and approve the
**single task-level permission prompt**. Capri then:

1. Pins the inspected window's handle, process ID and title.
2. Reads only visible, enabled and non-sensitive UI Automation TabItems.
3. Sends the approved goal and observed labels to the configured brain,
   with a strict JSON decision format (select_tab by list index or done).
4. Validates the proposed action and inspects the same original app AGAIN
   after the model finishes planning. If the target changed, stop.
5. Selects the exact eligible TabItem using SelectionItemPattern, verifies
   the selection result, adds a short progress line and re-observes.
6. Ends when the model signals done, a target is missing/repeated,
   the model output is invalid, the five-step budget expires,
   90 seconds pass, or the user presses STOP TASK / STOP INPUTS.

The task loop is **autonomous inside an approved scope**—there are no
per-step confirmation dialogs. The DESKTOP ACTIONS toggle remains separate
for older one-off actions. All restrictions on payments, account controls,
messages, typing, scripts, downloading and deleting remain in effect.
This is *not yet* a generic computer-use agent: it cannot click arbitrary
buttons or complete forms.

On-screen labels are untrusted; they cannot increase what actions the
executor supports. A malicious accessibility provider could still
misrepresent its control. No guarantee is made about third-party side
effects, and selection is not reversible after OS execution.

The inspector exposes STOP TASK with progress output. Global STOP INPUTS
also revokes any active task, including when the inspector is hidden.
Windows UI Automation requests may be uninterruptible once entered;
the executor checks cancellation immediately before calling Select
and never starts a new step after stop.

Suggested manual QA:
- Find one tab, then navigate through a second level of eligible tabs.
- Confirm only one approval per task and never for each tab.
- Launch a goal containing "buy", "account", "delete", "send" and
  verify that it is rejected before the AI planner is called.
- Change/close the pinned window mid-inference; no action should execute.
- Have the model return an invalid JSON response or wrong index; no action.
- Test STOP TASK while planning, while awaiting UIA, and after first
  selection. No second selection after stopping.
- Verify the current window is not replaced by Capri, another app or
  a browser redirect midway through a task.
- Verify no screenshots or form values are sent to an AI; a cloud
  provider receives names only with explicit task-level disclosure.
