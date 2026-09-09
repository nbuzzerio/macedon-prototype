using Macedon.LayoutGreybox;
using UnityEditor;
using UnityEngine;

namespace Macedon.LayoutGreybox.Editor
{
    [InitializeOnLoad]
    public static class LayoutGreyboxScaleGuard
    {
        static LayoutGreyboxScaleGuard()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            Debug.Log("[LayoutGreyboxScaleGuard DIAGNOSTIC] Initialized and subscribed to ObjectChangeEvents.changesPublished.");
        }

        public static bool Enforce(LayoutGreyboxBuilder builder)
        {
            if (builder == null || builder.transform.localScale == Vector3.one) return false;
            Vector3 attemptedScale = builder.transform.localScale;
            builder.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(builder.transform);
            Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Enforce reached '{builder.name}': {attemptedScale} -> {builder.transform.localScale}.", builder);
            return true;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            GameObject selected = Selection.activeGameObject;
            LayoutGreyboxBuilder selectedBuilder = selected == null ? null : selected.GetComponent<LayoutGreyboxBuilder>();
            bool trace = selectedBuilder != null && selected.transform.localScale != Vector3.one;
            if (trace)
                Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Callback invoked for selected scaled builder '{selected.name}'; stream length {stream.length}.", selectedBuilder);

            for (int index = 0; index < stream.length; index++)
            {
                ObjectChangeKind eventType = stream.GetEventType(index);
                if (trace) Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Event {index}: {eventType}.", selectedBuilder);
                if (eventType != ObjectChangeKind.ChangeGameObjectOrComponentProperties) continue;
                stream.GetChangeGameObjectOrComponentPropertiesEvent(index, out ChangeGameObjectOrComponentPropertiesEventArgs change);
                Object changed = EditorUtility.EntityIdToObject(change.instanceId);
                if (trace)
                    Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Property event instanceId {change.instanceId} resolved to '{(changed == null ? "null" : changed.GetType().Name + " " + changed.name)}'.", selectedBuilder);
                GameObject gameObject = changed switch
                {
                    GameObject value => value,
                    Component value => value.gameObject,
                    _ => null
                };
                if (gameObject != null && gameObject.TryGetComponent(out LayoutGreyboxBuilder builder))
                    Enforce(builder);
                else if (trace)
                    Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Resolved object did not lead to a GameObject with LayoutGreyboxBuilder.", selectedBuilder);
            }

            if (trace)
                Debug.Log($"[LayoutGreyboxScaleGuard DIAGNOSTIC] Callback complete; selected builder scale is {selected.transform.localScale}.", selectedBuilder);
        }
    }
}
