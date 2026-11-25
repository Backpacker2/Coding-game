# Changelog

Every version of Coding Game, newest first. Version numbers follow the rules in `CLAUDE.md` (section Versions).

## 0.0.4 — 2026-11-25

- Add the tiered agent workflow: `.claude/agents/` (lead on Sonnet, coder on Haiku), the `tiered-loop` skill and `docs/workflow.md`.
- Add `docs/TODO.md`, the road map toward a playable prototype.
- Decide to build the game in C# with Godot (`docs/decisions/0003-game-engine.md`).
- Add the .NET solution skeleton: `CodingGame.sln`, the `CodingGame.Core` library with `Direction`, and an xUnit test project.
- Add `.editorconfig`, the .NET analyzers at `latest-recommended` with warnings as errors, and a CI workflow that checks format, build and tests.
- Replace "Still to fill in" in `CLAUDE.md` with the build, test and format commands.

## 0.0.3 — 2026-11-25

- Add decision record 0001: player scripts run in our own small interpreter, with one front end per language subset and a shared evaluator that owns tick costs.

## 0.0.2 — 2026-11-25

- Add the first draft of `docs/game-design.md`: the first 30 minutes of play, with the first three crops, the unlock order and the coding concept each unlock teaches.

## 0.0.1 — 2026-11-25

- Add the README, MIT license and documentation folder.
- Add `CLAUDE.md` with the coding standards, and `docs/coding-standards.md` with the reasoning and sources.
- Add rules for privacy and publishing; ignore the private notes folder.
- Add the workflow for branches and versions, and this changelog.
