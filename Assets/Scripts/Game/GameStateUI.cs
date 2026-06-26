using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateUI : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private float deathScreenDelay = 1f;
    [SerializeField] private float victoryScreenDelay = 1f;

    private bool gameEnded;

    private void Awake()
    {
        deathPanel.SetActive(false);
        victoryPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowDeathScreen()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        StartCoroutine(ShowPanelAfterDelay(deathPanel, deathScreenDelay));
    }

    public void ShowVictoryScreen()
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        StartCoroutine(ShowPanelAfterDelay(victoryPanel, victoryScreenDelay));
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ShowPanelAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);

        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }
}