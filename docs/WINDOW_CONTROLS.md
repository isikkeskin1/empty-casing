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
