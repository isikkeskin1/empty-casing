# Character animation frames

**No Capri character art is committed to this public codebase.** User-supplied animations stay on the user's PC. The new **Apps & Tools → IMPORT ANIMATION PACK** button accepts your existing seven-state ZIP and immediately activates the frames without manual extraction. For third-party artwork restrictions, read [docs/ASSET_RIGHTS.md](../docs/ASSET_RIGHTS.md).

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

The default destination is %LOCALAPPDATA%\CapriDesktop\character. You can choose another destination with --output, then copy the seven folders into the character directory opened by the app's tray menu. The program also accepts a prepared animation ZIP; extract its **contents** directly into that folder.

Each state consists of sequential transparent PNG frames (0000.png, 0001.png, ...) and timing.json, e.g.:

~~~json
{"frameDurationsMs":[83,83,83]}
~~~

The frame list is sorted by file name and played at its encoded delays; if a manifest is missing, the app plays frames at 100ms each. Empty states fall back to idle. For correct rendering, keep the source's full transparent canvas and consistent frame dimensions.

**Preview controls:** right-click Capri or use the tray's Preview animation menu; choose Reload character art after importing frames. Sleep and perch stay active until you wake or change state. Toss, glitch and talk run as short temporary previews. Window-edge tracking, game awareness, music beat synchronization, and voice are not implemented yet.

Do not commit externally sourced files without checking their redistribution and commercial-use terms.

**Recommended current import:** choose `Capri-Animation-Pack-v0.2.zip` with the new GUI importer. The ZIP is validated and extracted into the character folder; existing art is preserved if validation or replacement fails. The import is local and remains in place when the app updates. Third-party assets cannot be included in public GitHub releases without verifying redistribution rights.\n