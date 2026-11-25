# 0003: The game is built in C# with Godot

- **Status:** accepted by the owner, 25 November 2026.

## Context

The game needs a language and an engine. It is a code editor next to a small grid that shows a drone and crops. The simulation and the interpreter must be deterministic, tested on their own, and separate from rendering (`CLAUDE.md`, Game-specific).

## Options

| Option | For | Against |
| --- | --- | --- |
| **C# with Godot** (chosen) | Static types everywhere. A full engine with an editor, scenes, input and export to desktop and web. Godot is free and open source. | Heavier toolchain (.NET SDK plus the Godot .NET editor). Web export for C# is still limited in Godot 4. |
| TypeScript in the browser | No install for players, a light toolchain, easy to share a link. | No engine: drawing, input and UI are built by hand or from libraries. |
| Python with Pygame | Easy to read. | Players must install Python. Types are only annotations. Pygame has no UI toolkit for a code editor. |

The owner chose C# with Godot.

## How the code is split

- **`CodingGame.Core`**: a plain C# class library with no reference to Godot. It holds the simulation and the interpreter, and it is where almost all the logic lives.
- **Tests**: an xUnit project that tests `CodingGame.Core` with `dotnet test`. No Godot is needed to run them, so they run in any terminal and in CI.
- **The Godot project**: rendering, the script editor and input only. It references `CodingGame.Core`, reads the game state and sends player actions to it. It never changes the state itself.

This makes the rule "rendering only reads the game state" a property of the build: `CodingGame.Core` cannot call Godot, because it does not reference it.

## Consequences

- Everyone who works on the game needs the .NET SDK and the Godot .NET editor (the version is pinned in the Godot project).
- The formatter and linter are `dotnet format` and the .NET analyzers, with warnings treated as errors.
- Web export is not a goal for the prototype. If it becomes one, this decision is revisited.
