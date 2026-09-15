namespace Macedon.Villagers
{
    public sealed class VillagerMovementOwnership
    {
        public VillagerMovementMode Mode { get; private set; } = VillagerMovementMode.FormationFollowing;

        public bool TryBeginTraversal()
        {
            if (Mode != VillagerMovementMode.FormationFollowing) return false;
            Mode = VillagerMovementMode.Traversal;
            return true;
        }

        public bool TryResumeFormation(bool movementReady)
        {
            if (Mode != VillagerMovementMode.Traversal || !movementReady) return false;
            Mode = VillagerMovementMode.FormationFollowing;
            return true;
        }

        public void BeginReturningHome() => Mode = VillagerMovementMode.ReturningHome;
        public void FinishReturningHome() => Mode = VillagerMovementMode.FormationFollowing;

        public void RestoreFormationForDevelopment() => Mode = VillagerMovementMode.FormationFollowing;
    }
}
