using System.Collections.Generic;
using UnityEngine;

namespace Macedon.Villagers
{
    [RequireComponent(typeof(Collider))]
    public sealed class RaidRouteVolume : MonoBehaviour
    {
        [SerializeField] private RaidRouteCoordinator coordinator;
        private readonly HashSet<Collider> playerColliders = new();
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && playerColliders.Add(other) && playerColliders.Count == 1) coordinator?.EnterRouteVolume();
        }
        private void OnTriggerExit(Collider other)
        {
            if (playerColliders.Remove(other) && playerColliders.Count == 0) coordinator?.ExitRouteVolume();
        }
        private void OnDisable()
        {
            if (playerColliders.Count > 0) coordinator?.ExitRouteVolume();
            playerColliders.Clear();
        }
    }
}
