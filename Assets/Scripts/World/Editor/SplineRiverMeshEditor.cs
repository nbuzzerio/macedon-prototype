using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineRiverMesh))]
public sealed class SplineRiverMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "This component represents one independently editable river channel. Knot Widths correspond to this channel's spline knots in route order. Slightly overlap channel endpoints to make forks and rejoins. The generated mesh is visual only; rebake the NavMesh after route changes.",
            MessageType.Info);

        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Rebuild River Mesh"))
        {
            ((SplineRiverMesh)target).Rebuild();
            SceneView.RepaintAll();
        }
    }
}
