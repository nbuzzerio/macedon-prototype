using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool destroyOnDeath = true;

    private int currentHealth;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        WolfChase wolfChase = GetComponent<WolfChase>();

        if (wolfChase != null && currentHealth > 0)
        {
            wolfChase.PlayDamageReaction();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log($"{gameObject.name} died.");

        DeathFall deathFall = GetComponent<DeathFall>();

        if (deathFall != null)
        {
            deathFall.PlayFall();
        }

        Collider collider = GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }

        StarterAssets.ThirdPersonController playerController =
    GetComponent<StarterAssets.ThirdPersonController>();

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }
}