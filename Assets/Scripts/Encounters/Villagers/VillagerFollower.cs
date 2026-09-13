using UnityEngine;
using UnityEngine.AI;

namespace Macedon.Villagers
{
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
        private NavMeshAgent agent;
        private int slot = -1;
        private float nextRepathTime;
        private bool placementWarningLogged;
        private bool targetWarningLogged;

        public VillagerRecruitmentStatus Status => recruitment.Status;
        public bool IsFollowing => Status == VillagerRecruitmentStatus.Following;
        public int Slot => slot;

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
            if (!IsFollowing || Time.time < nextRepathTime) return;
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
