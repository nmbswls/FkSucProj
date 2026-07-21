#if UNITY_EDITOR

using cfg.demo;
using My.Farm;
using My.Home;
using My.Map;
using My.Map.Scene;
using My.MapExport;
using SimpleJSON;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 在 Home_01_Editor 安装示范农田、种子篮 Entity、农业小站点位
public static class Home01FarmAssembler
{
    const string ScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
    const string OverlayId = "homestead_01";
    const int SiteFarmStation = 6;
    const string SeedBasketUniq = "farm_seed_basket";
    const string SeedBasketPrefabPath = "Assets/Resources/Prefab/Presentations/SeedBasket/SeedBasket.prefab";
    const string FarmPlotNode = "farm_plot_home01_field_a";
    const string ReclaimedVar = "home_01.reclaimed";

    [MenuItem("Tools/Maps/Home 01/Install Farm System")]
    public static void Install()
    {
        FarmPlotPrefabBuilder.Build();
        EnsureSeedBasketPrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var areaRoot = GameObject.Find("AreaRoot")?.transform;
        if (areaRoot == null)
        {
            throw new System.Exception("Home_01_Editor has no AreaRoot");
        }

        var overlayRoot = EnsureOverlayRoot(areaRoot, OverlayId);

        // 静态农田：marker + MapScenePrefabProvider，业务脚本在 Prefab/Map/FarmPlot 上
        EnsureFarmPlotStatic(overlayRoot, new Vector3(28f, 18f, 0f));

        EnsureSeedBasketEntity(overlayRoot, new Vector3(26.5f, 17f, 0f));

        EnsureSite(
            overlayRoot,
            "site_farm_station_ruin",
            new Vector3(25f, 20f, 0f),
            "Presentations/FacilityRuin/ruin_tavern",
            SiteFarmStation,
            MakeNoneCond(),
            MakeSiteLevelCond(SiteFarmStation, 1));

        EnsureSite(
            overlayRoot,
            "site_farm_station_built",
            new Vector3(25f, 20f, 0f),
            "Presentations/HomeFacility/tavern",
            SiteFarmStation,
            MakeSiteLevelCond(SiteFarmStation, 1),
            MakeNoneCond());

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var chunkRoot = areaRoot.GetComponentInChildren<MapChunkEditorRoot>(true);
        if (chunkRoot != null)
        {
            var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(chunkRoot);
            var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, chunkRoot, variantKey);
            Debug.Log($"[Home01FarmAssembler] Overlay export success={overlayResult.Success} msg={overlayResult.Message}");
        }
        else
        {
            Debug.LogWarning("[Home01FarmAssembler] MapChunkEditorRoot missing; scene installed but overlay not exported.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Home01 farm system installed (static FarmPlot + SeedBasket entity + farm_station site).");
    }

    static void EnsureFarmPlotStatic(Transform overlayRoot, Vector3 position)
    {
        var node = EnsureChild(overlayRoot, FarmPlotNode, position);
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);

        // 旧实现把 FarmPlotAreaProvider 挂在 marker 上，导出不会带走；清掉以免误导
        var legacy = node.GetComponent<FarmPlotAreaProvider>();
        if (legacy != null)
        {
            Object.DestroyImmediate(legacy);
        }

        var legacyVisual = node.Find("VisualRoot");
        if (legacyVisual != null)
        {
            Object.DestroyImmediate(legacyVisual.gameObject);
        }

        // 勿做成动态 Entity
        var dyn = node.GetComponent<DynamicEntityExportGenerator>();
        if (dyn != null)
        {
            Object.DestroyImmediate(dyn);
        }

        var mapProvider = node.GetComponent<MapScenePrefabProvider>() ?? node.gameObject.AddComponent<MapScenePrefabProvider>();
        mapProvider.Key = FarmPlotPrefabBuilder.ResourceKey;
        mapProvider.AppearCond = MakeCheckVariable(ReclaimedVar, true);
        mapProvider.DisappearCond = MakeNoneCond();

