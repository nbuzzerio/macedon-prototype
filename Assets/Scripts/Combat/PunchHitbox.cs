using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private CombatIdentity ownerIdentity;

    private void Awake()
    {
        ownerIdentity = GetComponentInParent<CombatIdentity>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage, ownerIdentity);
        }
    }
}
