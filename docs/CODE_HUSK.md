# Code/Husk Mode — coding-agent presence for Capri

This release adds a **read-only** lifecycle bridge for supported installed versions of Codex CLI and Claude Code. It is an original, minimal implementation inspired by the original Capri project's coding-companion concept. It does not copy upstream source code or give Capri permission to run code, answer approvals, or control another AI.

## Setup on Windows

1. Open **Capri → Control Deck → Apps & Tools → Code/Husk**.
2. Click **INSTALL CODEX HOOKS** or **INSTALL CLAUDE HOOKS**, then review and approve the exact user-level config file change. The installer makes a dated backup when that file exists.
3. Restart your local Codex or Claude Code app/CLI. If Codex requests trust for new hooks, review the generated command and approve only if it matches the installed local relay.
4. Turn on **Watch coding assistants (this session)** in Capri. Now start a coding turn in the assistant.

Capri will react locally to prompt submission, approval required, turn completion, tool error, subagent starts and context compaction **when the local assistant version emits those hook events**. It shows up to 24 recent activity summaries in RAM and up to eight recent events in the Control Deck. You can also type **what is codex doing**, **code husk status**, **start code husk**, or **stop code husk**.

### What changes on your computer?

- Codex: adds Capri-marked command hooks to `%USERPROFILE%\.codex\hooks.json`
- Claude Code: adds Capri-marked command hooks to `%USERPROFILE%\.claude\settings.json`
- Relay script: `%LOCALAPPDATA%\CapriDesktop\husk\CapriHuskRelay.ps1`

Existing hook entries, preferences and permissions are preserved by a JSON object merge. A timestamped `.capri-backup-*.json` copy is written next to each modified existing config file before replacement. Capri doesn't overwrite malformed or unfamiliar hook structures: installation stops with an error. To uninstall, use **REMOVE CODEX HOOKS** or **REMOVE CLAUDE HOOKS**. Only Capri's specifically marked hook handlers are removed; other handlers and config fields stay in place. Restart the corresponding coding assistant after removal.

The hook command launches local Windows PowerShell with `-NoProfile -NonInteractive -ExecutionPolicy Bypass` against the **specific local Capri relay script**, not arbitrary commands. It must be installed with your consent and can be inspected in the folder above. As with any coding-agent hook, **only trust commands and scripts you have reviewed**.

## Privacy / autonomy boundaries

The relay **does not read stdin**, command arguments beyond its own validated event flags, prompts, source code, file paths, outputs or conversation transcripts. It writes only the assistant name, event kind and Unix timestamp as short JSON lines into a local `events.ndjson` buffer. The bridge never sends these events to an AI provider or external service. It never clicks a tool permission dialog, modifies source files, starts another AI, or delegates computer control.

The Code/Husk watcher defaults **OFF at every Capri launch**. While ON it updates a lease file every two seconds; hooks do not write events without a lease less than ten seconds old. Disabling Code/Husk, STOP ALL or quitting Capri removes the lease, so installed hooks become inert. Quiet Mode hides automatic character reactions while retaining a manually accessible status feed. On-screen history is session RAM only.

The presence file contains *only* sanitized metadata, not source code. It is capped in size; old records are skipped when the monitor restarts. Local utilities running as the same Windows account may still read/write this file. Consider it a convenience activity indicator, not a security proof or an authoritative audit trail.

## Limits / real-world verification

- This monitors **local Codex CLI / Claude Code hook-capable clients**, not cloud ChatGPT Work tasks or arbitrary GitHub Actions jobs.
- It doesn't stream full agent thoughts or tool contents. There are intentionally **no per-file or per-command summaries** because those could expose secrets.
- Codex and Claude Code evolve their hook schemas; confirm the relevant installed CLI version supports the documented event names.
- Hooks can impose some process-spawn overhead. This initial integration intentionally avoids high-frequency pre-tool hooks.
- Windows CI can verify that the desktop compiles and the JSON hooks preserve existing data, but live assistant-hook behavior requires testing with actual installed versions.

See [Codex lifecycle hook documentation](https://learn.chatgpt.com/docs/hooks) and [Claude Code hooks reference](https://code.claude.com/docs/en/hooks) for authoritative provider behavior.
