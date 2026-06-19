using TMPro;
using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    public GameObject dialogueUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogueUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            dialogueUI.SetActive(false);
        }
    }
}