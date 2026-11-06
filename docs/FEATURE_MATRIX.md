# Feature matrix

The original fan project has a broad feature set. This file tracks our independent implementation, without claiming parity.

| Feature | Source status | Remaining work |
| --- | --- | --- |
| Installer and first-launch setup | Implemented | Clean Windows manual QA, signing |
| Local Ollama model downloads | Implemented | Large download and error testing |
| Cloud brain providers (4) | Implemented | Real-key and model compatibility QA |
| Encrypted API keys | Implemented | Recovery/migration QA |
| Windows TTS voice picker | Implemented | Voice/language pack testing |
| Fish Audio TTS (free/pro model choice, optional voice ID) | In source | Playback and real-key device QA |
| Animated desktop companion | Implemented | Mixed-DPI transparent hit testing |
| Screen vision/reactions | Selected or frontmost-app monitor with change suppression | Sensitive-area masking, multi-screen context, game events |
| Mic recognition + global toggle | Implemented | Selected mic, interruption, VAD, robust wake word |
| App/URL/media actions | Limited, approval-gated | UI Automation, action history, hard stop |
| Call mode, Discord/Zoom | Not implemented | Consent and speaker isolation |
| Additional cloud TTS (ElevenLabs/OpenAI) | Not implemented | Provider security and licensing |
| Song selection, music mini-player | Not implemented | Media session integration |
| Reminders and timers | Implemented: local relative scheduling | Absolute dates and OS background scheduler |
| Game clips/live reactions | Not implemented | Event and capture pipeline |
| File drop and reading | Not implemented | Parsers and explicit file selection |
| Mini-games, skins and clones | Not implemented | UI/rendering and assets |
| Code/Husk integration | Not implemented | Read-only hooks with secret redaction |
| Installer signing/updates | Not implemented | Safe release/update verification |

Suggested order: clean setup → voice and mic selection → multi-monitor and media context → UIA computer use with per-step approvals, hard cancellation and permanently prohibited purchases → reminders/files/call integrations.

All major additions should have a real feature PR, Windows CI and manual device testing.


The Input Control Center now has a selectable microphone, five-second meter, mute that closes capture, configurable global hotkeys, chosen Fish Audio playback device, and STOP INPUTS. Real-device Windows testing is still required. Gamepad control, voice-call loopback, full-duplex audio, background hotword and UI Automation actions are not yet implemented.

Smart watching supports configurable intervals (10 seconds–10 minutes), blank/unchanged frame suppression, in-RAM last observation and a voice-listening quiet mode. Mouse rub/head-pat interactions operate only over the character surface with cooldown; neither requires global pointer capture.
