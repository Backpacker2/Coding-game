# TODO: toward a playable prototype

The road map for Claude's work loop (see `workflow.md`). Each milestone becomes one pull request. A milestone is ticked when its pull request is open and reviewed, with a link; the owner merges.

The prototype is done when a player can write a Python or JavaScript script in the game, press Run, watch the drone farm, see errors with line numbers, and spend items on the first unlocks. That version is `0.1.0`.

## Milestones

- [ ] **0. Workflow.** Agent definitions, the `tiered-loop` skill, `workflow.md`, this list, and the engine decision.
- [ ] **1. One set of design documents.** Merge the three open design pull requests' changelog entries into one version. Make `game-design.md` follow `languages.md` (accepted by the owner): `sense()` with `GRASS`/`SOIL`/`GROWING`/`RIPE` instead of `can_harvest()` and `get_ground()`, and one table of tick costs.
- [ ] **2. Project skeleton.** `CodingGame.Core` library, xUnit test project, `.editorconfig`, analyzers with warnings as errors, `dotnet format`, one passing test, a GitHub Actions check. Fill in "Still to fill in" in `CLAUDE.md`.
- [ ] **3. Game data.** Crops and tick costs in `data/` (JSON), loaded and checked at the edge into precise types.
- [ ] **4. Simulation.** Farm grid with wrap-around, tile states, the drone, the five commands, growth over ticks, items. Deterministic, fully tested.
- [ ] **5. Syntax tree and evaluator.** The shared tree, an evaluator that runs one tick at a time, the drone API bound once, tick costs for loop iterations and function calls, script errors with line numbers. Design sketch first (two designs, one rejected) in `docs/decisions/`.
- [ ] **6. Python front end.** Tokenizer with indentation, parser for subset steps 1 to 5, Python-style error messages.
- [ ] **7. JavaScript front end.** Tokenizer and parser for the same subset, required semicolons, `===` only, JavaScript-style error messages.
- [ ] **8. Same script, same result.** Example scripts in both languages with expected end state and tick count, checked by tests.
- [ ] **9. Godot view.** Godot project that draws the farm and drone from the game state, a script editor, Run, Stop and step, and an error panel with the line highlighted.
- [ ] **10. First unlocks.** Items pay for the unlocks of the first 30 minutes; locked features give "not unlocked yet" errors. Release `0.1.0`.

## Waiting on the owner

- Merge the design pull requests #1, #2 and #3 (milestone 1 builds on them).
