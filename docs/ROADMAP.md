# Development plan

## Phase 1 — functional base (implemented in source)

Transparent movable Windows overlay, state-driven animation host with seven timed PNG animation states, one-shot previews and drop-in frame sequences, local Ollama chat, persistent history/settings, global shortcut, tray controls, smoke tests, Windows build pipeline.

**Release gate:** successful Windows CI run and manual keyboard/mouse tests on a Windows 10/11 desktop.

## Phase 1.5 — interactive desktop presence (implemented in source)

Window-edge snapping on drop, attaching to the frontmost eligible app, position tracking while an app moves, detaching when it minimizes or closes, and bounded toss/inertia physics. User-facing docking controls are available in the context/tray menu.

**Release gate:** confirm docking alignment, z-order, high-DPI multi-monitor behavior, and input interaction on an actual Windows desktop. Only build/smoke checks are automated so far.

## Phase 2 — faithful animation and interaction

Commission or create appropriately authorized sprite sequences of Capri. Frame-by-frame idle, blink, talking, sleeping, picked-up/tossed, perching and transition animations, with anchoring and hit regions. Improve layout for high-DPI/multimonitor, sound/voice integration, and idle system performance.

**Release gate:** smooth 60fps presentation where appropriate, no input-blocking overlay outside character regions, and clear rights to distribute each bundled asset.

## Phase 3 — conversational presence

Speech recognition with push-to-talk and optional local wake word, streaming TTS, interruption/barge-in, selectable AI providers, bounded long-term memory, local deletion/export, and latency metrics.

**Release gate:** graceful offline/error behavior, no surprise recording, explicit controls for microphone/network.

## Phase 4 — useful assistant actions

Permissioned screen capture, context extraction, Windows UI Automation (not arbitrary blind clicking), bounded plans, explicit approval for messaging/deleting/buying, audit logs, and immediate kill switch. Start with read-only screen questions before any computer-control actions.

**Release gate:** adversarial prompt-injection checks, cancellation/recovery, clear per-capability access switches, and no unattended privileged actions.

## Phase 5 — publishing

Reproducible installer, signing/reputation plan, auto-update safety, crash handling without telemetry by default, full dependency and rights inventory, user-facing security review, and independent user testing. Brand/trademark/character licensing must be resolved before a character-faithful public or commercial distribution.

**Principle:** quality over features, functional changes over commit counts.


## Phase 3.1 — local perception and real reactions (implemented in source)

Owner-initiated primary-monitor screenshots with Ollama vision, opt-in 45-second reactions displayed in the character bubble, Windows-installed microphone dictation started explicitly by the owner (prefix 'Capri'), optional local speech synthesis, and limited desktop app/website/media actions with per-action confirmation. Purchases, typing and arbitrary clicking are not implemented. This is a functional vertical slice, not feature parity with the reference project.

**Release gate:** Windows CI plus physical Windows checks of screen acquisition, microphone language-pack behavior, vision model compatibility, voice output and action confirmations. Screen perception is not continuous video, voice is not full-duplex, and speech/audio input permissions should be unmistakable to users.

## Phase 3.2 — deeper computer assistance (future)

A stronger perception/action plan with multi-monitor selection, UI Automation element grounding, explicit per-step visual confirmation, a visible action log and global cancellation. Any writing, deletion, remote communication or settings changes require a distinct irreversible-action approval. Shopping and payments remain excluded. Test prompt injection from web pages and desktop screenshots before expanding the action list.
