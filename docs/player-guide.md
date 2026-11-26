# Player guide

Coding Game puts you in charge of a drone on a farm. You control it with code. You write a script in Python or JavaScript, press Run, and the drone carries it out on the field, one tick at a time.

The same drone commands work in both languages. Only the way you write them changes. You start with a few commands and buy more with the items you harvest.

## The screen

- **Farm**: the field fills the screen and is centred, seen from an angle in 3D. Green tiles are grass, brown tiles are soil. A crop is a small shape: clover is gold, radish red and potato orange. A crop grows bigger as it grows, and a ripe crop glows a little. The drone hovers over its tile with spinning rotors.
- **Items** (top left): one pill for each item you own, with a coloured circle and the count, for example clover 3 and radish 1. Hover over a pill to see the item's name. With no items it says `No items yet`.
- **Code** (top right): shows or hides the Script window. See below.
- **Unlocks** (top right): opens the unlock list in its own window. Each unlock has its price and a Buy button. A bought unlock says `Unlocked`. Buy is greyed out while a script runs or when you cannot pay. The list scrolls when it is long.
- **Help** (top right): opens the help in its own window: what you can use now, for the language you selected. Each entry has a short description and an example. The help lists only what you have unlocked, so new entries appear when you buy an unlock. Switch the language and the examples change to that language's style.
- **Menu** (top right): **New game**. It is greyed out while a script runs.

The Unlocks, Help and Menu buttons work the same way: press once to open, and press again to close the window.

## The Script window

The Script window floats over the farm. You can move it by its title bar, resize it by its edges, and close it with its close button. Closing only hides it: press **Code** to bring it back. It has these parts, from top to bottom:

- **Toolbar**: the language picker (Python or JavaScript; each language keeps its own script), **Run**, **Step**, **Stop**, the speed picker and the tick count.
  - **Run** starts the script, or carries on with a script that Step paused. After a run has ended, Run starts a new one.
  - **Step** runs the script until the next line starts. The yellow line shows the line that is about to run.
  - **Stop** ends the run and resets the farm. Your items stay.
  - **Speed**: Slow, Normal or Fast. It only changes how fast the ticks run, not the result. Normal is the default.
  - **Ticks**: how many ticks this run has used. It says `finished` or `stopped by an error` when the run ends.
- **Editor**: your script, with line numbers and colours: keywords purple, drone commands blue, constants amber, numbers green, strings orange and comments grey. You cannot edit it while a script runs.
- **Notice**: a red message when something went wrong at start-up, for example when the save file could not be read.
- **Messages**: the message for an error in red, with its line number. The line with the error turns red. When there is no error, a grey hint says where errors appear.

## Commands

| Command | What it does | Ticks |
| --- | --- | --- |
| `move(direction)` | Moves the drone one tile: `NORTH`, `EAST`, `SOUTH` or `WEST`. At an edge it comes out at the opposite edge. | 20 |
| `harvest()` | Takes the crop on this tile. A ripe crop gives its items. | 20 |
| `plant(crop)` | Plants `RADISH` or `POTATO` on this tile. It pays the seed items. | 20 |
| `ripe()` | `True` if the crop on this tile is ripe. | 1 |
| `till()` | Turns grass into soil, or soil into grass. It removes the crop on the tile. | 20 |
| `ground()` | `GRASS` or `SOIL`, what this tile is made of. | 1 |

Examples, one line each:

- Python: `move(EAST)`, JavaScript: `move(EAST);`
- Python: `harvest()`, JavaScript: `harvest();`
- Python: `plant(RADISH)`, JavaScript: `plant(RADISH);`
- Python: `if ripe():`, JavaScript: `if (ripe()) { ... }`
- Python: `if ground() == SOIL:`, JavaScript: `if (ground() === SOIL) { ... }`

JavaScript needs semicolons, and braces around the body of `if`, `else`, `for` and `while`.

## Crops

| Crop | Grows on | Grow time | Seed | Harvest gives | After a harvest |
| --- | --- | --- | --- | --- | --- |
| `CLOVER` | grass | 200 ticks | free, it cannot be planted | 1 clover | grows again |
| `RADISH` | grass | 300 ticks | 2 clover | 4 radish | the tile is empty |
| `POTATO` | soil | 500 ticks | 2 radish | 6 potato | the tile is empty |

