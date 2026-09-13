using UnityEngine;
using UnityEngine.AI;

namespace Macedon.Characters
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcLocomotionAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FreeFallId = Animator.StringToHash("FreeFall");

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [Tooltip("The single Animator under the presentation child, normally Varangian_PlayerVisual.")]
        [SerializeField] private Animator animator;
        [Tooltip("Assigned only when the visual Animator has no controller. Use StarterAssetsThirdPerson for the current Varangian.")]
        [SerializeField] private RuntimeAnimatorController fallbackController;

        [Header("Locomotion Mapping")]
        [Tooltip("The Starter Assets locomotion blend tree reaches its run clip at Speed 6.")]
        [Min(0f)] [SerializeField] private float controllerMaximumBlendSpeed = 6f;
        [Min(0f)] [SerializeField] private float idleVelocityThreshold = 0.05f;
        [Min(0f)] [SerializeField] private float speedDampTime = 0.12f;

        private bool configurationWarningLogged;

        private void Reset()
        {
            agent = GetComponent<NavMeshAgent>();
            TryResolveUniqueChildAnimator();
        }

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) TryResolveUniqueChildAnimator();

            if (animator != null)
            {
                if (animator.runtimeAnimatorController == null && fallbackController != null)
                    animator.runtimeAnimatorController = fallbackController;

                // NavMeshAgent owns translation/rotation; animation is presentation only.
                animator.applyRootMotion = false;
            }

            ValidateConfiguration();
        }

        private void Update()
        {
            if (agent == null || animator == null || animator.runtimeAnimatorController == null) return;

            NpcLocomotionValues values = NpcLocomotionAnimationLogic.MapSpeed(
                agent.velocity,
                agent.speed,
                controllerMaximumBlendSpeed,
                idleVelocityThreshold);

            animator.SetFloat(SpeedId, values.BlendSpeed, speedDampTime, Time.deltaTime);
            animator.SetFloat(MotionSpeedId, values.MotionSpeed, speedDampTime, Time.deltaTime);
            bool grounded = agent.isActiveAndEnabled && agent.isOnNavMesh;
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(JumpId, false);
            animator.SetBool(FreeFallId, false);
        }

        private void TryResolveUniqueChildAnimator()
        {
            Animator[] candidates = GetComponentsInChildren<Animator>(true);
            if (candidates.Length == 1) animator = candidates[0];
        }

        private void ValidateConfiguration()
        {
            if (configurationWarningLogged) return;
            if (agent != null && animator != null && animator.runtimeAnimatorController != null) return;

            string problem = agent == null
                ? "has no NavMeshAgent"
                : animator == null
                    ? "needs an explicit Animator reference (automatic discovery only binds when exactly one child Animator exists)"
                    : "has no Animator Controller; assign StarterAssetsThirdPerson as the fallback or on the Animator";
            Debug.LogWarning($"NPC locomotion animator on '{name}' {problem}.", this);
            configurationWarningLogged = true;
        }
    }
}
