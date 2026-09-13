# MACEDON Tooling Roadmap

This is a living roadmap for production tools. Difficulty is approximate: **1** tiny, **2** small, **3** medium, **4** substantial, **5** major or multi-session.

> Tooling serves the playable game roadmap; it must not become the project itself. Build or extend a tool when it removes demonstrated production friction for the current slice or creates a durable foundation for near-term world work.

## DONE

| Item | Difficulty | Current result |
| --- | ---: | --- |
| Layout Greybox / LEGO Builder V1 | 4 | Versioned local-space layout format, validation, exact modular generation, ownership safety, and Unity Undo. |
| Layout scale guard | 2 | Protects layout generation from unsupported hierarchy scaling. |
| Navigation rebuild/validation tool | 3 | Rebuilds persistent scene NavMesh data and reports relevant agent placement. |
| Terrain Height Offset tool | 3 | Creates sculpting headroom while preserving world surface elevation, with validation and Undo. |
| Terrain Topography Exporter | 3 | Exports full-resolution, versioned metadata plus compact deterministic 16-bit height payload. |
| Terrain Topography Viewer V1 | 4 | Read-only Vite/TypeScript map with elevation color, contours, hillshade, slope, coordinates, pan, and zoom. |

## CURRENT VERTICAL SLICE

| Item | Difficulty | Why now |
| --- | ---: | --- |
| Expand asset contracts for village/fort | 3 | Add only the walls, gates, corners, and other modules actually required to author the Raider slice. |
| Village and fort reusable LEGO/layout sets | 4 | Turn repeated slice structures into movable, rebuildable authored sets. |
| Palisade production replacement workflow | 3 | Reliably replace current fixed-width palisade greyboxes while preserving layout intent. |
| Greybox → production asset replacer | 4 | Map stable contracts to production prefabs without losing transforms, ownership, or validation. |
| House support if houses are created | 3 | Add house contracts/placement only once real house assets and repeated placement needs exist. |
| NPC visual randomization tool, if useful | 2 | Use deterministic, constrained variation only if manual setup becomes a repeated slice bottleneck. |

## NEAR TERM

| Item | Difficulty | Direction |
| --- | ---: | --- |
| Editable terrain patch format / reverse import | 4 | Define a bounded, versioned delta tied to a source topography export. |
| Terrain patch preview/validate/apply | 4 | Validate identity, bounds, ranges, and staleness; preview before applying as one Unity Undo operation. |
| Coordinate/merge strategy for topo and Layout/LEGO viewers | 4 | Share world/local contracts and layers first; merge applications only when it reduces workflow cost. |
| Terrain feature stamps | 5 | Develop explicit, previewable hill, mountain, mountain-range, lake/basin, river/valley, and asymmetric-riverbank operations. |
| Slope-aware texture painting | 4 | Derive deterministic texture rules from elevation, slope, and authored masks. |
| Terrain-aware vegetation rules | 4 | Place vegetation using terrain constraints while preserving hand-authored exclusions and encounter objects. |
| Deterministic natural variation | 3 | Seed controlled variation so regenerated layouts/environment dressing remain stable. |
| Spline-based terrain operations | 5 | Generalize bounded terrain shaping along paths for rivers, valleys, roads, and related features. |

## LATER

| Item | Difficulty | Direction |
| --- | ---: | --- |
| Reusable quest tooling | 5 | Finish the Raider quest first and document repeated authoring/debugging pain points before designing a generalized quest editor. |
| Broader procedural environment tooling | 5 | Add generation only for proven repetitive work, with preview, ownership, validation, and Undo. |
| Advanced terrain round-trip editing | 5 | Layered patches, conflict/staleness handling, compositing, provenance, and richer browser editing after the bounded-patch workflow proves safe. |

## Tooling Gate

Before starting a new tool, identify the playable milestone it unblocks, the repeated manual pain it removes, and the smallest safe version. Prefer explicit data contracts, deterministic output, previews for destructive changes, validation before mutation, and Unity Undo. Defer generalized systems until at least one concrete production workflow has exposed their real requirements.
