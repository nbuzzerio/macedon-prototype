using System;
using UnityEditor;
using UnityEngine;

namespace Macedon.TerrainTools.Editor
{
    public sealed class TerrainHeightOffsetWindow : EditorWindow
    {
        private const string MenuPath = "Tools/MACEDON/Terrain Height Offset";
        private float offsetMeters = 20f;
        private Terrain selectedTerrain;
        private TerrainHeightOffsetAnalysis? analysis;
        private string analysisError;

        [MenuItem(MenuPath)]
        private static void Open() => GetWindow<TerrainHeightOffsetWindow>("Terrain Height Offset");

        private void OnEnable()
        {
            Selection.selectionChanged += SelectionChanged;
            SelectionChanged();
        }

        private void OnDisable() => Selection.selectionChanged -= SelectionChanged;

        private void SelectionChanged()
        {
            selectedTerrain = Selection.activeGameObject == null
                ? null
                : Selection.activeGameObject.GetComponent<Terrain>();
            RefreshAnalysis();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Offsets every internal height sample, then counter-translates the Terrain GameObject by the same world-meter distance. Existing terrain surfaces stay at the same world-space elevation while the heightmap gains sculpting headroom.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Selected Terrain", selectedTerrain, typeof(Terrain), true);

            if (selectedTerrain == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a Terrain component.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            offsetMeters = EditorGUILayout.FloatField("Offset (World Meters)", offsetMeters);
            if (EditorGUI.EndChangeCheck()) RefreshAnalysis();

            if (GUILayout.Button("Refresh Heightmap Analysis")) RefreshAnalysis();

            if (analysisError != null)
            {
                EditorGUILayout.HelpBox(analysisError, MessageType.Error);
                return;
            }

            if (!analysis.HasValue) return;
            TerrainHeightOffsetAnalysis value = analysis.Value;
            DrawAnalysis(value, selectedTerrain.transform.position.y);

            if (!value.IsValid)
            {
                EditorGUILayout.HelpBox(
                    $"Rejected: at least one height sample would leave [0, 1]. Samples are never individually clamped. Safe offset range: {value.MaximumSafeNegativeOffsetMeters:F3} m to +{value.MaximumSafePositiveOffsetMeters:F3} m.",
                    MessageType.Error);
            }
            else if (value.IsZeroOffset)
            {
                EditorGUILayout.HelpBox("A zero-meter offset is a safe no-op and will not dirty TerrainData or the Terrain Transform.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying || !value.IsValid || value.IsZeroOffset))
                if (GUILayout.Button("Apply Height Offset")) ConfirmAndApply();
        }

        private static void DrawAnalysis(TerrainHeightOffsetAnalysis value, float terrainTransformY)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Vertical Range", $"{value.VerticalRangeMeters:F3} m");
            EditorGUILayout.LabelField("Normalized Delta", value.NormalizedDelta.ToString("G9"));
            EditorGUILayout.LabelField("Current Height Range", $"{value.MinimumElevationMeters:F3} m to {value.MaximumElevationMeters:F3} m above terrain base");
            EditorGUILayout.LabelField("Resulting Height Range", $"{value.ResultingMinimumElevationMeters:F3} m to {value.ResultingMaximumElevationMeters:F3} m above terrain base");
            EditorGUILayout.LabelField("Current Terrain Transform Y", $"{terrainTransformY:F3} m");
            EditorGUILayout.LabelField("Resulting Terrain Transform Y", $"{terrainTransformY - value.OffsetMeters:F3} m");
            EditorGUILayout.LabelField("Resulting Sculpting Headroom", $"~{value.DownwardHeadroomMeters:F3} m down / ~{value.UpwardHeadroomMeters:F3} m up");
        }

        private void RefreshAnalysis()
        {
            analysis = null;
            analysisError = null;
            if (selectedTerrain == null || selectedTerrain.terrainData == null) return;
            try
            {
                analysis = TerrainHeightOffset.Analyze(selectedTerrain.terrainData, offsetMeters);
            }
            catch (Exception exception)
            {
                analysisError = exception.Message;
            }
        }

        private void ConfirmAndApply()
        {
            TerrainData terrainData = selectedTerrain == null ? null : selectedTerrain.terrainData;
            if (terrainData == null || Application.isPlaying) return;

            // Re-read immediately before confirmation so stale preview data can never bypass validation.
            TerrainHeightOffsetAnalysis current;
            try
            {
                current = TerrainHeightOffset.Analyze(terrainData, offsetMeters);
            }
            catch (Exception exception)
            {
                analysisError = exception.Message;
                return;
            }

            analysis = current;
            if (!current.IsValid || current.IsZeroOffset) return;

            string message =
                $"Terrain: {selectedTerrain.name}\n" +
                $"Requested offset: {current.OffsetMeters:+0.###;-0.###;0} m\n" +
                $"Vertical range: {current.VerticalRangeMeters:F3} m\n\n" +
                $"Current heightmap elevation: {current.MinimumElevationMeters:F3} m to {current.MaximumElevationMeters:F3} m above terrain base\n" +
                $"Resulting elevation: {current.ResultingMinimumElevationMeters:F3} m to {current.ResultingMaximumElevationMeters:F3} m above terrain base\n" +
                $"Terrain Transform Y: {selectedTerrain.transform.position.y:F3} m to {selectedTerrain.transform.position.y - current.OffsetMeters:F3} m\n" +
                $"Resulting sculpting headroom: ~{current.DownwardHeadroomMeters:F3} m down / ~{current.UpwardHeadroomMeters:F3} m up\n\n" +
                "The heightmap and Terrain Transform will change together so the existing surface remains fixed in world space. Continue?";
            if (!EditorUtility.DisplayDialog("Apply Terrain Height Offset?", message, "Apply Offset", "Cancel")) return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Offset Terrain Heightmap");
            try
            {
                Undo.RegisterCompleteObjectUndo(
                    new UnityEngine.Object[] { terrainData, selectedTerrain.transform },
                    "Offset Terrain Heightmap");
                TerrainHeightOffset.Apply(selectedTerrain, current);
                EditorUtility.SetDirty(terrainData);
                selectedTerrain.Flush();
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);
                SceneView.RepaintAll();
                RefreshAnalysis();
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                Debug.LogException(exception, selectedTerrain);
                RefreshAnalysis();
            }
        }
    }
}
