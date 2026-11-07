# Capri Desktop — Releases, updates and local diagnostics

## Automatic GitHub releases and updates

After every successful **main branch** Windows build, the same workflow now publishes an official stable GitHub Release with a unique version `v0.7.<workflow-run-number>`, including the exact installer, portable zip and SHA256SUMS. The version is stamped into the Windows assembly and Inno Setup installer, so the running app can recognize newer builds. PR checks never publish releases. The older tag-triggered release workflow skips these automatic `v0.7.*` tags to prevent duplicate jobs.

Installed copies have **Automatic Updates ON by default**. After startup and then every 30 minutes, Capri checks the pinned GitHub API for a newer stable release. Only a `Capri-Setup.exe` asset with the exact approved GitHub URL, a bounded asset size and a `sha256:` digest is downloadable. The archive is streamed to a local temporary file with a size cap, hashed, moved into staged storage only after verification, and rehashed before installation.

A verified download is **installed on Capri's next exit** using the existing per-user Inno Setup installer. A tray notification explains it is staged. To install immediately, use **About / Updates → INSTALL STAGED UPDATE**; this closes Capri first. Turning Automatic Updates off clears the pending install action. Portable copies don't auto-install because they may have custom paths. No update can start microphone capture, desktop action permissions, or transfer your personal memories.

Local character animations in `%LOCALAPPDATA%\\CapriDesktop\\character` are deliberately outside the install directory and preserved. **The public Windows installer does not bundle the third-party Capri animation pack**; use the new import button on your PC to activate the seven-state ZIP.

Limitations: downloaded installers are not yet Authenticode signed, and the GitHub-provided SHA256 digest is metadata trusted from the repository's Releases endpoint; it is not a separately signed software-update manifest. Real reboot/recovery, offline retries, anti-virus false-positive and installer replacement tests on Windows are still needed.

## Update Center

Open **Control Deck → Apps & Tools → ABOUT / UPDATES** or **system-tray icon → About / updates / support**. The Update Center displays the currently installed Windows assembly version (starting in this release: **0.6.0**). No update check runs automatically at startup.

**CHECK FOR UPDATES** makes a read-only HTTPS call to the fixed public GitHub Releases endpoint for `isikkeskin1/empty-casing`. The client accepts only a stable `vMAJOR.MINOR.PATCH` release with an official `https://github.com/isikkeskin1/empty-casing/releases/tag/...` link. It ignores drafts/pre-releases and rejects links on other hosts. It displays whether GitHub lists a `Capri-Setup.exe` asset; no executable is downloaded or run.

**OPEN OFFICIAL RELEASE** opens the confirmed GitHub release page in your default browser. You choose whether to download and run any installer. **SEE PREVIEW BUILDS** opens the repository's GitHub Actions Windows workflow. Actions artifacts expire after 14 days and aren't the same as stable Releases.

The first stable release will be automatically published by the next successful main-branch workflow; until that happens, the updater reports no stable release.

### Publish the first stable release

A separate `.github/workflows/release.yml` workflow builds and publishes a versioned GitHub Release **only when a maintainer pushes a matching tag**, for example `v0.6.0`. It refuses tags not matching both the WPF app's project version and Inno Setup's installer version.

A matching tag initiates the following sequence: Windows .NET 10 build → Core smoke tests → self-contained portable zip → Inno Setup installer → SHA256 checksum file → upload all three official release assets. Existing releases are not silently overwritten. The trigger requires normal repository write permissions, and publishing uses the repository-scoped GitHub Actions token, not secrets embedded in source.

Each published release contains:
- `Capri-Setup.exe` — Inno Setup Windows installer
- `CapriDesktop-win-x64.zip` — portable package
- `SHA256SUMS.txt` — SHA256 hashes for both artifacts

The standard `windows.yml` PR/main preview workflow also now includes `SHA256SUMS.txt` in its installer artifact. Hashes let you detect accidental or malicious download changes **relative to a trusted checksum**; they do not establish publisher identity. Unsigned installers can still show Microsoft SmartScreen warnings. **Automatic installation, rollback, digital code-signing and signed metadata are not implemented.** Do not describe GitHub checksum files as cryptographic signatures.

## Local-only support diagnostics

The same Update Center shows a **LOCAL DIAGNOSTICS** panel and offers **REFRESH REPORT**, **COPY REPORT** and **OPEN LOCAL DATA**. The app does not upload reports or send telemetry.

The journal is stored at `%LOCALAPPDATA%\CapriDesktop\diagnostics.log`, bounded to 60 entries and 48 KiB. It only writes:
- UTC timestamp
- event category (startup, shutdown, UI error, unobserved background task error, update-check error)
- **exception type name**, not the message, stack, filename, contents or memory

The generated report additionally contains the Windows OS version, architecture and .NET runtime version. It intentionally omits chat history, memories, models, tokens, system-process lists, hostnames, screen content, microphones and application paths. Review before copying. This is **not a crash minidump or complete technical trace**. Severe failures can occur before the journal initializes or after corruption, so not every crash is necessarily captured.

The local data folder also contains other personal data such as `history.json`, `personal-memories.json` and settings. **Do not upload the whole folder publicly.** Share only the curated diagnostic report after review.

## Release acceptance checklist

1. Push a valid version tag intentionally after checking the changelog and source revision.
2. Confirm the release workflow passed both build and smoke tests.
3. Download `Capri-Setup.exe` and `SHA256SUMS.txt`, calculate its SHA256 locally, and compare against the checksum from the trusted GitHub release page.
4. Test installer upgrade over an earlier build; retain a backup of the local data folder.
5. Test clean Windows installation, tray/About navigation, update check with/without a release, clipboard diagnostics and offline/error state.
6. Establish trusted code signing and update rollback before enabling any automatic installer execution.

No background updater service is installed or run by this change.
