# Capri Desktop — Speak Now microphone troubleshooting

**Speak Now** is the manual, one-request voice interaction in the Mainframe. It is separate from MIC ON (continuous Windows speech recognition). It should never silently activate your microphone before a click. A second click or **STOP** cancels the one-shot session; **STOP ALL** closes voice, call and screen inputs.

## What the voice indicator means

- **MIC STANDBY** — no one-shot recording is running.
- **CONNECTING** — Capri is opening the selected device/recognizer; the UI does not claim to listen yet.
- **LISTENING** — Windows has started microphone capture; speak one request. Windows speech has a 20-second maximum; optional local Whisper records for six seconds.
- **TRANSCRIBING** — Whisper is processing the six-second recording on the PC. No raw PCM is sent to an AI cloud provider.
- **MIC NEEDS ATTENTION** — microphone device, Windows recognizer, permissions or an audio callback failed; the user can still type to Capri.

## Troubleshooting

1. Open **INPUTS** from the voice panel. Pick a real microphone and use **TEST MIC** with continuous MIC ON disabled. Grant Windows microphone access to desktop apps in Windows Settings → Privacy & security → Microphone.
2. For **Windows Speech**, confirm an installed Windows Speech Recognition language is selected. A Windows text-to-speech voice alone does **not** install a dictation recognizer; unsupported speech language or missing device should now show an inline error, not close the Mainframe.
3. If Windows SAPI repeatedly fails, switch in INPUTS to **Whisper local**, install the verified model and choose manual **Speak Now**. Whisper is a separate path with native model/CPU requirements and a six-second voice turn, not an always-on wake word.
4. If using Discord call audio, **STOP CALL MODE** before Speak Now. Close conflicting microphone apps or test a different mic device. Headphones are recommended to prevent feedback.
5. If the issue persists, open **ABOUT / UPDATES → LOCAL DIAGNOSTICS → REFRESH REPORT**. Review/copy the privacy-filtered report and note the error category **voice-device-error**, Windows version, device name and whether Windows/Whisper was selected. Do **not** post API keys, raw audio or full local data folders.

## Developer acceptance tests

- Start Speak Now with no installed Windows recognition language; inline error without terminating app.
- Start with an unplugged/default microphone; report a recoverable error and keep the input textbox working.
- Rapid Speak → Stop → Speak; only one capture session should be active, and the previous one should finish teardown before reuse.
- Disable/mute microphone while Speak Now is active; release the capture and reset the status indicator.
- Disconnect microphone during active recognition; NAudio and SAPI callbacks must not throw a fatal worker exception.
- Test local Whisper with valid/invalid model, missing input, cancel during recording and cancel during transcription.
- Test exit/restart while Windows TTS is completing a sentence.
- Check the Mainframe at minimum supported size and at different display scaling factors. Character art, conversation and voice buttons must remain readable.

**Important:** Automated GitHub Windows builds verify compilation, smoke tests and packaging, not physical microphone drivers or all SAPI/Whisper native scenarios. The precise crash reported on a specific Windows machine can only be confirmed resolved by reproducing it with that device or its diagnostic error category.
