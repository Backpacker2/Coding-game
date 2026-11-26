# Languages

This document defines the drone API, the first two languages, and the small part of each language that players learn first.

**Status:** proposed, and accepted by the owner on 25 November 2026. What each command does to the field, what it costs and when it is unlocked are game rules: [`game-design.md`](game-design.md) and `data/` have the final say on those. This document only says how the commands are written and which language features exist.

## Principles

1. **One API, many languages.** The drone has one set of commands. A language only changes how you write them, never what they do (`CLAUDE.md`, Game-specific).
2. **Real code only.** Every script the game accepts is valid code in the real language and does the same thing there. Players can take what they learn to a real editor. The game may accept *less* than the real language, never something different.
3. **Same script, same result.** A script translated line by line into another language gives the same end state and the same number of ticks (`CLAUDE.md`, Tests). That only works if the subsets cover the same ideas, so the subsets are chosen together.
4. **Small first.** A player starts with a handful of constructs and unlocks more. Anything not yet unlocked gives a clear error, not a crash.

## The drone API

### Shape and names

The commands are plain global functions: `move(NORTH)`, not `drone.move(NORTH)`. Calling a function is the first thing a player learns, and objects and methods come much later.

Every command name is a single lowercase word, and every constant a single uppercase word. That way one name is correct in every language, and the question "`can_harvest` or `canHarvest`?" never comes up for the API. Both choices, and the rejected alternatives, are in [`decisions/0002-drone-api-shape.md`](decisions/0002-drone-api-shape.md).

### Commands

| Command | Arguments | Returns | Unlocked at (game-design step) |
| --- | --- | --- | --- |
| `move(direction)` | `NORTH`, `EAST`, `SOUTH` or `WEST` | nothing | 0 |
| `harvest()` | none | nothing | 0 |
| `plant(seed)` | a crop constant, such as `RADISH` | nothing | 3 |
| `ripe()` | none | `True` / `true` if the crop on this tile can be harvested | 4 |
| `till()` | none | nothing | 6 |
| `ground()` | none | `GRASS` or `SOIL` | 6 |

The two sensors each answer one question with one simple value, so a player can use them with nothing more than `if` and `==`:

```python
if ripe():
    harvest()
```

Crop constants (`RADISH`, `POTATO`) come from the crop list in `data/`, not from code. The language layer reads that list, so adding a crop never means editing each language.

Constants instead of strings (`plant(RADISH)`, not `plant("radish")`): a typo then gives a clear "not defined" error on the right line, and players do not need to know about strings yet.

A command given a wrong kind of argument, such as `move(5)` or `plant(NORTH)`, is an error in the script, reported like any other (see Errors). Everything that depends on the field, such as planting on the wrong ground, is a game rule and is described in `game-design.md`.

### Ticks

The tick cost of each command lives in `data/` (first numbers in `game-design.md`).

Language constructs cost ticks too, otherwise `while True:` with nothing inside would be free and would never end. To keep principle 3, ticks are charged for ideas that every language shares, never for syntax:

| Construct | Ticks |
| --- | --- |
| A drone command | its cost from `data/` |
| One loop iteration | 1 |
| Calling a function the player wrote | 1 |
| Everything else (assignment, arithmetic, comparison, `if`) | 0 |

These numbers are game rules too and move to `data/`. The point fixed here is *what* is charged.

## The first two languages

**Python first, JavaScript second.**

- They are the two languages people most often learn first, and both are widely used in real work.
- Both are dynamically typed, with similar rules for numbers, booleans and functions. The same small script really means the same thing in both, which principle 3 depends on.
- The difference that remains is the syntax: indentation and `:` against braces and `;`, `and` against `&&`, `def` against `function`. Seeing that difference is exactly the point of the game.
- Python goes first because it has the least syntax. `game-design.md` proposes that JavaScript appears around minute 30, once the player knows loops, conditions and functions.

**Rejected: Python and C.** C shows a bigger contrast (static types, compiling, memory), but it needs type declarations from the very first line, so a first C script cannot match a first Python script. C fits better as a third language, once players know the ideas and are ready for types.

**Rejected: Python and Lua.** Lua is small and easy to interpret, but far fewer players will ever use it outside the game.

## The first subset

The steps follow the unlock order in `game-design.md`. Each row is one idea, written in both languages.

### Sequence (step 0)

| Idea | Python | JavaScript |
| --- | --- | --- |
| Call a command | `harvest()` | `harvest();` |
| Pass an argument | `move(EAST)` | `move(EAST);` |
| A comment | `# ...` | `// ...` |

