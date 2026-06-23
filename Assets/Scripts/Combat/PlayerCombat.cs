using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private CombatMovementController controller;
    [SerializeField] private Collider punchHitbox;
    [SerializeField] private float punchLockDuration = 0.75f;
    [SerializeField] private float hitboxActiveDelay = 0.25f;
    [SerializeField] private float hitboxActiveDuration = 0.2f;

    private bool isPunching;
    private Health health;
    private CharacterController characterController;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (controller == null)
        {
            controller = GetComponent<CombatMovementController>();
        }

        if (punchHitbox != null)
        {
            punchHitbox.enabled = false;
        }

        characterController = GetComponent<CharacterController>();
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
        
        if (
            Mouse.current.leftButton.wasPressedThisFrame &&
            characterController.isGrounded
        )
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