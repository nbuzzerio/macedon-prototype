using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Macedon.Encounters;
using Macedon.Villagers;

public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private WolfQuest wolfQuest;
    [Header("Recruitable Villager (optional)")]
    [SerializeField] private VillagerProfile villagerProfile;
    [SerializeField] private WolfEncounterController wolfEncounter;
    [SerializeField] private Transform home;
    [SerializeField] private VillagerFollower follower;
    [Header("Shared Dialogue UI")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    public GameObject talkPromptUI;
    public GameObject dialogueUI;

    private bool playerInRange;
    private bool dialogueOpen;
    private int lastAmbientDialogueIndex = -1;
    private int lastRecruitReadyDialogueIndex = -1;
    private int lastFollowingDialogueIndex = -1;

    public VillagerProfile Profile => villagerProfile;
    public Transform Home => home;
    public VillagerDialogueState CurrentVillagerState =>
        VillagerDialogueLogic.StateFor(IsWolfCompleted(), follower != null && follower.IsFollowing);

    private void Awake()
    {
        if (follower == null) follower = GetComponent<VillagerFollower>();
    }

    private void Update()
    {
        if (!playerInRange || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        dialogueOpen = !dialogueOpen;

        if (dialogueOpen)
        {
            UpdateDialogue();
        }

        dialogueUI.SetActive(dialogueOpen);
        talkPromptUI.SetActive(!dialogueOpen);
    }

    private void UpdateDialogue()
    {
        if (dialogueText == null)
        {
            return;
        }

        if (villagerProfile != null)
        {
            UpdateProfileDialogue();
            return;
        }

        if (wolfQuest == null) return;

        switch (wolfQuest.CurrentStage)
        {
            case WolfQuest.QuestStage.NotStarted:
                dialogueText.text =
                    "Be careful... I've heard wolves have been spotted beyond the trees.";
                wolfQuest.StartQuest();
                break;

            case WolfQuest.QuestStage.WolfActive:
                dialogueText.text =
                    "The wolf is still out there beyond the trees. Please be careful.";
                break;

            case WolfQuest.QuestStage.WolfDefeated:
                dialogueText.text =
                    "Thank you. That wolf had been troubling us for days. " +
                    "But we have a larger problem now. Raiders have taken over an old fort nearby " +
                    "and have been terrorizing the village.";
                wolfQuest.IntroduceRaiderThreat();
                break;

            case WolfQuest.QuestStage.RaiderThreatIntroduced:
                dialogueText.text =
                    "The raiders are still holding the old fort nearby. The village needs help.";
                break;
        }
    }

    private void UpdateProfileDialogue()
    {
        VillagerDialogueState state = CurrentVillagerState;
        string[] lines = villagerProfile.DialogueFor(state);
        int previous = state == VillagerDialogueState.Ambient
            ? lastAmbientDialogueIndex
            : state == VillagerDialogueState.RecruitReady
                ? lastRecruitReadyDialogueIndex
                : lastFollowingDialogueIndex;
        int next = VillagerDialogueLogic.NextDialogueIndex(lines == null ? 0 : lines.Length, previous);
        if (next < 0)
        {
            dialogueText.text = string.Empty;
            return;
        }

        dialogueText.text = lines[next];
        if (state == VillagerDialogueState.Ambient) lastAmbientDialogueIndex = next;
        else if (state == VillagerDialogueState.RecruitReady)
        {
            lastRecruitReadyDialogueIndex = next;
            if (follower != null) follower.TryRecruit(IsWolfCompleted());
        }
        else lastFollowingDialogueIndex = next;
    }

    private bool IsWolfCompleted()
    {
        if (wolfEncounter != null) return wolfEncounter.IsCompleted;
        return wolfQuest != null && wolfQuest.CurrentStage >= WolfQuest.QuestStage.WolfDefeated;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        talkPromptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        dialogueOpen = false;
        talkPromptUI.SetActive(false);
        dialogueUI.SetActive(false);
    }
}
