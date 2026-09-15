using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Macedon.Characters;

namespace Macedon.Villagers
{
    public sealed class AuthoredFollowerTraversal : MonoBehaviour
    {
        private static int runtimeSessionId;
        [SerializeField] private VillagerParty party;
        [Tooltip("Safe root positions ordered from side A to side B, including takeoff and exit positions.")]
        [SerializeField] private Transform[] routePoints;
        [Min(0.1f)] [SerializeField] private float approachTolerance = 0.45f;
        [Min(0.5f)] [SerializeField] private float approachTimeout = 8f;
        [Min(0.1f)] [SerializeField] private float pointSampleRadius = 1.25f;
        [Min(0.1f)] [SerializeField] private float jumpDuration = 0.55f;
        [Min(0f)] [SerializeField] private float jumpArcHeight = 0.8f;
        [Min(0f)] [SerializeField] private float followerStartDelay = 0.35f;
        [Header("Development")]
        [SerializeField] private bool debugLogging = true;

        private readonly AuthoredTraversalState traversalState = new();
        private readonly List<VillagerFollower> armedFollowers = new();
        private readonly Dictionary<VillagerFollower, Vector3> stagingDestinations = new();
        private float nextGatherRepathTime;
        private bool configurationWarningLogged;
        private readonly HashSet<VillagerFollower> ownedFollowers = new();
        private readonly HashSet<VillagerFollower> recoveryPending = new();
        private bool cancellationRequested;
        private bool queueRunning;
        private VillagerFollower activeFollower;
        private int initializedRuntimeSessionId = -1;
        public bool IsRunning => traversalState.Phase != AuthoredTraversalPhase.Idle;
        public AuthoredTraversalPhase Phase => traversalState.Phase;

