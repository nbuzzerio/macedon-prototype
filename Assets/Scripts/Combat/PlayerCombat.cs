using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonController controller;
    [SerializeField] private Collider punchHitbox;
    [SerializeField] private float punchLockDuration = 0.75f;
    [SerializeField] private float hitboxActiveDelay = 0.25f;
    [SerializeField] private float hitboxActiveDuration = 0.2f;

    private bool isPunching;
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (controller == null)
        {
            controller = GetComponent<ThirdPersonController>();
        }

        if (punchHitbox != null)
        {
            punchHitbox.enabled = false;
        }
    }

    private void Update()
    {
        if (health != null && health.CurrentHealth <= 0)
        {
            return;
        }

        if (isPunching)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(Punch());
        }
    }

    private IEnumerator Punch()
    {
        isPunching = true;
        controller.enabled = false;
        animator.SetTrigger("Punch");

        yield return new WaitForSeconds(hitboxActiveDelay);

        punchHitbox.enabled = true;

        yield return new WaitForSeconds(hitboxActiveDuration);

        punchHitbox.enabled = false;

        yield return new WaitForSeconds(punchLockDuration - hitboxActiveDelay - hitboxActiveDuration);

        if (health == null || health.CurrentHealth > 0)
        {
            controller.enabled = true;
        }

        isPunching = false;
    }
}