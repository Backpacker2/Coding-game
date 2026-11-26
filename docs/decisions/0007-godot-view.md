# 0007: The Godot view

- **Status:** accepted (built in 0.4.0)
- **Date:** 2026-11-25

## Context

[`0003`](0003-game-engine.md) chose C# with Godot and split the code into `CodingGame.Core` (no Godot) and a Godot project that only draws and takes input. This record fixes how the Godot project is built and how it talks to Core. The requirements are:

1. **Core stays testable without Godot.** All logic for running a script lives in Core and is covered by xUnit tests.
2. **Rendering only reads.** The view never changes the game state ([`CLAUDE.md`](../../CLAUDE.md), Game-specific).
3. **CI builds the view without the editor.** Every push runs `dotnet format`, `dotnet build -warnaserror` and `dotnet test` on Ubuntu, with no Godot installed.
4. **One source for the game data.** The rules in `data/` are read by Core and by the view alike.

## Option A: a Godot C# project in `src/CodingGame.Godot/`, built with `Godot.NET.Sdk` (chosen)

- The Godot project is a normal .NET project in `src/CodingGame.Godot/`. It uses the `Godot.NET.Sdk/4.7.2` SDK and targets `net10.0`. It references `CodingGame.Core`.
- The project is added to `CodingGame.sln`. The root `dotnet format`, `dotnet build -warnaserror` and `dotnet test` then also cover the view.
- Godot 4.7.2 (the .NET build) is pinned. The version is the one in the SDK reference.
- The scene `main.tscn` has one `Control` root with `Main.cs` attached. The whole UI is built in code in `_Ready`. Hand-written `.tscn` files are error-prone, and C# code is checked by the compiler and the analyzers.
- The game loop is in `Main._Process`. It collects `delta * TicksPerSecond` into a budget and calls `RunSession.StepTick()` once per whole tick. A speed picker in the toolbar sets the rate: Slow 5, Normal 20 and Fast 200 ticks per second. Normal is the default, so a move (20 ticks) takes about one second. The speed only changes how many ticks run per frame, never what a tick does. It is not saved.
- Godot's frame rate only decides when ticks are called. It never changes what a tick does, so the simulation stays deterministic.

### Rendering only reads

- `FarmView.ShowField(Field, IReadOnlyList<Crop>)` stores the field and `_Draw` only calls getters.
- No code in the view calls a mutating method on `Field`.
- `Main` is the only caller of `RunSession.StepTick` and `StepLine`.

### Placeholder art

- Grass is dark green and soil is brown.
- A crop is a circle. Its colour comes from a small palette, indexed by the crop's position in the rules. Its radius grows with the crop's age. A ripe crop gets a white ring.
- The drone is a blue outlined square.
- There are no image assets and no import pipeline.

### Game data

- An MSBuild target, `CopyGameData`, copies `data/*` into `src/CodingGame.Godot/data/` before each build. That folder is generated and gitignored, so there is no second source of truth.
- `Main` loads the rules with `GameRules.Load(ProjectSettings.GlobalizePath("res://data"))`. It does not catch `GameRulesException`. A missing data folder is a build error, because the build always copies it.
- The starter scripts `data/starter.py` and `data/starter.js` are read from the same folder. Both start with the comment `collect two clover`.
- Export is not a goal for the prototype.

### Logic in Core

- `CodingGame.Core.Session.RunSession` wraps parsing, the field, the evaluator and the error text. It has xUnit tests.
- `GameRules` gets `StartField` (width and height) from a new `data/field.json`. The start size is data, not a number in the view.
- The Godot code only holds UI state: the session, the language, whether playing, and the tick budget.
- Godot has its own `ScriptLanguage` type, so `Main.cs` aliases the Core one.

### Run, Step and Stop

- **Run** starts a fresh session if there is none or the old one is no longer running. Otherwise it resumes. It always sets playing.
- **Step** pauses playing and runs until the next statement starts, so the current line changes. Like Run, it starts a fresh session if there is none or the old one is no longer running. A step is capped at 10,000 ticks, because a loop like `while (ripe()) {}` never changes line.
- **Stop** discards the session, pauses and shows a fresh field. Stopping costs nothing.
- The editor and the language picker are locked while the script is running. After it finishes or fails, the player can fix the script and press Run again.
- The highlight shows the line the evaluator last started. An error, including a parse error, highlights its line in red.

## Rejected alternatives

1. **Godot project at the repo root, or outside the solution.** The root would be cluttered. A project outside the solution would not be compiled by CI.
2. **UI written by hand in `.tscn`, or the view in GDScript.** Hand-written scenes are error-prone. GDScript leaves the type system that the rest of the game relies on.
3. **A symlink or a committed copy of `data/` inside the Godot project.** A symlink does not work on Windows. A committed copy is a second source of truth.
4. **Session logic inside the Godot code.** It could not be tested without Godot.
5. **Finding the data through `Assembly.Location` or a fixed `bin/Debug` path.** Both are fragile when Godot runs the game.

## Consequences

- Building the game needs the .NET 10 SDK. Playing it needs the Godot 4.7.2 .NET (mono) editor.
- The `net10.0` line in `CodingGame.Godot.csproj` must stay. Without it, the SDK defaults to `net8.0`, and the restore fails with NU1201. The Godot editor also adds `net8.0` to a project that has no `TargetFramework`.
- CI does not run Godot. It builds and tests the view with the .NET SDK only.
- The view was checked headless and in a virtual display on Linux only. Windows and macOS are checked by the owner's play-test.
- If the Godot editor cannot run with `net10.0` on some system, the fallback is to multi-target Core. That needs a new decision.

## Update 2026-11-26: the field is 3D

Since 0.9.0 `FarmView` is replaced by `FieldView3D`, which builds a 3D scene from the same `Field` and only reads it. The rule that rendering never changes the game stays as it is. The placeholder art above describes the 2D version of 0.4.0 to 0.8.0. See [`0011`](0011-3d-field.md).
