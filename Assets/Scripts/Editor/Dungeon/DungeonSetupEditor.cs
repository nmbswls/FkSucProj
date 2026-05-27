#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using My.Dungeon;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace My.Dungeon.Editor
{
    public static class DungeonSetupEditor
    {
        private const string ScenePath = "Assets/Scenes/Main/Main_Dungeon_TestCave.unity";
        private const string ConfigRoot = "Assets/Resources/Config/Dungeon";
        private const string RoomRoot = "Assets/Resources/Config/Dungeon/Rooms";
        private const string PatternRoot = "Assets/Resources/Config/Dungeon/Patterns";
        private const string MapExportPath = "Assets/Resources/MapExport/dungeon_test_cave.asset";

        [MenuItem("Tools/Dungeon/Setup Test Cave P1")]
        public static void SetupTestCaveP1()
        {
            EnsureFolders();
            var tileset = CreateOrLoadFloorTileset();
            var rooms = CreateTestRoomMetas();
            var def = CreateOrLoadDungeonDef(tileset, rooms);
            CreateOrUpdateMapExport();
            UpdateJsonConfigs();
            CreateShellScene();
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dungeon P1 setup complete. Run Tools/Dungeon/Preview Generation to visualize.");
            EditorGUIUtility.PingObject(def);
            EditorUtility.DisplayDialog(
                "Dungeon P1 Setup",
                "配置已就绪：\n"
                + "- Assets/Resources/Config/Dungeon/\n"
                + "- Main_Dungeon_TestCave 场景\n"
                + "- Build Settings 已加入场景\n\n"
                + "下一步：Tools/Dungeon/Preview Generation (seed=12345)\n"
                + "或在 Play 中 Console：dungeon_test 12345",
                "OK");
        }

        [MenuItem("Tools/Dungeon/Preview Generation (seed=12345)")]
        public static void PreviewGeneration()
        {
            if (DungeonEditorPreview.TryShow("test_cave", 12345, out var summary))
            {
                EditorUtility.DisplayDialog(
                    "Dungeon Preview",
                    summary + "\n\n已打开测试场景并在 Scene 视图显示布局。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Dungeon Preview Failed", summary, "OK");
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Config/Dungeon"))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Config", "Dungeon");
            }

            if (!AssetDatabase.IsValidFolder(RoomRoot))
            {
                AssetDatabase.CreateFolder(ConfigRoot, "Rooms");
            }

            if (!AssetDatabase.IsValidFolder(PatternRoot))
            {
                AssetDatabase.CreateFolder(ConfigRoot, "Patterns");
            }
        }

        private static DungeonFloorTileset CreateOrLoadFloorTileset()
        {
            string path = $"{ConfigRoot}/test_cave_floor.asset";
            var tileset = AssetDatabase.LoadAssetAtPath<DungeonFloorTileset>(path);
            if (tileset == null)
            {
                tileset = ScriptableObject.CreateInstance<DungeonFloorTileset>();
                AssetDatabase.CreateAsset(tileset, path);
            }

            var tiles = CollectGroundTilesFromVillage();
            tileset.TilesetId = "test_cave_floor";
            PopulateExamplePatterns(tileset, tiles);
            tileset.AccentDensity = 0.12f;
            tileset.BaseGridPhase = Vector2Int.zero;
            tileset.Allow1x1Patterns = false;
            EditorUtility.SetDirty(tileset);
            return tileset;
        }

        private static void PopulateExamplePatterns(DungeonFloorTileset tileset, List<TileBase> tiles)
        {
            if (tiles.Count == 0)
            {
                tileset.BasePatterns = new List<DungeonFloorPattern>();
                tileset.AccentPatterns = new List<DungeonFloorPattern>();
                return;
            }

            var tileA = tiles[0];
            var tileB = tiles.Count > 1 ? tiles[1] : tileA;

            tileset.BasePatterns = new List<DungeonFloorPattern>
            {
                CreateOrLoadPattern(
                    "cave_base_2x2_a",
                    EDungeonFloorPatternKind.Base,
                    new Vector2Int(2, 2),
                    100,
                    1,
                    new[]
                    {
                        Cell(0, 0, tileA), Cell(1, 0, tileB), Cell(0, 1, tileB), Cell(1, 1, tileA),
                    }),
                CreateOrLoadPattern(
                    "cave_base_2x2_b",
                    EDungeonFloorPatternKind.Base,
                    new Vector2Int(2, 2),
                    100,
                    1,
                    new[]
                    {
                        Cell(0, 0, tileB), Cell(1, 0, tileA), Cell(0, 1, tileA), Cell(1, 1, tileB),
                    }),
                CreateOrLoadPattern(
                    "cave_base_1x2_h",
                    EDungeonFloorPatternKind.Base,
                    new Vector2Int(2, 1),
                    50,
                    1,
                    new[]
                    {
                        Cell(0, 0, tileA), Cell(1, 0, tileB),
                    }),
                CreateOrLoadPattern(
                    "cave_base_1x2_v",
                    EDungeonFloorPatternKind.Base,
                    new Vector2Int(1, 2),
                    50,
                    1,
                    new[]
                    {
                        Cell(0, 0, tileA), Cell(0, 1, tileB),
                    }),
            };

            tileset.AccentPatterns = new List<DungeonFloorPattern>
            {
                CreateOrLoadPattern(
                    "cave_accent_1x2_h",
                    EDungeonFloorPatternKind.Accent,
                    new Vector2Int(2, 1),
                    50,
                    1,
                    new[]
                    {
                        Cell(0, 0, tileB), Cell(1, 0, tileA),
                    }),
                CreateOrLoadPattern(
                    "cave_accent_1x1",
                    EDungeonFloorPatternKind.Accent,
                    Vector2Int.one,
                    10,
                    1,
                    new[] { Cell(0, 0, tileB) }),
            };
        }

        private static DungeonFloorPatternCell Cell(int x, int y, TileBase tile)
        {
            return new DungeonFloorPatternCell
            {
                LocalOffset = new Vector2Int(x, y),
                Tile = tile,
            };
        }

        private static DungeonFloorPattern CreateOrLoadPattern(
            string id,
            EDungeonFloorPatternKind kind,
            Vector2Int size,
            int sizePriority,
            int weight,
            DungeonFloorPatternCell[] cells)
        {
            string path = $"{PatternRoot}/{id}.asset";
            var pattern = AssetDatabase.LoadAssetAtPath<DungeonFloorPattern>(path);
            if (pattern == null)
            {
                pattern = ScriptableObject.CreateInstance<DungeonFloorPattern>();
                AssetDatabase.CreateAsset(pattern, path);
            }

            pattern.PatternId = id;
            pattern.Kind = kind;
            pattern.SizeCells = size;
            pattern.Anchor = Vector2Int.zero;
            pattern.SizePriority = sizePriority;
            pattern.Weight = weight;
            pattern.Cells = new List<DungeonFloorPatternCell>(cells);
            EditorUtility.SetDirty(pattern);
            return pattern;
        }

        private static List<TileBase> CollectGroundTilesFromVillage()
        {
            var result = new List<TileBase>();
            var tileA = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath("6047915fd8c019d46a7e8222d301cd4e"));
            var tileB = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath("140cd02bb363245459f019dff9497477"));
            if (tileA != null) result.Add(tileA);
            if (tileB != null) result.Add(tileB);

            if (result.Count == 0)
            {
                var guids = AssetDatabase.FindAssets("t:TileBase");
                foreach (var g in guids)
                {
                    var t = AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(g));
                    if (t != null)
                    {
                        result.Add(t);
                        if (result.Count >= 2) break;
                    }
                }
            }

            return result;
        }

        private static List<DungeonRoomExportMeta> CreateTestRoomMetas()
        {
            var list = new List<DungeonRoomExportMeta>();

            list.Add(CreateRoomMeta("test_cave_start_12x10", EDungeonRoomRole.Start, new Vector2Int(12, 10), 1,
                new[] { MakeDoor(EDungeonCardinalDir.South, 5, 0, 2) },
                new[] { MakeSlot("born", 6, 5) }));

            list.Add(CreateRoomMeta("test_cave_combat_16x14", EDungeonRoomRole.Combat, new Vector2Int(16, 14), 2,
                new[] {
                    MakeDoor(EDungeonCardinalDir.North, 7, 13, 2),
                    MakeDoor(EDungeonCardinalDir.South, 7, 0, 2),
                },
                new[] { MakeSlot("obj_a", 8, 7) }));

            list.Add(CreateRoomMeta("test_cave_combat_20x12", EDungeonRoomRole.Combat, new Vector2Int(20, 12), 2,
                new[] {
                    MakeDoor(EDungeonCardinalDir.West, 0, 5, 2),
                    MakeDoor(EDungeonCardinalDir.East, 19, 5, 2),
                },
                new[] { MakeSlot("obj_b", 10, 6) }));

            list.Add(CreateRoomMeta("test_cave_combat_14x16", EDungeonRoomRole.Combat, new Vector2Int(14, 16), 1,
                new[] {
                    MakeDoor(EDungeonCardinalDir.North, 6, 15, 2),
                    MakeDoor(EDungeonCardinalDir.South, 6, 0, 2),
                },
                new[] { MakeSlot("obj_c", 7, 8) }));

            return list;
        }

        private static DungeonRoomExportMeta CreateRoomMeta(
            string id,
            EDungeonRoomRole role,
            Vector2Int size,
            int weight,
            DungeonDoorSocketExport[] doors,
            DungeonEntitySlotExport[] slots)
        {
            string path = $"{RoomRoot}/{id}.asset";
            var meta = AssetDatabase.LoadAssetAtPath<DungeonRoomExportMeta>(path);
            if (meta == null)
            {
                meta = ScriptableObject.CreateInstance<DungeonRoomExportMeta>();
                AssetDatabase.CreateAsset(meta, path);
            }

            meta.TemplateId = id;
            meta.Role = role;
            meta.Weight = weight;
            meta.SizeCells = size;
            meta.DoorSockets = new List<DungeonDoorSocketExport>(doors);
            meta.EntitySlots = new List<DungeonEntitySlotExport>(slots);
            FillInteriorMask(meta);
            EditorUtility.SetDirty(meta);
            return meta;
        }

        private static void FillInteriorMask(DungeonRoomExportMeta meta)
        {
            meta.EnsureMaskSize();
            for (int y = 0; y < meta.SizeCells.y; y++)
            {
                for (int x = 0; x < meta.SizeCells.x; x++)
                {
                    bool border = x == 0 || y == 0 || x == meta.SizeCells.x - 1 || y == meta.SizeCells.y - 1;
                    meta.WalkableMask[y * meta.SizeCells.x + x] = (byte)(border ? 0 : 1);
                }
            }

            if (meta.DoorSockets != null)
            {
                foreach (var door in meta.DoorSockets)
                {
                    for (int i = 0; i < door.Width; i++)
                    {
                        int cx = door.LocalCell.x;
                        int cy = door.LocalCell.y;
                        switch (door.Direction)
                        {
                            case EDungeonCardinalDir.South:
                            case EDungeonCardinalDir.North:
                                cx = door.LocalCell.x + i;
                                cy = door.LocalCell.y;
                                break;
                            case EDungeonCardinalDir.West:
                            case EDungeonCardinalDir.East:
                                cx = door.LocalCell.x;
                                cy = door.LocalCell.y + i;
                                break;
                        }

                        if (cx >= 0 && cy >= 0 && cx < meta.SizeCells.x && cy < meta.SizeCells.y)
                        {
                            meta.WalkableMask[cy * meta.SizeCells.x + cx] = 1;
                        }
                    }
                }
            }
        }

        private static DungeonDoorSocketExport MakeDoor(EDungeonCardinalDir dir, int x, int y, int width)
        {
            return new DungeonDoorSocketExport
            {
                Direction = dir,
                LocalCell = new Vector2Int(x, y),
                Width = width,
            };
        }

        private static DungeonEntitySlotExport MakeSlot(string slotId, int x, int y)
        {
            return new DungeonEntitySlotExport
            {
                SlotId = slotId,
                LocalCell = new Vector2Int(x, y),
                FaceDir = Vector2.down,
            };
        }

        private static DungeonDef CreateOrLoadDungeonDef(DungeonFloorTileset tileset, List<DungeonRoomExportMeta> rooms)
        {
            string path = $"{ConfigRoot}/test_cave.asset";
            var def = AssetDatabase.LoadAssetAtPath<DungeonDef>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DungeonDef>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.DungeonId = "test_cave";
            def.MinRooms = 5;
            def.MaxRooms = 7;
            def.SlotStrideCells = 24;
            def.GraphRandomness = 0.35f;
            def.Branchiness = 0.5f;
            def.CorridorWidthCells = 2;
            def.DestroyObjCfgId = "obj_01";
            def.FloorTileset = tileset;
            def.RoomTemplates = rooms;
            EditorUtility.SetDirty(def);
            return def;
        }

        private static void CreateOrUpdateMapExport()
        {
            var db = AssetDatabase.LoadAssetAtPath<MapExportDatabase>(MapExportPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MapExportDatabase>();
                AssetDatabase.CreateAsset(db, MapExportPath);
            }

            db.AreaId = "dungeon_test_cave";
            db.Buckets ??= new List<MapExportDatabase.ChunkExportItem>();
            db.EntityRefreshInfo ??= new List<DynamicEntityRefreshInfo>();
            db.NamedPoints ??= new List<NamedPoint>();
            EditorUtility.SetDirty(db);
        }

        private static void UpdateJsonConfigs()
        {
            AppendOverlayIfMissing();
            AppendVariantIfMissing();
        }

        private static void AppendOverlayIfMissing()
        {
            string path = "Assets/Resources/Config/Json/demo_tbareaoverlaystateinfo.json";
            var text = File.ReadAllText(path);
            if (text.Contains("\"dungeon_test_cave\""))
            {
                return;
            }

            const string entry = ",\n  {\n    \"id\": \"dungeon_test_cave\",\n    \"name\": \"测试洞穴\",\n    \"desc\": \"程序化地牢 P1\",\n    \"var_id\": \"dungeon_test_cave\",\n    \"day_period_limit\": 0,\n    \"map_data_name\": \"dungeon_test_cave\",\n    \"can_teleport\": false,\n    \"is_home\": false,\n    \"is_secret_base\": false,\n    \"always_alert\": true,\n    \"is_civil_area\": false,\n    \"is_danger_area\": true,\n    \"hunting_target\": false,\n    \"hunting_unlock_conds\": []\n  }";
            text = text.TrimEnd();
            if (text.EndsWith("]"))
            {
                text = text.Substring(0, text.Length - 1) + entry + "\n]";
                File.WriteAllText(path, text);
            }
        }

        private static void AppendVariantIfMissing()
        {
            string path = "Assets/Resources/Config/Json/demo_tbareavariantinfo.json";
            var text = File.ReadAllText(path);
            if (text.Contains("\"dungeon_test_cave\""))
            {
                return;
            }

            const string entry = ",\n  {\n    \"var_id\": \"dungeon_test_cave\",\n    \"name\": \"测试洞穴\",\n    \"desc\": \"程序化地牢 P1\",\n    \"logic_area_id\": \"dungeon_test_cave\",\n    \"show_in_map\": false,\n    \"show_conds\": [],\n    \"scene_name\": \"Main_Dungeon_TestCave\",\n    \"thumb_map\": \"\",\n    \"is_secret_base\": false\n  }";
            text = text.TrimEnd();
            if (text.EndsWith("]"))
            {
                text = text.Substring(0, text.Length - 1) + entry + "\n]";
                File.WriteAllText(path, text);
            }
        }

        private static void CreateShellScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rootGo = new GameObject("WorldAreaRoot");
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(rootGo.transform, false);
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            var groundGo = new GameObject("Tilemap_Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            var tilemap = groundGo.AddComponent<Tilemap>();
            groundGo.AddComponent<TilemapRenderer>();

            var accentGo = new GameObject("Tilemap_Accent");
            accentGo.transform.SetParent(gridGo.transform, false);
            var accentMap = accentGo.AddComponent<Tilemap>();
            var accentRenderer = accentGo.AddComponent<TilemapRenderer>();
            accentRenderer.sortingOrder = 1;

            var staticRoot = new GameObject("StaticPrefabRoot");
            staticRoot.transform.SetParent(rootGo.transform, false);

            var born = new GameObject("PlayerBornPos");
            born.transform.SetParent(rootGo.transform, false);

            var areaRoot = rootGo.AddComponent<WorldAreaRoot>();
            areaRoot.Grid = grid;
            areaRoot.TileGrounds = new[] { tilemap, accentMap };
            areaRoot.TileHole = null;
            areaRoot.PlayerBornPos = born.transform;
            areaRoot.StaticPrefabRoot = staticRoot.transform;

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;
            foreach (var s in scenes)
            {
                if (s.path == ScenePath)
                {
                    s.enabled = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
