# 0004: The syntax tree and the evaluator

- **Status:** accepted (built in 0.1.0)
- **Date:** 2026-11-25

## Context

[`0001`](0001-how-player-scripts-run.md) chose one shared syntax tree and one evaluator that owns the tick costs and the drone API. [`0003`](0003-game-engine.md) chose C#, with the interpreter in `CodingGame.Core` (folder `Interpreter/`), free of Godot. This record fixes how the tree and the evaluator are built. The requirements are:

1. **One tree for every language.** The Python and JavaScript front ends build the same tree, and every node carries its source line number.
2. **One tick per call.** The game loop calls the evaluator once per tick. That lets the Godot view pause, step, highlight the current line and stop an endless loop by no longer calling it.
3. **Ticks follow `languages.md`.** A loop iteration costs 1, a call of a player function costs 1, a drone command costs its cost from `data/`, and everything else costs 0.
4. **Drone commands are bound once.** `move`, `till`, `plant`, `harvest` and the sensors, plus the constants (`NORTH`, `SOIL`, `RADISH`), live in one table, not in each front end.
5. **Script errors are values.** An error has a kind and a line number. The wording (`NameError` or `ReferenceError`) depends on the language of the script. Nothing crashes.
6. **Deterministic, with a lean hot path.** The evaluator reads no clock and uses no unseeded random numbers or hash-ordered output. The per-tick path has no needless layers.

## Option A: tree walker with an explicit frame stack

The evaluator keeps its own stack of frames on the heap. A frame is a block of statements, a position in that block, and, if the block is a loop body, what to do at its end. `Step()` runs statements until one of them charges ticks, and then returns. All progress sits in plain fields, so the evaluator can stop between any two ticks.

A command that costs 20 ticks is charged in one go: its effect happens on the first tick and the evaluator then waits 19 more `Step()` calls. Expressions are evaluated recursively and never suspend.

**For:**

- Pausing, stepping and stopping need no extra machinery: the state is a few fields, so the current line is a plain read.
- No second program format. The tree the front end builds is the tree that runs, and a failing test points at the node.
- Small: the evaluator is one `switch` over statement kinds, a few hundred lines.

**Against:**

- A call that needs a result cannot suspend in the middle of an expression. That is fine in the first version, because player functions return nothing (return values come after minute 30, see `game-design.md`) and commands and sensors answer at once. Once functions return values, calls inside expressions need a pass that moves them into their own statement first.
- The loop state (which block, which iteration) is handled by hand instead of by the language.

## Option B: compile the tree to instructions for a tiny stack VM

A compiler turns the tree into a flat array of instructions (`Push`, `Call`, `Jump`, `JumpIfFalse`, ...). The VM has a program counter, a value stack and a call stack. Each instruction records its line number.

**For:**

- The fastest to run, and the simplest loop to step: one instruction at a time, with the state in the program counter.
- Calls in the middle of expressions and return values need no special case.

**Against:**

- A second representation of the program. Every new language feature touches the tree, the compiler and the VM, which breaks "one change, one place".
- A compiler to write and test (jump targets, loop exits, line tables) before the first script runs. For about eight node types that is more code than the evaluator it replaces.
- Speed is not the problem. Scripts are short and run at one tick per frame. Nothing has been measured, and `CLAUDE.md` says to optimise only what is measured to be slow.

## Variant: C# iterators (`yield return` once per tick)

Write a plain recursive tree walker as an iterator method and `yield return` where a tick is charged. This is the least code of all, because C# keeps the position in the loops for us. It is not chosen:

- Each level of nested iterators passes through the one above it when resumed, so the cost per tick grows with the nesting depth. This is the hot path.
- The state hides in compiler-generated classes, so the current line must be tracked on the side and the state is harder to test.
- The cost model (what yields, and when) is spread over the whole walker instead of sitting in one place.

If option A turns out to be too long to maintain, this is the first thing to reconsider.

## Decision

**Option A: a tree walker with an explicit frame stack.**

The stack is plain data, so pause, step, stop and the current line come for free, and there is exactly one program representation to keep in line with the language subsets. Option B is rejected for now because it adds a compiler without solving a problem we have. It becomes interesting when player functions return values, or if measuring shows the walker is too slow.

Tick rule: `Step()` runs zero-cost work (conditions, `if`, assignments) until something charges. A loop iteration and a player function call charge 1; a drone command charges its cost from `data/`. Every loop path charges, so a `Step()` call always ends, even for `while True:` with an empty body.

Error rule: `Step()` returns a `StepResult`. A script error is a `ScriptError` value in it, never an exception. Exceptions are only for bugs in the game.

