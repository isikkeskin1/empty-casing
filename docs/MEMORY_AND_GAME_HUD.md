# Memory Vault + Game Coach HUD

## Memory Vault: only what the owner explicitly saves

Open **Control Deck → 05 My memory**. You can add a short factual note, select it to edit or delete, export notes to a chosen text file, or use **FORGET EVERYTHING** with a confirmation.

Capri does **not** automatically store chat summaries, mic transcripts, game screenshots, files or contacts as long-term memory. Chat history and saved memory are separate. The vault is capped at **40 notes of 240 characters each** and refuses likely tokens/credentials, markup, instruction-like content and URLs. Existing notes can be exported as plaintext by the owner. These are convenience filters, not a comprehensive data-loss-prevention system. **Do not save passwords, authentication tokens, account secrets or sensitive private data in notes.**

The saved file is `%LOCALAPPDATA%\CapriDesktop\personal-memories.json`. It is local plaintext like the existing chat history, **not encrypted**. Users with access to your Windows profile may be able to read it. Deleting notes overwrites the active vault file but does not erase old OS backups or data that was previously sent to cloud providers.

### Memory and AI sharing

**Use my saved notes in AI replies** is OFF by default. Enabling it provides a bounded excerpt of saved notes only to normal conversations, not to the desktop action planner, screenshots, gaming APIs or system commands. The notes are framed as reference data, not instructions, and injected only into a temporary copy of the final user prompt. They do not rewrite your saved conversation history.

When using a cloud model, enable sharing only after reviewing a **provider-specific confirmation**. If you switch to a cloud provider that hasn't been approved, the memory context is paused; changing providers never silently starts sharing notes. Turning memory off stops sharing on subsequent replies. It cannot retract context already processed by a provider.

You can also type or speak simple explicit commands:

- `remember that I prefer short, clear answers`
- `list memories` / `what do you remember`
- `forget memory #2`

For safety, **forget everything** is a confirmed UI-only action. Numbering follows the displayed order and may change after deletions.

## Game Coach HUD: one-shot guidance over your game

The existing Steam Game Center now has **Display game tips in a 30-second click-through HUD**, **GAME HELP (SCREEN)** and **HIDE COACH HUD** controls. The default is ON for **explicitly requested** game help only. Game Center no longer opens the whole chat window when requesting a tip; Capri can use a hidden chat host to analyze a single chosen screen frame.

After a successful screenshot-based game-help request, a dark cyberpunk yellow/cyan card appears near the top-right of the main display for approximately 30 seconds. It includes up to 650 characters of advice and **does not take keyboard focus or intercept mouse events**. It automatically hides or can be dismissed through Game Center / STOP ALL. It sends **no game inputs** and doesn't inspect game process memory or game files.

Screenshots sent to a cloud model for game help now require an **individual yes/no confirmation**, even when they were requested through the Game Center. If you decline, no screenshot is sent for that request. If using local Ollama, image processing stays local.

### Limits to verify

The HUD uses a regular topmost WPF overlay, not an injected game overlay. It is intended for Windows desktop or borderless-windowed games, not guaranteed to appear over exclusive fullscreen or anti-cheat-protected games. Only the primary Windows work area is used for initial placement. Multi-monitor DPI and fullscreen rendering need manual testing. WPF native click-through is configured on source initialization.

The game coach is an **AI estimate based on one screenshot**, not actual game telemetry or autonomous gameplay. The original project's drawing tools, advanced game-automation controls, mod ecosystem, VR support and full-duplex audio remain separate unfinished projects.

## Release validation

The .NET 10 Windows workflow compiles the WPF UI and runs core smoke tests for explicit memory commands, duplicates, persistence, edit/remove/delete-all, credential filtering and temporary prompt injection. It also packages the executable and installer. Manual testing still needs to verify real monitor/window behavior and the visual style on your PC.
