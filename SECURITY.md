# Security and privacy

Capri Desktop is an early, unsigned, independently developed desktop companion. Verify the code and executable provenance before using it.

## Where data goes

| Feature | Destination | Contents |
| --- | --- | --- |
| Local Ollama | http://127.0.0.1:11434 | Chat prompts, approved images, action-planning requests |
| NVIDIA | https://integrate.api.nvidia.com/v1/ | Cloud prompts and approved images |
| Gemini | https://generativelanguage.googleapis.com/v1beta/openai/ | Cloud prompts and approved images |
| OpenRouter | https://openrouter.ai/api/v1/ | Cloud prompts and approved images |
| OpenAI | https://api.openai.com/v1/ | Cloud prompts and approved images |
| Windows speech | OS speech recognition and TTS | Microphone audio is processed by Windows when enabled; raw audio is not saved by Capri |

Our app implements no analytics or telemetry endpoint. This does **not** mean Windows, Ollama, or third-party AI services collect nothing.

API keys are stored under %LOCALAPPDATA%\CapriDesktop\credentials, encrypted using Windows DPAPI for the current logged-in user. They never appear in settings.json or GitHub commits. You can remove local data and encrypted key files by deleting the CapriDesktop data folder.

Screen watching and microphone recognition start OFF on each launch, and screen images are not saved by the app. Cloud screenshot sharing requires explicit confirmation. Desktop actions start OFF and require per-action approval. Only opening allowlisted apps, HTTPS sites and media controls is supported. No arbitrary click/type, command execution, file deletion, purchases or checkout. Screenshots and third-party pages are untrusted data, never commands.

The installer is not signed. A warning is not proof of malware, but it is not proof of a false positive either. Use source and provenance checks, and report potential vulnerabilities without posting secrets to public issues. See [asset rights](docs/ASSET_RIGHTS.md).


## Reminder information

Reminder text and due dates are stored unencrypted in %LOCALAPPDATA%\CapriDesktop\reminders.json (the same local directory used for ordinary chat history). Notifications can appear on the desktop or lock screen depending on Windows settings, so avoid writing highly sensitive secrets into reminders. No reminder text is transmitted to a cloud AI provider by the deterministic reminder engine.


## Fish Audio (optional voice provider)

When Fish Audio is selected with SPEAK REPLIES on, Capri sends reply, reminder or reaction text to https://api.fish.audio/v1/tts using a bearer token and a selected model header. TEST VOICE transmits a short fixed phrase. Fish sends MP3 bytes, decoded and played from memory using NAudio, not written to disk by Capri. Fish may process/retain text and may charge for a paid model.

The Fish key is encrypted with the Windows user's DPAPI in the credentials folder as fish.dpapi, never in source files or plain settings. The Voice Setup DELETE SAVED FISH KEY button deletes the local copy; it does not revoke the key at Fish. Revoke any previously disclosed key on the Fish website.

The endpoint is pinned and the app accepts only the s2.1-pro-free or s2.1-pro model headers. Input text, audio size and network duration are bounded. Voice reference IDs must refer to voices whose use is authorized.


## Configurable desktop input

Input Control Center uses Windows RegisterHotKey, not a global keylogger. The selected NAudio recording device feeds a bounded in-memory PCM stream to the Windows speech engine only after MIC ON. Mic mute and STOP INPUTS close recording. The five-second mic test saves or uploads no audio. Selected speaker routing currently applies to Fish Audio, while Windows TTS uses the default device. The stop control does not terminate unrelated OS jobs. See docs/INPUT_SYSTEMS.md.


## Smarter opt-in screen watcher and gestures

Auto watching now calculates a 16x9 grayscale frame signature in memory to avoid
resending identical or blank screens. The last accepted signature and last
short observation are volatile RAM only, with observation access limited to the
last three hours. Explicit LOOK NOW still sends a current screenshot by request.
A Follow frontmost app selector chooses the monitor containing the highest
eligible non-Capri window, falling back to the primary screen. A visible
selection and time interval are required; screen watching stays off at launch.

Mouse-rub gestures are detected locally within the companion WPF control,
without global pointer hooks. No mouse-motion history is written to disk.
Turning off petting in Input Control Center disables gesture reactions.

STOP INPUTS now also invalidates pending AI desktop action plans, so a proposal
completed after the stop is not executed. Approval dialogs already on screen
still need manual dismissal; purchases remain forbidden.