### C# types in `CodingGame.Core.Interpreter`

Tree (data only, every node has `int Line`):

- `Statement`: abstract record with sealed records `ExpressionStatement`, `If`, `While`, `Repeat` and `FunctionDefinition`.
- `Expression`: abstract record with sealed records `Literal`, `Name`, `Call` and `Compare`.
- `Repeat(Expression Count, Block Body)`: the shared node for `for i in range(n)` and `for (let i = 0; i < n; i++)`. A front end rejects any other `for` shape.
- `ScriptProgram(IReadOnlyList<Statement> Body, ScriptLanguage Language)`: what a front end returns.
- `ScriptLanguage`: enum, `Python` or `JavaScript`.

Running:

- `Value`: readonly struct for what a script can hold: a number, a boolean, or a constant from the table (direction, ground, crop).
- `DroneCommand`: a name, the kinds of its arguments, its cost in ticks from `data/`, and a delegate that acts on the field.
- `DroneApi`: the one table of commands and constants, built from `data/` and the field. Names resolve here at run time, so `mvoe` gives the language's "not defined" error.
- `Evaluator`: `Evaluator(ScriptProgram, DroneApi)`, `StepResult Step()`, `int CurrentLine`, `int TicksUsed`. It owns the frame stack.
- `Frame`: one block, a position in it, the loop it belongs to, and the local names of a player function call. Internal.
- `StepResult`: `Running`, `Finished` or `Failed(ScriptError)`.
- `ScriptError(ScriptErrorKind Kind, int Line, string Detail)`, with `ScriptErrorKind` as `UndefinedName`, `WrongArgument`, `NotUnlocked`, `CommandFailed` (a game rule was broken) and `TooDeep` (recursion). One function turns kind and language into the text (`NameError on line 3: ...` or `ReferenceError on line 3: ...`), so all wording lives in one file.

Build order, each step leaving the tests green:

1. Tree records and `Value`; tests build trees by hand.
2. `Evaluator` with straight-line commands, `DroneApi` and tick counting.
3. `Repeat`, `While`, `If`, `Compare`.
4. `FunctionDefinition` and calls to player functions, with the frame stack.
5. `ScriptError` and its wording per language.
6. The Python front end, then the JavaScript front end. The second one is the real test of the tree: the same script in both must give the same end state and the same tick count.

## Details settled while building

- A block is a plain `IReadOnlyList<Statement>`; a wrapper type added nothing.
- `Compare` carries a `CompareOperator` (`Equal`, `NotEqual`). Values of different kinds are simply not equal.
- `Value` has a `None` kind (its default), returned by commands that give nothing back.
- A command reports a broken game rule as a `CommandOutcome` with a failure detail, never as an exception. `plant` is the only command that can fail this way.
- `StepResult` is a `readonly record struct` with a `StepStatus`, so a step allocates nothing.
- Time: the effect of a command happens at once, then the field advances by exactly one tick per `Step()` that returns `Running`, never on `Finished` or `Failed`. A sensor in a condition is charged together with the branch it chooses.
- A player function call is a statement on its own. Used inside an expression it is a `WrongArgument` error, because functions return nothing yet and expressions never pause.
- Functions are registered when their definition runs, as in Python. JavaScript hoists functions, so its front end moves definitions to the top of their block (see [`0006`](0006-javascript-front-end.md)).
- `Evaluator.MaxCallDepth` is 100: a runaway recursion ends as a `TooDeep` script error long before it uses up memory.
- `NotUnlocked` has its wording and a test, but nothing produces it until unlocks exist (milestone 10).
- The analyzer rule CA1716 is off, so tree nodes can be named after what scripts write (`If`, `While`, `Call`).

## Consequences

- Front ends only build trees. They know nothing about commands, costs or the field. Adding a command means one entry in `DroneApi`, which makes it available in every language at once.
- Tick costs for loops, calls and commands come from `data/`. The evaluator contains no numbers.
- The evaluator is tested without Godot, by calling `Step()` in a loop and checking the field, `TicksUsed` and the errors. A script that never finishes is tested by stopping after a fixed number of steps.
- `Repeat` has no counter variable yet. A script that reads `i` gets a "not defined" error, which differs from real Python and JavaScript. The first lesson that needs the counter (variables, step 4 in `languages.md`) adds a name to `Repeat` and binds it in the frame.
- Return values will force a change (see option A, Against). That change is a tree pass or a move to option B, and it gets its own record.
- The end of a script costs no tick, so `TicksUsed` is the number of `Step()` calls that returned `Running`.
