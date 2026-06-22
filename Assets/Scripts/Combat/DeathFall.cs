using System.Collections;
using UnityEngine;

public class DeathFall : MonoBehaviour
{
    [SerializeField] private float fallDuration = 1.2f;
    [SerializeField] private Vector3 fallRotation = new(0f, 0f, 90f);
    [SerializeField] private float downwardOffset = 0.8f;

    public void PlayFall()
    {
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        StartCoroutine(FallOver());
    }

    private IEnumerator FallOver()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(fallRotation);

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.down * downwardOffset;

        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fallDuration;
            float easedT = t * t;

            transform.rotation = Quaternion.Slerp(startRotation, endRotation, easedT);
            transform.position = Vector3.Lerp(startPosition, endPosition, easedT);

            yield return null;
        }

        transform.rotation = endRotation;
        transform.position = endPosition;
    }
}