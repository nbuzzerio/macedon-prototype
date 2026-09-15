#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Macedon.Development.Editor
{
    public sealed class DevelopmentCheckpointWindow : EditorWindow
    {
        [MenuItem("Tools/MACEDON/Debug Checkpoints")]
        private static void Open() => GetWindow<DevelopmentCheckpointWindow>("Debug Checkpoints");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Runtime QA presets. Enter Play Mode and add/configure a DevelopmentCheckpointController in the scene.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                GUILayout.Button(new GUIContent("Fresh Start", "Restart Play Mode for a trustworthy fresh state in v1."));
            EditorGUILayout.LabelField("Fresh Start: restart Play Mode (safe reset is not available).", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Wolf Defeated")) Apply(controller => controller.ApplyWolfDefeated());
                if (GUILayout.Button("Party Recruited")) Apply(controller => controller.ApplyPartyRecruited());
                if (GUILayout.Button("River Start")) Apply(controller => controller.ApplyRiverStart());
            }
            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Checkpoint buttons are enabled only in Play Mode. No scene assets will be modified.", MessageType.Warning);
        }

        private static void Apply(System.Func<DevelopmentCheckpointController, bool> operation)
        {
            DevelopmentCheckpointController controller = FindFirstObjectByType<DevelopmentCheckpointController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("[Checkpoint] No DevelopmentCheckpointController exists in the playing scene.");
                return;
            }
            operation(controller);
        }
    }
}
#endif
