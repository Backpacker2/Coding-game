# Gameplay plan

How the Coding Game should play and feel, from the first minute to the long run, and the steps to get there. It builds on [`game-design.md`](game-design.md) (the first 30 minutes: crops, commands, unlock order) and on the visual plan in [`visual-plan.md`](visual-plan.md). It turns the research below into milestones 29 to 35 in [`TODO.md`](TODO.md).

We study other programming and automation games only to understand the genre: what makes the loop satisfying and where players get stuck. Our game keeps its own crops, commands, unlocks, names and text.

## Where we are (0.10.x)

The core loop from `game-design.md` works: write a script, Run, the drone farms, items pay for seven unlocks that teach sequence, loops, conditions and functions, and since 0.10.0 the field stays between runs. What the player is missing:

- **No visible goal.** Nothing on screen says what to aim for next. The player has to open Unlocks to find out what they can afford.
- **Onboarding is a starter script and a Help list.** There is no guided first step and no hint when the player is stuck.
- **No mastery.** Once a script works, there is no reason to make it better: no measure of how good it is.
- **One script window.** The player cannot keep a working script running while they try a better one.
- **No moments.** Buying an unlock, a first ripe potato or a script that finally works pass without ceremony.

## What the research says

Sources are listed at the end.

### The loop

1. **Write, run, watch, improve.** The fun of the genre is the moment a script works and the farm runs by itself. Every improvement to the code must have a visible effect on the field.
2. **It is partly an idle game.** Once a loop runs, players enjoy watching the drone work. Progress should keep coming while the script runs, without the player pressing anything.
3. **One continuous farm, no levels.** The field grows as the player progresses; nothing is thrown away.
4. **Unlocks are new ideas, not bigger numbers.** The best unlocks add a command or a language feature that makes the old script feel clumsy, so the player rewrites it. This is already the backbone of `game-design.md`.

### Learning

5. **Learn by doing, not by reading.** Text walls are the most criticised part of programming games. A new idea should come with a tiny task the player will almost surely solve, right away ("a hallway that gets slightly narrower", not a room with one door).
6. **Apply, then link, then vary.** After a new command: use it alone, then combine it with what the player knows, then meet it in a new situation.
7. **Difficulty jumps lose beginners.** Reviews of the best-known games in the genre name the same weak spot: sudden jumps and too little help when stuck. Hints must exist, but must be asked for, so experienced players are not slowed down.
8. **Experienced programmers want to go fast**, and to optimise. A measure of how good a script is (ticks per harvest, items per minute) gives them something to chase.

### Pacing

9. **Each reward costs a bit more than the last**, but income grows with the player's skill, so the gap feels fair. Prices go up; better scripts bring more.
10. **Always show the next goal and how close it is.** A progress bar toward the next unlock turns a number into a target.
11. **Save big moments for rare events.** A bought unlock or a new crop gets a short celebration; a harvest only gets its small burst (already built).
12. **Small charm matters.** A playful command that does nothing useful (the drone does a little spin) makes players experiment and share.

## Rules for every gameplay milestone

- Game rules and numbers (prices, goals, hints) live in `data/`, not in code.
- The simulation stays deterministic. Goals and stats are computed from the game state, never from wall-clock time.
- Every new command exists in every language at once (`CLAUDE.md`).
- A script error stays gameplay: a clear message with a line number, plus an optional hint.
- No content, names, text or unlock trees copied from other games.

## Who builds what

The build thread owns the game rules and the core library (`src/CodingGame.Core`, `data/`, the save file). This thread owns the view. Each milestone below says which part each thread builds; a milestone with both parts is built core first, then view.

## Milestones

### 29. Next goal on screen

- View: next to the item bar, the cheapest unlock the player cannot afford yet, with a progress bar ("for loop: 7 / 10 clover"). When one becomes affordable, the Unlocks button glows and shows a dot.
- Core: none; it reads `GameProgress` and the unlock prices.

### 30. Moments

- View: buying an unlock gives a short banner over the field ("New: while loop") with a one-line summary and a "Show me" link to its Help entry. A crop harvested for the first time gets a bigger burst.
- Core: none beyond what exists.

### 31. First steps (guided onboarding)

- Core: a short list of first goals in `data/goals.json`, each checked from the game state: "Run the script", "Harvest 3 clover", "Make the drone visit every tile", "Buy your first unlock". Progress is saved.
- View: one goal at a time in a small card under the item bar, with a tick when done. No text walls: each goal is one sentence and a tiny code hint that the player can open.

### 32. Hints when stuck

- Core: when the same script error happens three times in a row (counted in the session, not in the save, so runs stay deterministic), the error gets an extra hint line from `data/hints.json` (for example for a harvest of an unripe radish: "Check `ripe()` before you harvest").
- View: the hint shows under the error in the Messages box, in a calmer colour, folded open on request.

### 33. Script stats and personal bests

- Core: per run, items gained per 1,000 ticks (counted in ticks, never wall-clock time); kept as a best per crop in the save.
- View: a small stats line in the script window ("+12 radish per 1,000 ticks, best 15"), and a short "New best!" moment when beaten.

### 34. A second script window

- Core: more than one saved script (named tabs), one of which runs. Needs the save format (decision record).
- View: tabs in the script window, or a second floating window; the running one is marked.
- Waits for the owner's yes: it changes gameplay and the save format.

### 35. A playful command

- Core: `spin()` in every language: the drone turns once, costs a few ticks, does nothing else.
- View: the spin animation.

The persistent field (milestone 16, done in 0.10.0) was the base for all of these: goals, stats and idle play need a farm that stays.

## Later

- An order board: requests like "deliver 50 radish" as targets beside the unlocks (open question 6 in `game-design.md`).
- More drones as a late unlock, which teaches running code in parallel.
- The second language as an unlock that shows the player's own script translated side by side (open question 1).
- Sound, once the owner agrees to an asset dependency.

## Sources

- Steam store page of a farm-automation coding game: continuous progression without levels, concepts one at a time, the pleasure of pressing Run. <https://store.steampowered.com/app/2060160/The_Farmer_Was_Replaced/>
- DLCompare on the same game's automation loop: unlocks as new functions that force refactoring; players share code. <https://www.dlcompare.com/gaming-news/a-harvest-of-code-automating-the-farm-in-the-farmer-was-replaced>
- GamingOnLinux review: partly an idle game; one growing plot; several editor windows; "won't hold your hand". <https://www.gamingonlinux.com/2025/10/the-farmer-was-replaced-is-a-satisfying-way-to-learn-a-little-programming-with-automation>
- GameIndustry review: motivation from visible effect of each code improvement; playful commands add charm. <https://www.gameindustry.com/reviews/modern-gamer/the-farmer-was-replaced-harvests-fun-from-coding-fieldwork>
- SUPERJUMP on Human Resource Machine: apply, link, vary; difficulty jumps and little support lose beginners. <https://www.superjumpmagazine.com/integrating-learning-with-gameplay-human-resource-machine/>
- Zach Barth on SpaceChem's tutorial: learn by small experiments, not reading; a narrowing hallway. <https://www.gamedeveloper.com/design/spacechem-s-zach-barth-on-educational-games-tutorial-design>
- Tuts+ on incremental games: rates that grow, rewards that cost more each time, strategy in how to grow. <https://code.tutsplus.com/numbers-getting-bigger-what-are-incremental-games-and-why-are-they-fun--cms-23925a>
