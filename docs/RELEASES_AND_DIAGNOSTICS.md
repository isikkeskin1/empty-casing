# Capri Desktop — Releases, updates and local diagnostics

## Automatic updates from GitHub

Starting with **Capri 0.7**, every successful push to the `main` branch runs the Windows build, core tests, portable packaging, Inno Setup and SHA-256 checksums. After all stages succeed, GitHub Actions publishes a stable GitHub Release tagged **`v0.7.<run-number>`**, attaching `Capri-Setup.exe`, the portable ZIP and `SHA256SUMS.txt`. PRs and failed builds never publish releases. The GitHub token creating that release does not retrigger separate tag workflows. The app and installer versions are injected from the **same** CI build number to allow reliable comparisons.

The desktop app checks the repository's pinned `/releases/latest` endpoint about **35 seconds after startup** and then **every three hours**, with an opt-out under **About / Updates → Automatically check and download verified updates**. It ignores drafts, prereleases, invalid version tags and unexpected download URLs.

If a newer stable release exists, the updater accepts only a `Capri-Setup.exe` asset whose URL exactly matches the official repository/tag and which includes GitHub's `sha256:` release-asset digest. It downloads over HTTPS, permits only GitHub's expected release-asset redirect hosts, enforces strict file-size limits, and verifies the bytes against the SHA-256 digest before making an installer available.

**Restart to update:** When the verified download is ready, Capri notifies you via the floating companion and enables a system-tray entry and an Update Center button. Clicking it launches the verified per-user installer, closes Capri, and the silent Inno setup relaunches the upgraded companion. Existing `%LOCALAPPDATA%\\CapriDesktop` notes, settings, linked credentials and imported character art remain in place. No unverified executable or script is executed, and updates never interrupt active gameplay automatically.

The process is automatic through **checking, downloading and verification**. The final restart/install action remains user-initiated. This is deliberate while installers are still unsigned; it avoids surprise execution of remotely published software.

The existing tag-based `release.yml` workflow remains for explicitly created version tags. Ordinary `main` pushes use the automatic workflow above. Preview Actions artifacts remain available for 14 days but are **not** the auto-update channel.

### Package integrity and limitations

SHA-256 protects the downloaded installer against mismatched or corrupted bytes, as published by the trusted GitHub release. It is **not a digital publisher signature**: account compromise could replace both a package and its metadata. Windows code-signing and trusted rollback support are still needed for a mature release process. Downloaded updates can fail if GitHub is blocked, the asset digest is missing, a local security tool quarantines the installer, or installation fails. Capri reports the error and continues running.

### Bundled default animations and private overrides

The Windows workflow now downloads seven animation WebPs from a **pinned revision of the original Capri repository** and converts them to PNG frames during the build. Their independent **PolyForm Noncommercial 1.0.0** license and required attribution/third-party notices are included within `publish/character-default/` and every installer. This art is **not MIT licensed**, and bundled public releases must remain noncommercial. The Mainframe also visibly credits the original project; see [asset rights](ASSET_RIGHTS.md).

These default frames appear immediately on a new install without an import prompt. Your optional ZIP can still be imported and will override default animation files state-by-state. Imported files under `%LOCALAPPDATA%\\CapriDesktop\\character` survive app upgrades. Licensed private developer builds may still supply a ZIP under `assets/private/`.

The workflow must verify all seven default state folders and timing.json files, plus upstream notices, before publishing.

## Local-only support diagnostics

The same Update Center shows a **LOCAL DIAGNOSTICS** panel and offers **REFRESH REPORT**, **COPY REPORT** and **OPEN LOCAL DATA**. The app does not upload reports or send telemetry.

The journal is stored at `%LOCALAPPDATA%\CapriDesktop\diagnostics.log`, bounded to 60 entries and 48 KiB. It only writes:
- UTC timestamp
- event category (startup, shutdown, UI error, unobserved background task error, update-check error)
- **exception type name**, not the message, stack, filename, contents or memory

The generated report additionally contains the Windows OS version, architecture and .NET runtime version. It intentionally omits chat history, memories, models, tokens, system-process lists, hostnames, screen content, microphones and application paths. Review before copying. This is **not a crash minidump or complete technical trace**. Severe failures can occur before the journal initializes or after corruption, so not every crash is necessarily captured.

The local data folder also contains other personal data such as `history.json`, `personal-memories.json` and settings. **Do not upload the whole folder publicly.** Share only the curated diagnostic report after review.

## Release acceptance checklist

1. Push an authorized change to main; inspect the successful automatic GitHub Release and its exact source revision.
2. Confirm the release workflow passed both build and smoke tests.
3. Download `Capri-Setup.exe` and `SHA256SUMS.txt`, calculate its SHA256 locally, and compare against the checksum from the trusted GitHub release page.
4. Test installer upgrade over an earlier build; retain a backup of the local data folder.
5. Test clean Windows installation, immediate default animation playback and private override without reimporting, tray/About navigation, startup and three-hour update checks, verified staging, silent installer/relaunch, and offline/error states.
6. Establish trusted code signing and update rollback before enabling unattended automatic installer execution.

The update checks use the existing Capri process only; no background Windows service is installed.
