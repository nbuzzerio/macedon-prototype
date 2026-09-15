using UnityEngine;

namespace Macedon.Villagers
{
    public static class DevelopmentCheckpointPlacement
    {
        private static readonly Vector3[] LocalOffsets =
        {
            new(-1.5f, 0f, -1.5f),
            new(0f, 0f, -2.25f),
            new(1.5f, 0f, -1.5f)
        };

        public static Vector3 FollowerPosition(Transform checkpoint, int slot) =>
            FollowerPosition(checkpoint.position, checkpoint.rotation, slot);

        public static Vector3 FollowerPosition(Vector3 position, Quaternion rotation, int slot)
        {
            int index = Mathf.Clamp(slot, 0, LocalOffsets.Length - 1);
            return position + rotation * LocalOffsets[index];
        }
    }
}
