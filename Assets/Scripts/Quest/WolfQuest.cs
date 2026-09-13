using UnityEngine;
using UnityEngine.Events;
using Macedon.Encounters;

public class WolfQuest : MonoBehaviour
{
    public enum QuestStage
    {
        NotStarted,
        WolfActive,
        WolfDefeated,
        RaiderThreatIntroduced
    }

    [SerializeField] private GameObject wolfEnemy = null;
    [SerializeField] private WolfEncounterController wolfEncounter = null;
    [SerializeField] private UnityEvent onWolfEncounterCompleted = new();

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
        if (wolfEncounter != null)
        {
            wolfEncounter.BeginEncounter();
        }
    }

    public void CompleteWolfEncounter()
    {
        if (currentStage != QuestStage.WolfActive)
        {
            return;
        }

        currentStage = QuestStage.WolfDefeated;
        if (wolfEncounter != null)
        {
            wolfEncounter.CompleteEncounter();
        }
        onWolfEncounterCompleted.Invoke();
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
