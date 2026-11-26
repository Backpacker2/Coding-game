# Changelog

Every version of Coding Game, newest first. Version numbers follow the rules in `CLAUDE.md` (section Versions).

## 0.13.7 — 2026-11-26

- Fix: the Unlocks and Help windows were empty. Their content was 0 pixels high, because anchors do not size a control inside a window; it now follows the window's size.

## 0.13.6 — 2026-11-26

- Fix: when several first-steps goals get done in the same run, the goal card now ticks all of them, not only one.

## 0.13.5 — 2026-11-26

- Code and world linked: while a script runs, the running line floats over the drone (for example `move(EAST)`). When a run stops with an error, the drone shakes once and gives off a red burst. A status chip next to the tick count says Ready, Running, Finished or Error in its own colour.

## 0.13.4 — 2026-11-26

- Docs: the play-test guide covers script tabs, goals, the unlock banner, hints and spin().

## 0.13.3 — 2026-11-26

- After a run, a stats line under the toolbar shows per item what it gained, its rate per 1,000 ticks and the best rate so far. A new best turns the line green with "New best!".

## 0.13.2 — 2026-11-26

- Clean-up: the save file code is split by purpose; nothing changes for the player.

## 0.13.1 — 2026-11-26

- `spin()` now shows: the drone turns a full circle on its tile for every spin.

## 0.13.0 — 2026-11-26

- Script stats (Core): every run reports the items gained per 1,000 ticks, and the best rate per item is kept and saved (save format version 5). The stats line comes next.

## 0.12.6 — 2026-11-26

- One UI theme for the whole game: warm dark panels and windows with rounded corners, a soft shadow and cream text, matching buttons, tabs, menus, text fields, scroll bars and tooltips. Run is green, Stop is red, and the Messages box looks like the editor.

## 0.12.5 — 2026-11-26

- spin(): a playful command in Python and JavaScript that turns the drone once and costs 10 ticks.

## 0.12.4 — 2026-11-26

- A hint after a repeated error now sits under the Messages box in a calm blue, folded behind a Show hint button, instead of as a red notice. New game clears it.

## 0.12.3 — 2026-11-26

- First steps on screen: a small card under the goal bar shows the current first-steps goal in one sentence, with a ? button for a hint. A goal that gets done shows a tick for a moment before the next one; with every goal done the card hides.

## 0.12.2 — 2026-11-26

- Hints when stuck: when the same script error happens three times in a row, the message gets a hint from data/hints.json.

## 0.12.1 — 2026-11-26

- Script tabs: the script window has a tab per script and a + button to add one. Close a tab to remove its script (the last one stays), right-click a tab to rename it. During a run the tabs lock and the running script's tab is marked with ▶.

## 0.12.0 — 2026-11-26

- First-steps goals (Core): four first goals in `data/goals.json` (run the script, have 3 clover, visit every tile, buy an unlock), checked when a run ends and after a buy, and saved (save format version 4). The goal card comes next.

## 0.11.1 — 2026-11-26

- Buying an unlock shows a short banner over the field with what it adds and a Show me button that opens Help. It fades out after a few seconds.
- The first harvest of a crop, when the player has none of its item yet, gets a bigger burst.

## 0.11.0 — 2026-11-26

- Several scripts (Core): the game keeps a list of named scripts, each with a Python and a JavaScript text, and saves them (save format version 3; older saves load as one script named main). The tabs to switch between them come next. Design: decision 0013.

## 0.10.7 — 2026-11-26

- Show the next goal under the item bar: the next unlock, its price and a bar with how much of the price item the player has. When an unlock can be bought, the goal says so and the Unlocks button pulses softly.

## 0.10.6 — 2026-11-26

- Add the gameplay plan (`docs/gameplay-plan.md`): research on how farm-automation coding games play and feel, and milestones 29 to 35 (next goal, moments, first steps, hints, script stats, several scripts, a playful command), split between the view and the core.
- Add research on ready-made 3D models to the visual plan: sources, licences, the drone the owner shared, and how models would be imported.

## 0.10.5 — 2026-11-26

- Crops grow in three stages with their own shapes: a sprout, a young plant and a ripe crop. Ripe clover gets a flower, a ripe radish shows its red root, and a ripe potato bush flowers with potatoes at its base.
- Ripe crops sway gently, each tile out of step with the others, and every new stage pops in with a short bounce.

## 0.10.4 — 2026-11-26

- The field sits on a small grass island with low-poly trees and rocks around it, placed from their position so the view is the same every time.
- Tiles have a soil body and a thin top layer with a slight shade per tile; tilled soil shows furrows.
- A soft blue ring on the ground shows the tile the drone is over.
- Tuned greens so the field stands out from the island, darker clover, and a little more fog.

## 0.10.3 — 2026-11-26

- Fix: the build failed with NU1900 ("Error occurred while getting package vulnerability data") when the NuGet feed could not be reached. That warning no longer stops the build.

## 0.10.2 — 2026-11-26

- Fix: radish could never be planted, because every grass tile holds wild clover and `till()` comes later. Planting now replaces wild clover.
- The window title and the Menu show the version, so a tester can see which build runs.

## 0.10.1 — 2026-11-26

