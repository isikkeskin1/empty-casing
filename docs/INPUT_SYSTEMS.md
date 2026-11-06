# Capri Desktop input architecture

The original project at https://github.com/lachydotmcg/capri-desktop provides an extensive
feature guide; its code is PolyForm Noncommercial 1.0.0 and requires attribution. Our
implementation is independent so we can release our own code without copying the restricted
implementation. Character art rights are a separate question.

## Input channels and status

| Channel | Our implementation | Future work |
| --- | --- | --- |
| Typed prompts | Mainframe and user-confirmed desktop actions | More reliable app-aware text drafting |
| Global keyboard controls | Seven configurable RegisterHotKey combinations | Hotkey conflict diagnostics on more systems |
| Microphone | Selected WinMM/NAudio capture device streamed as bounded PCM into local Windows Speech Recognition | Better STT engine, language/device flexibility, low-latency VAD |
| Microphone test | Five-second live level meter, transient capture | Echo/error diagnosis |
| Microphone mute | Stops device capture, retains session setting | Push-to-talk |
| Recognized commands | Prefix Capri / Hey Capri before commands | Independent on-device wake word and audio interruption |
| Speaker | Selected device for Fish Audio in-memory MP3 playback | Selected output for Windows TTS and all sounds |
| Voice | Windows system TTS or opted-in Fish cloud TTS | Offline neural voice, sound style layers |
| Screen | Explicit snapshots or consented periodic watching of chosen monitor | Foreground-app follow and unchanged-frame filtering implemented; multi-screen simultaneous context remains future |
| Windows geometry | Track app rectangle and sit on its top edge | DPI and transparent hit-testing polish |
| Mouse | Drag, drop and toss Capri | Local head-pat/mouse-rub recognition implemented; cursor ride and icon interactions future |
| Stop inputs | One click disables screen, mic, desktop-action permission and spoken replies | Cancel all model requests/actions |
| Game input | Not built | Per-game user-taught controls, per-app opt-in and global stop |
| Call input | Not built | Opt-in loopback, speaker separation, caller cannot authorize actions |
| Music/audio loopback | Not built | Consent-based beat extraction and dance reactions |
| Files | Not built | Explicit file picker, read-only parsers, no unsolicited indexing |
| General computer use | Limited app/URL/media actions with per-action approval | UIA-targeted click/type and audit trail; no purchases |

## Input Control Center

Open Mainframe > INPUTS, the Setup wizard's CONFIGURE INPUTS button, or the tray menu.
Choose the capture device, inspect its five-second level meter and choose a playback
device for Fish Audio. Windows built-in speech still uses Windows default playback.

Click a shortcut field and press Ctrl+Alt or Ctrl+Shift with a letter, function key
(F1–F12) or Space. A shortcut must include Ctrl and at least Alt or Shift.
Duplicates are rejected; conflicts reported by Windows are shown to the user. This
uses RegisterHotKey and never installs a global keyboard logger.

| Action | Default shortcut |
| --- | --- |
| Chat panel | Ctrl+Shift+Space |
| Listening on/off | Ctrl+Alt+V |
| Mic mute/unmute | Ctrl+Alt+N |
| Focus typing | Ctrl+Alt+T |
| Hide/show companion | Ctrl+Alt+H |
| Bring near pointer | Ctrl+Alt+C |
| Stop all inputs | Ctrl+Alt+X |

STOP INPUTS disables microphone capture, screen watching and desktop-action
permission, and interrupts voice playback. It does not cancel unrelated OS
transactions or forcibly dismiss action-confirmation dialogues.

## Security and implementation

The microphone stream is 16kHz mono PCM, held only in memory. A bounded stream
feeds Windows System.Speech; overflow drops older samples rather than keeping
an unbounded backlog. The microphone is off at launch. Mute closes recording,
not merely ignores recognized words. The five-second diagnostic discards audio.
No raw microphone audio is transmitted by our speech-recognition service.

Device indexes can change when audio hardware is unplugged or drivers update.
Missing devices are rejected. Windows privacy settings may block access.
Screen observations and call audio must not be treated as owner commands.
Desktop actions still require approval, with payments and purchases prohibited.

## Manual QA before release

