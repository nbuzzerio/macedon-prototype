using Macedon.TerrainTools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Macedon.TerrainTools.Tests
{
    public sealed class TerrainHeightOffsetTests
    {
        private const float TerrainDataReadbackTolerance = 2f / ushort.MaxValue;
        private const float WorldHeightToleranceMeters = TerrainDataReadbackTolerance * 600f;
        private TerrainData terrainData;
        private GameObject terrainGameObject;
        private Terrain terrain;

        [SetUp]
        public void SetUp()
        {
            terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(1000f, 600f, 1000f) };
            terrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
            terrain = terrainGameObject.GetComponent<Terrain>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(terrainGameObject);
            Object.DestroyImmediate(terrainData);
        }

        [Test]
        public void TwentyMetersAcrossSixHundredMetersProducesExpectedNormalizedDelta()
        {
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 20f);
            Assert.That(result.NormalizedDelta, Is.EqualTo(20f / 600f).Within(0.0000001f));
        }

        [Test]
        public void TerrainDataSamplesReceiveUniformOffset()
        {
            SetTwoHeights(0.2f, 0.6f);
            float[,] before = terrainData.GetHeights(0, 0, 2, 1);
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 30f);

            TerrainHeightOffset.Apply(terrain, result);

            float[,] after = terrainData.GetHeights(0, 0, 2, 1);
            Assert.That(after[0, 0], Is.EqualTo(before[0, 0] + 0.05f).Within(TerrainDataReadbackTolerance));
            Assert.That(after[0, 1], Is.EqualTo(before[0, 1] + 0.05f).Within(TerrainDataReadbackTolerance));
        }

        [Test]
        public void UniformOffsetPreservesRelativeHeightDifferences()
        {
            float[,] heights = { { 0.1f, 0.25f }, { 0.4f, 0.8f } };
            float before = heights[1, 1] - heights[0, 0];
            TerrainHeightOffset.AddUniformOffset(heights, 0.1f);
            Assert.That(heights[1, 1] - heights[0, 0], Is.EqualTo(before).Within(0.0000001f));
        }

        [Test]
        public void PositiveOffsetMovesTerrainDownWithoutChangingXOrZ()
        {
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            terrain.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            terrain.transform.localScale = new Vector3(2f, 3f, 4f);
            Quaternion oldRotation = terrain.transform.rotation;
            Vector3 oldScale = terrain.transform.localScale;
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 20f);

            TerrainHeightOffset.Apply(terrain, result);

            Assert.That(terrain.transform.position, Is.EqualTo(new Vector3(12f, 65f, -34f)));
            Assert.That(terrain.transform.rotation, Is.EqualTo(oldRotation));
            Assert.That(terrain.transform.localScale, Is.EqualTo(oldScale));
        }

        [Test]
        public void PositiveOffsetPreservesWorldSpaceSurfaceElevation()
        {
            SetUniformHeight(0.4f);
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            float oldWorldHeight = WorldHeightAtFirstSample();

            TerrainHeightOffset.Apply(terrain, TerrainHeightOffset.Analyze(terrainData, 20f));

            Assert.That(WorldHeightAtFirstSample(), Is.EqualTo(oldWorldHeight).Within(WorldHeightToleranceMeters));
        }

        [Test]
        public void NegativeOffsetLowersSamplesMovesTerrainUpAndPreservesWorldSurface()
        {
            SetUniformHeight(0.5f);
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            float oldNormalizedHeight = FirstHeight();
            float oldWorldHeight = WorldHeightAtFirstSample();

            TerrainHeightOffset.Apply(terrain, TerrainHeightOffset.Analyze(terrainData, -60f));

            Assert.That(FirstHeight(), Is.EqualTo(oldNormalizedHeight - 0.1f).Within(TerrainDataReadbackTolerance));
            Assert.That(terrain.transform.position, Is.EqualTo(new Vector3(12f, 145f, -34f)));
            Assert.That(WorldHeightAtFirstSample(), Is.EqualTo(oldWorldHeight).Within(WorldHeightToleranceMeters));
        }

        [Test]
        public void PositiveOverflowMutatesNeitherTerrainDataNorTransform()
        {
            SetUniformHeight(0.98f);
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            float oldHeight = FirstHeight();
            Vector3 oldPosition = terrain.transform.position;
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 20f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.MaximumSafePositiveOffsetMeters, Is.EqualTo(12f).Within(0.02f));
            Assert.Throws<System.InvalidOperationException>(() => TerrainHeightOffset.Apply(terrain, result));
            Assert.That(FirstHeight(), Is.EqualTo(oldHeight).Within(TerrainDataReadbackTolerance));
            Assert.That(terrain.transform.position, Is.EqualTo(oldPosition));
        }

        [Test]
        public void NegativeUnderflowMutatesNeitherTerrainDataNorTransform()
        {
            SetUniformHeight(0.02f);
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            float oldHeight = FirstHeight();
            Vector3 oldPosition = terrain.transform.position;
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, -20f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.MaximumSafeNegativeOffsetMeters, Is.EqualTo(-12f).Within(0.02f));
            Assert.Throws<System.InvalidOperationException>(() => TerrainHeightOffset.Apply(terrain, result));
            Assert.That(FirstHeight(), Is.EqualTo(oldHeight).Within(TerrainDataReadbackTolerance));
            Assert.That(terrain.transform.position, Is.EqualTo(oldPosition));
        }

        [Test]
        public void ZeroOffsetMutatesAndDirtiesNeitherTerrainDataNorTransform()
        {
            SetUniformHeight(0.35f);
            terrain.transform.position = new Vector3(12f, 85f, -34f);
            float oldHeight = FirstHeight();
            Vector3 oldPosition = terrain.transform.position;
            EditorUtility.ClearDirty(terrainData);
            EditorUtility.ClearDirty(terrain.transform);
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 0f);

            TerrainHeightOffset.Apply(terrain, result);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.IsZeroOffset, Is.True);
            Assert.That(FirstHeight(), Is.EqualTo(oldHeight).Within(TerrainDataReadbackTolerance));
            Assert.That(terrain.transform.position, Is.EqualTo(oldPosition));
            Assert.That(EditorUtility.IsDirty(terrainData), Is.False);
            Assert.That(EditorUtility.IsDirty(terrain.transform), Is.False);
        }

        [Test]
        public void ConversionReadsVerticalRangeFromTerrainData()
        {
            terrainData.size = new Vector3(1000f, 250f, 1000f);
            TerrainHeightOffsetAnalysis result = TerrainHeightOffset.Analyze(terrainData, 20f);
            Assert.That(result.VerticalRangeMeters, Is.EqualTo(250f));
            Assert.That(result.NormalizedDelta, Is.EqualTo(0.08f).Within(0.0000001f));
        }

        private float FirstHeight() => terrainData.GetHeights(0, 0, 1, 1)[0, 0];

        private float WorldHeightAtFirstSample() => terrain.transform.position.y + FirstHeight() * terrainData.size.y;

        private void SetUniformHeight(float height)
        {
            int resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                    heights[y, x] = height;
            terrainData.SetHeights(0, 0, heights);
        }

        private void SetTwoHeights(float first, float second)
        {
            int resolution = terrainData.heightmapResolution;
            var heights = new float[resolution, resolution];
            heights[0, 0] = first;
            heights[0, 1] = second;
            terrainData.SetHeights(0, 0, heights);
        }
    }
}