        public bool ResetForDevelopment(out string error)
        {
            StopAllCoroutines();
            var followers = new HashSet<VillagerFollower>(ownedFollowers);
            foreach (VillagerFollower follower in armedFollowers) if (follower != null) followers.Add(follower);
            foreach (VillagerFollower follower in recoveryPending) if (follower != null) followers.Add(follower);
            foreach (VillagerFollower follower in followers)
            {
                if (follower == null || follower.MovementMode != VillagerMovementMode.Traversal) continue;
                if (!TryRestoreFormation(follower))
                {
                    error = $"could not restore '{follower.name}' to formation ownership.";
                    return false;
                }
            }

            armedFollowers.Clear();
            stagingDestinations.Clear();
            ownedFollowers.Clear();
            recoveryPending.Clear();
            cancellationRequested = false;
            queueRunning = false;
            activeFollower = null;
            nextGatherRepathTime = 0f;
            traversalState.Reset();
            error = null;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginRuntimeSession() => runtimeSessionId++;

        private void Awake() => EnsureRuntimeSessionInitialized();

        private void Update()
        {
            if (traversalState.Phase != AuthoredTraversalPhase.Staging &&
                traversalState.Phase != AuthoredTraversalPhase.Crossing) return;
            if (Time.time < nextGatherRepathTime) return;
            nextGatherRepathTime = Time.time + 0.2f;

            Transform staging = routePoints[0];
            foreach (VillagerFollower follower in armedFollowers)
            {
                if (follower == null || !follower.IsFollowing || follower.MovementMode != VillagerMovementMode.Traversal) continue;
                if (stagingDestinations.TryGetValue(follower, out Vector3 sampledStaging))
                {
                    if (AuthoredTraversalLogic.HasArrived(follower.transform.position, sampledStaging, approachTolerance))
                        follower.StopAuthoredMovement();
                    continue;
                }

                if (follower.TrySetAuthoredDestination(staging.position, pointSampleRadius, out sampledStaging))
                {
                    stagingDestinations[follower] = sampledStaging;
                    LogEntrySample(follower, "staging", staging.position, sampledStaging);
                }
            }
        }

        public void NotifyPlayerEntered(TraversalZone zone)
        {
            EnsureRuntimeSessionInitialized();
            bool routeValid = HasValidRoute();
            Log($"Enter Zone {zone} requested while phase={traversalState.Phase}, routeValid={routeValid}, " +
                $"owned={ownedFollowers.Count}, recovery={recoveryPending.Count}, queueRunning={queueRunning}.");
            Log($"Player entered Zone {zone}.");
            if (!routeValid)
            {
                Log($"Zone {zone} entry rejected because route validation failed: {RouteValidationDetails()}.");
                if (!configurationWarningLogged)
                {
                    Debug.LogWarning($"[Traversal] '{name}' route validation failed: {RouteValidationDetails()}.", this);
                    configurationWarningLogged = true;
                }
                return;
            }

            AuthoredTraversalPhase before = traversalState.Phase;
            if (!traversalState.Enter(zone))
            {
                Log($"Zone {zone} entry rejected while phase={before}.");
                return;
            }

            if (before == AuthoredTraversalPhase.Idle && traversalState.Phase == AuthoredTraversalPhase.Staging)
            {
                Log("Staging followers.");
                ArmFollowers();
                return;
            }
            if (traversalState.Phase == AuthoredTraversalPhase.Crossing)
            {
                Log("Crossing corridor active; followers remain staged.");
                return;
            }
            if (traversalState.Phase == AuthoredTraversalPhase.Running)
            {
                Log("Player reached Zone C; starting queued traversal.");
                queueRunning = true;
                StartCoroutine(TraverseParty());
                return;
            }
            if (traversalState.Phase == AuthoredTraversalPhase.Cancelling) RequestCancellation();
        }

        public void NotifyPlayerExited(TraversalZone zone)
        {
            Log($"Player exited Zone {zone}.");
            traversalState.Exit(zone);
        }

        private void ArmFollowers()
        {
            armedFollowers.Clear();
            stagingDestinations.Clear();
            foreach (VillagerFollower follower in party.FollowersInSlotOrder())
            {
                if (follower == null || !follower.IsFollowing || !follower.TryAcquireTraversal()) continue;
                armedFollowers.Add(follower);
                ownedFollowers.Add(follower);
            }
            var names = new List<string>();
            foreach (VillagerFollower follower in armedFollowers) names.Add(follower.name);
            Log($"Captured {armedFollowers.Count} follower(s): {(names.Count == 0 ? "<none>" : string.Join(", ", names))}.");
            nextGatherRepathTime = 0f;
        }

        private void RequestCancellation()
        {
            if (traversalState.Phase != AuthoredTraversalPhase.Cancelling && !traversalState.RequestCancellation()) return;
            if (!cancellationRequested) Log("Cancellation requested.");
            cancellationRequested = true;
            if (!queueRunning) CancelRemainingFollowers();
        }

        private void CancelRemainingFollowers()
        {
            foreach (VillagerFollower follower in new List<VillagerFollower>(armedFollowers))
            {
                if (follower == null || follower == activeFollower) continue;
                follower.StopAuthoredMovement();
                if (follower.TryReleaseTraversal()) ownedFollowers.Remove(follower);
                else if (recoveryPending.Add(follower)) StartCoroutine(RetryRestoration(follower));
            }
            armedFollowers.Clear();
            stagingDestinations.Clear();
            traversalState.Complete(recoveryPending.Count > 0);
            Log(recoveryPending.Count > 0
                ? $"Traversal cancelled with {recoveryPending.Count} follower(s) in RecoveryPending."
                : "Traversal cancelled; formation resumed and state returned Idle.");
            cancellationRequested = false;
        }

        private IEnumerator TraverseParty()
        {
            foreach (VillagerFollower follower in new List<VillagerFollower>(armedFollowers))
            {
                if (cancellationRequested || !traversalState.CanStartNextFollower) break;
                if (follower != null && follower.IsFollowing && follower.MovementMode == VillagerMovementMode.Traversal)
                {
                    activeFollower = follower;
                    Log($"Follower '{follower.name}' beginning traversal.");
                    yield return TraverseFollower(follower, TraversalSide.A);
                    activeFollower = null;
                }
                if (cancellationRequested || !traversalState.CanStartNextFollower) break;
                if (followerStartDelay > 0f) yield return new WaitForSeconds(followerStartDelay);
            }
            queueRunning = false;
            activeFollower = null;
            if (cancellationRequested)
            {
                CancelRemainingFollowers();
                yield break;
            }
            armedFollowers.Clear();
            stagingDestinations.Clear();
            traversalState.Complete(recoveryPending.Count > 0);
            Log(recoveryPending.Count > 0
                ? $"Traversal entered RecoveryPending for {recoveryPending.Count} follower(s)."
                : "Traversal completed and returned Idle.");
        }

        private IEnumerator TraverseFollower(VillagerFollower follower, TraversalSide from)
        {
            var order = new List<int>(AuthoredTraversalLogic.PointOrder(routePoints.Length, from));
            Transform first = routePoints[order[0]];
            stagingDestinations.Remove(follower);
            if (!follower.TrySetAuthoredDestination(first.position, pointSampleRadius, out Vector3 sampledEntry))
            {
                Fail(follower, "could not reach the authored traversal entry");
                yield break;
            }
            LogEntrySample(follower, "approach", first.position, sampledEntry);

            float approachElapsed = 0f;
            while (!AuthoredTraversalLogic.HasArrived(follower.transform.position, sampledEntry, approachTolerance))
            {
                if (follower.Agent == null || !follower.Agent.isActiveAndEnabled || !follower.Agent.isOnNavMesh)
                { Fail(follower, "lost its NavMesh placement approaching the traversal"); yield break; }
                approachElapsed += Time.deltaTime;
                if (approachElapsed >= approachTimeout)
                { Fail(follower, $"timed out after {approachTimeout:F1}s approaching the sampled traversal entry"); yield break; }
                if (approachElapsed > 0.1f && !follower.Agent.pathPending && follower.Agent.pathStatus == NavMeshPathStatus.PathInvalid)
                { Fail(follower, "received an invalid NavMesh path to the sampled traversal entry"); yield break; }
                yield return null;
            }

            follower.StopAuthoredMovement();
            NpcLocomotionAnimator animationDriver = follower.GetComponent<NpcLocomotionAnimator>();
            NavMeshAgent agent = follower.Agent;
            if (agent != null && agent.enabled) agent.enabled = false;
            for (int i = 1; i < order.Count; i++)
            {
                Vector3 start = follower.transform.position;
                Vector3 end = routePoints[order[i]].position;
                Quaternion turnStart = follower.transform.rotation;
                Quaternion travelRotation = AuthoredTraversalLogic.TravelRotation(start, end, turnStart);
                Log($"Follower '{follower.name}' jump {i}/{order.Count - 1}: {start} -> {end}.");
                animationDriver?.BeginJump();
                float elapsed = 0f;
                float duration = Mathf.Max(0.1f, jumpDuration);
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed > duration * 0.25f) animationDriver?.SetAirborne();
                    float normalizedTime = elapsed / duration;
                    follower.transform.position = AuthoredTraversalLogic.JumpPosition(start, end, normalizedTime, jumpArcHeight);
                    follower.transform.rotation = Quaternion.Slerp(turnStart, travelRotation, Mathf.Clamp01(normalizedTime / 0.2f));
                    yield return null;
                }
                follower.transform.position = end;
                follower.transform.rotation = travelRotation;
                animationDriver?.CompleteLanding();
                if (cancellationRequested && TryRestoreFormation(follower))
                {
                    Log($"Follower '{follower.name}' finished its current jump and safely cancelled traversal.");
                    yield break;
                }
                yield return null;
            }

