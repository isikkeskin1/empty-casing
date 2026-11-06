# Development plan

## Phase 1 — functional base (implemented in source)

Transparent movable Windows overlay, state-driven animation host with seven timed PNG animation states, one-shot previews and drop-in frame sequences, local Ollama chat, persistent history/settings, global shortcut, tray controls, smoke tests, Windows build pipeline.

**Release gate:** successful Windows CI run and manual keyboard/mouse tests on a Windows 10/11 desktop.

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
