namespace Macedon.Villagers
{
    public enum VillagerRecruitmentStatus
    {
        Unrecruited,
        Following,
        ReturningHome,
        RejoinReady
    }

    public sealed class VillagerRecruitmentState
    {
        public VillagerRecruitmentStatus Status { get; private set; }

        public bool TryRecruit(bool wolfCompleted)
        {
            if (!wolfCompleted || (Status != VillagerRecruitmentStatus.Unrecruited && Status != VillagerRecruitmentStatus.RejoinReady)) return false;
            Status = VillagerRecruitmentStatus.Following;
            return true;
        }

        public bool LeaveParty()
        {
            if (Status != VillagerRecruitmentStatus.Following) return false;
            Status = VillagerRecruitmentStatus.ReturningHome;
            return true;
        }

        public bool ArriveHome()
        {
            if (Status != VillagerRecruitmentStatus.ReturningHome) return false;
            Status = VillagerRecruitmentStatus.RejoinReady;
            return true;
        }
    }
}
