using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Macedon.Villagers
{
    public sealed class AutomaticDialogueQueue : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject dialoguePanel;
        [Min(0.1f)] [SerializeField] private float displayDuration = 2.5f;
        private readonly Queue<string> lines = new();
        private Coroutine presentation;

        public void Enqueue(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            lines.Enqueue(line);
            if (presentation == null) presentation = StartCoroutine(Present());
        }

        private IEnumerator Present()
        {
            while (lines.Count > 0)
            {
                if (dialogueText != null) dialogueText.text = lines.Dequeue();
                if (dialoguePanel != null) dialoguePanel.SetActive(true);
                yield return new WaitForSeconds(displayDuration);
            }
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            presentation = null;
        }

        private void OnDisable()
        {
            presentation = null;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }
}
