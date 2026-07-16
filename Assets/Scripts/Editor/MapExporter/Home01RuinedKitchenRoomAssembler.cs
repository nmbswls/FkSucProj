#if UNITY_EDITOR
using My;
using My.MapExport;
using SimpleJSON;
using cfg.demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Home01RuinedKitchenRoomAssembler
{
    const string ScenePath = "Assets/Scenes/Main/Home_01_Editor.unity";
    const string RoomId = "ruined_kitchen";
    const string InPoint = "ruined_kitchen_in_entry";
    const string OutPoint = "ruined_kitchen_out_entry";
    const string OldInPoint = "bedroom_in_entry";
    const string OldOutPoint = "bedroom_out_entry";
    const string Gate = "home_01.kitchen_front_cleared";
    const float OffsetX = 32f;

    [MenuItem("Tools/Maps/Home 01/Install Ruined Kitchen Room")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = FindMapChunkRoot(scene);
        if (root == null) throw new System.InvalidOperationException("Home_01_Editor has no MapChunkEditorRoot.");
        var areaRoot = root.transform;
        var mapVariant = root.MapVariantRoot;
        if (mapVariant == null) throw new System.InvalidOperationException("Home_01_Editor has no MapVariantRoot.");

        DuplicateRoomBranch(mapVariant.Find("Room/Bedroom"), mapVariant.Find("Room"), "RuinedKitchenRoom", OffsetX);
        DuplicateRoomBranch(mapVariant.Find("GridRoot/Bedroom"), mapVariant.Find("GridRoot"), "RuinedKitchenGround", OffsetX);
        var staticContainer = areaRoot.Find("homestead_01/Bedroom");
        if (staticContainer != null)
        {
            DuplicateRoomBranch(staticContainer, areaRoot.Find("homestead_01"), "RuinedKitchenStatic", OffsetX);
        }

        var namedRoot = areaRoot.Find("NamedPoint");
        DuplicateNamedPoint(namedRoot.Find(OldInPoint), namedRoot, InPoint, OffsetX);
        EnsureNamedPointAt(namedRoot, OutPoint, new Vector3(22f, 30f, 0f));
        RenameRoomId(mapVariant, "RuinedKitchenRoom", RoomId);
        RenameRoomId(mapVariant, "RuinedKitchenGround", RoomId);
        AssetDatabase.Refresh();
        EnsureRuinedKitchenDecor(mapVariant, namedRoot.Find(InPoint).position);

        DynamicEntityExportGenerator oldRoomReturn = null;
        DynamicEntityExportGenerator oldMainEntry = null;
        foreach (var generator in root.GetComponentsInChildren<DynamicEntityExportGenerator>(true))
        {
            if (generator.RefreshInfo?.InitInfo is not EntityInitInfo4Teleporter teleporter) continue;
            if (teleporter.TargetNamedPoint == InPoint)
            {
                oldMainEntry = generator;
            }
            else if (teleporter.TargetNamedPoint == OutPoint && generator.transform.position.x < -50f)
            {
                oldRoomReturn = generator;
            }
        }

        var ruinedKitchenEntry = FindOrCreateUniqueChild(root.transform.Find("DynamicRoot/Common"), "teleporter_to_ruined_kitchen", oldMainEntry);
        if (ruinedKitchenEntry != null)
        {
            var newEntry = ruinedKitchenEntry.gameObject;
            newEntry.transform.position = new Vector3(24f, 30f, 0f);
            var newEntryGenerator = newEntry.GetComponent<DynamicEntityExportGenerator>();
            var newEntryInfo = (EntityInitInfo4Teleporter)newEntryGenerator.RefreshInfo.InitInfo;
            newEntryInfo.TargetNamedPoint = InPoint;
            newEntryGenerator.RefreshInfo.InitInfo = newEntryInfo;
            newEntryGenerator.RefreshInfo.AppearCond = MakeCheckVariable(Gate, true);
            newEntryGenerator.RefreshInfo.DisappearCond = MakeNoneCond();
            EditorUtility.SetDirty(newEntry);
        }

        if (oldMainEntry != null)
        {
            var oldEntryInfo = (EntityInitInfo4Teleporter)oldMainEntry.RefreshInfo.InitInfo;
            oldEntryInfo.TargetNamedPoint = OldInPoint;
            oldMainEntry.RefreshInfo.InitInfo = oldEntryInfo;
            oldMainEntry.RefreshInfo.AppearCond = MakeNoneCond();
            oldMainEntry.RefreshInfo.DisappearCond = MakeNoneCond();
            EditorUtility.SetDirty(oldMainEntry.gameObject);
        }

        var ruinedKitchenReturn = FindOrCreateUniqueChild(root.transform.Find("DynamicRoot/Common"), "teleporter_from_ruined_kitchen", oldRoomReturn);
        if (ruinedKitchenReturn != null)
        {
            var newReturn = ruinedKitchenReturn.gameObject;
            newReturn.transform.position += new Vector3(OffsetX, 0f, 0f);
            var newReturnGenerator = newReturn.GetComponent<DynamicEntityExportGenerator>();
            var newReturnInfo = (EntityInitInfo4Teleporter)newReturnGenerator.RefreshInfo.InitInfo;
            newReturnInfo.TargetNamedPoint = OutPoint;
            newReturnGenerator.RefreshInfo.InitInfo = newReturnInfo;
            EditorUtility.SetDirty(newReturn);
        }

        if (oldRoomReturn != null)
        {
            var oldReturnInfo = (EntityInitInfo4Teleporter)oldRoomReturn.RefreshInfo.InitInfo;
            oldReturnInfo.TargetNamedPoint = OldOutPoint;
            oldRoomReturn.RefreshInfo.InitInfo = oldReturnInfo;
            EditorUtility.SetDirty(oldRoomReturn.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        var key = MapChunkEditorUtility.ResolveMapChunkKey(root);
        var chunk = MapChunkExportCore.Export(root, key, root.ChunkWorldSize, root.ChunkOrigin);
        if (!chunk.Success) throw new System.InvalidOperationException(chunk.Message);
        var overlay = MapOverlayExportCore.ExportAllOverlays(areaRoot.gameObject, root, key);
        if (!overlay.Success) throw new System.InvalidOperationException(overlay.Message);
        Debug.Log("[Home01RuinedKitchenRoomAssembler] Installed independent ruined_kitchen room and local teleport points.");
    }

    static void EnsureRuinedKitchenDecor(Transform mapVariant, Vector3 entryPosition)
    {
        var decorate = mapVariant.Find("Decorate/RuinedKitchenDecor");
        if (decorate == null)
        {
            var go = new GameObject("RuinedKitchenDecor");
            go.transform.SetParent(mapVariant.Find("Decorate"), false);
            decorate = go.transform;
        }

        for (int i = decorate.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(decorate.GetChild(i).gameObject);
        }

        var furniturePrefab = EnsurePropPrefab("Assets/Resources/Map/RoomProps/RuinedKitchen/collapsed_furniture.png", "Assets/Resources/Prefab/Map/RuinedKitchen/collapsed_furniture.prefab");
        var rubblePrefab = EnsurePropPrefab("Assets/Resources/Map/RoomProps/RuinedKitchen/wood_rubble_pile.png", "Assets/Resources/Prefab/Map/RuinedKitchen/wood_rubble_pile.prefab");
        PlaceProp(furniturePrefab, decorate, "ruined_kitchen_collapsed_furniture", entryPosition + new Vector3(-1.9f, 1.2f, 0f), 2);
        PlaceProp(rubblePrefab, decorate, "ruined_kitchen_wood_rubble_left", entryPosition + new Vector3(-3.0f, -0.8f, 0f), 1);
        PlaceProp(rubblePrefab, decorate, "ruined_kitchen_wood_rubble_right", entryPosition + new Vector3(2.8f, -1.0f, 0f), 1);
        EditorUtility.SetDirty(decorate.gameObject);
    }

    static GameObject EnsurePropPrefab(string spritePath, string prefabPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null) throw new System.InvalidOperationException("Missing ruined kitchen sprite: " + spritePath);
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null) return existing;

        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefabPath));
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static void PlaceProp(GameObject prefab, Transform parent, string name, Vector3 position, int sortingOrder)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.position = position;
        var renderer = instance.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sortingOrder = sortingOrder;
        EditorUtility.SetDirty(instance);
    }
    static void DuplicateRoomBranch(Transform source, Transform parent, string name, float offsetX)
    {
        if (source == null || parent == null) throw new System.InvalidOperationException($"Missing room branch source={source} parent={parent}.");
        var old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var copy = Object.Instantiate(source.gameObject, parent);
        copy.name = name;
        copy.transform.position = source.position + new Vector3(offsetX, 0f, 0f);
        EditorUtility.SetDirty(copy);
    }

    static Transform FindOrCreateUniqueChild(Transform parent, string name, DynamicEntityExportGenerator template)
    {
        if (parent == null || template == null)
        {
            return null;
        }

        Transform keeper = null;
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name != name)
            {
                continue;
            }

            if (keeper == null)
            {
                keeper = child;
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        if (keeper != null)
        {
            return keeper;
        }

        var copy = Object.Instantiate(template.gameObject, parent);
        copy.name = name;
        return copy.transform;
    }

    static void DuplicateNamedPoint(Transform source, Transform parent, string name, float offsetX)
    {
        if (source == null || parent == null) throw new System.InvalidOperationException($"Missing named point source={source} parent={parent}.");
        var old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var copy = Object.Instantiate(source.gameObject, parent);
        copy.name = name;
        copy.transform.position = source.position + new Vector3(offsetX, 0f, 0f);
        var point = copy.GetComponent<NamePointGenerator>();
        if (point != null)
        {
            var info = point.Info;
            info.Name = name;
            info.Position = copy.transform.position;
            point.Info = info;
        }
        EditorUtility.SetDirty(copy);
    }

    static Transform EnsureNamedPointAt(Transform parent, string name, Vector3 position)
    {
        var point = parent.Find(name);
        if (point == null)
        {
            var go = new GameObject(name);
            point = go.transform;
            point.SetParent(parent, false);
        }

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

    static void RenameRoomId(Transform mapVariant, string name, string roomId)
    {
        var room = mapVariant.Find("Room/" + name) ?? mapVariant.Find("GridRoot/" + name);
        if (room == null) return;
        var provider = room.GetComponent<Map.Scene.MapRoomProvider>();
        if (provider != null) provider.RoomId = roomId;
    }

    static CommonCheckCond MakeNoneCond() => CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse("{\"type\":0,\"param1\":0,\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"\",\"param6\":\"\"}"));

    static CommonCheckCond MakeCheckVariable(string key, bool shouldExist)
    {
        var p = shouldExist ? 0 : 1;
        return CommonCheckCond.DeserializeCommonCheckCond(JSON.Parse("{\"type\":2,\"param1\":" + p + ",\"param2\":0,\"param3\":0,\"param4\":0,\"param5\":\"" + key + "\",\"param6\":\"\"}"));
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
}
#endif
