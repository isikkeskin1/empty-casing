# Capri character animations: default + private overrides

**The normal Windows installer now includes all seven character animation states.** Users no longer need to import a ZIP to see the real animated companion. The build downloads seven animated WebPs from one pinned, official Capri upstream commit, converts them to PNG frame sequences using Pillow, and packages them under `character-default/` beside `Capri.Desktop.exe`. The WPF character renderer uses those files immediately on startup.

Default artwork is a **separately licensed noncommercial third-party component**, not part of this repository's MIT source-code license. The Windows installer carries `character-default/UPSTREAM-LICENSE.md`, `UPSTREAM-THIRD-PARTY-NOTICES.md` and `ART-LICENSE-AND-CREDIT.txt`. The Mainframe visibly credits the original project and artist. See [assets licensing](../docs/ASSET_RIGHTS.md). Commercial redistribution would require different permission.

## Your own animation overrides

Capri still supports your `Capri-Animation-Pack-v0.2.zip` via the right-click import, tray icon, and About/Updates screen. Imported files go to `%LOCALAPPDATA%\\CapriDesktop\\character`. A present state folder takes precedence over the shipped default; missing custom states automatically fall back to the packaged animation. The importer validates archive structure and frame signatures and will not overwrite existing custom content without confirmation. Your imported art is **never replaced during app updates**.

For private development builds, the optional `assets/private/Capri-Animation-Pack-v0.2.zip` still works and remains excluded from Git. The default installed art now works even without that optional file. Do not mix it into a public release without honoring its license.

## Import WebP animations

The converter supports seven files with these exact names:

| Animated WebP | Output folder | Where it appears |
| --- | --- | --- |
| idle.webp | idle/ | Normal idle loop |
| talk.webp | talk/ | Brief reply or talk preview |
| glitch.webp | glitch/ | Glitch preview and error state |
| tossed.webp | tossed/ | One-shot toss preview |
| sleep.webp | sleep/ | Sleep until manually woken or chat starts |
| perch.webp | perch/ | Manual perch visual preview |
| perch-music.webp | perch-music/ | Manual music-perch visual preview |

Use Python 3 and Pillow on Windows:

~~~powershell
py -m pip install Pillow
py tools/import_webp_animations.py --input "C:\path\to\source-webps"
~~~

The default destination is %LOCALAPPDATA%\CapriDesktop\character. You can choose another destination with --output, then copy the seven folders into the character directory opened by the app's tray menu. The new app also accepts a prepared animation ZIP directly through its **Import animation pack ZIP…** button. Extracting manually is no longer required.

Each state consists of sequential transparent PNG frames (0000.png, 0001.png, ...) and timing.json, e.g.:

~~~json
{"frameDurationsMs":[83,83,83]}
~~~

The frame list is sorted by file name and played at its encoded delays; if a manifest is missing, the app plays frames at 100ms each. Empty states fall back to idle. For correct rendering, keep the source's full transparent canvas and consistent frame dimensions.

**Preview controls:** right-click Capri or use the tray's Preview animation menu; choose Reload character art after importing frames. Sleep and perch stay active until you wake or change state. Toss, glitch and talk run as short temporary previews. Window-edge tracking, game awareness, music beat synchronization, and voice are not implemented yet.

Do not commit externally sourced files without checking their redistribution and commercial-use terms.
