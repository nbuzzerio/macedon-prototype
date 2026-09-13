using UnityEngine;

namespace Macedon.PlayerTraversal
{
    public static class SteepSlopeRules
    {
        public static float SlopeAngle(Vector3 surfaceNormal)
        {
            if (surfaceNormal.sqrMagnitude <= Mathf.Epsilon) return 90f;
            return Vector3.Angle(surfaceNormal, Vector3.up);
        }

        public static bool IsTraversable(Vector3 surfaceNormal, float maxTraversableSlope) =>
            SlopeAngle(surfaceNormal) <= Mathf.Clamp(maxTraversableSlope, 0f, 89.9f);

        public static bool CanInitiateJump(bool isGrounded, bool hasSurface, Vector3 surfaceNormal, float maxTraversableSlope) =>
            isGrounded && (!hasSurface || IsTraversable(surfaceNormal, maxTraversableSlope));

        public static Vector3 SuppressUphillMovement(Vector3 movement, Vector3 severeSurfaceNormal)
        {
            Vector3 uphill = Vector3.ProjectOnPlane(-severeSurfaceNormal, Vector3.up);
            if (uphill.sqrMagnitude <= Mathf.Epsilon) return movement;
            uphill.Normalize();
            float uphillAmount = Vector3.Dot(movement, uphill);
            return uphillAmount > 0f ? movement - uphill * uphillAmount : movement;
        }

        public static Vector3 DownhillDirection(Vector3 severeSurfaceNormal)
        {
            Vector3 downhill = Vector3.ProjectOnPlane(severeSurfaceNormal, Vector3.up);
            return downhill.sqrMagnitude <= Mathf.Epsilon ? Vector3.zero : downhill.normalized;
        }
    }
}