1. Open the Input Control Center on fresh Windows 10 and Windows 11 installs.
2. Select an alternate USB or Bluetooth microphone; run the 5-second meter.
3. Turn on microphone recognition, say Capri and a valid reminder, mute and unmute.
4. Unplug the active microphone; verify the app remains responsive and errors visibly.
5. Test every shortcut from browser, game and text editor; verify duplicates are rejected.
6. Move Capri near the cursor with Ctrl+Alt+C and focus typing with Ctrl+Alt+T.
7. Test Fish MP3 output on a nondefault speaker; Windows speech follows the system default.
8. Confirm STOP INPUTS stops mic capture, screen watching and speech output.
9. Inspect that local data contains no raw PCM or global-key event logs.
10. Test privacy rules with adversarial text on screen and simulated remote-call speech.

Windows CI and deterministic shortcut tests do not replace physical audio-device tests.
This update does not claim full-duplex calls, general keyboard/mouse automation or
feature parity with the original project.


## Smart observation and tactile input update

Mainframe's WATCH MONITOR offers Follow frontmost app, Primary and individual monitors.
WATCH EVERY supports 10, 30, 45, 60, 120, 300 or 600 second intervals. Opt-in
watching captures a small JPEG and calculates a 16x9 grayscale signature in memory
before sending to the selected local/cloud vision model. Unchanged and black
screens are skipped, reducing unnecessary API use. The model only receives
screenshots when meaningful visual changes have been observed. No screenshot is
saved to disk by this app. Watching stays quiet while live microphone listening
is active. Explicit LOOK NOW is never filtered by this automatic gate.

Up to one last proactive observation is retained in RAM for three hours; asking
"what did you see?" returns it without taking a new screenshot. This does not
persist across app restarts. It is not a full semantic screen-memory system.

Cursor strokes over Capri's own surface can trigger a friendly reaction after
several deliberate back-and-forth motions. A 10-second cooldown suppresses
repeat reactions. This is limited to Capri's WPF surface and does not hook or
record general mouse movements. Disable this behavior in Input Control Center.

Emergency stop increments the action-session generation. Any desktop action
proposal still being planned when stopped is rejected before its confirmation
and execution. Existing MessageBox approvals cannot be forcibly dismissed;
a future task controller must implement full per-step cancellation.


## Voice turn-taking v1 (Windows recognition)

- Mainframe TALK ONCE or Ctrl+Alt+Space: arm a single utterance, automatically
  time out after 20 seconds. If voice listening was previously off, the mic is
  closed after receiving the utterance or timing out. No wake phrase needed.
- MIC ON / Ctrl+Alt+V: continual Windows dictation, **only** treating speech
  prefixed by "Capri" or "Hey Capri" as a request.
- Optional follow-up: after an explicitly addressed turn, accept subsequent
  utterances without repeating the prefix within 20 seconds. Disabled by default.
- Optional speech interruption: with microphone listening enabled, permit
  the exact prefixed stop command to halt speech output, but no other command
  while Capri is speaking. Disabled by default, as echo cancellation is
  not provided.
- Local commands ("Capri mute microphone", "Capri stop inputs", "Capri stop")
  do not call the language model.
- Mainframe STOP VOICE cancels in-flight Fish TTS generation or stops active
  speech playback. STOP INPUTS also disables desktop-action permissions and
  perception channels.
- Recognizer duplicates are de-bounced before sending an AI request.
- A selected recording device, Windows Speech language pack and microphone
  permission remain prerequisites. Captured raw audio isn't saved by Capri.

This is a lower-latency turn-handling improvement but **not** full-duplex voice,
speaker separation, neural VAD, echo cancellation, or an always-on offline
keyword detector. Tests cover routing, timeouts, duplicates and local commands;
physical-device voice QA remains required.


## Music / system sound amplitude input

DANCE TO AUDIO in Mainframe uses WASAPI loopback on the default Windows
playback device. After a visible per-session consent warning, it derives
RMS amplitude and simple pulse timing, discarding raw sample buffers without
persisting or uploading them. The companion switches to a music-perch
animation while sound is present and returns to idle/perch after two seconds
of silence. A brief visual pulse can accompany stronger transients.

**Important:** output audio includes call participants, videos and other
applications. Unlike a microphone, it is output capture and cannot identify
which app produced each sound. It never authorizes desktop actions. This
feature starts OFF after every restart and STOP INPUTS turns it off.
No song-title detection, audio recording, speaker separation, or accurate
BPM estimation is claimed.
