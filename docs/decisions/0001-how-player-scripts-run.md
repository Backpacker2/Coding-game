# 0001: How player scripts run

- **Status:** accepted (built in 0.1.0 to 0.3.0)
- **Date:** 2026-11-25

## Context

The player controls the drone with a script written in one of several languages. The rules in `CLAUDE.md` put four hard demands on whatever runs that script:

1. **Same ticks in every language.** The same script in different languages must give the same end state *and the same number of ticks*. Ticks are the score, so this is the core of the game, not a detail.
2. **Deterministic.** The same script with the same starting state always gives the same result.
3. **Errors are gameplay.** A mistake in the player's script gives a clear message with a line number, in terms of the language the player is writing, never a crash.
4. **One drone API.** The same set of commands in every language; adding a command adds it everywhere at once.

The game also needs to run a script step by step: pause between ticks so the drone can be drawn moving, highlight the current line, and stop a script that loops forever.

The engine and the language of the game itself have not been chosen yet. This decision should not force that choice.

## Option A: our own interpreter, one front end per language subset

Each language gets a small front end (tokenizer and parser) for the subset the game teaches. Every front end turns the player's code into the same tree of statements and expressions. One evaluator runs that tree and charges ticks.

```
Python subset  ─┐
JS subset      ─┼─>  shared syntax tree  ─>  one evaluator (tick costs, drone API)  ─>  simulation
C subset       ─┘
```

**For:**

- **Equal ticks by construction.** Tick costs are attached to the shared tree (for example: one tick per evaluated operation, more for a drone command), so `for i in range(10)` and `for (let i = 0; i < 10; i++)` cost the same because they become the same node. The costs can live in `data/`.
- **Deterministic by construction.** We decide what exists: no clock, no unseeded random, no hash-ordered sets, no threads.
- **Step, pause and stop for free.** The evaluator advances one tick at a time, under the control of the game loop. Infinite loops simply keep costing ticks; the game decides when to stop them.
- **Error messages we control.** Each front end knows the source positions and can write the message in that language's terms.
- **The drone API is defined once**, in the evaluator. Front ends only map names (`move(North)`, `move(Direction.NORTH)`, ...).
- **Runs anywhere.** Works with any engine and any host language; no large download.
- **Teaching fit.** Only the subset each lesson needs exists, so unlocks can switch features on one at a time.

**Against:**

- We write and maintain a parser per language. Small subsets keep this manageable (a hand-written recursive-descent parser for a teaching subset is a few hundred lines), but it is real work, and each new feature (functions, lists, dictionaries) has to be added to the shared tree and to every front end.
- The player learns a subset, not the full language. Scripts copied from the internet may use features we do not have; the error has to say so clearly.
- Where languages really differ (integer division, string handling, Python's indentation vs. braces), we must choose which differences to keep and make visible, and which to hide.

## Option B: embed existing runtimes

Run real interpreters: for example Pyodide (CPython compiled to WebAssembly) for Python and the browser's own JavaScript engine or a sandboxed one (QuickJS compiled to WebAssembly, or JS-Interpreter, which can step through ES5) for JavaScript.

**For:**

- Full, real languages with their real behaviour. What the player learns transfers directly.
- No parsers to write. Fits the rule "do not build what a well-maintained library already does".

**Against:**

- **Equal ticks is not reachable.** Each runtime executes differently: Python and JavaScript produce different numbers of bytecodes, line events and function calls for the same loop. Tracing hooks (`sys.settrace` in Python, interrupt handlers in QuickJS) count different things in each language. The only tick rule that would match across languages is "only drone commands cost ticks", which removes the reward for efficient code, the heart of the game.
- **Pausing is hard.** A real runtime runs a script to the end. Pausing at each tick needs a worker thread with blocking calls, or source rewriting into generators or async code, different for each runtime.
- **Determinism needs constant care.** Every runtime brings a clock, random numbers, hash ordering and its own standard library. Each must be locked down or removed, separately per language, and kept that way through upgrades.
- **Error messages are the runtime's.** A Python traceback or a JavaScript stack trace includes frames from our glue code and is written for professionals.
- **The drone API is written once per runtime**, which breaks "adding a command means adding it to every language at once" in practice.
- **Size and platform.** Pyodide is a download of several megabytes and ties the game to a browser or WebAssembly host. Each language adds its own runtime.

## Option C (variant of A): existing parsers, our own evaluator

Use ready-made parsers (for example tree-sitter grammars, or a JavaScript parser such as Acorn) to build syntax trees, then translate those into our shared tree and run it with our own evaluator.

This keeps every advantage of option A for ticks and determinism and saves writing the grammar. But full-language parsers accept far more than the subset, so we still need a pass that rejects unsupported features with a clear message; their syntax error messages are aimed at editors, not beginners; and it adds one dependency per language. It is worth reconsidering per language if a hand-written parser turns out to be the slow or buggy part.

## Decision

**Option A: our own small interpreter.** One shared syntax tree and one evaluator that owns tick costs and the drone API, with a small hand-written front end per language subset.

The deciding factor is the tick rule. Equal tick counts across languages are a requirement of the game, and only an evaluator we own can guarantee them. Option B is rejected because it cannot meet that rule without dropping the cost of computation from the game. Option C is kept as a fallback for individual front ends.

Following "build nothing for later", the first step is the smallest version: one language subset, the shared tree, the evaluator, and a handful of drone commands. The second language comes in the prototype phase, and that is the real test of whether the shared tree is right.

## Consequences

- The syntax tree and the evaluator are the most important interface in the game. They get their own design sketch (two designs, one rejected) before code is written.
- Tick costs per operation are game rules, so they live in `data/`.
- Each language needs a written list of the subset it supports and how each feature maps onto the shared tree (`docs/languages.md`).
- Test suite: every example script exists in each language, and the tests check that all versions give the same end state and the same tick count.
- The interpreter loop is a hot path: no unnecessary layers there.
- Players learn subsets. The game should say so openly, and a clear "this feature is not available yet" error is part of the design, not a fallback.
