# Local multilingual voice, Discord Rich Presence and AI Chaos

This release adds functioning code paths, not a claim that every reference feature has been proven on a user's Windows hardware.

## 1. Multilingual voice — local Whisper Talk Once

Capri supports a second recognition engine via **Whisper.net 1.9.1** and the official CPU whisper.cpp runtime.

1. Open **INPUTS**. Choose your physical microphone. Press **INSTALL VERIFIED 148 MB WHISPER MODEL**.
2. Read the confirmation: model download comes only from the official `ggerganov/whisper.cpp` Hugging Face repository and is verified against the exact **SHA-256** `60ed5bc3dd14eea856493d334349b405782ddcaf0028d4b5df4088345fba2efe` and byte count before being installed.
3. Set **Recognition Engine = Whisper local — multilingual Talk Once** and Save.
4. Press the existing **Ctrl+Alt+Space Talk Once** shortcut. Capri captures up to six seconds of 16k mono microphone audio **in memory only**, then runs a local inference using automatic language detection. The transcript is treated as an explicit owner-directed voice prompt and passed to Capri's normal conversation UI.
5. STOP ALL cancels capture/inference and blocks a cancelled request from being submitted. The model remains on the owner's PC; it is not bundled in the installer or silently fetched.

The multilingual Whisper *base* model can recognize many languages (the Whisper family supports ~99), but **each language's accuracy and speed depends on speech, noise, PC performance and the model**. We cannot claim empirically verified support in 80 languages without actual multilingual test coverage. The base model is not tuned for every accent/language. This first integration is **press-to-talk only**: continuous hands-free wake phrase and Discord Call Mode still use the specifically selected **installed Windows speech recognizer**. This feature does not claim 80-language live call listening.

Model size ~148 MB. Use Windows 11, an x64 CPU compatible with the native runtime and the Microsoft VC++ Redistributable. On failure the normal Windows recognizer remains available. No raw audio or full recordings are sent to a cloud provider or written to a file.

## 2. AI Chaos — actual local-model replies

Typing **make 3 AI clones**, **make 5 talking clones**, or **let the clones talk** switches Chaos Mode into **local Ollama** dialogue. Clones have distinct fictional personalities and produce a sequence of at most **10 short, bounded, model-generated turns**. Each call includes only a fictional persona and the previous generated line; no saved memory, browsing, private chat messages, passwords, desktop screenshots or user mic audio. Cloud-brain mode refuses AI clones rather than making surprise billable calls. The previous decorative Chaos clones still work without a model.

The generated exchanges use on-screen speech bubbles and **local Windows TTS** through your normal speakers. Different installed Windows voices are selected for different clones where available; the output is sequential rather than spatialized multichannel audio. If Windows TTS fails, caption-only dialogue continues. Quiet Mode mutes their speech, and starting a call stops clone conversations before any call audio capture begins. They cannot issue commands, move the actual cursor or manage other apps. Holding Esc, STOP ALL, game/stream protections, or stopping clones cancels the model loop and closes clone windows. Use the **AI Chaos — LOCAL Ollama talking clones** context menu item as an alternate entrypoint.

## 3. Discord Rich Presence (publisher app registration required)

Capri implements **opt-in local Discord Rich Presence** using Discord's documented `\\.\pipe\discord-ipc-{0..9}` protocol. It handshakes with a **public publisher-owned application ID** (no Discord access token), then sends `SET_ACTIVITY` when enabled to describe states such as idle, sleeping, chatting, call companion and clone chaos. Use the Call Companion **DISCORD STATUS ON/OFF** toggle. The status uses text-only details/state; graphics and animated Discord activity assets require publisher-managed Discord art registration and are not bundled.

**For the Capri publisher:** create a Discord app ONCE in the official Discord Developer Portal and add its PUBLIC application ID as GitHub Actions repository variable `CAPRI_DISCORD_CLIENT_ID`, then publish a new release. Never put a bot token, account token or client secret in build variables. Until the publisher does this, Rich Presence's toggle explains that setup is missing and does not claim to connect.

RPC presence is **not** permission for Discord self-bots, arbitrary DMs, server voice auto-joining or message sending. Discord documents that automating ordinary user accounts outside its supported OAuth/bot APIs is prohibited; server chat/voice automation requires a separately authorized Discord bot/application and app permissions. Existing Call Companion uses user-joined Discord calls with explicit audio routing.

## 4. Remaining limitations and acceptance checks

- Real Windows and Discord acceptance tests remain required; GitHub CI verifies compilation, smoke tests and packaging, not audio recognition, IPC presence or on-screen animation quality.
- The pink task pointer remains **visual only**, with a bounded approved window/UI Automation runner. Unlimited cursor/keyboard control, credential-sensitive actions, payments and destructive operations are **not implemented**. An optional **12-second Chaos window wobble** is now available for one explicitly approved, allowlisted external app window only. It saves the original position, stops on manual repositioning and restores the window when STOP ALL/hold-Esc/timeout fires. It is a small reversible visual prank, **not** general AI window management.
- Discord **text status**, not GIF activity animations, is supported. Local voice call audio works via virtual cable; Capri cannot silently listen to or join any Discord channel by itself.
- Recommended QA: download Whisper once, reboot, test short clips in English/Italian/Turkish, test STOP ALL during inference, test AI clones with Ollama running/unavailable and while a game is focused, check Discord state publishing only after publisher ID setup, and ensure the app auto-update and installer remain functional.
