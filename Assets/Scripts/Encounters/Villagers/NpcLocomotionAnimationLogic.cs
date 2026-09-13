using UnityEngine;

namespace Macedon.Characters
{
    public readonly struct NpcLocomotionValues
    {
        public readonly float BlendSpeed;
        public readonly float MotionSpeed;

        public NpcLocomotionValues(float blendSpeed, float motionSpeed)
        {
            BlendSpeed = blendSpeed;
            MotionSpeed = motionSpeed;
        }
    }

    public static class NpcLocomotionAnimationLogic
    {
        public static NpcLocomotionValues MapSpeed(
            Vector3 worldVelocity,
            float agentMaximumSpeed,
            float controllerMaximumBlendSpeed,
            float idleThreshold)
        {
            worldVelocity.y = 0f;
            float speed = worldVelocity.magnitude;
            if (speed <= Mathf.Max(0f, idleThreshold)) return new NpcLocomotionValues(0f, 0f);

            float maximumBlend = Mathf.Max(0f, controllerMaximumBlendSpeed);
            float blendSpeed = Mathf.Clamp(speed, 0f, maximumBlend);
            float motionSpeed = agentMaximumSpeed <= 0f ? 0f : Mathf.Clamp01(speed / agentMaximumSpeed);
            return new NpcLocomotionValues(blendSpeed, motionSpeed);
        }
    }
}
