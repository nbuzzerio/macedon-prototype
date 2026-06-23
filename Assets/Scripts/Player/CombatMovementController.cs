using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(CharacterController))]
public class CombatMovementController : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 2f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float strafeSpeed = 2f;
    [SerializeField] private float backpedalSpeed = 1.1f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float lookSensitivity = 1f;
    [SerializeField] private float doubleTapWindow = 0.3f;

    private CharacterController controller;
    private StarterAssetsInputs input;
    private Animator animator;

    private float verticalVelocity;
    private float lastBackTapTime = -1f;
    private bool wasPressingBack;

    private int animIDSpeed;
    private int animIDGrounded;
    private int animIDJump;
    private int animIDFreeFall;
    private int animIDMotionSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<StarterAssetsInputs>();
        animator = GetComponent<Animator>();

        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void Update()
    {
        Vector2 moveInput = input.move;

        HandleAboutFace(moveInput);
        HandleRotation();
        HandleMovement(moveInput);
        UpdateAnimator(moveInput);
    }

    private void HandleRotation()
    {
        float mouseX = input.look.x * lookSensitivity;
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleAboutFace(Vector2 moveInput)
    {
        bool isPressingBack = moveInput.y < -0.5f;

        if (isPressingBack && !wasPressingBack)
        {
            if (Time.time - lastBackTapTime <= doubleTapWindow)
            {
                transform.Rotate(Vector3.up * 180f);
                lastBackTapTime = -1f;
            }
            else
            {
                lastBackTapTime = Time.time;
            }
        }

        wasPressingBack = isPressingBack;
    }

    private void HandleMovement(Vector2 moveInput)
    {
        Vector3 moveDirection =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float speed = GetMoveSpeed(moveInput);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (input.jump && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.SetBool(animIDJump, true);
            }

            input.jump = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            moveDirection * speed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private float GetMoveSpeed(Vector2 moveInput)
    {
        if (input.sprint && moveInput.y > 0.1f)
        {
            return sprintSpeed;
        }

        if (moveInput.y < -0.1f)
        {
            return backpedalSpeed;
        }

        if (Mathf.Abs(moveInput.x) > 0.1f && moveInput.y <= 0.1f)
        {
            return strafeSpeed;
        }

        return forwardSpeed;
    }

    private void UpdateAnimator(Vector2 moveInput)
    {
        if (animator == null)
        {
            return;
        }

        float animationSpeed = 0f;

        if (moveInput != Vector2.zero)
        {
            animationSpeed = input.sprint && moveInput.y > 0.1f
                ? sprintSpeed
                : forwardSpeed;
        }

        if (controller.isGrounded)
        {
            animator.SetBool(animIDJump, false);
        }

        animator.SetFloat(animIDSpeed, animationSpeed);
        animator.SetFloat(animIDMotionSpeed, moveInput.magnitude);
        animator.SetBool(animIDGrounded, controller.isGrounded);
        animator.SetBool(animIDFreeFall, !controller.isGrounded && verticalVelocity < -2f);
    }
}