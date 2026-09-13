using UnityEngine;

namespace Macedon.Encounters
{
    public static class CameraShakeMath
    {
        public static float Envelope(float elapsed, float duration)
        {
            if (duration <= 0f || elapsed < 0f || elapsed >= duration) return 0f;
            float remaining = 1f - elapsed / duration;
            return remaining * remaining;
        }

        public static Vector3 Offset(float elapsed, float duration, float amplitude, float frequency, float seed)
        {
            float envelope = Envelope(elapsed, duration);
            if (envelope == 0f || amplitude <= 0f) return Vector3.zero;
            float phase = elapsed * Mathf.Max(0f, frequency);
            return new Vector3(
                Mathf.PerlinNoise(seed, phase) * 2f - 1f,
                Mathf.PerlinNoise(seed + 17.3f, phase) * 2f - 1f,
                Mathf.PerlinNoise(seed + 41.7f, phase) * 2f - 1f) * (amplitude * envelope);
        }
    }

    public sealed class LocalCameraShake : MonoBehaviour
    {
        [Tooltip("Use the Camera transform, or preferably a dedicated shake pivot below the mouse-look transform.")]
        [SerializeField] private Transform shakeTarget;
        [SerializeField, Min(0f)] private float duration = 0.65f;
        [SerializeField, Min(0f)] private float amplitude = 0.32f;
        [SerializeField, Min(0f)] private float frequency = 28f;

        private float elapsed;
        private float activeDuration;
        private float activeAmplitude;
        private float activeFrequency;
        private float seed;
        private Vector3 appliedOffset;
        private bool playing;

        private void Reset() => shakeTarget = transform;

        public void Play() => Play(duration, amplitude, frequency);

        public void Play(float shakeDuration, float shakeAmplitude, float shakeFrequency)
        {
            RemoveAppliedOffset();
            activeDuration = Mathf.Max(0f, shakeDuration);
            activeAmplitude = Mathf.Max(0f, shakeAmplitude);
            activeFrequency = Mathf.Max(0f, shakeFrequency);
            elapsed = 0f;
            seed = Time.unscaledTime * 13.37f + GetInstanceID() * 0.001f;
            playing = shakeTarget != null && activeDuration > 0f && activeAmplitude > 0f;
        }

        private void LateUpdate()
        {
            RemoveAppliedOffset();
            if (!playing) return;
            elapsed += Time.unscaledDeltaTime;
            appliedOffset = CameraShakeMath.Offset(elapsed, activeDuration, activeAmplitude, activeFrequency, seed);
            shakeTarget.localPosition += appliedOffset;
            if (elapsed >= activeDuration) { RemoveAppliedOffset(); playing = false; }
        }

        private void OnDisable()
        {
            RemoveAppliedOffset();
            playing = false;
        }

        private void RemoveAppliedOffset()
        {
            if (shakeTarget != null) shakeTarget.localPosition -= appliedOffset;
            appliedOffset = Vector3.zero;
        }
    }
}
