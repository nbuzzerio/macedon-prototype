using UnityEngine;

namespace Macedon.Encounters
{
    public interface IEncounterPlayerControl
    {
        void BeginCinematicControl();
        void EndCinematicControl(Transform gameplayCamera);
    }
}
