using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private WolfQuest wolfQuest;
    [SerializeField] private TextMeshProUGUI dialogueText;
    public GameObject talkPromptUI;
    public GameObject dialogueUI;

    private bool playerInRange;
    private bool dialogueOpen;

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
        if (wolfQuest == null || dialogueText == null)
        {
            return;
        }

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
