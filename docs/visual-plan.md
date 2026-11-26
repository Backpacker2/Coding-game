# Visual plan

How the Coding Game should look and feel, and the steps to get there. It builds on the owner's direction in [`visual-direction.md`](visual-direction.md) (milestones 18 to 21, done) and turns it into milestones 22 to 28 in [`TODO.md`](TODO.md).

## Where we are (0.9.1)

The screenshot of 0.9.0 (`screenshots/v0.9.0.png` in the project files) shows the structure is right: a 3D field, a hovering drone, a floating script window, round buttons. What still makes it look like a prototype:

- **Flat background.** One blue-grey colour behind the field. The field floats in nothing.
- **Raw colours.** Pure `Colors.Gold`, `Red` and `Orange` crops, a fully saturated green, no tone mapping. Clover is drawn as three yellow discs, so it does not read as clover.
- **Hard, heavy shadows** and a dull sun, so the tiles look like plastic blocks.
- **Default editor UI.** The script window, buttons and dropdowns are Godot's dark editor theme. Next to a bright farm it looks like a tool, not a game.
- **Growth is only scale.** A seedling is a small ripe crop.
- **The code and the world are not linked.** The running line is a faint yellow bar; nothing on the field shows which command is running.

## What the research says

Sources are listed at the end. The lessons that apply to a small, primitive-built, low-poly farm:

### Style

1. **Silhouette and proportion beat detail.** Low-poly reads well when each object has a clear, chunky shape. Small detail does not show from the camera distance. So every crop needs a shape you can recognise in grey, not only a colour.
2. **Earthy, slightly muted colours, with saturated accents.** Ground and grass in analogous, calm hues; crops, the drone's light and the UI's action buttons are the few saturated colours, so the eye goes to what matters.
3. **One palette, in one place.** Every colour comes from one palette file, so the look is decided once and changes once.
4. **Flat-shaded faces with one sun.** Low-poly looks best with one directional light, soft-enough shadows and a coloured ambient (warm sun, cool shade). Lighting gives depth first; fog gives distance second.
5. **Distance fog and a sky gradient** place the field in a world. The field should sit on a small island of land, not float in a colour.
6. **Hide popping.** Things appear and disappear with a short scale tween, never in one frame.

### Feel ("juice")

7. **The more common an action, the smaller its effect.** A move gets a glide and a tilt; a plant a puff; a harvest a burst and a "+N"; an unlock bought or a first ripe potato a bigger moment. An error gets a red flash and a small shake, never more.
8. **Ease-out for things that arrive, ease-in for things that leave.** Short durations: 0.1 to 0.3 seconds for UI, a bounce (`Tween.TransitionType.Back`) for pop-ins.
9. **Ship at about 60% of the juice that feels like enough.** This is a learning game: the player must still read the field and their code.

### Readability for learners

10. **The running line and the drone must be visibly linked.** Programming games that teach (step-through debuggers, visual-programming puzzles) show the current instruction and its effect at the same moment. Here: a strong highlight on the running line, an arrow in the gutter, and the command that is running shown as a small label over the drone.
11. **State must be visible without reading numbers.** A crop's growth stage shows in its shape (sprout, young, ripe). Ripe crops are clearly different: brighter and slightly swaying.
12. **Errors are gameplay.** The error line goes red, the drone flashes and shakes once, and the message box is easy to see. Never a crash-like popup.
13. **Shapes as well as colours.** About one in twelve men has a colour-vision deficiency. Each crop has its own shape, and the resource icons are the crop's shape, not only a coloured dot.
14. **Big, calm UI.** Rounded panels, a readable font size (16 px body, 20 px for counts), high contrast in the editor, and a few clear button states (normal, hover, pressed, disabled).

### Godot techniques (compatibility renderer)

The project uses `gl_compatibility`, so the plan avoids features that need Forward+ (SSAO, SSIL, SDFGI, volumetric fog). It uses:

- `WorldEnvironment` with a `ProceduralSkyMaterial` sky, a filmic tone map, depth fog and a gentle glow for ripe crops and the drone's light. Each setting is checked in the build; any the compatibility renderer ignores is dropped, not worked around.
- `DirectionalLight3D` with shadows, a warm colour and a cool ambient light.
- `StandardMaterial3D` with flat colours from the palette, and per-tile variation from a hash of the tile position (no randomness, so the view stays the same every time).
- `CpuParticles3D` for bursts (already used), `Tween` for every motion, `Label3D` for text in the world.
- One `Theme` resource built in code, set on the root `Main` control and on the windows, so every control is styled in one place instead of with overrides scattered per control.

## Rules for every visual milestone

