# Capri // Screen Sketch Mode

Capri now offers a small, explicit screen-drawing tool similar in spirit to the original project's annotation features. It draws temporary strokes in an independent WPF overlay, **not into a game process**, screenshots, recordings or desktop files.

## Using it

Open **Steam Game Center → SCREEN SKETCH** or **Control Deck → Apps & Tools → DRAW ON SCREEN**, or say `Capri, draw on screen` / type `start screen drawing`. Read the confirmation carefully: **while the sketch window is active, mouse input is captured by the drawing surface rather than the underlying game or app**.

When enabled, Capri shows a neon cyberpunk floating toolbar. Pick **CYAN**, **YELLOW** or **PINK** and draw on the rest of the display. **UNDO** removes the last stroke; **CLEAR** erases all local strokes; **SAVE PNG** exports *only the strokes on a transparent background* to a file you choose.

You can leave drawing mode via **ESC**, **EXIT**, **STOP ALL**, or the automatic **three-minute timeout**. The overlay closes and normal mouse use returns. Unexported ink is discarded. The mode doesn't run automatically at startup or in response to a model's generated text.

## Privacy & game compatibility

- Screen Sketch stores no screenshots, game memory, files, or microphone audio; it only keeps strokes in temporary WPF InkCanvas memory while enabled.
- Export creates a user-chosen **transparent PNG of your drawings**, not a recording of the desktop or game.
- This is a topmost WPF window and is most suitable for Windows desktop and **borderless-windowed games**. It may fail to appear above *exclusive fullscreen* games or anti-cheat-sensitive applications.
- It uses Windows' virtual desktop rectangle; monitor DPI differences and negative multi-monitor coordinates require real-PC testing.
- The drawing surface deliberately intercepts normal clicks while open and therefore is **not** the same as the existing read-only click-through Game Coach HUD.
- No third-party image assets or original upstream source code were copied.

This closes a basic owner-driven on-screen drawing gap, **not** AI-generated pointer annotations, game input automation or visual object tracking. Those require separate perception and coordinate-validation systems.

## Release gate

The Windows CI workflow compiles the WPF InkCanvas/toolbar code, runs core strict-command tests and builds installer artifacts. Manual test: draw in a normal browser, ESC out, open a borderless game, draw and export a PNG, trigger STOP ALL and verify the game gets mouse input again.
