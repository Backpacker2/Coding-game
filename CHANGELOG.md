# Changelog

Every version of Coding Game, newest first. Version numbers follow the rules in `CLAUDE.md` (section Versions).

## 0.3.0 — 2026-11-25

- Add the JavaScript front end: a tokenizer and a parser for commands, `if`/`else if`/`else`, `while`, `for (let i = 0; i < n; i++)`, `function`, `===` and `!==`, with required semicolons and braces. Function declarations are hoisted, as in real JavaScript. Parsed JavaScript runs on the same evaluator with the same tick counts as Python.
- Report errors in the player's JavaScript with a line number and JavaScript's wording: `Unexpected token`, `expected ';'`, a hint to use `===` instead of `==`, and "'x' is not unlocked yet" for real JavaScript the game does not teach yet.
- Count Python `elif` chains toward the nesting limit, so a very long chain gives an error instead of a crash.
- Record the design in decision 0006 and add the JavaScript error messages to `docs/languages.md`.

## 0.2.0 — 2026-11-25

- Add the Python front end: a tokenizer that turns indentation into blocks, and a parser for commands, `if`/`elif`/`else`, `while`, `for ... in range(n)`, `def`, `pass`, `==` and `!=`. It builds the shared syntax tree, so parsed Python runs on the evaluator.
- Report errors in the player's Python with a line number and Python's wording: `SyntaxError`, `IndentationError`, and "'x' is not unlocked yet" for real Python the game does not teach yet. Deep nesting gives an error instead of a crash.
- Record the design in decision 0005 and add the new error messages to `docs/languages.md`.

## 0.1.0 — 2026-11-25

- Add the interpreter core: the shared syntax tree, the values a script holds, the drone API bound once for every language, and an evaluator that runs one tick per step. It supports commands, `if`/`else`, `while`, repeat loops, comparisons and the player's own functions, and reports script errors with a line number in Python or JavaScript wording.
- Record the details settled while building in decision 0004.

## 0.0.8 — 2026-11-25

- Name GitHub releases "Coding Game v<version>", list them oldest first, and mark the newest as Latest.

## 0.0.7 — 2026-11-25

- Add the simulation: a `Field` of tiles that wraps around at the edges, the drone, items, crop growth over ticks, and the drone actions move, harvest, till, plant, ripe and ground. Planting reports a broken game rule as a `PlantResult`, never as an exception.
- Publish a GitHub release named `V<version>` for every changelog version, with its changelog notes, from CI. Keep going when an old version cannot be tagged.

## 0.0.6 — 2026-11-25

- Add the game data: `data/crops.json` (clover, radish, potato) and `data/ticks.json`, loaded and checked by `GameRules.Load`, with a clear error naming the file for anything invalid.
- Decide how the interpreter is built: a tree walker with a frame stack (`docs/decisions/0004-syntax-tree-and-evaluator.md`).
- Let CI tag every changelog version on `main`, since cloud sessions cannot push tags.
- Name constants in PascalCase in `.editorconfig`.

## 0.0.5 — 2026-11-25

- Add the tiered agent workflow: `.claude/agents/` (lead and merger on Sonnet, coder on Haiku), the `tiered-loop` skill and `docs/workflow.md`.
- Add `docs/TODO.md`, the road map toward a playable prototype.
- Decide to build the game in C# with Godot (`docs/decisions/0003-game-engine.md`).
- Add the .NET solution skeleton: `CodingGame.sln`, the `CodingGame.Core` library with `Direction`, and an xUnit test project.
- Add `.editorconfig`, the .NET analyzers at `latest-recommended` with warnings as errors, and a CI workflow that checks format, build and tests.
- Replace "Still to fill in" in `CLAUDE.md` with the build, test and format commands.

## 0.0.4 — 2026-11-25

- Add `docs/languages.md`: the drone API, Python and JavaScript as the first two languages, and the first subset of each.
- Add decision record 0002: drone commands are global functions with one-word names.
- Rename the sensors in `docs/game-design.md` to the drone API names: `ripe()` and `ground()`.

## 0.0.3 — 2026-11-25

- Add decision record 0001: player scripts run in our own small interpreter, with one front end per language subset and a shared evaluator that owns tick costs.

## 0.0.2 — 2026-11-25

- Add the first draft of `docs/game-design.md`: the first 30 minutes of play, with the first three crops, the unlock order and the coding concept each unlock teaches.

## 0.0.1 — 2026-11-25

- Add the README, MIT license and documentation folder.
- Add `CLAUDE.md` with the coding standards, and `docs/coding-standards.md` with the reasoning and sources.
- Add rules for privacy and publishing; ignore the private notes folder.
- Add the workflow for branches and versions, and this changelog.
