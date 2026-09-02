using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SplineRiverGroup : MonoBehaviour
{
    [SerializeField] private bool includeInactiveChannels = true;

    public SplineRiverMesh[] Channels =>
        GetComponentsInChildren<SplineRiverMesh>(includeInactiveChannels);

    [ContextMenu("Rebuild All River Channels")]
    public void RebuildAll()
    {
        SplineRiverMesh[] channels = Channels;

        for (int i = 0; i < channels.Length; i++)
        {
            channels[i].Rebuild();
        }
    }
}
