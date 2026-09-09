using System;

namespace Macedon.LayoutGreybox
{
    [Serializable]
    public sealed class LayoutV1
    {
        public int formatVersion = -1;
        public string name;
        public string units;
        public LayoutGrid grid;
        public LayoutAnchor anchor;
        public string defaultPlacementMode;
        public long deterministicSeed = -1;
        public LayoutPath[] paths;
        public LayoutOverlay[] overlays;
    }

    [Serializable]
    public sealed class LayoutGrid { public double spacingMeters = double.NaN; }

    [Serializable]
    public sealed class LayoutAnchor
    {
        public LayoutPoint3 position;
        public double rotationDegrees = double.NaN;
    }

    [Serializable]
    public sealed class LayoutPoint2 { public double x = double.NaN; public double z = double.NaN; }

    [Serializable]
    public sealed class LayoutPoint3
    {
        public double x = double.NaN;
        public double y = double.NaN;
        public double z = double.NaN;
    }

    [Serializable]
    public sealed class LayoutPath
    {
        public string id;
        public int assetContractId;
        public LayoutPoint2[] points;
        public string placementMode;
        public string state;
        public LayoutModifiers modifiers;
    }

    [Serializable]
    public sealed class LayoutOverlay
    {
        public string id;
        public int assetContractId;
        public string pathId;
        public double distanceAlongPathMeters;
        public string state;
        public LayoutModifiers modifiers;
    }

    // V1 reserves modifiers as a separate object. Fields are intentionally deferred.
    [Serializable]
    public sealed class LayoutModifiers { }
}
