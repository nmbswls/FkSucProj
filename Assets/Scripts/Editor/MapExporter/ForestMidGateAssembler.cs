using System.Collections.Generic;
using cfg.demo;
using My;
using My.Map.DualGrid;
using My.MapExport;
using SimpleJSON;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ForestMidGateAssembler
{
    const string ForestScene = "Assets/Scenes/Main/Forest_01_Editor.unity";
    const string OverlayId = "forest_01";
    const string BuffId = "forest_reed_blessing";

    [MenuItem("Window/Map/Assemble Forest Mid Gate")]
    public static void Run()
    {
        var root = OpenEditorScene(ForestScene);
        var dual = Object.FindObjectOfType<DualTileMap>(true);
        if (dual == null || dual.DataTilemap == null)
        {
            throw new System.InvalidOperationException("Forest dual tilemap not found.");
        }

        EnsureNamedPoint(root, "forest_mid_vine_top", new Vector3(31f, 39f, 0f), ENamedPointType.Normal);
        EnsureNamedPoint(root, "forest_mid_vine_land", new Vector3(31f, 42f, 0f), ENamedPointType.Normal);
        EnsureNamedPoint(root, "forest_mid_jump_takeoff", new Vector3(23f, 36f, 0f), ENamedPointType.Normal);
        EnsureNamedPoint(root, "forest_mid_jump_land", new Vector3(29f, 39f, 0f), ENamedPointType.Normal);

        PaintMidGateBand(dual);
        EnsureInteractConfigs();
        EnsureSouthOuterRingExpansion(root);
        EnsureEastPoacherOutpost(root);
        EnsureNorthHeartlandExpansion(root);
        EnsureForestInteractables(root);
        SaveAndExport(root);
    }

    static void PaintMidGateBand(DualTileMap dual)
    {
        var data = dual.DataTilemap;
        var fill = ResolveFirstDataTile(data);
        if (fill == null)
        {
            return;
        }

        for (int x = 18; x <= 46; x++)
        {
            if (x >= 28 && x <= 34)
            {
                continue;
            }

            data.SetTile(new Vector3Int(x, 37, 0), fill);
            data.SetTile(new Vector3Int(x, 38, 0), fill);
        }

        for (int x = 29; x <= 33; x++)
        {
            data.SetTile(new Vector3Int(x, 37, 0), null);
            data.SetTile(new Vector3Int(x, 38, 0), null);
        }

        for (int x = 24; x <= 26; x++)
        {
            data.SetTile(new Vector3Int(x, 37, 0), null);
        }

        dual.RefreshAll();
        EditorUtility.SetDirty(data);
        if (dual.ViewTilemap != null)
        {
            EditorUtility.SetDirty(dual.ViewTilemap);
        }
    }

    static TileBase ResolveFirstDataTile(Tilemap tilemap)
    {
        tilemap.CompressBounds();
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(pos);
            if (tile != null)
            {
                return tile;
            }
        }

        return null;
    }

    static void EnsureInteractConfigs()
    {
        EnsureFolder("Assets/Resources/Config/Entity", "InteractPoint");
        EnsureFolder("Assets/Resources/Prefab/Presentations", "InteractPoint");

        CloneInteractConfig("vine_01_seed", "forest_mid_vine_gate", cfg =>
        {
            cfg.ShowName = "藤蔓墙";
            cfg.PrefabName = "vine_seed";
            cfg.NameOffset = 0.8f;
            cfg.MainStatusInfo.InteractInfos[0].Label = "以花露祝福催动藤蔓";
            cfg.MainStatusInfo.InteractInfos[0].UnLabel = "以花露祝福催动藤蔓";
            cfg.MainStatusInfo.InteractInfos[0].CheckInteractCond = new List<My.Config.InteractCheckCond>
            {
                new My.Config.InteractCheckCond
                {
                    CheckType = My.Config.InteractCheckCond.ECheckType.PlayerHasBuff,
                    Param3 = BuffId,
                }
            };
            cfg.MainStatusInfo.InteractInfos[0].Outputs[1].Param3 = "forest_mid_vine_grown";
            cfg.ExtraStatusInfos[0].InteractInfos[0].CheckCommonCond[0] = MakeCheckVariable("forest_mid_vine_grown", true);
            cfg.ExtraStatusInfos[0].InteractInfos[0].Outputs[0].Param3 = "forest_mid_vine_top";
            cfg.ExtraStatusInfos[0].InteractInfos[0].Outputs[0].Param4 = "forest_mid_vine_land";
        });

        CloneInteractConfig("vine_01_top", "forest_mid_vine_top", cfg =>
        {
            cfg.MainStatusInfo.InteractInfos[0].CheckCommonCond[0] = MakeCheckVariable("forest_mid_vine_grown", true);
            cfg.MainStatusInfo.InteractInfos[0].Outputs[0].Param3 = "forest_mid_vine_land";
            cfg.MainStatusInfo.InteractInfos[0].Outputs[0].Param4 = "forest_mid_vine_land";
        });

        CloneInteractConfig("forest_heart_altar", "forest_reed_pillar", cfg =>
        {
            cfg.ShowName = "柱状灯心草";
            cfg.PrefabName = "forest_heart_altar";
            cfg.MainStatusInfo.InteractInfos.Clear();
            cfg.MainStatusInfo.InteractInfos.Add(new My.Config.MapInteractInfo
            {
                InteractId = 1,
                Label = "触碰灯心草",
                UnLabel = "触碰灯心草",
                HideWhenFail = false,
                NeedDist = 0.6f,
                Outputs = new List<My.Config.LogicInteractOutput>
                {
                    new My.Config.LogicInteractOutput
                    {
                        OutputType = My.Config.LogicInteractOutput.EOutputType.AddBuff,
                        Param1 = 180000,
                        Param2 = 1,
                        Param3 = BuffId,
                    },
                }
            });
        });

        CloneInteractConfig("forest_heart_altar", "forest_spring_leaf", cfg =>
        {
            cfg.ShowName = "弹簧叶片";
            cfg.PrefabName = "forest_heart_altar";
            cfg.MainStatusInfo.InteractInfos.Clear();
            cfg.MainStatusInfo.InteractInfos.Add(new My.Config.MapInteractInfo
            {
                InteractId = 1,
                Label = "借叶片弹起",
                UnLabel = "借叶片弹起",
                HideWhenFail = false,
                NeedDist = 0.7f,
                Outputs = new List<My.Config.LogicInteractOutput>
                {
                    new My.Config.LogicInteractOutput
                    {
                        OutputType = My.Config.LogicInteractOutput.EOutputType.RelocateFakeJump2D,
                        Param3 = "forest_mid_jump_takeoff",
                        Param4 = "forest_mid_jump_land",
                    },
                }
            });
        });
    }

    static void EnsureForestInteractables(MapChunkEditorRoot root)
    {
        EnsureDynamicInteractPoint(root, "forest_mid_reed_01", new Vector3(24f, 33f, 0f), "forest_reed_pillar");
        EnsureDynamicInteractPoint(root, "forest_mid_vine_gate_01", new Vector3(31f, 36f, 0f), "forest_mid_vine_gate");
        EnsureDynamicInteractPoint(root, "forest_mid_vine_top_01", new Vector3(31f, 40f, 0f), "forest_mid_vine_top");
        EnsureDynamicInteractPoint(root, "forest_mid_spring_leaf_01", new Vector3(22f, 35f, 0f), "forest_spring_leaf");
    }

    static void EnsureDynamicInteractPoint(MapChunkEditorRoot root, string uniqName, Vector3 position, string cfgId)
    {
        var overlayRoot = EnsureOverlayRoot(root, OverlayId);
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
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            RespawnInterval = 0f,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4InteractPoint
            {
                CfgId = cfgId,
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.zero,
                OrgFactionId = My.Map.Entity.EFactionId.None,
            },
        };

        EditorUtility.SetDirty(go);
    }

    static void CloneInteractConfig(string srcId, string dstId, System.Action<Config.Map.MapInteractPointConfig> mutator)
    {
        var src = AssetDatabase.LoadAssetAtPath<Config.Map.MapInteractPointConfig>($"Assets/Resources/Config/Entity/InteractPoint/{srcId}.asset");
        if (src == null)
        {
            throw new System.InvalidOperationException("missing interact cfg: " + srcId);
        }

        var dstPath = $"Assets/Resources/Config/Entity/InteractPoint/{dstId}.asset";
        var dst = AssetDatabase.LoadAssetAtPath<Config.Map.MapInteractPointConfig>(dstPath);
        if (dst == null)
        {
            dst = Object.Instantiate(src);
            dst.CfgId = dstId;
            dst.name = dstId;
            AssetDatabase.CreateAsset(dst, dstPath);
        }

        mutator?.Invoke(dst);
        EditorUtility.SetDirty(dst);
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

    static Transform EnsureNamedPoint(MapChunkEditorRoot root, string pointName, Vector3 fallbackPosition, ENamedPointType pointType)
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

    static void EnsureFolder(string parent, string child)
    {
        var path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
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

        Debug.Log($"[ForestMidGateAssembler] Exported {variantKey}: {overlayResult.Message}");
    }

static void EnsureSouthOuterRingExpansion(MapChunkEditorRoot root)
    {
        EnsureDynamicLootPoint(root, "south_grass_loot_01", new Vector3(31f, 15f, 0f), "forest_grass_clump");
        EnsureDynamicLootPoint(root, "south_leaf_loot_01", new Vector3(34f, 10f, 0f), "forest_leaf_pile");
        EnsureDynamicLootPoint(root, "south_scrap_patch_01", new Vector3(28f, 12f, 0f), "forest_bush");
        EnsureDynamicLootPoint(root, "south_scrap_patch_02", new Vector3(36f, 13f, 0f), "forest_bush");
        EnsureDynamicNpc(root, "south_slime_green_02", new Vector3(27f, 16f, 0f), "slime_green", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "south_slime_green_03", new Vector3(37f, 16f, 0f), "slime_green", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "south_fly_swarm_02", new Vector3(33f, 18f, 0f), "forest_fly_swarm", My.Map.Entity.EFactionId.Beast, "default_monster");
    }

    static void EnsureMidSecondPassSetpiece(MapChunkEditorRoot root)
    {
        EnsureDynamicLootPoint(root, "mid_watch_scrap_01", new Vector3(27f, 34f, 0f), "forest_leaf_pile");
        EnsureDynamicLootPoint(root, "mid_highgrass_hide_01", new Vector3(24f, 39f, 0f), "forest_grass_clump");
        EnsureDynamicNpc(root, "mid_slime_brown_02", new Vector3(40f, 35f, 0f), "slime_brown", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "mid_fly_swarm_02", new Vector3(26f, 40f, 0f), "forest_fly_swarm", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "poacher_scout_01", new Vector3(39f, 34f, 0f), "home_robber_n", My.Map.Entity.EFactionId.Bandit, "default_monster");
        EnsureDynamicNpc(root, "poacher_scout_02", new Vector3(42f, 36f, 0f), "home_robber_n", My.Map.Entity.EFactionId.Bandit, "default_monster");
    }
    static void EnsureEastPoacherOutpost(MapChunkEditorRoot root)
    {
        EnsureDynamicNpc(root, "poacher_guard_01", new Vector3(48f, 28f, 0f), "home_robber_n", My.Map.Entity.EFactionId.Bandit, "default_monster");
        EnsureDynamicNpc(root, "poacher_guard_02", new Vector3(51f, 32f, 0f), "home_robber_n", My.Map.Entity.EFactionId.Bandit, "default_monster");
        EnsureDynamicNpc(root, "poacher_hunter_01", new Vector3(45f, 35f, 0f), "home_robber_n", My.Map.Entity.EFactionId.Bandit, "default_monster");
        EnsureDynamicLootPoint(root, "poacher_loot_bag_01", new Vector3(50f, 34f, 0f), "forest_bush");
        EnsureDynamicNpc(root, "east_slime_brown_02", new Vector3(44f, 23f, 0f), "slime_brown", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "east_slime_crystal_02", new Vector3(51f, 39f, 0f), "slime_crystal", My.Map.Entity.EFactionId.Beast, "default_monster");
    }

    static void EnsureNorthHeartlandExpansion(MapChunkEditorRoot root)
    {
        EnsureDynamicLootPoint(root, "north_dew_patch_01", new Vector3(28f, 49f, 0f), "forest_grass_clump");
        EnsureDynamicLootPoint(root, "north_dew_patch_02", new Vector3(36f, 50f, 0f), "forest_grass_clump");
        EnsureDynamicLootPoint(root, "north_relic_scrap_01", new Vector3(31f, 53f, 0f), "forest_leaf_pile");
        EnsureDynamicLootPoint(root, "north_root_cluster_01", new Vector3(34f, 56f, 0f), "forest_bush");
        EnsureDynamicNpc(root, "north_slime_crystal_02", new Vector3(34f, 51f, 0f), "slime_crystal", My.Map.Entity.EFactionId.Beast, "default_monster");
        EnsureDynamicNpc(root, "north_fly_swarm_02", new Vector3(29f, 55f, 0f), "forest_fly_swarm", My.Map.Entity.EFactionId.Beast, "default_monster");
    }

    static void EnsureDynamicLootPoint(MapChunkEditorRoot root, string uniqName, Vector3 position, string cfgId)
    {
        var overlayRoot = EnsureOverlayRoot(root, OverlayId);
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
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = false,
            RespawnInterval = 0f,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4LootPoint
            {
                CfgId = cfgId,
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.zero,
                OrgFactionId = My.Map.Entity.EFactionId.None,
            },
        };

        EditorUtility.SetDirty(go);
    }

    static void EnsureDynamicNpc(MapChunkEditorRoot root, string uniqName, Vector3 position, string cfgId, My.Map.Entity.EFactionId factionId, string enmityCfgId)
    {
        var overlayRoot = EnsureOverlayRoot(root, OverlayId);
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
            AppearCond = MakeNoneCond(),
            DisappearCond = MakeNoneCond(),
            WillRespawn = true,
            RespawnInterval = 30f,
            DungeonNodeId = -1,
            SpawnPolicy = EDungeonSpawnPolicy.Immediate,
            InitInfo = new EntityInitInfo4Npc
            {
                CfgId = cfgId,
                Position = new Vector2(position.x, position.y),
                FaceDir = Vector2.left,
                OrgFactionId = factionId,
                EnmityConfId = enmityCfgId,
            },
        };

        EditorUtility.SetDirty(go);
    }

}

