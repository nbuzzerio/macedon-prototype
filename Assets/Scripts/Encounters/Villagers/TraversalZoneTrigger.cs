using System.Collections.Generic;
using UnityEngine;

namespace Macedon.Villagers
{
    [RequireComponent(typeof(Collider))]
    public sealed class TraversalZoneTrigger : MonoBehaviour
    {
        [SerializeField] private AuthoredFollowerTraversal traversal;
        [SerializeField] private TraversalZone zone;
        private readonly HashSet<Collider> playerColliders = new();

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && playerColliders.Add(other) && playerColliders.Count == 1)
                traversal?.NotifyPlayerEntered(zone);
        }

        private void OnTriggerExit(Collider other)
        {
            if (playerColliders.Remove(other) && playerColliders.Count == 0)
                traversal?.NotifyPlayerExited(zone);
        }

        private void OnDisable()
        {
            if (playerColliders.Count > 0) traversal?.NotifyPlayerExited(zone);
            playerColliders.Clear();
        }
    }
}
