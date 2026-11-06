# Now Playing — real Windows media sessions

Windows 10/11 apps such as Spotify and modern browsers may expose current
media through System Media Transport Controls (SMTC), an official Windows
API. This is more reliable than merely reading the master volume meter.

## Use it

- Open Mainframe → NOW PLAYING to see the active media session's title,
  artist, source app and play/pause status.
- Ask Capri "what song is playing?" or "what video is playing?"
- Use the panel controls or ask "pause music", "resume video",
  "next song" or "previous track".
- Media controls need desktop actions enabled, with per-action
  confirmation unless HANDS-FREE was armed.
- DANCE TO AUDIO uses existing local WASAPI sound-level pulses for
  animation. While enabled, Capri checks current SMTC metadata about
  once every 10 seconds and may show a "Now vibing" overlay when
  the title changes. No cloud call is needed for that reaction.

## Privacy and limitations

The SMTC APIs expose title/artist/source metadata published by media apps;
they don't provide call transcripts, browser history, raw microphone or
system-output audio. Song titles stay in memory and are not uploaded to
AI models or saved to the chat database by the poll. Because media
titles may include video names, DANCE TO AUDIO remains per-session opt-in.
STOP ALL turns it off.

Some apps, ad blockers, private windows, player versions and games do
not register SMTC sessions. In those cases Capri reports that Windows
has no current media session. Playing a track by its exact title,
searching Spotify/YouTube, lyrics, playlist editing and tempo-accurate
beat syncing are not implemented in this feature.

## Manual Windows acceptance tests

- Open Spotify, start a track; verify title, artist and source.
- Pause in Spotify; verify the next refresh displays paused.
- Try next/previous and ensure only the selected media session responds.
- Play a YouTube video in Edge/Chrome; verify whatever SMTC metadata
  the browser actually publishes.
- Enable DANCE TO AUDIO, switch tracks and verify only one overlay
  reaction per change. Turn DANCE off and confirm polling stops.
- Disable desktop actions and try a media control; no action must run.
- Try HANDS-FREE; valid commands no longer require Capri popups.
- Verify there is no source title/artist in the persisted chat history.
- Verify offline operation and compatibility with Windows 10 2004+ and 11.

No song detection from raw audio, call recording or audio upload occurs.
