using System;

namespace Macedon.Encounters
{
    public enum WolfEncounterPhase
    {
        NotStarted,
        Active,
        Completed
    }

    public sealed class WolfEncounterState
    {
        public WolfEncounterPhase Phase { get; private set; }
        public event Action<WolfEncounterPhase> Changed;

        public bool Begin()
        {
            if (Phase != WolfEncounterPhase.NotStarted) return false;
            Phase = WolfEncounterPhase.Active;
            Changed?.Invoke(Phase);
            return true;
        }

        public bool Complete()
        {
            if (Phase != WolfEncounterPhase.Active) return false;
            Phase = WolfEncounterPhase.Completed;
            Changed?.Invoke(Phase);
            return true;
        }
    }
}
