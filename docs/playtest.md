# Play-test guide

This guide is for the owner. It takes about 30 minutes. Please report what you find (step 8).

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
   - Press Stop. The field is reset, but your items stay.
   - Switch the language between Python and JavaScript. The starter script changes too, if you have not edited it.
   - Cause an error. For example type `harvest(` (a syntax error), or `mvoe(EAST)` (a name that is not defined, found when the line runs). The line turns red and the message shows the line number.
   - Harvest clover with the starter script and press Run several times. Watch the Items label grow. Items stay between runs and after Stop.
   - Buy `for loop` for 10 clover, then write a `for` loop and run it.
   - Buy `Bigger field` for 30 clover. The field becomes 5x5 at once.
   - Before you buy `Radish and plant()`, write `plant(RADISH)`. The error `'plant' is not unlocked yet` shows with the line number, and the line turns red. The drone does not move.
   - Buy `Radish and plant()` for 40 clover, and plant a radish.
   - After buying `for loop`, write a loop that moves the drone 10 times and run it.
   - Later, once you have radish to buy `if / else and ripe()` and `while loop`, write a JavaScript loop that never ends: `while (ripe()) {}`. Press Run, watch the tick counter, then press Stop. Press Step on it too: it stops after a fixed number of ticks.

8. What to report:
   - Is the speed too fast or too slow? A move should take about one second.
   - Is anything hard to read or ugly?
   - Is anything confusing?
   - Did anything crash, or did Godot print an error?
   - Does the 3x3 starting field look right?
   - Are the prices fair? How many minutes did it take to reach the radish unlock?
   - Is the unlock list clear? Did you know what to do next?

## Known limits

- Progress is lost when the game closes.
- JavaScript is not an unlock yet: both languages are open from the start.
- There is no speed slider.
- The art is placeholder: coloured circles for crops and a blue square for the drone.

## Troubleshooting

- **Godot cannot find the .NET SDK:** install the SDK from step 1, then restart Godot.
- **NU1201 error when building:** a `net8.0` line was added to `src/CodingGame.Godot/CodingGame.Godot.csproj`. Remove the `net8.0` line. The `net10.0` line must stay.
- **Empty window and a `GameRulesException` in Godot's output:** the `data` folder was not copied. Run `dotnet build src/CodingGame.Godot` in the code folder, then restart the game.
