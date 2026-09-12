export interface Vec3 { x: number; y: number; z: number }
export interface PayloadDescriptor {
  file: string; encoding: string; width: number; height: number;
  rowOrder: string; columnOrder: string; byteLength: number;
}
export interface TopographyMetadata {
  formatVersion: number; terrainName: string; terrainDataAssetPath: string;
  worldOrigin: Vec3; sizeMeters: Vec3; heightmapResolution: number;
  minimumNormalizedHeight: number; maximumNormalizedHeight: number;
  minimumLocalElevationMeters: number; maximumLocalElevationMeters: number;
  minimumWorldElevation: number; maximumWorldElevation: number;
  heightData: PayloadDescriptor; coordinateConvention: string;
}
export interface Dataset { metadata: TopographyMetadata; normalized: Float32Array }
export interface ViewTransform { scale: number; offsetX: number; offsetY: number }

const finite = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);

export function parseMetadata(input: unknown): TopographyMetadata {
  if (!input || typeof input !== 'object') throw new Error('Metadata must be a JSON object.');
  const m = input as TopographyMetadata;
  if (m.formatVersion !== 1) throw new Error(`Unsupported formatVersion: ${String(m.formatVersion)}`);
  if (!Number.isInteger(m.heightmapResolution) || m.heightmapResolution < 2) throw new Error('Invalid heightmapResolution.');
  if (!m.heightData || m.heightData.encoding !== 'uint16-little-endian-normalized') throw new Error('Unsupported height payload encoding.');
  if (m.heightData.width !== m.heightmapResolution || m.heightData.height !== m.heightmapResolution) throw new Error('Payload dimensions do not match heightmapResolution.');
  if (m.heightData.rowOrder !== 'local-z-ascending' || m.heightData.columnOrder !== 'local-x-ascending') throw new Error('Unsupported coordinate ordering.');
  if (m.heightData.byteLength !== m.heightData.width * m.heightData.height * 2) throw new Error('Invalid payload byteLength.');
  if (!finite(m.worldOrigin?.x) || !finite(m.worldOrigin.y) || !finite(m.worldOrigin.z) || !finite(m.sizeMeters?.x) || !finite(m.sizeMeters.y) || !finite(m.sizeMeters.z) || m.sizeMeters.x <= 0 || m.sizeMeters.y <= 0 || m.sizeMeters.z <= 0) throw new Error('Invalid terrain origin or size.');
  return m;
}

export function decodePayload(metadata: TopographyMetadata, buffer: ArrayBuffer): Float32Array {
  if (buffer.byteLength !== metadata.heightData.byteLength) throw new Error(`Payload has ${buffer.byteLength} bytes; expected ${metadata.heightData.byteLength}.`);
  const view = new DataView(buffer); const result = new Float32Array(buffer.byteLength / 2);
  for (let i = 0; i < result.length; i++) result[i] = view.getUint16(i * 2, true) / 65535;
  return result;
}

export const sampleIndex = (x: number, z: number, width: number): number => z * width + x;
export const localElevation = (normalized: number, metadata: TopographyMetadata): number => normalized * metadata.sizeMeters.y;
export const worldElevation = (local: number, metadata: TopographyMetadata): number => metadata.worldOrigin.y + local;
export const sampleToLocal = (x: number, z: number, metadata: TopographyMetadata): { x: number; z: number } => ({
  x: x / (metadata.heightmapResolution - 1) * metadata.sizeMeters.x,
  z: z / (metadata.heightmapResolution - 1) * metadata.sizeMeters.z,
});

export function calculateSlopeDegrees(data: Float32Array, metadata: TopographyMetadata, x: number, z: number): number {
  const n = metadata.heightmapResolution;
  const x0 = Math.max(0, x - 1), x1 = Math.min(n - 1, x + 1), z0 = Math.max(0, z - 1), z1 = Math.min(n - 1, z + 1);
  const dxMeters = (x1 - x0) * metadata.sizeMeters.x / (n - 1);
  const dzMeters = (z1 - z0) * metadata.sizeMeters.z / (n - 1);
  const dhdx = (data[sampleIndex(x1, z, n)] - data[sampleIndex(x0, z, n)]) * metadata.sizeMeters.y / dxMeters;
  const dhdz = (data[sampleIndex(x, z1, n)] - data[sampleIndex(x, z0, n)]) * metadata.sizeMeters.y / dzMeters;
  return Math.atan(Math.hypot(dhdx, dhdz)) * 180 / Math.PI;
}

export function contourLevels(minimum: number, maximum: number, interval: number): number[] {
  if (!(interval > 0) || !Number.isFinite(interval)) return [];
  const levels: number[] = []; let level = Math.ceil(minimum / interval) * interval;
  if (Object.is(level, -0)) level = 0;
  for (; level <= maximum + interval * 1e-8 && levels.length < 10000; level += interval) levels.push(level);
  return levels;
}

export function crossesContour(a: number, b: number, level: number): boolean {
  return (a < level && b >= level) || (b < level && a >= level);
}

export function fitToView(viewWidth: number, viewHeight: number, terrainWidth: number, terrainHeight: number, padding = 32): ViewTransform {
  const availableWidth = Math.max(1, viewWidth - padding * 2), availableHeight = Math.max(1, viewHeight - padding * 2);
  const scale = Math.min(availableWidth / terrainWidth, availableHeight / terrainHeight);
  return { scale, offsetX: (viewWidth - terrainWidth * scale) / 2, offsetY: (viewHeight - terrainHeight * scale) / 2 };
}
