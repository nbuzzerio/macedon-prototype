# Custom Editor Tools

This document records project-owned Unity production tools, their authoring contracts, and planned extensions. These tools support repeatable scene construction; they are not gameplay systems.

## Palisade Arc Builder

### Purpose

`PalisadeArcBuilder` lays out repeated palisade prefab modules along a circular arc between two scene transforms. It removes the error-prone work of manually positioning and rotating many fixed-width wall pieces while keeping the result as ordinary prefab instances that can be inspected in the scene.

The component is available through **Add Component > World > Palisade Arc Builder**. Its generation implementation and custom Inspector are editor-only, so it does not generate or rebuild geometry during play.

### Controls

- **Palisade Prefab**: the prefab asset root instantiated for every module. The prefab's local X axis is treated as its width axis.
- **Start / End**: scene transforms whose positions define the centers of the first and last modules. Only their positions are used; their rotations and scales do not define the arc.
- **Arc Bulge**: midpoint offset in meters. Positive values bow toward `Cross(Up, End - Start)` and negative values bow in the opposite direction. Zero produces a straight run.
- **Segment Width**: expected module width along prefab-local X. It determines module count; it does not rescale the prefab.
- **Generated Root**: read-only ownership reference shown by the custom Inspector.
- **Generate / Rebuild Arc**: replaces the tracked modules with a newly evaluated run.
- **Clear Generated**: removes tracked modules without deleting the generated root.

When selected, the builder draws a cyan gizmo preview and endpoint markers. The Inspector also reports the proposed module count or a validation warning.

### Generated hierarchy

The first build creates and tracks this hierarchy:

```text
Builder GameObject
`-- Palisade Arc - Generated
    |-- Palisade prefab instance
    |-- Palisade prefab instance
    `-- ...
```

The generated root is parented directly beneath the builder with identity local position, rotation, and scale. Every generated module is a prefab instance parented directly beneath that root and is also recorded in the builder's serialized tracking list.

### Space and placement behavior

The existing Arc Builder evaluates the Start and End positions in **world space**, projects their chord onto the world XZ plane, and places every module root at Start's world-space Y. It aligns each module's local X axis to the evaluated horizontal arc tangent. Start and End represent module centers, not outside edges.

Although placement is calculated in world space at rebuild time, the generated hierarchy is parented beneath the builder. After generation, moving or rotating the builder therefore moves or rotates the entire generated arc as one set. Rebuilding reevaluates the current world positions of Start and End; it does not preserve an earlier local-space layout independently of those transforms.

The builder, all of its ancestors, and the generated root must have unit scale. This prevents scaled module widths and transform shear. A future tool should distinguish this behavior from its own explicit local-space contract.

### Prefab usage

The assigned object must be a top-level prefab asset selected from the Project window. Scene objects, nested prefab children, and non-prefab objects are rejected. Instances retain their prefab connection, are placed at unit scale, and have their transform overrides recorded.

The inspected greybox module is:

`Assets/Custom_Assets/Environment/Medieval_Kit/Prefabs/Greybox/Palisade_Slot_3m.prefab`

It is a 3 m-wide placeholder built from a scaled cube child with a box collider. `Assets/Custom_Assets/Environment/Medieval_Kit/Prefabs/Wall_01.prefab` is an existing finished wall prefab in the same kit, but the Arc Builder does not contain an automatic greybox-to-production replacement mapping.

### Undo, rebuild, clear, and ownership safety

Generate/rebuild and clear each run as one named Unity Undo group. If generation throws an exception, the operation is reverted to the beginning of that group and the exception is logged.

Rebuild destroys all tracked module instances, retains an existing valid generated root, and creates a complete replacement set. Clear destroys the tracked modules and empties the tracking list, but retains the generated root for later reuse. Both operations are disabled in Play Mode.

Before destructive work, the custom Inspector verifies ownership. It refuses to clear or rebuild if the generated root was moved away from the builder, a tracked module was moved away from the generated root, or scene content/non-source prefab children were added inside a generated module. Move such content outside the generated modules before rebuilding. This safeguard prevents an automated rebuild from silently deleting manual scene work.

### Known limitations

- Supports one horizontal circular arc or straight run only; endpoints' vertical difference, rotation, and scale are ignored.
- All module roots use Start's Y; there is no terrain conforming or per-module elevation.
- Module count is rounded to the closest practical interval count. The prefab is never stretched to close a span exactly.
- Rejects builds at 1,000 or more intervals, degenerate endpoints, non-finite values, invalid prefab assignments, and non-unit relevant hierarchy scale.
- Assumes the prefab's physical width matches **Segment Width** and lies along local X; that contract is configured manually rather than stored as asset metadata.
- Rebuild replaces generated instances. Safe manual additions inside them are intentionally not preserved; detected additions block the operation.
- Generates only one prefab type and has no corners, gates, doors, state, variation, production replacement, serialization format, or multi-floor semantics.
- The current `Palisade_Slot_3m` greybox name predates the planned naming convention documented below.

