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
  - [x] Radish could not be planted (0.10.2)
- [x] **14. Documentation of what was built.** (0.6.2) README status, player guide, architecture overview.
- [x] **15. In-game help and New game.** (0.7.0) A panel listing the commands, constants and language features the player has unlocked, and a New game button that resets the progress and scripts after a confirmation. Design: [`docs/decisions/0010-in-game-help.md`](decisions/0010-in-game-help.md).
- [x] **16. Crops stay on the field.** (0.10.0) The field belongs to the player's progress: crops keep growing in later runs, the drone stays where it stopped, and the field is saved (save format version 2). The owner chose this on 2026-11-26. Design: [`docs/decisions/0012-persistent-field.md`](decisions/0012-persistent-field.md).
- [x] **17. Syntax highlighting.** (0.7.1) The script editor colours keywords, drone commands, constants, numbers, strings and comments for the selected language.
- [x] **18. Layout from the first screenshot.** (0.7.2) The window opens maximised, the farm is centred, unlocks and help are tabs, the messages box has a title and a hint, and the drone is a marker.
- [x] **19. The farm is the scene.** (0.8.0) Floating code window, resource bar, round buttons with overlays. Owner's look: [docs/visual-direction.md](visual-direction.md).
- [x] **20. 3D field.** (0.9.0) A low-poly 3D field seen from an angle, made from Godot primitives, with growing crops and a hovering drone. Design: [`docs/decisions/0011-3d-field.md`](decisions/0011-3d-field.md).
- [x] **21. Feedback.** (0.9.1) Smooth drone movement, harvest particles, "+N" popups, light.

Milestones 22 to 28 follow [`visual-plan.md`](visual-plan.md), with the research behind them. A second orchestrator thread builds them and owns the view files.

- [x] **22. Palette, sky and light.** (0.10.1) One palette, calmer colours, clover that looks like clover, a sky gradient, fog, tone mapping and a warmer sun.
- [x] **23. The field in its world.** (0.10.4) An island around the field with trees and rocks, layered tiles, furrows, a ring under the drone.
- [x] **24. Crops that grow.** (0.10.5) Sprout, young and ripe shapes for each crop, a gentle sway when ripe, pop-in tweens.
- [ ] **25. Game UI theme.** One theme for every window and button, coloured Run and Stop with icons, icon buttons, item pills with crop shapes.
- [ ] **26. Code and world linked.** A strong running-line highlight with a gutter arrow, the running command over the drone, a red flash on errors, a run status chip.
- [ ] **27. Camera.** Orbit, zoom and reset, with limits; the fixed angle stays the default.
- [ ] **28. Polish and check.** Motion pass, colour-blindness and font-size check, before and after screenshots.

Milestones 29 to 35 follow [`gameplay-plan.md`](gameplay-plan.md). The view halves are built by the visuals thread; halves marked "Core: build thread" are built and ticked by the build thread.

- [x] **29. Next goal on screen.** (0.10.7) The cheapest unlock not yet affordable, with a progress bar; the Unlocks button glows when one is affordable.
- [x] **30. Moments.** A banner for a bought unlock with a link to its help, and a bigger burst for a first harvest. (0.11.1)
- [ ] **31. First steps.** Goals in `data/goals.json` checked from the game state and saved (Core: build thread, done in 0.12.0). A goal card under the item bar (view).
- [ ] **32. Hints when stuck.** A hint after the same error three times, counted in the session, from `data/hints.json` (Core: build thread). The hint line in Messages (view).
- [ ] **33. Script stats.** Items per 1,000 ticks and a best per crop in the save (Core: build thread). A stats line and a "New best" moment (view).
- [x] **34. Several scripts.** Named scripts, one runs, save format 3 (Core: build thread, done in 0.11.0, decision [0013](decisions/0013-several-scripts.md); the owner agreed on 2026-11-26). Tabs in the script window (view). (0.12.1)
- [ ] **35. A playful command.** `spin()` in every language (Core: build thread). The spin animation (view).

## Waiting on the owner

- Answers to the open questions in `docs/game-design.md`, after the play-test.
