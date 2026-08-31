using UnityEngine;

public class CombatIdentity : MonoBehaviour
{
    public enum CombatSide
    {
        Neutral,
        PlayerSide,
        Hostile
    }

    [SerializeField] private CombatSide combatSide = CombatSide.Neutral;

    public CombatSide Side => combatSide;

    public bool CanDamage(CombatIdentity target)
    {
        if (target == null)
        {
            return false;
        }

        if (combatSide == CombatSide.Neutral || target.combatSide == CombatSide.Neutral)
        {
            return false;
        }

        return combatSide != target.combatSide;
    }
}
