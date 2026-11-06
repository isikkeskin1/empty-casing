# Desktop movement and window attachment

This milestone makes Capri interact with Windows desktop **window geometry**. It does not read window contents or operate apps.

## Controls

- Drag Capri anywhere to move her.
- Drop Capri with the mouse near the **top edge** of another visible application (90 physical pixels) to snap her into the perch pose.
- While perched she follows the window's position, checking every 120ms.
- Minimize or close the target app to detach Capri automatically.
- Right-click Capri, or use her tray menu: **Perch on frontmost window** and **Detach from window**.
- Throw Capri with a quick drag and release to see damped desktop bouncing. The motion is bounded to the virtual desktop rectangle.
- Double-click opens chat. A click without moving should not detach the character from a window.

## Implementation

- \`WindowDockingService\` enumerates visible, non-minimized, non-cloaked top-level windows and reads their DWM frame bounds. It excludes our own process and titleless system windows.
- \`PerchLayout\` converts the selected horizontal point into a relative position, calculates layout independently of Win32, clamps it to available window width and prevents fully off-screen perching above a maximized window.
- \`CompanionWindow\` follows the stored window handle; when it becomes unavailable, the overlay returns to regular floating behavior.
- \`TossMotion\` is a deterministic bounded motion step that includes gravity, friction and collisions. Smoke tests exercise placement and the settling behavior.

The perch position is currently tuned for the user's 150×294 sitting animation, which displays without distortion inside the 240×384 art container. Future art can use a per-animation anchor manifest rather than a fixed approximate seat offset.

## Known limitations and manual checks

The Windows CI run verifies the C# build and deterministic core tests but **does not validate interactive window geometry** on a real desktop.

Manually test Windows 10/11 normal windows, maximized windows, minimizing/closing the attached target, mixed display scaling, multiple monitors and negative desktop coordinates. Ensure that an image overlay does not interfere with controls behind transparent character pixels. Window hit-test shaping, high-DPI alignment, a configurable seat anchor and cross-monitor tracking are planned improvements. Fullscreen games can refuse overlays or use different coordinate systems.

Do not describe this as full game integration or actual screen perception: it is window-bound movement only.
