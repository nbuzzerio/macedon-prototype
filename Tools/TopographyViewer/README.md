# MACEDON Terrain Topography Viewer

Read-only V1 browser viewer for full-resolution Unity Terrain topography exports.

## Run

From `Tools/TopographyViewer`:

```bash
npm install
npm run dev
```

Open the local Vite URL, then select (or drag onto the map) both matching export files: `Terrain.topography.json` and `Terrain.height.raw`. The app runs independently of Unity. Use `npm run build`, `npm run typecheck`, and `npm test` for static validation.

## Controls and capabilities

- Wheel zoom; left or middle drag pans; **Fit terrain** resets the view. Space+drag is also supported.
- Toggle diagnostic elevation color, contours, directional hillshade, and a steep-slope overlay.
- Configure contour interval, major-line cadence, light azimuth, and slope threshold.
- Read local X/Z, world X/Z, local/world elevation, and slope beneath the cursor.
- +X is map-right. Ascending local +Z is map-up. No data rows are silently reordered: screen Y is explicitly projected from `size.z - localZ`.

## Export format V1

The JSON contains version, terrain identity and asset path, world origin, dimensions, heightmap resolution, min/max normalized height, min/max local elevation, min/max world elevation, coordinate-order declarations, and a companion payload descriptor. The RAW file contains one unsigned 16-bit little-endian normalized sample per heightmap point. Samples are row-major: rows increase in local +Z and columns increase in local +X. Decode a value as `uint16 / 65535`; local elevation is `normalized * sizeMeters.y`; world elevation is `worldOrigin.y + local elevation`.

Using two files avoids very large JSON arrays while preserving the complete heightmap with deterministic 16-bit quantization. Keep each JSON and RAW pair together. Browser security prevents the JSON from automatically opening a sibling file, so both must be supplied in one selection/drop.

## Known limitations

- Read-only: no painting, patch creation, terrain stamps, or Unity import.
- One regular square Unity heightmap; no tiled-terrain composition.
- Nearest-sample cursor values and CPU-rendered contours; extremely dense contour intervals can be slow.
- No texture/splatmap, holes, trees, water, structures, or Layout Greybox overlays.
- Export V1 requires an unrotated, unit-scale Terrain transform. Translation, including Transform Y, is supported.
- RAW has no independent header; the matching JSON is required.

## Future round-trip direction (not implemented)

The next architecture should retain this immutable source snapshot, let the browser author a bounded patch/delta in explicit terrain-local coordinates, and return only that patch to Unity:

```text
Unity Terrain → export topography → browser inspect/edit
→ bounded terrain patch/delta → Unity preview/validate/apply → Undo
```

Patch metadata should identify the source export, bounds, sample spacing, intended blend behavior, and before/after validation data. Unity should preview and reject stale or out-of-range patches before registering TerrainData with Undo and applying them. This would target missed riverbed sections, uneven banks, spikes, slope mistakes, and local feature-shape corrections without turning the browser into an unrestricted scene editor.
