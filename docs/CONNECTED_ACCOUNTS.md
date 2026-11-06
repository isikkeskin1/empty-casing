# Connecting Spotify and Roblox

The mainframe has a **CONNECTIONS** panel. These are real integrations
using official provider endpoints; there is no hardcoded app ID or
preauthorized personal account in the repository.

## Spotify: register and connect

1. Sign into https://developer.spotify.com/dashboard and register
   an app (note current Spotify Development Mode restrictions).
2. In the app settings register this exact redirect URL, including
   the final slash:
   http://127.0.0.1:18441/oauth/spotify/
3. Copy the application's **Client ID** (NOT the client secret).
4. Mainframe → CONNECTIONS → Spotify Client ID → SAVE CLIENT ID →
   CONNECT SPOTIFY. Your default browser opens Spotify's official OAuth
   page for consent; Capri listens on the localhost-only callback.
5. With authorized playback scopes and Spotify Premium, use ACCOUNT /
   PLAYBACK, pause/resume/next/previous, or search a track and select
   PLAY SELECTED SPOTIFY TRACK. The Spotify app/client must have an
   active Spotify Connect device. A browser/browser player may also
   supply a device depending on Spotify's support.
6. Commands in chat/voice: "spotify now", "spotify pause",
   "spotify resume", "spotify next", "spotify previous",
   "spotify play [exact song title]" or "play [song] on spotify".
   Ambiguous track names are NOT auto-selected: choose the recording
   in CONNECTIONS.

Spotify's February 2026 developer rules require the owner of a
Development Mode app to have Spotify Premium, and typically allow up
to five authorized users. Playback endpoints require Premium, with
scope and device limitations. This is a personal development integration,
not a commercial Spotify streaming client, and it does not copy,
redistribute or synchronize Spotify recordings to visuals.

Spotify: https://developer.spotify.com/documentation/web-api/concepts/authorization
Spotify development mode: https://developer.spotify.com/blog/2026-02-06-update-on-developer-access-and-platform-security

## Roblox: register and connect

1. In Roblox Creator Dashboard https://create.roblox.com/dashboard/credentials
   register an **OAuth 2.0 app**. Registration may require identity
   verification and Roblox app review for larger audiences.
2. Enable the **openid** and **profile** scopes only.
3. Register this exact redirect URI:
   http://127.0.0.1:18442/oauth/roblox/
4. Copy the public Client ID. In Capri CONNECTIONS enter it, save,
   press CONNECT ROBLOX, and approve in the Roblox browser window.
5. SHOW MY PROFILE reads your account ID/display name via official
   https://apis.roblox.com/oauth/v1/userinfo, not a cookie-based
   private Roblox client endpoint.
6. To launch a Roblox experience, enter a numeric **place ID** and
   press OPEN ROBLOX EXPERIENCE, or type "join roblox [place ID]".
   Capri opens Roblox's documented game-start link with validated
   numeric ID. Roblox now recommends creator share links for public
   promotional launch campaigns; those are distinct from OAuth APIs.

Roblox Open Cloud OAuth does **not** grant third-party apps generic
remote control over Roblox clients, in-game movement, purchases,
in-game chat, teleport to arbitrary players or other people's games.
You can expand creator-specific workflows later using additional
approved scopes and official Open Cloud APIs, with separate consent.

Roblox: https://create.roblox.com/docs/cloud/auth/oauth2-reference
Launch links: https://create.roblox.com/docs/production/promotion/deeplinks

## Privacy

- Never paste your Roblox .ROBLOSECURITY cookie, passwords, Spotify
  client secret, API keys or OAuth tokens into Capri Chat or GitHub.
- The public Client IDs may be saved in normal settings; tokens are
  protected using Windows DPAPI under your logged-in Windows user.
- PKCE/state protect login; the app listens only on 127.0.0.1.
- The provider controls are allowed only when DESKTOP ACTIONS is armed.
  HANDS-FREE skips Capri confirmation popups for supported playback
  and numeric experience launches, not purchases or account changes.
- UNLINK deletes the tokens stored on your PC. To revoke the provider's
  server-side grant, remove Capri from Spotify/Roblox connected-app
  settings as well.
- Client IDs and redirect URIs must be registered by you; CI validates
  compilation and pure logic but cannot authenticate your private
  accounts or simulate Spotify Premium/Roblox client behavior.

## Manual test checklist

- Register each app, use official browser login and validate the correct
  local callback returns once (wrong state must be rejected).
- Close browser/reject consent; no tokens should be saved.
- Verify tokens aren't in settings.json/history/debug logs.
- Restart and verify DPAPI tokens survive under the same Windows user.
- Expire access tokens and validate refresh; Roblox rotates refresh
  tokens, so make sure updated token pairs are saved.
- Spotify with Premium: profile, search, play/pause/next/previous.
  Check behavior with no active Spotify Connect device.
- Reject ambiguous song titles; only play the manually selected exact
  track URI.
- Roblox: validate the linked identity; reject nonnumeric place IDs.
  Confirm a real game-start launches Roblox or prompts a client launch.
- STOP ALL disarms new playback and game-launch actions even when
  HANDS-FREE is saved.
- Test Disconnect followed by server-side revocation.
