# Capri Desktop — independent Windows companion

An **unofficial, noncommercial fan project** building an interactive desktop companion inspired by Capri from *Cyberpunk: Edgerunners 2*.

This is an **independent implementation**. It does not fork or reuse code, animations, sprites, audio, or other assets from lachydotmcg/capri-desktop. No copyrighted character art or voice recordings are bundled. This early build uses a temporary hologram until appropriately licensed/created Capri animation frames are available.

## Current milestone: local assistant + desktop presence

- Transparent, always-on-top, draggable companion with window-edge snapping, follow-on-move, detach on minimize/close and bounded toss motion
- Optional user-provided PNG sequences for idle, talk, glitch, sleep, toss, perch and music-perch; individual frame timing supported
- Local chat powered by Ollama, with an editable model name
- Local JSON history and settings, including saved companion position
- Tray menu, show/hide shortcut (Ctrl+Shift+Space), and opening the animation folder
- Screen questions: **LOOK NOW** explicitly captures the primary display and sends a JPEG to a local Ollama vision model
- Opt-in watch mode: capture every 45 seconds and show brief AI reactions as a temporary character bubble
- Windows microphone recognition: toggle MIC ON, then start a request with **"Capri"**; native Windows text-to-speech output can be enabled separately
- Approval-gated desktop actions: launch selected applications, open HTTPS pages and send media keys. No purchases, arbitrary clicks, command execution, typing, deletion or messaging
- Windows CI which compiles, runs smoke checks and produces a downloadable Windows build artifact

**Not yet implemented:** seamless streamed voice conversation, character-specific voice, multi-monitor vision selection, game-event understanding, Discord call participation, general computer use, semantic memory or an installer. Current speech uses the Windows-installed recognizer and voice; current vision is a **single primary-monitor snapshot**, not a continuous video feed. The current action allowlist is deliberately limited and every action needs consent. See [docs/ASSISTANT_FEATURES.md](docs/ASSISTANT_FEATURES.md).

## Get started (Windows 10/11)

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Ollama](https://ollama.com/download/windows). Then in PowerShell:

~~~powershell
ollama pull qwen3:4b
ollama pull qwen3-vl:4b
dotnet run --project src/Capri.Desktop/Capri.Desktop.csproj
~~~

If Ollama is not running, start the Ollama Windows app. The text model and vision model in the mainframe must both match models installed locally in Ollama. Screen questions require a vision-capable model.

Alternatively, use a successful run's artifact from **Actions > Windows build**, extract it and run Capri.Desktop.exe. The CI build is self-contained, but the local AI server and model are not bundled.

**Controls:** drag the floating character to move it; double-click to open chat; Ctrl+Shift+Space toggles chat. **Drop near another app's top edge** to perch on its window; she follows that window while it moves and detaches if it's minimized or closed. **Flick her quickly** to toss her around the screen with damped bouncing. Right-click her or use the tray to select **Perch on frontmost window**, **Detach from window**, or any manual animation preview. Music-perch is still a visual preview; beat synchronization isn't implemented.

The snap system only reads top-level window rectangles. It does not inspect your app contents or take control of other windows. High-DPI multi-monitor placement and fullscreen game behavior need manual Windows testing.

**Importing the seven animated WebP files:** Run `python -m pip install Pillow`, then `python tools/import_webp_animations.py --input "C:\path\to\webp-folder"`. This creates PNG frame folders and `timing.json` files in `%LOCALAPPDATA%\CapriDesktop\character`. Alternatively, extract the locally supplied converted test ZIP directly into that character folder. Select **Reload character art** in the app. See [assets/README.md](assets/README.md) and [docs/ASSET_RIGHTS.md](docs/ASSET_RIGHTS.md).

## Screen, mic and desktop assistant

Select **LOOK NOW** to inspect the primary monitor once. For proactive remarks, opt into **WATCH SCREEN** in the Mainframe; screenshots go only to local Ollama, are not saved, and stop when you disable the toggle or exit the app. Reactions appear briefly over Capri. All monitoring starts off each launch.

**MIC ON** uses Microsoft's installed Windows speech recognizer, not a cloud transcription endpoint provided by this app. Say "Capri, what's on my screen?" (while WATCH SCREEN is on) or "Capri, open notepad" (with DESKTOP ACTIONS enabled). Spoken recognition depends on the language pack you have installed. **SPEAK REPLIES** uses your system voice; it isn't an imitation of a voice actor.

**DESKTOP ACTIONS** currently accepts a small allowlist (notepad, calculator, explorer, edge, chrome, VS Code, Spotify, Discord, Steam, HTTPS web links, music controls). It asks you to confirm **each action** and supports no purchase or payment flow. Screen text never becomes a command. Do not treat this as unrestricted desktop control.

Read [docs/ASSISTANT_FEATURES.md](docs/ASSISTANT_FEATURES.md) for exact supported behavior, limitations and privacy details.

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
