using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Macedon.TerrainTools.Editor
{
    [Serializable]
    public sealed class TerrainTopographyDocument
    {
        public int formatVersion = 1;
        public string terrainName;
        public string terrainDataAssetPath;
        public TopographyVector3 worldOrigin;
        public TopographyVector3 sizeMeters;
        public int heightmapResolution;
        public float minimumNormalizedHeight;
        public float maximumNormalizedHeight;
        public float minimumLocalElevationMeters;
        public float maximumLocalElevationMeters;
        public float minimumWorldElevation;
        public float maximumWorldElevation;
        public TerrainTopographyPayload heightData;
        public string coordinateConvention = "Rows are Unity local +Z, columns are local +X; viewer +X is right and +Z is up.";
    }

    [Serializable]
    public sealed class TerrainTopographyPayload
    {
        public string file;
        public string encoding = "uint16-little-endian-normalized";
        public int width;
        public int height;
        public string rowOrder = "local-z-ascending";
        public string columnOrder = "local-x-ascending";
        public int byteLength;
    }

    [Serializable]
    public struct TopographyVector3
    {
        public float x;
        public float y;
        public float z;

        public TopographyVector3(Vector3 value) { x = value.x; y = value.y; z = value.z; }
    }

    public static class TerrainTopographyExport
    {
        public const string DefaultExportDirectory = "Exports/TerrainTopography";

        [MenuItem("Tools/MACEDON/Export Terrain Topography", false, 120)]
        private static void ExportSelected()
        {
            Terrain terrain = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<Terrain>()
                : null;
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Export Terrain Topography", "Select a GameObject with a Terrain component.", "OK");
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string directory = Path.Combine(projectRoot, DefaultExportDirectory);
            Directory.CreateDirectory(directory);
            string stem = SanitizeFileName(terrain.name);
            string metadataPath = Path.Combine(directory, stem + ".topography.json");

            if (File.Exists(metadataPath) &&
                !EditorUtility.DisplayDialog("Replace terrain export?", metadataPath + " already exists. Replace its metadata and RAW payload?", "Replace", "Cancel"))
                return;

            try
            {
                TerrainTopographyDocument document = Export(terrain, metadataPath);
                EditorUtility.RevealInFinder(metadataPath);
                Debug.Log($"Exported Terrain Topography V{document.formatVersion}: {metadataPath}", terrain);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Terrain export failed", exception.Message, "OK");
            }
        }

        [MenuItem("Tools/MACEDON/Export Terrain Topography", true)]
        private static bool ValidateExportSelected() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<Terrain>() != null;

        public static TerrainTopographyDocument BuildDocument(Terrain terrain, string payloadFileName, out byte[] payload)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            TerrainData data = terrain.terrainData;
            if (data == null) throw new ArgumentException("Terrain must have TerrainData.", nameof(terrain));
            if (terrain.transform.rotation != Quaternion.identity || terrain.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("V1 supports Terrain transforms with identity rotation and unit scale only.");

            int resolution = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            payload = EncodeUInt16LittleEndian(heights, out float minimum, out float maximum);
            float baseY = terrain.transform.position.y;
            float verticalRange = data.size.y;
            return new TerrainTopographyDocument
            {
                terrainName = terrain.name,
                terrainDataAssetPath = AssetDatabase.GetAssetPath(data),
                worldOrigin = new TopographyVector3(terrain.transform.position),
                sizeMeters = new TopographyVector3(data.size),
                heightmapResolution = resolution,
                minimumNormalizedHeight = minimum,
                maximumNormalizedHeight = maximum,
                minimumLocalElevationMeters = NormalizedToLocalMeters(minimum, verticalRange),
                maximumLocalElevationMeters = NormalizedToLocalMeters(maximum, verticalRange),
                minimumWorldElevation = LocalToWorldElevation(NormalizedToLocalMeters(minimum, verticalRange), baseY),
                maximumWorldElevation = LocalToWorldElevation(NormalizedToLocalMeters(maximum, verticalRange), baseY),
                heightData = new TerrainTopographyPayload
                {
                    file = payloadFileName,
                    width = resolution,
                    height = resolution,
                    byteLength = payload.Length
                }
            };
        }

        public static TerrainTopographyDocument Export(Terrain terrain, string metadataPath)
        {
            if (string.IsNullOrWhiteSpace(metadataPath)) throw new ArgumentException("An output path is required.", nameof(metadataPath));
            string fullMetadataPath = Path.GetFullPath(metadataPath);
            string directory = Path.GetDirectoryName(fullMetadataPath);
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Output path must have a directory.", nameof(metadataPath));
            Directory.CreateDirectory(directory);
            string payloadName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fullMetadataPath)) + ".height.raw";
            TerrainTopographyDocument document = BuildDocument(terrain, payloadName, out byte[] payload);
            File.WriteAllBytes(Path.Combine(directory, payloadName), payload);
            File.WriteAllText(fullMetadataPath, JsonUtility.ToJson(document, true) + Environment.NewLine);
            return document;
        }

        public static float NormalizedToLocalMeters(float normalizedHeight, float terrainHeight) => normalizedHeight * terrainHeight;
        public static float LocalToWorldElevation(float localMeters, float terrainTransformY) => terrainTransformY + localMeters;

        public static int SampleIndex(int x, int z, int resolution)
        {
            if (resolution <= 0 || x < 0 || z < 0 || x >= resolution || z >= resolution) throw new ArgumentOutOfRangeException();
            return z * resolution + x;
        }

        public static byte[] EncodeUInt16LittleEndian(float[,] heights, out float minimum, out float maximum)
        {
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            int rows = heights.GetLength(0), columns = heights.GetLength(1);
            byte[] bytes = new byte[rows * columns * 2];
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            for (int z = 0; z < rows; z++)
            for (int x = 0; x < columns; x++)
            {
                float value = heights[z, x];
                minimum = Mathf.Min(minimum, value);
                maximum = Mathf.Max(maximum, value);
                ushort encoded = (ushort)Mathf.RoundToInt(Mathf.Clamp01(value) * ushort.MaxValue);
                int index = (z * columns + x) * 2;
                bytes[index] = (byte)(encoded & 0xff);
                bytes[index + 1] = (byte)(encoded >> 8);
            }
            return bytes;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "Terrain" : value;
        }
    }
}
