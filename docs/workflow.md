# Workflow: how Claude builds this game

This is how every Claude session works on the Coding Game. The goal is the quality of the strongest model at a fraction of its cost: the expensive model only plans and checks, and the cheap models do the volume work.

## Roles

| Role | Model | Defined in | Does | Never does |
| --- | --- | --- | --- | --- |
| Orchestrator | Opus 5.5 (the main session) | `.claude/skills/tiered-loop/` | Keeps `docs/TODO.md`, picks the next milestone, starts the other agents, gives the final review, commits, opens the pull request, talks to the owner. | Writes large amounts of code itself. |
| Lead | Sonnet 5.5 | `.claude/agents/lead.md` | Splits a milestone into small, exact tasks. Reviews each coder result strictly against `CLAUDE.md`. | Edits files. |
| Coder | Haiku 5.5 | `.claude/agents/coder.md` | Carries out one task, test first. | Commits, or touches files outside its task. |
| Merger | Sonnet 5.5 | `.claude/agents/merger.md` | After a coding session: reviews every open pull request, resolves conflicts between them and `main`, and merges what passes. | Merges anything that fails a test, a rule or the privacy check. |

### Why the orchestrator starts every agent

Claude Code agents cannot start agents of their own. So the lead does not start the coders: it writes the task list, and the orchestrator hands each task to a coder and each result back to the lead. The chain of responsibility stays the same (Opus → Sonnet → Haiku); only the hand-offs go through the orchestrator.

The agent types `lead` and `coder` are only known to a session that started inside this repository. A session that started elsewhere starts a general agent with the right model instead, and tells it to read and follow its definition file.

## The loop

1. **Pick.** The orchestrator takes the first open milestone in `docs/TODO.md` and marks it in progress.
2. **Split.** The lead turns the milestone into numbered tasks (format in `.claude/agents/lead.md`).
3. **Check the plan.** The orchestrator reads the task list once. Wrong direction is cheapest to fix here.
4. **Build.** A coder carries out each task. Tasks that touch different files run in parallel.
5. **Review, level 1.** The lead reviews each result. On `CHANGES REQUESTED` the same coder fixes it. After two failed rounds the task goes back to the lead to be split smaller.
6. **Review, level 2.** The orchestrator reviews the whole milestone diff: design, interfaces, and whether it matches the design documents. It fixes small things itself; anything larger goes back to step 2.
7. **Ship.** The orchestrator runs the full checklist from `CLAUDE.md`, updates `CHANGELOG.md`, runs the privacy check below, commits, pushes and opens a pull request for the owner.
8. **Tick off and hand over.** The milestone is ticked in `docs/TODO.md` with a link to its pull request and a short hand-over: what is done, what is next, open questions. Then the session ends, and the next milestone starts in a fresh session (two or three small milestones may share one).
9. **Merge.** At the end of a coding session the orchestrator starts the merger. It reviews all open pull requests, resolves conflicts, and merges into `main` with git under the noreply identity, one version tag per merge. The orchestrator reads its report and tells the owner what was merged.

The owner gave standing agreement (25 November 2026) that the merger merges into `main` when a pull request passes its review. The loop still stops and asks the owner when a choice changes the game itself: a design decision, a new dependency, or anything else that cannot be undone.

## Privacy check before every upload

The owner's standing rule: nothing that goes online may show who the owner is. Before every push, merge, pull request, comment or other upload, the orchestrator checks all of it and removes anything personal:

- **Files:** no real names, email addresses, home town, school, absolute paths or computer user names (`CLAUDE.md`, Privacy and publishing).
- **Commits:** author and committer are `Backpacker2` with the GitHub noreply address; messages hold no names. The only other identity allowed is the trailer `Co-Authored-By: Claude <noreply@anthropic.com>`, which every commit carries. Check with `git log --format='%an %ae %cn %ce %s' origin/main..HEAD`.
- **Pull request and comment text:** refer to "the owner", never a name.

If anything is found, it is removed before the upload, from the history too (`CLAUDE.md`). An upload that fails this check does not happen.

## Keeping cost down

The analysis behind these rules is in `docs/token-efficiency.md`.

- **One milestone per session.** Every turn re-reads the whole conversation, so a long session gets more expensive with every step. A fresh session starts from `docs/TODO.md` and its hand-over.
- **No re-reading of `CLAUDE.md`.** It is loaded into every agent automatically. Agents read only the files their job needs.
- **Quiet checks, run twice.** Every log line stays in the context. Use `dotnet build -warnaserror -v q -nologo` and `dotnet test --nologo -v q`, and keep only the last 20 lines unless something fails. The coder runs the full suite, and the merger runs it on the merged result. The lead and the orchestrator read those results and only run the checks again after changing code themselves or when a result is missing.
- The orchestrator reads diffs and reports, not whole files, wherever it can.
- Tasks are small and fully specified, so the cheapest model can do them right the first time.
- Reviews are strict early. One rejected task is cheaper than one bad milestone.
- Parallel coders only for tasks with no shared files.

## Documenting everything

- `docs/TODO.md`: what is done, what is next. Ticked off with a pull request link.
- `CHANGELOG.md`: every version.
- `docs/decisions/`: every important design choice, with the rejected alternative.
- Pull request descriptions: what changed and why, and what the reviews found.

## Starting a session

Ask Claude to "continue the game loop" (or run the `tiered-loop` skill). It reads this file and `docs/TODO.md` and picks up where the last session stopped.
