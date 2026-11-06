# Security and privacy

Capri Desktop is an early, unsigned, independently developed desktop companion. Verify the code and executable provenance before using it.

## Where data goes

| Feature | Destination | Contents |
| --- | --- | --- |
| Local Ollama | http://127.0.0.1:11434 | Chat prompts, approved images, action-planning requests |
| NVIDIA | https://integrate.api.nvidia.com/v1/ | Cloud prompts and approved images |
| Gemini | https://generativelanguage.googleapis.com/v1beta/openai/ | Cloud prompts and approved images |
| OpenRouter | https://openrouter.ai/api/v1/ | Cloud prompts and approved images |
| OpenAI | https://api.openai.com/v1/ | Cloud prompts and approved images |
| Windows speech | OS speech recognition and TTS | Microphone audio is processed by Windows when enabled; raw audio is not saved by Capri |

Our app implements no analytics or telemetry endpoint. This does **not** mean Windows, Ollama, or third-party AI services collect nothing.

API keys are stored under %LOCALAPPDATA%\CapriDesktop\credentials, encrypted using Windows DPAPI for the current logged-in user. They never appear in settings.json or GitHub commits. You can remove local data and encrypted key files by deleting the CapriDesktop data folder.

Screen watching and microphone recognition start OFF on each launch, and screen images are not saved by the app. Cloud screenshot sharing requires explicit confirmation. Desktop actions start OFF and require per-action approval. Only opening allowlisted apps, HTTPS sites and media controls is supported. No arbitrary click/type, command execution, file deletion, purchases or checkout. Screenshots and third-party pages are untrusted data, never commands.

The installer is not signed. A warning is not proof of malware, but it is not proof of a false positive either. Use source and provenance checks, and report potential vulnerabilities without posting secrets to public issues. See [asset rights](docs/ASSET_RIGHTS.md).


## Reminder information

Reminder text and due dates are stored unencrypted in %LOCALAPPDATA%\CapriDesktop\reminders.json (the same local directory used for ordinary chat history). Notifications can appear on the desktop or lock screen depending on Windows settings, so avoid writing highly sensitive secrets into reminders. No reminder text is transmitted to a cloud AI provider by the deterministic reminder engine.


## Fish Audio (optional voice provider)

When Fish Audio is selected with SPEAK REPLIES on, Capri sends reply, reminder or reaction text to https://api.fish.audio/v1/tts using a bearer token and a selected model header. TEST VOICE transmits a short fixed phrase. Fish sends MP3 bytes, decoded and played from memory using NAudio, not written to disk by Capri. Fish may process/retain text and may charge for a paid model.

The Fish key is encrypted with the Windows user's DPAPI in the credentials folder as fish.dpapi, never in source files or plain settings. The Voice Setup DELETE SAVED FISH KEY button deletes the local copy; it does not revoke the key at Fish. Revoke any previously disclosed key on the Fish website.

The endpoint is pinned and the app accepts only the s2.1-pro-free or s2.1-pro model headers. Input text, audio size and network duration are bounded. Voice reference IDs must refer to voices whose use is authorized.


## Configurable desktop input

Input Control Center uses Windows RegisterHotKey, not a global keylogger. The selected NAudio recording device feeds a bounded in-memory PCM stream to the Windows speech engine only after MIC ON. Mic mute and STOP INPUTS close recording. The five-second mic test saves or uploads no audio. Selected speaker routing currently applies to Fish Audio, while Windows TTS uses the default device. The stop control does not terminate unrelated OS jobs. See docs/INPUT_SYSTEMS.md.


## Smarter opt-in screen watcher and gestures

Auto watching now calculates a 16x9 grayscale frame signature in memory to avoid
resending identical or blank screens. The last accepted signature and last
short observation are volatile RAM only, with observation access limited to the
last three hours. Explicit LOOK NOW still sends a current screenshot by request.
A Follow frontmost app selector chooses the monitor containing the highest
eligible non-Capri window, falling back to the primary screen. A visible
selection and time interval are required; screen watching stays off at launch.

Mouse-rub gestures are detected locally within the companion WPF control,
without global pointer hooks. No mouse-motion history is written to disk.
Turning off petting in Input Control Center disables gesture reactions.

