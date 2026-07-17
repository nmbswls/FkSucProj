#if UNITY_EDITOR
using My;
using My.Map;
using My.MapExport;
using SimpleJSON;
using cfg.demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Village01HerbShopAssembler
{
    const string VillageEditorScenePath = "Assets/Scenes/Main/Main_Area_01_Editor.unity";
    const string RoomEntry = "village_01_herb_shop_entry";
    const string VillageReturn = "village_01_herb_shop_return";
    const string NpcUniqName = "village_herbalist";
    const string NpcCfgId = "village_herbalist";
    const string CharacterKey = "village_herbalist";
    const float EmbeddedRoomOffsetX = 100f;

    [MenuItem("Tools/Maps/Village 01/Install Herb Shop Room")]
    public static void Install()
    {
        InstallEmbeddedRoom();
    }

    static void InstallEmbeddedRoom()
    {
        var scene = EditorSceneManager.OpenScene(VillageEditorScenePath, OpenSceneMode.Single);
        var root = FindMapChunkRoot(scene);
        if (root == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor has no MapChunkEditorRoot.");
        }

        var areaRoot = root.transform;
        var mapVariantRoot = root.MapVariantRoot;
        var roomRoot = mapVariantRoot.Find("Room");
        var gridRoot = mapVariantRoot.Find("GridRoot");
        var sourceRoom = FindRoomPrefabBranch(roomRoot);
        var sourceGridRoom = gridRoot != null ? gridRoot.Find("Building_01") : null;
        if (sourceRoom == null || sourceGridRoom == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor room source branches are missing.");
        }

        DuplicateBranch(sourceRoom, roomRoot, "HerbShopRoom", EmbeddedRoomOffsetX);
        DuplicateBranch(sourceGridRoom, gridRoot, "HerbShopBuilding_01", EmbeddedRoomOffsetX);

        var namedRoot = EnsureChild(areaRoot, "NamedPoint");
        var sourceIn = namedRoot.Find("building_01_in");
        var sourceOut = namedRoot.Find("building_01_out");
        if (sourceIn == null || sourceOut == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor building room NamedPoints are missing.");
        }

        var entryPosition = sourceIn.position + new Vector3(EmbeddedRoomOffsetX, 0f, 0f);
        var returnPosition = sourceOut.position + new Vector3(EmbeddedRoomOffsetX, 0f, 0f);
        EnsureNamedPoint(namedRoot, RoomEntry, entryPosition);
        EnsureNamedPoint(namedRoot, VillageReturn, returnPosition);

        var common = EnsureChild(EnsureChild(areaRoot, "DynamicRoot"), "Common");
        EnsureTeleporter(common, "teleporter_from_herb_shop", entryPosition, "", VillageReturn);
        EnsureNpc(common, entryPosition + new Vector3(0f, 3f, 0f));

        var decorate = EnsureChild(mapVariantRoot, "Decorate");
        EnsurePrefabProvider(decorate, "herb_shop_counter", entryPosition + new Vector3(0f, 2f, 0f), "house_village_01");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var chunkResult = MapChunkExportCore.Export(root, root.MapVariantSceneName, root.ChunkWorldSize, root.ChunkOrigin);
        var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, root, root.MapVariantSceneName);
        if (!chunkResult.Success || !overlayResult.Success)
        {
            throw new System.InvalidOperationException(
                $"Village01 herb shop export failed. chunk={chunkResult.Message} overlay={overlayResult.Message}");
        }

        PatchVillageOutdoorEntry();

        AssetDatabase.SaveAssets();
        Debug.Log("[Village01HerbShopAssembler] Installed and exported embedded village_01 herb shop room.");
    }

    static void PatchVillageOutdoorEntry()
    {
        var scene = EditorSceneManager.OpenScene(VillageEditorScenePath, OpenSceneMode.Single);
        var root = FindMapChunkRoot(scene);
        if (root == null)
        {
            throw new System.InvalidOperationException("Main_Area_01_Editor has no MapChunkEditorRoot.");
        }

        var areaRoot = root.transform;
        var namedRoot = EnsureChild(areaRoot, "NamedPoint");
        EnsureNamedPoint(namedRoot, VillageReturn, new Vector3(47.5f, 37.2f, 0f));
        var common = EnsureChild(EnsureChild(areaRoot, "DynamicRoot"), "Common");
        EnsureTeleporter(common, "teleporter_to_village_herb_shop", new Vector3(47.5f, 37.2f, 0f), "", RoomEntry);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var chunkResult = MapChunkExportCore.Export(root, root.MapVariantSceneName, root.ChunkWorldSize, root.ChunkOrigin);
        var overlayResult = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, root, root.MapVariantSceneName);
        if (!chunkResult.Success || !overlayResult.Success)
        {
            throw new System.InvalidOperationException(
                $"Village01 outdoor export failed. chunk={chunkResult.Message} overlay={overlayResult.Message}");
        }
    }

    static Transform FindRoomPrefabBranch(Transform roomRoot)
    {
        if (roomRoot == null)
        {
            return null;
        }

        foreach (var provider in roomRoot.GetComponentsInChildren<MapScenePrefabProvider>(true))
        {
            if (provider.Key == "1/Room01")
            {
                return provider.transform;
            }
        }

        return roomRoot.childCount > 0 ? roomRoot.GetChild(0) : null;
    }

    static void DuplicateBranch(Transform source, Transform parent, string name, float offsetX)
    {
        var old = parent.Find(name);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        var copy = Object.Instantiate(source.gameObject, parent);
        copy.name = name;
        copy.transform.position = source.position + new Vector3(offsetX, 0f, 0f);
        EditorUtility.SetDirty(copy);
    }

    static void EnsureNamedPoint(Transform parent, string name, Vector3 position)
    {
        var point = parent.Find(name);
        if (point == null)
        {
            point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.gameObject.AddComponent<NamePointGenerator>();
        }

        point.position = position;
        var generator = point.GetComponent<NamePointGenerator>();
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

    static void EnsureTeleporter(Transform parent, string name, Vector3 position, string targetMap, string targetPoint)
    {
        var go = EnsureChild(parent, name).gameObject;
        go.transform.position = position;
        var generator = go.GetComponent<DynamicEntityExportGenerator>() ?? go.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = name,
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Teleporter
            {
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
                TargetMapName = targetMap,
                TargetNamedPoint = targetPoint,
            },
        };
        EditorUtility.SetDirty(go);
    }

    static void EnsureNpc(Transform parent, Vector3 position)
    {
        var go = EnsureChild(parent, NpcUniqName).gameObject;
        go.transform.position = position;
        var generator = go.GetComponent<DynamicEntityExportGenerator>() ?? go.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = NpcUniqName,
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Npc
            {
                CfgId = NpcCfgId,
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.down,
                MoveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove,
                EnmityConfId = "default_npc",
                IsPeace = true,
                CharacterKey = CharacterKey,
            },
        };
        EditorUtility.SetDirty(go);
    }

    static void EnsurePrefabProvider(Transform parent, string name, Vector3 position, string key)
    {
        var go = EnsureChild(parent, name).gameObject;
        go.transform.position = position;
        var provider = go.GetComponent<MapScenePrefabProvider>() ?? go.AddComponent<MapScenePrefabProvider>();
        provider.Key = key;
        provider.AppearCond = MakeNoneCond();
        provider.DisappearCond = MakeNoneCond();
        EditorUtility.SetDirty(go);
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        child = new GameObject(name).transform;
        child.SetParent(parent, false);
        EditorUtility.SetDirty(parent.gameObject);
        return child;
    }

    static CommonCheckCond MakeNoneCond()
    {
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse(
            "{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}"));
    }

    static MapChunkEditorRoot FindMapChunkRoot(Scene scene)
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
