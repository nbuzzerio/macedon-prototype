using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("World/Palisade Arc Builder")]
public sealed class PalisadeArcBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private GameObject palisadePrefab;
    [SerializeField] private Transform start;
    [SerializeField] private Transform end;
    [Tooltip("Midpoint offset in meters. Positive bows toward Cross(Up, End - Start).")]
    [SerializeField] private float arcBulge = 5f;
    [Tooltip("Prefab width along local X. Endpoints are first/last module centers.")]
    [SerializeField, Min(0.01f)] private float segmentWidth = 3f;
    [SerializeField, HideInInspector] private Transform generatedRoot;
    [SerializeField, HideInInspector] private System.Collections.Generic.List<GameObject> generated = new();

    public GameObject PalisadePrefab => palisadePrefab;
    public Transform Start => start;
    public Transform End => end;
    public float SegmentWidth => segmentWidth;
    public Transform GeneratedRoot { get => generatedRoot; set => generatedRoot = value; }
    public System.Collections.Generic.List<GameObject> Generated => generated;

    // Double precision and half-angle identities keep nearly straight arcs stable.
    public bool TryGetArc(out Arc arc)
    {
        arc = default;
        if (start == null || end == null || !Finite(arcBulge) ||
            !Finite(segmentWidth) || segmentWidth < 0.01f) return false;
        Vector3 chord = end.position - start.position;
        chord.y = 0f;
        float length = chord.magnitude;
        if (!Finite(length) || length < 0.01f || !Finite(start.position.y)) return false;
        arc = new Arc(start.position, chord / length, length, arcBulge);
        return true;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public readonly struct Arc
    {
        private readonly Vector3 origin, along, side;
        private readonly double chord, bulge;
        public readonly double Radius, Sweep;

        public Arc(Vector3 origin, Vector3 along, double chord, double bulge)
        {
            this.origin = origin;
            this.along = along;
            side = Vector3.Cross(Vector3.up, along);
            this.chord = chord;
            this.bulge = bulge;
            Sweep = 4.0 * System.Math.Atan2(System.Math.Abs(bulge), chord * 0.5);
            Radius = bulge == 0 ? double.PositiveInfinity :
                chord * chord / (8.0 * System.Math.Abs(bulge)) + System.Math.Abs(bulge) * 0.5;
        }

        public int IntervalCount(float width)
        {
            double ideal = bulge == 0 ? chord / width :
                Sweep / (2.0 * System.Math.Atan(width / (2.0 * Radius)));
            // Sentinel lets the inspector reject impractically large builds before mutation.
            return ideal > 999 ? 1000 : System.Math.Max(1, (int)System.Math.Round(ideal));
        }

        public void Evaluate(float t, out Vector3 position, out Vector3 tangent)
        {
            if (bulge == 0)
            {
                position = origin + along * (float)(chord * t);
                tangent = along;
                return;
            }
            double angle = (t - 0.5) * Sweep;
            double sign = System.Math.Sign(bulge);
            double sinHalf = System.Math.Sin(angle * 0.5);
            double x = chord * 0.5 + Radius * System.Math.Sin(angle);
            double z = bulge - sign * 2.0 * Radius * sinHalf * sinHalf;
            position = origin + along * (float)x + side * (float)z;
            tangent = along * (float)System.Math.Cos(angle) - side * (float)(sign * System.Math.Sin(angle));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!TryGetArc(out Arc arc)) return;
        Gizmos.color = Color.cyan;
        arc.Evaluate(0f, out Vector3 previous, out _);
        Gizmos.DrawWireSphere(previous, 0.3f);
        for (int i = 1; i <= 96; i++)
        {
            arc.Evaluate(i / 96f, out Vector3 next, out _);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawWireSphere(previous, 0.3f);
    }
#endif
}