            Vector3 exit = follower.transform.position;
            if (agent == null || !NavMesh.SamplePosition(exit, out NavMeshHit hit, pointSampleRadius,
                    new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask }))
            { Fail(follower, "could not restore NavMesh placement at the authored exit"); yield break; }
            follower.transform.position = hit.position;
            agent.enabled = true;
            if (!agent.isOnNavMesh || !agent.Warp(hit.position))
            { Fail(follower, "could not synchronize its agent at the authored exit"); yield break; }
            if (follower.TryReleaseTraversal())
            {
                ownedFollowers.Remove(follower);
                Log($"Follower '{follower.name}' completed traversal and resumed formation following.");
            }
        }

        private void Fail(VillagerFollower follower, string reason)
        {
            follower.GetComponent<NpcLocomotionAnimator>()?.CompleteLanding();
            bool restored = TryRestoreFormation(follower);
            if (!restored && recoveryPending.Add(follower)) StartCoroutine(RetryRestoration(follower));
            string recovery = restored ? "Formation following was safely restored." : "Traversal ownership is retained while local NavMesh restoration retries.";
            Debug.LogWarning($"[Traversal] '{name}': follower '{follower.name}' {reason}. {recovery}", this);
            if (!restored) Log($"Follower '{follower.name}' entered RecoveryPending.");
        }

        private bool TryRestoreFormation(VillagerFollower follower)
        {
            if (follower == null) return false;
            NavMeshAgent agent = follower.Agent;
            if (agent == null) return false;
            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(follower.transform.position, out NavMeshHit hit, pointSampleRadius, filter)) return false;
                if (agent.enabled) agent.enabled = false;
                follower.transform.position = hit.position;
                agent.enabled = true;
                if (!agent.isOnNavMesh || !agent.Warp(hit.position)) return false;
            }
            if (!follower.TryReleaseTraversal()) return false;
            ownedFollowers.Remove(follower);
            recoveryPending.Remove(follower);
            if (recoveryPending.Count == 0 && traversalState.Phase == AuthoredTraversalPhase.RecoveryPending)
            {
                traversalState.Reset();
                Log("Recovery completed; traversal returned Idle.");
            }
            return true;
        }

        private IEnumerator RetryRestoration(VillagerFollower follower)
        {
            while (follower != null && !TryRestoreFormation(follower)) yield return new WaitForSeconds(0.5f);
            recoveryPending.Remove(follower);
        }

        private void OnDisable()
        {
            foreach (VillagerFollower follower in new List<VillagerFollower>(ownedFollowers))
            {
                if (!TryRestoreFormation(follower)) recoveryPending.Add(follower);
            }
            armedFollowers.Clear();
            stagingDestinations.Clear();
            traversalState.Complete(recoveryPending.Count > 0);
            if (recoveryPending.Count > 0)
                Log($"Traversal disabled with {recoveryPending.Count} follower(s) in RecoveryPending.");
        }

        private void OnEnable()
        {
            EnsureRuntimeSessionInitialized();
            foreach (VillagerFollower follower in new List<VillagerFollower>(recoveryPending))
                if (follower != null) StartCoroutine(RetryRestoration(follower));
        }

        private void OnDrawGizmosSelected()
        {
            if (routePoints == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < routePoints.Length; i++)
            {
                if (routePoints[i] == null) continue;
                Gizmos.DrawSphere(routePoints[i].position, 0.15f);
                if (i > 0 && routePoints[i - 1] != null) Gizmos.DrawLine(routePoints[i - 1].position, routePoints[i].position);
            }
        }

        private bool HasValidRoute()
        {
            if (party == null || routePoints == null || routePoints.Length < 2) return false;
            foreach (Transform point in routePoints) if (point == null) return false;
            return true;
        }

        private string RouteValidationDetails()
        {
            if (party == null) return "Party is unassigned";
            if (routePoints == null) return "Route Points is null";
            if (routePoints.Length < 2) return $"Route Points has {routePoints.Length} element(s)";
            for (int i = 0; i < routePoints.Length; i++)
                if (routePoints[i] == null) return $"Route Points element {i} is null";
            return $"valid Party and {routePoints.Length} non-null route points";
        }

        private void EnsureRuntimeSessionInitialized()
        {
            if (initializedRuntimeSessionId == runtimeSessionId) return;

            StopAllCoroutines();
            var staleFollowers = new HashSet<VillagerFollower>(ownedFollowers);
            foreach (VillagerFollower follower in armedFollowers) if (follower != null) staleFollowers.Add(follower);
            foreach (VillagerFollower follower in recoveryPending) if (follower != null) staleFollowers.Add(follower);
            if (party != null)
                foreach (VillagerFollower follower in party.FollowersInSlotOrder())
                    if (follower != null && follower.MovementMode == VillagerMovementMode.Traversal) staleFollowers.Add(follower);

            var unresolved = new List<VillagerFollower>();
            foreach (VillagerFollower follower in staleFollowers)
                if (follower != null && follower.MovementMode == VillagerMovementMode.Traversal && !TryRestoreFormation(follower))
                    unresolved.Add(follower);

            armedFollowers.Clear();
            stagingDestinations.Clear();
            ownedFollowers.Clear();
            recoveryPending.Clear();
            foreach (VillagerFollower follower in unresolved)
            {
                ownedFollowers.Add(follower);
                recoveryPending.Add(follower);
            }
            cancellationRequested = false;
            queueRunning = false;
            activeFollower = null;
            nextGatherRepathTime = 0f;
            configurationWarningLogged = false;
            traversalState.ResetForRuntimeSession();
            if (recoveryPending.Count > 0) traversalState.Complete(true);
            initializedRuntimeSessionId = runtimeSessionId;
            Log($"Runtime session initialized: phase={traversalState.Phase}, armed=0, owned={ownedFollowers.Count}, " +
                $"recovery={recoveryPending.Count}, queueRunning=false.");
        }

        private void Log(string message)
        {
            if (debugLogging) Debug.Log($"[Traversal] {message}", this);
        }

        private void LogEntrySample(VillagerFollower follower, string phase, Vector3 authored, Vector3 sampled)
        {
            Log($"Follower '{follower.name}' {phase} entry: authored {authored}, sampled {sampled}, offset {Vector3.Distance(authored, sampled):F2}m.");
        }
    }
}
