# Character animation frames

**No Capri character art is committed to this public codebase.** User-supplied animations stay on the user's PC. For third-party artwork restrictions, read [docs/ASSET_RIGHTS.md](../docs/ASSET_RIGHTS.md).

## First-run image pack import (v0.7+)

The original animation pack is **Capri-Animation-Pack-v0.2.zip**. It contains all seven states, 256 transparent PNG frames and timing files.

1. Open Capri → right-click her placeholder → **Import animation pack ZIP…** (or tray → Import animation ZIP, or About / Updates → Import Animation Pack).
2. Select the original ZIP file as-is. Capri validates the seven folders, frame numbering, PNG headers and timing before writing. There is no need to extract files manually.
3. After import, Capri reloads the frames immediately and displays the character in Idle, Talking, Glitch, Tossed, Sleep, Perch and Music-Perch states.
4. Existing custom art is preserved; you must explicitly confirm replacement. A failed pack import restores the previous state.

**Licensed local builds:** you can supply a private `assets/private/Capri-Animation-Pack-v0.2.zip` before running the Windows build/publish workflow on your own PC. The publish job copies it next to the executable, and Capri automatically imports it on first launch when the art folder is still empty. Subsequent updates preserve user art. This private ZIP is ignored by Git and must not be published to public Releases unless you have redistribution rights. Standard public Actions builds do **not** contain this copyrighted pack.

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