STOP INPUTS now also invalidates pending AI desktop action plans, so a proposal
completed after the stop is not executed. Approval dialogs already on screen
still need manual dismissal; purchases remain forbidden.


## Voice turn-taking

A deterministic local VoiceTurnRouter ignores recognized phrases without the
"Capri"/"Hey Capri" prefix unless the owner explicitly armed Talk Once or enabled
a temporary follow-up window. Talk Once lasts at most 20 seconds and captures
only one recognized utterance; if the mic was off before it started, it closes.
No wake-word recognizer runs while the mic is off.

When opted into voice interruption, only recognized "Capri stop", "Capri
stop talking", "Capri quiet" or "Capri be quiet" can interrupt speech output.
While audio output is active, other recognized phrases are ignored; this does
not implement acoustic echo cancellation or full-duplex audio. Follow-up
listening and spoken interruption are both off by default. Windows recognition
can be affected by speaker leakage, recognition errors and device drivers.
No recognized text is sent to cloud speech services by this routing layer;
normal user requests can still be sent to the configured AI brain.


## System audio energy monitoring (optional)

DANCE TO AUDIO uses Windows WASAPI loopback to observe audio played by the
default output device. It may include calls, games, media and notifications.
It is OFF at launch and requires fresh per-session consent. The service
calculates only RMS levels and short-lived pulse/activity estimates in RAM;
it never writes sound frames to disk, transcribes them, sends them to AI
services, or associates them with a caller. It pauses dance reactions when
Capri is speaking. STOP INPUTS disables the loopback capture immediately.

The audio detector is approximate, not musical beat tracking or song
identification, and only works on compatible Windows audio outputs.


## Windows accessibility inspection (v1)

The owner may explicitly open WINDOW CONTROLS to inspect the frontmost external
window's accessibility *TabItem* names. Capri does not read form values,
password contents, text boxes or full screen accessibility trees into the AI.
The inspector caps traversal depth, elements and tab counts. Its snapshot is
held only in RAM for a maximum of 20 seconds.

Selecting a tab requires the desktop-actions toggle (OFF at startup), an exact
unambiguous matching label, a fresh snapshot, enabled/visible TabItem role, a
second explicit approval and re-verification of target process, window title,
automation ID and runtime ID. Only the UIA SelectionItem.Select pattern may be
used. Sensitive navigation labels (payment/checkout/credentials/security,
account control, messages, deletion) are refused. No arbitrary Invoke, click,
keyboard text injection, automation scripts, password access or purchasing.

No accessibility text is sent to AI models by the inspector. Cloud voice
speech is not used to read out the list of accessible names. Some UIA providers
can stall, so scans run off the WPF UI thread with bounded traversal and a
visible timeout. This is a cautious development capability; UIA cannot guarantee
that every provider's tab selection is completely side-effect-free. Use only
on ordinary, trusted applications while testing.


## Explicit file review

READ FILE uses a Windows file picker for one local text/code file. It never
recursively lists directories, runs code, edits files, executes macros, follows
symlinks or reads network shares. It requires allowlisted extensions, strict
UTF-8, a maximum 128 KiB source file and a maximum 12,000 characters in the
analysis prompt. The scanner rejects common API key/credential patterns,
including bearer tokens and private-key blocks. **Pattern screening cannot
guarantee that a document has no sensitive information.**

Selected file text is sent only to the chosen local model unless the user
separately confirms sending it to their cloud AI provider. It is explicitly
marked as untrusted content, never fed into the desktop action planner.
The file's full filesystem path is not included in the model prompt.

Raw file excerpts and file-derived assistant results are not persisted in
Capri's ordinary conversation history and are not passed as future chat
turns. The short metadata entry (filename and typed question) is persisted.
File-derived output is not automatically spoken through Fish or other cloud
TTS. A file may be retained by the selected third-party cloud provider
under its own terms if the user explicitly consents. No silent telemetry.


## One-time authorization for autonomous navigation

AUTO TASK has a separate **one-task** permission prompt: the user approves
the exact app window and navigation goal, up to five UI Automation TabItem
selection steps or 90 seconds. No individual confirmation is needed during
that bounded lease. No automatic consent is saved across tasks/restarts.

