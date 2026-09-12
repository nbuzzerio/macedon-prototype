using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MacedonNavigationEditor
{
    private const string RebuildMenuPath = "Tools/MACEDON/Rebuild Navigation";
    private const string ValidateAgentMenuPath = "Tools/MACEDON/Validate Selected NavMesh Agent";
    private static readonly List<NavMeshSurface> TrackedSurfaces = new();
    private static Scene trackedScene;

    [MenuItem(RebuildMenuPath)]
    private static void RebuildNavigation()
    {
        Scene scene = SceneManager.GetActiveScene();
        NavMeshSurface[] surfaces = FindSceneComponents<NavMeshSurface>(scene);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Rebuild Navigation", "There is no loaded active scene to rebuild.", "OK");
            return;
        }

        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("Rebuild Navigation", "Save the active scene before rebuilding navigation so Unity has a durable location for its NavMeshData assets.", "OK");
            return;
        }

        if (surfaces.Length == 0)
        {
            EditorUtility.DisplayDialog("Rebuild Navigation", $"No NavMeshSurface components were found in the active scene '{scene.name}'.", "OK");
            return;
        }

        NavMeshAssetManager manager = NavMeshAssetManager.instance;
        NavMeshSurface[] baking = surfaces.Where(manager.IsSurfaceBaking).ToArray();
        if (baking.Length > 0)
        {
            EditorUtility.DisplayDialog("Rebuild Navigation", $"A bake is already running for: {SurfaceNames(baking)}", "OK");
            return;
        }

        NavMeshSurface[] invalid = surfaces.Where(surface => surface.agentTypeID < 0).ToArray();
        if (invalid.Length > 0)
        {
            EditorUtility.DisplayDialog("Rebuild Navigation", $"These surfaces have no valid agent type and were not rebuilt: {SurfaceNames(invalid)}", "OK");
            return;
        }

        string summary = string.Join("\n", surfaces.Select(surface =>
            $"• {HierarchyPath(surface.transform)} (agent: {AgentTypeName(surface.agentTypeID)}, existing data: {(surface.navMeshData == null ? "none" : surface.navMeshData.name)})"));
        if (!EditorUtility.DisplayDialog(
                "Rebuild Navigation?",
                $"Rebuild {surfaces.Length} NavMeshSurface component(s) in active scene '{scene.name}' from current scene geometry?\n\n{summary}\n\nUnity will replace the surfaces' baked NavMeshData assets. This bake is not a normal Undo operation.",
                "Rebuild",
                "Cancel"))
            return;

        TrackedSurfaces.Clear();
        TrackedSurfaces.AddRange(surfaces);
        trackedScene = scene;
        manager.StartBakingSurfaces(surfaces.Cast<Object>().ToArray());
        EditorApplication.update -= MonitorBake;
        EditorApplication.update += MonitorBake;
        Debug.Log($"Started rebuilding {surfaces.Length} NavMeshSurface component(s) in '{scene.name}': {SurfaceNames(surfaces)}");
    }

    [MenuItem(RebuildMenuPath, true)]
    private static bool CanRebuildNavigation() => !Application.isPlaying;

    private static void MonitorBake()
    {
        NavMeshAssetManager manager = NavMeshAssetManager.instance;
        if (TrackedSurfaces.Any(surface => surface != null && manager.IsSurfaceBaking(surface))) return;

        EditorApplication.update -= MonitorBake;
        var results = new List<string>();
        foreach (NavMeshSurface surface in TrackedSurfaces)
        {
            if (surface == null)
            {
                results.Add("a removed surface: no result");
                continue;
            }

            string assetPath = surface.navMeshData == null ? "no NavMeshData" : AssetDatabase.GetAssetPath(surface.navMeshData);
            results.Add($"{HierarchyPath(surface.transform)}: {assetPath}");
        }

        Debug.Log($"Navigation rebuild finished for '{trackedScene.name}'.\n{string.Join("\n", results)}");
        ReportSceneAgentCoverage(trackedScene);
        TrackedSurfaces.Clear();
        SceneView.RepaintAll();
    }

    [MenuItem(ValidateAgentMenuPath)]
    private static void ValidateSelectedAgent()
    {
        NavMeshAgent agent = Selection.activeGameObject == null
            ? null
            : Selection.activeGameObject.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogWarning("Select a GameObject with a NavMeshAgent component, then run Validate Selected NavMesh Agent again.");
            return;
        }

        Debug.Log(AgentCoverageMessage(agent), agent);
    }

    [MenuItem(ValidateAgentMenuPath, true)]
    private static bool CanValidateSelectedAgent() => !Application.isPlaying;

    private static void ReportSceneAgentCoverage(Scene scene)
    {
        foreach (NavMeshAgent agent in FindSceneComponents<NavMeshAgent>(scene))
            Debug.Log(AgentCoverageMessage(agent), agent);

        NavMeshAgent wolf = FindSceneComponents<NavMeshAgent>(scene)
            .FirstOrDefault(agent => agent.name.IndexOf("wolf", System.StringComparison.OrdinalIgnoreCase) >= 0);
        Transform wolfSpawn = FindSceneComponents<Transform>(scene)
            .FirstOrDefault(transform => transform.name == "WolfSpawnPoint");
        if (wolf != null && wolfSpawn != null)
            Debug.Log(PointCoverageMessage("Wolf spawn position", wolfSpawn.position, wolf));
    }

    private static string AgentCoverageMessage(NavMeshAgent agent)
    {
        bool isOnNavMesh = false;
        if (agent.isActiveAndEnabled)
        {
            try
            {
                isOnNavMesh = agent.isOnNavMesh;
            }
            catch (System.InvalidOperationException)
            {
                // Spatial sampling below remains valid when the agent is not currently bound.
            }
        }

        return $"NavMeshAgent '{HierarchyPath(agent.transform)}': active/enabled={agent.isActiveAndEnabled}, on NavMesh={isOnNavMesh}. " +
               PointCoverageMessage("Position", agent.transform.position, agent);
    }

    private static string PointCoverageMessage(string label, Vector3 position, NavMeshAgent agent)
    {
        float searchDistance = Mathf.Max(2f, agent.height * 2f);
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        bool found = NavMesh.SamplePosition(position, out NavMeshHit hit, searchDistance, filter);
        return found
            ? $"{label} is within {Vector3.Distance(position, hit.position):F3} m of compatible NavMesh (searched {searchDistance:F3} m)."
            : $"{label} has no compatible NavMesh within {searchDistance:F3} m.";
    }

    private static T[] FindSceneComponents<T>(Scene scene) where T : Component =>
        Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(component => component.gameObject.scene == scene && !EditorUtility.IsPersistent(component))
            .OrderBy(component => HierarchyPath(component.transform))
            .ToArray();

    private static string AgentTypeName(int agentTypeId)
    {
        string name = NavMesh.GetSettingsNameFromID(agentTypeId);
        return string.IsNullOrEmpty(name) ? agentTypeId.ToString() : name;
    }

    private static string SurfaceNames(IEnumerable<NavMeshSurface> surfaces) =>
        string.Join(", ", surfaces.Select(surface => HierarchyPath(surface.transform)));

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
