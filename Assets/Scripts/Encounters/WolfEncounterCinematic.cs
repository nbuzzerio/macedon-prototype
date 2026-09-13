using UnityEngine;
using UnityEngine.Events;

namespace Macedon.Encounters
{
    public sealed class WolfEncounterCinematic : MonoBehaviour
    {
        [Header("Camera and local player")]
        [SerializeField] private Transform gameplayCamera = null;
        [Tooltip("Drag the Player GameObject here. It must contain exactly one component implementing IEncounterPlayerControl.")]
        [SerializeField] private GameObject playerControlObject = null;

        [Header("Framing")]
        [SerializeField] private Transform wolfFocus = null;
        [SerializeField] private Transform treeFocus = null;

        [Header("Presentation responders")]
        [SerializeField] private LocalCameraShake cameraShake = null;
        [SerializeField] private EncounterTreeFallResponder treeFall = null;
        [SerializeField] private EncounterExitResponder exitReveal = null;
        [SerializeField] private WolfEncounterController encounter = null;

        [Header("Timing")]
        [SerializeField] private WolfCinematicTiming timing = new()
        {
            wolfLookDuration = 0.35f,
            wolfHoldDuration = 1.6f,
            impactDuration = 0.65f,
            treeLookDuration = 0.65f,
            treeHoldDuration = 1.25f
        };

        [SerializeField] private UnityEvent onSequenceCompleted = new();

        private readonly WolfCinematicSequenceState sequence = new();
        private IEncounterPlayerControl controls;
        private string playerControlError;
        private Quaternion phaseStartRotation;
        private bool ownsPlayerControl;

        public WolfCinematicPhase Phase => sequence.Phase;

        private void Awake()
        {
            TryResolvePlayerControl(playerControlObject, out controls, out playerControlError);
            sequence.PhaseEntered += HandlePhaseEntered;
        }

        private void OnValidate()
        {
            if (playerControlObject == null) return;
            if (!TryResolvePlayerControl(playerControlObject, out _, out string error))
                Debug.LogError(error, this);
        }

        private void OnDestroy() => sequence.PhaseEntered -= HandlePhaseEntered;

        public void Play()
        {
            if (gameplayCamera == null || wolfFocus == null || treeFocus == null)
            {
                Debug.LogError("Wolf cinematic requires Gameplay Camera, Wolf Focus, and Tree Focus references.", this);
                return;
            }

            if (controls == null)
            {
                Debug.LogError(playerControlError ?? "Wolf cinematic requires a Player Control GameObject.", this);
                return;
            }

            sequence.TryBegin();
        }

        private void Update()
        {
            if (sequence.Phase == WolfCinematicPhase.Idle || sequence.Phase == WolfCinematicPhase.Complete) return;
            sequence.Advance(Time.deltaTime, timing);
            UpdateCameraFraming();
        }

        private void UpdateCameraFraming()
        {
            switch (sequence.Phase)
            {
                case WolfCinematicPhase.TurnToWolf:
                    InterpolateLook(wolfFocus);
                    break;
                case WolfCinematicPhase.HoldWolf:
                case WolfCinematicPhase.Impact:
                    HoldLook(wolfFocus);
                    break;
                case WolfCinematicPhase.TurnToTrees:
                    InterpolateLook(treeFocus);
                    break;
                case WolfCinematicPhase.HoldTrees:
                    HoldLook(treeFocus);
                    break;
            }
        }

        private void HandlePhaseEntered(WolfCinematicPhase phase)
        {
            switch (phase)
            {
                case WolfCinematicPhase.TurnToWolf:
                    phaseStartRotation = gameplayCamera.rotation;
                    controls?.BeginCinematicControl();
                    ownsPlayerControl = controls != null;
                    break;
                case WolfCinematicPhase.Impact:
                    cameraShake?.Play();
                    break;
                case WolfCinematicPhase.TurnToTrees:
                    phaseStartRotation = gameplayCamera.rotation;
                    break;
                case WolfCinematicPhase.HoldTrees:
                    treeFall?.Play();
                    exitReveal?.Reveal();
                    encounter?.ReleaseBoundary();
                    break;
                case WolfCinematicPhase.Complete:
                    cameraShake?.Stop();
                    RestorePlayerControl();
                    onSequenceCompleted.Invoke();
                    break;
            }
        }

        private void InterpolateLook(Transform target)
        {
            Quaternion desired = LookRotation(gameplayCamera.position, target.position, gameplayCamera.rotation);
            float t = sequence.NormalizedTime(timing);
            t = t * t * (3f - 2f * t);
            gameplayCamera.rotation = Quaternion.Slerp(phaseStartRotation, desired, t);
        }

        private void HoldLook(Transform target) =>
            gameplayCamera.rotation = LookRotation(gameplayCamera.position, target.position, gameplayCamera.rotation);

        private void RestorePlayerControl()
        {
            if (!ownsPlayerControl) return;
            controls.EndCinematicControl(gameplayCamera);
            ownsPlayerControl = false;
        }

        private void OnDisable()
        {
            cameraShake?.Stop();
            RestorePlayerControl();
        }

        public static Quaternion LookRotation(Vector3 cameraPosition, Vector3 targetPosition, Quaternion fallback)
        {
            Vector3 direction = targetPosition - cameraPosition;
            return direction.sqrMagnitude <= 0.000001f ? fallback : Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        public static Quaternion FlatYawRotation(Quaternion cameraRotation, Quaternion fallback)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, Vector3.up);
            return flatForward.sqrMagnitude <= 0.000001f
                ? fallback
                : Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        public static bool TryResolvePlayerControl(
            GameObject controlObject,
            out IEncounterPlayerControl control,
            out string error)
        {
            control = null;
            if (controlObject == null)
            {
                error = "Wolf cinematic requires a Player Control GameObject.";
                return false;
            }

            MonoBehaviour[] components = controlObject.GetComponents<MonoBehaviour>();
            int matchCount = 0;
            foreach (MonoBehaviour component in components)
            {
                if (component is not IEncounterPlayerControl candidate) continue;
                control = candidate;
                matchCount++;
            }

            if (matchCount == 1)
            {
                error = null;
                return true;
            }

            control = null;
            error = matchCount == 0
                ? $"Wolf cinematic Player Control object '{controlObject.name}' has no component implementing IEncounterPlayerControl."
                : $"Wolf cinematic Player Control object '{controlObject.name}' has {matchCount} components implementing IEncounterPlayerControl; exactly one is required.";
            return false;
        }
    }
}
