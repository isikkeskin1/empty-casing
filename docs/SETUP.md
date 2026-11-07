# Capri Desktop setup guide

This is an independent, unofficial fan project. **Do not confuse the original developer's installer with ours.** Download the official latest release from https://github.com/isikkeskin1/empty-casing/releases/latest . Capri's default animation art is third-party and NONCOMMERCIAL, with mandatory attribution.

## Install

Download **Capri-Setup.exe** and the accompanying **SHA256SUMS.txt** from the official GitHub Release, verify the installer digest, and launch it. The installer is currently unsigned: inspect any SmartScreen or antivirus warning instead of assuming it is a false positive. It installs for the current Windows user, without administrator installation privileges, adds a Start menu shortcut and an optional desktop shortcut, and opens the wizard on first run.

The installer includes the self-contained .NET app, an auto-update client, and **seven credited third-party animation sequences** under PolyForm Noncommercial terms. It does not include Ollama, local AI/Whisper model weights, paid API access, audio cable drivers or custom licensed voices. Uninstall leaves settings, history, personal memories and encrypted provider credentials under %LOCALAPPDATA%\CapriDesktop unless you explicitly delete that folder. A previous known-good settings.json.backup may remain for crash recovery; chat/memory/reminder files are not duplicated into retained backups.

Open **About / Updates → RUN READINESS CHECK** to see exactly what is set up and what still requires the publisher or your PC's hardware. This is a local readiness check; it does not upload the support report.

## Discord voice and optional multilingual microphone input

- For local multilingual **Talk Once**, open INPUTS and install the official SHA-256-verified ~148 MB Whisper base model. Select Whisper mode and use the Talk Once shortcut. MIC ON / "Hey Capri" still require a Windows-installed speech recognizer.
- For voice calls, install VB-CABLE from the official VB-Audio website. In CALL COMPANION, choose your Discord speaker output as **Hear Your Call**; choose CABLE Input as Capri's **call-only** speaker; choose CABLE Output as Discord's microphone. Use **TEST DISCORD MIC ROUTE** while Discord's input meter is open. The optional real-mic mix is armed only when you start Call Mode, and both paths stop when Call Mode ends.
- Call audio loopback hears all applications routed to the selected Windows output device; it is not per-Discord-app isolation. Use headphones, avoid a feedback loop, obtain consent from participants, and prefer a dedicated call output device.
- Spotify and Roblox login buttons use official provider browser OAuth, but require a one-time publisher registered app ID in the build. Consumers never need to paste account cookies or access tokens. Discord Rich Presence similarly requires a publisher public Discord app ID.
- This does not automatically join Discord voice channels, access user DMs, or turn normal Discord accounts into self-bots. For tests, refer to [CALL COMPANION](CALL_COMPANION.md) and [release acceptance](PUBLIC_PREVIEW_ACCEPTANCE.md).

## Choose her brain

**LOCAL:** Click DOWNLOAD OLLAMA, install the official Windows app, then DOWNLOAD LOCAL MODELS. The defaults are qwen3:4b text and qwen3-vl:4b vision, and large downloads need free disk space. The model downloader has a cancel button. TEST CONNECTION verifies the installed model.

**CLOUD:** Choose NVIDIA, Gemini, OpenRouter or OpenAI. Click the official API-key link, paste a key, choose a chat model ID and optionally a vision-capable model. Keys are stored using Windows CurrentUser DPAPI, not in settings.json. Model access, rates and free quotas differ; a cloud test can consume credits. Cloud chat and approved images leave your PC for that provider. Do not choose cloud for private screenshots if you do not agree with its privacy policies.

## Choose her voice

The wizard lists installed Windows TTS voices and supports PREVIEW. It does not clone an actor's voice. MIC ON is enabled later in the Mainframe and uses the Windows speech recognizer and a "Capri" prefix. If speech recognition cannot find a language pack, install one in Windows Settings. Windows microphone access must be enabled under Privacy & security.

## Safety

MIC ON, WATCH SCREEN, and desktop automation start OFF at each launch. For cloud vision, Capri prompts before sending a one-shot screenshot or beginning watch mode. Every desktop action requires approval. This release cannot purchase, checkout, delete files, send messages or execute arbitrary shell commands.

Windows SmartScreen or antivirus may warn because the installer is unsigned. Investigate warnings; do not assume they are false positives.

See [SECURITY.md](../SECURITY.md) and [feature matrix](FEATURE_MATRIX.md).


## Reminders and monitor selection

Mainframe has a **WATCH MONITOR** picker. Choose Primary display or one of your attached monitors before pressing LOOK NOW or enabling WATCH SCREEN. Only that monitor is captured, not all displays simultaneously.

Set a timer by typing or saying "set a timer for 30 seconds" or "remind me in 10 minutes to stretch". Use "show reminders" for pending entries and "cancel reminder #2" to cancel one. Reminders are stored on your PC, displayed in Capri's chat/bubble and as Windows tray notifications. The app must be open to deliver live notifications; missed reminders are delivered after the next app launch.

Ctrl+Alt+V toggles microphone recognition for the current session (if Windows language packs permit it); Ctrl+Shift+Space toggles the chat panel. Neither shortcut starts cloud screen watching.


## Fish Audio cloud voice

Open SET UP BRAIN / VOICE. Under HER VOICE select CLOUD — Fish Audio. Generate a NEW API key at https://fish.audio/app/api-keys and paste it into the Fish key field (separate from your chat-brain key). Choose S2.1 Pro Free (s2.1-pro-free) for the free developer tier, if eligible, or S2.1 Pro (s2.1-pro) for the potentially billed production tier. Fish documents the free offer through November 30, 2026, subject to fair use; verify current limits before testing.

Optional voice reference ID: enter an ID only for a voice you have permission to use. If blank, Fish uses the default voice. TEST VOICE makes a real API call and may use quotas or paid credits. Turn on Speak replies aloud after setup and SAVE. Every spoken reply, reaction or reminder is then sent as text to Fish. Audio is played directly in memory.

The key is encrypted for this Windows account in %LOCALAPPDATA%\CapriDesktop\credentials\fish.dpapi. DELETE SAVED FISH KEY removes the local encrypted copy, not the key from Fish's servers. Revoke a disclosed key and generate another. This feature does not record or transmit microphone audio to Fish and does not provide voice cloning.
