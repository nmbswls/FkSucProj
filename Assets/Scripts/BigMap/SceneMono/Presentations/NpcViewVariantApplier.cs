using My.Config;
using My.Map.Scene;
using UnityEngine;

namespace My.Map
{
    /// <summary>
    /// Replaces only the visual agent below an NPC shell. Gameplay components
    /// remain owned by the shell prefab.
    /// </summary>
    public static class NpcViewVariantApplier
    {
        public static void Apply(GameObject shell, string npcId, long entityId)
        {
            if (shell == null || !NpcViewRandomizationCatalog.TryResolveViewPrefabName(npcId, entityId, out var viewName))
                return;

            var viewRoot = shell.transform.Find("view");
            if (viewRoot == null)
                return;

            var viewPrefab = Resources.Load<GameObject>("Prefab/Presentations/Npc/" + viewName);
            if (viewPrefab == null)
                return;

            var currentAgent = viewRoot.Find("agent");
            if (currentAgent != null)
                Object.Destroy(currentAgent.gameObject);

            var instance = Object.Instantiate(viewPrefab, viewRoot, false);
            instance.name = "agent";
            shell.GetComponent<SceneUnitPresenter>()?.RefreshViewHierarchy();
        }
    }
}
