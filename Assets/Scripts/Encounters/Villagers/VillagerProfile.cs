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

        public string DisplayName => displayName;
        public string[] AmbientDialogue => ambientDialogue;
        public string[] RecruitReadyDialogue => recruitReadyDialogue;
        public string[] FollowingDialogue => followingDialogue;

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
