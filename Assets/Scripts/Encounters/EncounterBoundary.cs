using UnityEngine;

namespace Macedon.Encounters
{
    public sealed class EncounterBoundary : MonoBehaviour
    {
        [Tooltip("Only encounter-specific blockers. Do not assign permanent world collision.")]
        [SerializeField] private Collider[] blockers = System.Array.Empty<Collider>();

        public bool IsBlocking { get; private set; }

        public void SetEncounterActive(bool active)
        {
            IsBlocking = active;
            foreach (Collider blocker in blockers)
                if (blocker != null) blocker.enabled = active;
        }
    }
}