### Repeat a fixed number of times (steps 1 and 2)

| Idea | Python | JavaScript |
| --- | --- | --- |
| Repeat *n* times | `for i in range(n):` | `for (let i = 0; i < n; i++) { ... }` |
| A loop inside a loop | indent one level further | braces inside braces |

Here the two languages differ most. Python's `for` hides the counter. JavaScript's `for` shows a variable, a comparison and `++` in one line, all before the player has learned any of them. That is a reason to keep JavaScript until after minute 30, and a good first lesson when it arrives: the same loop, with the machinery visible.

### Choose (steps 4 and 6)

| Idea | Python | JavaScript |
| --- | --- | --- |
| If / else | `if ripe():` `else:` | `if (ripe()) { } else { }` |
| More than two options | `elif ...:` | `else if (...) { }` |
| Compare | `==`, `!=` | `===`, `!==` |

JavaScript only allows `===` and `!==`. Plain `==` gives an error that explains why `===` is safer. Real JavaScript accepts `==`, but its type conversions are a well-known trap for beginners, and leaving it out keeps principle 2.

### Repeat while something is true (step 5)

| Idea | Python | JavaScript |
| --- | --- | --- |
| Repeat forever | `while True:` | `while (true) { ... }` |
| Repeat while a condition holds | `while ripe():` | `while (ripe()) { ... }` |

### Functions (step 7)

| Idea | Python | JavaScript |
| --- | --- | --- |
| Define a function | `def farm_tile():` | `function farmTile() { ... }` |
| Call it | `farm_tile()` | `farmTile();` |

Here players meet the naming conventions of each language: `snake_case` in Python, `camelCase` in JavaScript. The game does not enforce them, but the examples follow them.

### After minute 30

These come next, in the order `game-design.md` settles on. They are listed here because the two languages treat them differently, and each needs a decision before it is unlocked:

| Idea | Python | JavaScript | Watch out |
| --- | --- | --- | --- |
| Create a variable | `count = 0` | `let count = 0;` | JavaScript needs `let` the first time only. `var` stays out. |
| Arithmetic | `+` `-` `*` | `+` `-` `*` | `%` differs: `-7 % 3` is `2` in Python and `-1` in JavaScript. `/` brings in decimal numbers. Both wait. |
| Combine conditions | `and`, `or`, `not` | `&&`, `\|\|`, `!` | |
| Parameters and return | `def f(x):` `return x` | `function f(x) { return x; }` | |
| Print for debugging | `print(x)` | `console.log(x);` | |

Lists and arrays, strings, dictionaries and objects, classes, `import`, exceptions, lambdas and arrow functions are further away and get their own section when they are designed.

### JavaScript details

- Semicolons are required. Real JavaScript can leave them out, but its rules for that are complex, and with semicolons required every accepted script is still valid JavaScript.
- Braces are required after `if`, `else`, `for` and `while`, even around a single line. This is valid JavaScript too, and it avoids the classic bug where a second indented line looks like it belongs to the `if`.
- Function declarations are hoisted, as in real JavaScript: a function can be called before the line that defines it. Calling costs the same ticks.

### An example in both languages

The script from step 7 of `game-design.md`, line for line. Both versions must end with the same field and the same number of ticks.

```python
def farm_tile():
    if ripe():
        harvest()
        if ground() == SOIL:
            plant(POTATO)
        else:
            plant(RADISH)

while True:
    for y in range(3):
        for x in range(3):
            farm_tile()
            move(EAST)
        move(NORTH)
```

```javascript
function farmTile() {
    if (ripe()) {
        harvest();
        if (ground() === SOIL) {
            plant(POTATO);
        } else {
            plant(RADISH);
        }
    }
}

while (true) {
    for (let y = 0; y < 3; y++) {
        for (let x = 0; x < 3; x++) {
            farmTile();
            move(EAST);
        }
        move(NORTH);
    }
}
```

### Example scripts

Principle 3 is checked by tests. Every folder in `tests/examples/` holds one example in three files: `script.py`, `script.js` and `expected.json`. The expected file gives the field size, the end status (`Finished` or `Failed`), the tick count, the drone position, the items, the tiles that matter, and for a failing script the error kind and line. One test runs every folder in both languages against that one file, so the two scripts must end the same way.

- An error is compared by kind and line only. The wording differs per language and has its own tests.
- A new example is a new folder. No test code changes.
- There is no setup language. Every example starts on a field of grass with ripe clover, and the script itself reaches any early-game state, for example by harvesting clover before it plants a radish.

## Errors in the player's script

