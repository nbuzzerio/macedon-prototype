using System.Collections.Generic;
using UnityEngine;

namespace Macedon.LayoutGreybox
{
    public enum LayoutPathRole { Empty, Segment, Corner, Edge, Gate, Junction, Discrete }

    public sealed class LayoutAssetContract
    {
        public int Id { get; }
        public string Key { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string StructuralFamilyKey { get; }
        public LayoutPathRole PathRole { get; }
        public double WidthMeters { get; }
        public double HeightMeters { get; }
        public double DepthMeters { get; }
        public bool TerrainConformingAllowed { get; }
        public GameObject Prefab { get; }

        public LayoutAssetContract(int id, string key, string displayName, string category,
            string structuralFamilyKey, LayoutPathRole pathRole, double widthMeters,
            double heightMeters, double depthMeters, bool terrainConformingAllowed, GameObject prefab)
        {
            Id = id;
            Key = key;
            DisplayName = displayName;
            Category = category;
            StructuralFamilyKey = structuralFamilyKey;
            PathRole = pathRole;
            WidthMeters = widthMeters;
            HeightMeters = heightMeters;
            DepthMeters = depthMeters;
            TerrainConformingAllowed = terrainConformingAllowed;
            Prefab = prefab;
        }
    }

    public static class LayoutGreyboxContractRegistry
    {
        public static IReadOnlyList<LayoutAssetContract> Create(GameObject palisadeWall3mPrefab)
        {
            return new[]
            {
                new LayoutAssetContract(0, "Empty", "Empty", "System", "Empty",
                    LayoutPathRole.Empty, 0, 0, 0, false, null),
                new LayoutAssetContract(1, "Palisade_Wall_3m", "Palisade Wall 3m", "Palisade",
                    "Palisade_Wall", LayoutPathRole.Segment, 3, 4, 0.4, true, palisadeWall3mPrefab)
            };
        }
    }
}
