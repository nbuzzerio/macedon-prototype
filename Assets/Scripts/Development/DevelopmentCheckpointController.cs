using System;
using System.Linq;
using Macedon.Encounters;
using Macedon.Villagers;
using StarterAssets;
using UnityEngine;

namespace Macedon.Development
{
    public sealed class DevelopmentCheckpointController : MonoBehaviour
    {
        [Header("Authoritative gameplay systems")]
        [SerializeField] private WolfQuest wolfQuest;
        [SerializeField] private WolfEncounterController wolfEncounter;
        [SerializeField] private Health wolfHealth;
        [SerializeField] private EncounterTreeFallResponder treeFall;
        [Tooltip("Optional. Applied when the scene has an authored encounter exit reveal.")]
        [SerializeField] private EncounterExitResponder exitReveal;
        [SerializeField] private VillagerParty party;
        [SerializeField] private VillagerFollower[] villagers = Array.Empty<VillagerFollower>();
        [SerializeField] private AuthoredFollowerTraversal riverTraversal;
        [Header("Checkpoint scene references")]
        [SerializeField] private DevelopmentCheckpointLocations locations;
        [Min(0.1f)] [SerializeField] private float followerNavMeshSampleRadius = 3f;

        public bool ApplyWolfDefeated()
        {
            Debug.Log("[Checkpoint] Applying Wolf Defeated.", this);
            if (!ValidateWolfReferences()) return false;
            wolfQuest.CompleteForDevelopment();
            wolfEncounter.CompleteForDevelopment();
            wolfHealth?.KillForDevelopment();
            treeFall.ApplyCompletedStateForDevelopment();
            exitReveal?.Reveal();
            wolfEncounter.ReleaseBoundary();
            Debug.Log("[Checkpoint] Wolf encounter marked complete.", this);
            return true;
        }

        public bool ApplyPartyRecruited()
        {
            if (party == null || villagers == null || villagers.Length != 3)
            {
                Debug.LogError("[Checkpoint] Party Recruited requires a VillagerParty and exactly three configured villagers.", this);
                return false;
            }
            if (villagers.Any(villager => villager == null))
            {
                Debug.LogError("[Checkpoint] Party Recruited has a missing villager reference.", this);
                return false;
            }
            if (!ApplyWolfDefeated()) return false;

            foreach (VillagerFollower villager in villagers)
            {
                if (!villager.IsFollowing && !villager.TryRecruit(true))
                {
                    Debug.LogError($"[Checkpoint] Failed to recruit {villager.name} through its recruitment API.", villager);
                    return false;
                }
            }

            var registered = party.FollowersInSlotOrder();
            bool valid = registered.Count == 3 && registered.Distinct().Count() == 3 &&
                         registered.All(follower => follower != null && follower.IsFollowing && follower.Slot >= 0) &&
                         registered.Select(follower => follower.Slot).Distinct().Count() == 3;
            if (!valid)
            {
                Debug.LogError("[Checkpoint] Party registry is not exactly three unique Following villagers with unique slots.", this);
                return false;
            }
            Debug.Log("[Checkpoint] Party Recruited: 3 followers registered.", this);
            return true;
        }

        public bool ApplyRiverStart()
        {
            if (locations == null || locations.RiverStart == null || riverTraversal == null || party == null || party.Player == null)
            {
                Debug.LogError("[Checkpoint] River Start requires Locations.RiverStart, River Traversal, and the party Player.", this);
                return false;
            }
            if (!ApplyPartyRecruited()) return false;

            Transform destination = locations.RiverStart;
            TeleportPlayer(party.Player, destination);
            foreach (VillagerFollower follower in party.FollowersInSlotOrder())
            {
                Vector3 desired = DevelopmentCheckpointPlacement.FollowerPosition(destination, follower.Slot);
                if (!follower.TryWarpForDevelopment(desired, followerNavMeshSampleRadius, out Vector3 placed, out string error))
                {
                    Debug.LogError($"[Checkpoint] Failed to place {follower.name} on NavMesh: {error}", follower);
                    return false;
                }
                Debug.Log($"[Checkpoint] {follower.name} warped to {placed}.", follower);
            }

            if (!riverTraversal.ResetForDevelopment(out string resetError))
            {
                Debug.LogError($"[Checkpoint] Failed to reset river traversal: {resetError}", riverTraversal);
                return false;
            }
            Debug.Log("[Checkpoint] River Start applied.", this);
            return true;
        }

        private bool ValidateWolfReferences()
        {
            if (wolfQuest != null && wolfEncounter != null && wolfHealth != null && treeFall != null) return true;
            Debug.LogError("[Checkpoint] Wolf Defeated requires Wolf Quest, Encounter, Wolf Health, and Tree Fall references.", this);
            return false;
        }

        private static void TeleportPlayer(Transform player, Transform destination)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            player.SetPositionAndRotation(destination.position, destination.rotation);
            if (wasEnabled) controller.enabled = true;

            StarterAssetsInputs input = player.GetComponent<StarterAssetsInputs>();
            if (input != null)
            {
                input.MoveInput(Vector2.zero);
                input.JumpInput(false);
                input.SprintInput(false);
            }
            Physics.SyncTransforms();
        }
    }
}
