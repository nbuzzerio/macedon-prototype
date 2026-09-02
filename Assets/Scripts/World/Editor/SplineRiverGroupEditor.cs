using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

[CustomEditor(typeof(SplineRiverGroup))]
public sealed class SplineRiverGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "A river group collects independent SplineRiverMesh channels on this object and its children. Select a channel to edit its spline and per-knot widths. Forks and rejoins are made by overlapping channel endpoints; junction meshes are not welded.",
            MessageType.Info);

        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        SplineRiverGroup group = (SplineRiverGroup)target;

        if (GUILayout.Button("Rebuild All River Channels"))
        {
            group.RebuildAll();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Add Empty Channel"))
        {
            CreateEmptyChannel(group);
        }
    }

    private static void CreateEmptyChannel(SplineRiverGroup group)
    {
        int channelNumber = group.Channels.Length + 1;
        var channelObject = new GameObject($"Channel_{channelNumber:00}");
        Undo.RegisterCreatedObjectUndo(channelObject, "Add River Channel");
        Undo.SetTransformParent(channelObject.transform, group.transform, "Parent River Channel");
        channelObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        channelObject.transform.localScale = Vector3.one;
        channelObject.layer = group.gameObject.layer;

        SplineContainer splineContainer = channelObject.AddComponent<SplineContainer>();
        splineContainer.Spline.Add(new BezierKnot(Vector3.zero));
        splineContainer.Spline.Add(new BezierKnot(new Vector3(5f, 0f, 0f)));

        var surfaceObject = new GameObject("WaterSurface");
        Undo.RegisterCreatedObjectUndo(surfaceObject, "Add River Surface");
        Undo.SetTransformParent(surfaceObject.transform, channelObject.transform, "Parent River Surface");
        surfaceObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        surfaceObject.transform.localScale = Vector3.one;
        surfaceObject.layer = group.gameObject.layer;

        MeshFilter meshFilter = surfaceObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = surfaceObject.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        MeshRenderer sourceRenderer = FindSourceRenderer(group);
        if (sourceRenderer != null)
        {
            meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
        }

        SplineRiverMesh channel = channelObject.AddComponent<SplineRiverMesh>();
        SerializedObject serializedChannel = new SerializedObject(channel);
        serializedChannel.FindProperty("surfaceMeshFilter").objectReferenceValue = meshFilter;
        serializedChannel.ApplyModifiedPropertiesWithoutUndo();
        channel.Rebuild();

        Selection.activeGameObject = channelObject;
        EditorGUIUtility.PingObject(channelObject);
    }

    private static MeshRenderer FindSourceRenderer(SplineRiverGroup group)
    {
        SplineRiverMesh[] channels = group.Channels;

        for (int i = 0; i < channels.Length; i++)
        {
            MeshFilter filter = channels[i].SurfaceMeshFilter;
            if (filter != null && filter.TryGetComponent(out MeshRenderer renderer))
            {
                return renderer;
            }
        }

        return null;
    }
}
