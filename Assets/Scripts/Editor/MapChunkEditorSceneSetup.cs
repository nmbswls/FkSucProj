using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 从运行时场景复制 GridRoot 到 Editor 场景，供 Tilemap chunk 导出
public static class MapChunkEditorSceneSetup
{
    const string RuntimeScenePath = "Assets/Scenes/Main/Main_Area_01.unity";

    public static bool CopyGridFromRuntimeScene(MapChunkEditorRoot editorRoot)
    {
        if (editorRoot == null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "MapChunkEditorRoot is missing.", "OK");
            return false;
        }

        if (editorRoot.StaticPrefabRoot == null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "StaticPrefabRoot is not assigned on MapChunkEditorRoot.", "OK");
            return false;
        }

        var staticRoot = editorRoot.StaticPrefabRoot;
        if (staticRoot.Find("GridRoot") != null)
        {
            EditorUtility.DisplayDialog("Map Chunk Setup", "GridRoot already exists under StaticRoot.", "OK");
            return false;
        }

        var editorScene = editorRoot.gameObject.scene;
        Scene runtimeScene = default;
        bool openedAdditive = false;

        try
        {
            runtimeScene = SceneManager.GetSceneByPath(RuntimeScenePath);
            if (!runtimeScene.isLoaded)
            {
                runtimeScene = EditorSceneManager.OpenScene(RuntimeScenePath, OpenSceneMode.Additive);
                openedAdditive = true;
            }

            WorldAreaRoot runtimeArea = null;
            foreach (var root in runtimeScene.GetRootGameObjects())
            {
                runtimeArea = root.GetComponentInChildren<WorldAreaRoot>(true);
                if (runtimeArea != null)
                {
                    break;
                }
            }

            if (runtimeArea == null || runtimeArea.Grid == null)
            {
                EditorUtility.DisplayDialog("Map Chunk Setup", "Grid not found in runtime scene.", "OK");
                return false;
            }

            runtimeArea.ApplyTileGroundsFromLogicHeightConfig();

            var clone = Object.Instantiate(runtimeArea.Grid.gameObject);
            clone.name = "GridRoot";
            clone.transform.SetParent(staticRoot, false);

            var layerNames = runtimeArea.LogicHeightConfig?.GroundLayerNames != null &&
                             runtimeArea.LogicHeightConfig.GroundLayerNames.Length > 0
                ? runtimeArea.LogicHeightConfig.GroundLayerNames.ToHashSet()
                : runtimeArea.TileGrounds
                    .Where(t => t != null)
                    .Select(t => t.name)
                    .ToHashSet();
            var tilemaps = clone.GetComponentsInChildren<Tilemap>(true)
                .Where(t => layerNames.Count == 0 || layerNames.Contains(t.name))
                .ToArray();

            Undo.RegisterCreatedObjectUndo(clone, "Copy GridRoot");
            EditorSceneManager.MarkSceneDirty(editorScene);

            Debug.Log($"[MapChunkSetup] Copied GridRoot with {tilemaps.Length} tilemap layer(s) to {editorScene.name}.");
            return true;
        }
        finally
        {
            if (openedAdditive && runtimeScene.isLoaded)
            {
                EditorSceneManager.CloseScene(runtimeScene, true);
            }
        }
    }
}

public class MapChunkExporterWindow : EditorWindow
{
    [SerializeField] private GameObject areaRoot;
    [SerializeField] private string mapName = "Main_Area_01";
    [SerializeField] private float chunkCellSize = 32f;
    [SerializeField] private Vector2 chunkOrigin;
    [SerializeField] private int backgroundSortingOrder;
    [SerializeField] private bool exportBackgroundChunks = true;
    [SerializeField] private bool exportTilemapChunks = true;
    [SerializeField] private bool exportVisualBake = true;
    [SerializeField] private bool exportWalkGridPrefab = true;

