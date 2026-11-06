# Real perception and assistant functions

The independent Capri Desktop implementation now has a **functional first assistant layer**. The reference project advertises a wider feature set. Our goal is deliberate functional development, not exaggerated parity.

| Capability | This build | Not yet supported |
| --- | --- | --- |
| Vision | Primary monitor screenshot on LOOK NOW | Continuous video; OCR-driven UI element grounding |
| Proactive reactions | Local Ollama vision evaluation every 45 seconds when enabled | Game-event hooks, accurate damage/health detection |
| Microphone | Windows local dictation when MIC ON; say "Capri" before a request | Voice activity detection, robust multilingual speaker-ID, call mode |
| Voice output | Windows installed TTS voice if SPEAK REPLIES is on | Character-accurate voices, streaming, barge-in |
| Actions | Approve per action: launch allowlisted applications, HTTPS URL, media keys | General clicking, typing, filesystem, remote messaging, buying |
| Windows interaction | Perch, toss, follow app windows | Alpha-perfect hit testing, universal fullscreen behavior |

## How to test

1. Install .NET 10 and Ollama (or download the CI ZIP). Pull a text model: \`ollama pull qwen3:4b\` and a vision model: \`ollama pull qwen3-vl:4b\`.
2. In Mainframe, set text and vision model names and choose SAVE.
3. Press LOOK NOW (this authorizes exactly one screenshot). Capri should answer a screen question.
4. Enable WATCH SCREEN for a limited test. She captures the **primary monitor** every 45 seconds and may comment briefly. Turn it off when finished. The screenshots are transmitted to local Ollama only and never written to disk by this app.
5. Turn on MIC ON. Use a speech language installed in Windows. Say "Capri, open notepad". Enable DESKTOP ACTIONS to permit an action **after a confirmation dialog**.
6. Turn on SPEAK REPLIES to hear Windows's synthetic voice. Use headphones when microphone listening is also on to avoid audio feedback.
7. In the tray, select Stop mic and screen watching to revoke both permission toggles immediately.

## Important security boundaries

- Screen watching and microphone recognition **start off on every launch**, regardless of any previous session. The app does not read webcam images or record system audio.
- An explicit screenshot capture or a screen-watching session sends resized JPEG bytes only to \`http://127.0.0.1:11434/api/chat\`. No screenshot files are saved; Ollama model/provider configuration and its own retention settings are outside the app.
- Speech recognition uses the Windows Speech API. The app does not save raw microphone audio. Accepted voice requests become ordinary locally saved chat messages. Windows speech recognition availability and related system settings vary by locale; verify Windows privacy settings separately.
- Images and text encountered on screen are **untrusted**. Screen analysis cannot call the action planner; only spoken/typed requests from the user may propose actions.
- The action planner can propose only \`open_app\`, \`open_url\` and \`media\`. A separate policy validator rejects unrecognized kinds, non-HTTPS URLs and unapproved applications; there is a confirmation dialog before each action. No automatic purchases or checkout, file deletions, message sending, arbitrary click/type, scripts or command-shell execution.
- The watch-mode text reactions appear temporarily and are not automatically stored in the conversation history. Answers to explicit screenshot questions *are* saved as chat text. Clearing chat deletes those messages.
- This project has no telemetry. All runtime requests go to the fixed local Ollama endpoint; the app does not silently add remote model providers.

## Limitations

The microphone feature is recognition, not a privacy-preserving always-on wake-word recognizer; while MIC ON the Windows recognition engine hears audio for dictation and the app only responds to phrases starting with "Capri". Turn it OFF when not needed.

Vision is not guaranteed to be correct. The model may misread small fonts or hallucinate. Proactive reaction timing is periodic and may miss fast gameplay events. On watch errors, watching disables itself. Running a vision model may use substantial GPU memory. Tests verify the request/response shape and safety policy, but Windows device integration requires manual testing.

The reference repository is useful for **behavioral inspiration**, not reusable code. Capri and its original animations belong to their respective rights holders; see ASSET_RIGHTS.md before public distribution.
