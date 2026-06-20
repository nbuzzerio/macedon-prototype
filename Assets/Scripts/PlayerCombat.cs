using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ThirdPersonController controller;
    [SerializeField] private float punchLockDuration = 0.75f;

    private bool isPunching;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (controller == null)
        {
            controller = GetComponent<ThirdPersonController>();
        }
    }

    private void Update()
    {
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

        yield return new WaitForSeconds(punchLockDuration);

        controller.enabled = true;
        isPunching = false;
    }
}