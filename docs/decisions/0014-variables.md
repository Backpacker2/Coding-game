# 0014: Variables and counting

- **Status:** accepted (built in 0.15.0)
- **Date:** 2026-11-26

## Context

The for loop of [`game-design.md`](../game-design.md) unlock 1 names its counter `i`, but until now the player could not use it. The owner tried the counter and wanted to read it. The design says variables belong to "after minute 30" and asks whether the loop variable teaches them (open question 5). The unlock "Variables and counting" (40 potato) gives the counter, variables, `+ - *` and `< > <= >=`, in both Python and JavaScript. The same script must mean the same thing in both languages ([`languages.md`](../languages.md), principles 2 and 3).

The two languages scope names differently. In Python a variable made in a function is local to it, and a loop counter stays after the loop. In JavaScript `let` is block scoped, and a name made with `let` inside a block is not visible outside it. A design has to decide which of these rules the game follows.

## Option A: one variable table, and a check before the run (chosen)

- Each run has one table of variables. A loop counter is not in that table: it is read from the frame of its loop, which exists only while the loop runs.
- `ScopeCheck` (in `Interpreter/`) runs after parsing and before the unlock check. It only accepts scripts whose meaning is the same in real Python and real JavaScript:
  - the counter is read only inside its own loop, and is never changed;
  - no counter has the same name as the counter of a loop around it;
  - a variable is changed only outside functions; a function can read it;
  - JavaScript `let` is used only at the top level, and only once per name;
  - no name of the game is a variable or counter: no command, no constant and not `range`.
- The evaluator then needs no scope rules of its own. A name is looked up in the counters of the running loops and then in the one table, and the check has already refused every script where that could give a different answer from Python or JavaScript.

**For:** the evaluator stays small and fast, because it has one table and no scope rules in its hot path. Each rule is in one place, `ScopeCheck`. The errors come before the run, so the drone does not move, as with unlocks.

**Against:** some valid Python and JavaScript scripts are refused, such as a variable changed in a function. The list of limits is the price, and it is written below.

## Option B: real scopes per language in the evaluator (rejected)

- The evaluator gets a scope model for each language: a function scope for Python, and a block scope for JavaScript. Each front end tells the evaluator which model to use.

**For:** more of real Python and JavaScript works, so a player's script is closer to what they would write in a real editor.

**Against:**

- Two scope models live in one evaluator, so a change to one language can break the other. Knowledge of the rules is split across the front ends and the evaluator.
- The lookup in the hot path (each read and each write of a variable) gets more work: a chain of frames, chosen by the language.
- The extra freedom matters only for scripts a beginner rarely needs yet.

## Decision

Option A. The limits are the list in Option A and in the table below. Each can be lifted later, one at a time, without breaking a script that is already accepted: a rule that is lifted only allows more scripts.

## Consequences

- The name rules of both languages are in one place, `ScopeCheck`, so a rule is changed in one file.
- `count += 1` is refused, with `'+=' is not part of this game yet`, as in the other syntax the game does not have.
- The error messages are listed in [`languages.md`](../languages.md) and in [`player-guide.md`](../player-guide.md).
- Assignment, arithmetic and comparison cost 0 ticks, as in [`languages.md`](../languages.md).

### Limits

| Not accepted | Why it is refused | Can be lifted later? |
| --- | --- | --- |
| Reading a loop counter outside its loop | The Python counter stays after the loop, the JavaScript one does not. | Yes, once both give the same value. |
| Changing a loop counter | The counter is set by the loop only. | Yes, with a rule for what the loop does next. |
| A loop inside a loop with the same counter name | JavaScript would shadow it; Python would overwrite it. | Yes, with one rule for both. |
| Changing a variable inside a function | Python makes a local name there; JavaScript changes the outer one. | Yes, with a `global` rule. |
| `let` inside a block, or `let` twice for one name | JavaScript block scope differs from Python. | Yes, with block scope in the evaluator (Option B). |
| A variable or counter with a game name, or `range` | The name would hide a command or constant in one language. | No need: the names are the game's. |
| `+=` and the other shorthand operators | Not taught yet. | Yes, as a separate unlock or a separate lesson. |
