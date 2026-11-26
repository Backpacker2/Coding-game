# Game design

Status: **draft**. This version covers only the first 30 minutes of play. Everything marked **(guess)** is a first proposal to be tested in the prototype; open questions are collected at the end.

## Goal of the first 30 minutes

By minute 30 a new player has written a script that farms a mixed 3×3 field forever, using the four core ideas of programming in this order:

1. **Sequence**: commands run one after another, top to bottom.
2. **Loops**: repeat commands instead of writing them out (`for`, then `while`).
3. **Conditions**: let the script decide (`if`, `else`, comparisons).
4. **Functions**: give a piece of the script a name and reuse it.

Each concept is unlocked at the moment the player feels the pain it solves. The game never asks for a concept before the player wants it.

The first goals the player is shown are listed in `data/goals.json`, in the order they are shown.

## Core loop

1. The player writes a script and presses Run.
2. The drone carries out the script on the field, one command at a time. Every command costs ticks.
3. Harvesting gives items (one item type per crop).
4. Items pay for unlocks: new commands, new language features, new crops and a bigger field.
5. The new unlock makes the old script feel slow or clumsy, so the player rewrites it.

In the prototype, the unlocks and their prices are in `data/unlocks.json`. Items stay between runs and after Stop, until the game closes.

## Words used in this document

One word per concept, as in `CLAUDE.md`. These are also the words the game itself uses.

| Word | Meaning |
| --- | --- |
| tile | One square of the field. |
| field | All tiles the drone can reach. |
| ground | What a tile is made of: `grass` or `soil`. |
| crop | What grows on a tile (clover, radish, potato). |
| seed | What `plant()` puts in the ground. Costs items. |
| item | What a harvest gives. Items are the currency. |
| tick | The unit of simulated time. Every command costs ticks. |
| unlock | Something the player buys with items. |

## The first three crops

All numbers go into `data/` once the game has code; they are listed here only to make the design concrete. **(guess, all numbers)**

| Crop | Ground | Planted by | Grow time | Yield | Notes |
| --- | --- | --- | --- | --- | --- |
| Clover | grass | grows by itself | 200 ticks | 1 clover | Regrows after harvest. Free, slow and always there. Planting another crop replaces it. |
| Radish | grass | `plant(RADISH)`, seed costs 2 clover | 300 ticks | 4 radish | Harvesting before it is ripe destroys it and gives nothing. |
| Potato | soil | `plant(POTATO)`, seed costs 2 radish | 500 ticks | 6 potato | Only grows on soil; `till()` turns grass into soil and back. |

Each crop adds exactly one new rule, and each rule is what makes the next concept worth learning:

- Clover: nothing can go wrong, so the player only has to learn to give commands.
- Radish: harvesting too early wastes it, so the player needs to **check before acting** (conditions).
- Potato: the field becomes mixed, so the script must **treat tiles differently** (conditions with comparisons), and the same walking pattern is needed for several jobs (functions).

## Command costs

**(guess, all numbers)**

| Command | Ticks | Notes |
| --- | --- | --- |
| `move(direction)` | 20 | `NORTH`, `EAST`, `SOUTH`, `WEST`. |
| `harvest()` | 20 | On an unripe crop: destroys it (radish, potato) or does nothing (clover, still costs ticks). |
| `plant(seed)` | 20 | Fails with a gameplay error if the tile already has a planted crop or the wrong ground. |
| `till()` | 20 | Switches the tile between grass and soil. Removes the crop on it. |
| `ripe()` | 1 | Sensor: is the crop on this tile ripe? |
| `ground()` | 1 | Sensor: `GRASS` or `SOIL`. |

Sensors are cheap on purpose: checking is never the expensive part, so the game never punishes a player for being careful.

## Unlock order and timeline

Times are where we want an average beginner to be, not hard limits. **(guess, all times and costs)**

| # | Around | Unlock | Costs | Concept |
| --- | --- | --- | --- | --- |
| 0 | 0:00 | Start: `move`, `harvest`, a field of 3 tiles in a row | – | Sequence |
| 1 | 0:03 | `for` loop (repeat a fixed number of times) | 10 clover | Loops |
| 2 | 0:07 | Field grows to 3×3 (in the prototype the start field is 3x3 and this unlock grows it to 5x5, see `data/unlocks.json`) | 30 clover | Nested loops |
| 3 | 0:11 | Radish and `plant()` | 40 clover | (new crop) |
| 4 | 0:14 | `if` / `else` and `ripe()` | 20 radish | Conditions |
| 5 | 0:18 | `while` loop | 40 radish | Loops that run on a condition |
| 6 | 0:22 | Potato, `till()` and `ground()` | 60 radish | Comparisons (`==`) |
| 7 | 0:27 | Functions (`def` and calling your own function) | 30 potato | Functions |
| 8 | 0:31 | Variables and counting (guess): the loop counter, your own variables, `+ - *` and `< > <= >=` | 40 potato | Variables |

