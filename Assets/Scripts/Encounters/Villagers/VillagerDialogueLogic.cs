namespace Macedon.Villagers
{
    public enum VillagerDialogueState
    {
        Ambient,
        RecruitReady
    }

    public static class VillagerDialogueLogic
    {
        public static VillagerDialogueState StateForWolfCompletion(bool wolfCompleted) =>
            wolfCompleted ? VillagerDialogueState.RecruitReady : VillagerDialogueState.Ambient;

        public static int NextDialogueIndex(int lineCount, int previousIndex)
        {
            if (lineCount <= 0) return -1;
            if (lineCount == 1) return 0;
            return previousIndex < 0 || previousIndex >= lineCount - 1 ? 0 : previousIndex + 1;
        }
    }
}
