using UnityEngine;

public static class UIHealthColors
{
    public static Color GetHealthColor(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent > 0.60f)
        {
            return Color.green;
        }

        if (healthPercent > 0.40f)
        {
            return Color.yellow;
        }

        if (healthPercent > 0.20f)
        {
            return new Color(1f, 0.5f, 0f);
        }

        return Color.red;
    }
}