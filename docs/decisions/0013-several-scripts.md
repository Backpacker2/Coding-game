# 0013: Several scripts

- **Status:** accepted (Core built in 0.11.0; the tabs UI comes with the visuals milestone 34)
- **Date:** 2026-11-26

## Context

The player has one Python script and one JavaScript script, one for each language ([`0009`](0009-save-format.md)). The owner wants several named scripts, shown as tabs, so the player can keep a harvest script and a planting script side by side. Only one script runs at a time. The requirements are:

1. **Named scripts in an order.** The player can add, rename, remove and select scripts. The order of the tabs is kept when the game is saved.
2. **Each language keeps its own text.** Switching language inside a script must not lose or carry over text, as in [`0009`](0009-save-format.md).
3. **One selected script.** The editor shows the selected script and Run runs it.
4. **Saved.** Every script and the selection are part of the save file, and old saves keep loading ([`0009`](0009-save-format.md), [`0012`](0012-persistent-field.md)).

## Option A: a script has a name and one text per language (chosen)

- `ScriptBook` holds an ordered list of scripts. Each script has a name and two texts, one Python and one JavaScript.
- The language picker changes the language of the book, and the editor shows the text of the selected script in that language, as the game does today.
- New scripts start with the starter texts. A new script is named "script 2", "script 3" and so on, with the first free number.
- The save format moves to version 3: `scripts` is a list of named scripts and `selectedScript` names the selected one. Versions 1 and 2 load as one script named `main`.

**For:** the tab is the unit the player thinks in. The language is a setting of the whole book, so a script can be read in either language, as now. Adding a language later adds one text to each script.

**Against:** every script holds a text for every language, even when the player never writes one of them.

## Option B: a script has one fixed language and one text (rejected)

- Each script is either Python or JavaScript, and has one text. A Python tab and a JavaScript tab are separate tabs.

**For:** the save format is simpler: one text per script, and no empty texts.

**Against:**

- Switching language would mean switching tabs or converting the text. Today the player switches language and keeps the text of each language. That behaviour would change.
- A tab would need a language icon, and "the same script in two languages" would be two tabs with two names.
- The picker and the tabs would both choose the language, so one choice would have two places.

## Decision

Option A.

## Consequences

- `ScriptBook` (in `Session/`) is the one place for the names, the order, the selection and the texts. Its name rule (1 to 20 characters, trimmed, no line break, unique ignoring case) is checked when a name is set and when a save file is read.
- Save format version 3 holds `scripts` as a list of `{ name, python, javaScript }` and `selectedScript`. Versions 1 and 2 load as one script named `main`; a text that is `null` in those files becomes the starter text. See [`0009`](0009-save-format.md).
- The starter texts move from `Main.cs` to `GameRules`, because they are game data read from `data/starter.py` and `data/starter.js`. `GameRules.StarterText` gives the text for a language.
- New game resets the book to one script named `main` with the starter texts, and saves at once.
- The tabs UI and the rule for which script Run uses (always the selected one) are built with the visuals milestone. Until then the editor shows the selected script, which is `main`.
