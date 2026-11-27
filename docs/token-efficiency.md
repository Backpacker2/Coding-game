# Token efficiency: where the tokens go and what to change

The owner sees many cache-read tokens and few output tokens across the project. This document explains why, ranks the causes by estimated impact, and proposes changes. On 27 November 2026 the owner agreed to all ten changes; they are now in `CLAUDE.md`, `docs/workflow.md`, the `tiered-loop` skill and the agent files.

The numbers are estimates from the repository, the project's threads and how Claude Code bills tokens. No per-session usage figures were available, so the ranking is about orders of magnitude, not exact counts.

## Why cache reads dominate

Every turn of an agent (every tool call and every reply) sends the whole conversation so far to the model again. The part that was sent before is read from the prompt cache. So:

> cache-read tokens per session ≈ average context size × number of turns

Output only grows with what the model writes, which is small for an agent that mostly reads files, runs commands and edits a few lines. A ratio of 50 or 100 cache-read tokens to one output token is normal for agent work. Cache reads cost about a tenth of fresh input per token, so they look worse than they are, but at this volume they are still the largest part of the cost.

The lever is therefore the product of two things: **how big the context is** and **how many turns run on it**.

## The biggest token sinks, ranked

| # | Sink | Why it is big | Estimated share |
| --- | --- | --- | --- |
| 1 | One long build session for many milestones | The build thread ran about 14 hours (25 Nov 21:28 to 26 Nov 11:41 UTC) and shipped dozens of milestones and 69 merges in one context. Late in that session every turn re-reads hundreds of thousands of tokens of old diffs, test logs and reports that no longer matter. | Largest, likely half or more |
| 2 | Fixed start-up cost of every agent | Each session and each subagent starts with a large system prompt: tool schemas, a long list of deferred tools and MCP servers, skill lists and `CLAUDE.md` (11 KB, about 3,000 tokens). That is tens of thousands of tokens before any work, and it is re-read on every turn of that agent. One milestone starts a lead twice or more, one coder per task and a merger. | Large |
| 3 | Duplicate reading of the rules and documents | `CLAUDE.md` is already loaded into every agent automatically, yet `coder.md`, `lead.md` and the `tiered-loop` skill each tell the agent to read it again. The lead also reads `docs/workflow.md`, `docs/TODO.md` and every file the milestone touches; the orchestrator reads `workflow.md` and `TODO.md` again each loop. The design documents in `docs/` add up to about 130 KB (about 33,000 tokens). | Medium |
| 4 | Build and test output in the context | `dotnet build` and `dotnet test` run about five times per change: the coder, the lead's review, the orchestrator, and the merger twice. Each run's full log lands in an agent's context and is then re-read on every following turn. | Medium |
| 5 | Review overhead per small task | The lead reviews every coder task separately with the full checklist, and the orchestrator reviews the milestone again, then the merger reviews it a third time. For a small task, three reviews of the same diff cost more than the code itself. | Medium |
| 6 | One pull request and one merger pass per milestone | 69 merges means 69 times: fetch, read diff, renumber versions, run all checks, push. Version clashes between parallel threads ("merge origin/main and renumber") add rework. | Small to medium |
| 7 | Coordinator wake-ups | Every reply in a thread wakes the coordinator, which carries the project chat history. Many short progress replies mean many coordinator turns. | Small to medium |
| 8 | Screenshots | Each screenshot is about 1,500 to 2,500 tokens. Once in the context it is re-read on every later turn, so an early screenshot in a long session costs that amount hundreds of times. | Small, grows with session length |
| 9 | Status page updates | Already slimmed down on 26 November to one small `now/build` update per step. Each update is still a turn on the full context, so its real cost is the context, not the write. | Small now |

## Proposed changes, in order of priority

Each change names what it saves and what it costs. The first three give most of the gain.

1. **One fresh session per milestone (or per two or three small ones).** The orchestrator ships the milestone, writes a short hand-over (what was done, what is next, open questions) in `docs/TODO.md`, and the next milestone starts in a new thread. Saves the most, because the context stays small; addresses sink 1. Costs a little start-up per session, and needs the coordinator to start each new thread.
2. **Stop telling agents to re-read `CLAUDE.md`.** Remove "Read `CLAUDE.md`" from `coder.md`, `lead.md` and the `tiered-loop` skill, because it is already in their context. Let the lead read only the files the milestone touches, and let the orchestrator read only the open part of `docs/TODO.md`. Addresses sink 3. No downside.
3. **Quiet builds and tests.** Use `dotnet build -warnaserror -v q -nologo` and `dotnet test --nologo -v q`, and only show the last lines (`| tail -20`) unless something fails. Run the full suite twice per change, not five times: once by the coder, once by the merger on the merged result. The lead reads the coder's reported result instead of running it again. Addresses sink 4.
4. **Lighter reviews for small tasks.** Let the lead review all coder tasks of a milestone in one pass instead of one by one, and let the orchestrator's own review look only at design and interfaces (as `workflow.md` already says). Keep the strict per-task review for the interpreter, the save format and other important interfaces. Addresses sink 5.
5. **Fewer, bigger merges.** Bundle small milestones into one branch and one pull request, so one merger pass covers them. One thread at a time on `main`-touching work avoids renumbering. Addresses sink 6.
6. **Fewer progress replies.** Reply in a thread only for a result, a question or a pull request; keep progress on the checklist. Each reply skipped is one coordinator wake skipped. Addresses sink 7.
7. **Screenshots only when the owner asks or a visual milestone ships, and at scale 1 for checks.** Save them to project files rather than reading them back into the context. Addresses sink 8.
8. **Shorter `CLAUDE.md`.** Move the reasoning and the less-used sections (versions table, branch details, merge steps) to `docs/coding-standards.md` or `docs/workflow.md`, which only the agents that need them read. Done on 27 November: the branch and version rules moved out, which took it from 11 KB to 9 KB, about 450 tokens less on every turn of every agent. The remaining rules are used in nearly every task, so they stay. Addresses sink 2 partly; the tool and system prompt part is outside the repository.
9. **Status page: update only when the state changes** (start, waiting on the owner, pull request open, merged), not at every loop step. Addresses sink 9.
10. **Consider Sonnet as the orchestrator for routine milestones**, and keep Opus for milestones that change an important interface. The orchestrator carries the longest context, so its price per token matters most. This changes the tiered setup, so it is the owner's choice.

## How to check the effect

After the first changes, compare one milestone in a fresh session with a similar milestone from the long build session: tokens per merged pull request is the simplest measure. The usage page of the Claude account shows the totals per day.
