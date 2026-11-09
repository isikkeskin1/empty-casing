# Capri Desktop voice, microphone & call edge-case matrix

**Scope:** v0.7.x voice reliability hardening. Automated CI runs .NET 10 core smoke tests, validates WPF compilation and builds a Windows installer. **Hardware tests are separate**, because GitHub Windows runners do not simulate physical microphone drivers, native input permissions, live Discord calls or actual local Whisper accuracy.

| Scenario | Expected outcome | Verification |
| --- | --- | --- |
| Speak Now while microphone absent | Inline error; no process exit | Windows hardware QA required |
| Windows microphone permission disabled | Inline setup guidance; typing remains usable | Windows hardware QA required |
| Windows recognizer not installed | Failed startup is recoverable; does not claim listening | Windows image QA required |
| Unsupported selected recognizer language | Safe error; previous settings preserved | Logic reviewed; Windows QA required |
| Microphone disconnected while listening | End capture, one diagnostic event, return to ready/error | Event path implemented; physical unplug QA required |
| Microphone failure while STARTING | Fail closed, do not arm one-shot permission | Code review; physical QA required |
| Native WinMM/COM throws during Stop | Best-effort resource release; do not terminate WPF | Code review; Windows driver QA required |
| 20-second manual listening timeout | Disarm one-shot and close temporary mic | Voice router smoke; UI timer QA required |
| Press Speak Now twice quickly | Second press cancels; no duplicate microphone | UI logic review; physical QA required |
| 80 concurrent Start attempts | Unique generation token; only one current session | **Automated smoke** |
| STOP followed by START | Old recognition/stop callbacks ignored | **Automated smoke** plus WPF gate review |
| 800 delayed callbacks from old session | None accepted | **Automated smoke** |
| STOP called repeatedly | Safe, idle state | **Automated smoke** |
| Recognizer emits two error/completion events | One failure per active session | Event guard reviewed; Windows QA required |
| PCM reader blocked waiting for microphone | STOP wakes it without deadlock | **Automated smoke** |
| 2,000 PCM callbacks during STOP | No worker exception | **Automated smoke** |
| Driver sends invalid or oversized PCM count | Ignore malformed input | **Automated smoke** |
| Live audio buffer disposed before callback | Reader/writer ignore stale data | **Automated smoke** |
| Windows mic enabled independently from Speak Now | One-shot STOP must not claim continuous mic is off | UI logic reviewed |
| Speech reply playing during recognition | No arbitrary command triggered by Capri's own TTS | Voice router smoke |
| Wake word embedded in another word | No false wake on “caprice” | **Automated smoke** |
| Duplicate recognized phrase within two seconds | Ignored | **Automated smoke** |
| Voice follow-ups disabled | No ambient prompt permitted | **Automated smoke** |
| Speak Now utterance expired | Cannot authorize request | **Automated smoke** |
| Explicit STOP ALL during speech | End inputs and cancel reply | Voice router smoke; Windows QA required |
| Stop during Whisper recording | Cancel; close microphone; no transcript sent | Logic reviewed; Windows hardware QA required |
| Stop during Whisper inference | Ignore late result; no AI prompt submitted | Logic reviewed; native Whisper QA required |
| Silent Whisper recording | No fabricated request submitted | **Automated amplitude-gate smoke** |
| Less than 200 ms audible sound | No Whisper inference started | **Automated amplitude-gate smoke** |
| Malformed Whisper PCM frame | Rejected without creating audible input | **Automated smoke** |
| Whisper model missing or modified | Refuse to load; prompt user to reinstall | **Automated SHA-256 policy smoke** |
| Whisper download cancelled/incomplete | Remove .partial; preserve prior verified model | Code review; network QA required |
| Real mic unplugged during Whisper capture | Stop promptly with recovery text | Event handler implemented; hardware QA required |
| Change input device mid-recording | Block; require finishing one-shot first | Code review; UI QA required |
| Test mic while Whisper/Call Mode active | Block extra capture handle | Code review; UI QA required |
| Close INPUTS during mic test | Cancel; no invalid WPF UI callback | Code review; UI QA required |
| STOP ALL during microphone test | Cancel test | Code review; UI QA required |
| Mic test device stops before timer | Surface disconnect instead of fake successful peak | Event path implemented; hardware QA required |
| Enter Call Mode during Speak Now | Block until recording closes | Code review; live-call QA required |
| Call output disconnected | Stop loopback, virtual cable and reply | Event handler implemented; live-call QA required |
| Call real microphone/cable disconnected | Stop both capture and outgoing bridge | Event handler implemented; live-call QA required |
| Queued speech from a previous call | Reject if listener belongs to old call | Session identity guard reviewed |
| Call participant says a computer command | Never authorize desktop automation | CallCompanionPolicy smoke |
| Exit app while SAPI/TTS callback in flight | Close resources; callbacks should not revive windows | Code review; Windows restart QA required |
| Offline/cloud provider unavailable | Speech text stays local until user-authorized brain request; UI shows reply failure | Existing app flow; live-provider QA required |
| Small screen, 125–200% DPI, second monitor | Buttons, voice status and animation remain usable | Manual UI QA required |
| Windows driver crash or native library access violation | Managed exception handling cannot guarantee recovery | External native crash QA and crash dump investigation required |
| Audio output device removed during Fish playback | Stop/Dispose native device safely; surface a status message, preserve chat | Code path hardened; unplug hardware QA required |
| MP3 playback reports error during app shutdown | Ignore late event; no WPF dispatcher crash or stale completion of new output | Code path reviewed; real Windows exit QA required |
| Rapidly cancel and replace Fish MP3 playback | Old output's completion must not affect the new session | Session identity guarded; device QA required |
| Windows TTS voice missing or failing to start | Return companion to idle, show setup guidance and keep text chat usable | Code path reviewed; Windows language/device QA required |
| Close setup while Ollama model is downloading | Cancel owner token, ignore queued progress, never dispose CTS midrequest | Code path reviewed; network cancellation QA required |
| Close setup during cloud brain/Fish preview | Ignore late window updates, keep next launch functional | Code path reviewed; provider QA required |
| Close INPUTS during Whisper model install | Ignore queued progress callbacks, cancel download and preserve verified model | Code path reviewed; network QA required |
| Fail setup due to invalid model or missing provider key | Do not leave partially applied brain/voice settings | Transactional validation implemented; manual QA required |
| Launch Capri twice while running | Exactly one audio/tray/hotkey stack; second launch opens existing Mainframe | **Windows named-object smoke**; installed-app interaction QA required |
| Launch again before first-run setup completed | Existing setup wizard appears instead of second overlay | Code path reviewed; first-run hardware QA required |

## What can be called complete?

The automated suite verifies **deterministic policy and managed-concurrency edge cases**, not all physical scenarios. A production claim for Speak Now requires a tester on a clean Windows 11 x64 system to run physical unplug/replug, mic permissions, supported/unavailable language packs, repeated Stop/Start (at least 50 cycles), device index change, Whisper silence/Italian/Turkish/English, Discord calls with and without VB-CABLE, app exit during capture, and 100% / 150% / 200% display DPI.

If the desktop application still exits unexpectedly, open **About / Updates → Local Diagnostics → Refresh Report** and share only the privacy-filtered diagnostic categories. There is no in-product crash log containing private microphone audio or complete user chats. Hardware crash investigations may require a separately consented Windows crash dump; do not post one publicly.

**Legal/distribution:** the installer remains unsigned and default character art is separately licensed noncommercial content. OAuth provider applications and device acceptance remain external release gates.
