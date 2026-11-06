# Visual Lookout, Quiet Mode & Calendar Reminders

This release adds an **original, owner-armed screen alert** inspired by the original Capri project's "keep an eye out" workflow. No original source code, character assets, or UI art are copied.

## Visual Lookout

1. Open **Senses & Control → Presence & Vision**.
2. Choose a monitor and enable **WATCH SCREEN**. For cloud AI, approve the existing repeated-screenshot disclosure. Your screen image is processed locally if Ollama is selected.
3. In **Visual Lookout**, write a short visible condition such as `my health is low` or `the download finishes`. Press **START LOOKOUT**, or say/type `Capri, tell me when my health is low`.
4. Capri checks visibly changed screenshots at the configured watch interval. When its vision model returns a strict positive verdict, it shows a one-shot local notification and cancels that alert.
5. Use **CANCEL LOOKOUT**, `cancel lookout`, `lookout status`, or **STOP ALL** to revoke it.

Lookouts live **only in RAM**, last for up to two hours, and do not survive restarting the app. They do not require desktop-action permissions, cannot issue game inputs and never start screen capture without WATCH SCREEN. The existing screen change gate skips blank/unchanged frames, and the app checks the alert's cancellation generation after each asynchronous model call. Normal ambient commentary is replaced by the focused visual condition during an active lookout: only one screenshot-model request is sent per eligible sample.

**Limitations:** Visual recognition is not game telemetry. It may miss a small HUD, an occluded icon or an unchanging screen. A model output other than exactly `ALERT` is treated as non-alert; positive detection is labelled *possible* and should be checked by the owner. The existing WATCH SCREEN cloud consent applies to subsequent screenshot processing. There is no background monitoring while Capri is closed.

## Quiet Mode

Turn on **Quiet mode (no commentary)** in **Senses & Control → Voice**, or say `Capri, quiet mode`. The mode is temporary for the current app session; it suppresses unsought music, presence and screen commentary and automatic text-to-speech. Text chat, manual tools, active lookouts and user-scheduled reminder tray notifications remain usable. `quiet off` turns it off.

Quiet Mode doesn't change hardware permission, uninstall models, or persist a microphone setting; use STOP ALL for a full input and action shutdown. Previously armed WATCH SCREEN pauses routine visual observations while Quiet Mode is active, unless there is an explicit lookout. Existing reminders still surface as Windows tray notifications.

## Calendar reminders

Besides `remind me in 10 minutes to stretch`, you can now say:

- `remind me tomorrow at 18:30 to exercise`
- `remind me on 2026-12-05 at 09:00 to call Mum`
- `remind me on 5 December at 9am to call Mum`

Times are parsed deterministically in your **computer's local time zone**. Dates without a year select the next occurrence. Invalid, ambiguous daylight-saving times, past dates and dates more than a year away are rejected rather than guessed. Saved reminder entries include their exact timestamp; overdue reminders appear when Capri restarts.

The app still does **not** register OS-level background alarms: it must be open to notify exactly on time. No calendar account connection is needed.
