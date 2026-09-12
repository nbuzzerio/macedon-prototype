using Macedon.TerrainTools.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Macedon.TerrainTools.Tests
{
    public sealed class TerrainTopographyExportTests
    {
        private TerrainData data;
        private GameObject gameObject;
        private Terrain terrain;

        [SetUp]
        public void SetUp()
        {
            data = new TerrainData { heightmapResolution = 33, size = new Vector3(1000f, 600f, 1000f) };
            gameObject = Terrain.CreateTerrainGameObject(data);
            terrain = gameObject.GetComponent<Terrain>();
            terrain.transform.position = new Vector3(10f, -20f, 30f);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(gameObject); Object.DestroyImmediate(data); }

        [Test]
        public void ElevationConversionsIncludeTerrainTransformY()
        {
            Assert.That(TerrainTopographyExport.NormalizedToLocalMeters(0.25f, 600f), Is.EqualTo(150f));
            Assert.That(TerrainTopographyExport.LocalToWorldElevation(150f, -20f), Is.EqualTo(130f));
        }

        [Test]
        public void DocumentReportsCompleteMinMaxAndWorldRange()
        {
            var heights = new float[33, 33]; heights[0, 0] = 0.1f; heights[32, 32] = 0.75f; data.SetHeights(0, 0, heights);
            TerrainTopographyDocument result = TerrainTopographyExport.BuildDocument(terrain, "test.height.raw", out byte[] payload);
            Assert.That(result.minimumNormalizedHeight, Is.EqualTo(0f).Within(2f / ushort.MaxValue));
            Assert.That(result.maximumNormalizedHeight, Is.EqualTo(0.75f).Within(2f / ushort.MaxValue));
            Assert.That(result.maximumLocalElevationMeters, Is.EqualTo(450f).Within(0.02f));
            Assert.That(result.maximumWorldElevation, Is.EqualTo(430f).Within(0.02f));
            Assert.That(payload.Length, Is.EqualTo(33 * 33 * 2));
        }

        [Test]
        public void PayloadUsesZRowsAndXColumnsWithoutFlipping()
        {
            float[,] heights = { { 0f, 0.25f }, { 0.5f, 1f } };
            byte[] bytes = TerrainTopographyExport.EncodeUInt16LittleEndian(heights, out _, out _);
            Assert.That(Read(bytes, TerrainTopographyExport.SampleIndex(1, 0, 2)), Is.EqualTo(16384).Within(1));
            Assert.That(Read(bytes, TerrainTopographyExport.SampleIndex(0, 1, 2)), Is.EqualTo(32768).Within(1));
        }

        [Test]
        public void BuildingDocumentDoesNotMutateOrDirtyTerrain()
        {
            float[,] before = data.GetHeights(0, 0, 33, 33);
            Vector3 position = terrain.transform.position;
            EditorUtility.ClearDirty(data); EditorUtility.ClearDirty(terrain.transform);
            TerrainTopographyExport.BuildDocument(terrain, "test.height.raw", out _);
            CollectionAssert.AreEqual(before, data.GetHeights(0, 0, 33, 33));
            Assert.That(terrain.transform.position, Is.EqualTo(position));
            Assert.That(EditorUtility.IsDirty(data), Is.False);
            Assert.That(EditorUtility.IsDirty(terrain.transform), Is.False);
        }

        private static ushort Read(byte[] bytes, int sampleIndex) => (ushort)(bytes[sampleIndex * 2] | bytes[sampleIndex * 2 + 1] << 8);
    }
}
