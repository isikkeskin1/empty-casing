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

## Voice Conversation v2 — interrupt, rephrase and stay in-game

The main speech pipeline now distinguishes **STOP REPLY** from the emergency **STOP ALL** command. This is built on the existing local Windows speech recognizer and the selected Windows/Fish voice, not an always-streaming audio model.

| Owner control | Behavior |
| --- | --- |
| `Ctrl+Alt+R` / Mainframe **STOP REPLY** | Cancel a pending chat/model response and speech, without disarming mic or desktop controls |
| `Capri, cancel that` / `Capri, stop answering` | Explicit spoken reply interruption; speaker barge-in requires separately enabled voice-interrupt option |
| `Ctrl+Alt+Space` / **TALK ONCE** | Interrupt an old turn, then capture one new utterance with a 20-second timeout |
| Recognized new question while the model thinks | Cancel previous network generation (where provider honors cancellation) and keep only the **latest** new spoken question |
| `Ctrl+Alt+X` / **STOP ALL** | Cancel current model reply and speech **and** revoke the mic, screenshot watcher and desktop action permissions |
| Spoken voice question during a game | By default, keep the game focused; Capri processes the question in chat without popping the full window into the foreground. Set **Open full chat for spoken requests** in INPUTS to opt in. When speech replies are off, the full chat opens to display the answer. |

**No new automatic microphone opt-in:** microphone listening, optional follow-ups and voice barge-in remain individually controlled. The global Talk Once and Stop Reply shortcuts work even when the game is focused, provided Windows allows the shortcut registration.

**Conversation follow-ups now start counting after the spoken answer finishes.** A previous 20-second window could expire while the AI was thinking or Fish Audio was synthesizing. The new voice router tracks a conversation reply and reopens the opted-in 20-second window on local speech-completion events. If no audio reply is produced, it refreshes after the text reply is ready. It never reopens the lease after STOP ALL, a cancelled pending reply, or a microphone shutdown.

A cancelled model answer is not saved to the transcript or automatically spoken. Explicit file-review and screenshot replies also observe cancellation. **Cancellation cannot undo an app launch, API request or other external action already committed**, and some external providers may still finish work after Capri discards their result.

## Remaining engineering constraints

- No acoustic echo cancellation. Voice interruption is opt-in and limited to
  explicit prefixed cancellation/stop phrases because speakers may echo into microphones.
- No partial speech generation: Fish Audio still synthesizes the MP3 before playback,
  and Windows speech still receives a complete answer. This update improves control
  and perceived responsiveness, but is **not** full-duplex low-latency voice streaming.
- Not a streaming neural voice-to-voice architecture: recognition is based on
  complete utterances and Windows language packs.
- Spoken replies may arrive from a remote Fish Audio TTS provider only if the
  user chose it. The voice routing layer does not send raw microphone audio
  there. Model prompts may be sent to the selected brain.
- Voice input can now cancel an in-flight local/cloud chat reply, and the active
  reply sends a cancellation token to supported model/vision providers. Some tool
  actions and third-party HTTP side effects cannot be rolled back after dispatch.
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
9. Verify STOP INPUTS cancels pending model output, and external actions queued
   before cancellation obey their existing per-action permission checks.
10. Test long mic sessions, device-unplug recovery, hotkey conflicts,
   multilingual speech recognition and error feedback.

A green Windows CI build and core smoke tests do not replace this QA.

11. Ask a long model question, press STOP REPLY / Ctrl+Alt+R and confirm no stale answer or voice output appears.
12. With mic on, ask a slow question and rephrase while the model thinks; only the newest spoken question should be answered.
13. With follow-ups enabled, let a spoken response finish after more than 20 seconds and test a wake-free follow-up.
14. Test Talk Once as a keyboard barge-in while Capri is generating or speaking.
15. Keep a full-screen game in focus while speaking to Capri; ensure the Control Deck does not steal focus when automatic chat pop-ups are OFF.
