# Capri Desktop — independent Windows companion

An **unofficial, noncommercial fan project** building an interactive desktop companion inspired by Capri from *Cyberpunk: Edgerunners 2*.

This is an **independent implementation**. It does not fork or reuse code, animations, sprites, audio, or other assets from lachydotmcg/capri-desktop. No copyrighted character art or voice recordings are bundled. This early build uses a temporary hologram until appropriately licensed/created Capri animation frames are available.

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

The installer includes the .NET runtime, **not** Ollama, model weights, copyrighted animation assets, or cloud credits. Developer setup is described in [the guide](docs/SETUP.md).

**Smart screen input:** Optional WATCH SCREEN uses a tiny in-memory grayscale signature to skip unchanged or blank frames, and stays quiet during voice listening. Ask "what did you see?" for Capri's latest observation from this session (kept in RAM for up to three hours). Gentle mouse rubs over the companion trigger a playful reaction; disable them in INPUTS.

**Reminders and timers:** Try "remind me in 10 minutes to stretch", "set a timer for 30 seconds", "show reminders" or "cancel reminder #1" in the Mainframe or by voice. Reminders are stored locally and fire while Capri is running (or upon reopening if overdue). They are not cloud calendar events.

**Controls:** drag the floating character to move it; double-click to open chat; Ctrl+Shift+Space toggles chat. **Drop near another app's top edge** to perch on its window; she follows that window while it moves and detaches if it's minimized or closed. **Flick her quickly** to toss her around the screen with damped bouncing. Right-click her or use the tray to select **Perch on frontmost window**, **Detach from window**, or any manual animation preview. Music-perch is still a visual preview; beat synchronization isn't implemented.

The snap system only reads top-level window rectangles. It does not inspect your app contents or take control of other windows. High-DPI multi-monitor placement and fullscreen game behavior need manual Windows testing.

**Importing the seven animated WebP files:** Run `python -m pip install Pillow`, then `python tools/import_webp_animations.py --input "C:\path\to\webp-folder"`. This creates PNG frame folders and `timing.json` files in `%LOCALAPPDATA%\CapriDesktop\character`. Alternatively, extract the locally supplied converted test ZIP directly into that character folder. Select **Reload character art** in the app. See [assets/README.md](assets/README.md) and [docs/ASSET_RIGHTS.md](docs/ASSET_RIGHTS.md).

## Screen, mic and desktop assistant

Select **LOOK NOW** to inspect the selected monitor once. Mainframe's monitor picker now includes **Follow frontmost app** and its watch interval can be set between 10 seconds and 10 minutes. For proactive remarks, opt into **WATCH SCREEN** in the Mainframe; screenshots go to your **selected local or cloud brain** (cloud requires consent), are not saved, and stop when you disable the toggle or exit the app. Reactions appear briefly over Capri. All monitoring starts off each launch.

**Fish voice:** Open SET UP BRAIN / VOICE, choose CLOUD — Fish Audio, and enter a newly generated Fish API key. The free S2.1 model is a limited developer offer; selecting Pro may incur charges. An optional voice reference ID must be appropriately licensed. Spoken text is transmitted to Fish; MP3 playback remains in memory. **MIC ON** uses Microsoft's installed Windows speech recognizer, not a cloud transcription endpoint provided by this app. Say "Capri, what's on my screen?" (while WATCH SCREEN is on) or "Capri, open notepad" (with DESKTOP ACTIONS enabled). Spoken recognition depends on the language pack you have installed. **SPEAK REPLIES** uses your system voice; it isn't an imitation of a voice actor.

**DESKTOP ACTIONS** currently accepts a small allowlist (notepad, calculator, explorer, edge, chrome, VS Code, Spotify, Discord, Steam, HTTPS web links, music controls). It asks you to confirm **each action** and supports no purchase or payment flow. Screen text never becomes a command. Do not treat this as unrestricted desktop control.

Read [docs/ASSISTANT_FEATURES.md](docs/ASSISTANT_FEATURES.md) for exact supported behavior, limitations and privacy details.

## Input Control Center

Open INPUTS in Mainframe or Input settings / hotkeys in the tray. Choose a real microphone, test its level for five seconds, pick Fish Audio's speaker and customize seven global shortcuts. Mic mute stops capture; STOP INPUTS ends mic, screen and speech and disables desktop actions. See [Input architecture](docs/INPUT_SYSTEMS.md).

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

This is not affiliated with CD PROJEKT RED, Netflix, the show's production team or voice performers. Capri and related universe/character elements belong to their respective owners. The independent **source code** in this repository is MIT licensed (see [LICENSE](LICENSE)); that does **not** grant rights to character art, names, trademarks, recorded voices, or other third-party material. Public distribution of a character-faithful build will require a rights review. Commercial distribution would require appropriate permissions.
