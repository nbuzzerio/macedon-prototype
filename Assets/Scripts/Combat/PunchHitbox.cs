using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log($"Punch hit: {other.gameObject.name}");

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}