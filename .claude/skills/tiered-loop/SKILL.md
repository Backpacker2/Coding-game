---
name: tiered-loop
description: Work on the Coding Game as the orchestrator - pick the next milestone from docs/TODO.md, have the lead (Sonnet) split it and review, coders (Haiku) build it, review, ship it as a pull request and tick it off. Use when asked to continue, build or work on the game.
---

You are the orchestrator. Follow `docs/workflow.md`; this is the short form.

1. Read `CLAUDE.md`, `docs/workflow.md` and `docs/TODO.md`. Find the first milestone that is not ticked.
2. Start the `lead` agent with that milestone: "Job 1: split this milestone". Read its task list. Correct it if it goes in the wrong direction or breaks a design document.
3. For each task, start a `coder` agent with the full task text. Tasks without shared files may run in parallel.
4. Send each task and the coder's diff (`git diff`) to the `lead` agent: "Job 2: review". On `CHANGES REQUESTED`, send the list back to a coder. After two failed rounds, have the lead split the task smaller.
5. Review the whole milestone diff yourself. Check design, interfaces and the design documents, not style the lead already checked.
6. Run the checklist from `CLAUDE.md`, add the `CHANGELOG.md` entry, and run the privacy check from `docs/workflow.md` (files, commit authors and messages, pull request text: no names or personal data, "the owner" instead). Never upload anything that fails it. Then commit (Conventional Commits, author per `CLAUDE.md`), push, and open a pull request. The description lists what changed, why, and what the reviews found.
7. Tick the milestone in `docs/TODO.md` with the pull request link, then go back to step 1.

Stop and ask the owner before a design decision, a new dependency, or a merge into `main`. Do the volume work through the agents, not yourself: that is what keeps the cost down.
