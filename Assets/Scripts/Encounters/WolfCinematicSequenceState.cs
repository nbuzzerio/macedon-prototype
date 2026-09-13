using System;
using UnityEngine;

namespace Macedon.Encounters
{
    public enum WolfCinematicPhase
    {
        Idle,
        TurnToWolf,
        HoldWolf,
        Impact,
        TurnToTrees,
        HoldTrees,
        Complete
    }

    [Serializable]
    public struct WolfCinematicTiming
    {
        [Min(0f)] public float wolfLookDuration;
        [Min(0f)] public float wolfHoldDuration;
        [Min(0f)] public float impactDuration;
        [Min(0f)] public float treeLookDuration;
        [Min(0f)] public float treeHoldDuration;

        public static WolfCinematicTiming Default => new()
        {
            wolfLookDuration = 0.35f,
            wolfHoldDuration = 1.6f,
            impactDuration = 0.65f,
            treeLookDuration = 0.65f,
            treeHoldDuration = 1.25f
        };

        public float DurationFor(WolfCinematicPhase phase) => phase switch
        {
            WolfCinematicPhase.TurnToWolf => Mathf.Max(0f, wolfLookDuration),
            WolfCinematicPhase.HoldWolf => Mathf.Max(0f, wolfHoldDuration),
            WolfCinematicPhase.Impact => Mathf.Max(0f, impactDuration),
            WolfCinematicPhase.TurnToTrees => Mathf.Max(0f, treeLookDuration),
            WolfCinematicPhase.HoldTrees => Mathf.Max(0f, treeHoldDuration),
            _ => 0f
        };
    }

    public sealed class WolfCinematicSequenceState
    {
        public WolfCinematicPhase Phase { get; private set; } = WolfCinematicPhase.Idle;
        public float PhaseElapsed { get; private set; }
        public event Action<WolfCinematicPhase> PhaseEntered;

        public bool TryBegin()
        {
            if (Phase != WolfCinematicPhase.Idle) return false;
            Enter(WolfCinematicPhase.TurnToWolf);
            return true;
        }

        public void Advance(float deltaTime, WolfCinematicTiming timing)
        {
            if (Phase == WolfCinematicPhase.Idle || Phase == WolfCinematicPhase.Complete) return;
            PhaseElapsed += Mathf.Max(0f, deltaTime);
            while (Phase != WolfCinematicPhase.Complete)
            {
                float duration = timing.DurationFor(Phase);
                if (duration > 0f && PhaseElapsed < duration) break;
                PhaseElapsed = duration > 0f ? PhaseElapsed - duration : PhaseElapsed;
                Enter((WolfCinematicPhase)((int)Phase + 1));
            }
        }

        public float NormalizedTime(WolfCinematicTiming timing)
        {
            float duration = timing.DurationFor(Phase);
            return duration <= 0f ? 1f : Mathf.Clamp01(PhaseElapsed / duration);
        }

        private void Enter(WolfCinematicPhase phase)
        {
            Phase = phase;
            PhaseEntered?.Invoke(phase);
        }
    }
}
