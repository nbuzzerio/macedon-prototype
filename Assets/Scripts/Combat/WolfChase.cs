using UnityEngine;
using UnityEngine.AI;

public class WolfChase : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;

    private NavMeshAgent agent;
    private Health selfHealth;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<Health>();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (selfHealth != null && selfHealth.CurrentHealth <= 0)
        {
            agent.ResetPath();
            agent.enabled = false;
            enabled = false;
            return;
        }

        Health playerHealth = player.GetComponent<Health>();

        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            agent.ResetPath();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            agent.SetDestination(player.position);
            return;
        }

        agent.ResetPath();

        if (Time.time >= nextAttackTime)
        {
            Debug.Log("Wolf attacks!");

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            nextAttackTime = Time.time + attackCooldown;
        }
    }
}