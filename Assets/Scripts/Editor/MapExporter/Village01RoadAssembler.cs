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
using UnityEngine.Tilemaps;

public static class Village01RoadAssembler
{
    const string EditorScenePath = "Assets/Scenes/Main/Village01_Road_Editor.unity";
    const string SceneName = "Village01_Road";
    const string GroundSpritePath = "Assets/Arts/Tile/basic_01/ground_grasss.png";
    const string GroundTilePath = "Assets/Resources/Map/Prototype/village_01_road_ground.asset";
    const string TownGateTriggerPrefabPath = "Assets/Resources/Map/Zone/Village01RoadTownGateTrigger.prefab";
    const string TownGateTriggerResourceKey = "Map/Zone/Village01RoadTownGateTrigger";
    const string TownGateDialogId = "village_01_road_town_gate";
    const string RoadGateOpenSwitch = "village_01.road_gate_open";
    const int RoadQuestId = 216;
    const string RoadQuestStepId = "216_s2";

    [MenuItem("Tools/Maps/Village 01/Install Road Prototype")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(EditorScenePath, OpenSceneMode.Single);
        var root = FindMapChunkRoot(scene);
        if (root == null)
        {
            throw new System.InvalidOperationException("Village01_Road_Editor has no MapChunkEditorRoot.");
        }

        root.MapVariantSceneName = SceneName;
        root.GroundLayerNames = new[] { "Ground" };
        root.PaintWorldRect = new Rect(-16f, 0f, 32f, 240f);

        var mapVariant = root.MapVariantRoot;
        var gridRoot = mapVariant.Find("GridRoot");
        var ground = gridRoot != null ? gridRoot.Find("Ground") : null;
        if (ground == null)
        {
            throw new System.InvalidOperationException("Village01_Road_Editor has no GridRoot/Ground tilemap.");
        }

        gridRoot.gameObject.SetActive(true);
        BuildGround(ground.GetComponent<Tilemap>());
        EnsureCollider(ground.gameObject);

        ClearChildren(root.transform.Find("NamedPoint"));
        CreateNamedPoint(root.transform.Find("NamedPoint"), "village_01_road_from_home", new Vector3(0f, 5f, 0f));
        CreateNamedPoint(root.transform.Find("NamedPoint"), "village_01_road_to_home", new Vector3(0f, 12f, 0f));
        CreateNamedPoint(root.transform.Find("NamedPoint"), "village_01_road_to_town", new Vector3(0f, 232f, 0f));

        var dynamicRoot = root.transform.Find("DynamicRoot");
        ClearChildren(dynamicRoot);
        var common = EnsureChild(dynamicRoot, "Common");
        CreateTeleporter(common, "teleporter_to_home", new Vector3(0f, 5f, 0f), "homestead_01", "entry_01");
        CreateTeleporter(
            common,
            "teleporter_to_village_01",
            new Vector3(0f, 232f, 0f),
            "village_01",
            "entry_bottom",
            MakeCheckVariableCond(RoadGateOpenSwitch, true));
        CreateRoadMonster(common, "village_01_road_slime_01", new Vector3(-4f, 72f, 0f));
        CreateRoadMonster(common, "village_01_road_slime_02", new Vector3(4f, 122f, 0f));
        CreateRoadMonster(common, "village_01_road_slime_03", new Vector3(0f, 172f, 0f));

        CreateTownGateTriggerPrefab();
        InstallTownGateTrigger(root.MapVariantRoot.Find("Trigger"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(root);
        var chunk = MapChunkExportCore.Export(root, variantKey, root.ChunkWorldSize, root.ChunkOrigin);
        if (!chunk.Success)
        {
            throw new System.InvalidOperationException("Village01_Road MapChunk export failed: " + chunk.Message);
        }

        var overlay = MapOverlayExportCore.ExportAllOverlays(root.transform.gameObject, root, variantKey);
        if (!overlay.Success)
        {
            throw new System.InvalidOperationException("Village01_Road MapExport export failed: " + overlay.Message);
        }

        PatchHomeDeparture();
        PatchVillageDeparture();

        Debug.Log("[Village01RoadAssembler] Installed and exported village_01_road prototype.");
    }

    static void PatchHomeDeparture()
    {
        const string homeScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
        var homeScene = EditorSceneManager.OpenScene(homeScenePath, OpenSceneMode.Single);
        var homeRoot = FindMapChunkRoot(homeScene);
        if (homeRoot == null)
        {
            throw new System.InvalidOperationException("Home_01_Editor has no MapChunkEditorRoot.");
        }

        DynamicEntityExportGenerator departure = null;
        foreach (var generator in homeRoot.GetComponentsInChildren<DynamicEntityExportGenerator>(true))
        {
            if (generator.gameObject.name != "entry_01" || generator.RefreshInfo?.InitInfo is not EntityInitInfo4Teleporter)
            {
                continue;
            }

            departure = generator;
            break;
        }

        if (departure == null)
        {
            throw new System.InvalidOperationException("Home_01_Editor has no entry_01 teleporter.");
        }

        var info = (EntityInitInfo4Teleporter)departure.RefreshInfo.InitInfo;
        info.TargetMapName = "village_01_road";
        info.TargetNamedPoint = "village_01_road_from_home";
        departure.RefreshInfo.InitInfo = info;
        EditorUtility.SetDirty(departure.gameObject);
        EditorSceneManager.MarkSceneDirty(homeScene);
        EditorSceneManager.SaveScene(homeScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(homeRoot);
        var chunk = MapChunkExportCore.Export(homeRoot, variantKey, homeRoot.ChunkWorldSize, homeRoot.ChunkOrigin);
        if (!chunk.Success)
        {
            throw new System.InvalidOperationException("Home_01 MapChunk export failed: " + chunk.Message);
        }

        var areaRoot = homeRoot.transform.parent != null ? homeRoot.transform.parent : homeRoot.transform;
        var overlay = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, homeRoot, variantKey);
        if (!overlay.Success)
        {
            throw new System.InvalidOperationException("Home_01 MapExport export failed: " + overlay.Message);
        }
    }

    static void PatchVillageDeparture()
    {
        const string villageScenePath = "Assets/Scenes/Main/Main_Area_01_Editor.unity";
        var villageScene = EditorSceneManager.OpenScene(villageScenePath, OpenSceneMode.Single);
        var villageRoot = FindMapChunkRoot(villageScene);
        if (villageRoot == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor has no MapChunkEditorRoot.");
        }

        var dynamicRoot = villageRoot.transform.Find("DynamicRoot");
        if (dynamicRoot == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor has no DynamicRoot.");
        }

        var common = dynamicRoot.Find("Common") ?? EnsureChild(dynamicRoot, "Common");
        var old = common.Find("teleporter_to_village_01_road");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        var teleporter = new GameObject("teleporter_to_village_01_road");
        teleporter.transform.SetParent(common, false);
        teleporter.transform.position = new Vector3(46.45f, 37.23f, 0f);
        teleporter.AddComponent<DynamicEntityExportGenerator>().RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = "teleporter_to_village_01_road",
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Teleporter
            {
                Position = new Vector2(46.45f, 37.23f),
                FaceDir = Vector2.down,
                TargetMapName = "village_01_road",
                TargetNamedPoint = "village_01_road_to_town",
            },
        };

        EditorSceneManager.MarkSceneDirty(villageScene);
        EditorSceneManager.SaveScene(villageScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var variantKey = MapChunkEditorUtility.ResolveMapChunkKey(villageRoot);
        var chunk = MapChunkExportCore.Export(villageRoot, variantKey, villageRoot.ChunkWorldSize, villageRoot.ChunkOrigin);
        if (!chunk.Success)
        {
            throw new System.InvalidOperationException("Main_Area_01 MapChunk export failed: " + chunk.Message);
        }

        var areaRoot = villageRoot.transform.parent != null ? villageRoot.transform.parent : villageRoot.transform;
        var overlay = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, villageRoot, variantKey);
        if (!overlay.Success)
        {
            throw new System.InvalidOperationException("Main_Area_01 MapExport export failed: " + overlay.Message);
        }
    }

    static void BuildGround(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            throw new System.InvalidOperationException("Village01_Road ground Tilemap is missing.");
        }

        var tile = EnsureGroundTile();
        tilemap.ClearAllTiles();
        for (var y = 0; y < 240; y++)
        {
            for (var x = -16; x < 16; x++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        tilemap.CompressBounds();
        EditorUtility.SetDirty(tilemap);
    }

    static TileBase EnsureGroundTile()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TileBase>(GroundTilePath);
        if (existing != null)
        {
            return existing;
        }

        var sprite = FindSprite(GroundSpritePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException("Missing prototype ground sprite: " + GroundSpritePath);
        }

        EnsureDirectory("Assets/Resources/Map/Prototype");
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = "village_01_road_ground";
        tile.sprite = sprite;
        tile.color = Color.white;
        AssetDatabase.CreateAsset(tile, GroundTilePath);
        return tile;
    }

    static Sprite FindSprite(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    static void EnsureCollider(GameObject ground)
    {
        if (ground.GetComponent<TilemapCollider2D>() == null)
        {
            ground.AddComponent<TilemapCollider2D>();
        }
    }

    static void CreateNamedPoint(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.AddComponent<NamePointGenerator>().Info = new NamedPoint
        {
            Name = name,
            PointType = ENamedPointType.Normal,
            Position = position,
            Rotation = go.transform.rotation,
            Scale = go.transform.localScale,
        };
    }

    static void CreateTeleporter(
        Transform parent,
        string name,
        Vector3 position,
        string targetMap,
        string targetPoint,
        CommonCheckCond appearCond = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.AddComponent<DynamicEntityExportGenerator>().RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = name,
            AppearCond = appearCond ?? MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Teleporter
            {
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.up,
                TargetMapName = targetMap,
                TargetNamedPoint = targetPoint,
            },
        };
    }

    static void CreateRoadMonster(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.AddComponent<DynamicEntityExportGenerator>().RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = name,
            AppearCond = MakeTaskStepCond(RoadQuestId, RoadQuestStepId),
            DisappearCond = MakeTaskFinishCond(RoadQuestId),
            WillRespawn = false,
            RespawnInterval = 0f,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Npc
            {
                CfgId = "slime_green",
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
                MoveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                EnmityConfId = string.Empty,
                IsPeace = false,
            },
        };
    }

