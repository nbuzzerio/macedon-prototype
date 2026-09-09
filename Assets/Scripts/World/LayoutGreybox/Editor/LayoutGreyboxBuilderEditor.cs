using System.Collections.Generic;
using System.Linq;
using Macedon.LayoutGreybox;
using UnityEditor;
using UnityEngine;

namespace Macedon.LayoutGreybox.Editor
{
    [CustomEditor(typeof(LayoutGreyboxBuilder))]
    public sealed class LayoutGreyboxBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var builder = (LayoutGreyboxBuilder)target;
            LayoutGreyboxScaleGuard.Enforce(builder);
            EditorGUILayout.HelpBox("Layout Greybox roots use fixed physical dimensions and must remain at unit scale.", MessageType.Info);
            EditorGUILayout.HelpBox("Layout coordinates map directly to local Unity X/Z. +X is right, +Z is forward, and Y is elevation. Modules fill each path from endpoint to endpoint. Terrain-Conforming generation is not yet supported.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Generated Root", builder.GeneratedRoot, typeof(Transform), true);

            LayoutValidationResult validation = Validate(builder);
            string ownershipError = ValidateOwnership(builder);
            string setupError = ValidateSetup(builder);
            DrawValidation(validation, setupError, ownershipError);

            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Validate Layout")) LogValidation(builder, validation, setupError, ownershipError);

            using (new EditorGUI.DisabledScope(Application.isPlaying || !validation.CanGenerate || setupError != null || ownershipError != null))
                if (GUILayout.Button("Generate / Rebuild")) Change(builder, validation.Layout, true);

