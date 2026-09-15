using System.Collections.Generic;
using UnityEngine;

namespace Macedon.Villagers
{
    public enum TraversalSide { A, B }

    public static class AuthoredTraversalLogic
    {
        public static bool HasArrived(Vector3 position, Vector3 sampledDestination, float tolerance) =>
            Vector3.Distance(position, sampledDestination) <= Mathf.Max(0f, tolerance);

        public static Vector3 JumpPosition(Vector3 start, Vector3 end, float normalizedTime, float arcHeight)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return Vector3.Lerp(start, end, t) + Vector3.up * (4f * Mathf.Max(0f, arcHeight) * t * (1f - t));
        }

        public static Quaternion TravelRotation(Vector3 start, Vector3 end, Quaternion fallback)
        {
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(end - start, Vector3.up);
            return horizontalDirection.sqrMagnitude <= 0.000001f
                ? fallback
                : Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
        }

        public static IEnumerable<int> PointOrder(int count, TraversalSide from)
        {
            if (from == TraversalSide.A)
                for (int i = 0; i < count; i++) yield return i;
            else
                for (int i = count - 1; i >= 0; i--) yield return i;
        }
    }
}
