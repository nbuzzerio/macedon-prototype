using UnityEngine;
using UnityEngine.AI;

public class WolfChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float roamInterval = 4f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;

    [Header("Animation Timing")]
    [SerializeField] private float howlDuration = 2.5f;

    private NavMeshAgent agent;
    private Health selfHealth;
    private Animator animator;

    private Vector3 spawnPosition;
    private float nextRoamTime;
    private float nextAttackTime;

    private bool hasAggro;
    private bool hasHowled;
    private bool isHowling;
    private bool isDead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
    }

    private void Update()
    {
        if (player == null || isDead)
        {
            return;
        }

        if (selfHealth != null && selfHealth.CurrentHealth <= 0)
        {
            Die();
            return;
        }

        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            StopWolf();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > detectionRange && !hasAggro)
        {
            Roam();
            return;
        }

        if (!hasAggro)
        {
            StartAggro();
            return;
        }

        if (isHowling)
        {
            StopWolf();
            return;
        }

        if (distanceToPlayer > attackRange)
        {
            ChasePlayer();
            return;
        }

        AttackPlayer(playerHealth);
    }

    private void StartAggro()
    {
        hasAggro = true;

        if (!hasHowled)
        {
            hasHowled = true;
            isHowling = true;
            StopWolf();

            if (animator != null)
            {
                animator.SetTrigger("Howl");
            }

            Invoke(nameof(FinishHowl), howlDuration);
        }
    }

    private void FinishHowl()
    {
        isHowling = false;
    }

    private void Roam()
    {
        if (Time.time >= nextRoamTime)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 roamTarget = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            agent.SetDestination(roamTarget);
            nextRoamTime = Time.time + roamInterval;
        }

        SetAnimationState(agent.velocity.magnitude > 0.1f, false);
    }

    private void ChasePlayer()
    {
        SetAnimationState(true, true);
        agent.SetDestination(player.position);
    }

    private void AttackPlayer(Health playerHealth)
    {
        StopWolf();

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        Debug.Log("Wolf attacks!");

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private void Die()
    {
        isDead = true;
        StopWolf();

        if (agent.enabled)
        {
            agent.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        enabled = false;
    }

    private void StopWolf()
    {
        if (agent.enabled)
        {
            agent.ResetPath();
        }

        SetAnimationState(false, false);
    }

    private void SetAnimationState(bool isMoving, bool isChasing)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsChasing", isChasing);
    }
}