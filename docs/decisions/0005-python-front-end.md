# 0005: The Python front end

- **Status:** accepted (built in 0.2.0)
- **Date:** 2026-11-25

## Context

[`0001`](0001-how-player-scripts-run.md) chose a small hand-written front end for each language subset. Each front end turns the player's code into the shared tree from [`0004`](0004-syntax-tree-and-evaluator.md). This record fixes how the Python front end is built. The requirements are:

1. **Real Python only.** The game accepts a subset of Python and never something different ([`languages.md`](../languages.md), principle 2).
2. **Python's words in errors.** A script error has a line number and reads like a real Python error (`SyntaxError`, `IndentationError`, `NameError`).
3. **Python indentation.** Blocks are marked by indentation, not by braces.
4. **The first error wins.** Errors come in source order, as in real Python.
5. **Small.** The subset is a few ideas: commands, `if`, `while`, `for ... in range(...)` and `def`.

## Option A: a hand-written tokenizer and recursive descent (chosen)

A tokenizer turns the text into tokens and records indentation as `Indent` and `Dedent` tokens. A recursive-descent parser reads the tokens and builds the tree. Each grammar rule is one method.

**For:**

- Indentation is easy to handle in the tokenizer, where the line structure is known.
- Each error can say exactly what the player did wrong, in Python's words, with the line number.
- The whole front end is about 600 lines in five files. Adding a construct means one rule and one test.

**Against:**

- We write and test the parser ourselves. A bug in it is our bug.

## Option B: a parser generator or a combinator library (rejected)

Use a grammar tool such as ANTLR, or a parser library, and write the Python grammar for it.

**Against:**

- Python's indentation needs a custom lexer in any case. The ready-made grammars handle it with hooks.
- The subset is small, so the library would bring far more than the game needs.
- It adds a dependency and a build step that generates code. `CLAUDE.md` says a new dependency needs the owner's agreement first.
- Its error messages are aimed at editors, as [`0001`](0001-how-player-scripts-run.md) option C notes. We need messages that a beginner can read.

## Decision

**Option A: a hand-written tokenizer and recursive descent.** This is the choice [`0001`](0001-how-player-scripts-run.md) already made for every front end.

### Code layout

- `PythonTokenizer`: an internal static class. It returns the tokens and an optional error (`TokenizeResult`).
- `PythonFrontEnd`: a public sealed partial class. Its one public entry point is `Parse(string source)`, which returns a `ParseResult`. The rules sit in three files: `PythonFrontEnd.cs` (statements and the error rules), `PythonFrontEnd.Compound.cs` (`if`, `while`, `for`, `def` and blocks) and `PythonFrontEnd.Expressions.cs`.
- `ParseResult`: shared by all front ends in `Interpreter/`. A front end has no separate parser class, so there is no layer that only passes calls on.

### Decisions

- **One private exception inside the parser.** The recursive methods throw a private `ParseFailure` exception that carries the `ScriptError`. `Parse` catches it once and returns `ParseResult.Failure`. It never leaves the class. The alternative was a `TryParse` result or an `out` parameter in every method, which is much noisier. This is a conscious exception to [`0004`](0004-syntax-tree-and-evaluator.md), where exceptions are only for bugs in the game.
- **The tokenizer does not throw.** On a problem it returns the tokens before the problem and the error. It ends the list with an end-of-file token on the line of the error. The parser raises the tokenizer's error only when it reaches that end-of-file token. So a parser error on line 2 wins over a tokenizer error on line 10.
- **Two error kinds.** `SyntaxError` covers invalid code. `IndentationError` covers a block indented wrongly, and it exists only for Python, because Python teaches its real error names. JavaScript has no indentation rule, so it has no arm for this kind.
- **Tabs in indentation are rejected.** Real Python accepts them. The game is stricter here, which [`languages.md`](../languages.md) principle 2 allows.
- **One rule for things not yet unlocked.** The rule sits in one method, `UnexpectedToken`. A keyword that is not one of `if elif else while for in def pass True False` gives `NotUnlocked` with the keyword as written. An operator other than `( , : == !=` gives `NotUnlocked` with its own text. A keyword that follows an expression goes through the same rule. Strings and decimal numbers are also `NotUnlocked`, and so are parameters in `def`, `for` without `range`, `range` with other arguments, and chained comparisons such as `a == b == c`.
- **Unknown characters are syntax errors.** Characters such as `$`, `?`, `!` or a non-ASCII letter give `SyntaxError: invalid character 'c'`.
- **`pass` is accepted and produces no node.** Without it an empty body such as `def f(): pass` could not be written.
- **Bodies start on a new line.** A one-line body such as `if x: harvest()` gives `SyntaxError: the body must start on a new line`.
- **Parentheses around an expression are not supported.** `(` at the start of an expression gives `SyntaxError: invalid syntax`. The subset has no use for them yet.
- **The loop variable is parsed and dropped.** `for i in range(n):` becomes a `Repeat` with the count `n` and no name for `i`. Reading `i` gives a `NameError`, as [`0004`](0004-syntax-tree-and-evaluator.md) describes.
- **`print` is a normal call.** `print(x)` parses as any other call and fails at run time with a `NameError` until `print` is unlocked.
- **Test helper renamed.** The test helper `EvaluatorTestSupport.Python(...)` is now `PythonProgram(...)`. Inside the test namespace `CodingGame.Core.Tests.Interpreter.Python`, the name `Python` refers to the namespace, which hides the helper.

## Consequences

- Every new Python construct needs a rule in the parser, a tree node from `0004` and a test. The JavaScript front end can copy the same shape: a tokenizer, a `Parse` method and the same `ParseResult`.
- Each rule that rejects a feature has a message that tells the player the feature is not unlocked yet, not that the code is wrong ([`languages.md`](../languages.md), principle 4).
- The parser has no knowledge of commands, costs or the field. It only builds the tree, as [`0004`](0004-syntax-tree-and-evaluator.md) requires.
- If a parser library is ever wanted, this record is the place to argue for it. Until then the front end stays hand-written.
