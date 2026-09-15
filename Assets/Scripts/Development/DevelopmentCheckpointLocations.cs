using UnityEngine;

namespace Macedon.Development
{
    public sealed class DevelopmentCheckpointLocations : MonoBehaviour
    {
        [SerializeField] private Transform riverStart;
        public Transform RiverStart => riverStart;
    }
}
