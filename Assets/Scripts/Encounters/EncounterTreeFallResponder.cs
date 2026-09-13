using System;
using System.Collections;
using UnityEngine;

namespace Macedon.Encounters
{
    [Serializable]
    public struct FallingTree
    {
        public Transform tree;
        [Tooltip("Local Euler rotation added to the tree's starting rotation.")]
        public Vector3 fallRotation;
        [Min(0f)] public float delay;
        [Min(0.01f)] public float duration;
    }

    public sealed class EncounterTreeFallResponder : MonoBehaviour
    {
        [SerializeField] private FallingTree[] trees = Array.Empty<FallingTree>();
        private bool hasPlayed;

        public void Play()
        {
            if (hasPlayed) return;
            hasPlayed = true;
            foreach (FallingTree tree in trees)
                if (tree.tree != null) StartCoroutine(Fall(tree));
        }

        private static IEnumerator Fall(FallingTree entry)
        {
            if (entry.delay > 0f) yield return new WaitForSeconds(entry.delay);
            Quaternion start = entry.tree.localRotation;
            Quaternion end = start * Quaternion.Euler(entry.fallRotation);
            float duration = Mathf.Max(0.01f, entry.duration), elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                entry.tree.localRotation = Quaternion.Slerp(start, end, eased);
                yield return null;
            }
            entry.tree.localRotation = end;
        }
    }
}