    static void CreateTownGateTriggerPrefab()
    {
        EnsureDirectory("Assets/Resources/Map/Zone");
        var prefabRoot = new GameObject("Village01RoadTownGateTrigger");
        try
        {
            var zoneLayer = LayerMask.NameToLayer("Zone");
            if (zoneLayer >= 0)
            {
                prefabRoot.layer = zoneLayer;
            }

            var collider = prefabRoot.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(28f, 4f);

            var trigger = prefabRoot.AddComponent<DialogTriggerZone>();
            trigger.DialogId = TownGateDialogId;
            trigger.EnableCondition = new List<CommonCheckCond>
            {
                MakeTaskFinishCond(RoadQuestId),
                MakeCheckVariableCond(RoadGateOpenSwitch, false),
            };

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, TownGateTriggerPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(prefabRoot);
        }
    }

    static void InstallTownGateTrigger(Transform triggerRoot)
    {
        if (triggerRoot == null)
        {
            throw new System.InvalidOperationException("Village01_Road_Editor has no MapVariantRoot/Trigger.");
        }

        var existing = triggerRoot.Find("town_gate_dialog_trigger");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var go = new GameObject("town_gate_dialog_trigger");
        go.transform.SetParent(triggerRoot, false);
        go.transform.position = new Vector3(0f, 228f, 0f);
        var provider = go.AddComponent<MapScenePrefabProvider>();
        provider.Key = TownGateTriggerResourceKey;
        provider.AppearCond = MakeNoneCond();
        provider.DisappearCond = MakeCheckVariableCond(RoadGateOpenSwitch, true);
    }

    static CommonCheckCond MakeNoneCond()
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}"));
    }

    static CommonCheckCond MakeTaskFinishCond(int questId)
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            $"{{\"type\":1,\"param1\":{questId},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}}"));
    }

    static CommonCheckCond MakeTaskStepCond(int questId, string stepId)
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            $"{{\"type\":5,\"param1\":{questId},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"{stepId}\",\"param6\":\"\"}}"));
    }

    static CommonCheckCond MakeCheckVariableCond(string variableName, bool shouldExist)
    {
        var param1 = shouldExist ? 0 : 1;
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            $"{{\"type\":2,\"param1\":{param1},\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"{variableName}\",\"param6\":\"\"}}"));
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static MapChunkEditorRoot FindMapChunkRoot(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var root = go.GetComponentInChildren<MapChunkEditorRoot>(true);
            if (root != null)
            {
                return root;
            }
        }

        return null;
    }
}
#endif
