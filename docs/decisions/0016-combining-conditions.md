# 0016: Combining conditions

- **Status:** accepted (built in 0.16.0)
- **Date:** 2026-11-27

## Context

After minute 30 the game teaches `and`, `or` and `not` (Python) and `&&`, `||` and `!` (JavaScript), per [`languages.md`](../languages.md). A condition is a value that is true or false: `ripe()`, a comparison, `True` or `true`. Python and JavaScript do not agree on what the operators give for other values. In Python `and` and `or` give one of their operands, and any value has a truth value. In JavaScript `&&` and `||` also give one of their operands, but the sets of false values differ: `0` is false in both, `""` and `None` are false in Python only, and `null`, `undefined` and `NaN` are false in JavaScript only. A script that uses `and` on a number would give a different result in each language, and the game must give the same result for the same script ([`languages.md`](../languages.md), principles 2 and 3).

The rule of the game is that the same script means the same thing in both languages. Each operand of a logical operator must therefore be a true or false value.

## Option A: booleans only, checked at run time (chosen)

- `and`, `or` and `not` (`&&`, `||` and `!`) take true or false values only. Any other value is a run-time `WrongArgument` error, with the line number, in the language of the script, for example `TypeError on line 2: and needs true or false on both sides, got a number`.
- Short-circuit: the right side of `and` and `or` is not evaluated when the left side decides the result. Its ticks (for example `ripe()`) are not spent then.
- Precedence is each language's own: `and` binds tighter than `or`, and both bind looser than comparisons. `not` binds looser than comparisons in Python (`not x == 3` is `not (x == 3)`) and tighter in JavaScript (`!x === 3` is `(!x) === 3`).
- One unlock, "and, or, not" (60 potato), gives all three operators.

**For:** with booleans only, the two languages give the same result for every script that runs. Each rule is in one place, the evaluator, and the message names the operator the player wrote.

**Against:** a script that would work in real Python with a number (`if count and ...`) is refused. The player writes a comparison instead, such as `count > 0`, which is the lesson.

## Option B: truthiness as in each language (rejected)

- `and`, `or` and `not` accept any value and use the truth rules of each language: `0` is false in both, but `""` and `None` are false only in Python, and `null` only in JavaScript.

**For:** more real Python and JavaScript scripts run unchanged.

**Against:**

- The same script gives different results in the two languages for the same values, which breaks the rule that a script means the same in either language.
- The truth rules of both languages would have to be taught before the lesson, and they are a known trap for beginners.
- The game has no strings and no `None` yet, so the extra freedom is small now.

## Decision

Option A. A value that is not true or false is refused when the script reaches it, with the operator named as the player wrote it.

## Consequences

- The operators are the evaluator's `Logical` and `Not` nodes, checked with the other names in `UnlockCheck` and `ScopeCheck`. `elif` (Python) needs no new node: it is an `if` in the `else` list, as `else if` is in JavaScript.
- The error messages are in [`languages.md`](../languages.md) and in [`player-guide.md`](../player-guide.md).
- Short-circuit means that `False and ripe()` costs no tick for `ripe()`, in both languages.
