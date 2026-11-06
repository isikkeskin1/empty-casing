# Real perception and assistant functions

The independent Capri Desktop implementation now has a **functional first assistant layer**. The reference project advertises a wider feature set. Our goal is deliberate functional development, not exaggerated parity.

| Capability | This build | Not yet supported |
| --- | --- | --- |
| Vision | Selected monitor screenshot on LOOK NOW | Continuous video; OCR-driven UI element grounding |
| Proactive reactions | Selected local or cloud vision model every 45 seconds when enabled | Game-event hooks, accurate damage/health detection |
| Microphone | Windows dictation with MIC ON or Ctrl+Alt+V; say "Capri" before a request | Voice activity detection, robust multilingual speaker-ID, call mode |
| Voice output | Windows TTS or opt-in Fish Audio MP3 TTS | Additional providers, streaming, barge-in |
| Actions | Approve per action: launch allowlisted applications, HTTPS URL, media keys | General clicking, typing, filesystem, remote messaging, buying |
| Windows interaction | Perch, toss, follow app windows | Alpha-perfect hit testing, universal fullscreen behavior |
| Timers/reminders | Relative timers and locally persisted reminders; tray notifications | OS service while Capri is closed; recurring calendar reminders |

## How to test

1. Install .NET 10 and Ollama (or download the CI ZIP). Pull a text model: \`ollama pull qwen3:4b\` and a vision model: \`ollama pull qwen3-vl:4b\`.
2. In Mainframe, set text and vision model names and choose SAVE.
3. Press LOOK NOW (this authorizes exactly one screenshot). Capri should answer a screen question.
4. Enable WATCH SCREEN for a limited test. She captures the **selected monitor** every 45 seconds and may comment briefly. Turn it off when finished. The screenshots are transmitted to local Ollama only and never written to disk by this app.
5. Turn on MIC ON. Use a speech language installed in Windows. Say "Capri, open notepad". Enable DESKTOP ACTIONS to permit an action **after a confirmation dialog**.
6. Turn on SPEAK REPLIES to hear Windows's synthetic voice. Use headphones when microphone listening is also on to avoid audio feedback.
7. In the tray, select Stop mic and screen watching to revoke both permission toggles immediately.

## Important security boundaries

- Screen watching and microphone recognition **start off on every launch**, regardless of any previous session. The app does not read webcam images or record system audio.
- An explicit screenshot capture or a screen-watching session sends resized JPEG bytes only to \`http://127.0.0.1:11434/api/chat\`. No screenshot files are saved; Ollama model/provider configuration and its own retention settings are outside the app.
- Speech recognition uses the Windows Speech API. The app does not save raw microphone audio. Accepted voice requests become ordinary locally saved chat messages. Windows speech recognition availability and related system settings vary by locale; verify Windows privacy settings separately.
- Images and text encountered on screen are **untrusted**. Screen analysis cannot call the action planner; only spoken/typed requests from the user may propose actions.
- The action planner can propose only \`open_app\`, \`open_url\` and \`media\`. A separate policy validator rejects unrecognized kinds, non-HTTPS URLs and unapproved applications; there is a confirmation dialog before each action. No automatic purchases or checkout, file deletions, message sending, arbitrary click/type, scripts or command-shell execution.
- The watch-mode text reactions appear temporarily and are not automatically stored in the conversation history. Answers to explicit screenshot questions *are* saved as chat text. Clearing chat deletes those messages.
- This project has no telemetry. All runtime requests go to the fixed local Ollama endpoint; the app does not silently add remote model providers.

## Limitations

The microphone feature is recognition, not a privacy-preserving always-on wake-word recognizer; while MIC ON the Windows recognition engine hears audio for dictation and the app only responds to phrases starting with "Capri". Turn it OFF when not needed.

Vision is not guaranteed to be correct. The model may misread small fonts or hallucinate. Proactive reaction timing is periodic and may miss fast gameplay events. On watch errors, watching disables itself. Running a vision model may use substantial GPU memory. Tests verify the request/response shape and safety policy, but Windows device integration requires manual testing.

The reference repository is useful for **behavioral inspiration**, not reusable code. Capri and its original animations belong to their respective rights holders; see ASSET_RIGHTS.md before public distribution.


## Local reminders and mic shortcut

Commands accepted directly by the app (no AI provider call): "remind me in 10 minutes to stretch", "set a timer for 30 seconds", "list reminders" and "cancel reminder #1". Timers/reminders are persisted under local app data; when the app runs, a Windows tray notification and Capri reaction are displayed at the due time. Overdue reminders trigger when the app starts again. Windows may suppress notifications through Focus Assist.

Select the monitor to watch in the Mainframe. Ctrl+Alt+V toggles microphone recognition. This toggle does not grant additional desktop permissions.


## Fish Audio voice

Choose Fish Audio in the voice panel and enter a newly generated API key. The default model is s2.1-pro-free; paid s2.1-pro is optional. The reference ID is optional and should name a voice you have permission to use. Capri sends JSON text to Fish's fixed HTTPS endpoint with an explicit model header and decodes the MP3 entirely in memory. No MP3 is saved. Fish may consume quotas or credits. Windows voice remains a fully local alternative.

This does not create a voice chat call, clone anyone's voice, recognize spoken input through Fish, or enable a character-accurate licensed voice automatically.


## Screen observation refinement

Mainframe can choose Follow frontmost app, Primary or a specific monitor, with
a 10-second to 10-minute automatic watch interval. A 16x9 grayscale in-memory
thumbnail is used only to skip blank/unchanged frames. The last brief observation
is accessible in the same session by asking "what did you see?" for three hours.
Proactive comments pause while active microphone listening is enabled, avoiding
interruptions. No screen frames or signatures are stored permanently.

Capri reacts to deliberate horizontal or vertical mouse rubs on her body,
with a cooldown and a toggle in INPUTS. Gesture detection is localized to
the WPF overlay. It does not monitor the user's general pointer movements.


## Voice conversation controls

Talk Once uses the selected local microphone for a single turn (maximum 20
seconds), with no wake prefix needed. Ctrl+Alt+Space is its default shortcut.
Continuous dictation still requires the "Capri"/"Hey Capri" prefix. Owners may
separately opt into short 20-second follow-up turns and interruptible speech;
both are off by default. Local stop/mute/shutdown phrases bypass the AI brain.
Voice turns are de-duplicated. This still uses Windows speech-recognition
language packs, not streaming, echo-cancelled full-duplex chat.


## Audio-responsive animation

An optional DANCE TO AUDIO switch listens to Windows *output* playback energy,
including music and games, using WASAPI loopback. No raw sound is stored,
transcribed or uploaded by Capri. The detector reports simple activity and
timing pulses to the companion's music-perch animation; it returns to idle
after playback stops. Consent is renewed every app session. No song title
recognition or call-speaker separation is implemented.


## Accessible Windows tab control

WINDOW CONTROLS explicitly inspects the foreground external application's
accessible UI Automation TabItems, without reading text values, passwords,
arbitrary buttons or pages into a cloud model. Capri can select a fresh,
unambiguous, allowed tab after a separate confirmation with desktop actions
enabled. The selected UI element is resolved again by window and runtime ID
immediately before selection. The snapshot expires in 20 seconds. Payment,
credential, security, destructive and message-related tab labels are blocked.

This is limited tab navigation, not general computer use. No arbitrary
InvokePattern, clicking coordinates, keyboard text injection, file changes
or checkout is implemented.


## Read an explicitly selected file

Mainframe READ FILE opens the Windows file picker. Supported small UTF-8 text
and source-code files are checked for likely secrets and limited to a 12k
character excerpt before being analyzed by the chosen brain. A cloud brain
requires additional consent for each file. No files are edited or executed,
no folders are indexed, and the model cannot trigger desktop actions from
file content. Raw excerpts and file-derived replies are kept ephemeral
in the session and not re-sent as future conversation history.


## One-task desktop navigation autonomy

The AUTO TASK button uses a model-guided observe/plan/act/observe loop over
the pinned application's accessible, non-sensitive tabs. The user approves
one named task (maximum five tab selections and 90 seconds), rather than
confirming each step. The model can suggest only an observed index or done.
Every selection is locally validated and re-resolved to the same window
and accessible control. The task can be interrupted with STOP TASK or STOP
INPUTS. Cloud users explicitly authorize sharing limited app/tab metadata
for the task; screenshots and form values aren't sent.

This version does not click arbitrary buttons, fill text fields, send
messages, alter files, execute shell commands or make purchases.


## Game Center

Lists locally installed Steam games using bounded read-only manifest discovery,
including additional libraryfolders.vdf paths. Uses a verified numeric Steam
app ID to launch via the registered steam://rungameid URI. Supports narrow
"play [installed game]" commands and a searchable list in Mainframe.
Does not download games, bypass DRM/anti-cheat or execute arbitrary paths.

GAME HELP sends a one-frame screenshot to the configured vision model after
the user requests it. Cloud screenshots always require separate approval,
even when hands-free desktop actions are armed. Answers are grounded only in
visible UI and make no claim of access to game internals.


## Media session awareness

Local Windows System Media Transport Controls now provide real track/video
title, artist and source metadata for compatible apps. A NOW PLAYING window
can pause, play, skip next and skip previous via the current media
session. Natural commands like "what song is playing" or "pause music"
use the same methods; HANDS-FREE only skips confirmation when DESKTOP ACTIONS
is enabled. Under DANCE TO AUDIO, a local 10-second poll produces
short track-change reactions without sending metadata to a cloud model.


## Voice Call Companion (experimental)

An explicit-activation voice call mode captures default Windows output
with WASAPI loopback and recognizes addressed speech locally with Windows
Speech Recognition. Capri answers with the selected AI brain and Fish voice
through the user-selected virtual audio cable endpoint, after an audible
AI introduction. Incoming caller speech is not allowed to operate the PC.
There is no auto-join, account impersonation, or silent recording.


## Native provider connections

Spotify and Roblox account integrations use separate OAuth2 PKCE flows,
provider authorization and Windows DPAPI token storage. Spotify supports
connected profile/current playback, track search and direct player
controls; Roblox supports a linked profile and validated experience
launches. Hands-free mode removes action confirmation dialogs for
already-supported, locally validated playback/launch controls, but never
bypasses spending or account-safety limits. No provider API tokens are
given to the chat model.