- Rendering only reads the game state; animation never feeds back into the simulation.
- Art only from Godot primitives, built in code. No imported models, textures, fonts or sounds until the owner agrees to an asset dependency.
- Colours, sizes and durations of the look live in the view project (`Palette`), not in `data/`: they are not game rules.
- No randomness without a fixed seed; per-tile variation comes from the tile's position.
- Every milestone ends with a screenshot in the pull request, so the owner sees the change.

## Milestones

Each milestone is one pull request and one version.

### 22. Palette, sky and light

- A `Palette` class holds every colour of the view: ground, grass, soil, crops, drone, UI, sky. All other view code uses it.
- Calmer crop colours, and clover drawn as green three-leaf clover instead of yellow discs.
- Sky: a procedural sky gradient behind the field (replaces the flat background colour), depth fog, filmic tone mapping.
- Light: a warm sun at a lower angle, softer shadows, a cool ambient light.
- Fix the zero-delta case in the drone glide (a zero frame time gives a NaN tilt).

### 23. The field in its world

- The field sits on an island: a grass ground plane around it with a soil edge, a few low-poly trees and rocks around the border, placed by position hash.
- Tiles get a top layer and sides in different colours (grass on soil), a small bevel feel from a slightly smaller top box, and a little colour variation per tile.
- Tilled soil shows furrows (thin boxes).
- A tile under the drone gets a soft highlight ring, so the player sees where the drone works.

### 24. Crops that grow

- Each crop has three stages with their own shapes: sprout, young plant, ripe. The stage follows the crop's age, as today's scale does.
- Clover: low leaves that spread. Radish: leaves above a red root that shows when ripe. Potato: a bushy plant with flowers when ripe.
- Ripe crops sway gently and glow a little; new crops pop in with a short scale tween.

### 25. Game UI theme

- One `GameTheme` builds a Godot `Theme` in code: warm dark panels with rounded corners and a soft shadow (dark, not light, because the syntax colours and the overlays on the field are made for a dark background), a darker high-contrast editor inside, styled buttons with clear states, dropdowns, scroll bars and window title bars.
- Run is green, Stop is red, Step is neutral, with drawn icons (triangle, square, step arrow).
- The round top-right buttons get drawn icons with the text as a tooltip.
- The item bar pills use the theme, show the crop's shape as their icon, and pulse when a count rises.

### 26. Code and world linked

- The running line gets a strong highlight and an arrow in the gutter; an error line goes red.
- The running command shows over the drone as a small label (for example `move(EAST)`) while it runs.
- An error flashes the drone red and gives one small shake.
- A run status chip in the script window: Ready, Running, Finished or Error, each with its colour.

### 27. Camera

- The camera can orbit (right-drag), zoom (mouse wheel) and reset (a button), with smooth motion and limits so the field never leaves the screen.
- The default view stays the fixed angle the owner liked.

### 28. Polish and check

- A pass over all motion: durations, easing, the "60%" rule.
- Readability check with a colour-blindness simulation of a screenshot, and font sizes at 1280 × 720.
- Before and after screenshots of 0.9.1 and the result in `docs/`, and `visual-direction.md` updated.

Result (0.14.7):

- Motion: the drone eases between tiles, crops pop in with a 0.25 s overshoot, ripe crops sway gently, an error shakes the drone for 0.4 s, and the camera eases. Two motions were linear and now ease: the "+N" number rises fast and slows down before it fades, and the unlock banner fades out with ease-in. Nothing else was added, to stay at about 60% of the juice.
- Colour blindness: a deuteranopia and a protanopia simulation of a screenshot show that the three crops stay apart by shape (leaves, bulb, tubers), also on the item pills. Run and Stop also differ by their symbol and text, and the status chip by its word.
- 1280 × 720: the script window's toolbar cut off the status chip. The toolbar now wraps onto a second row. The 16 px UI font stays readable.
- Before and after: `docs/images/visual-before-after.jpg` (0.9.3 and 0.14.6), linked from `visual-direction.md`.

## Research: real 3D models

The owner asked to look into ready-made 3D models, for example animated drones on Sketchfab. Findings (checked 2026-11-26):

### Sources and licences

| Source | Licence | Fit for this public repository |
| --- | --- | --- |
| Kenney (kenney.nl) | CC0, no credit required | Yes. Low-poly kits in one consistent style. |
| Quaternius (quaternius.com) | Quaternius Asset License: free use in games, no credit, but the assets may not be redistributed as assets | Doubtful. A public repository hands out the raw model files, which may count as redistribution. Ask the author or keep them out of the repository. |
| Sketchfab | Per model. Many downloadable models are CC BY (credit the author), some CC BY-NC or BY-SA, and some are not downloadable | Only CC0 or CC BY, with the author credited. Not NC (non-commercial) or ND, and nothing marked as not for AI use. |
| Poly Pizza | Mix of CC0 and CC BY | Same rule as Sketchfab. |

