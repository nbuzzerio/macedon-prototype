using UnityEngine;
using UnityEngine.AI;

namespace Macedon.Villagers
{
    [RequireComponent(typeof(VillagerFollower))]
    public sealed class VillagerRouteDeviation : MonoBehaviour
    {
        [SerializeField] private RaidRouteCoordinator route;
        [SerializeField] private VillagerProfile profile;
        [SerializeField] private Transform home;
        [SerializeField] private AutomaticDialogueQueue dialogueQueue;
        [Min(0f)] [SerializeField] private float warning1After = 5f;
        [Min(0f)] [SerializeField] private float warning2After = 8f;
        [Min(0f)] [SerializeField] private float abandonAfter = 10f;
        [Min(0.1f)] [SerializeField] private float homeSampleRadius = 2f;
        [Min(0.1f)] [SerializeField] private float homeArrivalTolerance = 1f;

        private readonly RouteDeviationState deviation = new();
        private VillagerFollower follower;
        private bool returnFailureLogged;
        private bool configurationValid;

        private void Awake()
        {
            follower = GetComponent<VillagerFollower>();
            configurationValid = route != null && profile != null && home != null && dialogueQueue != null;
            if (!configurationValid)
                Debug.LogWarning($"Villager route deviation on '{name}' requires Route, Profile, Home, and Dialogue Queue references; deviation is disabled.", this);
        }

        private void Update()
        {
            if (follower.Status == VillagerRecruitmentStatus.ReturningHome) { UpdateReturnHome(); return; }
            bool eligible = configurationValid && follower.IsFollowing && follower.MovementMode != VillagerMovementMode.Traversal;
            RouteDeviationStage transition = deviation.Update(eligible, route != null && route.IsPlayerOnRoute,
                Time.deltaTime, warning1After, warning2After, abandonAfter);
            if (transition == RouteDeviationStage.Warning1) dialogueQueue?.Enqueue(profile?.Warning1Dialogue);
            else if (transition == RouteDeviationStage.Warning2) dialogueQueue?.Enqueue(profile?.Warning2Dialogue);
            else if (transition == RouteDeviationStage.Abandon)
            {
                dialogueQueue?.Enqueue(profile?.AbandonDialogue);
                follower.BeginReturningHome();
            }
        }

        private void UpdateReturnHome()
        {
            if (home == null || follower.Agent == null) return;
            if (Vector3.Distance(transform.position, home.position) <= homeArrivalTolerance)
            { follower.MarkArrivedHome(); returnFailureLogged = false; return; }
            if (follower.TrySetAuthoredDestination(home.position, homeSampleRadius)) { returnFailureLogged = false; return; }
            if (!returnFailureLogged)
            { Debug.LogWarning($"Returning villager '{name}' could not find compatible NavMesh near Home and will retry.", this); returnFailureLogged = true; }
        }
    }
}
