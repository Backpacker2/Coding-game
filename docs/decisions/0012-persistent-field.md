# 0012: Persistent field

- **Status:** accepted (built in 0.10.0)
- **Date:** 2026-11-26

## Context

Every run started on a new field of ripe wild clover, so anything the player planted was gone when the run ended. The field is what the player works on, so it should belong to the player's progress: it survives between runs and is saved. Time does not pass between runs. The field only changes during ticks, so the game stays deterministic: the same script on the same starting state, including the field, gives the same result ([`CLAUDE.md`](../../CLAUDE.md), Game-specific). The requirements are:

1. **One field per game.** The tiles, the crops with their age and the drone's position belong to the progress.
2. **The bigger field keeps what is on it.** Buying BIGGER_FIELD must not wipe out planted crops.
3. **Deterministic.** Two runs of the same script from the same state give the same end state.
4. **Saved.** The field is part of the save file ([`0009`](0009-save-format.md)).

## Option A: the progress owns one field (chosen)

- `GameProgress` owns one `Field` for the whole game. It is created with the start field for a new player, and restored from the save file for an old one.
- `RunSession.Start` runs on `progress.Field`. The drone starts where the last run left it.
- Buying a bigger field grows the field in place (`Field.Grown`): the tiles that are already there are kept with their age, and the new tiles are grass with ripe wild crop.
- New game makes a fresh start field.
- The field is saved with the progress, as a list of tiles in row-major order and the drone position. The save format moves to version 2 (see [`0009`](0009-save-format.md)).

**For:** one source of truth for the tiles; the drone keeps its place; a run changes the field it is given, the same as it changes the items.

**Against:** a run that is stopped halfway keeps what it changed (for example a planted crop, or the drone's new place). This is the same as a harvest, which is kept too.

## Option B: a fresh field per run, with the planted tiles saved separately (rejected)

- Each run still starts on a new field built from the start size. The planted tiles are saved in their own list and put back on the new field when the run starts.

**For:** `RunSession` stays as it was, and a run cannot change the state until it ends.

**Against:**

- Two sources of truth for the same tiles: the field of a run and the saved list of planted tiles must be kept in step, and every new rule about a tile (for example soil) has to be written in both places.
- The drone would jump back to (0, 0) every run, unless its position is saved as well. That is the same work again.
- The saved list would have to be replayed on every run, which costs work the field already has.

## Consequences

- `Stop` no longer resets the field, because the run has already changed it. The idle view shows the real field, not a fresh one.
- Save format version 2 adds the field. Version 1 saves still load, and get a fresh start field (the same as before this decision).
- A new game gives a fresh start field, the same as the reset of the items and unlocks in [`0010`](0010-in-game-help.md).
- A test that asserted a fresh field every run is changed to the new rule: a crop planted in one run is still there in the next.
