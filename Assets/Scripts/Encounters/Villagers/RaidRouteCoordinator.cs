using UnityEngine;

namespace Macedon.Villagers
{
    public sealed class RaidRouteCoordinator : MonoBehaviour
    {
        [SerializeField] private AuthoredFollowerTraversal[] validTraversals;
        private int playerVolumeCount;
        public bool IsPlayerOnRoute
        {
            get
            {
                if (playerVolumeCount > 0) return true;
                if (validTraversals != null)
                    foreach (AuthoredFollowerTraversal traversal in validTraversals)
                        if (traversal != null && traversal.IsRunning) return true;
                return false;
            }
        }
        public void EnterRouteVolume() => playerVolumeCount++;
        public void ExitRouteVolume() => playerVolumeCount = Mathf.Max(0, playerVolumeCount - 1);
    }
}
