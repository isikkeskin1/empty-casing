# Capri Game Clips — Local OBS Replay Buffer

The original Capri project offers game clipping. Our independent Windows implementation uses **OBS Studio's official built-in WebSocket v5 API**, not proprietary recorder drivers, screen-scraping or a stream upload service. Capri issues only four fixed actions over `ws://127.0.0.1`: `GetReplayBufferStatus`, `StartReplayBuffer`, `SaveReplayBuffer` and `GetLastReplayBufferReplay`. It never starts a livestream, changes OBS scenes or edits your OBS recording configuration.

## Get started

1. Install and start [OBS Studio](https://obsproject.com/). In **Settings → Output**, set up and enable **Replay Buffer** and choose the replay duration (for example, 30 or 60 seconds), file format and destination. Add a Game Capture or Display Capture source. OBS must be running and capturing the correct content before you can save a meaningful clip.
2. In **Tools → WebSocket Server Settings**, enable the WebSocket server and authentication. Keep the normal port `4455` unless you changed it. Copy the password OBS displays there. OBS 28+ includes WebSocket support without a plugin.
3. Open Capri's **Control Deck → Apps & Tools → Game Clips / OBS Replay**. Enter your port and paste the password, then press **SAVE CONNECTION**. The password is encrypted using Windows DPAPI for the current user. It is never written to `settings.json`.
4. Enable **clip controls for this session**. Press **CHECK OBS** to verify the connection. If Replay Buffer is off, press **START BUFFER**. This starts OBS's rolling replay recording; OBS determines duration, encoding, quality, disk path and audio sources.
5. During gameplay, say **Capri, clip that**, type **clip that**, or press **SAVE CLIP**. Capri asks OBS to save its buffered moments. Then press **FIND LAST CLIP** and **SHOW SAVED FILE** to select the verified local file in Explorer.

**Important:** If you only enable the WebSocket server but do not enable Replay Buffer in OBS Output settings, clip saving won't work. Capri cannot retroactively record moments from before the OBS replay buffer started.

## Supported commands

- `clip that`, `save replay` — save OBS's rolling buffer
- `start replay buffer` — start rolling recording
- `clip status` — show OBS buffer status
- `show last clip` — retrieve the last replay's path (no auto-upload or file edits)

Capri requires the **per-session clip-controls switch** to be enabled before voice, chat and UI commands can contact OBS. The OBS password is a local-only encrypted credential, separate from Spotify, Roblox and AI provider tokens. **STOP ALL** disarms clip commands and cancels outstanding requests. OBS itself may continue recording its replay buffer after Capri is disarmed; you can stop it inside OBS.

## Safety and limitations

- Capri only connects to **127.0.0.1** with a validated local port; user prompts and AI plans cannot supply network destinations or arbitrary OBS WebSocket request names.
- Password authentication is required; no password-free OBS WebSocket control.
- No listening to OBS live audio, accessing streams, uploading clips, messaging contacts or modifying existing OBS scenes.
- OBS's own `SaveReplayBuffer` response means it accepted the request, not that the video is already fully written. Capri reports the save as **requested**; the file is discoverable afterward from OBS.
- The optional local file viewer accepts only absolute paths with common video extensions, checks for an existing file, rejects network paths and opens only Windows Explorer.
- The client makes short-lived connections for each explicit action and uses a nine-second timeout. It is not a persistent remote-control service.
- OBS is a separate app and not bundled in the Capri installer. It must be configured correctly and running.
- The clipping integration compiles and has protocol smoke tests in CI, but capture reliability and real microphone/game-audio mixes require real Windows/OBS testing.

**Next milestone:** better game-specific visual guidance, foreground-game detection, and a more responsive audio conversation pipeline. Real-time game-play automation is not included.
