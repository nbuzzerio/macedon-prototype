using UnityEngine;

namespace Macedon.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class NpcAnimationEventReceiver : MonoBehaviour
    {
        private NpcLocomotionAnimator locomotion;

        private void Awake() => locomotion = GetComponentInParent<NpcLocomotionAnimator>();

        private void OnFootstep(AnimationEvent animationEvent)
        {
            // Intentional receiver for shared Starter Assets clips; future hook for NPC footstep audio/effects.
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            // Traversal owns physical landing; this forwards the shared clip's presentation event.
            if (animationEvent.animatorClipInfo.weight > 0.5f)
                locomotion?.HandleLandingAnimationEvent();
        }
    }
}
