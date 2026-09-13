using UnityEngine;

namespace Macedon.Villagers
{
    public static class VillagerFormationLogic
    {
        public static Vector3 LocalOffsetForSlot(int slot)
        {
            switch (slot)
            {
                case 0: return new Vector3(-1.75f, 0f, -2.25f);
                case 1: return new Vector3(1.75f, 0f, -2.25f);
                case 2: return new Vector3(0f, 0f, -3.75f);
                default:
                    int row = slot / 2 + 1;
                    float side = slot % 2 == 0 ? -1f : 1f;
                    return new Vector3(side * 2.25f, 0f, -2.25f - row * 1.5f);
            }
        }

        public static Vector3 WorldTarget(Vector3 playerPosition, float playerYawDegrees, int slot) =>
            playerPosition + Quaternion.Euler(0f, playerYawDegrees, 0f) * LocalOffsetForSlot(slot);

        public static bool ShouldMove(Vector3 followerPosition, Vector3 targetPosition, float targetTolerance)
        {
            Vector3 difference = targetPosition - followerPosition;
            difference.y = 0f;
            float tolerance = Mathf.Max(0f, targetTolerance);
            return difference.sqrMagnitude > tolerance * tolerance;
        }
    }
}
