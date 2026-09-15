using UnityEngine;
using UnityEngine.AI;

namespace Macedon.Villagers
{
    public enum VillagerMovementMode { FormationFollowing, Traversal, ReturningHome }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class VillagerFollower : MonoBehaviour
    {
        [Header("Party")]
        [SerializeField] private VillagerParty party;
        [Header("Movement")]
        [Min(0.1f)] [SerializeField] private float targetTolerance = 0.65f;
        [Min(0.05f)] [SerializeField] private float repathInterval = 0.2f;
        [Min(0.1f)] [SerializeField] private float initialNavMeshSearchRadius = 2f;
        [Min(0.1f)] [SerializeField] private float targetNavMeshSearchRadius = 1.5f;

        private readonly VillagerRecruitmentState recruitment = new();
        private readonly VillagerMovementOwnership movementOwnership = new();
        private NavMeshAgent agent;
        private int slot = -1;
        private float nextRepathTime;
        private bool placementWarningLogged;
        private bool targetWarningLogged;

        public VillagerRecruitmentStatus Status => recruitment.Status;
        public bool IsFollowing => Status == VillagerRecruitmentStatus.Following;
        public int Slot => slot;
        public VillagerMovementMode MovementMode => movementOwnership.Mode;
        public NavMeshAgent Agent => agent;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        private void OnEnable()
        {
            if (IsFollowing) RegisterWithParty();
        }

        private void OnDisable()
        {
            if (party != null) party.Unregister(this);
            slot = -1;
        }

        private void Update()
        {
            if (!IsFollowing || MovementMode != VillagerMovementMode.FormationFollowing || Time.time < nextRepathTime) return;
            nextRepathTime = Time.time + repathInterval;

            if (party == null || party.Player == null || slot < 0) return;
            if (!EnsureOnNavMesh()) return;

            Vector3 formationTarget = party.TargetForSlot(slot);
            if (!VillagerFormationLogic.ShouldMove(transform.position, formationTarget, targetTolerance))
            {
                StopAgent();
                return;
            }

            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(formationTarget, out NavMeshHit hit, targetNavMeshSearchRadius, filter))
            {
                if (!targetWarningLogged)
                {
                    Debug.LogWarning($"Follower '{name}' could not find compatible NavMesh near its formation target. It will wait and retry.", this);
                    targetWarningLogged = true;
                }
                StopAgent();
                return;
            }

            targetWarningLogged = false;
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        public bool TryRecruit(bool wolfCompleted)
        {
            if (IsFollowing || !wolfCompleted || party == null) return false;
            if (!RegisterWithParty()) return false;
            if (!recruitment.TryRecruit(true))
            {
                party.Unregister(this);
                slot = -1;
                return false;
            }

            EnsureOnNavMesh();
            nextRepathTime = 0f;
            return true;
        }

        public bool LeaveParty()
        {
            if (!recruitment.LeaveParty()) return false;
            if (party != null) party.Unregister(this);
            slot = -1;
            StopAgent();
            return true;
        }

        public bool TryAcquireTraversal()
        {
            if (!IsFollowing || MovementMode != VillagerMovementMode.FormationFollowing) return false;
            if (!movementOwnership.TryBeginTraversal()) return false;
            StopAgent();
            return true;
        }

        public bool TryReleaseTraversal()
        {
            bool movementReady = agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
            if (!movementOwnership.TryResumeFormation(movementReady)) return false;
            nextRepathTime = 0f;
            return true;
        }

        public bool BeginReturningHome()
        {
            if (!LeaveParty()) return false;
            movementOwnership.BeginReturningHome();
            return true;
        }

        public bool MarkArrivedHome()
        {
            if (!recruitment.ArriveHome()) return false;
            movementOwnership.FinishReturningHome();
            StopAgent();
            return true;
        }

        public bool TrySetAuthoredDestination(Vector3 target, float sampleRadius)
        {
            return TrySetAuthoredDestination(target, sampleRadius, out _);
        }

        public bool TrySetAuthoredDestination(Vector3 target, float sampleRadius, out Vector3 sampledDestination)
        {
            sampledDestination = target;
            if (!EnsureOnNavMesh()) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(target, out NavMeshHit hit, sampleRadius, filter)) return false;
            sampledDestination = hit.position;
            agent.isStopped = false;
            return agent.SetDestination(hit.position);
        }

        public void StopAuthoredMovement() => StopAgent();

        public bool TryWarpForDevelopment(Vector3 desiredPosition, float sampleRadius, out Vector3 placedPosition, out string error)
        {
            placedPosition = transform.position;
            error = null;
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                error = "NavMeshAgent is missing.";
                return false;
            }

            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, Mathf.Max(0.1f, sampleRadius), filter))
            {
                error = $"no compatible NavMesh within {sampleRadius:F1} m of {desiredPosition}.";
                return false;
            }

            Vector3 originalPosition = transform.position;
            bool originallyEnabled = agent.enabled;
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
            }
            if (agent.enabled) agent.enabled = false;
            transform.position = hit.position;
            agent.enabled = true;
            if (!agent.isOnNavMesh || !agent.Warp(hit.position))
            {
                agent.enabled = false;
                transform.position = originalPosition;
                agent.enabled = originallyEnabled;
                error = "NavMeshAgent.Warp failed; the original transform and enabled state were restored.";
                return false;
            }

            movementOwnership.RestoreFormationForDevelopment();
            agent.isStopped = false;
            nextRepathTime = 0f;
            placementWarningLogged = false;
            targetWarningLogged = false;
            placedPosition = hit.position;
            return true;
        }

        private bool RegisterWithParty()
        {
            if (party == null) return false;
            bool added = party.TryRegister(this, out int assignedSlot);
            slot = assignedSlot;
            return added || assignedSlot >= 0;
        }

        private bool EnsureOnNavMesh()
        {
            if (agent == null || !agent.isActiveAndEnabled) return false;
            if (agent.isOnNavMesh) return true;

            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, initialNavMeshSearchRadius, filter) && agent.Warp(hit.position))
            {
                placementWarningLogged = false;
                return agent.isOnNavMesh;
            }

            if (!placementWarningLogged)
            {
                Debug.LogWarning($"Follower '{name}' is not on compatible NavMesh and none was found within {initialNavMeshSearchRadius:F1} m. It will wait and retry without calling SetDestination.", this);
                placementWarningLogged = true;
            }
            return false;
        }

        private void StopAgent()
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            if (agent.hasPath) agent.ResetPath();
        }
    }
}
