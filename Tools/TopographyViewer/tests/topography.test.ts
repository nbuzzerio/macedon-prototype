import { describe, expect, it } from 'vitest';
import { calculateSlopeDegrees, contourLevels, crossesContour, decodePayload, fitToView, localElevation, parseMetadata, sampleIndex, sampleToLocal, worldElevation, type TopographyMetadata } from '../src/topography';

const metadata: TopographyMetadata = {
  formatVersion: 1, terrainName: 'Test', terrainDataAssetPath: '', worldOrigin: { x: 100, y: -20, z: 200 },
  sizeMeters: { x: 2, y: 100, z: 2 }, heightmapResolution: 3,
  minimumNormalizedHeight: 0, maximumNormalizedHeight: 1, minimumLocalElevationMeters: 0,
  maximumLocalElevationMeters: 100, minimumWorldElevation: -20, maximumWorldElevation: 80,
  heightData: { file: 'test.height.raw', encoding: 'uint16-little-endian-normalized', width: 3, height: 3, rowOrder: 'local-z-ascending', columnOrder: 'local-x-ascending', byteLength: 18 },
  coordinateConvention: '+X right, +Z up',
};

describe('topography data', () => {
  it('validates and decodes little-endian samples', () => {
    expect(parseMetadata(metadata).terrainName).toBe('Test');
    const bytes = new Uint8Array(18); bytes[2] = 0xff; bytes[3] = 0xff;
    expect(decodePayload(metadata, bytes.buffer)[1]).toBe(1);
  });
  it('maps x across columns and ascending z across rows', () => {
    expect(sampleIndex(1, 2, 3)).toBe(7);
    expect(sampleToLocal(1, 2, metadata)).toEqual({ x: 1, z: 2 });
  });
  it('converts local and world elevations', () => {
    expect(localElevation(0.5, metadata)).toBe(50);
    expect(worldElevation(50, metadata)).toBe(30);
  });
  it('calculates gradient slope in meters', () => {
    const data = new Float32Array([0, .01, .02, 0, .01, .02, 0, .01, .02]);
    expect(calculateSlopeDegrees(data, metadata, 1, 1)).toBeCloseTo(45, 5);
  });
  it('selects deterministic contour levels and crossings', () => {
    expect(contourLevels(-3, 12, 5)).toEqual([0, 5, 10]);
    expect(crossesContour(4, 5, 5)).toBe(true); expect(crossesContour(5, 6, 5)).toBe(false);
  });
  it('fits rectangular terrain without changing map orientation', () => {
    expect(fitToView(1000, 600, 1000, 1000, 50)).toEqual({ scale: .5, offsetX: 250, offsetY: 50 });
  });
});