            using (new EditorGUI.DisabledScope(Application.isPlaying || builder.GeneratedRoot == null || ownershipError != null))
                if (GUILayout.Button("Clear Generated")) Change(builder, null, false);
        }

        private static LayoutValidationResult Validate(LayoutGreyboxBuilder builder)
        {
            IReadOnlyList<LayoutAssetContract> contracts = LayoutGreyboxContractRegistry.Create(builder.PalisadeWall3mPrefab);
            return LayoutGreyboxValidator.ParseAndValidate(builder.LayoutJson == null ? null : builder.LayoutJson.text, contracts);
        }

        private static string ValidateSetup(LayoutGreyboxBuilder builder)
        {
            if (!builder.gameObject.scene.IsValid() || EditorUtility.IsPersistent(builder))
                return "Use a Layout Greybox Builder in a scene or Prefab Mode.";
            GameObject prefab = builder.PalisadeWall3mPrefab;
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || prefab.transform.parent != null)
                return "Assign the top-level Palisade_Slot_3m prefab asset for Asset Contract ID 1.";
            for (Transform current = builder.transform; current != null; current = current.parent)
                if ((current.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                    return "Builder and its ancestors must have scale (1,1,1).";
            return null;
        }

        private static string ValidateOwnership(LayoutGreyboxBuilder builder)
        {
            Transform root = builder.GeneratedRoot;
            if (root == null)
            {
                if (builder.GeneratedPathRoots.Any(item => item != null) || builder.GeneratedInstances.Any(item => item != null))
                    return "Generated ownership references are inconsistent. Undo the deletion or remove this component and configure it again.";
                return null;
            }
            if (root.parent != builder.transform) return "Generated Root was moved. Restore it directly beneath this builder before clearing or rebuilding.";
            if (HasAddedComponents(root)) return "Generated Root contains added components. Remove or move that authored content before clearing or rebuilding.";

            var paths = new HashSet<Transform>(builder.GeneratedPathRoots.Where(item => item != null).Select(item => item.transform));
            var instances = new HashSet<Transform>(builder.GeneratedInstances.Where(item => item != null).Select(item => item.transform));
            foreach (Transform child in root)
                if (!paths.Contains(child)) return "Generated Root contains untracked scene content. Move it outside before clearing or rebuilding.";
            foreach (Transform path in paths)
            {
                if (path.parent != root) return "A generated Path root was moved. Restore it beneath Generated Root.";
                if (HasAddedComponents(path)) return "A generated Path root contains added components. Remove or move that authored content before clearing or rebuilding.";
                foreach (Transform child in path)
                    if (!instances.Contains(child)) return "A generated Path contains untracked scene content. Move it outside before clearing or rebuilding.";
            }
            foreach (Transform instance in instances)
            {
                if (!paths.Contains(instance.parent)) return "A generated module was moved. Restore it beneath its tracked Path root.";
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (child != instance && (PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == null || PrefabUtility.IsAddedGameObjectOverride(child.gameObject)))
                        return "A generated prefab instance contains added scene content. Move it outside before clearing or rebuilding.";
                    foreach (Component component in child.GetComponents<Component>())
                        if (component != null && PrefabUtility.IsAddedComponentOverride(component))
                            return "A generated prefab instance contains an added component. Remove it before clearing or rebuilding.";
                }
            }
            return null;
        }

        private static bool HasAddedComponents(Transform transform) =>
            transform.GetComponents<Component>().Any(component => component != null && component != transform);

        private static void DrawValidation(LayoutValidationResult validation, string setupError, string ownershipError)
        {
            foreach (LayoutValidationIssue issue in validation.Errors)
                EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}", MessageType.Error);
            foreach (LayoutValidationIssue issue in validation.GenerationBlockers)
                EditorGUILayout.HelpBox($"{issue.Code}: {issue.Message}", MessageType.Warning);
            if (setupError != null) EditorGUILayout.HelpBox(setupError, MessageType.Warning);
            if (ownershipError != null) EditorGUILayout.HelpBox(ownershipError, MessageType.Error);
            if (validation.CanGenerate && setupError == null && ownershipError == null)
                EditorGUILayout.HelpBox($"Valid Layout V1: {validation.Layout.paths.Length} path(s). Ready to generate.", MessageType.Info);
        }

        private static void LogValidation(LayoutGreyboxBuilder builder, LayoutValidationResult validation, string setupError, string ownershipError)
        {
            if (validation.CanGenerate && setupError == null && ownershipError == null)
                Debug.Log($"Layout '{validation.Layout.name}' is valid and ready to generate ({validation.Layout.paths.Length} paths).", builder);
            else
            {
                foreach (LayoutValidationIssue issue in validation.Errors.Concat(validation.GenerationBlockers)) Debug.LogWarning($"{issue.Code}: {issue.Message}", builder);
                if (setupError != null) Debug.LogWarning(setupError, builder);
                if (ownershipError != null) Debug.LogWarning(ownershipError, builder);
            }
        }

        private static void Change(LayoutGreyboxBuilder builder, LayoutV1 layout, bool rebuild)
        {
            if (Application.isPlaying || ValidateOwnership(builder) != null) return;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(rebuild ? "Rebuild Layout Greybox" : "Clear Layout Greybox");
            try
            {
                Undo.RegisterCompleteObjectUndo(builder, "Track Layout Greybox");
                if (builder.GeneratedRoot != null) Undo.DestroyObjectImmediate(builder.GeneratedRoot.gameObject);
                builder.GeneratedRoot = null;
                builder.GeneratedPathRoots.Clear();
                builder.GeneratedInstances.Clear();
                if (rebuild) Generate(builder, layout);
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

        private static void Generate(LayoutGreyboxBuilder builder, LayoutV1 layout)
        {
            IReadOnlyList<LayoutAssetContract> contracts = LayoutGreyboxContractRegistry.Create(builder.PalisadeWall3mPrefab);
            List<LayoutModulePlacement> placements = LayoutGreyboxPlacement.Build(layout, contracts);
            var root = new GameObject("Layout Greybox - Generated");
            Undo.RegisterCreatedObjectUndo(root, "Create Layout Greybox Root");
            Undo.SetTransformParent(root.transform, builder.transform, "Parent Layout Greybox Root");
            Undo.RecordObject(root.transform, "Place Layout Greybox Root");
            root.transform.localPosition = LayoutGreyboxPlacement.AnchorLocalPosition(layout);
            root.transform.localRotation = LayoutGreyboxPlacement.AnchorLocalRotation(layout);
            root.transform.localScale = Vector3.one;
            builder.GeneratedRoot = root.transform;

            foreach (IGrouping<string, LayoutModulePlacement> path in placements.GroupBy(item => item.PathId))
            {
                var pathRoot = new GameObject($"Path_{SafeName(path.Key)}");
                Undo.RegisterCreatedObjectUndo(pathRoot, "Create Layout Path Root");
                Undo.SetTransformParent(pathRoot.transform, root.transform, "Parent Layout Path Root");
                Undo.RecordObject(pathRoot.transform, "Place Layout Path Root");
                pathRoot.transform.localPosition = Vector3.zero;
                pathRoot.transform.localRotation = Quaternion.identity;
                pathRoot.transform.localScale = Vector3.one;
                builder.GeneratedPathRoots.Add(pathRoot);
                foreach (LayoutModulePlacement placement in path)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(placement.Contract.Prefab, pathRoot.transform);
                    if (instance == null) throw new System.InvalidOperationException($"Could not instantiate prefab for contract ID {placement.Contract.Id}.");
                    Undo.RegisterCreatedObjectUndo(instance, "Create Layout Module");
                    Undo.RecordObject(instance.transform, "Place Layout Module");
                    instance.transform.localPosition = placement.LocalPosition;
                    instance.transform.localRotation = placement.LocalRotation;
                    instance.transform.localScale = Vector3.one;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    builder.GeneratedInstances.Add(instance);
                }
            }
        }

        private static string SafeName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace('\\', '_');
        }
    }
}
