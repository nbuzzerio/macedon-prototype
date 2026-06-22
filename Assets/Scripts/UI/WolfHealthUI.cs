using System.Collections;
using TMPro;
using UnityEngine;

public class WolfHealthUI : MonoBehaviour
{
    [SerializeField] private Health wolfHealth;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private float fadeDelay = 1f;
    [SerializeField] private float fadeDuration = 2f;

    private bool hasStartedFade;

    private void Update()
    {
        if (healthText == null)
        {
            return;
        }

        if (wolfHealth == null)
        {
            return;
        }

        healthText.text =
            $"Wolf Health: {wolfHealth.CurrentHealth}/{wolfHealth.MaxHealth}";

        healthText.color = UIHealthColors.GetHealthColor(
            wolfHealth.CurrentHealth,
            wolfHealth.MaxHealth);

        if (wolfHealth.CurrentHealth <= 0 && !hasStartedFade)
        {
            hasStartedFade = true;
            StartCoroutine(FadeOutHealthText());
        }
    }

    private IEnumerator FadeOutHealthText()
    {
        yield return new WaitForSeconds(fadeDelay);

        Color startColor = healthText.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float alpha = 1f - elapsed / fadeDuration;
            healthText.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha);

            yield return null;
        }

        healthText.gameObject.SetActive(false);
    }
}