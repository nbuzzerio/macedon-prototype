using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CombatIdentity))]
public class HumanoidCombatAI : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;

    private NavMeshAgent agent;
    private Health selfHealth;
    private CombatIdentity combatIdentity;
    private CombatIdentity targetIdentity;
    private Health targetHealth;

    private float nextTargetSearchTime;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();
        combatIdentity = GetComponent<CombatIdentity>();
        agent.stoppingDistance = attackRange;
    }

    private void Update()
    {
        if (selfHealth.IsDead)
        {
            StopAndDisable();
            return;
        }

        if (!HasValidTarget())
        {
            ClearTarget();
        }

        if (Time.time >= nextTargetSearchTime)
        {
            FindNearestTarget();
            nextTargetSearchTime = Time.time + targetSearchInterval;
        }

        if (!HasValidTarget())
        {
            StopMoving();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetIdentity.transform.position);

        if (distanceToTarget > attackRange)
        {
            ChaseTarget();
            return;
        }

        StopMoving();
        FaceTarget();
        AttackTarget();
    }

    private void FindNearestTarget()
    {
        CombatIdentity nearestIdentity = null;
        Health nearestHealth = null;
        float nearestDistanceSquared = detectionRange * detectionRange;

        CombatIdentity[] identities = FindObjectsByType<CombatIdentity>(FindObjectsSortMode.None);

        foreach (CombatIdentity candidateIdentity in identities)
        {
            if (candidateIdentity == combatIdentity || !combatIdentity.CanDamage(candidateIdentity))
            {
                continue;
            }

            Health candidateHealth = candidateIdentity.GetComponent<Health>();

            if (candidateHealth == null || candidateHealth.IsDead)
            {
                continue;
            }

            float distanceSquared =
                (candidateIdentity.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            nearestIdentity = candidateIdentity;
            nearestHealth = candidateHealth;
            nearestDistanceSquared = distanceSquared;
        }

        targetIdentity = nearestIdentity;
        targetHealth = nearestHealth;
    }

    private bool HasValidTarget()
    {
        return targetIdentity != null &&
            targetHealth != null &&
            !targetHealth.IsDead &&
            combatIdentity.CanDamage(targetIdentity);
    }

    private void ClearTarget()
    {
        targetIdentity = null;
        targetHealth = null;
    }

    private void ChaseTarget()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(targetIdentity.transform.position);
    }

    private void AttackTarget()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        targetHealth.TakeDamage(attackDamage, combatIdentity);
        nextAttackTime = Time.time + attackCooldown;
    }

    private void FaceTarget()
    {
        Vector3 direction = targetIdentity.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void StopMoving()
    {
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void StopAndDisable()
    {
        StopMoving();
        enabled = false;
    }
}
