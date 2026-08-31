using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SplineContainer))]
public sealed class SplineRiverMesh : MonoBehaviour
{
    [Header("Output")]
    [SerializeField] private MeshFilter surfaceMeshFilter;

    [Header("Shape")]
    [SerializeField, Min(0.1f)] private float defaultWidth = 4f;
    [SerializeField] private List<float> knotWidths = new();
    [SerializeField, Range(0.1f, 4f)] private float samplesPerMeter = 0.5f;

    [Header("UVs")]
    [SerializeField, Min(0.1f)] private float uvMetersPerTile = 4f;

    private SplineContainer splineContainer;
    private Mesh generatedMesh;

    public IReadOnlyList<float> KnotWidths => knotWidths;

    private void OnEnable()
    {
        CacheComponents();
        Spline.Changed += OnSplineChanged;
        Rebuild();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    private void OnDestroy()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }
    }

    private void OnValidate()
    {
        defaultWidth = Mathf.Max(0.1f, defaultWidth);
        samplesPerMeter = Mathf.Clamp(samplesPerMeter, 0.1f, 4f);
        uvMetersPerTile = Mathf.Max(0.1f, uvMetersPerTile);
        CacheComponents();
        SyncWidthCount();
        Rebuild();
    }

    [ContextMenu("Rebuild River Mesh")]
    public void Rebuild()
    {
        CacheComponents();

        if (surfaceMeshFilter == null || splineContainer == null || splineContainer.Splines.Count == 0)
        {
            return;
        }

        Spline spline = splineContainer.Spline;

        if (spline == null || spline.Count < 2)
        {
            ClearMesh();
            return;
        }

        SyncWidthCount();

        float splineLength = spline.GetLength();
        int sampleCount = Mathf.Clamp(Mathf.CeilToInt(splineLength * samplesPerMeter) + 1, 2, 4096);
        var vertices = new Vector3[sampleCount * 2];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[(sampleCount - 1) * 6];
        Transform outputTransform = surfaceMeshFilter.transform;
        Vector3 localUp = outputTransform.InverseTransformDirection(Vector3.up).normalized;

        Vector3 previousPosition = (Vector3)splineContainer.EvaluatePosition(0f);
        float traveledDistance = 0f;

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            float t = sampleIndex / (sampleCount - 1f);
            Vector3 position = (Vector3)splineContainer.EvaluatePosition(t);
            Vector3 tangent = (Vector3)splineContainer.EvaluateTangent(t);
            tangent.y = 0f;

            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = sampleIndex > 0 ? position - previousPosition : Vector3.forward;
                tangent.y = 0f;
            }

            tangent.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
            float halfWidth = EvaluateWidth(spline, t) * 0.5f;

            if (sampleIndex > 0)
            {
                traveledDistance += Vector3.Distance(previousPosition, position);
            }

            int vertexIndex = sampleIndex * 2;
            vertices[vertexIndex] = outputTransform.InverseTransformPoint(position - right * halfWidth);
            vertices[vertexIndex + 1] = outputTransform.InverseTransformPoint(position + right * halfWidth);
            normals[vertexIndex] = localUp;
            normals[vertexIndex + 1] = localUp;
            float flowV = traveledDistance / uvMetersPerTile;
            uvs[vertexIndex] = new Vector2(0f, flowV);
            uvs[vertexIndex + 1] = new Vector2(1f, flowV);

            previousPosition = position;
        }

        for (int segmentIndex = 0; segmentIndex < sampleCount - 1; segmentIndex++)
        {
            int vertexIndex = segmentIndex * 2;
            int triangleIndex = segmentIndex * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 1;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        EnsureMesh();
        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.normals = normals;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateBounds();
        surfaceMeshFilter.sharedMesh = generatedMesh;
    }

    private void CacheComponents()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
        }
    }

    private void SyncWidthCount()
    {
        if (splineContainer == null || splineContainer.Splines.Count == 0)
        {
            return;
        }

        int knotCount = splineContainer.Spline.Count;

        while (knotWidths.Count < knotCount)
        {
            knotWidths.Add(defaultWidth);
        }

        if (knotWidths.Count > knotCount)
        {
            knotWidths.RemoveRange(knotCount, knotWidths.Count - knotCount);
        }

        for (int i = 0; i < knotWidths.Count; i++)
        {
            knotWidths[i] = Mathf.Max(0.1f, knotWidths[i]);
        }
    }

    private float EvaluateWidth(Spline spline, float normalizedT)
    {
        if (knotWidths.Count == 0)
        {
            return defaultWidth;
        }

        if (knotWidths.Count == 1 || normalizedT <= 0f)
        {
            return knotWidths[0];
        }

        if (normalizedT >= 1f)
        {
            return knotWidths[knotWidths.Count - 1];
        }

        float targetDistance = normalizedT * spline.GetLength();
        float accumulatedDistance = 0f;
        int curveCount = spline.Closed ? spline.Count : spline.Count - 1;

        for (int curveIndex = 0; curveIndex < curveCount; curveIndex++)
        {
            float curveLength = spline.GetCurveLength(curveIndex);

            if (targetDistance <= accumulatedDistance + curveLength || curveIndex == curveCount - 1)
            {
                float curveT = curveLength > 0.0001f
                    ? (targetDistance - accumulatedDistance) / curveLength
                    : 0f;
                int nextKnot = spline.Closed ? (curveIndex + 1) % spline.Count : curveIndex + 1;
                return Mathf.Lerp(knotWidths[curveIndex], knotWidths[nextKnot], Mathf.Clamp01(curveT));
            }

            accumulatedDistance += curveLength;
        }

        return knotWidths[knotWidths.Count - 1];
    }

    private void EnsureMesh()
    {
        if (generatedMesh != null)
        {
            return;
        }

        generatedMesh = new Mesh
        {
            name = $"{name} Generated River Surface",
            hideFlags = HideFlags.HideAndDontSave
        };
        generatedMesh.MarkDynamic();
    }

    private void ClearMesh()
    {
        if (generatedMesh != null)
        {
            generatedMesh.Clear();
        }

        if (surfaceMeshFilter != null)
        {
            surfaceMeshFilter.sharedMesh = generatedMesh;
        }
    }

    private void OnSplineChanged(Spline changedSpline, int knotIndex, SplineModification modification)
    {
        if (splineContainer != null && splineContainer.Splines.Count > 0 &&
            changedSpline == splineContainer.Spline)
        {
            SyncWidthCount();
            Rebuild();
        }
    }
}