An error in a script stops the drone and shows a message with the line number, in the style of the language the player is writing (`CLAUDE.md`, Error handling). Messages use the real error names, so players learn to read real error messages, followed by one plain sentence:

- Python: `NameError on line 3: name 'mvoe' is not defined`
- JavaScript: `ReferenceError on line 3: mvoe is not defined`

When the player uses something that exists in the real language but is not unlocked yet, the message says so: `'import' is not unlocked yet`. That keeps principle 2: the code is not wrong, it is just ahead of the game.

A script that is not valid code gives a syntax error. Python reports its own error names for indentation as well:

- Python: `SyntaxError on line 1: expected ':'`, for `if ripe()` with no colon.
- Python: `IndentationError on line 2: unexpected indent`, for an indented line that does not follow a block header.
- Python: `IndentationError on line 2: expected an indented block after 'if' statement on line 1`, for `if ripe():` with no indented line after it.

In Python, the following give `'...' is not unlocked yet`:

- Keywords such as `import`, `from`, `return`, `and`, `or`, `not`, `None`, `class` and `try`.
- Operators other than `( ) , : == !=`, such as `+`, `=`, `<`, `[`, `.` and `;`.
- Strings and decimal numbers.
- Parameters in `def`, `for` without `range`, `range` with more than one argument, and chained comparisons such as `a == b == c`.

Python errors stop at the first problem in the script, in source order, as in real Python.

In JavaScript, the following give `'...' is not unlocked yet`:

- Keywords other than `if`, `else`, `while`, `for`, `function`, `true` and `false`, such as `var`, `return`, `class` and `try`. `let` is allowed only in the header of a `for` loop.
- Operators other than `( ) { } , ; === !==`, such as `+`, `=`, `<`, `++`, `&&`, `!`, `[` and `.`.
- Strings, decimal numbers and block comments.
- Parameters in `function`, a `for` that is not exactly `for (let i = 0; i < n; i++)`, and chained comparisons such as `a === b === c`.

Some mistakes are plain JavaScript errors:

- `==` and `!=` give `SyntaxError on line 1: '==' is not allowed, use '===': '==' converts types before it compares`. Use `===` or `!==` instead.
- A missing `;` gives `SyntaxError on line 1: expected ';'`.
- A body without braces gives `SyntaxError on line 2: expected '{', braces are required around the body`.
- Other mistakes give `Unexpected token`, `Unexpected identifier`, `Unexpected number` or `Unexpected end of input`, with the line number, for example `SyntaxError on line 1: Unexpected token ')'`.

### Features that are not bought yet

The game teaches some features only after the player buys the unlock that opens them. Using one of them gives `'x' is not unlocked yet`, with the line number, in the language of the script. The check runs on the whole script before it starts, so the drone does not move at all. The first locked use in source order is reported (in JavaScript, function declarations are checked first, because they are hoisted). A locked feature is refused even in a branch that would never run.

The unlocks are listed in `data/unlocks.json`. They open these features:

| Unlock | Price | Opens |
| --- | --- | --- |
| for loop | 10 clover | `for` |
| Bigger field | 30 clover | nothing: the field grows to 5x5 |
| Radish and plant() | 40 clover | `plant`, `RADISH` |
| if / else and ripe() | 20 radish | `if`, `ripe` (`elif` and `else if` count as `if`) |
| while loop | 40 radish | `while` |
| Potato, till() and ground() | 60 radish | `till`, `ground`, `POTATO`, `SOIL`, `GRASS`, `==`, `!=` |
| Functions | 30 potato | `def` |

The name in the message is the one the player wrote. Two exceptions: in JavaScript the function feature is named `function`, and the comparison is named `===` or `!==`. A JavaScript `==` is still a syntax error, as described above. For example, a Python and a JavaScript script that call `plant(RADISH)` both give `'plant' is not unlocked yet`.

## How scripts run

Scripts run in the game's own small interpreter: one front end per language subset, one shared program format, and one evaluator that owns the tick costs and the drone API ([`decisions/0001-how-player-scripts-run.md`](decisions/0001-how-player-scripts-run.md)). This document is the list of features each front end supports and how they map onto each other.

## Settled questions

1. The first two languages are Python, then JavaScript.
2. Commands have one-word names; the sensors are `ripe()` and `ground()`.
3. Crops and directions are uppercase constants, not strings.
4. JavaScript in the game requires semicolons and braces.
5. JavaScript only allows `===` and `!==`, with an error that explains why `==` is not allowed.
6. Features the player has not bought yet are reported before the script runs, with the line number ([`decisions/0008-unlocks.md`](decisions/0008-unlocks.md)).
