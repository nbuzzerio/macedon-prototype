using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Macedon.Villagers
{
    public sealed class VillagerParty : MonoBehaviour
    {
        [Tooltip("The gameplay Player root whose position and facing define follower slots.")]
        [SerializeField] private Transform player;

        private readonly VillagerPartyRegistry<VillagerFollower> registry = new();

        public Transform Player => player;
        public int FollowerCount => registry.Count;

        public bool TryRegister(VillagerFollower follower, out int slot)
        {
            if (player == null)
            {
                slot = -1;
                Debug.LogWarning("VillagerParty needs an explicit Player Transform before followers can register.", this);
                return false;
            }

            return registry.Register(follower, out slot);
        }

        public bool Unregister(VillagerFollower follower) => registry.Unregister(follower);

        public IReadOnlyList<VillagerFollower> FollowersInSlotOrder() =>
            registry.Entries.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();

        public Vector3 TargetForSlot(int slot) =>
            VillagerFormationLogic.WorldTarget(player.position, player.eulerAngles.y, slot);
    }
}
