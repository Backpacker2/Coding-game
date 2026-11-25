# 0002: Drone commands are global functions with one-word names

**Status:** accepted, 25 November 2026.

## Context

The drone API is the same in every language (`CLAUDE.md`, Game-specific). Players meet it in their very first line of code, before they know about variables, loops or objects. See [`../languages.md`](../languages.md).

Two questions: how a command is called, and how it is named so the same name works in Python, JavaScript and later C.

## Shape

### Design A: global functions (chosen)

```python
move(NORTH)
if ripe():
    harvest()
```

- A first script needs only one idea: calling a function.
- C, a likely later language, has no methods, so it would use this shape anyway.

### Design B: methods on a drone object (rejected)

```python
drone.move(NORTH)
if drone.ripe():
    drone.harvest()
```

- It reads naturally and would make several drones easy later.
- But the dot is an extra idea (objects and methods) in the very first line, before the player can understand it.
- C would need a different shape (`move(drone, NORTH)`), which breaks "the same commands in every language".
- Several drones are not planned. Building for them now goes against "build nothing for later".

## Names

### Design A: one word each (chosen)

`move`, `harvest`, `plant`, `till`, `ripe`, `ground`, and uppercase constants such as `NORTH` and `RADISH`.

- A one-word name is correct in every naming convention, so the API looks exactly the same in every language.

### Design B: descriptive names in each language's convention (rejected)

`can_harvest()` and `get_ground()` in Python, `canHarvest()` and `getGround()` in JavaScript (the placeholders in the first `game-design.md` draft).

- The names read well, but the same command would be spelled differently per language, and every new command would need a name in each convention.
- Using `can_harvest` unchanged in JavaScript avoids that, but teaches a style that real JavaScript code does not use.

## Consequence

If several drones are ever added, the shape is revisited then, with that real case in front of us. A new command needs a one-word name; if none fits, look at the command first.
