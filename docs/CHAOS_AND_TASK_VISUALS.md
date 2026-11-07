# Chaos Mode, hanging animation, and the pink task pointer

## Chaos Mode — animated clones

Type **clone yourself**, **make 3 clones** (1–5), or **start chaos mode** to display non-interactive, animated Capri clone windows around your current pointer. They use the same local seven-state character art as Capri, drift slowly, and display short alternating speech bubbles. This is a *visual* exchange, not cloned AI agents using a new brain/context or real microphones.

Clones are intentionally **click-through**: they never intercept mouse clicks, press keys, drag other applications, or steal the real cursor. Right-click the main Capri overlay to spawn/stop clones, or type **stop clones**, **chaos off**, or **end chaos mode**.

**Hold Esc for ~0.85 seconds** while Chaos Mode is active: a brief press does nothing, a sustained press closes all clones and triggers Capri's STOP ALL INPUTS cleanup (cancelling a task, stopping voice/call capture, and ending cursor ride). This uses a short-lived Windows key-state poll only while clones exist, not a permanent global keyboard hook or key logger. No cursor/window state is modified by clones, so there are no external windows to restore.

Chaos Mode refuses to start when common game executables are foreground or known OBS/Streamlabs broadcast applications are running; if such an application becomes active while clones exist, clones close. This is a conservative list, not exhaustive game detection. Physical fullscreen and multi-monitor testing remains required.

## Distinct hanging pose

Ctrl-drop near a window attaches Capri on its edge in **HANGING** mode. If custom character art includes a `hang` PNG sequence it takes precedence. Otherwise Capri animates the existing perching sequence with a separate pendulum-like inverted sway. This is a procedural hanging pose, not a falsely advertised new licensed animation pack. Normal drop still perches.

## Pink pointer + steps in AUTO TASK

The existing task engine authorizes a **single** Windows UI Automation navigation objective in one pinned external window. It runs up to five safe, explicitly validated actions under a 90-second lease, using named tabs, tree expanders and navigation buttons. In this release, the engine shows:

- A prominent dark/pink progress HUD with recent steps and STOP TASK.
- A click-through **pink visual arrow** at the *freshly revalidated* target's UIA rectangle center before the action.

This pink arrow is **not** your mouse pointer. It does not move the OS cursor, click on arbitrary positions, send text, or access sensitive UI. The user can keep using their physical cursor. STOP TASK and STOP ALL prevent further actions; actions already committed by Windows cannot necessarily be undone.

## Future work, not done yet

The reference's autonomous, speaking AI clones; cloning a live conversational agent; grabbing users' cursors; dragging other application windows with exact reversible rollback; a process-isolated Discord output capture; Discord bot voice attendance and messages; unbounded unrestricted computer control; and verified 80-language speech input are **not included in this release**. All would require separate technical design, privacy/permission systems and Windows device testing.
