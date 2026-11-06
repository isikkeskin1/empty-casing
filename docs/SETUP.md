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
