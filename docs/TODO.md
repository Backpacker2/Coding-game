# TODO: toward a playable prototype

The road map for Claude's work loop (see `workflow.md`). Each milestone becomes one pull request. A milestone is ticked when its pull request is open and reviewed, with a link; the merger agent merges it at the end of the session.

The prototype is done when a player can write a Python or JavaScript script in the game, press Run, watch the drone farm, see errors with line numbers, and spend items on the first unlocks. That is the first version the owner play-tests by hand; every playable milestone after it gets a human test round too.

## Milestones

- [x] **0. Workflow.** ([#4](https://github.com/Backpacker2/Coding-game/pull/4)) Agent definitions, the `tiered-loop` skill, `workflow.md`, this list, and the engine decision.
- [x] **1. One set of design documents.** ([#3](https://github.com/Backpacker2/Coding-game/pull/3)) `languages.md` and `game-design.md` agree: sensors `ripe()` and `ground()`, crop constants, clover grows on grass.
- [x] **2. Project skeleton.** ([#4](https://github.com/Backpacker2/Coding-game/pull/4)) `CodingGame.Core` library, xUnit test project, `.editorconfig`, analyzers with warnings as errors, `dotnet format`, one passing test, a GitHub Actions check. Fill in "Still to fill in" in `CLAUDE.md`.
- [x] **3. Game data.** (0.0.6) Crops and tick costs in `data/` (JSON), loaded and checked at the edge into precise types.
- [x] **4. Simulation.** (0.0.7) Farm grid with wrap-around, tile states, the drone, the six commands, growth over ticks, items. Deterministic, fully tested.
- [x] **5. Syntax tree and evaluator.** (0.1.0) The shared tree, an evaluator that runs one tick at a time, the drone API bound once, tick costs for loop iterations and function calls, script errors with line numbers. Design sketch done: `docs/decisions/0004-syntax-tree-and-evaluator.md`.
- [x] **6. Python front end.** (0.2.0) Tokenizer with indentation, parser for subset steps 1 to 5, Python-style error messages. Design: `docs/decisions/0005-python-front-end.md`.
- [x] **7. JavaScript front end.** (0.3.0) Tokenizer and parser for the same subset, required semicolons, `===` only, JavaScript-style error messages. Design: `docs/decisions/0006-javascript-front-end.md`.
- [x] **8. Same script, same result.** (0.3.1) Example scripts in both languages with expected end state and tick count, checked by tests.
- [x] **9. Godot view.** (0.4.0) Godot project that draws the farm and drone from the game state, a script editor, Run, Stop and step, and an error panel with the line highlighted. Design: `docs/decisions/0007-godot-view.md`, play-test: `docs/playtest.md`.
- [x] **10. First unlocks.** (0.5.0) Items pay for the unlocks of the first 30 minutes; locked features give "not unlocked yet" errors. First playable release. Design: `docs/decisions/0008-unlocks.md`.
- [x] **11. Save and load.** (0.6.0) Progress (items and bought unlocks) and the player's script survive closing the game. A versioned save file, checked at the edge like the data files. Design: [`docs/decisions/0009-save-format.md`](decisions/0009-save-format.md).
- [x] **12. Run speed.** (0.6.1) A speed control in the window (slow, normal, fast) so long scripts do not take minutes; ticks stay deterministic.
- [ ] **13. Play-test round 1.** Fix what the owner finds in the play-test of 0.4.0 and 0.5.0.
- [x] **14. Documentation of what was built.** (0.6.2) README status, player guide, architecture overview.
- [ ] **15. In-game help and New game.** A panel listing the commands and constants the player has unlocked, and a New game button that clears the save after a confirmation.
- [ ] **16. Crops stay on the field.** Today every Run starts on a fresh field, so a planted radish is gone when the run ends. Keep the field between runs so planting pays off. Needs a design decision (and the save format) and the owner's view after the play-test.

## Waiting on the owner

- Answers to the open questions in `docs/game-design.md`, after the play-test.
