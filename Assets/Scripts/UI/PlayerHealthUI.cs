using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private TextMeshProUGUI healthText;

    private void Update()
    {
        if (playerHealth == null || healthText == null)
        {
            return;
        }

        healthText.text =
            $"Health: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";

        healthText.color = UIHealthColors.GetHealthColor(
            playerHealth.CurrentHealth,
            playerHealth.MaxHealth);
    }
}