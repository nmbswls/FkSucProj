#if UNITY_EDITOR
using System.Collections.Generic;
using My.Dungeon;
using My.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Dungeon.Editor
{
    public static class DungeonEditorPreview
    {
        public const string ScenePath = "Assets/Scenes/Main/Main_Dungeon_TestCave.unity";

        private static DungeonGenerationResult _activeResult;
        private static WorldAreaRoot _activeRoot;
        private static string _statusText = string.Empty;
        private static bool _subscribed;

        [InitializeOnLoadMethod]
        private static void EnsureSceneGuiHook()
        {
            if (_subscribed)
            {
                return;
            }

            _subscribed = true;
            SceneView.duringSceneGui += OnSceneGui;
        }

        public static bool TryShow(string dungeonId, int seed, out string summary)
        {
            summary = string.Empty;
            DungeonConfigCatalog.EnsureLoaded();

            if (!OpenPreviewScene())
            {
                summary = "Preview cancelled or scene missing.";
                return false;
            }

            var root = Object.FindObjectOfType<WorldAreaRoot>();
            if (root == null)
            {
                summary = "WorldAreaRoot not found in Main_Dungeon_TestCave scene.";
                Debug.LogError(summary);
                return false;
            }

            root.EditorResolveFromSceneHierarchy();
            EnsureAccentTilemapLayer(root);

            var a = DungeonGenerator.Generate(dungeonId, seed);
            var b = DungeonGenerator.Generate(dungeonId, seed);
            if (a == null)
            {
                summary = "Generation failed. Check dungeon config assets.";
                Debug.LogError(summary);
                return false;
            }

            bool stable = b != null
                          && a.Rooms.Count == b.Rooms.Count
                          && a.WalkableCells.Count == b.WalkableCells.Count;

            if (!ApplyToScene(a, root))
            {
                summary = "Generation OK but tilemap stamp failed. Check Console.";
                return false;
            }

            _activeResult = a;
            _activeRoot = root;
            _statusText = BuildStatusText(a, seed, stable);

            FrameWalkableBounds(root, a);
            SceneView.RepaintAll();

            summary = _statusText;
            Debug.Log($"Dungeon preview: {_statusText}");
            return true;
        }

        public static void Clear()
        {
            _activeResult = null;
            _activeRoot = null;
            _statusText = string.Empty;
            SceneView.RepaintAll();
        }

        public static bool HasActivePreview => _activeResult != null && _activeRoot != null;

        private static bool OpenPreviewScene()
        {
            var active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return true;
            }

            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError($"Preview scene missing: {ScenePath}.");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        private static bool ApplyToScene(DungeonGenerationResult result, WorldAreaRoot root)
        {
            if (root.TileGrounds != null)
            {
                foreach (var tm in root.TileGrounds)
                {
                    if (tm != null)
                    {
                        Undo.RegisterCompleteObjectUndo(tm, "Dungeon Preview");
                    }
                }
            }

            if (root.TileHole != null)
            {
                Undo.RegisterCompleteObjectUndo(root.TileHole, "Dungeon Preview");
            }

            if (!DungeonTilemapStamper.Apply(result, root))
            {
                return false;
            }

            ApplyBornMarker(result, root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            return true;
        }

        private static void ApplyBornMarker(DungeonGenerationResult result, WorldAreaRoot root)
        {
            if (result.RuntimeMapData?.NamedPoints == null)
            {
                return;
            }

            foreach (var point in result.RuntimeMapData.NamedPoints)
            {
                if (point.PointType != ENamedPointType.BornPos)
                {
                    continue;
                }

                var marker = root.transform.Find("PreviewBornPos");
                if (marker == null)
                {
                    var go = new GameObject("PreviewBornPos");
                    go.transform.SetParent(root.transform, false);
                    marker = go.transform;
                    Undo.RegisterCreatedObjectUndo(go, "Dungeon Preview Born");
                }

                Undo.RecordObject(marker, "Dungeon Preview Born");
                marker.position = new Vector3(point.Position.x, point.Position.y, 0f);
                break;
            }
        }

        private static void FrameWalkableBounds(WorldAreaRoot root, DungeonGenerationResult result)
        {
            if (result.WalkableCells == null || result.WalkableCells.Count == 0 || root.Grid == null)
            {
                return;
            }

            var grid = root.Grid;
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var cell in result.WalkableCells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }

            var minWorld = grid.GetCellCenterWorld(new Vector3Int(minX, minY, 0)) - grid.cellSize * 0.5f;
            var maxWorld = grid.GetCellCenterWorld(new Vector3Int(maxX, maxY, 0)) + grid.cellSize * 0.5f;
            var center = (minWorld + maxWorld) * 0.5f;
            var size = maxWorld - minWorld + grid.cellSize;

            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.Frame(new Bounds(center, size), false);
            }
        }

        private static string BuildStatusText(DungeonGenerationResult result, int seed, bool stable)
        {
            int entityCount = result.RuntimeMapData?.EntityRefreshInfo?.Count ?? 0;
            return $"seed={seed} rooms={result.Rooms.Count} walkable={result.WalkableCells.Count} entities={entityCount} stable={(stable ? "OK" : "FAIL")}";
        }

        private static void OnSceneGui(SceneView view)
        {
            DrawHud();

            if (_activeResult == null || _activeRoot == null || _activeRoot.Grid == null)
            {
                return;
            }

            var grid = _activeRoot.Grid;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            foreach (var room in _activeResult.Rooms)
            {
                if (room.Meta == null)
                {
                    continue;
                }

                DrawRoomBounds(grid, room);
            }

            DrawBornPoint(grid, _activeResult);
            DrawEntitySlots(grid, _activeResult);
        }

        private static void DrawHud()
        {
            Handles.BeginGUI();
            var rect = new Rect(12f, 12f, 420f, HasActivePreview ? 88f : 36f);
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(rect);
            GUILayout.Label("Dungeon Preview", EditorStyles.boldLabel);
            if (HasActivePreview)
            {
                GUILayout.Label(_statusText);
                GUILayout.Label("Green=Start  Yellow=Combat  Cyan=DestroyObj  Blue=Born");
            }
            else
            {
                GUILayout.Label("Run Tools/Dungeon/Preview to visualize layout.");
            }

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawRoomBounds(Grid grid, PlacedRoom room)
        {
            int ox = room.GridOriginCells.x;
            int oy = room.GridOriginCells.y;
            int sx = room.Meta.SizeCells.x;
            int sy = room.Meta.SizeCells.y;

            var min = grid.GetCellCenterWorld(new Vector3Int(ox, oy, 0)) - grid.cellSize * 0.5f;
            var max = grid.GetCellCenterWorld(new Vector3Int(ox + sx - 1, oy + sy - 1, 0)) + grid.cellSize * 0.5f;
            var center = (min + max) * 0.5f;
            var size = max - min;

            Color color = room.Role switch
            {
                EDungeonRoomRole.Start => new Color(0.2f, 0.95f, 0.35f, 1f),
                EDungeonRoomRole.Combat => new Color(0.95f, 0.85f, 0.15f, 1f),
                _ => new Color(0.75f, 0.75f, 0.75f, 1f),
            };

            Handles.color = color;
            Handles.DrawWireCube(center, new Vector3(size.x, size.y, 0.05f));

            var labelPos = grid.GetCellCenterWorld(new Vector3Int(ox + sx / 2, oy + sy / 2, 0));
            Handles.Label(labelPos, $"{room.GraphNodeId}:{room.TemplateId}");
        }

        private static void DrawBornPoint(Grid grid, DungeonGenerationResult result)
        {
            if (result.RuntimeMapData?.NamedPoints == null)
            {
                return;
            }

            foreach (var point in result.RuntimeMapData.NamedPoints)
            {
                if (point.PointType != ENamedPointType.BornPos)
                {
                    continue;
                }

                var pos = new Vector3(point.Position.x, point.Position.y, 0f);
                Handles.color = new Color(0.25f, 0.55f, 1f, 1f);
                Handles.SphereHandleCap(0, pos, Quaternion.identity, grid.cellSize.x * 0.35f, EventType.Repaint);
                Handles.Label(pos + Vector3.up * grid.cellSize.y * 0.6f, "Born");
                break;
            }
        }

        private static void DrawEntitySlots(Grid grid, DungeonGenerationResult result)
        {
            if (result.RuntimeMapData?.EntityRefreshInfo == null)
            {
                return;
            }

            Handles.color = new Color(0.2f, 0.95f, 0.95f, 1f);
            foreach (var refresh in result.RuntimeMapData.EntityRefreshInfo)
            {
                if (refresh.InitInfo == null)
                {
                    continue;
                }

                var pos = new Vector3(refresh.InitInfo.Position.x, refresh.InitInfo.Position.y, 0f);
                Handles.DrawWireDisc(pos, Vector3.forward, grid.cellSize.x * 0.28f);
                Handles.Label(pos + Vector3.up * grid.cellSize.y * 0.45f, refresh.UniqName);
            }
        }

        private static void EnsureAccentTilemapLayer(WorldAreaRoot root)
        {
            if (root.TileGrounds != null && root.TileGrounds.Length > 1 && root.TileGrounds[1] != null)
            {
                return;
            }

            if (root.Grid == null || root.TileGrounds == null || root.TileGrounds.Length == 0 || root.TileGrounds[0] == null)
            {
                return;
            }

            var accentGo = new GameObject("Tilemap_Accent");
            accentGo.transform.SetParent(root.Grid.transform, false);
            var accentMap = accentGo.AddComponent<Tilemap>();
            var accentRenderer = accentGo.AddComponent<TilemapRenderer>();
            accentRenderer.sortingOrder = 1;

            Undo.RegisterCreatedObjectUndo(accentGo, "Dungeon Accent Tilemap");
            root.EditorSetTileGrounds(new[] { root.TileGrounds[0], accentMap });
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
    }
}
#endif
