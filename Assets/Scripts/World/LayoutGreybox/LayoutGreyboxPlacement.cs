using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Macedon.LayoutGreybox
{
    public readonly struct LayoutModulePlacement
    {
        public readonly string PathId;
        public readonly LayoutAssetContract Contract;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;

        public LayoutModulePlacement(string pathId, LayoutAssetContract contract, Vector3 localPosition, Quaternion localRotation)
        { PathId = pathId; Contract = contract; LocalPosition = localPosition; LocalRotation = localRotation; }
    }

    public static class LayoutGreyboxPlacement
    {
        public static List<LayoutModulePlacement> Build(LayoutV1 layout, IReadOnlyList<LayoutAssetContract> contracts)
        {
            var placements = new List<LayoutModulePlacement>();
            foreach (LayoutPath path in layout.paths)
            {
                LayoutAssetContract selected = contracts.Single(contract => contract.Id == path.assetContractId);
                var family = contracts.Where(contract => contract.StructuralFamilyKey == selected.StructuralFamilyKey).ToArray();
                double dx = path.points[1].x - path.points[0].x;
                double dz = path.points[1].z - path.points[0].z;
                double length = Math.Sqrt(dx * dx + dz * dz);
                if (!LayoutGreyboxValidator.TryGetModuleSequence(length, family, out List<LayoutAssetContract> modules))
                    throw new InvalidOperationException($"Validated path '{path.id}' has no modular solution.");
                var direction = new Vector3((float)(dx / length), 0, (float)(dz / length));
                Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up);
                double distance = 0;
                foreach (LayoutAssetContract module in modules)
                {
                    double center = distance + module.WidthMeters * 0.5;
                    placements.Add(new LayoutModulePlacement(path.id, module,
                        new Vector3((float)(path.points[0].x + dx / length * center), 0,
                            (float)(path.points[0].z + dz / length * center)), rotation));
                    distance += module.WidthMeters;
                }
            }
            return placements;
        }

        public static Vector3 AnchorLocalPosition(LayoutV1 layout) => new((float)layout.anchor.position.x,
            (float)layout.anchor.position.y, (float)layout.anchor.position.z);

        // Layout angles are counter-clockwise from +X toward +Z; Unity Y rotation uses the opposite sign.
        public static Quaternion AnchorLocalRotation(LayoutV1 layout) => Quaternion.Euler(0, (float)-layout.anchor.rotationDegrees, 0);
    }
}