### Intended production workflow

1. Place a builder and separate Start and End transforms in a scene or Prefab Mode.
2. Assign a correctly oriented, fixed-width prefab asset and enter its local-X module width.
3. Position the endpoints, adjust bulge, and inspect the gizmo and proposed module count.
4. Generate explicitly, then move the builder as a single set if needed.
5. Keep authored scene content outside generated module instances. Rebuild whenever the controlling values change.
6. Use Unity Undo for accidental generation, rebuild, or clear operations.

The Arc Builder is suitable for a focused repeated-module task. It is not the source-format architecture for the planned Grid Greybox Builder.

## Layout Greybox Pipeline

The pipeline contract is independent of its authoring application:

```text
External authoring source
`-- MACEDON Layout JSON
    `-- Unity editor reader / validator / greybox generator
        `-- Movable generated structure root
```

A future Layout Painter may be a separate Vite and TypeScript application in its own repository. No web editor is maintained in this Unity repository. Mock JSON can be written or generated externally now.

### Layout Format V1

The canonical source is versioned, human-readable and AI-readable JSON. Top-level fields are `formatVersion`, `name`, `units`, `grid`, `anchor`, `defaultPlacementMode`, `deterministicSeed`, `paths`, and `overlays`.

Each V1 path is one explicit straight run with a unique ID, an asset-contract ID, and exactly two finite local X/Z points. Its optional `placementMode`, `state`, and `modifiers` remain separate from structural identity. Only `SEGMENT` contracts are legal in `paths`. Overlays are reserved in the format but are not generated in V1.

Continuous architecture is vector geometry, not raster occupancy. The measurement grid defaults to 1 meter but is not the underlying geometry and does not restrict arbitrary angles.

The canonical coordinate convention is:

- +X is right.
- +Z is forward, shown as up in top-down authoring.
- Y is elevation.
- 0 degrees is +X and 90 degrees is +Z.

All coordinates are local to the layout. The Unity generated root receives the anchor position. Because positive layout rotation turns +X toward +Z, Unity applies the opposite signed Y Euler rotation to that root. Moving or rotating the builder afterward moves the generated layout as one "LEGO set."

### Unity Layout Greybox Builder

`LayoutGreyboxBuilder` is an editor authoring component available through **Add Component > World > Layout Greybox Builder**. Add it to the intended movable structure object, such as `Fort_Courtyard_Greybox`, assign a Layout V1 JSON `TextAsset`, and assign the existing `Palisade_Slot_3m` prefab to Asset Contract ID 1.

The custom Inspector provides **Validate Layout**, **Generate / Rebuild**, and **Clear Generated**. Generated prefab instances are organized beneath `Layout Greybox - Generated`, with one `Path_<id>` child per source path. Generation is local-space and never runs during play.

Foundation-Level generation places every module at local Y zero beneath the generated root; the root itself receives the anchor's local Y. Terrain-Conforming files are parsed and contract-validated, but generation is explicitly blocked until correct terrain sampling is implemented.

The initial Unity-side registry is intentionally small and code-defined:

- ID 0: `Empty`, permanently reserved.
- ID 1: `Palisade_Wall_3m`, `SEGMENT`, family `Palisade_Wall`, dimensions 3 m x 4 m x 0.4 m, terrain conforming allowed, mapped through the component's prefab field to the existing `Palisade_Slot_3m` asset.

The existing prefab predates `Greybox_[ProductionAssetName]` naming and is not renamed. Future catalog work may externalize the registry, but IDs must never be repurposed.

### Validation and modular generation

Unity validates format version, meter units, grid, anchor, seed range, placement modes, unique path IDs, known contracts, `SEGMENT` path roles, finite two-point geometry, terrain permission, and modular fit. Comparison uses one-millimeter precision.

Available widths from the same structural family determine exact constructibility. Invalid runs report the nearest lower and higher legal lengths and cannot generate. The tool never stretches, merges, shortens, lengthens, or approximates modules. A valid run is filled endpoint-to-endpoint with prefab instances whose local X axes follow the run direction.

Rebuild and clear use Unity Undo. Before deleting anything, ownership checks require the generated root, path roots, and module instances to remain in their tracked hierarchy and reject untracked scene content inside it.

### Current sample

`Assets/Custom_Assets/Environment/Medieval_Kit/Layouts/Fort_Courtyard_Mock_V1.json` is a realistic acceptance layout containing a four-sided central courtyard and three surrounding rectangular structures. Every run is divisible by 3 meters; the layout is data, not hardcoded generator behavior.

### Deferred decisions and features

- Correct Terrain-Conforming sampling behavior.
- Gates, corners, junctions, endcaps, and overlays.
- Multi-segment paths and multiple floors.
- A durable external asset catalog and ID-governance workflow.
- Production replacement and imported-prefab bounds validation.
- Presentation variation using the deterministic seed.
- Manual override preservation across rebuilds.
- Any separate authoring application.