        EditorUtility.SetDirty(node.gameObject);
    }

    static void EnsureSeedBasketPrefab()
    {
        var dir = System.IO.Path.GetDirectoryName(SeedBasketPrefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            EnsureFolder(dir);
        }

        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SeedBasketPrefabPath);
        if (existing != null && existing.GetComponent<SceneSeedBasketPresenter>() != null)
        {
            return;
        }

        var root = new GameObject("SeedBasket");
        root.layer = LayerMask.NameToLayer("MapTarget");
        var presenter = root.AddComponent<SceneSeedBasketPresenter>();

        var target = new GameObject("Target");
        target.transform.SetParent(root.transform, false);
        target.layer = LayerMask.NameToLayer("MapTarget");
        var col = target.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        var view = new GameObject("View");
        view.transform.SetParent(root.transform, false);
        var sr = view.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.72f, 0.55f, 0.28f, 1f);
        sr.sortingOrder = 20;

        var so = new SerializedObject(presenter);
        var hint = so.FindProperty("hintPivot");
        if (hint != null)
        {
            hint.objectReferenceValue = view.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(root, SeedBasketPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(SeedBasketPrefabPath);
    }

    static void EnsureFolder(string assetFolder)
    {
        var parts = assetFolder.Split('/');
        var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(cur, parts[i]);
            }

            cur = next;
        }
    }

    static void EnsureSeedBasketEntity(Transform overlayRoot, Vector3 position)
    {
        var node = EnsureChild(overlayRoot, SeedBasketUniq, position);

        // 清理旧临时组件 / Missing Script
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);

        var generator = node.GetComponent<DynamicEntityExportGenerator>()
                        ?? node.gameObject.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = SeedBasketUniq,
            AppearCond = MakeCheckVariable(ReclaimedVar, true),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4SeedBasket
            {
                CfgId = "SeedBasket",
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
                LogicAreaId = OverlayId,
            },
        };

        EditorUtility.SetDirty(node.gameObject);
    }

    static Transform EnsureOverlayRoot(Transform areaRoot, string overlayId)
    {
        var dynamic = areaRoot.Find("DynamicRoot");
        if (dynamic == null)
        {
            var go = new GameObject("DynamicRoot");
            go.transform.SetParent(areaRoot, false);
            dynamic = go.transform;
        }

        var overlay = dynamic.Find(overlayId);
        if (overlay == null)
        {
            var go = new GameObject(overlayId);
            go.transform.SetParent(dynamic, false);
            overlay = go.transform;
        }

        return overlay;
    }

    static Transform EnsureChild(Transform parent, string name, Vector3 localPos)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }

        t.localPosition = localPos;
        return t;
    }

    static void EnsureSite(
        Transform overlayRoot,
        string nodeName,
        Vector3 position,
        string prefabKey,
        int siteId,
        CommonCheckCond appearCond,
        CommonCheckCond disappearCond)
    {
        var node = overlayRoot.Find(nodeName) ?? new GameObject(nodeName).transform;
        node.SetParent(overlayRoot, false);
        node.position = position;

        var mapProvider = node.GetComponent<MapScenePrefabProvider>() ?? node.gameObject.AddComponent<MapScenePrefabProvider>();
        mapProvider.Key = prefabKey;
        mapProvider.AppearCond = appearCond;
        mapProvider.DisappearCond = disappearCond;

        var siteProvider = node.GetComponent<TownFacilitySiteProvider>() ?? node.gameObject.AddComponent<TownFacilitySiteProvider>();
        siteProvider.SiteId = siteId;

        var interact = node.GetComponent<TownFacilitySiteInteract>() ?? node.gameObject.AddComponent<TownFacilitySiteInteract>();
        interact.SiteId = siteId;

        EditorUtility.SetDirty(node.gameObject);
    }

    static CommonCheckCond MakeNoneCond()
    {
        return ParseCond("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}");
    }

    static CommonCheckCond MakeSiteLevelCond(int siteId, int minLevel)
    {
        var key = TownFacilityCondKeys.BuildSiteLevelCond(siteId);
        return ParseCond(
            $"{{\"type\":2,\"param1\":{minLevel},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"{key}\",\"param6\":\"\"}}");
    }

    static CommonCheckCond MakeCheckVariable(string key, bool shouldExist)
    {
        var param1 = shouldExist ? 0 : 1;
        return ParseCond(
            "{\"type\":2,\"param1\":" + param1 + ",\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"" + key + "\",\"param6\":\"\"}");
    }

    static CommonCheckCond ParseCond(string json)
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(json));
    }
}

#endif