Downloading from Sketchfab needs an account, so the owner downloads a chosen model; Claude cannot.

### The drone the owner shared

"Dragon Flycore – Robot Dragonfly Drone, Animated" by robotanimator on Sketchfab: CC BY 4.0 (credit required, commercial use allowed), downloadable, with 7 animations. It has about 720,000 triangles and realistic PBR materials. That is roughly a thousand times our whole drone and far from the flat low-poly look, so it would need heavy simplification and would clash with the field. Not a good fit as it is.

Better fits found in the same search, all CC BY and downloadable: "Military Drone Low-Poly" (344 triangles, 2 animations), "Gungnir Class Riot Drone" (263 triangles, 1 animation), "Sci-Fi Camera Drone" (about 5,400 triangles, 1 animation). Their style still needs checking next to the field.

### How models would come in

- Format: glTF 2.0 binary (`.glb`), which Godot imports directly, with its animations (for example spinning rotors) as an `AnimationPlayer`.
- Budget: a few thousand triangles for the drone, a few hundred for a crop or tree, so a 10×10 field stays fast on the compatibility renderer.
- Style: flat colours, no realistic textures, so models match the primitive field. Recolour them to the `Palette` where needed.
- Where: `src/CodingGame.Godot/models/`, with `models/CREDITS.md` listing each model, its author, source link and licence.
- Code: `FieldView3D` keeps its interface. A model replaces a primitive shape behind it (decision 0011, option C), one shape at a time, starting with the drone.

### Decision and what was built

The owner agreed on 2026-11-26 to CC0 and CC BY models with a credits file. In 0.15.1 the island's trees and rocks, every crop's sprout, the radish and the potato's bush became Kenney Nature Kit models (CC0), recoloured to the palette in code. Design: [`decisions/0015-imported-models.md`](decisions/0015-imported-models.md). Credits: `src/CodingGame.Godot/models/CREDITS.md`. The clover keeps its primitive shape because the kit has none. The drone stays primitive until the owner picks a low-poly CC BY drone.

## Later, needs the owner

- **Sound.** Harvest pops, a soft drone hum and UI clicks make a big difference to feel. Generated sounds are possible without assets, but real sound effects are an asset dependency.
- **A custom font.** A new asset, so it waits for the owner.
- **The drone model.** A low-poly CC BY drone, picked with the owner (Sketchfab needs his account to download).
- **Several code windows.** Open question from the owner; it is a gameplay change, not only a visual one.

## Sources

- Sketchfab model data for the shared drone and a search for downloadable low-poly drones, through Sketchfab's public API. <https://sketchfab.com/3d-models/dragon-flycore-robot-dragonfly-drone-animated-1e8cd639f2914163bb77cdc952ab4937>
- Quaternius Asset License. <https://quaternius.com/license.html>
- Kenney asset licence (CC0). <https://kenney.nl/assets>

- Shedworks on the readability of Sable's art: lighting, shadows, distance fog, fading outlines. <https://www.gamedeveloper.com/marketing/how-shedworks-refined-the-art-of-sable-in-pursuit-of-readability>
- "In love with polygons": vertex-coloured flat low-poly with one hard-shadowed sun. <https://gamedeveloper.com/design/in-love-with-polygons>
- "Implementing low-poly style in game dev": silhouette over detail, earthy pastels with complementary accents. <https://itch.io/blog/1127405/blog-implementing-low-poly-style-in-game-dev>
- GameAnalytics, "Squeezing more juice out of your game design": simple juice for common actions, ease-out for entrances. <https://www.gameanalytics.com/blog/squeezing-more-juice-out-of-your-game-design>
- Godot tweens and feedback lesson: tween durations, transition types, ship at 60% juice. <https://bjamba.github.io/teach-me-engineering/learn-godot/module-06-juice-polish/lesson-01-tweens-feedback.html>
- Godot documentation: environment and post-processing, GUI skinning and `StyleBoxFlat`. <https://docs.godotengine.org/en/stable/tutorials/3d/environment_and_post_processing.html>, <https://docs.godotengine.org/en/stable/tutorials/ui/gui_skinning.html>
- PCGamesN on the farm-automation coding genre: the satisfaction is watching your code run the farm. <https://www.pcgamesn.com/the-farmer-was-replaced/out-now-learn-to-code>
