# Capri Game Center — Steam launch and game coaching

## 1. Launch an installed PC game

Open Mainframe → **GAMES**, then REFRESH to read the local installed Steam
library metadata. Select a game and press LAUNCH SELECTED, or tell Capri
"play [exact Steam game title]" or "launch game [title]".

Game Center discovers the Steam root from the current user's Steam registry
key, then HKLM fallback and the usual Windows Steam folder. It reads
Steam's libraryfolders.vdf paths and appmanifest_*.acf files in the
library's steamapps directory. No network, Steam API keys or download
service are needed. A numeric app ID is verified against its manifest and
the game is launched with the registered steam://rungameid/ protocol.
Steam may show its own update, login or game launcher screens.

The catalog doesn't include Roblox games, game shortcuts not registered
in Steam, or titles installed via Xbox, Epic, GOG or Battle.net.
We will implement those as separate provider adapters rather than
execute arbitrary paths suggested by a model.

## 2. Ask for help while playing

Click **GAME HELP** or say/type "Capri, game help: what do I do next?"
A single screenshot from your configured monitor is passed to the
configured vision brain. Capri can identify visible objectives, UI
controls, enemies and likely next steps. It can give tips and explanations,
but does not secretly read game data, memory, accounts or network traffic.

For local Ollama vision, the image is processed locally. For cloud models,
the screenshot is transmitted only after a new approval. HANDS-FREE
controls do not override screenshot-sharing privacy. Check that your
screen doesn't show private notifications or chats before sharing.

This is **not** cheats, player automation, anti-cheat evasion, gameplay
control, reliable game identification or a live continuous coach.
Advice may be wrong when a screenshot lacks context.

## Manual Windows acceptance

- Verify installed Steam games in both default and additional library folders.
- Verify an invalid/mismatched appmanifest is ignored; no arbitrary EXEs.
- Verify duplicate/partial titles are not guessed when ambiguous.
- Test a real game launch with desktop actions enabled, and again with
  HANDS-FREE armed to confirm no repeated Capri approval dialog.
- Test a disabled desktop action state; games must not launch.
- Test STOP INPUTS during any pending permission check; no new launch.
- Test a local screenshot coaching session and a cloud one: cloud requires
  separate one-frame confirmation.
- Test games with multiple monitors, HDR, full-screen exclusive mode, black
  frames and anti-cheat. Windows screen capture may fail in exclusive mode.
- Verify screenshots are not saved in permanent chat history and no raw
  image reaches Fish TTS.
- Test game coaching during active voice chat. It must not interrupt other
  people's calls or transmit their audio.

## Next work

The separate Now Playing feature reads local Windows media-session metadata and gives short track-change reactions while DANCE TO AUDIO is enabled. Discord voice participation needs approved
Discord integration/voice transport and explicitly disclosed AI identity,
plus per-call audio routing; ordinary app launching is not enough to join
or speak in a call.
