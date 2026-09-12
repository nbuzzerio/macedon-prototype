using System;
using UnityEngine;

namespace Macedon.TerrainTools.Editor
{
    public readonly struct TerrainHeightOffsetAnalysis
    {
        public TerrainHeightOffsetAnalysis(
            float verticalRangeMeters,
            float offsetMeters,
            float normalizedDelta,
            float minimumNormalizedHeight,
            float maximumNormalizedHeight)
        {
            VerticalRangeMeters = verticalRangeMeters;
            OffsetMeters = offsetMeters;
            NormalizedDelta = normalizedDelta;
            MinimumNormalizedHeight = minimumNormalizedHeight;
            MaximumNormalizedHeight = maximumNormalizedHeight;
        }

        public float VerticalRangeMeters { get; }
        public float OffsetMeters { get; }
        public float NormalizedDelta { get; }
        public float MinimumNormalizedHeight { get; }
        public float MaximumNormalizedHeight { get; }
        public float ResultingMinimumNormalizedHeight => MinimumNormalizedHeight + NormalizedDelta;
        public float ResultingMaximumNormalizedHeight => MaximumNormalizedHeight + NormalizedDelta;
        public float MinimumElevationMeters => MinimumNormalizedHeight * VerticalRangeMeters;
        public float MaximumElevationMeters => MaximumNormalizedHeight * VerticalRangeMeters;
        public float ResultingMinimumElevationMeters => ResultingMinimumNormalizedHeight * VerticalRangeMeters;
        public float ResultingMaximumElevationMeters => ResultingMaximumNormalizedHeight * VerticalRangeMeters;
        public float MaximumSafeNegativeOffsetMeters => -MinimumElevationMeters;
        public float MaximumSafePositiveOffsetMeters => VerticalRangeMeters - MaximumElevationMeters;
        public float DownwardHeadroomMeters => ResultingMinimumElevationMeters;
        public float UpwardHeadroomMeters => VerticalRangeMeters - ResultingMaximumElevationMeters;
        public bool IsZeroOffset => OffsetMeters == 0f;
        public bool IsValid =>
            ResultingMinimumNormalizedHeight >= 0f && ResultingMaximumNormalizedHeight <= 1f;
    }

    public static class TerrainHeightOffset
    {
        public static TerrainHeightOffsetAnalysis Analyze(TerrainData terrainData, float offsetMeters)
        {
            if (terrainData == null) throw new ArgumentNullException(nameof(terrainData));
            if (!IsFinite(offsetMeters)) throw new ArgumentOutOfRangeException(nameof(offsetMeters), "Offset must be finite.");

            float verticalRange = terrainData.size.y;
            if (!IsFinite(verticalRange) || verticalRange <= 0f)
                throw new ArgumentOutOfRangeException(nameof(terrainData), "TerrainData vertical size must be finite and greater than zero.");

            int resolution = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
            FindRange(heights, out float minimum, out float maximum);
            return new TerrainHeightOffsetAnalysis(
                verticalRange, offsetMeters, offsetMeters / verticalRange, minimum, maximum);
        }

        public static void AddUniformOffset(float[,] heights, float normalizedDelta)
        {
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            if (!IsFinite(normalizedDelta))
                throw new ArgumentOutOfRangeException(nameof(normalizedDelta), "Normalized offset must be finite.");

            int rows = heights.GetLength(0);
            int columns = heights.GetLength(1);
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    heights[y, x] += normalizedDelta;
        }

        public static void Apply(Terrain terrain, TerrainHeightOffsetAnalysis analysis)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            TerrainData terrainData = terrain.terrainData;
            if (terrainData == null) throw new ArgumentException("Terrain must have TerrainData.", nameof(terrain));
            if (!analysis.IsValid) throw new InvalidOperationException("The requested offset exceeds the normalized height range.");
            if (analysis.IsZeroOffset) return;

            int resolution = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
            AddUniformOffset(heights, analysis.NormalizedDelta);
            terrainData.SetHeights(0, 0, heights);

            Vector3 position = terrain.transform.position;
            position.y -= analysis.OffsetMeters;
            terrain.transform.position = position;
        }

        private static void FindRange(float[,] heights, out float minimum, out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            foreach (float height in heights)
            {
                minimum = Mathf.Min(minimum, height);
                maximum = Mathf.Max(maximum, height);
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
