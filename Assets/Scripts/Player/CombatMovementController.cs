using UnityEngine;
using StarterAssets;
using Macedon.PlayerTraversal;

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
    [SerializeField] private bool invertVerticalLook = false;
    [SerializeField] private float minimumPitch = -35f;
    [SerializeField] private float maximumPitch = 65f;
    [SerializeField] private Transform cameraPitchTransform;
    [SerializeField] private float doubleTapWindow = 0.3f;

    [Header("Steep Slope Traversal")]
    [SerializeField, Range(1f, 89f)] private float maxTraversableSlope = 50f;
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.25f;
    [SerializeField, Min(0f)] private float steepSlopeSlideSpeed = 1.5f;
    [SerializeField, Min(0f)] private float severeContactMemory = 0.15f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private CharacterController controller;
    private StarterAssetsInputs input;
    private Animator animator;

    private float verticalVelocity;
    private float cameraPitch;
    private float cameraLocalYaw;
    private float lastBackTapTime = -1f;
    private bool wasPressingBack;
    private Vector3 currentSurfaceNormal = Vector3.up;
    private Vector3 recentSevereSurfaceNormal = Vector3.up;
    private bool hasCurrentSurface;
    private float lastSevereContactTime = float.NegativeInfinity;
    private readonly RaycastHit[] groundProbeHits = new RaycastHit[8];

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
        controller.slopeLimit = maxTraversableSlope;

        if (cameraPitchTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);
            cameraPitchTransform = childCamera != null
                ? childCamera.transform
                : Camera.main != null ? Camera.main.transform : null;
        }

        if (cameraPitchTransform != null)
        {
            Vector3 cameraAngles = cameraPitchTransform.localEulerAngles;
            cameraPitch = ClampPitch(Mathf.DeltaAngle(0f, cameraAngles.x), minimumPitch, maximumPitch);
            cameraLocalYaw = cameraAngles.y;
        }

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

        if (cameraPitchTransform == null)
        {
            return;
        }

        cameraPitch = AccumulatePitch(cameraPitch, input.look.y, lookSensitivity, invertVerticalLook, minimumPitch, maximumPitch);
        cameraPitchTransform.localRotation = Quaternion.Euler(cameraPitch, cameraLocalYaw, 0f);
    }

    public static float AccumulatePitch(float currentPitch, float mouseY, float sensitivity, bool invertVerticalLook, float minimum, float maximum)
    {
        float verticalDirection = invertVerticalLook ? -1f : 1f;
        return ClampPitch(currentPitch + mouseY * sensitivity * verticalDirection, minimum, maximum);
    }

    public static float ClampPitch(float pitch, float minimum, float maximum)
    {
        if (minimum > maximum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        return Mathf.Clamp(pitch, minimum, maximum);
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
        ProbeSurface();

        Vector3 moveDirection =
            transform.forward * moveInput.y +
            transform.right * moveInput.x;

        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float speed = GetMoveSpeed(moveInput);

        bool hasSevereSurface = TryGetSevereSurface(out Vector3 severeNormal);
        if (hasSevereSurface)
        {
            moveDirection = SteepSlopeRules.SuppressUphillMovement(moveDirection, severeNormal);
            moveDirection += SteepSlopeRules.DownhillDirection(severeNormal) * steepSlopeSlideSpeed / Mathf.Max(speed, 0.01f);
            if (verticalVelocity > 0f) verticalVelocity = 0f;
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (input.jump && SteepSlopeRules.CanInitiateJump(
                controller.isGrounded,
                hasCurrentSurface,
                currentSurfaceNormal,
                maxTraversableSlope) && !hasSevereSurface)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.SetBool(animIDJump, true);
            }

            input.jump = false;
        }

        if (input.jump && controller.isGrounded)
        {
            // Consume denied grounded jumps so holding/pressing into a severe face cannot queue a jump.
            input.jump = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            moveDirection * speed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void ProbeSurface()
    {
        float radius = Mathf.Max(0.01f, controller.radius * 0.9f);
        Vector3 center = transform.TransformPoint(controller.center);
        float halfHeight = Mathf.Max(controller.height * 0.5f, radius);
        Vector3 origin = center + Vector3.down * (halfHeight - radius);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            radius,
            Vector3.down,
            groundProbeHits,
            groundProbeDistance + controller.skinWidth,
            groundLayers,
            QueryTriggerInteraction.Ignore);
        hasCurrentSurface = false;
        float nearestDistance = float.PositiveInfinity;
        currentSurfaceNormal = Vector3.up;
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = groundProbeHits[index];
            if (hit.collider == controller || hit.distance >= nearestDistance) continue;
            hasCurrentSurface = true;
            nearestDistance = hit.distance;
            currentSurfaceNormal = hit.normal;
        }
    }

    private bool TryGetSevereSurface(out Vector3 surfaceNormal)
    {
        if (hasCurrentSurface && !SteepSlopeRules.IsTraversable(currentSurfaceNormal, maxTraversableSlope))
        {
            surfaceNormal = currentSurfaceNormal;
            return true;
        }

        if (Time.time - lastSevereContactTime <= severeContactMemory)
        {
            surfaceNormal = recentSevereSurfaceNormal;
            return true;
        }

        surfaceNormal = Vector3.up;
        return false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (SteepSlopeRules.IsTraversable(hit.normal, maxTraversableSlope)) return;
        recentSevereSurfaceNormal = hit.normal;
        lastSevereContactTime = Time.time;
        if (verticalVelocity > 0f) verticalVelocity = 0f;
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
