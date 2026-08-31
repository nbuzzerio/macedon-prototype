using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SplineRiverMesh))]
public sealed class SplineRiverMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Knot Widths correspond to spline knots in route order. The generated mesh is visual only; rebake the NavMesh after route changes.",
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
