# Contributing

The project is intentionally developed through **real, reviewable milestones**.

- Each commit represents one coherent, implemented change. Do not make activity/spam commits, falsify dates, rewrite history for appearance, or claim tests passed when they did not.
- Work on feature branches and open pull requests. Windows build and smoke checks should pass before merging into main.
- Keep the AI provider separate from UI and OS integration; future desktop control must be opt-in and approval-gated.
- Do not add copyrighted character sprites, voice clips, third-party code or models without documented redistribution rights. The project is independent, not a fork of another Capri application.
- Never commit API keys or personal transcripts. Application data belongs in %LOCALAPPDATA%\CapriDesktop.
- New features should add or update practical smoke tests where possible and honestly document unfinished functionality.

Build locally with .NET 10 and run the smoke project before opening a PR. Windows CI publishes a testable executable artifact for manual evaluation.
