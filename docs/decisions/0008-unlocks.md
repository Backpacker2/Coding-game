# 0008: Unlocks

- **Status:** accepted (built in 0.5.0)
- **Date:** 2026-11-26

## Context

[`game-design.md`](../game-design.md) lets items pay for unlocks: language features, drone commands, crops and a bigger field. A feature that is locked must give a clear `'x' is not unlocked yet` error. The unlock list is an important interface: every front end, the Godot view and the later save format depend on it. The requirements are:

1. **One place decides what is locked.** A new unlock is data, not code, and it covers every language at once ([`CLAUDE.md`](../../CLAUDE.md), Game-specific).
2. **A locked feature is reported before the script runs**, with a line number, in the language of the script.
3. **Items outlive one run**, so they can pay for unlocks. The simulation stays deterministic: the same script, the same items and the same unlocks give the same result.
4. **Core stays free of Godot.** Progress lives in memory. Progress is saved since 0.6.0, see [`0009`](0009-save-format.md).

## Option A: a check on the shared tree before the script runs (chosen)

- `data/unlocks.json` lists the unlocks. Each has a price and a list of `features`: plain names such as `for`, `while`, `if`, `def`, `==`, drone commands (`plant`, `ripe`, `till`, `ground`) and constants (`RADISH`, `POTATO`, `SOIL`, `GRASS`).
- `GameProgress` in `CodingGame.Core.Session` holds the item bank and the bought unlocks. It can say which features are still locked. A feature that is in no unlock is always free (`move`, `harvest`, `CLOVER`, `NORTH`).
- After a script parses, `UnlockCheck.FindLocked` walks the tree in source order and returns the first locked use as a `NotUnlocked` error with its line. `RunSession.Start` runs this check, and a locked use fails the session with 0 ticks, just like a parse error.
- The item bank is owned by `GameProgress` and shared with the field. Harvests and seed payments change the bank directly, also when the player presses Stop. The field itself (tiles, crops, drone) is fresh on every run.
- Buying takes the price from the bank and marks the unlock as bought. The item key is removed when its count reaches 0. The Godot panel is disabled while a script runs, so the field cannot change under a running script.
- An unlock may carry a `fieldSize`. The field size is the one of the last bought unlock that has one, else the start size from `data/field.json`.
- The buy order is free. The prices already force an order, so there is no `requires` field.

**For:** one place decides; the front ends and the evaluator do not change; nothing runs before the error is shown; the error text is the same in both languages; a new feature name is a data change.

**Against:** a script that mentions a locked feature in a branch that would never run is still refused. This is wanted: the rule is simple and the player sees the error at once.

## Option B: a run-time check in the evaluator and the drone API (rejected)

- The set of unlocked features is passed into `Evaluator` and `DroneApi`. Locked commands are missing from the command table, or the evaluator checks the feature when it runs `if`, `while`, `for` or `def`.

**For:** the evaluator would stay the single owner of "what may run".

**Against:**

- A locked command missing from the table gives `'x' is not defined`, not `'x' is not unlocked yet`.
- The error only shows when the line is reached. Earlier lines have already run and changed the field, and a rarely used branch hides the lock until much later.
- Both hot paths (the command call and the statement execution) get an extra lookup.
- Every test that runs a script would need the unlock set as a new argument.

A variant was also rejected: passing a feature set into both front ends. That puts the same lock logic in two front ends, and the front ends do not know the drone command names.

## Decision

Option A. The locked features are a set of names. The tree check returns the first locked use in source order (in JavaScript, after hoisting, so a function declaration is checked first). `RunSession.Start` runs the check after parsing.

The front ends keep their own `NotUnlocked` errors for features that are not taught in this version (strings, `+`, `return`). The new check covers features that are taught but not bought yet. Both use the same message on purpose.

Progress is in memory only at this version. Progress is saved since 0.6.0, see [`0009`](0009-save-format.md).

## Consequences

- `data/unlocks.json` holds the seven unlocks of the first 30 minutes. A test checks that every feature name is a real keyword, drone command or constant.
- `elif` and `else if` are part of the `if` unlock. `==` and `!=` come with the soil unlock (`till`, `ground`). JavaScript is not an unlock in this version.
- The start field stays 3x3 for testing, so the bigger-field unlock grows it to 5x5, not to 3x3 as in the design table.
- Progress is saved since 0.6.0, see [`0009`](0009-save-format.md).
- Items now persist across runs and across Stop, and since 0.6.0 across restarts.
