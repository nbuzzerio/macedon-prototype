namespace Macedon.Villagers
{
    public enum VillagerRecruitmentStatus
    {
        Unrecruited,
        Following
    }

    public sealed class VillagerRecruitmentState
    {
        public VillagerRecruitmentStatus Status { get; private set; }

        public bool TryRecruit(bool wolfCompleted)
        {
            if (!wolfCompleted || Status == VillagerRecruitmentStatus.Following) return false;
            Status = VillagerRecruitmentStatus.Following;
            return true;
        }

        public bool LeaveParty()
        {
            if (Status != VillagerRecruitmentStatus.Following) return false;
            Status = VillagerRecruitmentStatus.Unrecruited;
            return true;
        }
    }
}
