# Coding standards: the reasoning

This document explains where the rules in `CLAUDE.md` come from. `CLAUDE.md` only holds the rules; this document holds the reasons, and the alternatives that were rejected.

Researched on 25 November 2026.

## Why a short CLAUDE.md with a separate document

Claude reads `CLAUDE.md` at the start of every session, but treats it as context, not as enforced configuration. The more specific and concise the instructions, the more consistently they are followed ([Claude Code docs](https://code.claude.com/docs/en/memory)). General instructions such as "write clean code" work poorly; rules need to be specific and contain something that cannot be learned from the code itself ([DataCamp](https://www.datacamp.com/tutorial/writing-the-best-claude-md)). Rules that a linter already enforces do not belong there.

That is why `CLAUDE.md` only says what to do, and the explanation lives here.

## Complexity as the main problem

The foundation comes from *A Philosophy of Software Design* by John Ousterhout (Stanford). His core point: the biggest problem in software is complexity, and it grows in small steps. These rules come from his [summary of design principles and red flags](https://yingang.github.io/aposd-zh/en/summary.html):

- **Deep modules.** A module is good when its interface is much simpler than what sits behind it. A "shallow" module, whose interface is almost as complex as its code, only adds a layer.
- **No pass-through methods.** A function that mostly forwards its arguments to a similar function is a red flag.
- **No information leakage.** When one design decision shows up in several modules, a change means editing all of them.
- **Define special cases out of existence.** A design in which an error cannot exist beats a check everywhere.
- **Design it twice.** For important choices, look at more than one design first.
- **Hard to name means bad design.** If no precise name comes to mind, the thing itself is often not well defined.

The same message, in an informal style, appears in [The Grug Brained Developer](https://grugbrain.dev/): complexity is the enemy, and saying "no" to a feature or an abstraction is the best weapon against it.

## Abstraction: only once the pattern is really there

In [The Wrong Abstraction](https://sandimetz.com/blog/2016/1/20/the-wrong-abstraction), Sandi Metz describes a familiar pattern. Someone sees duplication and turns it into an abstraction. After that, every new case adds another parameter and another `if`, until nobody understands it. Her advice: duplication is far cheaper than the wrong abstraction. If you are stuck in the wrong abstraction, move the code back into the callers and start again.

**Rejected alternative: strict DRY ("Don't Repeat Yourself").** DRY says every repetition has to go. Metz has her critics: duplication has costs too, and never abstracting builds up debt ([Jason Swett](https://www.codewithjason.com/?p=2480)). So we take a middle path: abstract at the third real case, not the first.

## Behaviour close to where it happens

[Locality of Behaviour](https://htmx.org/essays/locality-of-behaviour/) (Carson Gross) says you should be able to see what a piece of code does by reading only that piece. It sometimes conflicts with DRY and with separation of concerns, so it is a trade-off, not a hard rule. That is why code is organised by part of the game rather than by technical kind of file.

## AI slop: what we explicitly guard against

"Slop" is code that looks fine and passes the tests, but makes the project harder to maintain. Because Claude helps write this project, these pitfalls are listed separately in `CLAUDE.md`.

A study by OX Security of more than 300 repositories found the same patterns again and again in AI-generated code ([InfoQ](https://www.infoq.com/news/2025/11/ai-code-technical-debt)):

| Pattern | What it is | Found in |
| --- | --- | --- |
| Comments everywhere | Comments meant for the AI make the code harder for people to review | 90–100% |
| By-the-book fixation | Textbook patterns instead of code that fits this application | 80–90% |
| Avoidance of refactors | Doing what was asked without improving the structure | 80–90% |
| Over-specification | Handling for edge cases that are unlikely to happen | 80–90% |
| Bugs déjà vu | Rewriting functionality instead of reusing it, repeating earlier bugs | 80–90% |

Anaconda describes similar pitfalls, each with a fix ([Anaconda](https://www.anaconda.com/blog/deslopping-ai-code)). These rules come from there:

- Comments about why, not what.
- No constants or helper functions used only once, unless the name explains something.
- No `try`/`catch` around code that cannot fail; such blocks hide real errors.
- Reuse existing helpers instead of writing a second version.
- Fix the cause of an error, not the place where it shows up. Never change a test to match wrong output.
- Split long files, because helpers get missed and duplicated in large files.

Other common forms: TODOs that never go away, over-defensive checks on input that is already typed, types redeclared that already exist, and dead code from earlier attempts ([DEV Community](https://dev.to/heavykenny/ai-slop-is-becoming-a-software-engineering-problem-2n00)).

## Catching errors at the edges

From [Parse, don't validate](https://lexi-lambda.github.io/blog/2019/11/05/parse-don-t-validate/) (Alexis King): check untrusted input as early as possible, at the moment it enters the system, and turn it into a precise type there. After that, the rest of the code does not need to check again.

Coding Game has three edges: player scripts, save files and config files.

## Game-specific: fixed ticks, simulation separate from rendering

From *Game Programming Patterns* by Robert Nystrom, the chapter [Game Loop](https://gameprogrammingpatterns.com/game-loop.html): run the simulation in fixed steps and render as often as the machine allows. With variable steps the game becomes non-deterministic, because rounding errors add up differently on each machine. Fixed steps also make the game easier to debug.

For Coding Game this matters even more. The whole game revolves around ticks: how much a script costs decides how well you play. So the same script must always give the same number of ticks and the same result, and the same in every language.

## Performance: not up front, but not naive either

Kent Beck: make it work, make it right, make it fast. Optimising before you know where it is slow costs time and makes the code more complex.

In ["Clean" Code, Horrible Performance](https://www.computerenhance.com/p/clean-code-horrible-performance), Casey Muratori shows that well-known "clean code" rules, such as preferring polymorphism over `switch`, cost a lot of speed. Muratori comes from game engine work, where speed comes first.

**Rejected alternative: adopting *Clean Code* by Robert Martin as a whole.** Its rules about very small functions and class hierarchies produce many layers, which conflicts with deep modules (Ousterhout) and with speed in the hot paths (Muratori). We do keep its good parts: clear names and code that reads like what it does.

The middle path: in most of the code, readability comes first. In the interpreter loop and the tick update, which run millions of times, no unnecessary layers.

## Working practices: linter, types, documentation, tests

In [10 Easy Tips to Write Code Like a Pro](https://towardsdatascience.com/how-to-write-code-like-a-pro-659760804724/), Graham Harrison gives practical habits for Python. Some fit well; some conflict with the rest of this research.

Adopted:

- **Linter.** A linter points out syntax and style problems and catches subtle errors. Code must pass the linter without warnings.
- **Types.** Without types, a caller cannot see what a function expects. Types make code easier to read and use. Required in Coding Game, whatever language the game is written in.
- **Doc comments and generated documentation.** Harrison generates documentation from types and docstrings. We adopt that, but only for public interfaces, so it does not conflict with the rule that comments must not repeat the code.
- **A fixed folder structure.** One folder with everything in it becomes messy. The exact layout matters less than using it consistently. We use `src/`, `tests/`, `data/` and `docs/`.
- **Unit tests.** Tests let you change code with confidence, because one test run shows what broke.
- **Repeated refactoring.** After a working version, clean up: remove unnecessary lines, use existing libraries instead of home-made code. This matches Kent Beck: first make it work, then make it right.
- **Knowledge in one place.** Harrison changed a text encoding but missed a few places, which caused bugs in production. That is exactly Ousterhout's rule: one design decision in one place.

Rejected:

- **Object-oriented programming with inheritance and polymorphism as the default.** This often leads to deep class hierarchies and extra layers, which conflicts with deep modules (Ousterhout) and costs speed in the hot paths (Muratori). We only use classes where they give a simpler interface, and prefer composition over inheritance.
- **Strict DRY.** Harrison wants all duplication gone. We follow Metz: a few duplicated lines are fine, but knowledge (such as that text encoding) lives in one place.
- **A general "Utilities" library.** A `utils` folder becomes a junk drawer where nothing can be found. Code needed in several places gets a module whose name says what it does.
- **Handling exceptions everywhere.** Harrison lists this as part of refactoring. We only catch errors where we can do something useful with them; otherwise they hide real problems (see AI slop).

Two tips are about learning, not about code: write about what you build, and keep reading widely. They are not in `CLAUDE.md`, because they are not rules for the code. A devlog in `docs/` may fit later, since learning is the goal of the project.

## Code review: what to look for

[Google's code review guidelines](https://google.github.io/eng-practices/review/reviewer/looking-for.html) list design, functionality, complexity, tests, naming, comments, style, consistency and documentation. Two points were adopted:

- Watch especially for over-engineering: generality or features the system does not need yet.
- Keep large style changes separate from behaviour changes.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) gives a fixed format: `type(scope): description`. The benefit is that the history shows at a glance what each commit does. If a commit fits more than one type, split it.

## Still open

- The style rules per language follow once the language and engine for the game itself have been chosen. We will then use that language's official formatter and linter.
