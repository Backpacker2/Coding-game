# CLAUDE.md — Coding Game

A programming game in which you control a drone on a farm with code, and in which you learn several programming languages. See `README.md`.

**Status:** phase 1 (planning). There is no code yet, and the language, engine and interpreter approach have not been chosen. Do not write code until the owner asks for it.

The reasoning and sources behind every rule are in `docs/coding-standards.md`.

## Principle

Complexity is the main enemy. Choose the simplest solution that solves today's problem. Every line of code has to earn its place.

## Language

- Everything in this repository is in English: code, comments, documentation, commit messages, issues and file names.

## Design

- Make modules deep: a small, simple interface with a lot of work behind it. No functions that only pass their arguments on to another function.
- Keep each design decision in one place. If one change means editing three modules, knowledge is leaking.
- Only abstract at the third real case. Duplication is cheaper than the wrong abstraction.
- If an existing abstraction no longer fits a new case, do not add a parameter or a flag. Move the code back into the callers first and look again.
- Build nothing for later: no extra parameters, settings, interfaces with a single implementation, factories or plugin systems "just in case".
- Keep behaviour close to where it happens. Reading one piece of code should show what it does without opening five other files.
- Design special cases out of existence instead of handling them everywhere.
- Use types wherever the language allows it (static types or type annotations). A caller should see from the signature what a function expects and returns.
- Only use classes where they give the caller a simpler interface. No inheritance hierarchies: prefer composition.
- Do not build what a well-known, well-maintained library already does. Propose that library instead of writing your own code (see Git: dependencies).
- For an important interface (the drone API, the interpreter, the save format), sketch two designs, pick one and record the rejected alternative in `docs/decisions/`.

## Structure

- Fixed folders in the root: `src/` (code), `tests/`, `data/` (game rules and numbers), `docs/`. No loose files in the root except README, LICENSE, CLAUDE.md and tool configuration.
- Inside `src/`, organise code by part of the game (for example interpreter, simulation, rendering), not by technical kind of file.
- Order within a file: imports, constants, types, functions.
- Split a file by purpose as soon as it becomes hard to oversee (guideline: around 400 lines).
- Before writing something new, check whether it already exists. Improve the existing version instead of creating a second one.
- Follow the naming and patterns of the surrounding code.

## Naming

- A name says what something is or does. Avoid vague names such as `data`, `info`, `manager`, `helper`, `utils`, `handle`, `tmp`.
- One word per concept across the whole project. So not `crop` and `plant` for the same thing.
- Put the unit in the name when it is not obvious: `growTimeTicks`, not `growTime`.
- If no good name comes to mind, the design is probably wrong. Look at that first.

## Comments

- Only write down why something is the way it is: a constraint, a workaround or the reason behind a number.
- Never repeat what the code already says. No narrating comments ("loop over all tiles"), no notes to an AI, no commented-out code.
- A TODO only with a reference to an issue.
- Every public function, class and module gets a doc comment in the standard format of the language: what it does, its parameters, what it returns and which errors it can raise. The documentation is generated from these.
- A doc comment describes what something does and how to use it, not how it works inside. Internal functions only get a comment when the reason is not obvious.

## Error handling

- Never swallow errors. No `try`/`catch` around code that cannot fail, and no silent fallback that returns `null` or a default value.
- Check input at the edges: player scripts, save files and config files. Turn that input into precise types there, and rely on those types inside without checking again.
- An error in the player's script is gameplay, not a crash: a clear message with a line number, in the player's programming language.
- Fix the cause, not the symptom. Never change a test so that wrong output passes.

## Game-specific

- The simulation is separate from rendering. Rendering only reads the game state and never changes it.
- The simulation runs in fixed ticks and is deterministic: the same script with the same starting state always gives the same result. Randomness only with a fixed seed.
- Game rules and numbers (crops, costs, growth times, unlocks) live in data files, not in code.
- The drone API is the same set of commands in every language. Adding a command means adding it to every language at once.

## Performance

- Make it work, make it right, make it fast. Only optimise what has been measured to be slow.
- "Right" is a fixed step, not an extra: every working version is followed by a clean-up pass with the checklist below.
- In the hot paths (the interpreter loop and the tick update), no unnecessary layers or indirection.

## Tests

- Test behaviour, not implementation. A test fails when the code is broken, and uses simple assertions.
- Each language gets a set of example scripts with the expected end state. The same script in different languages must give the same end state and the same number of ticks.
- Every fixed bug comes with a test that shows the bug.
- All tests pass before every commit. No refactoring without tests.

## Privacy and publishing

- Nothing personal in the repository: no real names, email addresses, home town, school, absolute paths (such as `/Users/...`), computer user names, or references to private documents and chats. Write "the owner", never a name.
- No secrets: API keys, tokens, passwords, `.env` files.
- Nothing goes online (push, public repository, release, issue, gist) unless the owner has seen it first and explicitly agrees.
- Before every push, check both the files and the commit history (author, email, messages) for personal or sensitive information, and report what you found.
- If something slipped into a commit that has not been pushed yet, rewrite the history so it no longer appears anywhere. If it has already been pushed, report it immediately, and treat a leaked key as known and replace it.

## Git

- Small commits with one topic. Keep clean-ups and behaviour changes in separate commits.
- Messages follow Conventional Commits, with an English description in the imperative mood: `feat(interpreter): add while loop`. Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`.
- Commits use the GitHub account `Backpacker2` with GitHub's noreply address, never a real name or a private email address. No AI attribution: no `Co-Authored-By`, no "Generated with".
- A new dependency only after agreement with the owner, with the reason in the commit message.

## Check before every commit

Go through this before proposing or committing anything:

- [ ] Linter without warnings and all tests green?
- [ ] No personal data or secrets in the files or in the commit message?
- [ ] Types on every function, and a doc comment on every public function?
- [ ] Comments that repeat what the code says?
- [ ] Constants or helper functions used only once that explain nothing?
- [ ] `try`/`catch` or checks that cannot fail, or errors that disappear silently?
- [ ] A second version of something that already existed?
- [ ] Code or options for a future case that is not needed now?
- [ ] Dead code, debug prints, half-renamed variables or old attempts?
- [ ] A test changed to accept wrong output?
- [ ] Style changes mixed with behaviour changes?

## Still to fill in

Once the language and engine are chosen:
- Commands for building, testing and linting.
- The official formatter and linter of that language. Rules the linter already enforces are not repeated here.
- The test framework and the tool that generates documentation from the doc comments.