Each unlock's price is paid in the newest crop, so the player always has a reason to farm the newest thing.

### 0. Start: sequence (0:00 to 0:03)

The field is 3 tiles in a row, all clover. The script editor already holds a working three-line script, so the very first Run does something:

```python
harvest()
move(EAST)
harvest()
```

The player's first task is to extend it to the third tile. The script runs once and stops. Running it again by hand works, but the drone ends at the far end of the row, so the player has to add `move` commands back, or add them in a smarter order. This teaches that **order matters** and that **the drone does exactly what is written, nothing more**.

Pain that leads to the next unlock: pressing Run again and again, and long scripts of copied lines.

### 1. `for` loop (around 0:03)

```python
for i in range(10):
    harvest()
    move(EAST)
```

The player can now harvest the row many times without copying lines. The lesson is **repeat instead of copy**. **(guess)** Moving off the edge of the field wraps the drone around to the other side, so a row loop needs no turning logic yet; see open question 2.

### 2. Field grows to 3×3 (around 0:07)

Nine tiles do not fit in one row loop, so the player puts one loop inside another:

```python
for i in range(10):
    for x in range(3):
        harvest()
        move(EAST)
    move(NORTH)
```

Nested loops are hard for beginners. That is why the bigger field is its own unlock and not part of the `for` unlock: the player first gets comfortable with one loop.

### 3. Radish and `plant()` (around 0:11)

Radish yields more than clover but must be planted and grows slower. The script from step 2 now hurts: it harvests radishes before they are ripe and destroys them. The player notices that their item count goes down, not up.

A workaround without new concepts exists: walk around the field a few times without harvesting, then harvest. It is slow and fragile, which is the point.

### 4. `if` / `else` and `ripe()` (around 0:14)

```python
for i in range(100):
    for x in range(3):
        if ripe():
            harvest()
            plant(RADISH)
        move(EAST)
    move(NORTH)
```

The lesson: **look before you act**. The script now behaves differently on different tiles, depending on what it finds.

Pain that leads to the next unlock: `range(100)` is a guess. The script stops while the player is away, or runs far longer than needed.

### 5. `while` loop (around 0:18)

```python
while True:
    ...
```

The lesson: **repeat until something changes**, or forever. Introduced after `if` so the player already knows what a condition is; `while` is then "an `if` that keeps checking". **(guess)** A script that runs forever is stopped with the Stop button, and a full stop never costs the player anything.

### 6. Potato, `till()` and `ground()` (around 0:22)

Potatoes need soil, clover and radish need grass. The best field is now mixed, for example one row of soil and two rows of grass. The script must ask what kind of tile it is standing on:

```python
if ground() == SOIL:
    plant(POTATO)
else:
    plant(RADISH)
```

The lesson: **compare a value** (`==`) and choose between more than two options with `elif`. In the prototype `elif` is part of the `if` unlock, and `==` and `!=` come with this unlock.

### 7. Functions (around 0:27)

The script is now 20 to 30 lines, with the same "walk the whole field" pattern written out more than once (once to till, once to farm). Functions let the player name a piece of it:

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

The lesson: **give a piece of code a name**, so the main script reads like a description of what the drone does. Parameters and return values come after minute 30.

## After minute 30 (sketch only)

Not designed yet; listed so the first 30 minutes point somewhere. **(guess, all of it)**

- Variables you create yourself (counting harvests, remembering a position).
- Function parameters and return values.
- A fourth crop whose rule rewards lists or position tracking.
- The second programming language (see open question 1).

## Open questions

1. **When does the second language appear?** It is the core idea of the game, so it should come early, but not before the player is comfortable with one. Proposal: right after functions (around minute 30), as an unlock that shows the player's own current script side by side in the second language, then lets them switch.
2. **What happens at the edge of the field?** Proposal: the drone wraps around to the other side, so early loops need no turning logic. Alternative: the edge blocks the drone and `move` fails with a gameplay error, which teaches more care but makes steps 1 and 2 much harder.
3. **Which language comes first?** The examples use Python because it is the most common first language and has the least syntax. Language choice belongs in `languages.md`.
4. ~~**Are the command names final?**~~ Settled: the names follow decision record 0002 and [`languages.md`](languages.md).
5. **Does the loop variable `i` count as teaching variables?** In this draft the player uses it without being taught it. Variables could be its own unlock, before or after functions. Note (2026-11-26): the counter is now taught by the unlock Variables and counting (40 potato), after Functions; see [`decisions/0014-variables.md`](decisions/0014-variables.md).
6. **Is there a goal or story beyond unlocks?** For example a farm order board ("deliver 50 radish"), which would give the player targets other than "the next unlock".
7. **Can the player lose items?** Radish destroyed by an early harvest is the only loss in this draft. Is that enough tension, or too punishing for minute 11?
