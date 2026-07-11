using Config.Map;
using My.Map.Scene;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestLootPointAssembler
{
    const string ForestScene = "Assets/Scenes/Main/Forest_01_Editor.unity";
    const string LootPointConfigDir = "Assets/Resources/Config/Entity/LootPoint";
    const string LootPointPrefabDir = "Assets/Resources/Prefab/Presentations/LootPoint";
    const string LootPointTemplatePrefab = LootPointPrefabDir + "/spoil_small.prefab";

    struct LootPointDef
    {
        public string OldUniqName;
        public string NewUniqName;
        public string CfgId;
        public string ShowName;
        public int DropBundleId;
        public string SpritePath;
        public string SpriteName;
        public float ViewScale;
        public float ViewYOffset;
    }

    static readonly LootPointDef[] LootPoints =
    {
        new()
        {
            OldUniqName = "forest_dew_nw_01",
            NewUniqName = "forest_grass_nw_01",
            CfgId = "forest_grass_clump",
            ShowName = "草丛",
            DropBundleId = 920,
            SpritePath = "Assets/MyTextures/scene/tall_grass_single.png",
            SpriteName = "tall_grass_single_0",
            ViewScale = 0.75f,
            ViewYOffset = 0.08f,
        },
        new()
        {
            OldUniqName = "forest_dew_sw_01",
            NewUniqName = "forest_leaf_pile_sw_01",
            CfgId = "forest_leaf_pile",
            ShowName = "落叶堆",
            DropBundleId = 922,
            SpritePath = "Assets/Arts/Tile/basic_01/template_autumn_15.png",
            SpriteName = "",
            ViewScale = 0.55f,
            ViewYOffset = 0.04f,
        },
        new()
        {
            OldUniqName = "forest_dew_center_01",
            NewUniqName = "forest_bush_center_01",
            CfgId = "forest_bush",
            ShowName = "灌木",
            DropBundleId = 921,
            SpritePath = "Assets/MyTextures/scene/craft1/Bush3.png",
            SpriteName = "",
            ViewScale = 0.7f,
            ViewYOffset = 0.12f,
        },
        new()
        {
            OldUniqName = "forest_dew_ne_01",
            NewUniqName = "forest_grass_ne_01",
            CfgId = "forest_grass_clump",
            ShowName = "草丛",
            DropBundleId = 920,
            SpritePath = "Assets/MyTextures/scene/tall_grass_single.png",
            SpriteName = "tall_grass_single_4",
            ViewScale = 0.75f,
            ViewYOffset = 0.08f,
        },
    };

    [MenuItem("Window/Map/Assemble Forest Loot Points")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        EnsureLootPointAssets();
        ConfigureForestScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ForestLootPointAssembler] Done.");
    }

    static void EnsureLootPointAssets()
    {
        EnsureFolder("Assets/Resources/Config/Entity", "LootPoint");
        EnsureFolder("Assets/Resources/Prefab/Presentations", "LootPoint");

        EnsureLootPointConfig("forest_grass_clump", "草丛", 920);
        EnsureLootPointConfig("forest_bush", "灌木", 921);
        EnsureLootPointConfig("forest_leaf_pile", "落叶堆", 922);

        foreach (var def in LootPoints)
        {
            EnsureLootPointPrefab(def);
        }
    }

    static void EnsureLootPointConfig(string cfgId, string showName, int dropBundleId)
    {
        var path = $"{LootPointConfigDir}/{cfgId}.asset";
        var config = AssetDatabase.LoadAssetAtPath<MapLootPointConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MapLootPointConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.CfgId = cfgId;
        config.ShowName = showName;
        config.PrefabName = cfgId;
        config.DefaultLocked = false;
        config.UnlockItemCost.Clear();
        config.LootOpenTime = 0.2f;
        config.LootOverrideAnim = string.Empty;
        config.LootRequiment = new MapLootPointConfig.CLootRequiment
        {
            ReqType = MapLootPointConfig.ELootReqType.None,
            Param1 = 0,
            Param2 = 0,
            Param3 = string.Empty,
            Param4 = string.Empty,
        };
        config.DefaultDropId = dropBundleId;
        config.HasOwner = false;
        config.IsPrecious = false;
        EditorUtility.SetDirty(config);
    }

    static void EnsureLootPointPrefab(LootPointDef def)
    {
        var prefabPath = $"{LootPointPrefabDir}/{def.CfgId}.prefab";
        var root = PrefabUtility.LoadPrefabContents(LootPointTemplatePrefab);
        try
        {
            root.name = def.CfgId;

            var sprite = LoadSprite(def.SpritePath, def.SpriteName);
            if (sprite == null)
            {
                throw new System.InvalidOperationException($"Sprite not found: {def.SpritePath} {def.SpriteName}");
            }

            var icon = root.GetComponentInChildren<SpriteRenderer>(true);
            if (icon == null)
            {
                throw new System.InvalidOperationException($"SpriteRenderer not found in {LootPointTemplatePrefab}");
            }

            icon.sprite = sprite;
            icon.transform.localScale = Vector3.one * def.ViewScale;
            icon.transform.localPosition = new Vector3(0f, def.ViewYOffset, 0f);

            var presenter = root.GetComponent<LootPointPresenter>();
            if (presenter != null)
            {
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("icon").objectReferenceValue = icon;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (var col in root.GetComponentsInChildren<CircleCollider2D>(true))
            {
                if (col.gameObject.name == "Target")
                {
                    col.radius = 0.25f;
                    col.transform.localPosition = new Vector3(0f, def.ViewYOffset, 0f);
                }
                else
                {
                    col.radius = 0.12f;
                    col.transform.localPosition = Vector3.zero;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Sprite LoadSprite(string path, string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    static void ConfigureForestScene()
    {
        var root = OpenEditorScene(ForestScene);
        foreach (var def in LootPoints)
        {
            ApplyLootPointDef(root, def);
        }

        SaveAndExport(root);
    }

    static void ApplyLootPointDef(MapChunkEditorRoot root, LootPointDef def)
    {
        var generator = FindGenerator(root, def.NewUniqName) ?? FindGenerator(root, def.OldUniqName);
        if (generator == null)
        {
            Debug.LogWarning($"[ForestLootPointAssembler] Loot point not found: {def.OldUniqName}");
            return;
        }

        generator.gameObject.name = def.NewUniqName;
        generator.RefreshInfo.UniqName = def.NewUniqName;
        if (generator.RefreshInfo.InitInfo is not EntityInitInfo4LootPoint lootInfo)
        {
            lootInfo = new EntityInitInfo4LootPoint
            {
                Position = generator.transform.position,
                FaceDir = Vector2.zero,
            };
            generator.RefreshInfo.InitInfo = lootInfo;
        }

        lootInfo.CfgId = def.CfgId;
        lootInfo.Position = generator.transform.position;
        lootInfo.FaceDir = Vector2.zero;
        EditorUtility.SetDirty(generator);
        EditorUtility.SetDirty(generator.gameObject);
    }

    static DynamicEntityExportGenerator FindGenerator(MapChunkEditorRoot root, string uniqName)
    {
        foreach (var generator in root.GetComponentsInChildren<DynamicEntityExportGenerator>(true))
        {
            if (generator.RefreshInfo != null && generator.RefreshInfo.UniqName == uniqName)
            {
                return generator;
            }

            if (generator.gameObject.name == uniqName)
            {
                return generator;
            }
        }

        return null;
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

        Debug.Log($"[ForestLootPointAssembler] Exported {variantKey}: {overlayResult.Message}");
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
