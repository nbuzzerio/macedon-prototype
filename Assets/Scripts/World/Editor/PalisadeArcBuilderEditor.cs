using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PalisadeArcBuilder))]
public sealed class PalisadeArcBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var builder = (PalisadeArcBuilder)target;
        EditorGUILayout.HelpBox("Local X is module width. Endpoints mark first/last module centers; all roots use Start's Y. Positive bulge bows toward Cross(Up, End - Start). Changes update the preview; click Rebuild to apply.", MessageType.Info);
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Generated Root", builder.GeneratedRoot, typeof(Transform), true);

        string safetyError = ValidateOwnership(builder);
        string buildError = ValidateBuild(builder);
        if (safetyError != null || buildError != null)
            EditorGUILayout.HelpBox(safetyError ?? buildError, MessageType.Warning);
        if (builder.TryGetArc(out var arc))
            EditorGUILayout.LabelField("Proposed Modules", (arc.IntervalCount(builder.SegmentWidth) + 1).ToString());

        using (new EditorGUI.DisabledScope(Application.isPlaying || safetyError != null || buildError != null))
            if (GUILayout.Button("Generate / Rebuild Arc")) Change(builder, true);
        using (new EditorGUI.DisabledScope(Application.isPlaying || safetyError != null))
            if (GUILayout.Button("Clear Generated")) Change(builder, false);
    }

    private static string ValidateBuild(PalisadeArcBuilder builder)
    {
        if (!builder.gameObject.scene.IsValid() || EditorUtility.IsPersistent(builder))
            return "Use a builder in a scene or Prefab Mode.";
        if (builder.PalisadePrefab == null || !PrefabUtility.IsPartOfPrefabAsset(builder.PalisadePrefab) ||
            builder.PalisadePrefab.transform.parent != null)
            return "Assign a prefab asset root from the Project window.";
        if (!builder.TryGetArc(out var arc)) return "Assign distinct XZ endpoints and a finite bulge/positive width.";
        if (arc.IntervalCount(builder.SegmentWidth) >= 1000) return "Arc exceeds the 1,000 module limit. Reduce its size or increase width.";
        // Unit scale on every ancestor avoids scaled widths and shear under rotated parents.
        for (Transform parent = builder.transform; parent != null; parent = parent.parent)
            if ((parent.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                return "Builder and its ancestors must have scale (1,1,1).";
        if (builder.GeneratedRoot != null && (builder.GeneratedRoot.localScale - Vector3.one).sqrMagnitude > 0.000001f)
            return "Generated Root must have scale (1,1,1).";
        return null;
    }

    private static string ValidateOwnership(PalisadeArcBuilder builder)
    {
        if (builder.GeneratedRoot != null && builder.GeneratedRoot.parent != builder.transform)
            return "Generated Root was moved or belongs to another builder. Restore it beneath this builder before clearing/rebuilding.";
        foreach (GameObject segment in builder.Generated)
        {
            if (segment == null) continue;
            if (builder.GeneratedRoot == null || segment.transform.parent != builder.GeneratedRoot)
                return "A tracked segment was moved. Restore it beneath Generated Root before clearing/rebuilding.";
            foreach (Transform child in segment.GetComponentsInChildren<Transform>(true))
            {
                if (child == builder.transform || child == builder.Start || child == builder.End)
                    return "Move builder/endpoints outside generated segments before clearing/rebuilding.";
                if (child != segment.transform && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == null)
                    return "A generated segment contains added scene content. Move that content outside the segment before clearing/rebuilding.";
                if (child != segment.transform && PrefabUtility.IsAddedGameObjectOverride(child.gameObject))
                    return "A generated segment contains an added prefab. Move it outside the segment before clearing/rebuilding.";
            }
        }
        return null;
    }

    private static void Change(PalisadeArcBuilder builder, bool rebuild)
    {
        string error = ValidateOwnership(builder) ?? (rebuild ? ValidateBuild(builder) : null);
        if (error != null || Application.isPlaying) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(rebuild ? "Rebuild Palisade Arc" : "Clear Palisade Arc");
        try
        {
            Undo.RegisterCompleteObjectUndo(builder, "Track Palisade Arc");
            foreach (GameObject segment in builder.Generated)
                if (segment != null) Undo.DestroyObjectImmediate(segment);
            builder.Generated.Clear();
            if (rebuild)
            {
                if (builder.GeneratedRoot == null)
                {
                    var root = new GameObject("Palisade Arc - Generated");
                    Undo.RegisterCreatedObjectUndo(root, "Create Generated Root");
                    Undo.SetTransformParent(root.transform, builder.transform, "Parent Generated Root");
                    Undo.RecordObject(root.transform, "Place Generated Root");
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    root.transform.localScale = Vector3.one;
                    builder.GeneratedRoot = root.transform;
                }
                builder.TryGetArc(out var arc);
                int intervals = arc.IntervalCount(builder.SegmentWidth);
                for (int i = 0; i <= intervals; i++)
                {
                    var segment = (GameObject)PrefabUtility.InstantiatePrefab(builder.PalisadePrefab, builder.GeneratedRoot);
                    Undo.RegisterCreatedObjectUndo(segment, "Create Palisade Segment");
                    Undo.RecordObject(segment.transform, "Place Palisade Segment");
                    arc.Evaluate(i / (float)intervals, out Vector3 position, out Vector3 tangent);
                    segment.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.Cross(tangent, Vector3.up), Vector3.up));
                    segment.transform.localScale = Vector3.one;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(segment.transform);
                    builder.Generated.Add(segment);
                }
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(builder);
            EditorUtility.SetDirty(builder);
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            SceneView.RepaintAll();
        }
        catch (System.Exception exception)
        {
            Undo.FlushUndoRecordObjects();
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(exception, builder);
        }
    }
}
