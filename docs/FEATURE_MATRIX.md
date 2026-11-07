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
| Mic recognition + global toggle | Implemented: per-session opt-in, selected mic, voice commands and hotkeys | Hardware, wake-word and noise testing |
| Voice Conversation v2 | Implemented: cancel network reply, spoken rephrase, STOP REPLY hotkey, lease refresh after playback, no-focus chat option | Real Windows/Fish headset QA; no streaming ASR or echo cancellation |
| App/URL/media actions | Limited, approval-gated | UI Automation, action history, hard stop |
| Call mode, Discord/Zoom | In source: opt-in selected WASAPI output listening, Fish/real-mic virtual cable routing, consent and STOP ALL | Verify VB-CABLE, real Discord headset, feedback isolation and privacy on Windows |
| Additional cloud TTS (ElevenLabs/OpenAI) | Not implemented | Provider security and licensing |
| Song selection, music mini-player | Not implemented | Media session integration |
| Reminders and timers | Implemented: local relative scheduling | Absolute dates and OS background scheduler |
| Visual Lookout (owner-armed screen condition alerts) | Implemented: 2-hour RAM rule, uses consented WATCH SCREEN, strict verdict, one notification | Physical-game vision QA and confidence calibration |
| Quiet Mode | Implemented: per-session suppression of unsolicited commentary and speech | Accessibility and user-state QA |
| Absolute-date reminders | Implemented: ISO dates, tomorrow, English month names, local time | DST/time-zone field testing; no OS background alarm |
| Game clips via OBS Replay Buffer | Implemented: authenticated localhost WebSocket 5, one-shot save, discovery, optional voice command | Manual Windows/OBS recording & device tests; built-in recording fallback not implemented |
| File drop and reading | Not implemented | Parsers and explicit file selection |
| Editable Personal Memory Vault | Implemented: 40 local notes, commands, UI CRUD/export/clear; opt-in contextual use and cloud-provider consent | Validate on Windows and consider DPAPI encryption/migration |
| Game Coach overlay HUD | Implemented: click-through 30-second visual tips from explicit screenshot help | Borderless/fullscreen game testing, multi-monitor placement |
| Screen Sketch / owner drawing | Implemented: InkCanvas overlay, neon pen colors, undo/clear/PNG, Esc/STOP ALL/3-minute expiry | Real Windows mouse/stylus, mixed-DPI and borderless-game QA |
| Animated Chaos clones | In source: 1–5 click-through visual clones, optional local Ollama voice/dialogue, hold-Esc stop and approved 12-second window wobble | Test game detection, voices, click-through, reversible window motion; mini-games/skins still absent |
| Code/Husk integration | Read-only Codex/Claude Code lifecycle hooks and per-session local activity feed | Test with installed Codex/Claude versions; hook schemas can change; no per-file details |
| Versioned release packaging & checksums | Implemented: auto main-branch releases plus manual tag workflow, matching versions, hashes | Validate first automatic main release and signing |
| GitHub automatic release publishing | Implemented: main-branch validated installer/portable/checksums and versioned GitHub Release on success | Live release credentials/workflow and real update acceptance testing |
| In-app auto-updates | Implemented: startup and 10-minute GitHub checks, staged digest-verified installers, safe managed-install idle/exit apply and manual Restart to update | Real install/relaunch QA; publisher code-signing and rollback not yet present |
| Default character animation | Implemented: seven-state noncommercial upstream WebPs converted at Windows build, bundled as default PNG frames; animated immediately on first launch, original license and notices included | Windows graphical QA, noncommercial redistribution/credit compliance |
| Character art overrides/import | Implemented: seven-state ZIP validation, one-click import and reload; local imported state overrides default, survives updates | Mixed-DPI and animated timing QA |
| Mainframe visual redesign | Implemented: wider split chat/animated hero, dark premium panels, magenta/cyan/lime theme, improved onboarding and transcript | Physical Windows layout/contrast/responsiveness QA |
| Local diagnostics | Implemented: bounded local event/exception-type journal, user-reviewed copyable support report | Real crash/permission and clipboard QA; no telemetry, dumps or stack traces |
| Authenticode code signing | Not implemented: installer currently UNSIGNED; verified GitHub digest is not a publisher identity | Publisher certificate, Windows signature, SmartScreen reputation and rollback |

Suggested order: clean setup → voice and mic selection → multi-monitor and media context → UIA computer use with per-step approvals, hard cancellation and permanently prohibited purchases → reminders/files/call integrations.

All major additions should have a real feature PR, Windows CI and manual device testing.


The Input Control Center now has selectable microphone and speech language, five-second level testing, opt-in verified local Whisper Talk Once, global hotkeys, Fish output choice and STOP INPUTS. A separate Call Companion uses selected Windows output loopback and a virtual microphone cable. Bounded, approval-gated UI Automation navigation is in source. Real-device QA, true full-duplex echo cancellation, general gamepad control and unlimited arbitrary desktop input remain outstanding.

Smart watching supports configurable intervals (10 seconds–10 minutes), blank/unchanged frame suppression, in-RAM last observation and a voice-listening quiet mode. Mouse rub/head-pat interactions operate only over the character surface with cooldown; neither requires global pointer capture.


