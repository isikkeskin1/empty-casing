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