    [MenuItem("Window/Map Chunk Exporter")]
    public static void Open()
    {
        GetWindow<MapChunkExporterWindow>("Map Chunk Exporter");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Map Source", EditorStyles.boldLabel);
        areaRoot = (GameObject)EditorGUILayout.ObjectField("Area Root", areaRoot, typeof(GameObject), true);
        if (GUILayout.Button("Use Selected AreaRoot"))
        {
            if (Selection.activeGameObject != null)
            {
                areaRoot = Selection.activeGameObject;
            }
        }

        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        if (chunkEditor == null)
        {
            EditorGUILayout.HelpBox("AreaRoot 上需要 MapChunkEditorRoot 组件。", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Source Texture",
                chunkEditor.SourceTexture != null ? chunkEditor.SourceTexture.name : "(none)");
            EditorGUILayout.LabelField("Source Pixel Size", chunkEditor.SourceTextureSize.ToString());
            EditorGUILayout.LabelField("Imported Asset Size", chunkEditor.ImportedTextureSize.ToString());
            EditorGUILayout.LabelField("Texture PPU", chunkEditor.TexturePPU.ToString());
            EditorGUILayout.LabelField("Slice Pixel Size", chunkEditor.SlicePixelSize.ToString());
            EditorGUILayout.LabelField("Grid Status",
                MapChunkEditorTilemapResolver.HasTilemapSource(chunkEditor) ? "Ready" : "Missing");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Map Chunk Key", "Resources/MapChunk/{scene_name}");
        mapName = EditorGUILayout.TextField("Scene Name (Variant)", mapName);
        chunkCellSize = EditorGUILayout.FloatField("Chunk Cell Size", chunkCellSize);
        chunkOrigin = EditorGUILayout.Vector2Field("Chunk Origin", chunkOrigin);
        backgroundSortingOrder = EditorGUILayout.IntField("Background Sorting Order", backgroundSortingOrder);

        exportBackgroundChunks = EditorGUILayout.Toggle("Background (bg_*)", exportBackgroundChunks);
        exportTilemapChunks = EditorGUILayout.Toggle("Walk Grid Chunks (tm_*)", exportTilemapChunks);
        using (new EditorGUI.DisabledScope(!exportTilemapChunks))
        {
            exportVisualBake = EditorGUILayout.Toggle("  Bake Visual Layers", exportVisualBake);
        }
        exportWalkGridPrefab = EditorGUILayout.Toggle("Walk Grid Prefab (GridRoot)", exportWalkGridPrefab);
        if (chunkEditor != null && exportWalkGridPrefab)
        {
            chunkEditor.ExportGridRoot3DCollision = EditorGUILayout.Toggle(
                "  GridRoot 3D Collision (slow)",
                chunkEditor.ExportGridRoot3DCollision);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Sync From MapChunkEditorRoot"))
        {
            SyncFromScene();
        }

        if (chunkEditor != null && !MapChunkEditorTilemapResolver.HasTilemapSource(chunkEditor))
        {
            EditorGUILayout.HelpBox("未找到可走 Tilemap：请在 WorldAreaRoot.TileGrounds 或 StaticRoot/GridRoot 下配置。", MessageType.Warning);
        }

        if (exportTilemapChunks && exportVisualBake)
        {
            EditorGUILayout.HelpBox(
                "Visual Bake：DualGrid View / Cliff 等 RuleTile 在导出前 bake 成静态 Tile，" +
                "写入 tm_* 的 Baked_* 层；逻辑层仍保留原始 Tile 引用。",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Painted Background", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "手绘背景 AI 工作流：在 Map Paint Background 窗口配置 PaintWorldRect、Generate Atlas、Import 回稿。",
            MessageType.Info);
        if (GUILayout.Button("Open Map Paint Background Window"))
        {
            EditorApplication.ExecuteMenuItem("Window/Map Paint Background");
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Export Map"))
        {
            Export();
        }
    }

    void SyncFromScene()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        MapChunkEditorUtility.SyncChunkSettings(chunkEditor, ref chunkCellSize, ref chunkOrigin);
        var key = MapChunkEditorUtility.ResolveMapChunkKey(chunkEditor);
        if (!string.IsNullOrEmpty(key))
        {
            mapName = key;
        }
    }

    void Export()
    {
        var chunkEditor = MapChunkEditorUtility.Resolve(areaRoot);
        if (chunkEditor == null)
        {
            EditorUtility.DisplayDialog("Map Chunk Export", "MapChunkEditorRoot not found on Area Root.", "OK");
            return;
        }

        if (!exportBackgroundChunks && !exportTilemapChunks && !exportWalkGridPrefab)
        {
            EditorUtility.DisplayDialog("Map Chunk Export", "Select at least one export target.", "OK");
            return;
        }

        MapChunkEditorUtility.PushChunkSettings(chunkEditor, chunkCellSize, chunkOrigin);
        chunkEditor.SceneName = mapName;
        EditorUtility.SetDirty(chunkEditor);

        var result = MapChunkExportCore.Export(
            chunkEditor,
            mapName,
            backgroundSortingOrder,
            chunkCellSize,
            chunkOrigin,
            exportBackgroundChunks,
            exportTilemapChunks,
            exportWalkGridPrefab,
            exportVisualBake);

        if (!result.Success)
        {
            EditorUtility.DisplayDialog("Map Chunk Export", result.Message, "OK");
            Debug.LogWarning("[MapChunkExport] " + result.Message);
            return;
        }

        Debug.Log("[MapChunkExport] " + result.Message);
        EditorUtility.DisplayDialog("Map Chunk Export", result.Message, "OK");
        if (result.Database != null)
        {
            EditorGUIUtility.PingObject(result.Database);
        }
    }

    void OnEnable()
    {
        if (areaRoot == null)
        {
            areaRoot = MapChunkEditorUtility.FindInActiveScene()?.gameObject;
        }

        SyncFromScene();
    }
}
