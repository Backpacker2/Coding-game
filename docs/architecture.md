# Architecture

This is the technical overview of the prototype (version 0.10.0). The rules for writing code are in [`../CLAUDE.md`](../CLAUDE.md). The reasons behind the big choices are in the decision records listed at the end of this document.

## Projects and folders

| Path | What it is |
| --- | --- |
| `src/CodingGame.Core/` | A class library with all the game logic. It does not use Godot, so the tests run without it. |
| `src/CodingGame.Core/Rules/` | `GameRules` reads and checks the files in `data/`. It also holds the types `Crop`, `TickCosts`, `FieldSize`, `Unlock`, `Goal` (with `GoalKind`) and `Hint`. |
| `src/CodingGame.Core/Simulation/` | `Field`: the tiles, the drone and the items. Also `Tile`, `Direction` and `PlantResult`. |
| `src/CodingGame.Core/Interpreter/` | The shared syntax tree, the `Evaluator`, the `DroneApi`, `UnlockCheck`, `HelpBook` and the script errors. |
| `src/CodingGame.Core/Interpreter/Python/` | The Python front end: tokenizer and parser. |
| `src/CodingGame.Core/Interpreter/JavaScript/` | The JavaScript front end: tokenizer and parser. |
| `src/CodingGame.Core/Session/` | `GameProgress`, `ScriptBook` (the named scripts and the selected one), `RunSession`, `HintTracker` (the count of the same error in a row), `SaveFile` and the types around them. |
| `src/CodingGame.Godot/` | The Godot 4.7 project (C#): `Main.cs` (the screen), `CodeWindow.cs`, `ResourceBar.cs`, `FieldView3D.cs`, `ScriptEditor.cs`, `UnlockPanel.cs`, `HelpPanel.cs`, `main.tscn` and `project.godot`. |
| `src/CodingGame.Godot/CodeWindow.cs` | The floating Script window: toolbar, editor, notice and Messages box. It shows the run and reports Run, Step, Stop and the language to `Main`. |
| `src/CodingGame.Godot/ResourceBar.cs` | The item bar at the top left: one pill per item with its crop colour and count. |
| `src/CodingGame.Godot/data/` | A copy of the top-level `data/` folder, made by the build and not in the repository. |
| `data/` | The game rules, the help texts and the starter scripts: `crops.json`, `ticks.json`, `field.json`, `unlocks.json`, `goals.json`, `hints.json`, `help.json`, `starter.py` and `starter.js`. |
| `tests/CodingGame.Core.Tests/` | The xUnit tests of Core. |
| `tests/examples/` | Example scripts, one folder per example. |
| `docs/` | The design documents, the decision records and the player guide. |

## Data flow

```
data/*.json ──► GameRules.Load ──► GameRules
                                       │
save.json ──► SaveFile.Load ──► GameProgress (items, bought unlocks, done goals, field)
                                       │ Field
                                       ▼
ScriptBook (selected script) + language ──► RunSession.Start
                                       │
                 front end (Python or JavaScript)
                                       │ ParseResult: a syntax tree or an error
                                       ▼
                   UnlockCheck.FindLocked (uses LockedFeatures)
                                       │ no locked feature used
                                       ▼
                   Evaluator + DroneApi ──► Field (advances one tick per step)

data/help.json ──► HelpBook.Load (checked against DroneApi) ──► HelpBook
                                                                  │ EntriesFor (LockedFeatures, language)
                                                                  ▼
                                                       HelpPanel (Godot, only shows)
```

- `GameRules.Load` reads the JSON files and the two starter scripts, turns them into precise types and checks them. A broken or missing file throws `GameRulesException`.
- `SaveFile.Load` reads `save.json`, checks it the same way and returns a `SavedGame`: the progress, the language and the `ScriptBook` with the named scripts. A missing file gives a new game.
- `RunSession.Start` parses the script. A parse error or a locked feature gives a session that has failed at once. Otherwise it creates an `Evaluator` with a `DroneApi` bound to the field of the progress.
- Each step of the evaluator runs one tick. The `DroneApi` table gives the command names and their costs, and the field holds the state.
- The items and the field live in `GameProgress`. The field keeps a reference to the same item store, so a harvest adds to the player's bank at once. A run changes the field of the progress, so crops and the drone stay as the run left them.
- `HelpBook.Load` reads `data/help.json` and checks it against the drone API and the keywords. `HelpBook.EntriesFor` returns the entries the player can use now, with the example for the selected language.

### Who reads and who changes

The Godot project never changes the field or the rules itself. It changes the game only through `GameProgress.Buy` (the Buy button in `UnlockPanel`), `GameProgress.Reset` (the New game item of the Menu in `Main`) and through `RunSession.Start`, `StepTick` and `StepLine` (the Run and Step buttons of `CodeWindow`, handled in `Main`). `GameProgress.CheckGoals` marks the goals done when `Main` ends a run or stops it. Everything else is read:

- `Main` reads the session status, the tick count, the error and the current line, and calls `SaveFile.Save` at the save points.
- `FieldView3D` reads the `Field` and builds its 3D scene from it.
- `ResourceBar` reads the items of the `Field` and shows one pill for each.
- `CodeWindow` shows the run state and the Messages box, and raises events for Run, Step, Stop and the language picker, which `Main` handles.
- `ScriptEditor` only shows and highlights the text, and colours its syntax for the selected language.
- `UnlockPanel` reads `GameProgress` to show each unlock and its price.
- `HelpPanel` shows the entries that `Main` gives it from `HelpBook`.

## Where each rule lives

| Rule | Where it is set | Read by |
| --- | --- | --- |
| Tick cost of `move`, `harvest`, `plant`, `ripe`, `till`, `ground` and `spin` | `data/ticks.json`, record `TickCosts` | `DroneApi` |
| Tick cost of a loop iteration and of a player function call | `data/ticks.json`, record `TickCosts` | `DroneApi`, `Evaluator` |
| Crops: ground, grow time, yield, seed cost and regrowth | `data/crops.json`, record `Crop` | `Field`, `DroneApi` (the crop constants) |
| Starting field size | `data/field.json`, record `FieldSize` | `GameProgress.FieldSize` |
| Unlocks: id, price, features and field size | `data/unlocks.json`, record `Unlock` | `UnlockCheck`, `GameProgress` |
| Help summary and example per language, for each command, constant and keyword | `data/help.json` | `HelpBook` |
| Growth, harvest, planting and tilling rules | `Simulation/Field.cs` | `DroneApi` |
| Command names, argument kinds and constants | `Interpreter/DroneApi.cs` | `Evaluator` |
| Which Python or JavaScript features exist | The two front ends | Parsing |
| Error wording per language | `ScriptError.ToMessage` | `RunSession.ErrorMessage` |
| Save format and its checks | `Session/SaveFile.cs` | `Main` |
| Speed of the window (5, 20 and 200 ticks a second) | `src/CodingGame.Godot/CodeWindow.cs` | `CodeWindow` |

The numbers that players can change are in `data/`. The behaviour is in code.

## Adding a drone command

The drone API is the same set of commands in every language (`CLAUDE.md`, Game-specific). The front ends do not know the command names: they produce the same call node, and `DroneApi` decides what it does. So a new command means:

1. Add its tick cost to `data/ticks.json`, to the record `TickCosts` and to `GameRules.ValidateTickCosts`.
2. Add the command to `DroneApi.Commands`, and change `Field` if the command changes the field.
3. Add its name to the features of an unlock in `data/unlocks.json`, if the player must buy it. A name that is in no unlock is always available.
4. Add a help entry to `data/help.json`: a summary and an example in each language. `HelpBook.Load` fails until every command, constant and keyword has one.
5. Add a test, an example folder in `tests/examples/` with a script in each language, and the command to `docs/languages.md` and `docs/player-guide.md`.

## Adding a language

A new language is a larger change, so it starts with a decision record that sketches two designs (`CLAUDE.md`, Design). After that, it needs:

- a front end in its own folder under `Interpreter/`, whose `Parse` method returns a `ParseResult` with the shared syntax tree,
- a new value in `ScriptLanguage`, and a case for it in `RunSession.Start`, `ScriptError.ToMessage`, `UnlockCheck` and the picker in `CodeWindow.cs`,
- a starter script in `data/` (read by `GameRules.Load`), and example folders in `tests/examples/`,
- an example for the new language in every entry of `data/help.json`.

## Testing

- `dotnet test` runs the xUnit tests in `tests/CodingGame.Core.Tests/`. They are grouped by `Rules`, `Simulation`, `Session` and `Interpreter`, with the front ends in `Interpreter/Python/` and `Interpreter/JavaScript/`.
- `tests/examples/` holds one folder per example, with `script.py`, `script.js` and `expected.json`. The expected file gives the status, the tick count, the drone position, the items, the tiles that matter and, for a failing script, the error kind and line. `ExampleScriptTests` runs every folder in both languages, so the same script must give the same end state and the same number of ticks. Error wording is tested separately.
- The Godot view has no automated tests. CI does not run Godot (decision 0007).
- CI runs `dotnet format --verify-no-changes`, `dotnet build -warnaserror` and `dotnet test` on every push and pull request.

## Decision records

| Record | Status | Decision in one line |
| --- | --- | --- |
| [0001](decisions/0001-how-player-scripts-run.md) | accepted | One shared syntax tree and one evaluator that owns the tick costs and the drone API, with a small front end per language. |
| [0002](decisions/0002-drone-api-shape.md) | accepted | Drone commands are global functions with one-word lowercase names, and constants are uppercase. |
| [0003](decisions/0003-game-engine.md) | accepted | The game is built in C# with Godot, and the logic stays in `CodingGame.Core`. |
| [0004](decisions/0004-syntax-tree-and-evaluator.md) | accepted, built in 0.1.0 | A tree walker with an explicit frame stack, running one tick per step. |
| [0005](decisions/0005-python-front-end.md) | accepted, built in 0.2.0 | A hand-written Python front end that turns indentation into blocks and uses Python's error names. |
| [0006](decisions/0006-javascript-front-end.md) | accepted, built in 0.3.0 | A hand-written JavaScript front end with required semicolons and braces, and only `===` and `!==`. |
| [0007](decisions/0007-godot-view.md) | accepted, built in 0.4.0 | Core holds all the logic, and the Godot view only draws and takes input. |
| [0008](decisions/0008-unlocks.md) | accepted, built in 0.5.0 | Unlocks are data, and a locked feature is reported before the script runs. |
| [0009](decisions/0009-save-format.md) | accepted, built in 0.6.0 | A versioned `save.json`, checked when it is loaded, with a broken file kept as `save.json.broken`. |
| [0010](decisions/0010-in-game-help.md) | accepted, built in 0.7.0 | Help texts live in `data/help.json`, checked against the drone API, and New game resets the progress in place and saves at once. |
| [0011](decisions/0011-3d-field.md) | accepted, built in 0.9.0 | The field is a 3D scene in a `SubViewportContainer`, built from primitive meshes; the drone's animation runs in real time and never changes the game. |
| [0012](decisions/0012-persistent-field.md) | accepted, built in 0.10.0 | `GameProgress` owns one field for the whole game, so crops and the drone stay between runs. The field is part of save format version 2. |
| [0013](decisions/0013-several-scripts.md) | accepted, Core built in 0.11.0 | `ScriptBook` holds named scripts in tab order, each with one text per language. The save format is version 3; the tabs UI comes with the visuals milestone. |
