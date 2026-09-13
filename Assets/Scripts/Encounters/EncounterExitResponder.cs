using System;
using UnityEngine;

namespace Macedon.Encounters
{
    public sealed class EncounterExitResponder : MonoBehaviour
    {
        [SerializeField] private GameObject[] enableOnCompletion = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] disableOnCompletion = Array.Empty<GameObject>();
        [SerializeField] private Collider[] disableCollisionOnCompletion = Array.Empty<Collider>();
        private bool applied;

        public void Reveal()
        {
            if (applied) return;
            applied = true;
            foreach (GameObject target in enableOnCompletion) if (target != null) target.SetActive(true);
            foreach (GameObject target in disableOnCompletion) if (target != null) target.SetActive(false);
            foreach (Collider target in disableCollisionOnCompletion) if (target != null) target.enabled = false;
        }
    }
}
