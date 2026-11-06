# Call Companion — experimental audio routing

Capri can participate **as an identified AI** in a call you joined using
Discord/Zoom/another app. This release does not use a Discord self-bot,
bypass Discord's terms or auto-join any voice channel.

## Setup

1. Install and configure a virtual audio cable (for example VB-CABLE).
   The **playback side** of the cable will receive Capri's voice; the
   **recording side** is selected as the microphone in your call app.
2. In Capri INPUTS choose the **non-default playback endpoint** that feeds
   that virtual cable; select Fish Audio as Capri's voice and enable
   SPEAK REPLIES. Fish voice needs a configured key and a working model.
3. Set the call application's **speaker** to Windows default output.
   The call application's **microphone** must be the recording side of
   the virtual cable. Your own real microphone won't be mixed in unless
   you configure an audio mixer separately.
4. Join the call yourself. Inform everyone that an AI companion will be
   listening and speaking, and obtain their permission.
5. Open Mainframe → VOICE CALL, check the participant-consent box and
   press START CALL MODE. Capri audibly identifies herself as AI.
6. If participants say "Capri, [question]", Windows local speech
   recognition extracts the request and the configured brain produces a
   concise conversational answer. The reply is synthesized by Fish and
   plays into your chosen virtual audio device.
7. STOP CALL MODE, close the panel or use STOP ALL to end recognition,
   stop voice and cancel pending replies.

## Boundaries

- The whole default playback stream may be audible to Capri, not just a
  Discord/Zoom call. Mute notifications/music as needed.
- Windows speech recognition requires a supported installed language.
- Only addressed text goes to the configured AI. Cloud brain and Fish
  provider receive addressed text/replies, **not** raw call PCM.
- Participant speech never reaches desktop action planning and does not
  inherit user's hands-free execution permissions.
- Neither call PCM nor an addressed transcript is stored to disk.
- Voice output is Fish Audio on a chosen non-default playback endpoint;
  Windows SpeechSynthesizer doesn't currently support this routing.
- A virtual cable must be configured by the owner; no automatic loopback
  driver installation or unauthorized voice integration.
- Echo, double routing, output device errors and speech latency require
  physical testing.
- The app does not join channels, initiate calls or impersonate the
  owner's human identity. It only speaks as a clearly disclosed AI.

## Manual tests

- Ensure START is blocked until consent and a Fish voice + non-default
  output are configured.
- Route the cable and verify the audible AI introduction reaches the
  call; check how local headphones are configured.
- Address Capri in normal spoken English; she should reply naturally.
- Try "Capri delete everything" and verify no PC action occurs.
- Verify other participants talking without "Capri" trigger nothing.
- Disable via STOP CALL MODE, STOP ALL and by closing the panel; no
  residual capture or calls to providers after the stop.
- Verify no raw recordings or live transcript are saved locally.
- Verify no input from the call can invoke settings, payments or commands.
