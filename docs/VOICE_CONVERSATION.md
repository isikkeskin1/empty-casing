# Capri voice conversation protocol — Windows first iteration

The goal is a responsive desktop companion with explicit control over recording
and over what recognized speech can command. We implement this as an independent
voice routing layer, not by copying code from the reference Capri repository.

## Interaction modes

| Input | Behavior | Default privacy |
| --- | --- | --- |
| Mainframe MIC ON / Ctrl+Alt+V | Start/stop Windows dictation on the selected mic | OFF every launch |
| "Capri, ..." or "Hey Capri, ..." | Route a high-confidence recognized request | Requires MIC ON |
| Mainframe TALK ONCE / Ctrl+Alt+Space | Arm one recognized utterance, up to 20 seconds; no prefix required | Explicit user action |
| Optional follow-ups | 20 seconds of prefix-free turns after an explicitly addressed turn | OFF |
| Optional voice interruption | Only "Capri stop" variants can interrupt while voice is playing | OFF |
| Mainframe STOP VOICE | Stop Windows TTS/Fish Audio generation and playback | User action |
| Ctrl+Alt+X / STOP INPUTS | Stop mic, watch, desktop-action permissions and speech | User action |

This implementation uses the existing local Windows speech recognizer, with
16kHz PCM from NAudio and confidence filtering. Talk Once does not create a
background hotword service. Speech-to-text runs only when the user explicitly
enables the mic or arms Talk Once. If Talk Once started with the mic off, the
recording device is closed on completion or timeout.

A pure, deterministic VoiceTurnRouter handles prefix boundaries (so "caprice"
does not awaken Capri), local stop/mute commands, duplicate suppression and
temporary follow-up permission. The AI planner is never needed for stop/mute.

## Remaining engineering constraints

- No acoustic echo cancellation. Voice interruption is opt-in because speakers
  may leak into microphones, creating false recognition.
- Not a streaming neural voice-to-voice architecture: recognition is based on
  complete utterances and Windows language packs.
- Spoken replies may arrive from a remote Fish Audio TTS provider only if the
  user chose it. The voice routing layer does not send raw microphone audio
  there. Model prompts may be sent to the selected brain.
- The current AI request layer remains one request at a time; a canceled
  voice playback does not cancel an unrelated model network request.
- No call audio separation, speaker identity verification or wake-word detection
  with the mic off. Remote call participants cannot authorize actions.

## Manual Windows acceptance

1. Use a nondefault USB/BT microphone; verify recognition and Talk Once.
2. Arm Talk Once while MIC OFF; say one request; confirm capture closes.
3. Arm Talk Once and stay silent 20 seconds; confirm capture closes.
4. Arm Talk Once while MIC ON; confirm the earlier continuous listening
   state is preserved after the turn.
5. Enable follow-ups: address Capri explicitly, then ask one follow-up without
   prefix inside 20 seconds. Repeat after timeout; it must be ignored.
6. Without enabling follow-ups, nearby speech must not trigger commands.
7. Use headphones and try opted-in "Capri stop" while Windows and Fish speak;
   verify other phrases cannot trigger desktop actions during playback.
8. Verify STOP VOICE halts playback without turning on a muted mic.
9. Verify STOP INPUTS never permits a pending model action to execute after
   shutdown.
10. Test long mic sessions, device-unplug recovery, hotkey conflicts,
   multilingual speech recognition and error feedback.

A green Windows CI build and core smoke tests do not replace this QA.
