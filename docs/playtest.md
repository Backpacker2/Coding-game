# Play-test guide

This guide is for the owner. It takes about 45 minutes. Please report what you find (step 16).

The field starts 3x3. This is for testing only, so that nested loops can be tried. The size is set in `data/field.json`. The Bigger field unlock grows it to 5x5.

## Steps

1. Install the .NET SDK 10 from https://dotnet.microsoft.com/download. Install the SDK, not only the runtime. Open a terminal and run `dotnet --version`. The output must start with `10.`.

2. Download Godot 4.7.2 from https://github.com/godotengine/godot/releases/tag/4.7.2-stable. Take the stable `mono` build, not the plain build. The plain build cannot run the C# code.
   - Windows: `Godot_v4.7.2-stable_mono_win64.zip`
   - macOS: `Godot_v4.7.2-stable_mono_macos.universal.zip`
   - Unzip the file.
   - On macOS, the first start may need right-click, then Open, because the app is not signed.

3. Get the code. Either run `git clone https://github.com/Backpacker2/Coding-game.git`, or download the zip of the newest release from the repository's Releases page.

4. Open Godot. Press Import, choose `src/CodingGame.Godot/project.godot` in the code folder, and press Import and Edit.

5. If Godot asks to create a C# solution, answer yes. If the build fails, open a terminal in the code folder, run `dotnet build src/CodingGame.Godot`, and send the output.

6. Press the Play button (top right, or F5). The game window opens.

7. Things to try:
   - Press Run on the starter script. The drone collects two clover.
   - Change the script and press Run again.
   - Press Step several times and watch the yellow line move.
   - While a loop runs, change the speed to Slow and then to Fast. The drone moves slower or faster, and the end result stays the same.
   - Press Stop. The run ends, the farm stays as the run left it, and your items stay.
   - Switch the language between Python and JavaScript. Each language keeps its own script text, so your edits come back when you switch back.
   - Cause an error. For example type `harvest(` (a syntax error), or `mvoe(EAST)` (a name that is not defined, found when the line runs). The line turns red and the message shows the line number.
   - Harvest clover with the starter script and press Run several times. Watch the item bar at the top left grow. Items stay between runs and after Stop.
   - Buy `for loop` for 10 clover, then write a `for` loop and run it.
   - Buy `Bigger field` for 30 clover. The field becomes 5x5 at once.
   - Before you buy `Radish and plant()`, write `plant(RADISH)`. The error `'plant' is not unlocked yet` shows with the line number, and the line turns red. The drone does not move.
   - Buy `Radish and plant()` for 40 clover. Then harvest a clover and plant a radish on that same tile, so you need at least 2 clover in the bank. The radish replaces the clover on the tile.
   - After buying `for loop`, write a loop that moves the drone 10 times and run it.
   - Later, once you have radish to buy `if / else and ripe()` and `while loop`, write a JavaScript loop that never ends: `while (ripe()) {}`. Press Run, watch the tick counter, then press Stop. Press Step on it too: it stops after a fixed number of ticks.
   - Buy something, edit the script in both languages, close the window, and start the game again. Your items, your unlocks, both scripts and the language should all be back.
   - Your progress is in the file `save.json`, in Godot's user data folder for the project "Coding Game" (Godot menu Project, Open User Data Folder). The New game item in the Menu resets it (step 9). Deleting `save.json` also starts over.
   - After a run that harvests something, a stats line under the toolbar shows what the run gained, its rate per 1,000 ticks and the best rate so far. Make the script faster (fewer moves for the same harvest) and run it again: the line turns green and says New best!
   - Open `save.json` in a text editor, replace its whole content with the word `garbage`, and start the game. A red notice explains that the save could not be read. The old file is kept as `save.json.broken` in the same folder, and the game starts as a new game.

8. Help and Unlocks windows:
   - At the start only the Script window is open. Press Help (top right). The help window opens; press Help again and it closes.
   - Press Unlocks (top right). The unlock list opens in its own window. Press Unlocks again to close it.
   - Close a window with its close button, then press its button again to open it.
   - Press Code to hide the Script window and press Code again to bring it back.
   - At the start the help lists `move`, `harvest`, `NORTH`, `EAST`, `SOUTH`, `WEST` and `CLOVER`. It does not list `for`, `while`, `if` or `def`.
   - Switch the language to JavaScript. The examples gain semicolons and braces.
   - Buy the for loop. `for` appears.
   - Buy Radish and plant(). `plant` and `RADISH` appear.

