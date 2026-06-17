#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Config.Map;
using My.Config;
using My.Map;
using My.Map.DualGrid;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ForestMapGenerationTool
{
    const string SceneName = "Forest_01";
    const string OverlayId = "forest_01";
    const string MapDataName = "forest_01";
    const string SceneFolder = "Assets/Scenes/Main";
    const string RuntimeScenePath = SceneFolder + "/" + SceneName + ".unity";
    const string EditorScenePath = SceneFolder + "/" + SceneName + "_Editor.unity";

    static readonly Vector2Int MapMin = new Vector2Int(0, 0);
    static readonly Vector2Int MapMax = new Vector2Int(64, 64);

    [MenuItem("Tools/Map/Generate Forest 01")]
    public static void GenerateForest01Menu()
    {
        GenerateForest01AndExport();
    }

    public static void GenerateForest01AndExport()
    {
        EnsureRuntimeScene();
        var editorRoot = EnsureEditorScene();
        BuildForestEditorScene(editorRoot);
        EnsureForestInteractConfigs();

        EditorSceneManager.SaveScene(editorRoot.gameObject.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var editPaintRect = editorRoot.PaintWorldRect;
        MapChunkExportCore.ExportResult chunkResult;
        try
        {
            // Batchmode + -nographics can crash while capturing painted backgrounds through URP.
            // The first forest pass only needs tile chunks and dynamic overlay data.
            editorRoot.PaintWorldRect = default;
            chunkResult = MapChunkExportCore.Export(
                editorRoot,
                SceneName,
                editorRoot.ChunkWorldSize,
                editorRoot.ChunkOrigin);
            if (!chunkResult.Success)
            {
                throw new System.Exception("[ForestMap] Chunk export failed: " + chunkResult.Message);
            }
        }
        finally
        {
            editorRoot.PaintWorldRect = editPaintRect;
            EditorUtility.SetDirty(editorRoot);
            EditorSceneManager.SaveScene(editorRoot.gameObject.scene);
        }

        var overlayResult = MapOverlayExportCore.ExportOverlay(
            editorRoot.gameObject,
            editorRoot,
            OverlayId,
            MapDataName);
        if (!overlayResult.Success)
        {
            throw new System.Exception("[ForestMap] Overlay export failed: " + overlayResult.Message);
        }

        Debug.Log("[ForestMap] Generated Forest_01. " + chunkResult.Message + "\n" + overlayResult.Message);
    }

    static void EnsureRuntimeScene()
    {
        EnsureFolder(SceneFolder);
        if (File.Exists(RuntimeScenePath))
        {
            AddRuntimeSceneToBuildSettings(RuntimeScenePath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneName;

        var areaRoot = new GameObject("AreaRoot");
        areaRoot.AddComponent<WorldAreaRoot>();
        var mapVariantRoot = CreateChild(areaRoot.transform, MapVariantSceneHierarchy.MapVariantRootName);
        var gridRoot = new GameObject(WorldAreaRoot.SceneGridRootName);
        gridRoot.transform.SetParent(mapVariantRoot, false);
        gridRoot.AddComponent<Grid>().cellSize = Vector3.one;

        EditorSceneManager.SaveScene(scene, RuntimeScenePath);
        AddRuntimeSceneToBuildSettings(RuntimeScenePath);
    }

    static MapChunkEditorRoot EnsureEditorScene()
    {
        EnsureFolder(SceneFolder);
        if (File.Exists(EditorScenePath))
        {
            EditorSceneManager.OpenScene(EditorScenePath, OpenSceneMode.Single);
            var existing = UnityEngine.Object.FindObjectOfType<MapChunkEditorRoot>();
            if (existing != null)
            {
                return existing;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = SceneName + "_Editor";

        var areaRoot = new GameObject("AreaRoot");
        var editorRoot = areaRoot.AddComponent<MapChunkEditorRoot>();
        editorRoot.MapVariantSceneName = SceneName;
        editorRoot.ChunkOrigin = Vector2.zero;
        editorRoot.GroundLayerNames = new[] { "Ground" };
        editorRoot.PaintWorldRect = new Rect(0f, 0f, 64f, 64f);

        var mapVariantRoot = CreateChild(areaRoot.transform, MapVariantSceneHierarchy.MapVariantRootName);
        CreateChild(mapVariantRoot, MapVariantSceneHierarchy.RoomFolderName);
        CreateChild(mapVariantRoot, MapVariantSceneHierarchy.DecorateFolderName);
        CreateChild(mapVariantRoot, MapVariantSceneHierarchy.TriggerFolderName);

        var gridRoot = new GameObject(WorldAreaRoot.SceneGridRootName);
        gridRoot.transform.SetParent(mapVariantRoot, false);
        gridRoot.AddComponent<Grid>().cellSize = Vector3.one;

        CreateChild(areaRoot.transform, "NamedPath");
        CreateChild(areaRoot.transform, "NamedPoint");
        var dynamicRoot = CreateChild(areaRoot.transform, MapVariantSceneHierarchy.DynamicRootName);
        CreateChild(dynamicRoot, MapVariantSceneHierarchy.CommonFolderName);
        CreateChild(dynamicRoot, OverlayId);

        var portalNetworks = CreateChild(areaRoot.transform, "PortalNetworks");
        portalNetworks.gameObject.AddComponent<PortalNetworkProvider>();
        CreateChild(areaRoot.transform, "NavObc");
        CreateChild(areaRoot.transform, "Col");

        EditorSceneManager.SaveScene(scene, EditorScenePath);
        return editorRoot;
    }

    static void BuildForestEditorScene(MapChunkEditorRoot editorRoot)
    {
        editorRoot.MapVariantSceneName = SceneName;
        editorRoot.ChunkOrigin = Vector2.zero;
        editorRoot.GroundLayerNames = new[] { "Ground" };
        editorRoot.PaintWorldRect = new Rect(0f, 0f, 64f, 64f);

        var mapVariantRoot = EnsureChild(editorRoot.transform, MapVariantSceneHierarchy.MapVariantRootName);
        var gridRoot = EnsureGridRoot(mapVariantRoot);
        ClearGeneratedChildren(gridRoot);
        BuildTilemaps(gridRoot);

        var decorateRoot = EnsureChild(mapVariantRoot, MapVariantSceneHierarchy.DecorateFolderName);
        ClearGeneratedChildren(decorateRoot);
        PlaceDecorations(decorateRoot);

        var namedPointRoot = EnsureChild(editorRoot.transform, "NamedPoint");
        ClearGeneratedChildren(namedPointRoot);
        AddNamedPoint(namedPointRoot, "born_center", new Vector3(32f, 32f, 0f), ENamedPointType.BornPos);
        AddNamedPoint(namedPointRoot, "entry_south", new Vector3(32f, 7f, 0f), ENamedPointType.Normal);
        AddNamedPoint(namedPointRoot, "heart_gate", new Vector3(32f, 56f, 0f), ENamedPointType.Normal);

        var dynamicRoot = EnsureChild(editorRoot.transform, MapVariantSceneHierarchy.DynamicRootName);
        EnsureChild(dynamicRoot, MapVariantSceneHierarchy.CommonFolderName);
        var overlayRoot = EnsureChild(dynamicRoot, OverlayId);
        ClearGeneratedChildren(overlayRoot);
        PlaceDynamicEntities(overlayRoot);

        EditorUtility.SetDirty(editorRoot);
    }

    static void BuildTilemaps(Transform gridRoot)
    {
        var grassBrush = LoadTile<TileBase>("Assets/Arts/Tile/basic_01/tile_asset/ground_grasss_127.asset");
        var highGrassBrush = LoadTile<TileBase>("Assets/Arts/Tile/basic_01/tile_asset/high_grass/template_high_grass_15_6.asset");
        var registry = AssetDatabase.LoadAssetAtPath<DualGridBrushRegistry>("Assets/Arts/DualTile/DualGridBrushRegistry.asset");
        var viewTile = AssetDatabase.LoadAssetAtPath<DualGridViewTile>("Assets/Arts/DualTile/DualGridViewTile.asset");
        if (grassBrush == null || highGrassBrush == null || registry == null || viewTile == null)
        {
            throw new System.Exception("[ForestMap] DualGrid tile assets are missing.");
        }

        var ground = CreateTilemapLayer(gridRoot, "Ground", 0);
        var hole = CreateTilemapLayer(gridRoot, "Hole", 0);
        hole.gameObject.SetActive(false);

        var dualGo = new GameObject("ForestDualGrid");
        dualGo.transform.SetParent(gridRoot, false);
        var dual = dualGo.AddComponent<DualTileMap>();
        dual.Grid = gridRoot.GetComponent<Grid>();
        dual.BrushRegistry = registry;
        dual.ViewTile = viewTile;

        var data = CreateTilemapLayer(dualGo.transform, "Data", -10);
        var view = CreateTilemapLayer(dualGo.transform, "View", 1);
        dual.DataTilemap = data;
        dual.ViewTilemap = view;
        dual.EnsureViewOffset();

        for (int x = MapMin.x; x < MapMax.x; x++)
        {
            for (int y = MapMin.y; y < MapMax.y; y++)
            {
                if (!IsWalkableForestCell(x, y))
                {
                    continue;
                }

                var pos = new Vector3Int(x, y, 0);
                ground.SetTile(pos, grassBrush);
                data.SetTile(pos, ShouldUseHighGrass(x, y) ? highGrassBrush : grassBrush);
            }
        }

        ground.CompressBounds();
        data.CompressBounds();
        dual.RefreshAll();
        view.CompressBounds();
    }

    static bool IsWalkableForestCell(int x, int y)
    {
        if (x < 4 || x >= 60 || y < 4 || y >= 60)
        {
            return false;
        }

        bool central = InEllipse(x, y, 32, 32, 10, 9);
        bool north = x >= 27 && x <= 37 && y >= 31 && y <= 58;
        bool south = x >= 28 && x <= 36 && y >= 6 && y <= 33;
        bool west = y >= 28 && y <= 36 && x >= 6 && x <= 33;
        bool east = y >= 28 && y <= 36 && x >= 31 && x <= 58;
        bool northwest = InEllipse(x, y, 15, 48, 8, 6);
        bool northeast = InEllipse(x, y, 50, 49, 8, 7);
        bool southwest = InEllipse(x, y, 16, 15, 8, 7);
        bool southeast = InEllipse(x, y, 50, 15, 7, 6);
        bool ring = InEllipse(x, y, 32, 32, 24, 22) && !InEllipse(x, y, 32, 32, 17, 15);

        if (!(central || north || south || west || east || northwest || northeast || southwest || southeast || ring))
        {
            return false;
        }

        return !IsForestRockPocket(x, y);
    }

    static bool IsForestRockPocket(int x, int y)
    {
        return InEllipse(x, y, 23, 42, 3, 2) ||
               InEllipse(x, y, 44, 38, 3, 3) ||
               InEllipse(x, y, 23, 23, 3, 2) ||
               InEllipse(x, y, 40, 18, 2, 3);
    }

    static bool ShouldUseHighGrass(int x, int y)
    {
        bool edgePatch = InEllipse(x, y, 15, 48, 7, 5) ||
                         InEllipse(x, y, 50, 49, 7, 5) ||
                         InEllipse(x, y, 16, 15, 6, 5) ||
                         InEllipse(x, y, 50, 15, 5, 4);
        bool dapple = PositiveMod(x * 37 + y * 19, 17) == 0;
        return edgePatch || dapple;
    }

    static bool InEllipse(int x, int y, int cx, int cy, int rx, int ry)
    {
        float dx = (x - cx) / (float)rx;
        float dy = (y - cy) / (float)ry;
        return dx * dx + dy * dy <= 1f;
    }

    static int PositiveMod(int value, int mod)
    {
        int result = value % mod;
        return result < 0 ? result + mod : result;
    }

    static void PlaceDecorations(Transform decorateRoot)
    {
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/tall_grass_patch.prefab", new Vector3(15f, 49f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/tall_grass_patch.prefab", new Vector3(49f, 49f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/tall_grass_single.prefab", new Vector3(22f, 39f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/tall_grass_single.prefab", new Vector3(42f, 24f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/Stone1_grass_shadow.prefab", new Vector3(23f, 42f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/Stone1_grass_shadow.prefab", new Vector3(44f, 38f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/guanmu.prefab", new Vector3(31f, 59f, 0f), Vector3.one);
        PlacePrefab(decorateRoot, "Assets/Arts/SceneObj/guanmu_tall.prefab", new Vector3(52f, 50f, 0f), Vector3.one);
    }

    static void PlaceDynamicEntities(Transform root)
    {
        AddDynamic(root, "forest_obstacle_south_01", new Vector3(32f, 21f, 0f),
            new EntityInitInfo4RemovableObstacle { CfgId = "removable_test_01" }, false);
        AddDynamic(root, "forest_obstacle_west_01", new Vector3(21f, 32f, 0f),
            new EntityInitInfo4RemovableObstacle { CfgId = "removable_test_01" }, false);
        AddDynamic(root, "forest_obstacle_east_01", new Vector3(44f, 32f, 0f),
            new EntityInitInfo4RemovableObstacle { CfgId = "removable_test_01" }, false);

        AddDynamic(root, "forest_dew_sw_01", new Vector3(15f, 16f, 0f),
            new EntityInitInfo4LootPoint { CfgId = "spoil_small" }, false);
        AddDynamic(root, "forest_dew_nw_01", new Vector3(14f, 48f, 0f),
            new EntityInitInfo4LootPoint { CfgId = "spoil_small" }, false);
        AddDynamic(root, "forest_dew_ne_01", new Vector3(50f, 49f, 0f),
            new EntityInitInfo4LootPoint { CfgId = "spoil_small" }, false);
        AddDynamic(root, "forest_dew_center_01", new Vector3(35f, 35f, 0f),
            new EntityInitInfo4LootPoint { CfgId = "spoil_small" }, false);

        AddDynamic(root, "forest_heart_altar", new Vector3(32f, 55f, 0f),
            new EntityInitInfo4InteractPoint { CfgId = "forest_heart_altar" }, false);

        AddDynamic(root, "forest_slime_green_01", new Vector3(18f, 47f, 0f),
            NewNpc("slime_green", Vector2.down), true);
        AddDynamic(root, "forest_slime_brown_01", new Vector3(49f, 17f, 0f),
            NewNpc("slime_brown", Vector2.left), true);
        AddDynamic(root, "forest_slime_crystal_01", new Vector3(51f, 50f, 0f),
            NewNpc("slime_crystal", Vector2.left), true);
        AddDynamic(root, "forest_cannon_north_01", new Vector3(31f, 51f, 0f),
            NewNpc("area_cannon", Vector2.down), false);
        AddDynamic(root, "forest_cannon_east_01", new Vector3(53f, 32f, 0f),
            NewNpc("area_cannon", Vector2.left), false);
    }

    static EntityInitInfo4Npc NewNpc(string cfgId, Vector2 faceDir)
    {
        return new EntityInitInfo4Npc
        {
            CfgId = cfgId,
            FaceDir = faceDir,
            IsPeace = false,
        };
    }

    static void AddDynamic(Transform parent, string uniqName, Vector3 position, EntityInitInfo initInfo, bool respawn)
    {
        var go = new GameObject(uniqName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        initInfo.Position = position;
        var generator = go.AddComponent<DynamicEntityExportGenerator>();
        generator.RefreshInfo = new DynamicEntityRefreshInfo
        {
            UniqName = uniqName,
            WillRespawn = respawn,
            RespawnInterval = respawn ? 30f : 0f,
            InitInfo = initInfo,
        };
    }

    static void AddNamedPoint(Transform parent, string pointName, Vector3 position, ENamedPointType type)
    {
        var go = new GameObject(pointName);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var marker = go.AddComponent<NamePointGenerator>();
        marker.Info = new NamedPoint
        {
            Name = pointName,
            PointType = type,
            Position = position,
            Rotation = Quaternion.identity,
            Scale = Vector3.one,
        };
    }

    static void EnsureForestInteractConfigs()
    {
        const string folder = "Assets/Resources/Config/Entity/InteractPoint";
        EnsureFolder(folder);
        const string path = folder + "/forest_heart_altar.asset";
        var config = AssetDatabase.LoadAssetAtPath<MapInteractPointConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MapInteractPointConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.CfgId = "forest_heart_altar";
        config.ShowName = "Forest Heart";
        config.PrefabName = "empty";
        config.NameOffset = -1f;
        config.PersistByUniqName = true;
        config.MainStatusInfo = new MapInteractPointConfig.StatusInfo
        {
            StatusId = 0,
            HasBlock = true,
            InteractInfos = new List<MapInteractInfo>
            {
                new MapInteractInfo
                {
                    InteractId = 1,
                    Label = "Offer",
                    UnLabel = "Offer(no)",
                    HideWhenFail = false,
                    Outputs = new List<LogicInteractOutput>
                    {
                        new LogicInteractOutput
                        {
                            OutputType = LogicInteractOutput.EOutputType.ChangeSelfStatus,
                            Param1 = 1,
                        },
                    },
                },
            },
        };
        config.ExtraStatusInfos = new List<MapInteractPointConfig.StatusInfo>
        {
            new MapInteractPointConfig.StatusInfo
            {
                StatusId = 1,
                HasBlock = false,
                InteractInfos = new List<MapInteractInfo>(),
            },
        };
        config.InitState = 0;
        config.StateChangeRules = new List<MapInteractPointConfig.StateChangeRule>();
        EditorUtility.SetDirty(config);
    }

    static Tilemap CreateTilemapLayer(Transform parent, string layerName, int sortingOrder)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(parent, false);
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return tilemap;
    }

    static Transform EnsureGridRoot(Transform mapVariantRoot)
    {
        var gridRoot = mapVariantRoot.Find(WorldAreaRoot.SceneGridRootName);
        if (gridRoot == null)
        {
            var go = new GameObject(WorldAreaRoot.SceneGridRootName);
            go.transform.SetParent(mapVariantRoot, false);
            gridRoot = go.transform;
        }

        var grid = gridRoot.GetComponent<Grid>();
        if (grid == null)
        {
            grid = gridRoot.gameObject.AddComponent<Grid>();
        }

        grid.cellSize = Vector3.one;
        gridRoot.gameObject.SetActive(true);
        return gridRoot;
    }

    static void PlacePrefab(Transform parent, string prefabPath, Vector3 position, Vector3 scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            return;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return;
        }

        instance.name = Path.GetFileNameWithoutExtension(prefabPath);
        instance.transform.position = position;
        instance.transform.localScale = scale;
    }

    static T LoadTile<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    static void ClearGeneratedChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    static Transform EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        return existing != null ? existing : CreateChild(parent, name);
    }

    static Transform CreateChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    static void AddRuntimeSceneToBuildSettings(string scenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == scenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
