# Context Sense — make Capri aware without constant screenshots

Capri's companion responses can include a short hint of the current
external app and music session. This is designed for low-friction
conversation: "I see you're in Roblox" or "Still listening to ...?"
rather than treating everything as a generic chatbot session.

## Using it

Open Mainframe → enable **CONTEXT SENSE · APP + MUSIC**.

For every ordinary AI chat reply, Capri reads:
- The process name of the most prominent external visible window,
  excluding Capri itself (may not always be the true foreground).
- The current Windows System Media Transport Controls song/video title
  and artist, if an app exposes them and playback is active.

The two values are trimmed and appended to a temporary copy of the
user's message as untrusted observations. They are NOT persisted in
the normal chat history. Because a configured cloud text model receives
that temporary hint, this mode must be explicitly enabled by the owner.
It is off by default on first use, but the preference is saved.
Press STOP ALL to turn the setting off and stop related input permissions.

You can always ask "what app am I in?" without opting into ongoing
reply-context sharing; that question is answered locally from process
information alone.

## Boundaries

- No screen pixels, file contents, password fields, window titles,
  mic audio, browser tabs or Roblox client memory are captured here.
- No active game state, quest objectives or player positions are
  inferred from process names alone. Use GAME HELP for explicit visual
  coaching, with separate cloud screenshot consent if applicable.
- Context is approximate: game launchers, overlays and full-screen
  apps may make the active app name stale or wrong.
- If Spotify/YouTube isn't publishing a Windows media session,
  there may be no song information. No raw audio fingerprinting.
- External strings are untrusted data, not instructions. They cannot
  cause Capri to perform arbitrary computer actions.
- The mode is compatible with local Ollama and cloud brains; it is
  never silently enabled by hands-free desktop action permission.

## Manual QA

- With the toggle off, verify no context is appended to outgoing text.
- Enable it and open Roblox/Steam/a normal app; verify current process.
- Play a Spotify track/video and verify the visible title hint.
- Confirm no raw screenshot, chat title or password content is sent.
- With a cloud model, inspect requests: only the short approved hint
  is added, and no tokens/provider secrets are included.
- Turn OFF while a context read is pending; the next model request
  must use the original message without hints.
- Press STOP ALL; verify Context Sense is disabled and stays disabled
  after restart.
- Confirm no appended hints are stored in normal chat history.
