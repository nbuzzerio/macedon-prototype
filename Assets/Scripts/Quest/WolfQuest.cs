using UnityEngine;

public class WolfQuest : MonoBehaviour
{
    public enum QuestStage
    {
        NotStarted,
        WolfActive,
        WolfDefeated,
        RaiderThreatIntroduced
    }

    [SerializeField] private GameObject wolfEnemy;

    private QuestStage currentStage = QuestStage.NotStarted;

    public QuestStage CurrentStage => currentStage;

    public void StartQuest()
    {
        if (currentStage != QuestStage.NotStarted)
        {
            return;
        }

        currentStage = QuestStage.WolfActive;
        wolfEnemy.SetActive(true);
    }

    public void CompleteWolfEncounter()
    {
        if (currentStage != QuestStage.WolfActive)
        {
            return;
        }

        currentStage = QuestStage.WolfDefeated;
    }

    public void IntroduceRaiderThreat()
    {
        if (currentStage != QuestStage.WolfDefeated)
        {
            return;
        }

        currentStage = QuestStage.RaiderThreatIntroduced;
    }
}
