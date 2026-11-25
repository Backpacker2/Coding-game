# 0006: The JavaScript front end

- **Status:** accepted (built in 0.3.0)
- **Date:** 2026-11-25

## Context

[`0001`](0001-how-player-scripts-run.md) chose a small hand-written front end for each language subset. [`0005`](0005-python-front-end.md) built the Python one. This record fixes how the JavaScript front end is built. The requirements are:

1. **Real JavaScript only.** The game accepts a subset of JavaScript and never something different ([`languages.md`](../languages.md), principle 2).
2. **JavaScript's words in errors.** A script error has a line number and reads like a real JavaScript error (`SyntaxError`, `ReferenceError`), with the details that V8 gives.
3. **Braces, not indentation.** Blocks are marked by braces and statements end with `;`.
4. **The first error wins.** Errors come in source order, as in real JavaScript.
5. **Small.** The subset is the same few ideas as Python: commands, `if`, `while`, a `for` that counts, and functions.

## Option A: a hand-written tokenizer and recursive descent (chosen)

The front end copies the shape of the Python one. A tokenizer turns the text into tokens with line numbers. A recursive-descent parser reads the tokens and builds the shared tree from [`0004`](0004-syntax-tree-and-evaluator.md). Each grammar rule is one method.

**For:**

- The same design as the Python front end, so a reader who knows one knows the other.
- Each error can say exactly what the player did wrong, with the line number.
- Braces and semicolons are simple to read in the tokenizer, with no indentation to track.

**Against:**

- We write and test the parser ourselves. A bug in it is our bug.
- Some code is copied from the Python front end. The copy is small, and it is cheaper than a shared layer that fits neither language well.

## Option B: one tokenizer and parser for both languages, with a configuration object (rejected)

Share one tokenizer and one parser, and pass a configuration object that switches the differences on or off.

**Against:**

- The two languages differ in blocks, terminators and error wording. Every difference would become a flag, and the flags would leak into each other.
- Two languages are not enough to know the right shared shape. [`CLAUDE.md`](../../CLAUDE.md) says to abstract only at the third real case.

## Decision

**Option A: a hand-written tokenizer and recursive descent.**

### Code layout

- `Token.cs`: the internal token kind, token and tokenizer result types.
- `JavaScriptTokenizer`: an internal static class. It returns the tokens and an optional error.
- `JavaScriptFrontEnd`: a public sealed partial class. Its one public entry point is `Parse(string source)`, which returns a `ParseResult`. The rules sit in three files: `JavaScriptFrontEnd.cs` (statements and the error rules), `JavaScriptFrontEnd.Expressions.cs` and `JavaScriptFrontEnd.Compound.cs` (`if`, `while`, `for`, `function` and blocks).
- Both front ends live in `Interpreter/`, in their own folder. Both return the shared `ParseResult`.

### Decisions

- **Token types are duplicated, not shared.** The JavaScript token kinds have no newline, indent or dedent kind, so the Python `Token` does not fit. Sharing would need one more abstraction for a second use. It waits for the third front end.
- **Hoisting.** JavaScript hoists function declarations, so a call may come before the line that defines it. The game does the same. Each statement list (the top level, every block and every function body) puts all its function definitions first, in source order, then all other statements, in source order. The reason is [`languages.md`](../languages.md), principle 2: without hoisting, `f(); function f() {}` would give a `ReferenceError` in the game, where real JavaScript runs it. Definitions cost 0 ticks, so the tick count is the same. Line numbers do not change.
- **Equality is `===` and `!==` only.** `==` and `!=` give a `SyntaxError` that says why, with these exact details:
  - `'==' is not allowed, use '===': '==' converts types before it compares`
  - `'!=' is not allowed, use '!==': '!=' converts types before it compares`

  They are a `SyntaxError`, not `NotUnlocked`, because real JavaScript accepts them. The game refuses them on purpose.
- **A missing `;` is a syntax error on the line of the token before it.** When the next token is on a later line, or the input ends, the error is `expected ';'`. Other tokens give V8-style details, for example `Unexpected token ')'`, `Unexpected identifier 'x'`, `Unexpected number` and `Unexpected end of input`.
- **Braces are required.** A body without `{` gives `expected '{', braces are required around the body`. This keeps the rule from [`languages.md`](../languages.md) for JavaScript.
- **No new error kind.** The messages use the kinds that already exist in the shared `ScriptErrorKind`. `IndentationError` is Python only, because JavaScript has no indentation rule. A one-statement body needs braces.
- **Subset.**
  - Keywords accepted: `if else while for function true false`. The word `let` is accepted only in the header of a `for`.
  - Every other JavaScript reserved word gives `NotUnlocked` with the word.
  - Operators other than `( ) { } , ; === !==` give `NotUnlocked` with their text.
  - Strings (`'`, `"` and backtick), decimal numbers and block comments give `NotUnlocked`.
  - `true` and `false` are keywords. `True` is an ordinary name and fails at run time, as in real JavaScript.
  - `$` is a valid identifier character, as in real JavaScript.
- **The `for` header has one shape.** It must be exactly `for (let N = 0; N < EXPR; N++)`, with the same name `N` three times. Any other shape gives `NotUnlocked` with `other for loops`. The loop variable is parsed and dropped, as in Python, so reading it gives a `ReferenceError`.
- **Parameters and parentheses.** Parameters in `function` give `NotUnlocked` with `parameters`. Parentheses around an expression are not supported. A chained comparison such as `a === b === c` gives `NotUnlocked` with `chained comparisons`.
- **Nesting has a limit.** The parser recurses once per nested call, block and `else if`. Past 100 levels it gives `SyntaxError: too many nested levels`. An `else if` chain counts as nesting, so a long chain cannot overflow the stack. [`0005`](0005-python-front-end.md) has the same limit, and counts `elif` the same way.

## Consequences

- The C front end can copy this shape. After the third front end, consider sharing `Token`.
- Each rule that rejects a feature gives a message that says the feature is not unlocked yet, not that the code is wrong ([`languages.md`](../languages.md), principle 4).
- Hoisting is one method on the statement list. The tree does not know whether a definition was hoisted.
- The tree shows `==` and `!=` in dumps for `===` and `!==`, because the tree is shared by both languages.
