# Capri Session Routines + Conversation Follow-Ups

This update makes Capri a **one-command, multi-step desktop session companion**. It does not give a language model unconstrained control of Windows.

## Commands

Say `Capri, gaming mode` (or type `set up my gaming session`) to open Discord, Steam and Spotify.

Say `Capri, Roblox mode` to open Discord, Roblox home in the default browser, and Spotify. The browser may still ask to open the Roblox client.

Say `Capri, Roblox dev mode` to open the official Roblox Creator Dashboard, VS Code and Spotify. This opens the creator site only; it does not edit your experiences or call creator write APIs.

Say `Capri, study mode`, `coding mode` or `focus mode` to open VS Code, Notepad and Spotify. The routine does not automatically play music or change your audio level.

Say `Capri, list routines` for the available presets.

These phrases are intentionally matched as **explicit, complete intents**. Unrelated or combined requests cannot silently add actions. The actions are stored as a fixed allowlisted plan in `CompanionRoutine.cs`, not generated from free-form model responses.

## Execution and consent

1. Enable DESKTOP ACTIONS or HANDS-FREE for **this app session**.
2. With ordinary DESKTOP ACTIONS, Capri previews the entire batch and asks **once**, before running any step. With HANDS-FREE, the routine runs without extra popups.
3. Before each action Capri validates the exact application or HTTPS destination again and checks that the session was not disarmed.
4. Every routine has **at most four steps** and never types, sends, purchases, deletes, changes settings, runs scripts or logs into accounts.
5. `STOP ALL`, turning DESKTOP ACTIONS off, or turning HANDS-FREE off revokes any remaining routine steps. Apps already opened cannot be automatically rolled back.

There is a short 250ms pause between steps so stop controls can be processed. A missing Windows app can halt the routine with a specific failure instead of creating a fake success report.

## Conversational follow-ups

In INPUTS enable voice follow-ups, then turn MIC ON. After `Capri, what's playing?` you can ask the next question without repeating `Capri` while the 20-second follow-up window is open. **Each accepted follow-up renews the 20 seconds**, so several questions can flow naturally. Silence for more than 20 seconds ends the lease; the next interaction must use the wake prefix. The feature stays opt-in.

During Capri's own spoken output, unprompted speech is ignored. If you enabled voice interruption, `Capri stop talking` stops her reply and **`Capri stop all`** revokes the mic, screen watching, hands-free controls, and unfinished routine tasks. Physical STOP ALL and the existing shortcuts remain available.

## Limitations and next milestone

This is **deterministic local orchestration**, not unconstrained AI computer use, low-latency streamed audio, or a 24/7 voice daemon. Existing Spotify OAuth controls work separately, subject to Spotify's account, device and API limitations. Roblox OAuth links the account but does not confer live arbitrary gameplay control. A secure, creator-owned event bridge will need its own permission and runtime path rather than treating every Roblox experience as remotely controllable.

Code location: `src/Capri.Core/CompanionRoutine.cs`, `src/Capri.Desktop/App.xaml.cs`, and `src/Capri.Core/VoiceTurnRouter.cs`. Automated smoke tests are in `tests/Capri.Core.Smoke`.
