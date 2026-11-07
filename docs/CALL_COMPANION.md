# Capri Voice Call Companion — Discord audio routing (experimental)

Capri works inside **a call you joined yourself**, using Windows audio endpoints. This does not use Discord self-bot tokens, inject into Discord, or auto-join voice channels.

## The two separate routes

**Incoming: Discord → Capri ears.** In Discord **Settings → Voice & Video → Output Device**, choose the Windows playback device you want to monitor. In Capri **CALL COMPANION → HEAR YOUR CALL**, select that exact device. Capri's WASAPI loopback reads that speaker endpoint locally, converts its audio to 16k PCM, and uses an installed Windows speech recognizer. It only sends a recognized, addressed "Capri, ..." request to the selected AI. Neither raw audio nor a full call transcript is uploaded or saved to disk.

**Outgoing: Capri (+ optional your real mic) → Discord participants.** Install a trusted virtual audio cable such as VB-CABLE. Select **CABLE Input** (virtual **playback** side) as the Fish Audio speaker device in Capri **INPUTS**. In Discord **Input Device**, select **CABLE Output** (virtual **recording** side). You and your friends may verify Discord's mic meter responds when Capri speaks. Fish voice synthesis plays directly into the cable. Enable **Also transmit my real microphone** in CALL COMPANION only if you want to hear *yourself* and Capri in the Discord call: a dedicated, session-scoped local bridge copies your chosen physical mic into the same cable output, letting Windows mix both signals. Your mic stays closed by this bridge until you explicitly start Call Mode.

> **Important:** Discord's **Output Device** should normally be your headphones or a dedicated call-only playback endpoint, **not the same CABLE Input** Capri uses to speak into Discord. Otherwise Capri can hear herself and cause feedback. A loopback captures *all* audio playing through the selected endpoint, not only Discord. Dedicated endpoints reduce incidental audio but do not guarantee process isolation.

## Set up

1. In **INPUTS**, choose Fish Audio, enable spoken replies, enter your Fish provider credentials through secure setup, select the virtual cable's playback device as Capri's Fish speaker, and select your physical microphone.
2. In Discord → Voice & Video, set **Input Device = CABLE Output** and **Output Device = your headphones/dedicated call speaker**. Discord should be set to an input mode compatible with your voice level (test Voice Activity vs Push-to-Talk).
3. In CALL COMPANION, select the same outgoing Discord **speaker** device for incoming capture, and optionally check **also transmit my real microphone**.
4. Join Discord voice yourself, explain that Capri is an AI participant and obtain other participants' permission to let her listen/respond. Check the consent box and press **START CALL MODE**.
5. Capri introduces herself audibly. A participant can say **"Capri, [question]"**; she will attempt a short answer through the virtual microphone.
6. To stop audio capture and any real-mic bridge, press **STOP CALL MODE**, close the Call panel, or use **STOP ALL INPUTS**. Losing the selected Windows speaker endpoint also closes the real-mic bridge.

## Discord desktop actions

The Call panel offers **Toggle Mute** (Discord's documented Ctrl+Shift+M) and **Toggle Deafen** (Ctrl+Shift+D). These local keyboard shortcuts are only sent on explicit owner command when DESKTOP ACTIONS or HANDS-FREE is enabled. They report **shortcut sent**, not a verified mute state: Capri cannot query whether Discord is currently muted/deafened.

Only genuine authorized Discord bot APIs should be used for future channel automation. Do **not** create self-bots or reuse browser cookies/user tokens to send messages or join calls.

## Current language and privacy boundaries

- Windows speech recognition runs in one **installed** recognition language at a time. INPUTS lists the engines actually installed on the computer. This is not verified 80-language speech input. The selected local recognizer applies to regular microphone and call audio.
- Live call prompts are conversational only: no desktop actions, purchases, messages, credentials, or private chat retrieval from participant speech.
- Call audio isn't written to a recording file or uploaded; generated reply text is provided to the configured Fish Audio service, and addressed questions may be sent to the chosen cloud AI model.
- If the speaker endpoint is unplugged or recognition fails, stop the session; review routing and permissions before restarting.
- A software-only CI pass cannot verify physical cable behavior, audio latency, echo suppression, exclusive microphone contention, or Discord's actual device configuration.

## Manual acceptance test

With participant permission, play the audible introduction; verify Discord mic indicator shows Capri's output. Speak into the selected microphone (optional bridge) and confirm the same indicator reacts. Speak from the **remote** participant side and verify Capri's recognition. Try an unrelated YouTube/notification sound and confirm the call route may hear it if sharing the same endpoint; keep Discord on a dedicated endpoint for privacy. Disconnect the incoming speaker: both capture and mic bridge must stop. Test STOP ALL while speaking and confirm no residual audio.