9. New game:
   - Open the Menu and press New game, then Cancel. Everything stays as it was.
   - Open the Menu and press New game again and confirm. Check that the item bar says No items yet, every unlock says Buy, the starter script is back in both languages, the language is Python, there is one script tab called `main`, and the field is 3x3.
   - Close the window and start the game again. It starts as a new game too.
   - Start a run with a long loop. Check that the New game item in the Menu is greyed out while it runs, and that it is active again after Stop.

10. Crops stay on the field:
   - Once you have bought `Radish and plant()`: a radish replaces the clover on its tile, so every starting tile can take one. The radish costs 2 clover to plant.
   - Write a script that harvests the drone's tile, plants `RADISH` on it and then moves the drone one tile east. Press Run and let it finish.
   - Press Run again with a script that moves the drone one tile west. The drone starts one tile east, where the last run stopped, so after the move it stands on the radish. The radish is still there and has kept growing. Do not harvest it yet: an unripe radish is destroyed.
   - Close the window and start the game again. The radish and the drone are still there.

11. Script tabs:
   - Start steps 11 to 14 with a new game: open the Menu, press New game and confirm. Items, goals and scripts start from zero.
   - At the start there is one tab, `main`. It has no close button, because a game always keeps one script.
   - Press the + button next to the tabs. A tab `script 2` appears and is selected. The editor shows the starter script.
   - Right-click the tab `script 2`. A window called Rename script opens. Type `drone two` and press Rename. The tab shows the new name.
   - Right-click the tab and rename it to `MAIN`. A red notice above the Messages box says `Name not changed: a script with this name already exists.` The name stays `drone two`. Capitals do not count, so `MAIN` is the same name as `main`.
   - Each tab keeps its own text. On `main`, in Python, add the line `spin()` at the end. Switch the language to JavaScript and add `spin();` at the end. Click `drone two`: it shows the starter script in JavaScript. Switch to Python: it shows the starter script in Python. Click `main`: Python ends with `spin()` and JavaScript ends with `spin();`. Delete both lines again.
   - Tabs lock during a run. Switch to Python, select `drone two` and replace its text with `spin()` on three lines. Choose Slow and press Run. The tab reads `▶ drone two`, the + button and the tab `main` are greyed out, and a right-click on `drone two` does nothing. Press Stop after about three seconds. The run stops, and the tabs work again.
   - Close a tab: select `drone two`, then click its × on that tab. The tab disappears and `main` is selected. With `main` alone there is no × again.
   - The tabs survive closing the game: press + to have two tabs again, with `script 2` selected. Close the game window and start the game again. Both tabs are there, and `script 2` is selected with its text.

12. The next-goal bar and the goal card:
   - At the start a bar under the item bar says `Next: for loop - 0 / 10 clover`. The card under it says `Run the starter script to the end.` Press `?` on the card: `Press Run.` appears under it. Press `?` again to hide it.
   - Click the tab `main` and press Run. Let the starter script finish. The card shows `✓ Run the starter script to the end.` in green for about 1.5 seconds, then `Have 3 clover.` with a `?` button. The item bar shows 2 clover.
   - Press Run again. The item bar shows 3 clover and the bar says `Next: for loop - 3 / 10 clover`. The card shows `✓ Have 3 clover.`, then `Fly the drone over every tile in one run.`
   - Click the tab `script 2` and replace its text with the script below. It moves the drone over all nine tiles of the 3x3 field in one run. In Python:
     ```
     move(EAST)
     move(EAST)
     move(SOUTH)
     move(WEST)
     move(WEST)
     move(SOUTH)
     move(EAST)
     move(EAST)
     ```
     In JavaScript:
     ```
     move(EAST);
     move(EAST);
     move(SOUTH);
     move(WEST);
     move(WEST);
     move(SOUTH);
     move(EAST);
     move(EAST);
     ```
     Press Run. The card shows `✓ Fly the drone over every tile in one run.`, then `Buy your first unlock.`
   - Click the tab `main` and press Run again and again until you have 10 clover. Clover that is not ripe gives nothing, so a run can give 0. When the bar says `Next: for loop - ready, open Unlocks`, the Unlocks button pulses softly.
   - Click Unlocks and press Buy on for loop. The card shows `✓ Buy your first unlock.` for a moment, then it hides, because all four goals are done. The bar now shows the next unlock, Bigger field, at 30 clover.

