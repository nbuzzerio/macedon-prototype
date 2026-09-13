using UnityEngine;

namespace Macedon.Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class NpcAnimationEventReceiver : MonoBehaviour
    {
        private void OnFootstep(AnimationEvent animationEvent)
        {
            // Intentional receiver for shared Starter Assets clips; future hook for NPC footstep audio/effects.
        }
    }
}
