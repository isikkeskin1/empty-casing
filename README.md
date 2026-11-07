# Capri Desktop — independent Windows companion

An **unofficial, noncommercial fan project** building an interactive desktop companion inspired by Capri from *Cyberpunk: Edgerunners 2*.

This is an **independent MIT-licensed implementation** with a separately licensed default character-art component. Windows releases include the seven upstream Capri animated character sequences under **PolyForm Noncommercial 1.0.0**, with required attribution and upstream notices included beside the packaged assets. The art is **not MIT-licensed** and may not be used commercially. The floating companion and Mainframe display real animations immediately after installation, without manual ZIP import. See [character-art rights](docs/ASSET_RIGHTS.md). This is not an official Capri project or endorsed by its creators or anime rights holders.

## Current milestone: local assistant + desktop presence

- Transparent, always-on-top, draggable companion with window-edge snapping, follow-on-move, detach on minimize/close and bounded toss motion
- Optional user-provided PNG sequences for idle, talk, glitch, sleep, toss, perch and music-perch; individual frame timing supported
- Local Ollama or optional NVIDIA, Gemini, OpenRouter and OpenAI cloud chat, with editable model IDs
- Local JSON history, settings and persistent reminders/timers with notification bubbles
- Tray menu, show/hide shortcut (Ctrl+Shift+Space), and opening the animation folder
- Screen questions: **LOOK NOW** captures the chosen monitor and sends a JPEG to the selected local or cloud vision model with permission
- Opt-in watch mode: capture every 45 seconds and show brief AI reactions as a temporary character bubble
- Windows microphone recognition: toggle MIC ON or use Ctrl+Alt+V, then start a request with **"Capri"**; native Windows text-to-speech output can be enabled separately
- Approval-gated desktop actions: launch selected applications, open HTTPS pages and send media keys. No purchases, arbitrary clicks, command execution, typing, deletion or messaging
- Per-user Windows installer, first-run brain and voice wizard, optional Fish Audio speech output with encrypted cloud keys using Windows DPAPI
- Windows CI builds a portable ZIP and installer executable

**Not yet implemented:** seamless streamed voice conversation, character-specific voice, multi-monitor vision selection, game-event understanding, Discord call participation, general computer use, semantic memory or an updater. Current speech uses the Windows-installed recognizer and voice; current vision is a **single primary-monitor snapshot**, not a continuous video feed. The current action allowlist is deliberately limited and every action needs consent. See [docs/ASSISTANT_FEATURES.md](docs/ASSISTANT_FEATURES.md).

## Get started (Windows 10/11 x64)

