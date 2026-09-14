namespace Macedon.Villagers
{
    public enum TraversalZone { A, B, C }
    public enum AuthoredTraversalPhase { Idle, Staging, Crossing, Running, Cancelling, RecoveryPending }

    public sealed class AuthoredTraversalState
    {
        public AuthoredTraversalPhase Phase { get; private set; } = AuthoredTraversalPhase.Idle;
        public bool OwnsFollowers => Phase != AuthoredTraversalPhase.Idle;
        public bool CanStartNextFollower => Phase == AuthoredTraversalPhase.Running;

        public bool Enter(TraversalZone zone)
        {
            AuthoredTraversalPhase next = Phase;
            if (Phase == AuthoredTraversalPhase.Idle && zone == TraversalZone.A) next = AuthoredTraversalPhase.Staging;
            else if (Phase == AuthoredTraversalPhase.Staging && zone == TraversalZone.B) next = AuthoredTraversalPhase.Crossing;
            else if (Phase == AuthoredTraversalPhase.Crossing && zone == TraversalZone.C) next = AuthoredTraversalPhase.Running;
            else if (Phase == AuthoredTraversalPhase.Running && zone == TraversalZone.B) next = AuthoredTraversalPhase.Cancelling;
            if (next == Phase) return false;
            Phase = next;
            return true;
        }

        public bool RequestCancellation()
        {
            if (Phase != AuthoredTraversalPhase.Running) return false;
            Phase = AuthoredTraversalPhase.Cancelling;
            return true;
        }

        public void Exit(TraversalZone zone)
        {
            // Zone exits do not imply backtracking; authored entry progression owns state changes.
        }

        public void Complete(bool recoveryPending) => Phase = recoveryPending
            ? AuthoredTraversalPhase.RecoveryPending
            : AuthoredTraversalPhase.Idle;

        public void Reset() => Phase = AuthoredTraversalPhase.Idle;
        public void ResetForRuntimeSession() => Phase = AuthoredTraversalPhase.Idle;
    }
}
