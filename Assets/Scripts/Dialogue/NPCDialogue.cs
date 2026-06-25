using UnityEngine;
using UnityEngine.InputSystem;

public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private WolfQuest wolfQuest;
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
        if (dialogueOpen && wolfQuest != null)
        {
            wolfQuest.StartQuest();
        }
        dialogueUI.SetActive(dialogueOpen);
        talkPromptUI.SetActive(!dialogueOpen);
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