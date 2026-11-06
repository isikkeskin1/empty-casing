# Capri Desktop — independent Windows companion

An **unofficial, noncommercial fan project** building an interactive desktop companion inspired by Capri from *Cyberpunk: Edgerunners 2*.

This is an **independent implementation**. It does not fork or reuse code, animations, sprites, audio, or other assets from lachydotmcg/capri-desktop. No copyrighted character art or voice recordings are bundled. This early build uses a temporary hologram until appropriately licensed/created Capri animation frames are available.

## Current milestone: desktop foundation

- Transparent, always-on-top, draggable companion window with an animated placeholder
- Optional PNG frame sequences for idle, think, reply and error states
- Local chat powered by Ollama, with an editable model name
- Local JSON history and settings, including saved companion position
- Tray menu, show/hide shortcut (Ctrl+Shift+Space), and opening the animation folder
- Windows CI which compiles, runs smoke checks and produces a downloadable Windows build artifact

**Not yet implemented:** finished Capri character art, audio/voice chat, screen perception, game awareness, autonomous desktop control or semantic memory. These are planned features, not claims about the present build.

## Get started (Windows 10/11)

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Ollama](https://ollama.com/download/windows). Then in PowerShell:

~~~powershell
ollama pull qwen3:4b
dotnet run --project src/Capri.Desktop/Capri.Desktop.csproj
~~~

If Ollama is not running, start the Ollama Windows app. The model name in the chat panel must match one installed in Ollama.

Alternatively, use a successful run's artifact from **Actions > Windows build**, extract it and run Capri.Desktop.exe. The CI build is self-contained, but the local AI server and model are not bundled.

**Controls:** drag the floating hologram to move it; double-click to open chat; Ctrl+Shift+Space toggles chat; right-click or use the tray for more actions. To replace the hologram with your own frames, use the tray's **Open character art folder** and see [assets/README.md](assets/README.md).

## Build and verify

~~~powershell
dotnet build src/Capri.Desktop/Capri.Desktop.csproj -c Release
dotnet run --project tests/Capri.Core.Smoke/Capri.Core.Smoke.csproj -c Release
dotnet publish src/Capri.Desktop/Capri.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
~~~

## Security and privacy

v0.1 does **not** capture your screen, listen to the microphone, inspect apps, control your keyboard/mouse or launch commands on behalf of AI. There is no telemetry. Text entered in chat is sent to your **local Ollama endpoint (127.0.0.1:11434)**. Settings and transcript are plain JSON in %LOCALAPPDATA%\CapriDesktop. Do not paste credentials or sensitive data into chat. The app doesn't expose model controls to other computers.

For architecture and realistic delivery stages, see [docs/ROADMAP.md](docs/ROADMAP.md). For scope and contribution practices, see [CONTRIBUTING.md](CONTRIBUTING.md).

## Rights

This is not affiliated with CD PROJEKT RED, Netflix, the show's production team or voice performers. Capri and related universe/character elements belong to their respective owners. The independent **source code** in this repository is MIT licensed (see [LICENSE](LICENSE)); that does **not** grant rights to character art, names, trademarks, recorded voices, or other third-party material. Public distribution of a character-faithful build will require a rights review. Commercial distribution would require appropriate permissions.
