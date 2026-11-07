# Capri Desktop — public preview ship checklist

**Distribution status: public noncommercial preview, not a signed commercial consumer release.**

Capri source is MIT, but its seven bundled default character animation sequences come from a third-party project under separate **PolyForm Noncommercial 1.0.0** terms with mandatory attribution. Do not sell the combined installer or claim ownership of its character art. Obtain appropriate rights before commercial distribution. The Windows installer is currently **not Authenticode-signed**, and GitHub SHA-256 hashes do not prove publisher identity.

This checklist is intentionally explicit about what the GitHub runner can and cannot certify.

## Automatic CI gates (must be green)

- Build Capri Desktop for Windows x64 / .NET 10 in Release mode, including WPF XAML compile and native Whisper.net package references.
- Execute core smoke tests, including settings persistence/recovery, destructive privacy-data cleanup, verified model SHA-256 vs same-size tampering, OAuth validation and release integrity.
- Publish self-contained Windows app; include all seven default animation states with `0000.png`, `timing.json`, third-party license and credit notices.
- Generate portable ZIP + Inno `Capri-Setup.exe`, write SHA256SUMS, and verify asset existence and basic size bounds.
- Publish only successful `main` builds; release tag must reference the **same exact commit** as CI. GitHub's official release asset digest and the attached SHA256SUMS must be consistent.
- No live provider access token, private Fish key, Roblox session cookie, Discord user credential or client secret may be committed or built into releases.

## Human QA: clean machine and user experience

- Install on a clean Windows 11 x64 system (Windows 10 supported versions only when personally tested). Verify uninstall and in-place upgrade from previous stable version, preserving the existing user settings.
- Launch immediately after install. Confirm seven-state overlay actually animates and stays visible. Right-click, drag, toss, sleep/wake, size, multimonitor positions, cursor ride, Shift/Ctrl-drop and click-through.
- Open SETUP. Choose local Ollama or a supported cloud provider; **Test Brain** returns an actual response. Decline optional permission prompts; no mic, desktop automation or screenshots activate on their own. Check keyboard shortcuts, STOP ALL and close/restart.
- Test Whisper one-shot with consent, microphone access, unplugged device, cancellation and a valid 148 MB model. Verify languages **English, Italian and Turkish** on real recorded speech with expected transcripts; do not claim 80 languages verified based only on a multilingual model file.
- Test Discord with a real, installed VB-CABLE. Confirm **CABLE Input** is Capri's call output, **CABLE Output** is the Discord input, and Discord's actual voice output is a separate speaker endpoint. Check the built-in quiet two-tone route test, AI speech, optional real mic mix, and an informed participant's remote speech. Test calls stopped/unplugged and no duplicate audio.
- Test local AI clones with Ollama on/off, installed Windows TTS on/off, hold Esc, game/stream protection, and approved 12-second window wobble plus original-position restoration. Visual task pointer must not intercept clicks.
- Test offline startup, invalid credentials, disabled microphone permission, failed model download, invalid publisher IDs, corrupt settings with known-good backup, connection cancellation and repeated restarts.
- Test automatic update staging and safe installation from an existing managed per-user install. Make sure gameplay, unsent chat, call audio and active automation are not interrupted. Confirm portable mode requires manual apply.
- Validate diagnostics contain no raw transcription, tokens, complete file paths or screenshots. Run **About / Updates → RUN READINESS CHECK**, review, and use COPY RESULTS as a support attachment when needed.

## Required publisher services (cannot be self-completed by an end user)

- Register one **Spotify OAuth application** with the supported PKCE redirect, scopes, intended audience, Premium/development mode limits, production approval as needed, and GitHub non-secret variable `CAPRI_SPOTIFY_CLIENT_ID`. Verify an actual browser sign-in under a test account.
- Register one **Roblox OAuth/Open Cloud application** with the documented `openid profile` scope and redirect; set public `CAPRI_ROBLOX_CLIENT_ID`. Verify a real browser sign-in; do not ask consumers for cookie/token.
- Register one **Discord application** and public application ID in `CAPRI_DISCORD_CLIENT_ID`. Verify Discord desktop Rich Presence, disable/clear, and whether application status graphics are actually registered. Voice-channel bot permissions are separate; do not use self-bots.
- Obtain an organization/publisher identity and **Authenticode code-signing** solution; validate signature and SmartScreen reputation on a clean machine. Do not assume GH digest alone replaces a signature.
- Resolve **commercial rights** for borrowed Capri art/marks/voice and the source project attribution if a paid or commercial distribution is planned.
- Provide a public privacy statement, support contact, license notices, uninstall/data deletion instructions and an incident/revocation process. Third-party services may have their own data processing.
- Run manual device acceptance, collect screenshots/videos/bug reports (only from opted-in testers), and freeze the release candidate before promoting it outside a noncommercial preview.

## User troubleshooting without credentials

1. In About / Updates, RUN READINESS CHECK, copy results after review.
2. Check animation status, brain configuration, installed Windows recognizer, verified Whisper model, local Discord app and call audio devices.
3. In INPUTS, test the microphone while MIC ON is off; in CALL COMPANION, use TEST DISCORD MIC ROUTE and watch the Discord microphone level meter.
4. Review local diagnostic event categories in About / Updates. Do not upload the entire local data folder (it can contain private history).
5. Report an issue through the project's official GitHub Issues page. Provide version, OS, steps, expected/actual result and the **curated support report only**.

**Ship decision:** Without publisher OAuth apps, verified signing, commercial art rights and physical Windows/audio QA, the responsible status remains **public noncommercial preview**. Automated CI green means the software compiled and packaged—not that every real-world feature is independently verified.
