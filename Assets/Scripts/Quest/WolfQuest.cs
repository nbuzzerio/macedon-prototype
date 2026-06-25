using UnityEngine;

public class WolfQuest : MonoBehaviour
{
    [SerializeField] private GameObject wolfEnemy;

    private bool questStarted;

    public void StartQuest()
    {
        if (questStarted)
        {
            return;
        }

        questStarted = true;
        wolfEnemy.SetActive(true);
    }
}