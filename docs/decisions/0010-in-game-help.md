# 0010: In-game help and New game

- **Status:** accepted (built in 0.7.0)
- **Date:** 2026-11-26

## Context

The player needs to know which commands, constants and language features they can use now, with an example in the selected language. The list must show only what is unlocked ([`0008`](0008-unlocks.md)). The names come from one place: the drone API and the unlock data. The owner also asked for a New game button, so the player can start over after a confirmation. The requirements are:

1. **Only what is unlocked.** A locked feature is not listed, so the list answers "what can I use right now".
2. **One place for the names.** A new command, constant or crop must not need a second list in the window.
3. **Examples per language.** Each entry has a short example in Python and in JavaScript.
4. **Core stays free of Godot.** The check and the choice of entries are in Core. Godot only shows them.
5. **A reset must not lose the save format.** The save stays version 1 ([`0009`](0009-save-format.md)).

## Option A: help texts in `data/help.json`, names checked against the drone API (chosen)

- `data/help.json` holds one entry per name: a summary, a Python example and a JavaScript example. The names are the drone commands, the constants and the five keywords (`for`, `while`, `if`, `def`, `==`).
- `HelpBook.Load` reads the file and checks it at the edge. Every entry must name a known command, constant or keyword, no name may repeat, the texts may not be empty, and every command, constant and keyword must have an entry. Any problem is a `GameRulesException`, so the game does not start with missing help.
- `HelpBook.EntriesFor` returns the entries whose name is not in `GameProgress.LockedFeatures`, with the example for the chosen language. A bought unlock therefore adds its entries at once.
- The window shows the entries in a Help tab next to the Unlocks tab. (2026-11-26: the Help button that showed or hid the panel was replaced by this tab after the owner's first screenshot, in 0.7.2.)
- New game calls `GameProgress.Reset`, which empties the items and the bought unlocks in place, so the field and the unlock panel keep the same object. The scripts go back to the starter files and the language to Python. The game then writes a fresh save at once. The save file is not deleted, and `save.json.broken` is left alone.

**For:** the names have one owner (the drone API and the unlock data); the texts are game content that can change without code; a missing entry fails at load and in a test; Godot holds no list.

**Against:** the keywords are written in `HelpBook`, and the same names appear in `UnlockCheck` and in the unlock tests. Three copies is the signal to move them into one place, at the next use.

## Option B: generate the help from the drone API only (rejected)

- Each command and constant gets its name from `DroneApi`, and the summary and examples are made up by code from the name.

**For:** no second file to keep in step.

**Against:** the drone API holds no summaries and no examples. A per-language example still needs a table, so the list would still need a place for the text. Generated examples would also be wrong for the two languages in details such as the braces and the semicolons.

## Option C: a hard-coded list in Godot (rejected)

- `HelpPanel` holds its own list of names and texts.

**For:** the simplest code for the window.

**Against:** a second list of names that drifts from the drone API. A new command would be missing from the help without any error. It also puts game knowledge in the view, which [`0007`](0007-godot-view.md) forbids.

## Hiding locked entries

Locked entries are hidden, not greyed out. The unlock panel already lists every unlock with its title and features, so the player can see what is still to come there. Greying out the help would need a second mapping from each feature to its unlock title.

## New game: reset in place and save, not delete

- The reset changes the progress in memory and then writes a new save. Deleting the file would leave the file and the window out of step until the next save. Writing the new state at once keeps the rule of [`0009`](0009-save-format.md): the file always matches what the player sees.
- The reset is in place (`GameProgress.Reset`) so the objects that share the item store keep working.
- The button is disabled while a script runs, the same rule as the Buy button, so the field cannot change under a running script.
- A confirmation dialog guards the button. There is no undo.

## Decision

Option A for the help texts. Hidden locked entries. New game resets in place and saves at once.

## Consequences

- A new command, constant or crop needs an entry in `data/help.json` with a summary and an example in each language. Without it the game fails to load, and a test fails. This adds to the rule in `CLAUDE.md`: adding a command means adding it to every language at once.
- The help is a view of the game state: it is rebuilt when a language is chosen and when an unlock is bought.
- New game does not reset the speed picker, which is a view preference and not progress.
- The save format stays at version 1.

## Update 2026-11-26: Help is an overlay window

Since 0.8.0 the help is no longer a tab next to the farm. The Help button at the top right opens it in its own window, and the button closes it again. The entries, the rules and the decision above stay as they are; only the place changed. The New game button moved into the Menu at the top right.

Update 2026-11-26 (0.10.0): since decision [0012](0012-persistent-field.md) the field belongs to `GameProgress`, and `Reset` replaces it with a fresh start field. The unlock panel still keeps the same `GameProgress` object.