A crop grows one tick at a time while a script runs. It is ripe when it has grown for its full grow time. At Normal speed a run goes on at about 20 ticks a second, so clover is ripe after about 10 seconds, radish after about 15 and potato after about 25.

Harvesting a radish or potato before it is ripe destroys it, and you get nothing. Clover that is not ripe stays as it is.

## Unlocks

Each unlock costs items and opens new commands or language features.

| Unlock | Price | Opens |
| --- | --- | --- |
| for loop | 10 clover | `for`, to repeat a number of times |
| Bigger field | 30 clover | the field grows to 5x5 |
| Radish and plant() | 40 clover | `plant()` and `RADISH` |
| if / else and ripe() | 20 radish | `if`, `else` and `ripe()`. Python's `elif` and JavaScript's `else if` count as `if`. |
| while loop | 40 radish | `while` |
| Potato, till() and ground() | 60 radish | `till()`, `ground()`, `POTATO`, `SOIL`, `GRASS`, and comparisons: `==` and `!=` in Python, `===` and `!==` in JavaScript |
| Functions | 30 potato | `def` in Python, `function` in JavaScript |

Buying an unlock uses up its items.

At the start, `move()`, `harvest()`, the four directions and `CLOVER` are available. Everything else in the table is locked until you buy it.

## Error messages

Errors name the line they happened on. Python errors look like Python errors, and JavaScript errors look like JavaScript errors:

- `SyntaxError on line 1: expected ':'` (Python, for `if ripe()` with no colon)
- `IndentationError on line 2: unexpected indent` (Python)
- `NameError on line 3: name 'mvoe' is not defined` (Python, a misspelt command)
- `ReferenceError on line 3: mvoe is not defined` (JavaScript)
- `SyntaxError on line 1: expected ';'` (JavaScript)
- `SyntaxError on line 1: '==' is not allowed, use '===': '==' converts types before it compares` (JavaScript)
- `Error on line 1: 'plant' is not unlocked yet` (a feature you have not bought yet, in either language)
- `RuntimeError on line 2: cannot plant RADISH: not enough seed items` (Python; JavaScript says `Error on line 2:` instead)
- `TypeError on line 1: move expects direction, got number` (a command with the wrong kind of argument)

An unlocked feature is checked before the run starts, so the drone does not move at all. A misspelt command is found only when its line runs, so the drone may have moved before the error.

## Tips

- **The field wraps around.** Walking east past the last column brings you back to the first column of the same row. Walking north past the top row brings you to the bottom row. A loop over the rows needs no turning logic.
- **Check before you act.** `ripe()` and `ground()` cost only 1 tick each. A harvest of an unripe radish destroys it, so check first.
- **A tile you till and turn back to grass is empty.** Tilling removes the crop, and clover cannot be planted, so that tile stays empty.
- **Harvesting a ripe radish or potato empties the tile.** Plant it again to keep farming it.
- **Each run starts on a fresh field.** The field is grass with ripe clover on every tile, and the drone starts on the top-left tile. Crops do not carry over from one run to the next. Your items do.
- **A loop can run forever.** A `while True:` loop, or `while (ripe()) {}` in JavaScript, never ends by itself. Press Stop. Step on a loop that does not start another line stops after 10,000 ticks, and the next Step carries on.
- **Recursion has a limit.** A function that calls itself more than 100 calls deep stops with a `RecursionError` in Python or a `RangeError` in JavaScript.

## Help and New game

The Help window shows the commands, constants and language words you can use now. Locked ones are left out until you buy the unlock that opens them. Each entry has an example in the language you selected.

New game is in the Menu. It erases everything: your items, your unlocks, both scripts and the language, which goes back to Python. It asks first, and there is no undo. Choose Keep playing in the question to cancel.

## Saving

The game saves your items, your bought unlocks, both of your scripts and the language you selected. It saves:

- after each buy and after New game,
- when a run starts,
- when a run ends or is stopped, and
- when you close the window.

The save is the file `save.json`, in Godot's user data folder for the project "Coding Game". In Godot, use Project, Open User Data Folder. Delete the file to start again.

If the game cannot read `save.json`, it keeps the file as `save.json.broken`, shows a red notice and starts a new game.