1. Open [GitHub Actions](https://github.com/isikkeskin1/empty-casing/actions), choose a successful **Windows build and installer** run, download the **Capri-Setup** artifact, extract its ZIP and open Capri-Setup.exe. Verify the source and provenance before running unsigned software.
2. The first-launch **CAPRI // SETUP** wizard opens. Choose LOCAL — Ollama or an optional cloud provider.
3. **Local:** click DOWNLOAD OLLAMA to visit the official download page, install Ollama, then click DOWNLOAD LOCAL MODELS for the text and vision models (defaults qwen3:4b and qwen3-vl:4b).
4. **Cloud:** open the selected provider's official API-key page, paste your key, select the exact model ID, and optionally set a vision-capable model. Providers can charge or impose rate limits.
5. Under HER VOICE, choose a Windows voice or Fish Audio. Fish uses a separate API key and plays MP3 replies without writing audio to disk. Test the selected voice, then click TEST CONNECTION and SAVE & OPEN MAINFRAME.
6. In Mainframe, enable mic, screen watch, speech output or desktop actions separately if desired. Purchasing and checkout remain blocked.

The installer includes .NET and **all seven default character animation states** (separately licensed, noncommercial, credited); it does **not** include Ollama, model weights, or cloud credits. An optional imported local pack overrides the bundled character without deleting your own files. Developer setup is described in [the guide](docs/SETUP.md).

**Smart screen input:** Optional WATCH SCREEN uses a tiny in-memory grayscale signature to skip unchanged or blank frames, and stays quiet during voice listening. Ask "what did you see?" for Capri's latest observation from this session (kept in RAM for up to three hours). Gentle mouse rubs over the companion trigger a playful reaction; disable them in INPUTS.

**Reminders and timers:** Try "remind me in 10 minutes to stretch", "set a timer for 30 seconds", "show reminders" or "cancel reminder #1" in the Mainframe or by voice. Reminders are stored locally and fire while Capri is running (or upon reopening if overdue). They are not cloud calendar events.

**Controls:** drag or quickly toss Capri; double-click to sleep/wake. Shift-drop sits anywhere; Ctrl-drop attaches to a nearby window edge with the existing perch pose. Right-click → Size scales the overlay from 50% to 160% and saves the choice, including positions on secondary monitors. Type **"ride my cursor"** and press **Ctrl+Alt+C** to hop off (or bring her here when she isn't riding). **Ctrl+Alt+H** hides/shows; **Ctrl+Alt+T** focuses chat; **Ctrl+Alt+P** toggles Quiet Mode. Open chat with Ctrl+Shift+Space or the companion context menu. Smooth cursor ride, click-through, and mixed-DPI placement need Windows QA; no global Esc hook, distinct hang frame, or chaos clones have been shipped. Read [interaction parity](docs/INTERACTION_AND_CONNECTION_PARITY.md).

The snap system only reads top-level window rectangles. It does not inspect your app contents or take control of other windows. High-DPI multi-monitor placement and fullscreen game behavior need manual Windows testing.

**Importing the seven animated WebP files:** Run `python -m pip install Pillow`, then `python tools/import_webp_animations.py --input "C:\path\to\webp-folder"`. This creates PNG frame folders and `timing.json` files in `%LOCALAPPDATA%\CapriDesktop\character`. Alternatively, extract the locally supplied converted test ZIP directly into that character folder. Select **Reload character art** in the app. See [assets/README.md](assets/README.md) and [docs/ASSET_RIGHTS.md](docs/ASSET_RIGHTS.md).

## Screen, mic and desktop assistant

Select **LOOK NOW** to inspect the selected monitor once. Mainframe's monitor picker now includes **Follow frontmost app** and its watch interval can be set between 10 seconds and 10 minutes. For proactive remarks, opt into **WATCH SCREEN** in the Mainframe; screenshots go to your **selected local or cloud brain** (cloud requires consent), are not saved, and stop when you disable the toggle or exit the app. Reactions appear briefly over Capri. All monitoring starts off each launch.

**Fish voice:** Open SET UP BRAIN / VOICE, choose CLOUD — Fish Audio, and enter a newly generated Fish API key. The free S2.1 model is a limited developer offer; selecting Pro may incur charges. An optional voice reference ID must be appropriately licensed. Spoken text is transmitted to Fish; MP3 playback remains in memory. **MIC ON** uses Microsoft's installed Windows speech recognizer, not a cloud transcription endpoint provided by this app. Say "Capri, what's on my screen?" (while WATCH SCREEN is on) or "Capri, open notepad" (with DESKTOP ACTIONS enabled). Spoken recognition depends on the language pack you have installed. **SPEAK REPLIES** uses your system voice; it isn't an imitation of a voice actor.

**DESKTOP ACTIONS** currently accepts a small allowlist (notepad, calculator, explorer, edge, chrome, VS Code, Spotify, Discord, Steam, HTTPS web links, music controls). It asks you to confirm **each action** and supports no purchase or payment flow. Screen text never becomes a command. Do not treat this as unrestricted desktop control.

Read [docs/ASSISTANT_FEATURES.md](docs/ASSISTANT_FEATURES.md) for exact supported behavior, limitations and privacy details.

## Explicit file reading (text and code)

Press **READ FILE** in Mainframe, select one local text/code file, and optionally
type a question in the chat box. Capri reviews at most the first 12,000
characters, with a 128 KiB file-size cap, strict UTF-8 and basic credential
detection. No drive indexing, directories, executables, PDFs or arbitrary
file writes. If the brain is cloud-based, Capri asks separately before
transmitting the selected excerpt.

File-derived answers appear only in the current session and are not
automatically sent in future chat requests, saved as ordinary local history,
or spoken aloud through Fish Audio. The selected filename/question are
stored as a normal chat entry. This is a *read-only* operation.
See [FILE_REVIEW](docs/FILE_REVIEW.md).

## Context Sense — lightweight ambient awareness

Mainframe → **CONTEXT SENSE · APP + MUSIC** lets Capri reference the
active external application and current media title/artist when she replies,
without the user explaining which game or song is open every time. Unlike
WATCH SCREEN, this mode never captures pixels; it reads only a short app
process name and local Windows Now Playing metadata, on demand during a
normal chat reply. No background scanning or prompt-history storage.

This is opt-in because the tiny context note is sent along with the normal
chat request to your configured brain, including a cloud provider if one
is selected. It is inserted into a temporary copy of the latest user
message and framed as **untrusted observations**, not executable commands.
STOP ALL disables and unsaves context sharing. When off, ordinary replies
do not gather or transmit the app/music hint.

You can ask "what app am I in?" to read the foreground external app's
process name directly and locally, without turning on continuous sense
or sending a screenshot. This is not mind reading or game-memory access.

## Mainframe v2 — character-first desktop interface

The Mainframe now treats the animated Capri character as the visual centerpiece instead of placing her in a 308-pixel thumbnail panel. Its main conversation page uses a responsive 44/56 split for a full-height character stage and a quieter, readable transcript. The stage uses the existing seven-frame-state art, updating her mood (idle, thinking, talking, error, sleeping, perching, music) alongside the floating companion; the **presence** indicator does not falsely claim the AI brain is online.

Other improvements: compact rail navigation instead of five repetitive outlined buttons, a single input area with visible placeholder, STOP REPLY next to the composer, first-conversation starter prompts, and mic/screenshot shortcuts beneath the character. Every action keeps its original handler, confirmation and privacy policy. The owner's art override and automatic GitHub updates remain unchanged.

The interface compiles and ships through Windows CI, but actual visual spacing, DPI and animations still warrant a Windows desktop check.

## CAPRI / NEON NOIR MAINFRAME & DEFAULT ANIMATION

The current Windows installer opens with an actual animated Capri character, without the old empty ART SLOT or first-time ZIP picker. All seven states are built into the installed `character-default/` folder with their separate license and copyright notices. Any private frames in `%LOCALAPPDATA%\\CapriDesktop\\character` override the shipped states automatically, and survive updates.

The Mainframe now uses a wider chat-plus-portrait composition; animated character art remains visible next to streaming chat, with high-contrast magenta, cyan and lime accents, grouped modules, better typography and a persistent message composer. The setup and shared control theme use the same design system.

## Capri 0.7 — real character animations + GitHub auto-updates

**No ZIP import needed for the normal installer.** The Windows build now converts all seven animation WebPs from a pinned upstream revision, retains the upstream PolyForm Noncommercial license and notices, and ships the PNG frames under `character-default/`. The floating character and the rebuilt Mainframe stage animate from those files on first launch. Previously imported custom animations still take priority; the ZIP importer remains available to replace your local override. The Mainframe now features a dedicated animated character stage, readable split-screen conversation, restrained neon-noir surfaces, and a more consistent setup flow. See [Animation assets](assets/README.md) and [Animation rights](docs/ASSET_RIGHTS.md).

**GitHub auto-release and direct updater:** Every successful push to `main` automatically publishes a numbered Windows GitHub Release with installer, portable ZIP and SHA-256. Capri now checks the pinned official release feed **8 seconds after startup and every 10 minutes**, and downloads/verifies new installers automatically. On a normal installed copy, the already verified installer **runs on Capri exit** or **when your computer has been idle for three minutes**, the update has been staged at least one minute, and the foreground is Capri or the Windows desktop. It never restarts during active model replies, voice, gaming/OBS, code-agent monitoring, screen interaction, or unsent message drafts. The existing **Restart to update** option still works immediately. Both automatic checking and unattended installation can be disabled in **About / Updates**. Portable copies aren't silently overwritten.

**Bootstrap limitation:** Versions released before this fix only stage the update; install the first fixed release once via the existing `Restart to update` button (or its official installer). After that, future releases can install automatically. The current unsigned per-user installer is SHA-256 verified against official GitHub metadata; that integrity check is not the same as code signing.

This replaces the previous manual-only update experience. The app still lacks a code-signing certificate and tested rollback, so integrity checks are **not** equivalent to a publisher signature. See [Release, update and diagnostics guide](docs/RELEASES_AND_DIAGNOSTICS.md).

## Screen Sketch — draw over your desktop

Capri can now open an interactive **Screen Sketch Mode** from **Game Center → SCREEN SKETCH**, **Apps & Tools → DRAW ON SCREEN**, or `Capri, draw on screen`. After a confirmation warning, use three neon pen colors, undo, clear, and an explicit **SAVE PNG** that exports only strokes, without capturing the screen. ESC, EXIT, STOP ALL and a three-minute timer always close the drawing surface. Unlike the click-through Game Coach HUD, the temporary sketch window deliberately captures mouse clicks while you draw, so do not leave it active during gameplay.

The overlay is a regular WPF window best suited to desktop/borderless apps, not anti-cheat overlays or exclusive fullscreen. See [Screen Sketch guide](docs/SCREEN_SKETCH.md) for safety and compatibility.

## Your Memory Vault + in-game coach HUD

Capri now has a real **Memory Vault** under **Control Deck → 05 My memory**. You can explicitly save, edit, forget, export, and clear short personal notes. Voice/chat commands include `remember that I prefer short answers`, `list memories`, and `forget memory #1`. No memory is mined from conversations, files or screenshots automatically. **Use my saved notes in AI replies** is OFF by default; cloud providers require separate opt-in before notes are included in normal chat prompts. Notes are plain local JSON; don't store secrets.

In the Steam Game Center, **GAME HELP (SCREEN)** can now respond through a **30-second cyberpunk click-through Game Coach HUD**, keeping your borderless game focused. It is an AI tip based on one chosen screenshot, not telemetry or game automation. Cloud screenshot help requests permission before sending the image. The HUD can be hidden and never captures gameplay continuously.

See [Memory Vault and Game HUD guide](docs/MEMORY_AND_GAME_HUD.md) for exact file paths, deletion limits, security and platform notes.

## Live model replies — incremental text and early Windows voice

Capri now displays **streaming chat tokens** as the model generates them. Local Ollama uses newline-delimited chat chunks; supported OpenAI-compatible cloud brains use Chat Completions server-sent events. This applies to normal assistant conversations, not action plans or screen screenshots. Partial text is an ephemeral UI preview: only successfully completed answers are saved to history.

For Windows TTS, Capri can **begin speaking complete sentences** before the full model reply has finished; the next sentence plays after the previous one. Toggle this under **INPUTS → Speak Windows voice sentences while the AI is still replying** (on by default). Fish Audio still uses complete MP3 synthesis and remains interruptible, but is not streaming voice. STOP REPLY / Talk Once / STOP ALL discard pending text and queued sentence audio; external operations already dispatched are not rolled back.

See [Voice conversation guide](docs/VOICE_CONVERSATION.md). This is streamed **model text** with early Windows TTS, not continuous full-duplex audio or streaming microphone recognition.

## Voice Conversation v2 — interrupt, rephrase, stay in-game

Capri's existing Windows speech recognizer and optional Fish Audio voice can now be controlled more naturally. **STOP REPLY** in Control Deck or **Ctrl+Alt+R** cancels the current chat/vision generation and audio playback *without* disarming desktop inputs. With separately opted-in spoken interruptions, **"Capri, cancel that"** does the same. **TALK ONCE** now interrupts an old answer before starting one new capture; if you rephrase while the model is thinking, only the newest recognized question is retained.

The optional 20-second wake-free follow-up window is refreshed **after Capri finishes speaking**, not just when you initially asked. Spoken requests also **avoid popping the chat window above a game by default**; choose "Open the full chat window for spoken requests" in INPUTS if you prefer visible conversations. When speech output is disabled, the full chat opens so your answers are visible.

These changes still use whole-utterance Windows recognition and whole-answer audio: **not** full-duplex low-latency streamed voice or acoustic echo cancellation. Voice-interrupt recognition remains opt-in to reduce speaker-loopback false triggers. STOP ALL continues to revoke every sensitive permission; canceling a reply cannot undo an already dispatched external action. See [Voice Conversation guide](docs/VOICE_CONVERSATION.md).

## Instant game clips via OBS Replay Buffer

Capri now supports real video highlights through a **local, authenticated OBS Studio WebSocket 5 connection**. In Control Deck → Apps & Tools → Game Clips, save your OBS WebSocket password (Windows DPAPI encrypted), arm clip controls for this session, start/check the OBS Replay Buffer and press **SAVE CLIP**, or say **“Capri, clip that.”** The clip is saved by OBS to the folder you configured. You can also locate the last file in Explorer.

OBS must be installed, configured and running; Capri cannot capture moments from before replay buffering started. This integration never livestreams, uploads, switches scenes or runs arbitrary OBS requests. STOP ALL disarms the link. See [OBS game clipping guide](docs/GAME_CLIPS_OBS.md).

## Code/Husk Mode — follow Codex and Claude Code work

Capri can now act as a quiet, read-only companion while **local Codex CLI or Claude Code** runs. In Control Deck → **Apps & Tools → Code/Husk**, install the respective user-level hooks with an explicit review prompt and automatic config backup, restart the coding assistant, then turn **Watch coding assistants** on for the session.

She can react to prompts, approvals needed, turn completion, tool errors, subagent starts and context compaction when the installed CLI emits the corresponding hooks. The event feed contains **only known event names and timestamps**, never prompts, code, tool arguments or file contents, and isn't sent to a model. Capri never approves actions on your behalf. **STOP ALL**, quitting Capri or turning the watcher off disables the short-lived local event relay; uninstall buttons remove only Capri's hook handlers.

See [Code/Husk Mode guide](docs/CODE_HUSK.md) for exact file locations, setup, event availability, privacy and rollback.

## Visual Lookout, Quiet Mode and calendar reminders

**Watch for a visible condition.** Enable WATCH SCREEN in Senses & Control (and approve the cloud screenshot disclosure if applicable), then enter `my health is low` or `the download finishes` into Visual Lookout. Or say `Capri, tell me when my health is low`. Capri uses the selected vision model to assess changed frames; a possible match sends a local alert and automatically disarms the lookout. `Cancel lookout`, STOP ALL, or the two-hour session limit ends it.

**Quiet Mode** suppresses unsolicited visual comments and speech while retaining manually requested tools and reminder tray notifications. It is a per-session switch, not a permission grant. Try `Capri, quiet mode` and `quiet off`.

**Calendar reminders:** `remind me on 5 December at 9am to call Mum` or `remind me tomorrow at 18:30 to exercise`. Dates and clocks use the local PC time zone. Reminders persist and overdue items trigger when Capri is next opened; this is not a background OS alarm.

See [Visual Lookout and calendar guide](docs/LOOKOUT_AND_CALENDAR.md) for consent details, limits and examples.

## Capri Control Deck — cyberpunk UI refresh

The desktop Mainframe now uses a consistent Capri-inspired **dark / neon-yellow / cyan / pink** visual language, guided by the owner's reference video. The chat-first layout has a permanent composer, accessible navigation rail, large routine cards, grouped opt-in controls, and a dedicated apps/tools page. Capri's floating desktop character has a small angular speech card with **TALK TO ME** and dismiss actions; the first button activates Talk Once or opens chat if Windows speech isn't configured.

Find the reusable palette and control templates in `src/Capri.Desktop/Themes/CapriTheme.xaml` and read [Control Deck UI guide](docs/CONTROL_DECK_UI.md) for workflow, accessibility and limitations. This is independent WPF code and styling. The seven noncommercial upstream character animations are now separately bundled with explicit credit and original license notices; video footage is not included.

## Session routines and conversation follow-ups

Capri can now handle an entire local app-launch session in one voice/text command:

- **"Capri, gaming mode"** opens Discord, Steam and Spotify.
- **"Capri, Roblox mode"** opens Discord, Roblox home and Spotify.
- **"Capri, Roblox dev mode"** opens the Creator Dashboard, VS Code and Spotify.
- **"Capri, study mode"** opens VS Code, Notepad and Spotify.
- **"Capri, open Discord, Steam and Spotify"** runs an exact two-to-four-app custom batch.

Enable DESKTOP ACTIONS or HANDS-FREE first. A manual routine has one batch preview, not a new confirmation for every app. Every step is revalidated, and **STOP ALL** cancels any remaining launches. No scripts, account changes, purchases, messages or file modifications are part of these routines. See [Session Routines](docs/SESSION_ROUTINES.md).

In INPUTS, enable optional voice follow-ups to keep a *sliding* 20-second conversation window open after each accepted follow-up. When voice interruption is also enabled, **"Capri stop all"** works while Capri is speaking. This is not yet streamed voice or always-on unrestricted computer control.

## Native Spotify + Roblox account connections (OAuth PKCE)

Mainframe → **CONNECTIONS** offers **Connect Spotify Account** and **Connect Roblox Account** via each provider's own browser-hosted OAuth authorization. Regular users never paste cookies, tokens, or developer client IDs. Capri's publisher must register the OAuth apps ONCE, whitelist the loopback callback URLs and set the non-secret GitHub repository variables `CAPRI_SPOTIFY_CLIENT_ID` and `CAPRI_ROBLOX_CLIENT_ID` for a consumer release. Until those public app IDs are configured and authorized by the providers, account linking is unavailable; the app explains this without pretending sign-in succeeded. An optional Advanced area supports developers with their own app registrations.

Without OAuth, users can **open Spotify** and use Windows local media sessions for basic playback, and **open Roblox / launch experiences** through official links. Spotify-only local playback checks the active session source to avoid controlling a different player. Full Spotify search/queue/profile and Roblox profile still require account authorization. Capri uses PKCE, random state and Windows DPAPI-encrypted OAuth credentials when linked.

**Spotify:** connected account profile, active Spotify Connect playback,
track search, play a selected track, pause/resume/skip via actual Spotify
Web API endpoints. Commands include "spotify now", "spotify next" and
"spotify play [song title]". Search ambiguities require selecting the
recording in CONNECTIONS. Most player APIs require Premium. In Spotify
2026 Development Mode, the app owner needs Premium and an allowlist applies.

**Roblox:** connect through Roblox Open Cloud OAuth to read your linked
profile/username. Say "roblox profile" or "join roblox [numeric place ID]"
to open a validated experience launch link. Roblox Open Cloud **does not
allow third-party apps to remotely control arbitrary in-game actions**.
Capri's Game Help vision is the route for gameplay advice, and official
creator APIs can be added separately by granular scoped permissions.

Read [Connections guide](docs/CONNECTED_ACCOUNTS.md). Purchases, payments,
Robux spending, sending messages and writing games are never authorized
by account linking or hands-free mode.

## Voice Call Companion (experimental, explicitly disclosed AI)

Mainframe → **VOICE CALL** configures an opt-in spoken companion over the
existing call application (Discord, Zoom, etc.). The call must be joined
by the account owner in that app. Select a Fish Audio voice and a non-default
output device connected to a virtual microphone cable in INPUTS. After
informing other call participants and confirming consent, START CALL MODE
listens to Windows default playback through local speech recognition and
responds only to phrases addressed to "Capri". Her first spoken line identifies
her as an AI companion. Call speech is *never* routed to desktop-command
planning, purchases, or prior private chat context.

Call audio PCM stays local and is not recorded. Addressed recognized text
may be sent to your configured cloud AI, and the reply is sent to Fish
Audio for synthesis. STOP CALL MODE, closing the panel, or STOP ALL closes
the loopback capture, cancels the task and stops speech. This does **not**
auto-join a Discord channel, impersonate you, or silently record others.
See [Call Mode](docs/CALL_COMPANION.md).

## Now Playing — song/video awareness and controls

Use **NOW PLAYING** in Mainframe or ask "what song is playing?" / "what
video is playing?" to read the current Windows media session (Spotify,
Chrome/Edge, or other apps that publish System Media Transport Controls).
Capri shows available title/artist/source information, and can pause,
resume, skip next and skip previous through the session. These
**commands are allowed only with DESKTOP ACTIONS enabled**; HANDS-FREE
removes repeated confirmation prompts.

When DANCE TO AUDIO is enabled, Capri keeps her existing local sound
energy/rhythm reactions and checks the Windows media session every
10 seconds for a track change. She can show a brief local "Now vibing"
reaction on song changes. The song title and PCM never go to a cloud
model for this feature. Media support depends on the playback app
exposing a Windows media session. See [Now Playing](docs/NOW_PLAYING.md).

## Game Center — installed Steam games and screenshot help

Open **GAMES** in Mainframe to search your installed Steam library and launch
a title through the local `steam://rungameid/<appid>` protocol. Capri discovers
Steam installations from Windows registry/default paths, then reads
`steamapps/libraryfolders.vdf` and installed `appmanifest_*.acf` records.
No web scraping, game downloads, code injection, arbitrary EXEs or hidden launch
options. **DESKTOP ACTIONS** must be enabled; HANDS-FREE skips launch confirmation
for matched installed games. You can also type/say "list games" or
"play [unique installed game name]".

Press **GAME HELP** while playing (or say "Capri, game help: ...") for a
single-frame explanation and actionable advice. Screen analysis uses the
selected vision model. **Cloud vision always asks before uploading a screenshot,**
even with HANDS-FREE enabled; local Ollama processes it locally. Advice is
based only on visible pixels, not game-memory reading, cheats, botting, or
anti-cheat bypass. See [Game Center](docs/GAME_CENTER.md).

## Hands-free mode (no approval popups for supported desktop actions)

Mainframe → **HANDS-FREE · NO POPUPS** is an opt-in setting. Turning it on
also arms **DESKTOP ACTIONS** in the current session. Capri will immediately
execute *supported and locally validated* desktop requests such as opening
allowlisted apps/HTTPS links, sending media controls, selecting verified tabs,
and navigating accessible controls in an approved AUTO TASK **without**
per-click or per-task approval dialogs.

The preference is saved. However **DESKTOP ACTIONS always starts disarmed**
after a restart, so re-arm it to resume hands-free commands. The prominent
**STOP ALL** button (or global STOP INPUTS hotkey) disables active actions and
ends any running autonomous task. Turning HANDS-FREE off restores confirmations.

**Not a permission to do literally anything:** no checkout, purchases, money
transfers, credential/account operations, arbitrary shell commands, Send/Save/
Delete buttons, mass file changes, or any unsupported actions. External file
sharing and screen/audio recording permissions keep their separate consent
controls. Hands-free cloud tasks automatically share only the task goal,
window title and eligible navigation labels with the configured brain.

## Autonomous desktop task sessions

The **AUTO TASK** button in Mainframe launches a scoped AI navigation task using
the goal typed in the chat prompt. Choose/refresh the target window in WINDOW
CONTROLS, then approve the task **once**. Capri inspects the approved application's accessible tabs, expandable
navigation trees and a small allowlist of navigation buttons, then uses your
local/cloud model to choose each verified step. It can operate those controls
without stopping for per-click approval. The run lasts at most **5
selections / 90 seconds**, and never changes to a different window.
**STOP TASK** or global **STOP INPUTS** cancels the remaining task.
Cloud mode explicitly discloses that the window title, tab labels and goal
will be sent to the chosen provider.

The navigation-button allowlist is deliberately narrow (for example Back,
Home, Help, Menu and Overview); it excludes generic Save, Send, Submit,
Next, checkout and other risky buttons. The agent cannot type, change
settings, delete files, buy anything or invoke arbitrary UI controls. It is a real model-guided observe/plan/act/reobserve
loop but not yet an unrestricted desktop agent. See
[Autonomous task safety](docs/AUTONOMOUS_TASKS.md).

## Windows control inspector (UI Automation v1)

Open **WINDOW CONTROLS** in Mainframe or **Inspect window controls** in the tray menu.
Capri reads only accessible *tab names* from the topmost external application, not
typed text, passwords or form values. After enabling DESKTOP ACTIONS for the current
session, choose a safe tab. Normally it needs one confirmation, but the
HANDS-FREE toggle skips that confirmation. Alternatively type
"inspect current window" or "switch to the Settings tab". The target is checked
again immediately before selection and expires after 20 seconds.

**This is not unrestricted computer use.** It cannot click arbitrary buttons,
type text, send messages, manipulate files, interact with checkout or payments,
or execute model-suggested commands from a webpage. App tabs without UIA
accessibility metadata may not be discoverable. See
[WINDOW_CONTROLS](docs/WINDOW_CONTROLS.md).

## Optional music and system-audio reactions

In Mainframe, **DANCE TO AUDIO** lets Capri react to the sound level and beats
of Windows playback. This is a separate opt-in each time the app launches:
system output can include calls, videos and games. Audio is reduced immediately
to numeric energy/pulse estimates, with no files saved or audio uploaded.
Capri temporarily uses her music-perch animation while playback is audible;
she stops once sound has been quiet for two seconds. This is not song
identification or microphone recording.

## Voice conversation controls

**TALK ONCE** in Mainframe or Ctrl+Alt+Space opens the selected microphone for one
utterance, for up to 20 seconds. You don't need to repeat "Capri" in that turn.
If the mic was previously off, it closes automatically after the turn or timeout.
For continuous voice commands, keep MIC ON and start requests with "Capri" or
"Hey Capri". Voice recognition remains local to installed Windows speech packs.

Input Control Center can opt into a 20-second follow-up window after addressing
Capri, and into the spoken "Capri, stop" interruption command. Both are OFF by
default, since speaker output and other people nearby can be mistakenly recognized.
The **STOP VOICE** button halts speech; **STOP INPUTS** also shuts down mic,
screen watching and desktop-action permissions. See [voice protocol](docs/VOICE_CONVERSATION.md).

## Input Control Center

Open INPUTS in Mainframe or Input settings / hotkeys in the tray. Choose a real microphone, test its level for five seconds, pick Fish Audio's speaker and customize eight global shortcuts. Mic mute stops capture; STOP INPUTS ends mic, screen and speech and disables desktop actions. See [Input architecture](docs/INPUT_SYSTEMS.md).

## Build and verify

~~~powershell
dotnet build src/Capri.Desktop/Capri.Desktop.csproj -c Release
dotnet run --project tests/Capri.Core.Smoke/Capri.Core.Smoke.csproj -c Release
dotnet publish src/Capri.Desktop/Capri.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
~~~

## Security and privacy

By default, screen watching, mic recognition and desktop actions are OFF. On request, local screen images are transmitted only to the Ollama server bound at 127.0.0.1, and image bytes are discarded after inference. Microphone input uses Windows' installed recognizer; recognition results may be stored as chat text once a "Capri" request is accepted. Spoken replies use Windows speech output. Approved app/web/media actions require confirmation and cannot run arbitrary shell commands. The window-perching feature reads window coordinates and minimized state only. There is no telemetry. Text entered in chat is sent to your **local Ollama endpoint (127.0.0.1:11434)**. Settings and transcript are plain JSON in %LOCALAPPDATA%\CapriDesktop. Do not paste credentials or sensitive data into chat. The app doesn't expose model controls to other computers.

For third-party artwork restrictions see [docs/ASSET_RIGHTS.md](docs/ASSET_RIGHTS.md). For architecture and realistic delivery stages, see [docs/ROADMAP.md](docs/ROADMAP.md). For scope and contribution practices, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Rights

This is not affiliated with CD PROJEKT RED, Netflix, the show's production team or voice performers. Capri and related universe/character elements belong to their respective owners. The independent **source code** is MIT licensed (see [LICENSE](LICENSE)); the bundled animation art remains under PolyForm Noncommercial 1.0.0 (attribution included with the installer) and is **not** MIT licensed. This source license does not grant commercial rights in characters, trademarks, recorded voices, artwork or third-party assets. Do not sell or include the art in a commercial product without obtaining appropriate permissions.
