using UnityEngine;

namespace Macedon.Villagers
{
    [CreateAssetMenu(fileName = "VillagerProfile", menuName = "MACEDON/Villager Profile")]
    public sealed class VillagerProfile : ScriptableObject
    {
        [SerializeField] private string displayName = "Villager";
        [TextArea(2, 4)] [SerializeField] private string[] ambientDialogue = new string[0];
        [TextArea(2, 4)] [SerializeField] private string[] recruitReadyDialogue = new string[0];
        [TextArea(2, 4)] [SerializeField] private string[] followingDialogue = new string[0];
        [TextArea(2, 4)] [SerializeField] private string warning1Dialogue;
        [TextArea(2, 4)] [SerializeField] private string warning2Dialogue;
        [TextArea(2, 4)] [SerializeField] private string abandonDialogue;
        [TextArea(2, 4)] [SerializeField] private string rejoinDialogue;

        public string DisplayName => displayName;
        public string[] AmbientDialogue => ambientDialogue;
        public string[] RecruitReadyDialogue => recruitReadyDialogue;
        public string[] FollowingDialogue => followingDialogue;
        public string Warning1Dialogue => warning1Dialogue;
        public string Warning2Dialogue => warning2Dialogue;
        public string AbandonDialogue => abandonDialogue;
        public string RejoinDialogue => rejoinDialogue;

        public string[] DialogueFor(VillagerDialogueState state)
        {
            switch (state)
            {
                case VillagerDialogueState.RecruitReady: return recruitReadyDialogue;
                case VillagerDialogueState.Following: return followingDialogue;
                default: return ambientDialogue;
            }
        }
    }
}
