using cfg.demo;
using My;
using My.MapExport;
using SimpleJSON;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HomeEastForestMapAssembler
{
    const string HomeScene = "Assets/Scenes/Main/Home_01_Editor.unity";
    const string EastScene = "Assets/Scenes/Main/TestLink_B_Editor.unity";
    const string EastRuntimeScene = "Assets/Scenes/Main/TestLink_B.unity";
    const string ForestScene = "Assets/Scenes/Main/Forest_01_Editor.unity";

    const string EastUnlockSwitch = "home_01.east_unlocked";
    const string ForestRouteUnlockSwitch = "home_01.forest_route_unlocked";

    [MenuItem("Window/Map/Assemble Home East Forest Route")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        ConfigureHome();
        ConfigureEastOutskirts();
        ConfigureEastRuntimeScene();
        ConfigureForest();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HomeEastForestMapAssembler] Done.");
    }

    static void ConfigureHome()
    {
        var root = OpenEditorScene(HomeScene);
        var eastPoint = EnsureNamedPoint(root, "home_east_exit", new Vector3(10f, 0f, 0f), ENamedPointType.Normal);
        EnsureTeleporter(
            root,
            "homestead_01",
            "teleporter_to_home_east",
            eastPoint.position,
            "test_link_b",
            "enter_from_home_01",
            MakeCheckVariable(EastUnlockSwitch, shouldExist: true));

        SaveAndExport(root);
    }

    static void ConfigureEastOutskirts()
    {
        var root = OpenEditorScene(EastScene);
        RemoveDynamicEntity(root, "test_link_b", "teleporter_to_a");

        var homeEntry = EnsureNamedPoint(root, "enter_from_home_01", new Vector3(-4f, 0f, 0f), ENamedPointType.Normal);
        var forestEntry = EnsureNamedPoint(root, "enter_from_forest_01", new Vector3(4f, 0f, 0f), ENamedPointType.Normal);

        EnsureTeleporter(
            root,
            "test_link_b",
            "teleporter_to_home_01",
            homeEntry.position,
            "homestead_01",
            "home_east_exit",
            null);

        EnsureTeleporter(
            root,
            "test_link_b",
            "teleporter_to_forest_01",
            forestEntry.position,
            "forest_01",
            "enter_from_home_east",
            MakeCheckVariable(ForestRouteUnlockSwitch, shouldExist: true));

        SaveAndExport(root);
    }

    static void ConfigureEastRuntimeScene()
    {
        var scene = EditorSceneManager.OpenScene(EastRuntimeScene, OpenSceneMode.Single);
        var areaRoot = FindSceneRoot(scene, "AreaRoot");
        if (areaRoot == null)
        {
            throw new System.InvalidOperationException($"AreaRoot not found in {EastRuntimeScene}");
        }

        var variantRoot = EnsureChild(areaRoot.transform, MapVariantSceneHierarchy.MapVariantRootName);
        var triggerRoot = EnsureChild(variantRoot, MapVariantSceneHierarchy.TriggerFolderName);
        EnsureForestForbidZone(triggerRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ConfigureForest()
    {
        var root = OpenEditorScene(ForestScene);
        EnsureNamedPoint(root, "enter_from_home_east", new Vector3(2f, 2f, 0f), ENamedPointType.Normal);
        SaveAndExport(root);
    }

    static MapChunkEditorRoot OpenEditorScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
        {
            var root = go.GetComponentInChildren<MapChunkEditorRoot>(true);
            if (root != null)
            {
                Selection.activeGameObject = root.gameObject;
                return root;
            }
        }

        throw new System.InvalidOperationException($"MapChunkEditorRoot not found in {scenePath}");
    }

    static GameObject FindSceneRoot(Scene scene, string rootName)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == rootName)
            {
                return go;
            }

            var child = go.transform.Find(rootName);
            if (child != null)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    static void EnsureForestForbidZone(Transform parent)
    {
        const string zoneName = "forest_route_locked_forbid_zone";
        var zone = parent.Find(zoneName);
        var go = zone != null ? zone.gameObject : new GameObject(zoneName);
        go.transform.SetParent(parent, false);
        go.transform.position = Vector3.zero;

        var checker = go.GetComponent<ForbidZoneChecker>();
        if (checker == null)
        {
            checker = go.AddComponent<ForbidZoneChecker>();
        }

        checker.EnterInnerDialogId = string.Empty;
        checker.DialogLockGlobalTime = false;
        checker.EnableCondition = new List<CommonCheckCond>
        {
            MakeCheckVariable(ForestRouteUnlockSwitch, shouldExist: false),
        };

        checker.OuterCol = EnsureZoneBox(go.transform, "Outer", new Vector2(4.7f, 0f), new Vector2(2.4f, 8f));
        checker.InnerCol = EnsureZoneBox(go.transform, "Inner", new Vector2(5.2f, 0f), new Vector2(1.2f, 7f));

        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(checker);
    }

    static BoxCollider2D EnsureZoneBox(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var child = parent.Find(name);
        var go = child != null ? child.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.layer = LayerMask.NameToLayer("Zone");

        var col = go.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = go.AddComponent<BoxCollider2D>();
        }

        col.isTrigger = true;
        col.offset = Vector2.zero;
        col.size = size;
        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(col);
        return col;
    }

    static Transform EnsureNamedPoint(
        MapChunkEditorRoot root,
        string pointName,
        Vector3 fallbackPosition,
        ENamedPointType pointType)
    {
        var namedRoot = EnsureChild(root.transform, "NamedPoint");
        var point = namedRoot.Find(pointName);
        if (point == null)
        {
            point = new GameObject(pointName).transform;
            point.SetParent(namedRoot, false);
            point.position = fallbackPosition;
        }

        var generator = point.GetComponent<NamePointGenerator>();
        if (generator == null)
        {
            generator = point.gameObject.AddComponent<NamePointGenerator>();
        }

        generator.Info = new NamedPoint
        {
            Name = pointName,
            PointType = pointType,
            Position = point.position,
            Rotation = point.rotation,
            Scale = point.localScale,
        };

        EditorUtility.SetDirty(point.gameObject);
        return point;
    }

    static void EnsureTeleporter(
        MapChunkEditorRoot root,
        string overlayId,
        string uniqName,
        Vector3 position,
        string targetMap,
        string targetPoint,
        CommonCheckCond appearCond)
    {
        var overlayRoot = EnsureOverlayRoot(root, overlayId);
        var existing = overlayRoot.Find(uniqName);
        var go = existing != null ? existing.gameObject : new GameObject(uniqName);
        go.transform.SetParent(overlayRoot, false);
        go.transform.position = position;

        var generator = go.GetComponent<DynamicEntityExportGenerator>();
        if (generator == null)
        {
            generator = go.AddComponent<DynamicEntityExportGenerator>();
        }

        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = uniqName,
            AppearCond = appearCond ?? MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            RespawnInterval = 0f,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Teleporter
            {
                CfgId = string.Empty,
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.zero,
                TargetMapName = targetMap,
                TargetNamedPoint = targetPoint,
            },
        };

        EditorUtility.SetDirty(go);
    }

    static void RemoveDynamicEntity(MapChunkEditorRoot root, string overlayId, string uniqName)
    {
        var dynamicRoot = root.transform.Find(MapVariantSceneHierarchy.DynamicRootName);
        var overlayRoot = dynamicRoot != null ? dynamicRoot.Find(overlayId) : null;
        var target = overlayRoot != null ? overlayRoot.Find(uniqName) : null;
        if (target == null)
        {
            return;
        }

        Object.DestroyImmediate(target.gameObject);
        EditorUtility.SetDirty(root.gameObject);
    }

    static Transform EnsureOverlayRoot(MapChunkEditorRoot root, string overlayId)
    {
        var dynamicRoot = EnsureChild(root.transform, MapVariantSceneHierarchy.DynamicRootName);
        EnsureChild(dynamicRoot, MapVariantSceneHierarchy.CommonFolderName);
        return EnsureChild(dynamicRoot, overlayId);
    }

    static Transform EnsureChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        EditorUtility.SetDirty(parent.gameObject);
        return child;
    }

    static CommonCheckCond MakeNoneCond()
    {
        return ParseCond("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}");
    }

    static CommonCheckCond MakeCheckVariable(string key, bool shouldExist)
    {
        var param1 = shouldExist ? 0 : 1;
        return ParseCond(
            "{\"type\":2,\"param1\":" + param1 +
            ",\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"" + key +
            "\",\"param6\":\"\"}");
    }

    static CommonCheckCond ParseCond(string json)
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(json));
    }

    static void SaveAndExport(MapChunkEditorRoot root)
    {
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(root);
        var chunkResult = MapChunkExportCore.Export(root, variantKey, root.ChunkWorldSize, root.ChunkOrigin);
        if (!chunkResult.Success)
        {
            throw new System.InvalidOperationException($"MapChunk export failed for {variantKey}: {chunkResult.Message}");
        }

        var overlayResult = MapOverlayExportCore.ExportAllOverlays(root.gameObject, root, variantKey);
        if (!overlayResult.Success)
        {
            throw new System.InvalidOperationException($"MapExport export failed for {variantKey}: {overlayResult.Message}");
        }

        Debug.Log($"[HomeEastForestMapAssembler] Exported {variantKey}: {overlayResult.Message}");
    }
}
