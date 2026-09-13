using UnityEngine;

namespace Macedon.Encounters
{
    [RequireComponent(typeof(Collider))]
    public sealed class EncounterStartTrigger : MonoBehaviour
    {
        [SerializeField] private WolfEncounterController encounter = null;
        [SerializeField] private string actorTag = "Player";

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (encounter == null || !other.CompareTag(actorTag)) return;
            encounter.BeginEncounter();
        }
    }
}
