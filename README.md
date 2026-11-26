# Coding Game

A programming game in which you control a drone on a farm with code, and in which you learn several programming languages instead of one. Inspired by [The Farmer Was Replaced](https://thefarmerwasreplaced.wiki.gg); not a copy.

## Idea

The drone has one fixed set of commands (the drone API). Each programming language is a different way to give those same commands. That way you see that a `for` loop in Python, JavaScript and C does the same thing, just written differently.

## Status

**A playable prototype (v0.6.2).** You write a Python or JavaScript script, press Run, and watch a drone farm clover, radish and potato. Errors show the line number. Harvested items buy unlocks, and your progress and both scripts are saved between sessions. The changes per version are in [`CHANGELOG.md`](CHANGELOG.md).

Install the .NET SDK 10 and Godot 4.7.2 (mono build), then open `src/CodingGame.Godot/project.godot` in Godot and press Play.
The full steps are in [`docs/playtest.md`](docs/playtest.md).

- [`docs/player-guide.md`](docs/player-guide.md): how to play, with the commands, crops and unlocks.
- [`docs/architecture.md`](docs/architecture.md): how the code is organised, for developers.

Design documents live in [`docs/`](docs/).

## License

[MIT](LICENSE)