13. The unlock banner:
   - Right after you buy for loop in step 12, a banner appears over the field. It says `New: for loop` and `You can now use for`, and it has a Show me button.
   - Press Show me. The Help window opens and the banner closes. Help now lists `for`. Press Help again to close the window.
   - Optional: when you can pay for it, buy Bigger field (30 clover). The banner says `The field is now 5x5`. This time do not press Show me. The banner fades out by itself after a few seconds.

14. Hints when stuck:
   - In Python, write `mvoe(EAST)` as the whole script and press Run. The Messages box shows `NameError on line 1: name 'mvoe' is not defined`. No hint appears.
   - Press Run again without changing the script. The same message comes, and still no hint.
   - Press Run a third time. A button `Show hint` appears under the Messages box. Press it: the text `Hint: Check the spelling. Help lists every name you can use.` appears. Press `Show hint` again to fold it away. The hint stays from the third run on, as long as the same error repeats.
   - Change the script to `move(EAST)` and press Run. The `Show hint` button disappears.
   - In JavaScript, write `mvoe(EAST);` and run it three times. The message is `ReferenceError on line 1: mvoe is not defined`, and the same hint appears after the third run.

15. spin():
   - Press Help. `spin` is listed from the start, with its example. It needs no unlock.
   - Write `spin()` as the whole script in Python and press Run. The tick count shows `Ticks: 10 - finished`. The drone turns one full circle on its tile.
   - Write three lines `spin()` and press Run. The tick count shows `Ticks: 30 - finished`. The drone turns three full circles, one after another. Each turn takes about 0.6 seconds, so the last turn can end a moment after the run says finished.
   - Do the same in JavaScript with `spin();` lines.

16. What to report:
   - Do the three speeds (Slow, Normal, Fast) feel right? At Normal, a move should take about one second.
   - Is anything hard to read or ugly?
   - Is anything confusing?
   - Did anything crash, or did Godot print an error?
   - Does the 3x3 starting field look right?
   - Are the prices fair? How many minutes did it take to reach the radish unlock?
   - Is the unlock list clear? Did you know what to do next?
   - Did saving surprise you anywhere?
   - Do the goals and the hints help? Did they come at the right time, or did they get in the way?

## Known limits

- Progress and all your scripts (every tab, in both languages) are saved when you close the game, after each buy, when a run starts or ends, and after you add, close or rename a tab. Items from a run that is cut off by a crash are lost.
- JavaScript is not an unlock yet: both languages are open from the start.
- The art is placeholder: simple coloured shapes built in code for the crops, and a grey drone with four rotors and a blue eye.

## Update to the newest version

Do this before every test round, so you test what is on `main`.

1. Close Godot.
2. Get the newest code: in the code folder run `git checkout main` and `git pull`. If `git pull` stops with "Your local changes to the following files would be overwritten by merge", run `git stash` and then `git pull` again. That sets the local changes aside; they are usually changes Godot made by itself (it rewrites `project.godot` when its version differs from 4.7.2, and its script editor can rewrite indentation when a `.cs` file is saved in it). Do not edit the code in Godot's script editor. Check with `git log -1 --oneline` that the newest merge is on top. Or, in GitHub Desktop, pick the branch `main` and press Fetch, then Pull origin. If you use the zip, download the newest release again.
3. Open the project in Godot and press the hammer icon (Build) at the top right, or run `dotnet build` in the code folder.
4. Still the old look? Close Godot, delete the folder `src/CodingGame.Godot/.godot` and open the project again. Godot rebuilds that folder. Your save is not in it and is kept.
5. The version is in `Directory.Build.props` and at the top of `CHANGELOG.md`; check that it matches the newest release.

The editor's built-in game view keeps the game at the size of that view. To see the game maximised, turn off Embed in the Game tab's menu, or run the exported game.

## Troubleshooting

- **Godot cannot find the .NET SDK:** install the SDK from step 1, then restart Godot.
- **NU1201 error when building:** a `net8.0` line was added to `src/CodingGame.Godot/CodingGame.Godot.csproj`. Remove the `net8.0` line. The `net10.0` line must stay.
- **NU1900 error "Error occurred while getting package vulnerability data":** fixed in 0.10.3. Update to the newest version (above). It means the NuGet vulnerability check could not reach the internet.
- **The game still looks like an older version:** follow "Update to the newest version" above.
- **Empty window and a `GameRulesException` in Godot's output:** the `data` folder was not copied. Run `dotnet build src/CodingGame.Godot` in the code folder, then restart the game.
