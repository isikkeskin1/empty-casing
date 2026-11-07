# Capri Desktop — consumer interaction + connection parity

This is an independent implementation plan based on the owner's requested Capri-like experience. Do not treat planned or source-only features as validated on a real Windows system.

## Available in source in this update

- **Scale:** right-click the floating companion → Size → 50%, 75%, 100%, 125%, 160%. The setting persists. Saved positions can be on any connected monitor, including negative-coordinate monitors. Mixed-DPI visual acceptance remains outstanding.
- **Drag and toss:** drag normally; a quick release starts bounded toss physics.
- **Shift-drop:** keeps Capri sitting at the position you dropped her, rather than attaching to a window.
- **Ctrl-drop:** uses a distinct hanging pendulum animation with the existing perch art, or optional owner-provided `hang` frames. Not new licensed hang assets.
- **Double-click:** toggle sleeping/waking. Open chat via the context menu or Ctrl+Alt+T / Ctrl+Shift+Space.
- **Type:** `ride my cursor`, `hop off`, `come here`, `go to sleep`, `wake up`. These commands are exact owner commands, never unconstrained model-generated mouse actions.
- **Cursor ride:** follows the pointer above the click target and sets the companion overlay click-through. Ctrl+Alt+C dismounts; when not riding, that same shortcut brings Capri near the pointer. Click-through and game input behavior need real Windows QA.
- **Ctrl+Alt+H:** hide/show Capri (pre-existing).
- **Ctrl+Alt+T:** focus the typing area (pre-existing).
- **Ctrl+Alt+P:** toggle Quiet Mode (new).
- **Ctrl+Alt+X:** STOP ALL INPUTS remains available. While Chaos Mode clones are active, holding Esc for ~0.85 seconds also clears clones and invokes STOP ALL. No always-on Esc hook.

## Provider connections — normal consumer vs app publisher

The Connections screen has primary **Connect Spotify Account** and **Connect Roblox Account** actions. Each uses the provider's official browser-hosted OAuth 2.0 PKCE consent, scoped account permissions and Windows DPAPI protection for resulting tokens. Users NEVER paste passwords, cookies or access tokens.

**One-time publisher setup:** Register Capri with both providers, whitelist the redirect URLs already specified in `LinkedAccountPolicy`, and configure the **PUBLIC**, non-secret GitHub Actions repository variables `CAPRI_SPOTIFY_CLIENT_ID` and `CAPRI_ROBLOX_CLIENT_ID`. CI embeds them as public assembly metadata. Without a registered public app and those variables, real OAuth login cannot succeed: Capri clearly says registration is pending rather than inventing a connection. Do not add client secrets to repo variables, build outputs or source.

A developer may still use the optional per-user public client-ID override under **Advanced**, but normal users should never be required to do so.

**No OAuth required:** Open Spotify on this PC (or Spotify's browser app); Windows' active Spotify media session can expose playback status and accept simple media commands when desktop actions are armed. The Spotify-specific command refuses to control unrelated media sessions. Generic Windows media commands continue to serve other players. Open Roblox's official website and launch a validated Roblox experience without connecting a Roblox account.

**OAuth required:** Spotify account profile, Web API track search, queue and connected-device control; Roblox Open Cloud account profile. Spotify development mode has account allowlisting/Premium and user limits; public consumer availability depends on approval/provider access. OAuth isn't equivalent to Spotify Connect's playback-device protocol.

## Not yet implemented / do not advertise as working

- Physical taskbar/icon snapping, a hand-authored hang animation in the default art pack, multi-monitor mixed-DPI collision handling, arbitrary direct-size slider, and mouse-gesture replay QA.
- Reliable multilingual voice recognition for 80 languages, full-duplex audio and streaming VAD; Windows recognizer and configured provider capabilities still govern voice.
- Full on-screen cursor automation and window-moving job orchestration. A visible progress/cancel HUD and pink **visual-only** target pointer now exist for the bounded UI Automation task runner.
- Discord voice joining, direct message sending on the user's behalf, and Discord status animation parity.
- Autonomous AI-powered talking clones, cursor stealing, or clone-directed window dragging and restoration. The new Chaos Mode does implement 1–5 visual animated clones, some game/stream gating and a hold-Esc STOP ALL while clones are active; they never touch external windows.
- Unsigned installer rollback, physical click-through QA, peripheral/Windows language compatibility, and official OAuth production acceptance.

## Manual acceptance checks

1. Use two monitors (including one positioned to the left), 100/150% DPI and saved/restored scaled sizes; verify overlay stays reachable.
2. Drag, toss, Shift-drop, Ctrl-drop near an external window, minimize/restore the window, double-click sleep/wake, ride cursor and Ctrl+Alt+C dismount.
3. Confirm cursor ride does not intercept clicks; close the app while riding and relaunch with no input capture.
4. Test Ctrl+Alt+H, Ctrl+Alt+T, Ctrl+Alt+P, Ctrl+Alt+X, and shortcut conflict warnings.
5. Test **without publisher OAuth configuration** (clear, honest fallback; no auth attempt), then test both browser PKCE flows after the publisher registers the actual applications.
6. Test Spotify local status and play/pause on an active desktop session; verify Spotify-only commands cannot pause YouTube or an unrelated player.