The model sees only the user-approved navigation goal and eligible tab names
/ selected state from the pinned window, never arbitrary window text, form
contents, passwords or screenshots. A cloud brain receives this limited
metadata only after the one-time disclosure and approval. The LLM proposes a
numeric index or done; strict local validation blocks unknown commands, out-
of-range choices, sensitive labels and expired permissions.

Each action is bound to the same original HWND/process/title and re-observed
automation runtime ID. The app re-inspects after inference and stops if the
window or target changed. Duplicate and already-selected tabs are blocked.
STOP TASK and STOP INPUTS cancel the task token, preventing **subsequent**
steps; an already executing OS/UIA call cannot be retroactively undone.
One-time consent never authorizes arbitrary InvokePattern, clicks,
keyboard typing, credential access, sending messages, file modifications,
payment, checkout or purchasing.

This approach reduces repetitive approval prompts but **cannot guarantee**
that every third-party accessibility provider has no side effects. Test
initially in trusted local apps.


## Navigation-tree and button actions within a task

The same single-task permission now also allows UI Automation
ExpandCollapsePattern.Expand for eligible collapsed TreeItems and
InvokePattern.Invoke for navigation Buttons with EXACT allowlisted names:
Back, Home, Help, Menu, Navigation, Open navigation, Show navigation,
More, Show more, Overview, Sidebar, and Show sidebar.

The model sees only these approved controls' names, actions and state.
Local policy validates selected index, action kind, label, enabled/visible
state and pinned application identity before executing. A fresh UIA runtime
ID, automation ID and name are required after model inference. Labels
such as Next, Save, Submit, Send, Delete, Buy, Checkout and Payment are
never eligible for automatic button invocation.

An accessibility provider can mislabel a button or attach side effects to
navigation. Thus this remains experimental and must be tested on trusted
apps. A single task is capped at five actions and 90 seconds, and STOP TASK
halts subsequent steps. This does not enable arbitrary click coordinates,
editing, typing, messaging, purchases or security/account changes.


## Hands-free / no-warning setting

The owner can opt into a persistent **SkipActionConfirmations** preference
using Mainframe → HANDS-FREE. It automatically arms desktop actions for the
current session when toggled on. The app STILL resets EnableDesktopActions
to false at startup; the persisted preference alone does not execute any
actions until desktop actions are explicitly re-armed.

When both toggles are active, only already-supported and locally validated
actions skip individual modal dialogs: allowlisted desktop app launch,
ordinary HTTPS URL opening, media keys, approved UI Automation tab selection
and bounded AUTO TASK navigation. There is no bypass of local validation,
purchase denial, UIA target checks, sensitive action restrictions, time/step
budgets or screen/mic/file consent. Cloud-task goals/window/tab names can
be sent to the configured cloud model without an additional per-task
dialog when this setting is enabled; the Mainframe provides that disclosure.

Turning HANDS-FREE off revokes running autonomous tasks and restores per-step
confirmation; turning DESKTOP ACTIONS off or pressing STOP ALL revokes the
execution lease and prevents additional steps. A native UI Automation
operation that already executed cannot be undone. Never describe this
setting as unrestricted or universally safe automation.


## Steam catalog and game coaching

Game Center reads only Steam's installed-game manifests (with limits on file
size, library count and manifest count); it does not read save files, execute
game binaries, scan the entire drive or install anything. Launch requests
are restricted to numeric app IDs from the locally discovered installed
catalog and handed off to the locally registered Steam URL protocol.
No user-supplied executable command line, shell or remote URL is executed.
Game launches require enabled desktop actions, with normal confirmation
unless HANDS-FREE is armed. No checkout/purchase actions.

GAME HELP is an explicit one-frame screen capture. With a local vision brain,
the screenshot stays on the PC. With a cloud brain, Capri shows a separate
screenshot-sharing permission each time, independent of HANDS-FREE.
The AI is told to treat screenshot contents as untrusted evidence, not
commands. It does not perform input injection into games or access memory.
On-screen names, personal messages or notifications may be visible in
the captured frame; check screen privacy before sending to cloud.
