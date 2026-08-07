#if UNITY_EDITOR
using System.Collections.Generic;
using My;
using My.Map;
using My.MapExport;
using SimpleJSON;
using cfg.demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Home01SelfSealAssembler
{
    const string EditorScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
    const string CollapseDialogId = "home_01_self_seal_collapse";
    const string BriefingDialogId = "home_01_self_seal_briefing";
    const string PlanKnownSwitch = "home_01.self_seal_plan_known";
    const string BriefedSwitch = "home_01.self_seal_briefed";
    const string HumanUnlockedSwitch = "player.human_form_unlocked";
    const string CollapsePrefabPath = "Assets/Resources/Map/Zone/Home01SelfSealCollapseTrigger.prefab";
    const string BriefingPrefabPath = "Assets/Resources/Map/Zone/Home01SelfSealBriefingTrigger.prefab";
    const string CollapseResourceKey = "Map/Zone/Home01SelfSealCollapseTrigger";
    const string BriefingResourceKey = "Map/Zone/Home01SelfSealBriefingTrigger";

    [MenuItem("Tools/Maps/Home 01/Install Self Seal Story")]
    public static void Install()
    {
        CreateTriggerPrefab(
            "Home01SelfSealCollapseTrigger",
            CollapsePrefabPath,
            CollapseDialogId,
            new Vector2(1f, 8f),
            new List<CommonCheckCond>
            {
                MakeTaskFinishCond(204),
                MakeCheckVariableCond(PlanKnownSwitch, false),
            });
        CreateTriggerPrefab(
            "Home01SelfSealBriefingTrigger",
            BriefingPrefabPath,
            BriefingDialogId,
            new Vector2(4f, 4f),
            new List<CommonCheckCond>
            {
                MakeCheckVariableCond(PlanKnownSwitch, true),
                MakeCheckVariableCond(BriefedSwitch, false),
            });

        var scene = EditorSceneManager.OpenScene(EditorScenePath, OpenSceneMode.Single);
        var areaRoot = GameObject.Find("AreaRoot")?.transform;
        if (areaRoot == null)
        {
            throw new System.InvalidOperationException("Home_01_Editor has no AreaRoot.");
        }

        var chunkRoot = areaRoot.GetComponent<MapChunkEditorRoot>();
        if (chunkRoot == null)
        {
            throw new System.InvalidOperationException("Home_01_Editor has no MapChunkEditorRoot on AreaRoot.");
        }

        var namedRoot = areaRoot.Find("NamedPoint") ?? NewChild(areaRoot, "NamedPoint");
        EnsurePoint(namedRoot, "home_carlisle_recovery", new Vector3(13.5f, 31f, 0f));

        var triggerRoot = areaRoot.Find("MapVariantRoot/Trigger");
        if (triggerRoot == null)
        {
            var variantRoot = areaRoot.Find("MapVariantRoot");
            if (variantRoot == null)
            {
                throw new System.InvalidOperationException("Home_01_Editor has no MapVariantRoot.");
            }

            triggerRoot = NewChild(variantRoot, "Trigger");
        }

        InstallProvider(
            triggerRoot,
            "self_seal_collapse_trigger",
            new Vector3(19.5f, 30f, 0f),
            CollapseResourceKey,
            MakeTaskFinishCond(204),
            MakeCheckVariableCond(PlanKnownSwitch, true));
        InstallProvider(
            triggerRoot,
            "self_seal_briefing_trigger",
            new Vector3(-91.11f, -90.19f, 0f),
            BriefingResourceKey,
            MakeCheckVariableCond(PlanKnownSwitch, true),
            MakeCheckVariableCond(BriefedSwitch, true));

        RequireHumanUnlockForNorthExit(areaRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(chunkRoot);
        var chunkResult = MapChunkExportCore.Export(
            chunkRoot,
            variantKey,
            chunkRoot.ChunkWorldSize,
            chunkRoot.ChunkOrigin);
        if (!chunkResult.Success)
        {
            throw new System.InvalidOperationException("Home_01 MapChunk export failed: " + chunkResult.Message);
        }

        var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, chunkRoot, variantKey);
        if (!overlayResult.Success)
        {
            throw new System.InvalidOperationException("Home_01 MapExport export failed: " + overlayResult.Message);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Home01SelfSealAssembler] Installed Carlisle routine point, story triggers, and Human-gated north exit.");
    }

    static void CreateTriggerPrefab(
        string prefabName,
        string prefabPath,
        string dialogId,
        Vector2 size,
        List<CommonCheckCond> conditions)
    {
        EnsureDirectory("Assets/Resources/Map/Zone");
        var root = new GameObject(prefabName);
        try
        {
            var zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer >= 0)
            {
                root.layer = zoneLayer;
            }

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;

            var trigger = root.AddComponent<DialogTriggerZone>();
            trigger.DialogId = dialogId;
            trigger.EnableCondition = conditions;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void InstallProvider(
        Transform parent,
        string name,
        Vector3 position,
        string resourceKey,
        CommonCheckCond appearCond,
        CommonCheckCond disappearCond)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var provider = go.AddComponent<MapScenePrefabProvider>();
        provider.Key = resourceKey;
        provider.AppearCond = appearCond;
        provider.DisappearCond = disappearCond;
        EditorUtility.SetDirty(go);
    }

    static void RequireHumanUnlockForNorthExit(Transform areaRoot)
    {
        foreach (var generator in areaRoot.GetComponentsInChildren<DynamicEntityExportGenerator>(true))
        {
            if (generator == null
                || generator.gameObject.name != "entry_01"
                || generator.RefreshInfo?.InitInfo is not EntityInitInfo4Teleporter)
            {
                continue;
            }

            generator.RefreshInfo.AppearCond = MakeCheckVariableCond(HumanUnlockedSwitch, true);
            EditorUtility.SetDirty(generator.gameObject);
            return;
        }

        throw new System.InvalidOperationException("Home_01_Editor has no entry_01 teleporter.");
    }

    static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsurePoint(Transform parent, string name, Vector3 position)
    {
        var point = parent.Find(name) ?? NewChild(parent, name);
        point.position = position;
        var generator = point.GetComponent<NamePointGenerator>() ?? point.gameObject.AddComponent<NamePointGenerator>();
        generator.Info = new NamedPoint
        {
            Name = name,
            PointType = ENamedPointType.Normal,
            Position = position,
            Rotation = point.rotation,
            Scale = point.localScale,
        };
        EditorUtility.SetDirty(point.gameObject);
    }

    static CommonCheckCond MakeTaskFinishCond(int questId)
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            $"{{\"type\":1,\"param1\":{questId},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}}"));
    }

    static CommonCheckCond MakeCheckVariableCond(string variableName, bool shouldExist)
    {
        var param1 = shouldExist ? 0 : 1;
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            $"{{\"type\":2,\"param1\":{param1},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"{variableName}\",\"param6\":\"\"}}"));
    }

    static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var slash = path.LastIndexOf('/');
        var parent = path.Substring(0, slash);
        var name = path.Substring(slash + 1);
        EnsureDirectory(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
