using UnityEngine;
using UnityEngine.AI;

public class WolfChase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform homePoint;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float leashRange = 25f;

    [Header("Roaming")]
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float roamInterval = 4f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float damageInterruptCooldown = 5f;

    [Header("Animation Timing")]
    [SerializeField] private float howlDuration = 2.5f;

    private NavMeshAgent agent;
    private Health selfHealth;
    private Animator animator;

    private Vector3 spawnPosition;
    private float nextRoamTime;
    private float nextAttackTime;
    private float nextDamageInterruptTime;

    private bool hasAggro;
    private bool hasHowled;
    private bool isHowling;
    private bool isDead;
    private bool isReturningHome;

    public bool HasAggro => hasAggro;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();
        animator = GetComponentInChildren<Animator>();

        spawnPosition = homePoint != null
            ? homePoint.position
            : transform.position;
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
        float distanceFromHome = Vector3.Distance(transform.position, spawnPosition);

        if (hasAggro && distanceFromHome > leashRange)
        {
            ResetAggro();
            isReturningHome = true;
            ReturnHome();
            return;
        }

        if (isReturningHome)
        {
            if (distanceFromHome > 2f)
            {
                ReturnHome();
                return;
            }

            isReturningHome = false;
        }

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
            FacePlayer();

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

    private void ResetAggro()
    {
        hasAggro = false;
        hasHowled = false;
        isHowling = false;
        CancelInvoke(nameof(FinishHowl));
    }

    private void ReturnHome()
    {
        SetAnimationState(true, false);
        agent.SetDestination(spawnPosition);
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

    private void FacePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude <= 0.01f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(directionToPlayer);
    }

    public void PlayDamageReaction()
    {
        if (isDead || animator == null)
        {
            return;
        }

        if (Time.time < nextDamageInterruptTime)
        {
            return;
        }

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Damage");

        nextAttackTime = Time.time + attackCooldown;
        nextDamageInterruptTime = Time.time + damageInterruptCooldown;
    }
}