# Capri // Control Deck visual system (v1)

This interface iteration follows the owner's November 6, 2026 portrait video reference: dark technical panels, an angular yellow frame on Capri's floating dialogue, electric cyan bottom trim, pink character accents, a small avatar stamp and an explicit **TALK TO ME** call to action.

It is an original WPF rendering of those general design ideas. No reference-video frames, third-party art, UI image assets, trademarked imagery, or character recordings are copied or shipped in the app.

## Start here

Open Capri's Mainframe with a double-click on the character or `Ctrl+Shift+Space`. The window now uses four readable sections:

- **Chat:** persistent transcript, permanent input composer, Talk Once shortcut. Send with Enter and create a new line with Shift+Enter.
- **Quick modes:** large, descriptive cards for Gaming, Roblox, Roblox Development and Focus. Buttons run the existing deterministic routines through the ordinary permission policy. Apps already launched are not rolled back when interrupted.
- **Senses & control:** grouped opt-in voice, presence, screenshot-watch and desktop actions. Model configuration and one-tap device settings remain available without hunting through menu bars.
- **Apps & tools:** access the existing official Spotify/Roblox connection window, Windows Now Playing, Steam Game Center, file review and reminders.

The left sidebar keeps three truthful, live UI summaries (mic / sensing / action permission), and keeps **STOP ALL · DISARM** reachable from all four sections. The input composer remains visible so you never need to navigate home just to ask Capri something. Chat and current controls are not recreated when switching pages.

## Floating character

A new compact neon card appears for existing reactions. The card has a yellow angular border, cyan accent, small Capri letter/avatar mark, readable message, close button, and a real `TALK TO ME` action. That action arms the existing 20-second Talk Once listener. If Windows speech recognition/mic setup is unavailable, it opens the Mainframe instead. Messages animate in once (200ms) and still hide automatically after 10 seconds. The rest of Capri's animation, perching, toss physics and petting controls are unchanged.

## Accessibility / consumer behavior

- Readable Segoe UI text for primary copy, terminal-inspired monospace reserved for small labels.
- Contrasting dark backgrounds with yellow/cyan accents used for hierarchy, not a full-screen flashing rainbow.
- Buttons have 36px minimum touch-friendly height in main panels, accessible hover/keyboard focus indicators, and normal Tab traversal.
- Static, localized short accent animations instead of constant glitch flicker.
- Critical audio/screenshot/desktop-action opt-ins are **not** hidden behind routine launch cards. Hands-free remains a permission preference, not a blanket unrestricted computer-use grant.
- At narrower widths, Quick Mode cards wrap into one column; long configuration panels and transcript scroll internally while the composer and STOP ALL remain accessible.
- Theme resources are centralized in `src/Capri.Desktop/Themes/CapriTheme.xaml`, so all later modules can use the same button, panel, label and input styles.

### Known limitations

This release is a Windows WPF visual/layout update. Automated CI compilation validates the XAML and C#, but it cannot replace hands-on DPI, screen reader, high-contrast and multi-monitor testing on a real Windows PC. Presence monitoring remains opt-in, Roblox game-specific input APIs are unchanged and Spotify authorization requirements are unchanged.
