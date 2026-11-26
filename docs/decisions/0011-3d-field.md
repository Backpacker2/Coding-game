# 0011: The 3D field

- **Status:** accepted (built in 0.9.0)
- **Date:** 2026-11-26

## Context

The owner's look for the screen is in [`visual-direction.md`](../visual-direction.md): the field is a 3D low-poly field, seen from an angle, with visible crops that grow and a drone hovering over it. The field was drawn in 2D by `FarmView`, as flat squares and circles. The requirements are:

1. **The look of the owner's direction.** Tiles with depth, light and shadow, crops that show their growth, and a drone that hovers.
2. **Original art only.** Everything is built in code from Godot primitive meshes. No imported models, textures or other assets, because that would be a new asset dependency, and a dependency needs the owner's agreement ([`CLAUDE.md`](../../CLAUDE.md), Git: dependencies).
3. **Rendering only reads.** The view never changes the game state ([`0007`](0007-godot-view.md)).
4. **Determinism stays.** The simulation runs in fixed ticks ([`CLAUDE.md`](../../CLAUDE.md), Game-specific). The view may animate, but the animation must not feed back into the game.

## Option A: a `FieldView3D` control with its own 3D world, built from primitive meshes (chosen)

- `FieldView3D` is a `SubViewportContainer`. It holds a `SubViewport` with its own 3D world: a camera, a directional light with shadows and a `WorldEnvironment`. The viewport is transparent, so the sky colour of `Main` shows behind the field.
- The field is built from primitive meshes: a `BoxMesh` for each tile and for the base, and for the crops and the drone a few `SphereMesh`, `CylinderMesh`, `PrismMesh` and `BoxMesh` parts. The tiles are rebuilt only when the size of the field changes.
- `ShowField(Field, IReadOnlyList<Crop>)` is called every frame by `Main`. It copies the tiles, the crops and the drone position to the scene and changes nothing in the field.
- The drone's hover and the spin of its rotors are view-only animation, driven by real time in `_Process`, not by ticks. They never touch the `Field`, so the simulation stays deterministic.

**For:** real depth, light and shadow with no hand-made projection; the scene can be animated in 3D; the whole look is in one control with a small public interface; no asset pipeline.

**Against:** a 3D scene is more to set up than a `_Draw` method, and the `gl_compatibility` renderer has to stay (it supports the directional light with shadows, see Consequences).

## Option B: fake isometric drawing in `_Draw` (rejected)

- Keep the 2D `FarmView` and project each tile, crop and drone by hand into an isometric view with `DrawPolygon`.

**For:** no 3D scene, and the code stays in the existing 2D control.

**Against:** no light, no shadows and no depth sorting, so every shape has to be ordered and shaded by hand. Each new shape is more projection code. A hover or a spinning rotor needs its own animation maths, while a 3D node only needs a transform. The look would still be flat.

## Option C: imported low-poly models in glTF (rejected for now)

- Use low-poly models made in a 3D tool, imported as glTF files.

**For:** nicer art, and an artist can change it without code.

**Against:** a new asset pipeline and dependency, which needs the owner's agreement. The models would also be a second source of the look that has to match the crop data. It can still come later: the primitive meshes can be replaced by models behind the same `FieldView3D` interface, without changing `Main`.

## Decision

Option A. `FieldView3D` replaces `FarmView`. The crop colours move to `FieldView3D.CropColor`, which the item bar uses.

## Consequences

- The `gl_compatibility` renderer stays. It supports a directional light with shadows, so the look does not need the Forward+ renderer.
- `FarmView` is removed. Its public members are kept in `FieldView3D` with the same shape: `ShowField` and `CropColor`.
- The placeholder art is primitive shapes: a tile is a box, a crop is a few spheres, a sphere with leaves or a bush, and the drone is a disc with four rotors. A ripe crop glows a little.
- The field is seen from a fixed angle. The camera is aimed at the centre of the field each time its size changes.
- The view has no automated tests, as before (decision 0007). The build checks the code; the look is for the owner's play-test ([`playtest.md`](../playtest.md)).