| Voice Talk Once | Implemented in source: 20-second one-shot mic session | Physical-device and language-pack QA |
| Optional short voice follow-ups | Implemented in source, OFF by default | Noise/echo and background speech QA |
| "Capri stop" speech interruption | Implemented with owner opt-in | Headset/echo safety and reliability QA |
| Acoustic echo cancellation, streaming VAD, full-duplex chat | Not built | Separate DSP/STT architecture |


| System-audio-reactive dance | Implemented in source: opt-in WASAPI RMS/pulses and music-perch animation | Multi-device, game and speaker QA |
| Multilingual local Whisper Talk Once | In source: optional 148 MB SHA256-verified model, six-second local transcription | Verify languages and real CPU/mic performance; song identification remains separate and unimplemented |


| UI Automation read-only window tab inspection | Implemented in source, manual opt-in | Physical Windows UIA provider QA |
| Safe tab selection | Explicit owner command, exact observed tab, confirmation and fresh re-check | Cross-app verification and action audit log |
| Arbitrary computer clicking, typing, file edits, messages | Not implemented | Grounded UIA targeting, task previews and policy enforcement |


| Explicit read-only text/code file review | Implemented in source, per-file user selection | Real-model and file-dialog QA |
| Cloud file-context sharing | Requires separate per-file confirmation | Privacy/provider acceptance QA |
| PDF/DOCX, images, folder indexing and file editing | Not implemented | Separate parsers, consent and action policy |


| Autonomous task-level UIA navigation | Implemented in source: one approval, model-chosen exact tabs, max 5 steps / 90s | Windows real-app QA |
| Live stop / revoke task permission | STOP TASK and STOP INPUTS cancel pending future steps | In-flight UIA provider calls cannot always be interrupted |
| Unrestricted AI clicking/typing | Not built | Stronger effect classification, per-app scopes and recovery |


| Task-scoped tree expansion | Implemented in source (UIA ExpandCollapsePattern) | App-specific accessibility/provider QA |
| Task-scoped navigation-button clicks | Implemented for exact Back/Home/Help/Menu/etc labels, no per-click prompts | Effect verification on trusted apps |
| Generic buttons (Save/Submit/Send/Delete/Checkout) | Always excluded from autonomous navigation | Requires separate higher-risk task policy |


| Hands-free no-confirmations setting | Implemented in source; saved preference; must arm desktop actions each session | Manual Windows permission UI QA |
| Auto-approved supported desktop commands | Allowlisted apps/HTTPS/media and validated UIA navigation | Does not add arbitrary clicks, purchases, sending or destructive actions |
| Emergency STOP ALL | Stops active microphone, watch, audio, desktop actions and queued AI task steps | Already-completed OS actions cannot be undone |


| Installed Steam game discovery | Implemented in source: bounded manifest/VDF reading, searchable Game Center | Test multiple Steam library folders |
| Open installed Steam games | Implemented in source using steam://rungameid/numeric ID | Local Steam protocol / UAC / launcher QA |
| Game coaching | One-shot screenshot with local/cloud vision, cloud disclosure | In-game screenshot quality and accuracy QA |
| Live-call speaking/listening | Implemented via user-joined Discord/Zoom call, virtual cable and selected output loopback | Does not automatically join calls; real hardware, echo prevention and participant consent QA required |
| Actual song metadata and synchronized dancing | Windows media metadata/track reactions implemented; dancing reacts to RMS/pulse, not verified beat-sync | Audio timing, browser/player compatibility and dance-art QA |


| Windows Now Playing metadata | Implemented in source using official Windows SMTC session | Spotify/browser/player compatibility QA |
| Pause/resume/previous/next media session | Implemented with desktop-action permission and optional hands-free | Media-app support varies |
| Local track-change reactions | Every 10s while DANCE TO AUDIO is enabled | Human review for noisy browser media |
| True BPM tracking, lyrics, music search/queue | Not implemented | Provider integrations and streaming DSP required |


| Disclosed AI Call Companion | Experimental call-playback local speech recognition, AI replies via routed Fish TTS | Real-device/virtual-cable QA |
| Discord automatic join / speaking through Discord bot connection | Not implemented; requires approved integration | Discord OAuth/application voice tooling |
| Call participant command restrictions | Only addressed conversational replies, no desktop action planner | Adversarial and real-call tests |


| Spotify official OAuth PKCE connection | Implemented in source, requires owner-registered Spotify app/client ID | Live OAuth and Premium device QA |
| Spotify native playback / track search | Official Web API /me/player, /me/player/play, search and skips | 2026 Development Mode limits apply |
| Roblox official OAuth PKCE profile | Implemented with openid/profile and userinfo | Live Roblox creator app registration/OAuth QA |
| Roblox experience launch | Numeric validated place ID to documented game-start link | Roblox now recommends share links for promotion |
| Spotify/Roblox credentials | Windows DPAPI, no secrets in source or chat context | Windows profile / logout QA |
| Unrestricted Roblox player-game control | Not supported by Roblox Open Cloud OAuth | Game-specific APIs, approved integrations and screen guidance |


| Context Sense app + song awareness | Implemented as opt-in, read-on-demand per model reply | Test foreground process and Windows SMTC on real desktop |
| Context privacy | Process name + current track/artist only, ephemeral model context, no local history | Cloud model sees these hints only when opted in |
| Literal BCI / reading thoughts | Not implemented | Requires separate medical hardware and consent |
