# 0009: Save format

- **Status:** accepted (built in 0.6.0)
- **Date:** 2026-11-26

## Context

Progress (the items and the bought unlocks) and the player's scripts must survive closing the game. The save format is an important interface ([`CLAUDE.md`](../../CLAUDE.md)): old saves must keep loading, and a broken save must never cost the player their work. The requirements are:

1. **Versioned.** The file says which format it is, and a game that does not know the version refuses it with a clear message.
2. **Checked at the edge.** The file is turned into precise types in one place. Inside, nothing checks again.
3. **A bad save does not crash the game and is not overwritten silently.**
4. **Deterministic.** The same state gives the same bytes.
5. **Core stays free of Godot.** Core gets a file path. Godot only supplies the folder (`user://`).
6. **No new dependency.** `System.Text.Json` is already used for the data files.

## Option A: one JSON file with a version number (chosen)

- `save.json` holds `version`, `items`, `boughtUnlocks`, `language` and `scripts` (one text per language, `null` when never saved).
- `SaveFile.Load` reads the version first, then reads the whole file with the same strict options as `data/*.json` (required fields, no unknown fields). It then checks the rules that JSON cannot express: every unlock id exists in `unlocks.json` and appears once, every item is a crop, every count is at least 1. Any problem is a `SaveException` that names the problem.
- A missing file is a new player, not an error. `Load` returns a fresh game.
- `SaveFile.Save` writes to `save.json.tmp` and moves it over `save.json`, so a crash never leaves half a file. Items are sorted by name and unlocks follow the order of `unlocks.json`, so the file is byte for byte the same for the same state.
- The window catches `SaveException`, renames the broken file to `save.json.broken`, starts a new game and tells the player. The player's data is kept, saving keeps working, and nothing is silent.
- The window saves after each buy, when a run ends or is stopped, when a run starts, and when it closes.
- Unknown ids and items refuse the whole save. The ids are stable data, and dropping them would silently lose purchases. Removing or renaming an unlock later needs a new save version with a migration.

**For:** one file, one version, one load path; the state is always consistent; an old file can be read by the code for its version; the player can read it.

**Against:** a script is stored as an escaped JSON string, so it is awkward to edit by hand.

## Option B: several files, scripts as plain text (rejected)

- `progress.json` for items and unlocks, and `script.py` and `script.js` for the scripts.

**For:** the scripts open in any editor.

**Against:**

- Several files cannot be written atomically together, so a crash can leave progress and scripts from different moments.
- The version number would live in one file only, and the scripts would have no version.
- Three load paths, three kinds of failure, and a half-present set (one script missing) becomes a case to decide.
- A new language adds a new file and a new case.
- The owner wants to hand-edit scripts in the game, not on disk.

## Decision

Option A, format version 1.

## Consequences

- Progress and scripts now survive closing the game. The play-test guide says where the file is and how to reset (delete it).
- New game resets the progress and scripts and writes a fresh save at once (0.7.0). It does not delete the file, and `save.json.broken` is left alone. See [`0010`](0010-in-game-help.md).
- A save the game cannot read is kept as `save.json.broken` (an older `.broken` file is replaced) and the player starts again with a notice.
- Changing what a version means (a removed or renamed unlock, a new language, a new field) must raise `CurrentVersion` and bring a migration for the old versions, with a new decision record.
- Items from a run are saved when the run ends. A crash in the middle of a run loses that run's harvest.
- Each language now keeps its own script text while the game runs. Switching language no longer carries edited text over to the other language's starter.

## Update 2026-11-26: version 2 adds the field

Since 0.10.0 the save format is version 2. It adds a `field`: the drone's tile and every tile with its crop and age, row by row. Version 1 files still load, and get a fresh start field. The field is part of the progress, see [`0012`](0012-persistent-field.md). The rules above stay as they are.

## Update 2026-11-26: version 3 holds several scripts

Since 0.11.0 the save format is version 3. `scripts` is a list of `{ name, python, javaScript }` in tab order, and `selectedScript` names the one the editor shows. Versions 1 and 2 still load, and their one script text per language becomes one script named `main`. See [`0013`](0013-several-scripts.md).

## Update 2026-11-26: version 4 adds done goals

Since 0.12.0 the save format is version 4. It adds `doneGoals`: the ids of the first-steps goals that are done, in the order of `data/goals.json`. Versions 1 to 3 still load, with no goal done. This is one more list with the same rules as `boughtUnlocks` (checked against the rules, an unknown or duplicate id refuses the save), so no new decision record was written.

## Update 2026-11-26: version 5 adds best rates

Since 0.13.0 the save format is version 5. It adds `bestRates`: the best rate per 1,000 ticks for each item, keyed by item name and sorted by name, with every rate at least 1. Versions 1 to 4 still load, with no best rate. The list has the same checks as `items`: an unknown item or a rate below 1 refuses the save. See [`docs/gameplay-plan.md`](../gameplay-plan.md), milestone 33. No new decision record was written.
