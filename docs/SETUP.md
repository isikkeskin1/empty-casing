# Capri Desktop setup guide

This is an independent, unofficial fan project. **Do not confuse the original developer's installer with ours.** Our builds are available at https://github.com/isikkeskin1/empty-casing/actions .

## Install

Download the Capri-Setup artifact from a successful Windows CI run and extract its ZIP to get Capri-Setup.exe. Verify the source before running unsigned software. It installs for the current Windows user, without an administrator prompt, offers Start menu and desktop shortcuts, and opens the setup wizard on first run.

The app installer bundles its own .NET runtime. It does not bundle Ollama, models, copyrighted character animations, paid API access or custom voices. Uninstall does not delete local app data, settings, history or encrypted keys, which remain under %LOCALAPPDATA%\CapriDesktop.

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