- Give the view one palette (`Palette`) for every colour of the field, the drone, the sky and the script window.
- Calmer, earthy colours; clover is drawn as a green three-leaf clover instead of yellow discs.
- A sky with a soft gradient replaces the flat background, with light fog, filmic tone mapping, glow for ripe crops and the drone's light, and a warmer, lower sun with softer shadows. The field fills the screen and is framed left of the script window.
- Fix a NaN drone tilt on a zero-length frame.

## 0.10.0 — 2026-11-26

- Crops stay on the field: what you plant keeps growing in later runs, the drone stays where it stopped, and the field is saved. Save files move to version 2; version 1 saves still load, with a fresh field. Design: decision 0012.

## 0.9.2 — 2026-11-26

- Add the visual plan (`docs/visual-plan.md`): research on how a farm-automation coding game should look and feel, and milestones 22 to 28 for palette and light, the field's surroundings, crop growth stages, a game UI theme, linking the running code to the drone, the camera and a polish pass.

## 0.9.1 — 2026-11-26

- The drone glides between tiles and leans as it flies. Harvesting bursts the crop into particles and shows the items gained above the drone; planting kicks up a little soil.

## 0.9.0 — 2026-11-26

- The field is 3D: low-poly tiles of grass and soil seen from an angle, crops that grow and glow when ripe, and a drone that hovers over its tile with spinning rotors. Everything is built from simple shapes in code. Design: decision 0011.

## 0.8.0 — 2026-11-26

- The farm fills the screen. The script is a floating window with its own Run, Step and Stop that can be moved, resized and closed (the Code button brings it back). Items show as icons with counts in a bar at the top left. Round buttons at the top right open the Unlocks, Help and Menu (New game) overlays.

## 0.7.2 — 2026-11-26

- Fix the layout from the owner's first screenshot: the window opens maximised and fills the screen, the farm is centred, the unlocks and the help are tabs next to the farm, the messages box under the editor has a title and a hint, and the drone is drawn as a marker.

## 0.7.1 — 2026-11-26

- Colour the script: keywords, drone commands, constants, numbers, strings and comments, for the selected language.

## 0.7.0 — 2026-11-26

- Add a Help button that shows the drone commands, constants and language features the player can use now, each with a short example in the selected language. New entries appear when an unlock is bought. The texts are in `data/help.json` and are checked against the drone API when the game loads.
- Add a New game button. After a confirmation it erases the items, the unlocks and both scripts, goes back to Python and saves the new state at once.
- Add `GameProgress.Reset`. Record the design in decision 0010.

## 0.6.2 — 2026-11-26

- Add `docs/player-guide.md` for players: the window, the commands, the crops and their growth, the unlocks, the error messages, tips and saving.
- Add `docs/architecture.md` for developers: the projects and folders, the data flow, where each rule lives, how to add a command or a language, testing and the decision records.
- Replace the README status, which said there was no code, with the real state of the prototype and links to both guides, and list both documents in `docs/README.md`.

## 0.6.1 — 2026-11-26

- Add a speed picker to the window (Slow, Normal, Fast) that sets how many ticks run per second, so long scripts finish faster. It only changes the pace of a run, not the result.

## 0.6.0 — 2026-11-26

- Add the save file `save.json` with version 1: the items, the bought unlocks, the script text of each language and the selected language. It is written to Godot's user data folder.
- Save after each buy, when a run ends or is stopped, when a run starts, and when the window closes. Progress and scripts now survive closing the game.
- Keep a save the game cannot read as `save.json.broken`, and start a new game with a red notice that says why.
- Keep each language's own script text, so switching language no longer carries edited text over to the other language's starter.
- Record the design in decision 0009, and update the play-test guide and decision 0008.

## 0.5.0 — 2026-11-26

- Add the first unlocks in `data/unlocks.json`: the for loop, a bigger field, radish and `plant()`, `if`/`else` and `ripe()`, the while loop, potato with `till()` and `ground()`, and functions. Each has a price in clover, radish or potato. The file is loaded and checked by `GameRules.Load`.
- Add `GameProgress`: the items now stay between runs and after Stop, the player can buy unlocks, and the field grows after the Bigger field unlock.
- Report a feature that is not bought yet as `'x' is not unlocked yet`, with its line number, before the script runs, in both languages.
- Add an unlock panel to the window, with each unlock's price and a Buy button. Buying is not possible while a script runs.
- Record the design in decision 0008, and update `docs/languages.md`, `docs/game-design.md` and the play-test guide.

## 0.4.0 — 2026-11-25

- Add the first playable window, built with Godot 4.7.2 (.NET): a script editor with line numbers, a Python/JavaScript picker, Run, Step and Stop, a tick counter, an error panel, and a farm view that draws the field, the crops and the drone. The running line is highlighted yellow, an error line red.
- Add `RunSession` in Core, which parses a script and runs it on a fresh field one tick or one line at a time, with tests.
- Read the starting field size from `data/field.json` (3x3 for testing) and add starter scripts in `data/starter.py` and `data/starter.js`.
- Record the design in decision 0007 and add the owner's play-test guide, `docs/playtest.md`.

## 0.3.1 — 2026-11-25

- Add thirteen example scripts in `tests/examples/`, each written in Python and in JavaScript with one expected result: end state, tick count, or error kind and line. One test runs them all in both languages, so the same script must give the same result.
- Remove the hand-paired end-to-end tests that the examples now cover.

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
