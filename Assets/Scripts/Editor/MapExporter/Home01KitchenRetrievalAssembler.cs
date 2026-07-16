#if UNITY_EDITOR
using My;
using My.MapExport;
using SimpleJSON;
using cfg.demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Home01KitchenRetrievalAssembler
{
    const string ScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
    const string KitchenFrontCleared = "home_01.kitchen_front_cleared";
    const string RubbleName = "home_kitchen_front_rubble";
    const string UtensilName = "home_kitchen_utensils";

    [MenuItem("Tools/Maps/Home 01/Install Sahel Kitchen Retrieval")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = FindMapChunkRoot(scene);
        if (root == null) throw new System.InvalidOperationException("Home_01_Editor has no MapChunkEditorRoot.");

        var areaRoot = root.transform.parent != null ? root.transform.parent : root.transform;
        var dynamicRoot = EnsureChild(root.transform, MapVariantSceneHierarchy.DynamicRootName);
        var commonRoot = EnsureChild(dynamicRoot, MapVariantSceneHierarchy.CommonFolderName);
        var namedRoot = EnsureChild(root.transform, "NamedPoint");

        var utensilPosition = ResolveUtensilPosition(root);
        EnsureNamedPoint(namedRoot, UtensilName, utensilPosition);
        EnsureDestroyObj(commonRoot, RubbleName, new Vector3(24.0f, 30.0f, 0f));
        EnsureInteractPoint(commonRoot, UtensilName, utensilPosition);
        GateRuinedKitchenTeleporters(root, KitchenFrontCleared);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(root);
        var chunkResult = MapChunkExportCore.Export(root, variantKey, root.ChunkWorldSize, root.ChunkOrigin);
        if (!chunkResult.Success) throw new System.InvalidOperationException($"MapChunk export failed: {chunkResult.Message}");
        var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, root, variantKey);
        if (!overlayResult.Success) throw new System.InvalidOperationException($"MapExport export failed: {overlayResult.Message}");
        Debug.Log($"[Home01KitchenRetrievalAssembler] Installed rubble={RubbleName}, utensil={UtensilName}, gated teleporter={KitchenFrontCleared}; chunk={chunkResult.Success}, overlay={overlayResult.Success}.");
    }

    static MapChunkEditorRoot FindMapChunkRoot(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var root = go.GetComponentInChildren<MapChunkEditorRoot>(true);
            if (root != null) return root;
        }
        return null;
    }

    static Vector3 ResolveUtensilPosition(MapChunkEditorRoot root)
    {
        var point = root.transform.Find("NamedPoint/ruined_kitchen_in_entry");
        return point != null ? point.position + new Vector3(1.2f, 0.8f, 0f) : new Vector3(-78.0f, -76.0f, 0f);
    }

    static void EnsureDestroyObj(Transform parent, string uniqName, Vector3 position)
    {
        var go = EnsureChild(parent, uniqName).gameObject;
        go.transform.position = position;
        var generator = go.GetComponent<DynamicEntityExportGenerator>() ?? go.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = uniqName,
            AppearCond = MakeCheckVariable(KitchenFrontCleared, false),
            DisappearCond = MakeCheckVariable(KitchenFrontCleared, true),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4DestroyObj
            {
                CfgId = "obj_home_kitchen_rubble",
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
            },
        };
        EditorUtility.SetDirty(go);
    }

    static void EnsureInteractPoint(Transform parent, string uniqName, Vector3 position)
    {
        var go = EnsureChild(parent, uniqName).gameObject;
        go.transform.position = position;
        var generator = go.GetComponent<DynamicEntityExportGenerator>() ?? go.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = uniqName,
            AppearCond = MakeCheckVariable(KitchenFrontCleared, true),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4InteractPoint
            {
                CfgId = "home_kitchen_utensils",
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
            },
        };
        EditorUtility.SetDirty(go);
    }

    static void GateRuinedKitchenTeleporters(MapChunkEditorRoot root, string variable)
    {
        foreach (var generator in root.GetComponentsInChildren<DynamicEntityExportGenerator>(true))
        {
            if (generator.RefreshInfo?.InitInfo is not EntityInitInfo4Teleporter teleporter || teleporter.TargetNamedPoint != "ruined_kitchen_in_entry") continue;
            generator.RefreshInfo.AppearCond = MakeCheckVariable(variable, true);
            EditorUtility.SetDirty(generator.gameObject);
        }
    }

    static Transform EnsureNamedPoint(Transform parent, string name, Vector3 position)
    {
        var point = EnsureChild(parent, name);
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
        return point;
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        EditorUtility.SetDirty(parent.gameObject);
        return go.transform;
    }

    static CommonCheckCond MakeNoneCond() => ParseCond("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}");

    static CommonCheckCond MakeCheckVariable(string key, bool shouldExist)
    {
        var param1 = shouldExist ? 0 : 1;
        return ParseCond("{\"type\":2,\"param1\":" + param1 + ",\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"" + key + "\",\"param6\":\"\"}");
    }

    static CommonCheckCond ParseCond(string json) => CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(json));
}
#endif
