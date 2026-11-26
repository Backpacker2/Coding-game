# 0015: Imported 3D models

- **Status:** accepted (built in 0.15.1)
- **Date:** 2026-11-26

## Context

Decision [`0011`](0011-3d-field.md) built the field from Godot primitive meshes and left imported glTF models (its option C) for later, because models are a new asset dependency that needs the owner's agreement. The research in [`visual-plan.md`](../visual-plan.md) (section "Research: real 3D models") compared sources and licences. On 2026-11-26 the owner agreed to CC0 and CC BY models, with a credits file. The requirements are:

1. **Licence fits a public repository.** The model files are in the repository, so anyone can download them. Only licences that allow that: CC0, or CC BY with the author credited.
2. **One look.** The models must match the flat low-poly field and the game's palette.
3. **The view's interface stays.** `FieldView3D` keeps `ShowField`; `Main` does not change ([`0011`](0011-3d-field.md)).
4. **Rendering only reads.** Models are view-only, like the primitives they replace.

## Option A: Kenney Nature Kit models, recoloured to the palette in code (chosen)

- The `.glb` files of the chosen models are copied unchanged into `src/CodingGame.Godot/models/`, with Godot's `.import` files next to them. `models/CREDITS.md` lists every file, its author, source and licence.
- `FieldView3D.Model(name, materialFor)` loads a model and replaces each of its materials by a palette material, picked by the model's own material name (for example `woodBark` becomes `Palette.TreeTrunk`). The palette stays the one place for colours, and a ripe crop's part can still glow.
- Replaced so far: the island's trees and rocks, the sprout of every crop, the radish (young leaves and a ripe turnip-shaped bulb in the radish colour) and the potato's bush. The clover keeps its primitive shape, because the kit has no clover, and the potato keeps its primitive tubers and flowers.

**For:** CC0 needs no credit and has no conditions; one consistent style; small files (under 15 KB each, 16 to 204 triangles); a model can be swapped by changing one file name.

**Against:** the models are about the look only, so a model that does not fit is replaced, not edited. Godot must import the models before the game runs (`godot --headless --import`, which the editor does on its own).

## Option B: models with their own colours (rejected)

- Use the models as they come, with Kenney's teal greens and orange wood.

**For:** no recolouring code.

**Against:** the kit's colours clash with the calmer palette of visual milestone 22, and colours would live in two places: the palette and every model file.

## Option C: Quaternius models (rejected)

- Quaternius has very nice low-poly farm packs.

**Against:** its licence forbids redistributing the assets as assets, and a public repository hands out the files. Not allowed here.

## Decision

Option A. Kenney CC0 models for trees, rocks and crops. The drone stays primitive until the owner picks a low-poly CC BY drone; that model will be credited in `models/CREDITS.md` with its author and licence.

## Consequences

- New models need a row in `models/CREDITS.md` and a licence check: CC0 or CC BY, never NC, ND, SA or "no AI".
- The colours of every model come from `Palette`, so a model never adds a colour of its own.
- Running the game from a fresh clone needs the import step, which the Godot editor does when it opens the project.
