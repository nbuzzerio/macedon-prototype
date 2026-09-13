using UnityEngine;
using UnityEngine.Events;

namespace Macedon.Encounters
{
    public sealed class WolfEncounterController : MonoBehaviour
    {
        [SerializeField] private bool beginOnEnable = false;
        [SerializeField] private EncounterBoundary boundary = null;
        [SerializeField] private UnityEvent onEncounterBegan = new();
        [SerializeField] private UnityEvent onEncounterCompleted = new();

        private readonly WolfEncounterState state = new();

        public WolfEncounterPhase Phase => state.Phase;
        public bool IsActive => Phase == WolfEncounterPhase.Active;
        public bool IsCompleted => Phase == WolfEncounterPhase.Completed;
        public event UnityAction EncounterBegan;
        public event UnityAction EncounterCompleted;

        private void Awake()
        {
            if (boundary != null) boundary.SetEncounterActive(false);
        }

        private void OnEnable()
        {
            if (beginOnEnable) BeginEncounter();
        }

        public void BeginEncounter()
        {
            if (!state.Begin()) return;
            if (boundary != null) boundary.SetEncounterActive(true);
            onEncounterBegan.Invoke();
            EncounterBegan?.Invoke();
        }

        public void CompleteEncounter()
        {
            if (!state.Complete()) return;
            if (boundary != null) boundary.SetEncounterActive(false);
            onEncounterCompleted.Invoke();
            EncounterCompleted?.Invoke();
        }

        // Wire Health.onDeath to this method. Kept semantic so a future authority layer can own confirmation.
        public void HandleWolfDeath() => CompleteEncounter();
    }
}
